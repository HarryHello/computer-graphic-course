// 计算机图形学 Assignment 1（HW1）实验报告
// 章节标题沿用作业原文（基础任务/提升任务/挑战任务/选做思考题）
// 编译：typst compile --root ../.. REPORT.typ
// 个人信息在 Report-Templates/info.typ 中集中维护，所有报告共用
#import "../../Report-Templates/templates/report.typ": *
#import "../../Report-Templates/info.typ": info

// 本报告专属信息（个人信息见 info.typ）
#let reportInfo = (
  title: "Assignment 1：3D Transformation",
  subtitle: "计算机图形学作业汇报（HW1）",
  time: "2026 年 10 月",
  banner: "../../Report-Templates/images/sysu.png",
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
  [渲染器], [课程框架 TinyRenderer（CPU 软光栅化，无硬件加速）],
  [窗口与交互], [SDL2（Homebrew 安装）],
  [数学库], [GLM 0.9.9.8（框架自带）],
  [模型], [`diablo3_pose.obj`、`floor.obj`（TinyObjLoader 加载）],
  [构建], [CMake + Apple Clang],
)

本次修改 `src/TRRenderer.cpp`（四个变换矩阵的逐元素实现）与 `src/main.cpp`（提升任务的缩放动画、挑战任务的投影切换）。构建后从 `0001/` 目录运行 `build/CGAssignment1`（模型按相对路径 `model/` 加载）。

= 基础任务：三维变换矩阵

按要求逐元素计算并赋值观察、透视投影、视口三个矩阵，未调用 `glm::lookAt`、`glm::perspective` 等取巧 API。`glm::mat4` 为列主序，`m[col][row]` 表示第 col 列第 row 行，下同。

== 观察矩阵（world → camera）

观察变换把相机搬回原点、世界向量变换到相机坐标系。由三个输入构造相机的正交基：z 轴指向相机后方（`camera - target` 方向），x 轴由 worldUp 与 z 叉乘得出，y 轴再由 z、x 叉乘得出。旋转部分是"世界 → 相机"的逆变换，基向量按行摆放；平移部分是相机位置在三个基向量上投影的负值，存放在第 3 列：

```cpp
// 相机局部基：z 指向相机后方，x/y 由 worldUp 叉乘得出
glm::vec3 zAxis = glm::normalize(camera - target);
glm::vec3 xAxis = glm::normalize(glm::cross(worldUp, zAxis));
glm::vec3 yAxis = glm::normalize(glm::cross(zAxis, xAxis));

// 旋转部分：矩阵的三行分别是相机基向量 x/y/z
// 平移部分：相机位置在基向量上的投影取负，存于第 3 列
vMat[0][0] = xAxis.x; vMat[1][0] = xAxis.y; vMat[2][0] = xAxis.z;
vMat[0][1] = yAxis.x; vMat[1][1] = yAxis.y; vMat[2][1] = yAxis.z;
vMat[0][2] = zAxis.x; vMat[1][2] = zAxis.y; vMat[2][2] = zAxis.z;
vMat[3][0] = -glm::dot(xAxis, camera);
vMat[3][1] = -glm::dot(yAxis, camera);
vMat[3][2] = -glm::dot(zAxis, camera);
vMat[3][3] = 1.0f;
```

== 透视投影矩阵（camera → clip）

透视投影的视锥体是平截头体：x、y 除以 $-z$ 产生近大远小，这个除法被放入 w 分量（$w = -z$）留给透视除法；z 线性映射到 $[-1, 1]$。设 $t = tan("fovy"\/2)$，$a$ 为宽高比，$n$、$f$ 为近、远平面距离：

$ mat(
  1/(a t), 0, 0, 0;
  0, 1/t, 0, 0;
  0, 0, -(f + n)/(f - n), -1;
  0, 0, -(2 f n)/(f - n), 0;
) $

```cpp
float tanHalfFovy = std::tan(glm::radians(fovy) / 2.0f);
pMat[0][0] = 1.0f / (aspect * tanHalfFovy);
pMat[1][1] = 1.0f / tanHalfFovy;
pMat[2][2] = -(far + near) / (far - near);
pMat[2][3] = -1.0f;                          // w = -z，供透视除法使用
pMat[3][2] = -(2.0f * near * far) / (far - near);
```

== 视口矩阵（NDC → screen）

把 NDC 坐标 $[-1, 1]$ 线性缩放平移到像素坐标。NDC 的 y 轴向上，而帧缓冲第 0 行是窗口顶部，所以 y 方向取负翻转：

$ mat(
  W\/2, 0, 0, W\/2;
  0, -H\/2, 0, H\/2;
  0, 0, 1, 0;
  0, 0, 0, 1;
) $

```cpp
vpMat[0][0] = width / 2.0f;
vpMat[1][1] = -height / 2.0f;   // 帧缓冲第 0 行是窗口顶部，y 翻转
vpMat[2][2] = 1.0f;
vpMat[3][0] = width / 2.0f;
vpMat[3][1] = height / 2.0f;
vpMat[3][3] = 1.0f;
```

== 正确性验证与运行效果

三个矩阵实现完成后，另写独立程序将它们的计算结果与 `glm::lookAt`、`glm::perspective` 逐元素比对，误差为 0（实现初期 view 的平移分量写错行、透视矩阵 $[2][3]$ 与 $[3][2]$ 两个元素放反，渲染出现乱线，靠该比对程序定位修复）。

运行效果：模型绕 y 轴持续旋转；按住鼠标左键横向拖动旋转摄像机，滚轮拉近、拉远：

#table(
  columns: (1fr, 1fr),
  align: center + horizon,
  inset: 6pt,
  table.header([*默认视角*], [*按住左键拖动旋转摄像机后*]),
  image("images/task1_basic_persp_b.png", width: 85%),
  image("images/task1_basic_persp.png", width: 85%),
)

