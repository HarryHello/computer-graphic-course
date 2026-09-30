#let line_height = 1em

#let fonts = (
  serif: ("Times New Roman", "Noto Serif CJK SC", "Songti SC", "簡宋"),
  sans: ("Heiti SC", "Noto Sans SC", "PingFang SC"),
  monospace: ("JetBrains Maple Mono", "Consolas", "Andale Mono", "Menlo"),
)

#let textbf(it) = block(text(font: fonts.sans, weight: "semibold", it))

#let textit(it) = block(text(style: "italic", it))
