# Inkshift

小组选择 **第 7 题：游戏建模和动画**，用 Unity 制作单人 3D 平台跳跃游戏。玩家在云上的四条路线中切换立方体和球体，跨越平台并留下颜料。

## 试玩

用 Unity **6000.3.28f1** 打开 [Game](Game/)，在 Project 窗口打开 [InkshiftTitle.unity](Game/Assets/Inkshift/Scenes/InkshiftTitle.unity)，点击 Play，再选择 **Start Run**。通关后可前往下一关，或返回标题页选择已经解锁的关卡。

| 按键 | 操作 |
| --- | --- |
| WASD 或方向键 | 按当前视角移动 |
| Tab | 切换球体与立方体 |
| 空格 | 跳跃，空中可再按一次 |
| Q / E | 左转或右转镜头 90° |
| R | 保持当前形态，回到最近的检查点或起点 |
| Esc | 暂停或继续 |

掉落和按 R 重试时，镜头会朝向复活点后续的主路线。每关记录最快通关用时，保存在本机。暂停菜单中的 **Restart Route** 会重开整关。

## 项目结构

- [游戏设计](DESIGN.md)记录移动规则、关卡节奏和视觉约定。
- [Scenes](Game/Assets/Inkshift/Scenes/)包含标题和四个关卡。可以直接打开任一关卡进入 Play 模式调试。
- [Campaign.asset](Game/Assets/Inkshift/Campaign.asset)定义关卡顺序、场景名和简介。[Player.prefab](Game/Assets/Inkshift/Player.prefab)包含共享的玩家控制参数。各场景的 **Route Markers** 保存检查点、提示区和终点。
- 构建时使用 Unity 的 **File → Build Profiles**，目标为 macOS 和 Windows。标题场景在场景列表第一位。

当前版本使用 Unity URP 光照和阴影。课程要求中的自实现 Phong 光照、阴影、Phong / Gouraud 着色模式及模型导入与网格查看留待后续完成。

## 题目要求

使用 OpenGL 或 Unity3D，结合建模工具，实现一个完整的游戏或动画。两种开发方式均需包含：

- 摄像机。
- 添加基本对象，例如线、立方体或贝塞尔曲线。
- 平移、旋转和缩放变换。
- 基础 Phong 光照。
- Phong 和 Gouraud 着色模式。
- 纹理映射和阴影。
- 模型导入与网格查看。

若使用 Unity3D，必须自行实现 Phong 光照和阴影；项目中有一两个光源和阴影由小组自行实现即可，无需所有对象都自行实现。题目对可使用的库不作限制，包括物理引擎库和动画库。

## 展示与提交

- 制作 PPT，介绍完成的基础功能和额外功能；现场演示程序，并回答老师和助教的问题。
- 提交包包含 `code/`（相关代码）、`release/`（可执行程序）、`README.md`（项目与实现简述）和 `demo/`（演示视频与结果图片）。提交前清理编译中间文件和无关文件。
- 题目文档注明截止时间为 **12 月 17 日 24:00**，未注明年份，请以课程最新通知为准。

题目来源：课程资料 `FinalProject.pdf`。
