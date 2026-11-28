#import "../../Report-Templates/templates/report.typ": *
#import "../../Report-Templates/info.typ": info

#let reportInfo = (
  title: "Ray Tracing",
  subtitle: "计算机图形学实验 3",
  time: "2026 年 11 月",
  banner: "../../Report-Templates/images/sysu.png",
)
#show: report.with(..reportInfo, ..info)
#show heading: it => block(sticky: true, it)
#show raw: set text(font: ("Menlo", "DejaVu Sans Mono", "Courier New"))
#let results = json("results.json")
#let bench = results.benchmark_summary
#let number(x) = str(calc.round(x, digits: 3))
#let pair(a, b, ca, cb) = grid(
  columns: (1fr, 1fr), gutter: 10pt,
  image("images/" + a + ".png", width: 100%),
  image("images/" + b + ".png", width: 100%),
  align(center, ca), align(center, cb),
)

= 开发环境与流程

本实验在搭载 Apple Silicon 的 Mac 上使用 Apple Clang 21 和 C++17 实现路径追踪渲染器，以 CMake 构建。窗口和事件处理沿用课程的 SDL2 框架，向量运算使用框架提供的 `vec3`，图像解码与 PNG 输出使用仓库已有的 stb 库。

摄像机对像素内的随机采样点发射射线。命中物体后，渲染器累加自发光，按材质采样散射方向，并用材质给出的衰减系数为后续贡献加权。射线离开场景时累加天空或背景的贡献。渲染器对每个像素的线性颜色样本求平均，再做平方根 Gamma 校正。工作线程逐行计算并发布完整行，SDL 主线程加锁读取共享画布。

= 基础任务

== 射线、摄像机与天空背景

射线以起点 $o$ 和方向 $d$ 表示，参数 $t$ 对应的位置为 $p(t) = o + t d$。射线还记录快门采样时刻。摄像机根据自身位置、目标点和竖直方向构建正交基。渲染器根据射线单位方向的竖直分量，在白色与蓝色之间插值得到天空颜色。

== 球体、法线与抗锯齿

球面求交将射线方程代入球面方程，按判别式求实根，取有效区间内最小的根。求交记录包含朝向入射侧的法线、表面正反面标记、纹理坐标（UV）与材质。法线着色将三个分量映射到 [0, 1]，作为 RGB 颜色。对像素内随机采样的颜色取平均可减轻轮廓锯齿。

#figure(pair("sky", "sphere", [天空背景], [球体和地面]), caption: [天空渐变与法线着色。])

#pagebreak()
== 漫反射、金属与电介质

漫反射将表面法线与均匀采样的随机单位向量相加，得到服从余弦分布的散射方向。向量和接近零时改用法线。金属在镜面反射方向上加入由粗糙度控制的扰动。电介质按入射侧确定折射率之比，以 Snell 定律计算折射方向，用 Schlick 近似估计反射概率，满足全反射条件时只计算反射。左侧空心玻璃球以两层同心球面表示空气与玻璃的界面。

#figure(image("images/materials.png", width: 72%), caption: [玻璃、漫反射和金属。640 × 360，每像素 256 个样本。])

== 可调整摄像机与聚焦模糊

给定焦平面上的采样点，薄透镜模型从光圈圆盘内随机选取射线起点。光圈直径为零时，射线从摄像机中心出发。下图固定摄像机位置、焦平面距离、路径采样种子与每像素样本数，对比两种光圈直径。右图蓝色球较清晰，离焦的地面边缘和反射细节更模糊。

#figure(pair("pinhole", "defocus", [光圈直径 0], [光圈直径 0.4]), caption: [景深对比。每像素 256 个样本。])

#pagebreak()
== 第一册最终场景

参照第一册第 13 章，场景由棋盘纹理地面、随机生成的小球和三个主体球组成，顶层共 486 个物体。小球的布局种子为 2026，材质包括漫反射、金属和玻璃。摄像机位于 (13, 2, 3)，朝向原点，垂直视场角为 20°，焦平面距离为 10，光圈直径为 0.1。

#figure(image("images/random.png", width: 100%), caption: [800 × 450，每像素 256 个样本，最大路径深度 40。])

渲染时将求交参数的下界设为 0.001，以减少散射射线与出发面的自相交。

#pagebreak()
== BVH 构建与计时对比

包围体层次结构（BVH）使用各物体的轴对齐包围盒。运动球的包围盒由其起点和终点处的包围盒合并得到。构建时沿节点包围盒的最长轴，按物体包围盒的中心坐标取中位数划分，再递归构建两棵子树。遍历先检查节点包围盒，第一棵子树命中后，用命中处的射线参数缩小第二棵子树的搜索上界。

射线在某轴上的方向分量接近零时，直接判断起点是否落在该轴的包围区间内，以避免除以零或近零值。矩形的包围盒略向外扩张，以免厚度为零。每个叶节点保存一个物体，避免在同一叶节点内对体积介质重复采样。

