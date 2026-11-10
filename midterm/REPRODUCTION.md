# Dynamic 3D Gaussians 复现与交接指南

## 当前进度与目标

本组已选择 Dynamic 3D Gaussians，论文阅读报告初稿在 [`report/REPORT.typ`](report/REPORT.typ)。**截至编写本指南，小组尚未在 PC 上运行作者代码，以下是待验证的操作方案。**

本次实验先使用作者的预训练结果，运行一个场景的可视化，保存画面和轨迹截图，并与论文中的对应场景比较。实验完成后，将实际配置、操作过程和观察填入报告的「小组实验与结果对照」一节。重新训练和定量评测可以另行决定。

论文是仓库中的 [arXiv v1](paper/Dynamic-3D-Gaussians.pdf)，作者主代码在 [`vendor/Dynamic3DGaussians/`](vendor/Dynamic3DGaussians/) 中，来源提交为 `7dbbd4dec404308524ff402756bdb8143a2589b0`。相关课程要求和作者代码差异见[期中目录说明](README.md)。

## 到 PC 后先检查

1. 拉取仓库并切换到 `midterm-report`，确认报告初稿已同步。运行前检查 `git status --short --branch`，保留 PC 上已有的未提交工作。
2. 确认 PC 的操作系统、NVIDIA 驱动、可用磁盘空间和网络。已知目标 GPU 是 RTX 4070 Ti SUPER，显存 16 GB。尚未在这台机器上验证兼容性、内存需求或运行时间。
3. 作者的预训练模型压缩包约 10.8 GiB，解压还需要额外空间。数据下载、作者 CUDA 依赖和 Conda 包都需要网络。大文件留在本地，不要纳入 Git。
4. 下文使用 Linux shell。若 PC 使用 Windows，可先检查是否已有 WSL2、Ubuntu、GPU 驱动和图形界面支持，再决定是否采用 WSL2。WSL2 路线尚未实测。若 PC 原生运行 Linux，则使用其终端。

在目标环境中记录下列命令的输出：

```sh
uname -a
nvidia-smi
nvcc --version
conda --version
```

