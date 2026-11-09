# 实验 2 光栅化与 Z-buffer

题目来自中山大学计算机图形学课程的 `Assignment2.pdf`。基础任务占 75%，提升任务占 25%。原题和两份安装说明保存在本目录的 PDF 中。

## 基础任务

1. 在 `src/TRShaderPipeline.cpp` 实现 Bresenham 线段光栅化。对三角形的每条边调用，用 `VertexData::lerp` 插值，并丢弃窗口外的点。
2. 在同一文件中实现基于边函数的三角形填充。扫描包围盒，判断像素覆盖，用 `VertexData::barycentricLerp` 插值，并处理共享边、退化三角形和窗口边界。
3. 在 `src/TRRenderer.cpp` 使用每个片元的 `cpos.z` 做深度测试。NDC 深度从近到远递增。被遮挡的片元应跳过着色和写入。报告展示线框、填充及深度测试的效果并说明做法。

## 提升任务

1. 在 `improve/rasterizer.cpp` 实现三角形填充。用像素中心判断覆盖，以重心坐标插值深度，使两个重叠三角形呈现正确遮挡。
2. 对每个像素做 2×2 超采样。每个样本独立维护颜色和深度，最后平均四个样本的颜色。对比单采样与超采样的边缘效果。

## 本地构建与运行

从 `0002/` 目录执行。需安装 CMake、支持 C++17 的编译器和 SDL2 开发库（macOS 可通过 Homebrew 安装 SDL2）。工程复用 `../0001/include/` 的 GLM、TinyObjLoader 和 `../0000/deps/` 的 stb_image_write，提升程序的矩阵运算和 PNG 输出分别使用 GLM 和 stb_image_write。

```sh
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --parallel
./build/CGAssignment2
```

基础程序默认用填充和深度测试绘制旋转模型。左键拖动转动相机，滚轮缩放，Esc 退出。也可不创建窗口，导出同一初始视角的三个渲染结果。

```sh
./build/CGAssignment2 --wireframe --output build/wireframe.png
./build/CGAssignment2 --no-depth --output build/fill-no-depth.png
./build/CGAssignment2 --output build/fill-depth.png
./build/CGAssignment2-Improve build/triangle-1x.png
./build/CGAssignment2-Improve --ssaa build/triangle-4x.png
```

报告沿用仓库的 Typst 模板及本地封面信息。若缺少 `../Report-Templates/info.typ`，可由同目录的 `info.example.typ` 复制后填写。

```sh
typst compile --root .. report/REPORT.typ report/REPORT.pdf
```

## 提交格式

报告 PDF 与源代码压缩为 `学号+姓名+HW2.zip`。课程要求省略报告源文件、截图、工程文件及构建产物。邮箱和截止时间以课程群公告为准。