两种求交方式使用同一个含 486 个物体的随机场景，分辨率为 320 × 180，每像素 16 个样本，最大路径深度为 40，工作线程为 8 个，路径采样种子为 2026。每轮分别启用和关闭 BVH 各一次，共三轮，轮换先后顺序。渲染从启动工作线程前计时，至所有线程退出为止，包含路径积分和 Gamma 校正。场景与 BVH 构建单独计时，PNG 编码和写入在渲染计时结束后进行。

#table(
  columns: (auto, 1fr, 1fr),
  align: (left, right, right), inset: 8pt,
  [*轮次*], [*BVH 渲染时间（s）*], [*逐物体求交渲染时间（s）*],
  ..range(1, 4).map(n => {
    let fast = results.benchmark.filter(r => r.repetition == n and r.bvh).first()
    let slow = results.benchmark.filter(r => r.repetition == n and not r.bvh).first()
    (str(n), number(fast.render_s), number(slow.render_s))
  }).flatten(),
  [中位数], number(bench.median_bvh_s), number(bench.median_list_s),
)

以逐物体求交的渲染时间中位数除以 BVH 的中位数，本次对比的加速比为 *#number(bench.median_list_s / bench.median_bvh_s) 倍*。在这组场景和参数下，六次渲染所得 PNG 文件的 SHA-256 校验值一致。

另在包含运动球和经旋转、平移的盒子的场景中，对 12,000 条射线比较两种求交方式。两种方式的命中状态及命中表面的正反面标记一致，求交参数之差的绝对值和法线向量之差的模均小于 $10^(-9)$。

== 理解与收获

路径追踪将可见性和材质散射纳入同一计算过程，沿路径累积光照，得到反射、折射与软阴影。BVH 降低了追踪每条路径的几何求交开销。增加每像素样本数可以降低蒙特卡洛估计的方差。

本实现从材质的散射分布采样方向，路径命中面积光源时才累加其贡献。Cornell Box 渲染到每像素 2048 个样本时，暗部仍有可见噪声。后续可结合显式光源采样和多重重要性采样，检验其能否改善收敛。

#pagebreak()
= 提升任务

== 运动模糊

摄像机在快门区间内均匀采样时刻，运动球的球心在起止位置之间按时间线性插值。散射射线继承同一时刻。两图使用同一场景、摄像机位置和路径采样种子。固定在快门开启时刻得到左图。右图对整个快门区间采样，部分小球的轮廓沿运动方向拉长。

#figure(pair("motion-still", "motion", [快门时刻固定为 0], [快门区间为 0 至 1]), caption: [运动模糊对比。每像素 128 个样本。])

== 实体纹理、Perlin 噪声与图像纹理

三维棋盘纹理按空间位置在两种颜色之间切换。Perlin 噪声由梯度向量、三组置换表和 Hermite 平滑插值得到。将七个频率层的噪声加权合成为湍流，用它扰动正弦波的相位以生成大理石条纹。图像纹理以球面经纬方向计算 UV 坐标，再从 RGB 图像中取色。

#figure(
  grid(columns: (1fr, 1fr, 1fr), gutter: 8pt,
    image("images/textures.png"), image("images/noise.png"), image("images/image.png"),
    align(center, [三维棋盘]), align(center, [Perlin 大理石]), align(center, [图像 UV 映射])),
  caption: [三种纹理的渲染结果。每像素 128 个样本。],
)

图像纹理场景和综合场景都从 `assets/uv-map.ppm` 读取自制测试图。蓝、橙色块和格线用于辨认 UV 方向并检查接缝。该图替代教程示例的地球地图，随源码提供，便于离线复现。

== 矩形、光源与实例变换

矩形由一个角点和两条边向量定义。求得射线与平面的交点后，检查面内坐标是否落在矩形范围内。盒子由六个矩形组成。路径命中发光几何体时，累加其材质的自发光贡献。经过旋转和平移的实例在局部坐标系中求交，再将交点与法线变换回世界坐标系。

#pagebreak()
#figure(
  grid(columns: (1fr, 1fr), gutter: 12pt,
    image("images/light.png", width: 100%), image("images/cornell.png", width: 100%),
    align(center, [发光球与矩形]), align(center, [Cornell Box 与变换实例])),
  caption: [左图每像素 1024 个样本，右图每像素 2048 个样本。],
)

左图关闭天空背景，由发光球和矩形照亮地面与物体。右图顶部的面积光源照亮 Cornell Box 内的两个白色盒子，两侧红墙和绿墙的反射光使盒子局部染色。

== 参与介质

常密度介质先确定射线在介质内的有效区间，将区间长度乘以方向向量的模，换算为可行进的空间距离。若按指数分布抽得的自由程在该距离以内，便在射线相应位置生成散射点，并按各向同性分布采样新方向。两团烟雾的密度均为 0.01，边界为经过旋转和平移的盒子。

#figure(image("images/smoke.png", width: 52%), caption: [Cornell Box 中的黑、白烟雾。480 × 480，每像素 1024 个样本。])

测试使用密度为 0.5 的单位球，对沿直径穿过球体的同一条射线抽样 20,000 次。散射事件的样本比例与理论概率 $1-exp(-1)$ 相差小于 0.015，所有采得的散射点均位于射线在球内的区间。

