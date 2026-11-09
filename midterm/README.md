# 计算机图形学期中论文阅读

## 选题

- 论文：Dynamic 3D Gaussians: Tracking by Persistent Dynamic View Synthesis
- 作者：Jonathon Luiten、Georgios Kopanas、Bastian Leibe、Deva Ramanan
- 会议：3DV 2024
- [项目主页](https://dynamic3dgaussians.github.io/) · [论文页面](https://arxiv.org/abs/2308.09713) · [作者代码](https://github.com/JonathonLuiten/Dynamic3DGaussians)
- 本地论文：[Dynamic-3D-Gaussians.pdf](paper/Dynamic-3D-Gaussians.pdf)，arXiv v1，2023 年 8 月 18 日。
- 作者代码快照：[vendor/Dynamic3DGaussians](vendor/Dynamic3DGaussians)，提交 `7dbbd4dec404308524ff402756bdb8143a2589b0`。保留了上游文件和许可证，快照不含上游 Git 历史。

## 作业要求

按课程《小组期中考核——论文阅读》，每组选择一篇论文，撰写不少于 3000 字的阅读报告，介绍论文内容并包含以下内容。

- 组内成员、学号、分工和贡献度。
- 相关工作、论文贡献、算法主体和结果分析。
- 每位组员的论文阅读收获。

运行作者代码或 Demo，将实验结果与论文效果图比较，可作为加分项。论文优缺点与改进方向也可作为加分项。报告 PDF 和组员阅读收获由组长整合提交，文件和邮件命名为“组号-期中报告”。课程文件标注的截止时间是 12 月 3 日 24:00，具体以课程通知为准。

## 报告模板

[report/REPORT.typ](report/REPORT.typ) 是论文式单栏报告骨架，[report/template.typ](report/template.typ) 定义排版和成员分工表。示例使用占位信息，可直接预览：

```sh
typst compile midterm/report/REPORT.typ midterm/report/REPORT.pdf
```

填写真实信息前，将 `report/info.example.typ` 复制为 `report/info.typ`，填写成员姓名、学号、负责部分和贡献度，再把 `REPORT.typ` 第二行的导入路径改为 `info.typ`。`info.typ` 已被忽略，避免成员信息进入版本库。按小组人数增删成员条目，贡献度由小组协商填写。正文中的提示文字和成员阅读收获也需要替换。

## 实验方向

优先在一台有 NVIDIA GPU 的机器上运行作者提供的预训练模型，保存几个时刻和视角的可视化截图，与论文中对应场景比较，再分析运动轨迹和局限。其他成员可基于论文、运行记录和截图参与分析。完整训练、定量评估及额外消融的范围由小组根据环境与时间决定。

作者提供的 [预训练模型压缩包](https://omnomnom.vision.rwth-aachen.de/data/Dynamic3DGaussians/output.zip) 约 10.8 GiB，未纳入本仓库。训练数据可从[作者仓库](vendor/Dynamic3DGaussians/README.md)获取。下载后按作者说明解压到代码目录，避免将数据、模型和生成文件提交到课程仓库。

环境、编译 CUDA 光栅化扩展及运行命令见[作者的安装与运行说明](vendor/Dynamic3DGaussians/README.md#installation)。作者另外维护 [diff-gaussian-rasterization-w-depth](https://github.com/JonathonLuiten/diff-gaussian-rasterization-w-depth) 依赖，运行时需单独安装。当前机器环境和 RTX 4070 Ti SUPER 的兼容性尚未验证。

作者说明公开代码与论文存在差异，包括颜色随时间的约束方式和地面损失。仓库列出的发布范围涵盖训练与可视化，评估、原始数据预处理和场景编辑代码在该版本中尚未发布。实验报告应说明所用版本和这些差异。具体内容参见[作者说明](vendor/Dynamic3DGaussians/README.md#differences-to-paper)。

## 许可

作者代码的许可及其依赖限制详见[上游许可证](vendor/Dynamic3DGaussians/LICENSE.md)和[作者的许可说明](vendor/Dynamic3DGaussians/README.md#notes-on-license)。
