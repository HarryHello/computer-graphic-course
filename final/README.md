# Inkshift

小组选择 **第 7 题：游戏建模和动画**，用 Unity 制作单人 3D 平台跳跃游戏。玩家在四间封闭实验室中切换立方体和球体，跨越平台和坡道，在地面和可触及的墙面留下颜料。

## 试玩

用 Unity **6000.3.28f1** 打开项目，在 Project 窗口打开标题场景后点击 Play。首次游玩选择 **Start Run**，也可通过 **Select Route** 进入已经解锁的关卡。[Unity 项目](Game/) · [标题场景](Game/Assets/Inkshift/Scenes/InkshiftTitle.unity)

| 按键 | 操作 |
| --- | --- |
| WASD 或方向键 | 按当前视角移动 |
| Tab | 切换球体与立方体 |
| 空格 | 跳跃，空中可再按一次 |
| Q / E | 左转或右转镜头 90° |
| R | 保持当前形态，回到最近的检查点或起点 |
| Esc | 暂停或继续 |

每关有三个检查点。掉落或按 R 时，角色保持当时的形态，镜头朝向复活点之后的主路线。暂停菜单中的 **Restart Route** 会重开整关。

抵达出口后，角色在卷帘门前停下，等门升起后滚进门内。结算显示用时、重试次数及最快纪录提示。选择 **Next Route** 后画面淡入黑场，再进入下一关。最后一关的主按钮返回标题。旧版通关进度继续用于解锁关卡，四条新路线的最快用时重新记录在本机。

## 项目结构

- 移动规则、关卡节奏和视觉约定见[游戏设计](DESIGN.md)。
- 场景目录包含标题和四个关卡。**Transfer Wing** 的场景文件名为 `Cloudline.unity`。可以直接打开任一关卡进入 Play 模式调试。[场景目录](Game/Assets/Inkshift/Scenes/)
- 关卡资产定义路线顺序、场景名和简介，玩家预制件保存共享的控制参数。各场景的 **Route Markers** 包含检查点和终点。[Campaign.asset](Game/Assets/Inkshift/Campaign.asset) · [Player.prefab](Game/Assets/Inkshift/Player.prefab)
- 修改路线布局后，可在 Unity 的 **Inkshift → Rebuild route** 中按关卡重建并保存场景。打开对应场景，运行 **Inkshift → Validate laboratory routes**，可在 Console 检查路线标记下方是否有可站立的碰撞面。
- 构建时使用 Unity 的 **File → Build Profiles**，目标为 macOS 和 Windows。标题场景在场景列表第一位。

当前版本使用 Unity URP 光照和阴影。四间实验室使用 Blender 模块与 Unity 材质搭建。课程要求中的自实现 Phong 光照和阴影、Phong / Gouraud 着色切换及网格查看留待后续完成。

## 实验室模型

模型源文件包含墙板、顶部桁架、壁灯、管架、管道弯头、卷帘门、排水格栅和平台支架八个可编辑模块。模块保留独立零件、倒角修改器和导出原点。Unity 由预制件引用 FBX 并配置材质。关卡出口的钢制门帘、导轨和上方门箱在场景中组装，门帘及其碰撞体由出口脚本一同抬升。[Blender 源文件](art/InkshiftLabKit.blend) · [FBX 模型](Game/Assets/Inkshift/Art/Lab/Models/) · [预制件](Game/Assets/Inkshift/Art/Lab/Prefabs/)

细化模型时按米编辑，以各集合的 `export origin` 为模块原点。导出时应用修改器、合并模块网格，保留原模型名以及材质槽的名称和顺序，使用 `-Z Forward`、`Y Up`。墙板宽 5.2 米、高 4 米，前表面朝 Blender 的 `-Y`。将导出的 FBX 替换到原路径，保留 Unity 生成的 `.meta` 文件。Unity 导入后按槽名核对预制件的材质，再打开使用该模块的场景检查拼缝、灯具和 Q / E 转向时的视野。

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
