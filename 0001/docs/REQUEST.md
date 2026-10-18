# Assignment 1：3D Transformation —— 作业需求

> 来源：`Assignment1.pdf`（Computer Graphics Teaching Stuff, Sun Yat-Sen University）
> 本次作业：在课程提供的 **CPU 软光栅化渲染器**框架上，实现渲染管线的三维变换部分（几何顶点的 Vertex Processing 阶段）。

## 一、基本信息

| 项目 | 内容 |
| --- | --- |
| 截止日期 | 具体截止日期见群公告 |
| 提交方式 | 将报告（**PDF 格式**）发送至邮箱（邮箱地址见群公告） |
| 打包命名 | 文档 + 源代码一起压缩，命名 **学号+姓名+HW1**（如 `19214044+张三+HW1.zip`） |
| 分数构成 | 基础 60% + 提升 25% + 挑战 15%，鼓励尽量全做，杜绝抄袭 |

提交注意：文档只交编译生成的 **pdf**（勿交 markdown/docx/图片源文件）；代码只交**源代码**（**删掉 build 目录下所有文件**，勿交教程/作业描述/工程文件/中间文件/二进制文件）。

## 二、代码框架

框架是一个仿 OpenGL 的软光栅化渲染器（无硬件加速），部署与构建见 `readme.pdf`。测试环境为 Windows 10 + VS2017/2019 与 Ubuntu 16.04。第三方依赖（框架已封装好，无需深入学习）：

- **GLM**：线性代数库（Assignment 0 已用过），需要掌握其使用
- **SDL2**：窗口库，负责创建窗口、显示渲染结果、鼠标交互
- **TinyObjLoader**：obj 模型加载

`src/` 各模块：

| 文件 | 职责 | 是否修改 |
| --- | --- | --- |
| `main.cpp` | 程序入口，渲染循环 | **需要修改** |
| `TRRenderer(.h/.cpp)` | 渲染状态/数据管理、调用绘制 | **需要修改** |
| `TRShaderPipeline(.h/.cpp)` | 顶点着色器、光栅化、片元着色器 | 无需修改 |
| `TRFrameBuffer(.h/.cpp)` | 帧缓冲（颜色 + 深度） | 无需修改 |
| `TRDrawableMesh(.h/.cpp)` | 加载 obj 网格、存储顶点数据 | 无需修改 |
| `TRWindowsApp(.h/.cpp)` | 窗口、计时、鼠标事件 | 无需修改 |

顶点着色器已实现（`Local -> World -> Camera -> Clip`），需要完成的是 **model / view / project / viewport** 四个矩阵的计算。**硬性要求**：通过代码计算矩阵每个元素的取值并逐个赋值；`glm::mat4` 是**列主序**的，`mat[x][y]` 表示第 x 列第 y 行。

## 三、任务

### 基础任务（60%）

在 `TRRenderer.cpp` 中实现三个函数（**逐元素赋值**）：

1. `calcViewMatrix(camera, target, worldUp)` —— 观察矩阵（world → camera），**禁止使用 `glm::lookAt`**（可用 `glm::normalize/cross/dot`）
2. `calcPerspProjectMatrix(fovy, aspect, near, far)` —— 透视投影矩阵（camera → clip），**禁止使用 `glm::perspective`**（可用 `std::tan` 等数学函数）
3. `calcViewPortMatrix(width, height)` —— 视口变换矩阵（NDC → 屏幕空间），在光栅化之前使用

实现正确时运行效果：模型（diablo3_pose）绕 y 轴持续旋转；按住鼠标左键横向拖动旋转摄像机，滚轮拉近/拉远。**需简述三种矩阵的实现思路，并贴出运行效果。**

### 提升任务（25%）

用 `glm::scale` 实现物体**放大→缩小→放大**的循环动画：放大至 3.0 倍后缩回原大小，依次循环，速度适中（需注释掉原有的旋转代码）。贴出效果并说明实现方法。

### 挑战任务（15%）

在 `TRRenderer.cpp` 中实现 `calcOrthoProjectMatrix(left, right, bottom, top, near, far)` —— 正交投影矩阵（**禁止使用 `glm::ortho`**）；在 `main.cpp` 中分别切换调用透视/正交投影，滚动滚轮拉近拉远，体会两种投影的差别。简述做法。

### 选做思考题

1. 简述正交投影和透视投影的区别。
2. 物体局部空间的顶点到最终屏幕空间的顶点，经历哪几个空间坐标系？裁剪空间下顶点的 w 值是哪个空间坐标下的 z 值？有什么空间意义？
3. 投影变换后顶点坐标是直接变换到 NDC（xyz 全在 [-1,1]）吗？透视除法（Perspective Division）是什么？为什么需要它？

## 四、注意事项

- 禁止直接调用 `glm::lookAt`、`glm::perspective`、`glm::ortho` 等取巧 API。
- 禁止作业文档抄袭；鼓励讨论，但须独立完成。
- 代码有疑问可在群里匿名讨论。

## 五、本目录说明

```
0001/
├── CMakeLists.txt      # 课程框架原始构建脚本（Windows 走 libs/，Linux 找系统 SDL2）
├── docs/               # 本需求文档 + 作业原文 + 框架部署说明
├── model/              # 渲染用的 obj 模型（代码以相对路径 model/ 加载，需从 0001/ 目录运行）
├── include/ libs/      # 框架自带第三方库（SDL2/Windows 头文件与 .lib、GLM、tiny_obj_loader）
├── src/                # 渲染器源码（待修改：TRRenderer.cpp、main.cpp）
└── build/              # Windows 运行库 SDL2.dll（git 忽略；按课程要求提交时需清空）
```
