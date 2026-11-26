# 实验 3 光线追踪

普通作业 Assignment 3，来源为中山大学计算机图形学课程的 `Assignment3.pdf`。基础和提升任务均已实现。各阶段效果、实际渲染参数和 BVH 计时保存在 report/ 目录。

## 一、资料与框架

| 文件 | 内容 |
| --- | --- |
| `Assignment3.pdf` | 课程作业原文 |
| `readme.pdf` | 课程原始环境安装说明 |
| `reference/Ray Tracing in One Weekend.pdf` | Peter Shirley，2020-12-07 版教程，主要对应基础任务 |
| `reference/Ray Tracing_ The Next Week.pdf` | Peter Shirley，3.2.3 版，2020-12-07，对应 BVH 和提升任务 |
| `reference/CGAssignment3.jpg` | 课程提供的参考图片 |

教程章节号以随作业提供的 PDF 为准。PDF 沿用仓库规则在本地保存并由 Git 忽略。

课程起始框架使用 SDL2 显示画布，用 C++ 标准线程执行渲染，提供 `vec3` 数学类及颜色写入函数，对应第一本教程的 Chapter 2 和 Chapter 3。原代码与构建文件中误写的 `CGAssignment4` 已统一为 `CGAssignment3`。

| 文件 | 职责 |
| --- | --- |
| `src/main.cpp` | 命令行参数、窗口入口、后台渲染与逐行显示 |
| `src/WindowsApp.cpp`、`include/RayTracer/WindowsApp.h` | 课程封装的窗口、事件和画布显示 |
| `include/RayTracer/vec3.h` | 三维向量、点和颜色运算 |

画布为 `std::vector<std::vector<color>>`，每个颜色通道使用 [0, 1] 浮点值。分辨率通过命令行参数 `--width` 和 `--height` 调整。光追逻辑位于 Renderer、Geometry 和 Material 模块。

## 二、基础任务（80%）

按作业原文顺序完成。各渲染任务需贴效果图，并说明遇到的问题和解决方法。

- [x] 第一册 Chapter 4。实现射线类和简单摄像机，渲染渐变的蓝色天空背景。
- [x] 第一册 Chapter 5–7。加入球体、法线与多物体处理、抗锯齿，渲染简单球形物体。
- [x] 第一册 Chapter 8–10。实现漫反射、金属和电介质材质，展示各材质效果。原题的“电解质”对应教程的 Dielectrics。
- [x] 第一册 Chapter 11–12。实现可调整的摄像机和聚焦模糊，展示效果。
- [x] 第一册 Chapter 13。构建并渲染最终场景，建议使用 Release 模式。
- [x] 第二册 Chapter 3。实现 BVH（包围体层次结构）加速，与未使用 BVH 的渲染时间对比，并说明问题及解决过程。
- [x] 撰写对光线追踪的理解、困惑、感想和收获。

原题提到给出最终场景参数，但随附 PDF 在该段后未列出参数。本实现参照随附第一册 Chapter 13.1，实际使用的场景与采样配置记录在报告中。

## 三、提升任务（20%）

- [x] 完成第二册的其余实现章节。包括运动模糊、实体纹理、Perlin 噪声、图像纹理、矩形与光源、实例变换、参与介质及综合场景。报告记录实际完成的章节和效果。

## 四、构建与运行

依赖 CMake 3.16+、C++17 编译器、SDL2 开发库。构建配置使用系统 SDL2 和标准线程库。macOS 可使用已安装的 Homebrew SDL2，Linux 和 Windows 也需提供 SDL2 的 CMake 配置包。若 CMake 找不到 SDL2，使用 `CMAKE_PREFIX_PATH` 指定其安装前缀。

从仓库根目录执行。

```sh
cmake -S 0003 -B 0003/build -DCMAKE_BUILD_TYPE=Release
cmake --build 0003/build --parallel
./0003/build/CGAssignment3
```

例如 Apple Silicon 上的 Homebrew 前缀可在配置时指定为 `-DCMAKE_PREFIX_PATH=/opt/homebrew`。多配置生成器构建时加 `--config Release`，并从相应的 `Release/` 子目录运行程序。

默认窗口显示第一册最终球体场景。每行渲染完成后更新显示，Esc 或关闭窗口结束。指定 `--output` 时直接导出 PNG。

```sh
./0003/build/CGAssignment3 --help
./0003/build/CGAssignment3 --scene materials --output materials.png
./0003/build/CGAssignment3 --scene defocus --aperture 0.4 --output defocus.png
./0003/build/CGAssignment3 --scene random --no-bvh --output random-list.png
./0003/build/CGAssignment3 --scene motion --shutter 0 --output motion-still.png
./0003/build/CGAssignment3 --scene cornell --width 480 --samples 1024 --output cornell.png
python3 0003/scripts/render_report.py
```

支持场景有 `sky`、`sphere`、`materials`、`defocus`、`random`、`motion`、`textures`、`noise`、`image`、`light`、`cornell`、`smoke`、`final`。使用 `--width`、`--height`、`--samples`、`--depth`、`--seed`、`--threads` 控制参数。尺寸为 2–4096，路径深度为 1–256，线程数 0 表示自动选择。默认采样数为 64、种子为 2026，输出使用平方根 Gamma 校正。`--seed` 控制路径采样，第一册随机场景布局固定使用种子 2026，第二册综合场景布局使用 519。`--no-bvh` 关闭顶层加速；综合场景的地面和球簇保留各自的内部 BVH。

程序打印一行 JSON，分别记录场景/BVH 构建时间和渲染时间，PNG 编码与磁盘写入在渲染计时外。报告脚本交替运行三轮 BVH 与逐物体求交，以同一随机种子比较图像校验值，取时间中位数。

## 五、代码与报告

| 模块 | 内容 |
| --- | --- |
| `include/RayTracer/Ray.h` | 射线与每像素随机数流 |
| `include/RayTracer/Geometry.h` | 球体、运动球、矩形、盒子、变换、介质、BVH |
| `include/RayTracer/Material.h` | 漫反射、金属、电介质、发光、各向同性散射 |
| `include/RayTracer/Texture.h`、`src/Texture.cpp` | 棋盘、Perlin、图像纹理 |
| `src/Scenes.cpp` | 13 类实验场景 |
| `src/Renderer.cpp` | 薄透镜、路径积分、多线程渲染 |
| `src/Image.cpp` | 图像编解码，依赖仓库已有的 `../0000/deps/` |
| `src/main.cpp`、`src/WindowsApp.cpp` | 命令行与课程 SDL 窗口 |
| `assets/uv-map.ppm` | 自制 UV 测试图，替代教程示例地球地图 |
| `report/REPORT.typ` | 共用模板的实验报告 |
| `report/results.json` | 图片与三轮 BVH 对比的参数、时间、SHA-256 |
| `scripts/render_report.py` | 生成报告图片和计时数据 |

报告共用 `Report-Templates/info.typ` 的封面信息。从仓库根目录编译。

```sh
typst compile --root . 0003/report/REPORT.typ 0003/report/REPORT.pdf
```

## 六、提交要求

截止时间和邮箱以课程群公告为准。PDF 报告与源代码压缩为 `学号+姓名+HW3.zip`。

报告提交编译后的 PDF。代码提交源文件，按课程要求排除教程、作业原文、报告源文件、图片资源、工程文件、构建目录和二进制产物。最终报告应说明实际完成情况，BVH 对比须记录实际测得的时间。
