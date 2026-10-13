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

### 2026-10-12 18:05 — Checkpoint 3 planning and environment check
- Task given: read the repo, get up to speed, and plan the first prototype for the
  Checkpoint 3 video due the same night.
- Produced: no files. The agent read the paper, logs and mockups, then inspected the
  machine: Unity 6000.4.7f1 with Windows build support only, no Android module, no
  Meta Quest Link, no record of a headset ever connected. It proposed a time-boxed
  plan and asked one question (how the prototype should run for the video).
- Built? N/A.
- Human correction needed: chose "simulator first, Quest Link in parallel" over a
  standalone APK. Asked why Android was needed at all; the agent clarified it is only
  needed for a standalone headset build, not for Link.
- Notes: the environment check changed the plan. The paper says Quest 2 standalone,
  but that path was not feasible in one evening on this machine.

---

### 2026-10-12 18:20 — Unity project creation
- Task given: create the Unity project.
- Produced: the agent tried to create an empty project from the command line in
  batch mode.
- Built? No — the author rejected the command.
- Human correction needed: the author preferred to create the project in Unity Hub
  and asked which options to pick. The agent recommended the VR template on
  6000.4.7f1 with Unity Cloud and Unity Version Control unchecked. The author created
  `PhysicsSandbox/` that way.
- Notes: the VR template was the better route than the agent's first attempt. It
  shipped XR Interaction Toolkit 3.4.1, OpenXR, the Input System and a working rig,
  which the empty project would have needed added by hand.

---

### 2026-10-12 18:30 — Editor bridge so the agent can drive an open editor
- Task given: set up the recommended project structure (open-ended).
- Produced: `Assets/PhysicsSandbox/AgentBridge/Bridge.cs`, an editor-only script in
  its own assembly. It polls a command file and lets the agent refresh and compile,
  call static methods, enter and leave Play mode, and render screenshots to PNG, all
  while the author's editor stays open. Also a Unity `.gitignore` and a copy of the
  XR Interaction Simulator sample.
- Built? Yes — compiled on first import; heartbeat file confirmed it was running.
- Human correction needed: none.
- Notes: this was the agent's own idea, made because batch mode cannot run against a
  project that is open in the editor. It is what made the rest of the session
  self-verifying: the agent could look at its own screenshots and read back numbers
  from Play mode instead of asking the author to check each change.

---

### 2026-10-12 18:35 — Core sandbox: materials, weight cue, lever, tank, panel
- Task given: build the first prototype scene per the plan.
- Produced (all under `Assets/PhysicsSandbox/`):
  `Runtime/MaterialLibrary.cs` (four materials, closed-form formulas),
  `Runtime/PseudoHaptics.cs` (mass to gain and lag mapping, cue on/off switch),
  `Runtime/PseudoHapticGrabTransformer.cs` (the cue, as a subclass of the toolkit's
  grab transformer), `Runtime/PseudoHapticGhost.cs` (wireframe showing the real hand
  position), `Runtime/PhysicsBlock.cs`, `Runtime/WaterVolume.cs` (buoyancy),
  `Runtime/Lever.cs`, `Runtime/SandboxHud.cs` (readout panel and demo controls),
  `Runtime/SimulatorAutoLoader.cs`, and `Editor/SandboxSceneBuilder.cs`, which
  generates `Scenes/Sandbox.unity` from code.
- Built? Yes — all three assemblies compiled with no errors on the first attempt.
  Verified by rendering the scene to PNG and by entering Play mode with no exceptions.
- Human correction needed: none on the code. Fixes the agent made after looking at
  its own screenshots: labels were half inside the blocks (text pivot), shaded faces
  were black (the template scene has no ambient lighting), metals rendered black (no
  reflections, so metallic was lowered).
- Notes: the agent read the toolkit's source in the package cache before writing the
  transformer, to learn which update phases call it and that the base class computes
  the one-to-one pose. Generating the scene from code rather than hand-editing made
  every layout change a one-line edit and a rebuild.

