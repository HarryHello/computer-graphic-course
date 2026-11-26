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

本实验在 Apple Silicon macOS 上使用 Apple Clang 21、CMake、C++17 和 SDL2。保留课程窗口与事件处理，复用原有 `vec3`，新增射线、几何体、材质、纹理、摄像机和路径积分模块。PNG 编解码复用仓库已有的 stb 库。

摄像机向像素内随机位置发射射线。命中物体后累加自发光，再按材质选择散射方向，并用反射率衰减后续贡献。未命中时返回天空色或背景。每个像素将多条路径平均，最后进行平方根 Gamma 校正。渲染线程按行分配工作，SDL 主线程读取加锁后的完整行。

= 基础任务

== 射线、摄像机与天空背景

射线保存起点、方向与时间，沿射线的位置为 $p(t) = o + t d$。摄像机根据视点、目标和竖直方向构建正交基。天空按单位射线方向的纵分量，在白色与蓝色之间插值。

== 球体、法线与抗锯齿

球面求交将射线方程代入球面方程，求判别式与两个根，选取合法区间内最近的根。求交记录保存朝向入射侧的法线、UV 和材质。法线可视化把分量映射到 [0, 1]。像素内随机偏移采样后取平均，减轻轮廓走样。

#figure(pair("sky", "sphere", [蓝色天空], [球体法线与地面]), caption: [前两个基础阶段])

#pagebreak()
== 漫反射、金属与电介质

漫反射使用“法线加随机单位向量”构造余弦分布的方向。金属使用反射方向加粗糙扰动。电介质按入射面选择折射率比，使用 Snell 定律和 Schlick 近似，并处理全反射。左侧空心玻璃球用内外两层球面表达空气与玻璃的边界。

#figure(image("images/materials.png", width: 72%), caption: [玻璃、漫反射和金属。640 × 360，256 samples/pixel。])

== 可调整摄像机与聚焦模糊

薄透镜在镜头圆盘内随机选择起点，让射线穿过同一个焦平面位置。光圈为零时所有射线共用镜头中心，增大光圈后离焦物体变模糊。下面固定视点、焦距、种子和采样数，只改变光圈直径。蓝色球附近的主体相对清楚，偏离焦平面的地面边界与反射细节变软。

#figure(pair("pinhole", "defocus", [光圈直径 0], [光圈直径 0.4]), caption: [景深对比，每幅 256 samples/pixel。])

#pagebreak()
== 第一册最终场景

棋盘地面、小球和三个主体球构成场景，顶层共 486 个物体。小球按固定种子生成，混合漫反射、金属与玻璃。摄像机位于 (13, 2, 3)，朝向原点，垂直视场角 20°，焦平面距离 10，光圈直径 0.1。构图采用随附第一册最终场景的形式，实际参数一并记录。

#figure(image("images/random.png", width: 100%), caption: [800 × 450，256 samples/pixel，最大路径深度 40。])

求交有效下界设为 0.001，减少散射射线重复命中出发面的自相交。折射前判断全反射，避免对非法透射方向开平方。累积在线性颜色空间进行，Gamma 校正在平均之后执行。

#pagebreak()
== BVH 构建与计时对比

各物体提供轴对齐包围盒，运动球的包围盒覆盖快门起止时刻。BVH 选择当前盒子的最长轴，按物体盒中心的中位数划分两组，再递归构建二叉树。遍历先做盒子测试，第一棵子树命中后缩小另一棵子树的搜索上界。

平行于某轴的射线单独判断起点是否在该轴区间内，避免零方向分量引起除零或 NaN。矩形包围盒添加微小厚度。叶节点只保存一个物体，避免体积介质在同一叶节点内被重复随机采样。

使用相同的 486 物体随机场景，320 × 180、16 samples/pixel、最大深度 40、8 个工作线程、种子 2026，只切换 `--no-bvh`。每种模式运行三次，交替先后顺序。渲染计时包含工作线程启动、路径积分和颜色转换。场景及 BVH 构建单独计时，PNG 编码和磁盘写入在计时区间外。

#table(
  columns: (auto, 1fr, 1fr),
  align: (left, right, right), inset: 8pt,
  [*轮次*], [*BVH 渲染 / s*], [*逐物体求交 / s*],
  ..range(1, 4).map(n => {
    let fast = results.benchmark.filter(r => r.repetition == n and r.bvh).first()
    let slow = results.benchmark.filter(r => r.repetition == n and not r.bvh).first()
    (str(n), number(fast.render_s), number(slow.render_s))
  }).flatten(),
  [中位数], number(bench.median_bvh_s), number(bench.median_list_s),
)

