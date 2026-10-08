// 计算机图形学 Assignment 0（HW0）实验报告
// 章节标题沿用 LearnOpenGL 教程原文名；作业任务的实验作为对应章节内的小节（标注"作业任务 N"）
// 数据来源与截图由 0000/scripts/capture.sh 生成；编译：typst compile --root ../.. REPORT.typ
// 个人信息（姓名/学号等）在 Report-Templates/info.typ 中集中维护，所有报告共用
#import "../../Report-Templates/templates/report.typ": *
#import "../../Report-Templates/info.typ": info

// 本报告专属信息（个人信息见 info.typ）
#let reportInfo = (
  title: "Assignment 0：OpenGL 入门",
  subtitle: "计算机图形学作业汇报（HW0）",
  time: "2026 年 10 月",
  banner: "../images/sysu.png",
)

#show: report.with(..reportInfo, ..info)

// 标题与其后内容保持同页，避免标题孤立页尾
#show heading: it => block(sticky: true, it)

#title(content: reportInfo.title)

= 开发环境与工程结构

#booktab(
  width: 100%,
  aligns: (left, left),
  columns: (1fr, 2.2fr),
  caption: [开发环境],
  [*项目*], [*配置*],
  [系统], [macOS（Apple Silicon）],
  [OpenGL 实际版本], [4.1（Apple 驱动由 Metal 转译，最高支持 4.1 Core Profile）],
  [窗口与上下文], [GLFW 3.4],
  [函数加载器], [GLAD（gl 3.3 core）],
  [数学库], [GLM 1.0.1],
  [图像加载], [stb\_image.h],
  [构建], [CMake + Apple Clang（依赖经 FetchContent 自动拉取）],
)

工程按教程章节划分，每个章节对应一个可执行程序（`src/<章节>/main.cpp`），公共代码在 `src/common/`；`scripts/capture.sh` 可一键重跑全部实验并生成本报告所有截图。

所有程序窗口标题均为"学号 - 章节名"（`glfwCreateWindow` 的第三个参数即标题字符串），学号由 CMake 编译期注入：

```cmake
# 构建时以 -DSTUDENT_ID=学号 传入
add_compile_definitions(STUDENT_ID="${STUDENT_ID}")
```

= OpenGL

OpenGL（Open Graphics Library）是 Khronos Group 维护的跨语言跨平台渲染 API，用来绘制二维和三维图形。它本身只是一份规范，规定接口的行为；真正的实现来自显卡厂商的驱动（GPU 加速），也有 Mesa 这样的纯软件实现。规范只管渲染，窗口和上下文要靠 GLFW 这类库配合窗口系统创建，本实验用的就是 GLFW。

计算机图形学研究图形的表示与渲染原理，包括几何变换、光栅化、着色、纹理映射、投影、消隐等。OpenGL 是其中实时光栅化管线的工业级接口，本次作业各步骤用到的图形学概念与 OpenGL 的对应关系如下：

#booktab(
  width: 100%,
  aligns: (left, left),
  columns: (1.4fr, 1.8fr),
  caption: [计算机图形学概念与 OpenGL 对应关系],
  [*图形学概念*], [*OpenGL 中的对应*],
  [顶点表示与图元], [VBO/VAO、`glDrawArrays`],
  [几何变换（模型/观察/投影）], [顶点着色器 + GLM 矩阵（坐标系统一章）],
  [光栅化与插值], [图元装配与光栅化阶段],
  [着色], [可编程顶点/片元着色器（GLSL）],
  [纹理映射], [纹理对象、采样器与环绕/过滤方式（纹理一章）],
  [消隐], [深度测试 `GL_DEPTH_TEST`],
)

= 创建窗口与你好，窗口

初始化 GLFW 并指定 3.3 Core Profile，`glfwCreateWindow` 创建窗口（标题在此传入），`gladLoadGL` 加载 OpenGL 函数指针，然后进入渲染循环：清屏、交换缓冲区、轮询事件，Esc 退出：

#figure(image("images/title_01_hello_window.png", width: 78%), caption: [创建窗口 / 你好，窗口])

= 你好，三角形

三个顶点经 VBO 上传 GPU，VAO 描述顶点属性的解析方式，顶点着色器输出 NDC 坐标，`glDrawArrays` 绘制；`--wireframe` 参数可切换线框模式观察：

#figure(image("images/title_02_hello_triangle.png", width: 78%), caption: [你好，三角形])

= 着色器

顶点属性携带颜色，经顶点着色器传给片元着色器，光栅化阶段自动对颜色做插值，得到渐变色三角形：

#figure(image("images/title_03_shaders.png", width: 78%), caption: [着色器])

== 实验：修改顶点着色器让三角形上下颠倒（作业任务 2）

NDC 坐标系 y 轴向上，把顶点着色器里 `gl_Position` 的 y 分量取反，三角形就会上下翻转。程序用 `--flip` 参数切换到这个着色器：

```glsl
// 上下颠倒的顶点着色器（src/03_shaders/main.cpp 中的 kVSFlipped）
#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aColor;
out vec3 ourColor;
void main() {
    gl_Position = vec4(aPos, 1.0);
    gl_Position.y = -gl_Position.y;  // 仅加这一行：y 取反，三角形上下颠倒
    ourColor = aColor;
}
```

三个顶点分别赋红、绿、蓝三色，颠倒后顶点位置和颜色插值一起翻转，红绿到了顶部，蓝色顶点朝下：

#table(
  columns: (1fr, 1fr),
  align: center + horizon,
  inset: 6pt,
  table.header([*正常（`./03_shaders`）*], [*颠倒（`./03_shaders --flip`）*]),
  image("images/task2_normal.png", width: 100%),
  image("images/task2_flipped.png", width: 100%),
)

