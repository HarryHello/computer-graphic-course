# 计算机图形学课程仓库

本仓库保存平时作业、期中论文阅读和期末项目的代码与资料。

## 目录

- `0000/`、`0001/`、`0002/` 等按编号存放平时作业。
- [`midterm/`](midterm/README.md) 存放期中论文、作者代码和报告模板。
- [`final/`](final/README.md) 存放期末项目资料与后续实现。
- [`Report-Templates/`](Report-Templates/README.md) 存放通用报告模板。

## 分支协作

`main` 保存课程材料和已完成的成果。任务进行期间，从 `main` 创建单独分支，完成后检查并合并回 `main`。

- `midterm-report` 用于期中报告、实验和结果分析。
- `final-project` 可用于期末项目。
- 工作量较大的平时作业可使用独立分支，例如 `assignment-3`。

开始工作前先同步对应分支。提交时保持改动范围清晰，并推送到该分支，便于其他成员获取和审阅。示例：

```sh
git switch midterm-report
git pull --ff-only
# 修改文件并检查结果
git add <文件路径>
git commit -m "docs(midterm): 描述本次修改"
git push
```

已有平时作业、期中报告和期末项目仍按目录组织；多个任务需要同时进行时，可以使用 `git worktree` 建立独立工作目录。新成员首次检出远程分支可使用 `git switch --track origin/midterm-report`。

## 文件管理

按根目录和各项目的 `.gitignore` 排除构建产物、报告 PDF、个人信息和本地环境文件。确需跟踪的论文资料可单独纳入版本库。期中报告成员信息的填写与预览方式见[期中报告说明](midterm/README.md#报告模板)。数据集、模型权重等大文件优先在项目说明中记录获取方式。