`nvidia-smi` 中显示的 CUDA 版本表示驱动支持的 CUDA 版本范围，不能代替 `nvcc --version`。扩展编译需要 CUDA Toolkit 的编译器及可用的 C++ 编译工具。若是 WSL2，GPU 驱动安装在 Windows 主机，不要在 WSL 中安装 Linux NVIDIA 显示驱动。可视化程序使用 Open3D 窗口，需要图形显示环境。参考 [NVIDIA 的 WSL 指南](https://docs.nvidia.com/cuda/wsl-user-guide/)和 [Microsoft 的 WSL GUI 说明](https://learn.microsoft.com/en-us/windows/wsl/tutorials/gui-apps)。

## 环境与作者依赖

从仓库根目录进入作者代码目录，后续相对路径均以该目录为基准：

```sh
cd midterm/vendor/Dynamic3DGaussians
conda env create --file environment.yml
conda activate dynamic_gaussians
```

这是作者原始环境文件，指定 Python 3.7、PyTorch 1.12.1、CUDA 运行时 11.6 和 Open3D 0.16.0。它可能与 RTX 4070 Ti SUPER、系统 CUDA Toolkit 或当前包源不兼容。若创建失败或 GPU 无法使用，先保留报错并检查版本，再按实际情况调整，不要直接覆盖机器现有环境。CUDA 11.8 的 `nvcc` 支持为 Ada 架构生成原生代码；较旧版本也可能通过兼容的 cubin 或 PTX 工作，是否可用取决于实际编译结果。参考 [NVIDIA Ada 兼容性指南](https://docs.nvidia.com/cuda/ada-compatibility-guide/)。

作者还要求单独安装带深度输出的 CUDA 光栅化器。下面将依赖放在用户目录，保留课程仓库中的作者代码快照：

```sh
mkdir -p ~/dynamic3d-dependencies
git clone --recursive https://github.com/JonathonLuiten/diff-gaussian-rasterization-w-depth.git ~/dynamic3d-dependencies/diff-gaussian-rasterization-w-depth
python -m pip install ~/dynamic3d-dependencies/diff-gaussian-rasterization-w-depth
```

如果该目录已经存在，先检查其中的仓库和改动，再决定如何复用。编译后记录 `git -C ~/dynamic3d-dependencies/diff-gaussian-rasterization-w-depth rev-parse HEAD`。作者主仓库和光栅化器的许可范围不同，详见各自的许可证及[作者许可说明](vendor/Dynamic3DGaussians/README.md#notes-on-license)。

验证环境时，先确认 Torch 能识别显卡，并测试扩展和 Open3D 能否导入：

```sh
python -c "import torch; print('torch:', torch.__version__, 'torch CUDA:', torch.version.cuda); print('CUDA 可用:', torch.cuda.is_available()); print('GPU:', torch.cuda.get_device_name(0) if torch.cuda.is_available() else '未检测到')"
python -c "import diff_gaussian_rasterization, open3d; print('CUDA 扩展与 Open3D 导入成功')"
```

导入成功只说明依赖已加载，实际渲染和图形窗口还需要后续验证。

## 获取模型并检查路径

作者提供的模型地址是 <https://omnomnom.vision.rwth-aachen.de/data/Dynamic3DGaussians/output.zip>。可先下载到课程仓库之外，再检查压缩包结构：

```sh
mkdir -p ~/dynamic3d-data
curl -L -C - -o ~/dynamic3d-data/output.zip https://omnomnom.vision.rwth-aachen.de/data/Dynamic3DGaussians/output.zip
unzip -l ~/dynamic3d-data/output.zip | head -40
```

作者说明直接在主代码目录解压。先确认列表中的路径以 `output/` 开头且没有与本地已有文件冲突，再执行：

```sh
unzip ~/dynamic3d-data/output.zip
test -f output/pretrained/juggle/params.npz && echo 'juggle 模型路径存在'
```

`visualize.py` 读取的格式是 `./output/<实验名>/<场景名>/params.npz`，预训练实验名为 `pretrained`。如压缩包目录不同，以实际列表为准并调整位置。仓库中作者的 `.gitignore` 已忽略该目录。默认程序会依次打开 `basketball`、`boxes`、`football`、`juggle`、`softball` 和 `tennis` 六个场景。

## 先运行一个场景

仍在作者代码目录中，运行单个场景，避免依次打开全部六个窗口：

```sh
python -c "import visualize as v; v.visualize('juggle', 'pretrained')"
```

成功时应出现 Open3D 窗口，显示动态场景。可以交互调整视角，确认动画随时间变化。关闭窗口即可结束单场景程序。渲染输出和轨迹并非默认自动保存，应在 PC 上截图或录屏，并记录场景和大致时间位置。

如基本画面正常，可单独启用轨迹，再运行同一场景：

```sh
python -c "import visualize as v; v.ADDITIONAL_LINES='trajectories'; v.visualize('juggle', 'pretrained')"
```

这里通过导入模块设置选项，保留作者的 `visualize.py` 快照。轨迹模式会跳过最初不足一个轨迹长度的帧。作者代码默认每隔 25 个前景高斯显示一条轨迹，轨迹长度为 15。展示旋转方向时可把 `trajectories` 改为 `rotations`。这些命令根据代码入口整理，尚未在目标 PC 执行。

## 常见问题与记录

- **CUDA 不可用或架构不兼容**：记录 `nvidia-smi`、`nvcc --version`、PyTorch 版本及完整报错。分别检查驱动、PyTorch 的 CUDA 构建与编译扩展所用 Toolkit。
- **扩展编译失败**：记录编译器和 CUDA Toolkit 版本，确认 `third_party/glm` 已获取，并检查 PyTorch 与编译器版本匹配。重新安装依赖前保留报错。
- **找不到 `params.npz`**：确认当前工作目录是作者主代码目录，再检查 `output/pretrained/<场景>/params.npz` 与解压层级。
- **窗口无法打开**：确认图形会话可用，尤其是 WSL2 的 GUI 支持；同时保存终端报错。
- **显存不足**：记录显存峰值与报错。代码会把整个序列的参数转移到 GPU，16 GB 能否容纳具体场景尚未验证。后续可考虑分帧加载或其他降内存处理，并在报告中说明修改及其对比较的影响。

实验记录至少包括运行日期、操作系统、GPU 和驱动、PyTorch/CUDA/扩展版本、作者代码提交、场景、模型来源、具体命令、截图位置与结果。优先保留同一场景的普通渲染和轨迹截图，对照论文图 1 或其他确实对应的场景与视角；无法确定一致时，说明差异和比较范围。

## 报告与下一次对话

在 [`report/REPORT.typ`](report/REPORT.typ) 的「小组实验与结果对照」中填写实际结果，引用所使用的模型和代码来源。论文原图与本组截图要分别标注，不把预训练可视化写成自行训练或论文定量指标的复现。编译与成员信息填写方式见[报告模板说明](README.md#报告模板)。

新的对话可以从下面的说明开始：

> 请先阅读 `AGENTS.md`、`midterm/README.md` 和 `midterm/REPRODUCTION.md`。我现在使用带 RTX 4070 Ti SUPER 的 PC，请检查当前环境，在 `midterm-report` 分支继续运行作者的预训练模型，并将真实的操作与结果写入期中报告。先检查现有环境和工作区，再决定需要安装的依赖。