---

### 2026-10-12 18:42 — Scripted self-test of cue, buoyancy and lever
- Task given: (agent-initiated) verify behavior against the paper's validation
  criteria without a person in the loop.
- Produced: `Editor/SandboxSelfTest.cs` with probes callable through the bridge:
  take over the right hand, move or glide it by a known amount, grab and release,
  place blocks, and report positions, cue parameters, lever torques and submerged
  fractions.
- Built? Yes. Measured in Play mode:
  steel 7.85 kg, hand lifted 0.300 m, block rose 0.118 m (gain 0.393, as predicted);
  pine 0.5 kg, same lift, block rose 0.263 m (gain 0.878);
  switching the cue off mid-hold gave one-to-one tracking with no jump;
  buoyancy: pine 0.500, ice 0.917, steel 1.000 submerged, matching density ratios;
  lever: 2.7 kg at 6.9 cm against 0.917 kg at 20.4 cm read 1.88 vs 1.80 N·m and
  settled at -0.2 degrees, reported as balanced.
- Human correction needed: none, but three agent mistakes were caught by the test
  itself: (1) the first version moved the whole rig to move the hand, and the rig's
  gravity pulled it back down, giving nonsense readings; (2) the second version
  destroyed the simulator, which deactivated the controllers; (3) the lever reported
  zero load because it used collision callbacks, which stop once bodies fall asleep.
  It was changed to an overlap query.
- Notes: two design changes came out of testing. A plain gain let a heavy block
  drift arbitrarily far from the hand on a long carry, so the shortfall now levels
  off (15 cm at most for the heaviest block). And loads resting on top of a pivot are
  an unstable equilibrium, so the hinge has a light centering spring and behaves like
  a beam balance. Both differ from the paper's wording and need a sentence there.

---

### 2026-10-12 18:48 — Unit tests for cue monotonicity
- Task given: (agent-initiated) cover the paper's "cue must be monotonic" criterion.
- Produced: `Tests/EditMode/PseudoHapticMappingTests.cs` (15 cases: gain never rises
  with mass, lag never falls, displayed lift never rises with mass, offset stays
  bounded, equal-mass blocks shrink with density, closed-form values) and
  `Editor/SandboxTestRunner.cs` to run them through the bridge.
- Built? Yes — 15 passed, 0 failed.
- Human correction needed: none.
- Notes: a long shell command containing the test file failed to parse before
  anything ran; the agent rewrote the files with its file tool instead.

---

### 2026-10-12 19:05 — Scripted demo sequence for desktop recording
- Task given: (agent-proposed, started when the VR path became uncertain) the loaner
  Quest 2 had a flat battery and an unknown account, so a recording with the
  keyboard-and-mouse simulator became the likely route.
- Produced: `Runtime/DemoDirector.cs`. Pressing N in simulator mode makes the right
  hand lift each same-size block 30 cm with the cue on, repeat the steel block with
  the cue off, lift two same-mass blocks, carry three blocks into the tank, and
  balance the lever, with captions and a live readout. It drives the real interactor
  and grab pipeline rather than animating the blocks. Added to the scene builder and
  to the panel's key hints.
- Built? Yes — compiled first time. Verified by running it in Play mode and reading
  captured frames: tank ended at pine 50%, ice 92%, steel sunk; lever ended at
  2.65 vs 2.67 N·m, tilt -0.25 degrees, balanced.
- Human correction needed: none yet; the author has not watched it.
- Notes: because the cue puts the block somewhere other than the hand, placing a
  block precisely needed a feedback loop on the block's displayed position, the same
  correction a person makes by eye. On the first run the demo started a second time
  after finishing; the cause was not identified (possibly a stray N key press). The
  end state was reworked so N after the end hands control back, and a second full
  run ended cleanly.

---

## Template (copy for each new entry)

### YYYY-MM-DD HH:MM — <short task title>
- Task given:
- Produced:
- Built? (yes/no/N/A):  how verified:
- Human correction needed:
- Notes:
