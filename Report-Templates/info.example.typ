// 个人信息配置模板（所有报告共用）—— 复制本文件为 info.typ 并填写真实信息：
//   cp info.example.typ info.typ
// info.typ 已被 .gitignore 忽略（含个人信息，不入库）；本文件是字段说明，随仓库提交。
// 注意：各报告程序窗口标题中的学号由 CMake 注入，需单独执行 cmake -B build -DSTUDENT_ID=你的学号。

#let info = (
  name: "（姓名）",           // 姓名
  stdid: "YOUR STUDENT ID",   // 学号（同时记得改 CMake 的 STUDENT_ID）
  classid: "（教学班号）",    // 教学班号
  major: "（专业）",          // 专业
  school: "（院系）",         // 院系
)
