#import "../../Report-Templates/templates/report.typ": *
#import "../../Report-Templates/info.typ": info

#let reportInfo = (
  title: "Assignment 3: Ray Tracing",
  subtitle: "计算机图形学实验报告",
  time: "2026 年 11 月",
  banner: "../../Report-Templates/images/sysu.png",
)

#show: report.with(..reportInfo, ..info)
#show heading: it => block(sticky: true, it)

本报告为待填写骨架。当前整理了课程起始框架，基础任务和提升任务均待完成。

= 开发环境

待填写实际使用的系统、编译器、依赖版本和构建配置。

= 基础任务

== 射线、摄像机与天空背景

待完成。第一册 Chapter 4。

== 球体、法线与抗锯齿

待完成。第一册 Chapter 5–7。

== 漫反射、金属与电介质材质

待完成。第一册 Chapter 8–10。

== 摄像机与聚焦模糊

待完成。第一册 Chapter 11–12。

== 最终场景

待完成。第一册 Chapter 13。填写实际的场景、分辨率、采样数和递归深度。

== BVH 加速与计时对比

待完成。第二册 Chapter 3。填写同一配置下使用 BVH 前后的实测时间。

== 理解与收获

待实际实现后填写。

= 提升任务

待完成。按第二册其余实现章节记录完成内容、实现方法和效果。