= 提升任务：缩放动画

用 `glm::scale` 实现放大、缩小循环：缩放相位随时间推进，缩放因子取 $2 - cos(phi)$，在 $[1, 3]$ 之间周期变化，相位为 0 时是原大，随时间放大到 3 倍再缩回，周而复始；速度由相位的系数控制。原旋转代码按要求注释：

```cpp
static float scalePhase = 0.0f;
scalePhase += (float)deltaTime * 0.0015f;
float scaleFactor = 2.0f - glm::cos(scalePhase);   // ∈ [1, 3]，循环
model_mat = glm::scale(glm::mat4(1.0f), glm::vec3(scaleFactor));
```

一个周期内不同时刻的画面，模型依次被放大（约 1 倍 → 约 2 倍 → 约 3 倍），随后缩回：

#table(
  columns: (1fr, 1fr, 1fr),
  align: center + horizon,
  inset: 4pt,
  table.header([*约 1 倍*], [*约 2 倍*], [*约 3 倍*]),
  image("images/task2_scale_small.png", width: 85%),
  image("images/task2_scale_mid.png", width: 85%),
  image("images/task2_scale_large.png", width: 85%),
)

= 挑战任务：正交投影矩阵

正交投影的视锥体是长方体，没有近大远小：x、y、z 各自线性映射到 $[-1, 1]$，w 保持 1。设视锥体六面为 $l$、$r$、$b$、$t$、$n$、$f$：

$ mat(
  2/(r - l), 0, 0, -(r + l)/(r - l);
  0, 2/(t - b), 0, -(t + b)/(t - b);
  0, 0, -2/(f - n), -(f + n)/(f - n);
  0, 0, 0, 1;
) $

```cpp
pMat[0][0] = 2.0f / (right - left);
pMat[1][1] = 2.0f / (top - bottom);
pMat[2][2] = -2.0f / (far - near);
pMat[3][0] = -(right + left) / (right - left);
pMat[3][1] = -(top + bottom) / (top - bottom);
pMat[3][2] = -(far + near) / (far - near);
```

`main.cpp` 中通过注释切换两种投影（当前启用透视）：

```cpp
renderer->setProjectMatrix(TRRenderer::calcPerspProjectMatrix(45.0f, (float)width / height, 0.1f, 10.0f));
//renderer->setProjectMatrix(TRRenderer::calcOrthoProjectMatrix(-2.0f, +2.0f, -2.0f, +2.0f, 0.1f, 10.0f));
```

滚动滚轮拉近拉远对比两种投影：透视下拉远模型明显变小（近大远小），正交下模型轮廓大小始终不变，只有超出长方体范围时才被裁掉：

#table(
  columns: (1fr, 1fr),
  align: center + horizon,
  inset: 6pt,
  table.header([*透视投影（近大远小）*], [*正交投影（大小与距离无关）*]),
  image("images/task1_basic_persp_b.png", width: 85%),
  image("images/task3_ortho.png", width: 85%),
)

= 选做思考题

#block(width: 100%, fill: luma(245), inset: (x: 10pt, y: 7pt), radius: 4pt)[
  *题目（1）　请简述正交投影和透视投影的区别。*
]

答：透视投影模拟人眼和相机：视锥体是平截头体，投影大小与距离成反比，近大远小，平行线在远处相交于灭点。正交投影的视锥体是长方体，投影大小与距离无关，平行线投影后依然平行。前者用于游戏、影视等真实感场景，后者用于 CAD 制图、工程图纸、2D 界面等场合。

#block(width: 100%, fill: luma(245), inset: (x: 10pt, y: 7pt), radius: 4pt)[
  *题目（2）　从物体局部空间的顶点到最终屏幕空间上的顶点，需要经历哪几个空间的坐标系？裁剪空间下的顶点的 w 值是哪个空间坐标下的 z 值？它有什么空间意义？*
]

答：局部空间经 model 矩阵到世界空间，经 view 矩阵到相机（观察）空间，经 projection 矩阵到裁剪空间，做透视除法后到 NDC，经视口变换到屏幕空间。

裁剪空间下顶点的 w 值是相机空间 z 值的相反数（$w = -z_"camera"$），即顶点到相机平面的距离。它有两个用途：透视除法以 w 为分母，除完才实现近大远小，逐属性插值也要靠它做透视校正；裁剪以它为判据，视锥体内顶点满足 $-w <= x, y, z <= w$，$w <= 0$ 的顶点在相机后方，直接被裁掉。

#block(width: 100%, fill: luma(245), inset: (x: 10pt, y: 7pt), radius: 4pt)[
  *题目（3）　经过投影变换之后，几何顶点的坐标值是被直接变换到了 NDC 坐标（即 xyz 的值全部都在 [-1,1] 范围内）吗？透视除法（Perspective Division）是什么？为什么要有这么一个过程？*
]

答：不是。投影矩阵输出的是裁剪空间下的齐次坐标，xyz 分量并不是 NDC（视锥体内的点除以 w 之前一般也不在 $[-1, 1]$ 内），要再除以 w 才落到 NDC。透视除法就是把齐次坐标的四个分量统一除以 w，得到三维笛卡尔坐标。

需要它的原因：投影必须和 model、view 一样用 $4 times 4$ 矩阵表达，而“除以 $-z$”不是线性变换，只能先把 $1\/-z$ 存进 w，把除法推迟为单独一步。这一步同时完成两件事：x、y 除以 $-z$ 产生近大远小；z 除以 w 得到非线性压缩的深度，供深度测试使用。GPU 在裁剪后自动执行透视除法，软光栅化渲染器则需要在光栅化前手动完成。
