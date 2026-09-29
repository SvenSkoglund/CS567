# Agentic-AI Development Log

Physics You Can Feel — VR Pseudo-Haptic Sandbox (CS567_SVEN)

Purpose: this log is the raw material for the agentic-AI development report in the
final paper. Per professor feedback, it must be kept from day one and cannot be
reconstructed at the end. Record every task given to the AI coding agent, what it
produced, whether it built, and what required human correction.

How to use: add one entry per agent task (or tight cluster of tasks). Keep it
factual and short. Favor many small honest entries over a few polished ones.
Copy the template block for each new entry.

Columns / fields per entry:
- Date/time
- Task given to the agent (the actual instruction or goal)
- What the agent produced (files/scripts changed, approach taken)
- Built? (yes / no / N/A) and how verified (compiler, Unity build, on-device, test)
- Human correction needed (what you had to fix or redirect, or "none")
- Notes (surprises, dead ends, workflow observations for the report)

---

## Entries

### 2026-09-28 — Checkpoint 2 paper revision (documentation, not Unity yet)
- Task given: read the existing proposal and professor feedback; correct the
  mass-vs-density cue error, fix three misused citations, add three verified
  references, add a Methodology/Study Design section, and reframe evaluation
  around the required human study.
- Produced: edits to main.tex (abstract, approach, system vision, evaluation ->
  prototype validation + new methodology section, deliverables, conclusion);
  edits to related-work.tex (pseudo-haptics grounding, Georgiou/Makransky/Kim
  corrections); three new BibTeX entries (Dominjon 2005, Rietzler 2018,
  Hestenes 1992) verified via Crossref; this log; README update.
- Built? Yes — pdflatex + bibtex, 3 passes, no undefined citations, no errors,
  5 pages. New refs confirmed present in main.bbl; Kim citation confirmed removed.
- Human correction needed: author to review prose voice and confirm study design
  wording matches intent before submission.
- Notes: no Unity code exists yet; code/prototype video not yet required this
  checkpoint. First code-generation agent tasks should be logged below as they
  happen.

---

### 2026-09-29 — Checkpoint 2 methodology restructure (updated instructions)
- Task given: apply professor's updated checkpoint page and Teams message —
  rewrite methodology in past tense as if the study was run, use the standard
  Participants/Apparatus/Procedure/Design structure, confirm the 12-participant
  repeated-measures minimum, and review Related Work as synthesis.
- Produced: rewrote the Methodology section into the four standard subsections in
  past tense; labeled the Prototype Validation section and fixed a cross-ref;
  aligned one Related Work line with the mass-not-density cue; set the deliverable
  participant count to twelve. Refreshed CS567-Checkpoint2-Skoglund.pdf.
- Built? Yes — pdflatex + bibtex, 3 passes, no undefined refs, no errors, 5 pages.
- Human correction needed: caught and fixed a broken cross-reference the agent
  first introduced (pointed at an unlabeled section).
- Notes: Related Work already reads as thematic synthesis rather than a summary
  list, so it was left largely intact per the CHI writing guides.

---

## Template (copy for each new entry)

### YYYY-MM-DD HH:MM — <short task title>
- Task given:
- Produced:
- Built? (yes/no/N/A):  how verified:
- Human correction needed:
- Notes:
