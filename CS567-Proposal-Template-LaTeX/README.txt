Physics You Can Feel — LaTeX source
===================================

LaTeX source for the CS 567 paper "Physics You Can Feel: A Pseudo-Haptic VR
Sandbox for Experiencing Mechanics and Material Properties" (Sven Skoglund,
group CS567_SVEN). Written in the ACM acmart format used for all checkpoints
and the final paper.

Building
--------
Overleaf (recommended; Overleaf Professional is free for CSU students):
  1. New Project -> Upload Project -> this folder (or a zip of it).
  2. Menu -> Compiler: pdfLaTeX. Main document: main.tex.
  3. Recompile.

Local:
  pdflatex main.tex && bibtex main && pdflatex main.tex && pdflatex main.tex
  (or: latexmk -pdf main.tex). Needs the acmart class; TeX Live 2020 or newer.

Files
-----
  main.tex                      the paper (abstract, body, methodology)
  related-work.tex              related work section, \input by main.tex
  references.bib                bibliography; entries verified via source DOIs
  HandDrawnMockup.jpeg          Figure 1, hand-drawn concept sketch
  GeminiEnhancedMockup.jpeg     Figure 2, AI-enhanced render of the sketch
  CS567-Checkpoint1-Skoglund.pdf  exported PDF, Checkpoint 1
  CS567-Checkpoint2-Skoglund.pdf  exported PDF, Checkpoint 2 (current)

Notes
-----
- The current PDF exports are committed for convenience; the authoritative
  source is main.tex.
- The submission link list (videos, Overleaf, GitHub) lives in ../readme.txt.
