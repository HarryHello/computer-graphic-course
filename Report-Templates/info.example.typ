// 个人信息配置模板（所有报告共用）—— 复制本文件为 info.typ 并填写真实信息：
//   cp info.example.typ info.typ
// info.typ 已被 .gitignore 忽略（含个人信息，不入库）；本文件是字段说明，随仓库提交。
// 注意：各报告程序窗口标题中的学号由 CMake 注入，需单独执行 cmake -B build -DSTUDENT_ID=你的学号。

#let info = (
  classid: "（教学班号）",    // 教学班号
  major: "（专业）",          // 专业
  school: "（院系）",         // 院系
  // 成员列表：填 1 人为个人报告；小组作业依次追加，封面表格随人数自适应宽度
  members: (
    (name: "姓名", stdid: "学号"),  // 姓名 + 学号（示例，请替换）
    // (name: "", stdid: ""),       // 小组作业时取消注释并依次填写
  ),
)
