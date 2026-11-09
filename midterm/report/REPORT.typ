#import "template.typ": paper, member-table
#import "info.example.typ": info

#show: paper.with(
  title: "Dynamic 3D Gaussians",
  subtitle: "基于持续动态视图合成的三维场景表示与追踪",
  course: "计算机图形学 · 期中论文阅读报告",
  authors: info.authors,
  date: info.date,
  summary: [
    在此概述研究问题、论文方法、主要结论和小组实验。填写真实结果后删除本提示。
  ],
)

= 引言

介绍研究背景、动态场景重建与新视角合成的问题，以及论文的研究目标。

= 背景与相关工作

概述三维高斯表示、新视角合成和动态场景追踪所需的背景。

= 方法

== 动态高斯表示

介绍高斯的空间、外观与运动属性。

== 逐帧优化与运动约束

解释模型初始化、随时间的更新以及局部运动约束。结合论文公式和自绘流程图说明。

== 新视角合成与追踪

说明动态场景的渲染过程以及高斯轨迹的含义。

= 论文实验与结果分析

介绍论文使用的数据、评估方式和关键结果，并标注图表来源。

= 小组实验与结果对照

记录代码版本、运行环境、使用的数据或预训练模型，以及与论文效果图的比较。实验完成后填写真实结果。

= 讨论与改进方向

结合论文和小组实验，分析方法的优点、局限和可行的改进方向。

= 结论

概括论文的主要贡献与小组的认识。

= 参考文献

+ Luiten, J., Kopanas, G., Leibe, B., & Ramanan, D. Dynamic 3D Gaussians: Tracking by Persistent Dynamic View Synthesis. 3DV, 2024. #link("https://arxiv.org/abs/2308.09713")
+ Kerbl, B., Kopanas, G., Leimkühler, T., & Drettakis, G. 3D Gaussian Splatting for Real-Time Radiance Field Rendering. ACM Transactions on Graphics, 2023. #link("https://arxiv.org/abs/2308.04079")

= 小组分工与贡献

请填写每位成员的姓名、学号、负责部分和贡献度。

#member-table(info.members)

= 成员阅读收获

各成员结合自己负责的内容填写阅读收获。
