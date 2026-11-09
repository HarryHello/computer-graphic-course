#let serif = ("Libertinus Serif", "Noto Serif CJK SC", "Songti SC")
#let sans = ("Noto Sans CJK SC", "PingFang SC", "Heiti SC")
#let ink = rgb("20252b")
#let muted = rgb("626b73")
#let rule = rgb("cbd0d3")

#let paper(title: "", subtitle: "", course: "", authors: "", date: "", summary: [], body) = {
  set document(title: title)
  set page(
    paper: "a4",
    margin: (x: 25mm, top: 23mm, bottom: 22mm),
    numbering: "1",
    number-align: center,
    footer: context {
      set text(font: sans, size: 8pt, fill: muted)
      align(center, counter(page).display())
    },
  )
  set text(font: serif, lang: "zh", size: 10.5pt, fill: ink)
  set par(justify: true, leading: 0.7em, spacing: 0.85em)
  set heading(numbering: "1.1")

  show heading.where(level: 1): it => {
    set text(font: sans, size: 13pt, weight: "bold")
    block(above: 1.35em, below: 0.65em, it)
  }
  show heading.where(level: 2): it => {
    set text(font: sans, size: 11pt, weight: "bold")
    block(above: 0.95em, below: 0.45em, it)
  }
  show figure.caption: it => {
    set text(font: sans, size: 9pt, fill: muted)
    it
  }

  text(font: sans, size: 9pt, weight: "bold", fill: muted, course)
  v(10pt)
  text(font: serif, size: 23pt, weight: "bold", title)
  if subtitle != "" {
    v(5pt)
    text(font: sans, size: 12pt, fill: muted, subtitle)
  }
  v(11pt)
  text(font: sans, size: 9.5pt, authors)
  if date != "" {
    h(1em)
    text(font: sans, size: 9pt, fill: muted, date)
  }
  v(11pt)
  line(length: 100%, stroke: 0.65pt + ink)
  v(9pt)
  text(font: sans, size: 9pt, weight: "bold", [摘　要])
  v(5pt)
  block(width: 100%, text(size: 10pt, summary))
  v(6pt)
  line(length: 100%, stroke: 0.45pt + rule)
  v(5pt)
  body
}

#let member-table(members) = {
  set text(font: sans, size: 9pt)
  table(
    columns: (1fr, 1.4fr, 3.8fr, 0.9fr),
    align: (left, left, left, right),
    inset: (x: 5pt, y: 7pt),
    stroke: none,
    table.hline(stroke: 0.8pt + ink),
    table.header(
      [*姓名*], [*学号*], [*负责部分*], [*贡献度*],
    ),
    table.hline(stroke: 0.45pt + rule),
    ..members.map(member => (
      member.name, member.stdid, member.role, member.contribution,
    )).flatten(),
    table.hline(stroke: 0.8pt + ink),
  )
}
