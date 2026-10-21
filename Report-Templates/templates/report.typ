#import "./shared.typ": *

#let report(
  title: "实验报告",
  subtitle: "Lab 0: 环境准备",
  classid: "计科一班",
  major: "计算机科学与技术",
  school: "计算机学院",
  members: (),
  time: "2022~2023 学年第二学期",
  banner: none + "",
  body,
) = {
  set document(title: title)

  show: shared

  let fieldname(name) = [
    #set align(right + horizon)
    #set text(font: fonts.serif)
    #name
  ]

  let fieldvalue(value) = [
    #set align(left + horizon)
    #set text(font: fonts.serif)
    #value
  ]

  align(center)[
    #if (banner == none) {
      v(200pt)
    } else {
      image(banner, height: 200pt, width: 95%, fit: "contain")
    }
    #linebreak()
    #v(50pt, weak: true)
    #block(text(weight: "semibold", 30pt, title))
    #v(50pt, weak: true)
    #block(text(weight: "medium", 24pt, subtitle))

    #v(80pt, weak: true)
    #set text(14.5pt)

    #grid(
      columns: (200pt, 240pt),
      row-gutter: 1em,
      fieldname(text("教学班号：") + h(1em)), fieldvalue(classid),
      fieldname(text("专") + h(2em) + text("业：") + h(1em)), fieldvalue(major),
      fieldname(text("院") + h(2em) + text("系：") + h(1em)), fieldvalue(school),
    )

    // 小组成员名单：第一列为标签，其余各列为组员姓名/学号（members 由 info.typ 提供）
    #if members.len() > 0 {
      v(28pt)
      table(
        columns: (auto,) + (1fr,) * members.len(),
        align: center + horizon,
        inset: (x: 6pt, y: 8pt),
        [*姓 名*], ..members.map(m => m.name),
        [*学 号*], ..members.map(m => m.stdid),
      )
    }

    #v(28pt)
    #set text(13pt)
    #text(time, font: fonts.sans)
    #pagebreak()
  ]

  set align(left + top)
  set par(justify: true, first-line-indent: 0em, leading: line_height)

  body
}

#let title(content: "") = align(center)[
  #set text(26pt, weight: "bold")
  #v(2em)
  #content
  #v(1.625em)
]