#pagebreak()
== 第二册综合场景

场景包含 400 个高度随机的地面盒子、发光矩形、运动球、玻璃球和粗糙金属球、彩色介质与全局稀薄介质、带图像纹理和 Perlin 纹理的球体，以及由 1,000 个白球组成的球簇。地面和球簇各有内部 BVH，球簇经旋转和平移后与其余物体一同纳入顶层 BVH。

#figure(image("images/final.png", width: 70%), caption: [600 × 600，每像素 2048 个样本，最大路径深度 40。])

摄像机位于 (478, 278, -600)，朝向 (278, 278, 0)，垂直视场角为 40°。照明、地面盒子和球簇的布置参照随附第二册第 10 章。布局种子为 519，路径采样种子为 2026。图像纹理使用自制网格，金属球的粗糙度系数为 0.35。

= 验证与复现

Release 构建运行了全部 13 类场景。小尺寸渲染通过了 AddressSanitizer 和 UndefinedBehaviorSanitizer 检查。命令行在收到无效参数时以非零状态退出，窗口循环通过了 SDL 虚拟视频驱动下的启动检查。每个像素的随机数流由像素编号和路径采样种子决定。在固定种子的检查中，使用 1 个和 8 个线程得到相同的 PNG 文件。

报告图像统一使用 8 个线程、路径采样种子 2026 和最大路径深度 40。解压提交包并编译后，用 `--scene` 选择场景，按下一节记录设置 `--width`、`--height` 和 `--samples`，再用 `--depth`、`--threads` 和 `--seed` 设置公共参数。

#pagebreak()
= 渲染记录与参考资料

下表记录 15 幅报告图像的分辨率、每像素样本数及实测渲染时间。

#table(
  columns: (1.4fr, auto, auto, auto),
  align: (left, right, right, right), inset: 5pt,
  [*场景*], [*分辨率*], [*样本/像素*], [*渲染时间（s）*],
  ..results.renders.map(r => (
    r.figure, str(r.width) + " × " + str(r.height),
    str(r.samples), number(r.render_s),
  )).flatten(),
)

复现 `pinhole` 使用 `--scene defocus --aperture 0`，复现 `motion-still` 使用 `--scene motion --shutter 0`。其余图像以表中的场景名称作为 `--scene` 的参数值。

算法参考 Peter Shirley 随作业提供的两册教程，按课程框架的接口组织实现。课程窗口框架采用 Wencong Yang 的 MIT 许可证。

- Peter Shirley，_Ray Tracing in One Weekend_，2020-12-07 版，第 4–13 章。
- Peter Shirley，_Ray Tracing: The Next Week_，3.2.3 版（2020-12-07），第 2–10 章。
- 中山大学计算机图形学课程，_Assignment 3: Ray Tracing_ 与配套代码框架。

#pagebreak()
= 提交包结构与运行

提交包以解压目录为根，报告、源码、头文件和运行时纹理按如下结构存放。

```text
.
├── REPORT.pdf
├── LICENSE.txt
├── src/
├── include/
│   ├── RayTracer/
│   ├── stb_image.h
│   └── stb_image_write.h
└── assets/
    └── uv-map.ppm
```

`src/` 包含程序入口、窗口、渲染器、场景、纹理和 PNG 输出的六个 C++ 实现文件。`include/RayTracer/` 存放配套头文件，两个 stb 头文件提供图像解码和 PNG 输出。`assets/uv-map.ppm` 是运行时读取的测试纹理，`LICENSE.txt` 载有课程框架的许可证。

编译需要支持 C++17 的编译器和 SDL2 开发库。以下命令适用于 macOS 和 Linux，通过 `sdl2-config` 获取 SDL2 的编译与链接参数。请在包含 `src/`、`include/` 和 `assets/` 的解压目录执行。

```sh
c++ -O3 -std=c++17 -Iinclude/RayTracer -Iinclude \
  $(sdl2-config --cflags) src/*.cpp \
  $(sdl2-config --libs) -pthread -o CGAssignment3
./CGAssignment3 --scene image --width 640 --samples 128 \
  --depth 40 --threads 8 --seed 2026 --output image.png
```

程序运行时从当前工作目录下的 `assets/` 读取纹理。此例将图像纹理场景写入 `image.png`。直接运行 `./CGAssignment3` 可在窗口中查看第一册最终球体场景，按 Esc 或关闭窗口退出。

复现 BVH 对比时，每轮各执行下列两条命令一次，共三轮，轮换先后顺序。每轮记录终端中的渲染时间和两个 PNG 文件的 SHA-256 校验值。三轮后比较两组渲染时间的中位数，并核对六个校验值。

```sh
./CGAssignment3 --scene random --width 320 --height 180 \
  --samples 16 --depth 40 --threads 8 --seed 2026 \
  --output bvh.png
./CGAssignment3 --scene random --width 320 --height 180 \
  --samples 16 --depth 40 --threads 8 --seed 2026 \
  --no-bvh --output list.png
```
