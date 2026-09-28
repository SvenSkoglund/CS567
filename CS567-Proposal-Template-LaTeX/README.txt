CS 567 Project Proposal - LaTeX template
========================================

Same template as CS567-Proposal-Template.docx, in LaTeX. Use whichever you
prefer; the checkpoints and the final paper use this same acmart format, so
starting here saves you a conversion later.

Overleaf (recommended - Overleaf Professional is free for CSU students):
  1. New Project -> Upload Project -> this zip.
  2. Menu -> Compiler: pdfLaTeX. Main document: main.tex.
  3. Recompile.

Local:
  latexmk -pdf main.tex          (or: pdflatex main; bibtex main; pdflatex main x2)
  Needs the acmart class - TeX Live 2020 or newer has it.

Files:
  main.tex        the proposal - fill this in
  references.bib  your bibliography, with two sample entries

Before you submit: delete every \guide{...} block and the checklist section at
the end, export to PDF, and upload the PDF (not the .tex) to Canvas.
