#import "../../Report-Templates/templates/report.typ": *
#import "../../Report-Templates/info.typ": info

#let reportInfo = (
  title: "Rasterization & Z-buffering",
  subtitle: "计算机图形学实验 2",
  time: "2026 年 11 月",
  banner: "../../Report-Templates/images/sysu.png",
)
#show: report.with(..reportInfo, ..info)
#show heading: it => block(sticky: true, it)
#show raw: set text(font: ("Menlo", "DejaVu Sans Mono", "Courier New"))

= 基础任务

在 Apple Silicon macOS 上用 CMake 和 Apple Clang 构建。基础程序沿用课程 TinyRenderer 的变换、着色和 SDL2 窗口，加载角色和地板两个 OBJ 模型。下方三幅图使用同一初始视角，分辨率为 666 × 500。

== Bresenham 线段光栅化

用整数误差决定沿 $x$、$y$ 中哪几个方向迈步。同一套判断覆盖陡线、缓线和反向线段，循环包含起终点。设步数 $n = max(|Delta x|, |Delta y|)$，第 $k$ 步以 $k/n$ 调用 `VertexData::lerp`。零长度线段只取起点。用实际步进的像素坐标覆盖插值坐标，并在写入片元列表前检查窗口边界。

== 边函数与重心插值

视口变换后沿用框架的整数屏幕坐标。对于有向边 $a b$ 和待测点 $p$，边函数为

$ E_(a b)(p) = (b_x-a_x)(p_y-a_y) - (b_y-a_y)(p_x-a_x). $

统一顶点绕序后，跳过面积为零的三角形，再遍历与屏幕相交的包围盒。三个边函数均为正时像素在内部。零值按上边、左边归属规则处理，使共享边上的样本只由其中一个三角形生成。

将三个对边函数值除以有向面积，得到重心权重。用 `VertexData::barycentricLerp` 插值得到片元属性，最终采用循环中的像素索引写入帧缓冲。

== 深度测试

透视除法后，`cpos.z` 保存近端为 $-1$、远端为 $1$ 的 NDC 深度。每帧把深度缓冲清为 $1$，若片元比缓冲中记录的片元更远，就跳过着色。通过测试的片元执行透视校正、着色并更新颜色和深度。开启深度测试后，模型露在地板前方的脚部恢复可见，两幅填充图有 3,924 个像素的 RGB 值不同。

#figure(
  grid(
    columns: (1fr, 1fr, 1fr),
    gutter: 8pt,
    image("images/wireframe.png"), image("images/fill-no-depth.png"), image("images/fill-depth.png"),
    align(center, [Bresenham 线框]),
    align(center, [填充，关闭深度测试]),
    align(center, [填充，开启深度测试]),
  ),
  caption: [模型在三种渲染设置下的初始帧],
)

#pagebreak()

= 提升任务

提升程序沿用课程给出的两组三角形顶点与颜色，用 GLM 完成变换，用 stb_image_write 导出 700 × 700 的 PNG。两个三角形相对相机分别位于深度 7 和 10，浅绿三角形更近，浅蓝三角形更远。

== 双三角形的 Z-buffer

包围盒内每个像素在中心 $(x+0.5, y+0.5)$ 测试覆盖。投影后将 NDC 深度映射到 $[0, 1]$，按屏幕空间重心权重线性插值。每个深度槽起始为 $+infinity$，样本通过测试后才更新该槽的颜色和深度。在重叠区域，先提交浅绿三角形、后提交浅蓝三角形，仍可看到浅绿三角形覆盖浅蓝三角形。

== 2 × 2 超采样

在横纵方向各取 $1/4$ 和 $3/4$ 两个偏移，组成四个独立子样本。每个样本分别进行覆盖测试和深度比较。全部三角形绘制完成后，将四个样本的颜色等权平均。外轮廓处部分样本保留背景色，遮挡边界处则可同时保留近、远三角形的颜色。

#figure(
  grid(
    columns: (1fr, 1fr),
    gutter: 16pt,
    align(center, image("images/triangle-1x.png", width: 120pt)),
    align(center, image("images/triangle-4x.png", width: 120pt)),
    align(center, image("images/edge-1x.png", width: 108pt)),
    align(center, image("images/edge-4x.png", width: 108pt)),
    align(center, [单采样]),
    align(center, [2 × 2 超采样]),
  ),
  caption: [同场景对比。第二行为重叠区域右侧斜边的等比例放大图。],
)

两种模式在重叠区内部的颜色一致，超采样图中有 832 个像素取背景色或三角形纯色之外的混合值。另用一个像素的场景复核样本深度。该像素的一个样本被近处红色覆盖，其余三个样本保留远处蓝色。交换提交顺序后，输出仍为四分之一红色和四分之三蓝色。