中位数加速比为 *#number(bench.median_list_s / bench.median_bvh_s) 倍*。六次输出的 PNG SHA-256 完全一致，说明本对比中加速未改变图像。三轮渲染时间见上表，各场景的出图参数与耗时见报告末尾的渲染记录。

固定随机数流按像素分配，线程调度不会改变采样序列。另对 12,000 条包含运动球和旋转平移盒子的射线，比较 BVH 与逐物体求交的命中状态、距离、法线及面朝向，检查全部通过。

== 理解与收获

光线追踪通过求交与材质散射连接可见性和颜色计算，反射、折射和软阴影由路径传播产生，有限采样则引入随机噪声。BVH 减少路径的几何求交量，提高采样数降低方差，两者解决的问题不同。

本实验采用教程的随机路径散射，Cornell Box 等小面积光源场景需要较高采样数，暗部仍有有限样本噪声。后续可用显式光源采样和多重重要性采样改善收敛。

#pagebreak()
= 提升任务

== 运动模糊

射线携带 [0, 1] 快门区间内的时刻，运动球中心按时间线性插值，散射后保留同一时间。下面两幅图使用同一套运动球、相机和种子。固定时间观察快门开启瞬间，完整快门区间对不同位置积分，使部分小球沿运动方向拉长。

#figure(pair("motion-still", "motion", [快门时间固定为 0], [快门区间为 0 到 1]), caption: [运动模糊，每幅 128 samples/pixel。])

== 实体纹理、Perlin 噪声与图像纹理

棋盘按三维位置选择两种颜色。Perlin 噪声用梯度向量、三组置换表和 Hermite 平滑插值，再叠加七层湍流形成大理石条纹。图像纹理通过球面的经纬 UV 映射读取磁盘上的 RGB 图像。

#figure(
  grid(columns: (1fr, 1fr, 1fr), gutter: 8pt,
    image("images/textures.png"), image("images/noise.png"), image("images/image.png"),
    align(center, [三维棋盘]), align(center, [Perlin 大理石]), align(center, [图像 UV 映射])),
  caption: [三种纹理，均为 128 samples/pixel。],
)

`assets/uv-map.ppm` 是随工程提供的合成测试图，使用蓝、橙区块和格线检查 UV 方向及接缝。图像纹理与综合场景均实际读取该文件。该图替代教程示例的地球地图，便于离线复现。

== 矩形、光源与实例变换

矩形由角点和两条边定义，求平面交点后检查面内坐标。盒子复用六个矩形。自发光材质返回辐射颜色，路径命中光源时累加贡献。旋转和平移实例先把射线变换到局部空间，再把交点与法线变回世界空间。

#pagebreak()
#figure(
  grid(columns: (1fr, 1fr), gutter: 12pt,
    image("images/light.png", width: 100%), image("images/cornell.png", width: 100%),
    align(center, [发光球与矩形]), align(center, [Cornell Box 与变换实例])),
  caption: [左图 1024 samples/pixel，右图 2048 samples/pixel。],
)

左图关闭天空，仅由发光几何体照明。右图用红、绿、白墙组成 Cornell Box，上方面积光源照亮两个经过旋转和平移的白色盒子，可观察间接光和墙面颜色传播。

== 参与介质

常密度介质先求射线的进入和离开区间，再按指数分布抽取自由程。距离位于边界内时生成体积散射，用各向同性材质选择后续方向。自由程使用空间距离，需要将射线参数区间长度乘以方向长度。下图黑、白烟雾使用变换后的盒子作为边界，密度为 0.01。

#figure(image("images/smoke.png", width: 52%), caption: [480 × 480，1024 samples/pixel。])

对单位球、密度 0.5、穿过直径的射线做 20,000 次抽样，命中概率与理论值 $1-exp(-1)$ 的差小于 0.015。检查同时确认采样点落在合法的进出区间内。

#pagebreak()
== 第二册综合场景

场景包含 400 个随机高度的地面盒子、发光矩形、运动球、玻璃与粗糙金属、彩色介质、全局稀薄介质、图像纹理、Perlin 纹理，以及由 1000 个白球组成的旋转平移实例。地面和白球簇分别构建内部 BVH，顶层统一加速。