= 纹理

用 stb\_image 加载 `container.jpg` 与 `awesomeface.png`，绑定到两个纹理单元，片元着色器 `mix()` 混合采样结果；开启 mipmap，加载时垂直翻转以对齐纹理坐标原点：

#figure(image("images/title_04_textures.png", width: 78%), caption: [纹理])

== 实验：纹理环绕方式与 4 个笑脸（作业任务 3）

把四边形四角的纹理坐标从教程的 `0.0f~1.0f` 扩大到 `0.0f~2.0f`，超出 `[0,1]` 的部分如何取样由环绕方式（`GL_TEXTURE_WRAP_S/T`）决定。程序用 `--wrap` 切换，两张纹理同步生效：

```cpp
glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, wrap);
glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, wrap);
```

#table(
  columns: (auto, 1fr, 1.5fr),
  align: (left + horizon, center + horizon, left + horizon),
  inset: 6pt,
  table.header(
    [*环绕方式*],
    [*运行结果*],
    [*现象*],
  ),
  [`GL_REPEAT`],
  image("images/task3_wrap_repeat.png", width: 100%),
  [取坐标小数部分，每个单位区间完整重复一遍 → *2×2 平铺，四角各一个完整笑脸*],
  [`GL_MIRRORED_REPEAT`],
  image("images/task3_wrap_mirrored.png", width: 100%),
  [每越过整数边界镜像一次 → 同样 2×2 共 4 个完整笑脸，两两镜像，接缝连续],
  [`GL_CLAMP_TO_EDGE`],
  image("images/task3_wrap_edge.png", width: 100%),
  [钳位到边缘像素，拉伸成条纹，图像失真],
  [`GL_CLAMP_TO_BORDER`],
  image("images/task3_wrap_border.png", width: 100%),
  [超出部分取边框色（设为黄色）→ 仅左下角 `[0,1]^2` 内是完整图像],
)

#block(breakable: false)[
*结论*：`GL_REPEAT` 和 `GL_MIRRORED_REPEAT` 都按 2×2 平铺，箱子四角各有一个完整笑脸，任务可以实现；两种 CLAMP 方式没有重复取样，放不出 4 个笑脸。`GL_REPEAT` 在接缝处画面直接跳变，`GL_MIRRORED_REPEAT` 镜像衔接更自然，所以程序默认用后者。
]

= 变换

顶点着色器引入模型矩阵后，用 GLM 做三步变换：`translate` 移到右下角，`rotate` 绕 z 轴随时间旋转，`scale` 缩小一半：

#figure(image("images/title_05_transformations.png", width: 78%), caption: [变换])

= 坐标系统

引入完整的 MVP 变换链（`projection * view * model`），在 3D 空间摆放 10 个不同朝向的箱子，开启深度测试（`GL_DEPTH_TEST`）处理遮挡：

#figure(image("images/title_06_coordinate_systems.png", width: 78%), caption: [坐标系统])

== 实验：FoV 与 aspect-ratio（作业任务 4）

`glm::perspective` 构造透视投影矩阵，把视点空间的透视平截头体压缩成规范立方体，实现近大远小。实验通过两个运行参数改变投影：`--fov` 修改 FoV，`--aspect` 修改宽高比系数（`src/06_coordinate_systems/main.cpp`）：

```cpp
glm::mat4 projection = glm::perspective(
    glm::radians(fov),                     // 竖直视野角
    (float)fbw / (float)fbh * aspectScale, // 宽高比
    0.1f, 100.0f);                         // near / far 平面
```

=== 实验一：固定 aspect = 4:3，改变 FoV（15° / 45° / 90° / 120°）

#table(
  columns: (1fr, 1fr, 1fr, 1fr),
  align: center + horizon,
  inset: 4pt,
  table.header([*FoV = 15°*], [*FoV = 45°（默认）*], [*FoV = 90°*], [*FoV = 120°*]),
  image("images/task4_fov_15.png", width: 100%),
  image("images/06_coordinate_systems.png", width: 100%),
  image("images/task4_fov_90.png", width: 100%),
  image("images/task4_fov_120.png", width: 100%),
)

=== 实验二：固定 FoV = 45°，改变 aspect 系数（0.5 / 2.0）

#table(
  columns: (1fr, 1fr),
  align: center + horizon,
  inset: 6pt,
  table.header([*aspect × 0.5（水平压缩）*], [*aspect × 2.0（水平拉伸）*]),
  image("images/task4_aspect_05.png", width: 100%),
  image("images/task4_aspect_20.png", width: 100%),
)

*结论*：

+ FoV 是竖直方向的视野角：15° 时视场窄，中间的箱子显得很大；90° 和 120° 时视场宽，单个箱子变小，画面边缘的透视变形也越来越明显。
+ aspect 是水平与竖直视野之比：乘 0.5 时画面被水平压扁，乘 2.0 时被拉宽。它等于视口的真实宽高比时物体比例才正常，所以窗口大小变化时要在 `framebuffer_size_callback` 里同步更新投影矩阵。
+ near/far 取 0.1 和 100，规定平截头体前后的范围，范围外的物体被裁掉。

// 摄像机整节（标题+正文+图）另起一页，避免末页只剩一张孤图
#pagebreak(weak: true)
= 摄像机

摄像机朝向用欧拉角（偏航/俯仰）表示：鼠标控制转向，滚轮改变 FoV 缩放，WASD 平移，观察矩阵由 `glm::lookAt` 构造：

#figure(image("images/title_07_camera.png", width: 78%), caption: [摄像机])
