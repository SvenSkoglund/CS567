CS 567 — Physics You Can Feel: A Pseudo-Haptic VR Sandbox
Sven Skoglund (group CS567_SVEN)
Checkpoint 2 — Methodology and Study Design

====================================================================
REQUIRED LINKS  (fill in before submitting; confirm each video opens
in a private/incognito tab so you know it is truly accessible)
====================================================================

Status video (3-5 min, YouTube unlisted):
  <PASTE_YOUTUBE_UNLISTED_LINK>

Code/prototype video (3-5 min, YouTube unlisted):
  <NOT YET — no code exists at Checkpoint 2. Add when the prototype runs.>

Overleaf project:
  <PASTE_OVERLEAF_PROJECT_LINK>

GitHub repository:
  <PASTE_GITHUB_REPO_LINK>

Paper PDF (in this submission and in the repo):
  CS567-Proposal-Template-LaTeX/CS567-Checkpoint2-Skoglund.pdf

====================================================================
CHECKPOINT 2 CHANGES SINCE CHECKPOINT 1
====================================================================
- Added Methodology and Study Design section: within-subjects study of the
  pseudo-haptic weight cue (on vs. off), pre/post misconception test in the
  style of the Force Concept Inventory, heaviness manipulation check, presence
  questionnaire, ~12 non-physics participants, class data-collection form
  (no IRB; not for outside publication).
- Corrected the core cue: it tracks MASS (and lever LOAD), not density.
  Density is shown through equal-mass block volume and float/sink, not felt as
  weight. This removes the misconception the earlier draft would have taught.
- Reframed the old "Evaluation Plan" as "Prototype Validation" (closed-form
  physics checks within 5%, monotonic cue, 72 Hz on Quest 2), which now feeds
  the human study rather than replacing it.
- Citations: removed the misread Kim 2022; corrected Georgiou and Makransky
  claims to match what those papers found; added three verified references
  (Dominjon 2005, Rietzler 2018, Hestenes 1992 / Force Concept Inventory).
- Started the agentic-AI development log (AGENT_DEV_LOG.md), per feedback that
  it must be kept from day one.

====================================================================
BUILDING THE PAPER
====================================================================
Overleaf (recommended; Overleaf Professional is free for CSU students):
  1. New Project -> Upload Project -> the LaTeX folder (or its zip).
  2. Menu -> Compiler: pdfLaTeX. Main document: main.tex.
  3. Recompile.

Local:
  cd CS567-Proposal-Template-LaTeX
  pdflatex main.tex && bibtex main && pdflatex main.tex && pdflatex main.tex
  (needs the acmart class; TeX Live 2020 or newer)

Files:
  CS567-Proposal-Template-LaTeX/main.tex          the paper
  CS567-Proposal-Template-LaTeX/related-work.tex  related work section
  CS567-Proposal-Template-LaTeX/references.bib     bibliography (verified)
  AGENT_DEV_LOG.md                                 agentic-AI development log
