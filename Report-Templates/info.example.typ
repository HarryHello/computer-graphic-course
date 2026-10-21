// 个人信息配置模板（所有报告共用）—— 复制本文件为 info.typ 并填写小组真实信息：
//   cp info.example.typ info.typ
// info.typ 已被 .gitignore 忽略（含个人信息，不入库）；本文件是字段说明，随仓库提交。
// 注意：各报告程序窗口标题中的学号由 CMake 注入，需单独执行 cmake -B build -DSTUDENT_ID=你的学号。

#let info = (
  classid: "（教学班号）",    // 教学班号
  major: "（专业）",          // 专业
  school: "（院系）",         // 院系
  // 小组成员 6 人：封面表格按数组顺序展示（第一列为姓名/学号标签）
  members: (
    (name: "组员一姓名", stdid: "组员一学号"),  // 姓名 + 学号（示例，请替换）
    (name: "", stdid: ""),
    (name: "", stdid: ""),
    (name: "", stdid: ""),
    (name: "", stdid: ""),
    (name: "", stdid: ""),
  ),
)