#figure(image("images/final.png", width: 70%), caption: [600 × 600，2048 samples/pixel，最大路径深度 40。])

摄像机位于 (478, 278, -600)，朝向 (278, 278, 0)，垂直视场角 40°。灯光、盒子分布和球簇位置采用随附第二册末章的结构，布局种子为 519，路径采样种子为 2026。图像纹理为自制网格，金属球粗糙扰动系数设为 0.35。

= 验证与复现

Release 构建已运行全部 13 类场景。使用 AddressSanitizer 和 UndefinedBehaviorSanitizer 做小尺寸检查，未发现内存访问或未定义行为错误。另验证了无效参数返回失败，1 与 8 个线程的固定种子输出一致，以及 SDL 虚拟视频驱动下的窗口循环启动。渐进显示按完整行发布，并用互斥锁保护共享画布。

报告图片统一使用 8 个线程、路径采样种子 2026 和最大深度 40。解压提交包并编译后，可通过 `--scene`、`--width`、`--height`、`--samples`、`--depth`、`--threads` 和 `--seed` 复现相应配置。各场景的分辨率、采样数与耗时见下一节。

#pagebreak()
= 渲染记录与参考资料

下表为本次报告出图的实测渲染时间。场景、分辨率和采样数不同，时间用于复现记录。

#table(
  columns: (1.4fr, auto, auto, auto),
  align: (left, right, right, right), inset: 5pt,
  [*场景*], [*分辨率*], [*样本/像素*], [*渲染/s*],
  ..results.renders.map(r => (
    r.figure, str(r.width) + " × " + str(r.height),
    str(r.samples), number(r.render_s),
  )).flatten(),
)

记录中的 `pinhole` 对应 `--scene defocus --aperture 0`，`motion-still` 对应 `--scene motion --shutter 0`。其余场景使用同名的 `--scene` 参数。

算法依据为 Peter Shirley 随作业提供的两册教程。第一册为 2020-12-07 版，第二册为 3.2.3 版（2020-12-07）。实现按本工程接口重新组织。课程窗口框架保留 Wencong Yang 的 MIT 许可证，PNG 与图像解码使用仓库已有的 stb 库。

- Peter Shirley, _Ray Tracing in One Weekend_，Chapter 4–13。
- Peter Shirley, _Ray Tracing: The Next Week_，Chapter 2–10。
- 中山大学计算机图形学课程，_Assignment 3: Ray Tracing_ 与配套代码框架。

#pagebreak()
= 提交包结构与运行

解压提交的压缩包后，文件按如下目录组织。以下路径均相对于解压目录。

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

`src/` 包含程序入口、窗口、渲染器、场景、纹理与图像输出的六个 C++ 实现文件。`include/RayTracer/` 包含配套头文件，两个 stb 头文件提供图像解码与 PNG 输出。`assets/uv-map.ppm` 是图像纹理与综合场景运行时读取的测试图，`LICENSE.txt` 保留课程框架的许可证。

编译需要支持 C++17 的编译器及 SDL2 开发库。以下命令使用 macOS/Linux 终端中的 `sdl2-config` 获取编译与链接参数。在同时包含 `src/`、`include/` 和 `assets/` 的解压目录执行。

```sh
c++ -O3 -std=c++17 -Iinclude/RayTracer -Iinclude \
  $(sdl2-config --cflags) src/*.cpp \
  $(sdl2-config --libs) -pthread -o CGAssignment3
./CGAssignment3 --scene image --width 640 --samples 128 \
  --depth 40 --threads 8 --seed 2026 --output image.png
```

程序从当前工作目录读取 `assets/` 中的纹理。上述命令生成图像纹理场景的 `image.png`。直接运行 `./CGAssignment3` 会打开窗口，显示第一册最终球体场景，按 Esc 或关闭窗口结束。

复现 BVH 对比时，使用相同场景、采样参数和种子，仅切换 `--no-bvh`。两条命令分别输出渲染时间与 PNG，可交替重复三轮并比较结果。

```sh
./CGAssignment3 --scene random --width 320 --height 180 \
  --samples 16 --depth 40 --threads 8 --seed 2026 \
  --output bvh.png
./CGAssignment3 --scene random --width 320 --height 180 \
  --samples 16 --depth 40 --threads 8 --seed 2026 \
  --no-bvh --output list.png
```
