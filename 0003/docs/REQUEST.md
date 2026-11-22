# 实验 3 光线追踪

普通作业 Assignment 3，来源为中山大学计算机图形学课程的 `Assignment3.pdf`。当前已整理课程起始框架，运行结果为渐变色画布，基础和提升任务均待完成。

## 一、资料与框架

| 文件 | 内容 |
| --- | --- |
| `Assignment3.pdf` | 课程作业原文 |
| `readme.pdf` | 课程原始环境安装说明 |
| `reference/Ray Tracing in One Weekend.pdf` | Peter Shirley，2020-12-07 版教程，主要对应基础任务 |
| `reference/Ray Tracing_ The Next Week.pdf` | Peter Shirley，3.2.3 版，2020-12-07，对应 BVH 和提升任务 |
| `reference/CGAssignment3.jpg` | 课程提供的参考图片 |

教程章节号以随作业提供的 PDF 为准。PDF 沿用仓库规则在本地保存并由 Git 忽略。

框架使用 SDL2 显示画布，用 C++ 标准线程执行渲染，已有 `vec3` 数学类及颜色写入函数，对应第一本教程的 Chapter 2 和 Chapter 3。原代码与构建文件中误写的 `CGAssignment4` 已统一为 `CGAssignment3`。

| 文件 | 职责 |
| --- | --- |
| `src/main.cpp` | 窗口与渲染线程入口、画布、`write_color`、待扩展的 `rendering` |
| `src/WindowsApp.cpp`、`include/RayTracer/WindowsApp.h` | 课程封装的窗口、事件和画布显示 |
| `include/RayTracer/vec3.h` | 三维向量、点和颜色运算 |

画布为 `std::vector<std::vector<color>>`，每个颜色通道使用 [0, 1] 浮点值。尺寸统一通过 `gWidth` 和 `gHeight` 调整。渲染逻辑从 `rendering()` 的 TODO 处继续。

## 二、基础任务（80%）

按作业原文顺序完成。各渲染任务需贴效果图，并说明遇到的问题和解决方法。

- [ ] 第一册 Chapter 4。实现射线类和简单摄像机，渲染渐变的蓝色天空背景。
- [ ] 第一册 Chapter 5–7。加入球体、法线与多物体处理、抗锯齿，渲染简单球形物体。
- [ ] 第一册 Chapter 8–10。实现漫反射、金属和电介质材质，展示各材质效果。原题的“电解质”对应教程的 Dielectrics。
- [ ] 第一册 Chapter 11–12。实现可调整的摄像机和聚焦模糊，展示效果。
- [ ] 第一册 Chapter 13。构建并渲染最终场景，建议使用 Release 模式。
- [ ] 第二册 Chapter 3。实现 BVH（包围体层次结构）加速，与未使用 BVH 的渲染时间对比，并说明问题及解决过程。
- [ ] 撰写对光线追踪的理解、困惑、感想和收获。

原题提到给出最终场景参数，但随附 PDF 在该段后未列出参数。后续实现可先参照随附第一册 Chapter 13.1，并记录实际使用的场景与采样配置。

## 三、提升任务（20%）

- [ ] 完成第二册的其余实现章节。包括运动模糊、实体纹理、Perlin 噪声、图像纹理、矩形与光源、实例变换、参与介质及综合场景。报告记录实际完成的章节和效果。

## 四、构建与运行

依赖 CMake 3.16+、C++11 编译器、SDL2 开发库。构建配置使用系统 SDL2 和标准线程库。macOS 可使用已安装的 Homebrew SDL2，Linux 和 Windows 也需提供 SDL2 的 CMake 配置包。若 CMake 找不到 SDL2，使用 `CMAKE_PREFIX_PATH` 指定其安装前缀。

从仓库根目录执行。

```sh
cmake -S 0003 -B 0003/build -DCMAKE_BUILD_TYPE=Release
cmake --build 0003/build --parallel
./0003/build/CGAssignment3
```

例如 Apple Silicon 上的 Homebrew 前缀可在配置时指定为 `-DCMAKE_PREFIX_PATH=/opt/homebrew`。多配置生成器构建时加 `--config Release`，并从相应的 `Release/` 子目录运行程序。

框架启动时显示 800 × 450 的渐变色画布，按 Esc 或关闭窗口退出。该渐变来自课程起始代码。窗口与渲染线程的运行验证不代表完成任何光追任务。

## 五、目录与报告

```text
0003/
├── CMakeLists.txt
├── LICENSE.txt
├── docs/
│   ├── REQUEST.md
│   ├── Assignment3.pdf
│   ├── readme.pdf
│   └── reference/
├── include/RayTracer/
│   ├── WindowsApp.h
│   └── vec3.h
├── src/
│   ├── main.cpp
│   └── WindowsApp.cpp
└── report/
    └── REPORT.typ
```

报告源文件为待填写骨架，沿用 `Report-Templates/` 的模板和本地 `info.typ`。后续实现时再填写方法、截图、计时与总结。需生成报告时，从仓库根目录执行。

```sh
typst compile --root . 0003/report/REPORT.typ 0003/report/REPORT.pdf
```

## 六、提交要求

截止时间和邮箱以课程群公告为准。PDF 报告与源代码压缩为 `学号+姓名+HW3.zip`。

报告提交编译后的 PDF。代码提交源文件，按课程要求排除教程、作业原文、报告源文件、图片资源、工程文件、构建目录和二进制产物。最终报告应说明实际完成情况，BVH 对比须记录实际测得的时间。
