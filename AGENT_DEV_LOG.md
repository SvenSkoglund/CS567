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

### 2026-10-12 19:45 — Recording the prototype video from the editor
- Task given: "record the video demonstration, and give me a basic script to record
  voice over it." The loaner headset could not be used that night (a former
  student's account was still on it), so the recording is of the simulator.
- Produced: a narrated mode in `Runtime/DemoDirector.cs` (title, status, validation,
  workflow and next-steps cards, a camera pan across the bench, longer holds),
  `Runtime/GameViewRecorder.cs` (pipes Game view frames to ffmpeg with game time
  locked to 30 fps), and `Editor/SandboxRecorder.cs` (sets the Game view to 1080p,
  starts the demo, stops when it ends, writes the segment timeline). Output:
  `video/checkpoint3-demo-silent.mp4`, 5:24 at 1920x1080, and
  `video/voiceover-script.md` timed against the recorded timeline.
- Built? Yes — compiled first time. A 30-second test take confirmed size, colour and
  orientation before the full take. The agent reviewed the result by extracting
  frames into a contact sheet.
- Human correction needed: none on the code. The author still has to record the
  voice-over and check the video end to end.
- Notes: the first full take had the idle left controller floating mid-shot; the
  agent hid it and re-recorded. Capturing inside Unity rather than recording the
  screen meant nothing from the desktop could end up in the video. Capture ran
  faster than real time, so each full take cost about two minutes.

---

### 2026-10-12 20:05 — Author review of the first take: cue direction, overlay, pacing
- Task given: after watching the first recording the author asked for three changes.
  (1) The offset stayed sideways after a sideways move, "as if there is some
  sideways gravity"; the block should end level with the hand. (2) Show the offset
  as a single line, not a wireframe cube. (3) Shorter, simpler title cards and a
  simpler script; the cards were on screen too long.
- Produced: `PseudoHapticMapping.DisplayedDisplacement` now scales only upward
  lift, so the block hangs below the hand and sideways or downward motion is
  one-to-one once the lag settles. `PseudoHapticGhost` draws one line. The narrated
  sequence was rewritten: cards cut from 2:44 to 0:57 with three or four short
  bullets each, and the time moved into demonstration (all four blocks with the cue
  off, all four same-mass blocks, four blocks in the tank, a second lever example).
  New take: 5:08. Script rewritten in plain language against the new timeline.
- Built? Yes — 18 unit tests pass (three new ones for sideways and downward
  motion). Measured in Play mode with steel: after a 0.5 m sideways move the offset
  settled to 0.0 cm; a 30 cm lift left it 12.7 cm below the hand; lowering returned
  it to 0.0 cm.
- Human correction needed: this entry is the correction. The agent's cue applied
  the gain in every direction, which passed all of its own numeric checks and was
  still wrong to look at. The author saw it within seconds of watching the video.
- Notes: the agent kept the video just above five minutes because the assignment
  asks for five to ten, and moved the time from cards to demonstration rather than
  cutting the length. The cue now differs from a plain control-to-display gain in
  two ways (vertical only, and levelling off with height); the paper needs to say so.

---

### 2026-11-01 — Checkpoints 4 and 5: ramp, experiment mode, logging, paper
- Task given: implement the Unity and paper changes for the combined checkpoints.
  The author's answers to the agent's questions: Quest 2 works over Link, the author
  performs the videos by hand, online section, build the ramp now. The author also
  passed on professor feedback: write in past tense, show a person doing the task in
  the Quest 2, add trial flow and logging, build the ramp, show your face.
- Produced: `Runtime/Ramp.cs` and a side table; `Runtime/StudyDesign.cs`
  (counterbalancing, parallel tests, questionnaire, heaviness trials);
  `Runtime/ExperimentSession.cs` (ID entry, training, blocks, desktop-only experimenter
  strip); `Runtime/ExperimentPanel.cs` and `PanelButton.cs` (point-and-squeeze answers);
  `Runtime/StudyLog.cs`; `Runtime/ExperimentPilot.cs` (editor-only scripted participant
  and ramp check); `Tests/EditMode/StudyDesignTests.cs`; `analysis/summarize.py`; a past
  tense rewrite of the paper; the two slides; the recording guide.
- Built? Yes. 28 unit tests pass. Scripted participant completed full sessions: 58
  responses, 39 grabs, no errors, pointer reached the button on 63 of 63 presses. Ramp
  acceleration within 3.1% of the closed form across materials and slopes.
  NOT verified: anything on the real headset (the agent had no headset connected, so
  everything ran in the simulator), and the paper's PDF build (no LaTeX on this machine;
  a portable compiler download was blocked by the permission check, so the source only
  passed static checks for references, citations and environments).
- Human correction needed: the agent's plan was redirected by the professor's feedback
  mid-task, and by the author's choice to play the videos by hand, which removed the
  planned scripted video capture.
- Notes: the ramp check found four real defects the agent had introduced or inherited.
  (1) PhysX's default patch friction held blocks at slopes where they should slide;
  enabling improved patch friction fixed it, taking pine at 20 degrees to 1.4% error.
  (2) The ramp code read a scaled unit cube's collider as 1 x 1 x 1. (3) Teleporting a
  sleeping rigidbody does not wake it, which would have left study blocks floating.
  (4) Timing kept counting after a block hit the stop. Also: Unity refused to add an
  editor-assembly MonoBehaviour at run time, so the pilot moved to the runtime assembly
  behind `#if UNITY_EDITOR`; RectTransform positions set through `transform.localPosition`
  did not survive scene saving; `DateTime.Now` jumped six hours within one session, so
  log times now use one clock reading plus a monotonic timer. The first pointer check
  read 36 of 63 because the rig smooths its aim; waiting for it gave 63 of 63.

---

### 2026-11-02 19:45 — Recording setup and headset-only narration prompter
- Task given: "help me get set up to capture the headset video combined with the webcam
  video in the corner", then a question: how will I see the narration script in the sim?
  The author chose a headset-only prompter from the options the agent offered.
- Produced: OBS Studio installed and configured outside the repo (Game view plus webcam
  scene, desktop plus webcam scene, webcam mic, hotkeys). `Runtime/NarrationPrompter.cs`,
  an editor-only cue card panel drawn as an OpenXR composition layer, with
  `video/cues-checkpoint4.txt` and `video/cues-checkpoint5.txt`. Turned off the
  Composition Layers Play mode preview, which would have drawn the panel into the video.
- Built? Yes. A 10-second OBS test recording played back with the composite and no dropped
  frames. In Play mode with the simulator: cards step and clamp at both ends, and with the
  preview off the panel is gone from the Game view while the prompter runs. Not yet checked:
  a full recording over Link. On Link, the panel showed in the headset and Y changed cards.
- Human correction needed: the author pointed out the Game view grows on entering Play
  (the crop was already measured for that layout), and clicked through the Unity layout
  settings and dialogs the agent could not reach.
- Notes: the agent's first hotkey choice, F10, would have skipped experiment tasks; a search
  of the input code caught it before use, so recording moved to F8 and F9. The first real
  headset run exposed a bug in the agent's earlier code: `SimulatorAutoLoader` tested
  `XRSettings.isDeviceActive` in `Start`, before the session was running, so the simulator
  started alongside the headset and took over the camera. The author reported the wrong view.
  It now checks for an XR display subsystem. The simulator no longer starts with Link on;
  a later Play run with Link disconnected confirmed the simulator fallback still starts.

---

### 2026-11-02 — Record button on the controller
- Task given: "can we make the record button a button on the headset controllers so i can
  trigger it in scene?"
- Produced: `Runtime/ObsRemote.cs`, an editor-only OBS WebSocket client that reads the password
  from OBS's own config. Holding the left menu button for a second toggles recording, and the
  prompter card shows REC with a timer.
- Built? Yes. Toggled from the editor bridge with OBS: recording started, the card showed
  REC 00:04, recording stopped, and the 5.4-second file played back. The physical button still
  has to be pressed on the real controller.
- Human correction needed: none yet.
- Notes: a patch script failed twice on escaping a newline through the shell, which caused
  extra Play mode restarts while the author was in the headset. Direct file edits fixed it.

---

### 2026-11-02 — Fixes found during headset recording tests
- Task given: the author reported a frozen webcam in OBS, then "A makes the character jump and
  doesn't seem to toggle the weight queue."
- Produced: `SandboxHud` disables the rig's `JumpProvider`, whose default action shares the A
  button with the cue toggle. The prompter card shows the cue state. OBS webcam lowered to
  640x360, plus a session-only watchdog that restarts the source if its picture stops changing.
  The headset mic recorded silence (-91 dB for 37 s), so OBS uses the webcam mic with a 15 dB
  boost, noise suppression and a limiter.
- Built? Yes. The author confirmed in the headset that A toggles the cue and no longer jumps.
  The webcam picture changed again after restarting the source.
- Human correction needed: the author spotted the frozen webcam. The agent had misread the frozen
  frame as a framing problem and told the author to re-aim the camera several times.
- Notes: the jump was hidden until now because the project had only run in the simulator. The
  cause of the silent headset mic was not found. The webcam freeze followed MJPEG decode errors
  in the OBS log; the watchdog's automatic restart has not yet had a real freeze to catch.

---

### 2026-11-02 — Renamed experiment mode to participant mode on screen
- Task given: "can we change it to be called participant mode instead of experiment mode?"
- Produced: new panel title and start button text, plus matching wording in the readme, recording
  guide and cue cards. Class names and code comments kept their old names to limit risk on the
  night of the deadline. The paper, the slides and the Checkpoint 4 video still say experiment mode.
- Built? Yes. Compiled, and a Play mode capture shows the new title and button.
- Human correction needed: none.
- Notes: the scene's stored placeholder title was left alone because the session sets it at start.

---

### 2026-11-02 — Redesign to two groups, and changes from the author's first full run
- Task given: after running participant mode in the headset, the author asked for fewer quiz
  questions and lifting pairs, a "neither" answer, a block color unlike any material, and a
  heavy block that falls quickly instead of lagging downward. The author then decided that doing
  both conditions was too convoluted and moved the study to one cue setting per participant.
- Produced: session flow of quiz, four lifting pairs, four tasks, quiz, five ratings, with odd
  IDs cue on and even IDs cue off. Purple study blocks, a "They felt the same" answer, near-zero
  lag when falling, and a more forgiving training pad. Updated tests, `analysis/summarize.py`,
  cue cards, guide, readme, slides, and the paper's methodology, with a note on the change.
- Built? Yes. 28 unit tests pass. The scripted participant finished one session in each group
  through the real pipeline: 24 responses, 16 grabs, 28 of 28 button presses, no errors. A timed
  check in the simulator showed a heavy block arriving at once after a drop and still catching up
  after a lift. The paper was not compiled, because no LaTeX install was found on this PC.
- Human correction needed: the design changes came from the author's own run. The agent had
  also proposed material looks for the lifting pairs, which implied impossible densities, and
  the author's choice of one unrelated color resolved that.
- Notes: found while testing: the summary screen and session file still hard-coded quiz scores
  as out of 6. Twelve participants may no longer be enough for a two-group study; the course's
  minimum for this design is not confirmed. Set B content remains in the code, unused.

---

### 2026-11-02 — Stations no longer pull the participant toward the bench
- Task given: the author reported being moved closer to the desk at every task, until stepping back
  had taken them to the back wall of the room.
- Produced: `ExperimentSession.Stage` now sets the distance from the bench only at the first
  station, then slides sideways by the distance between stations.
- Built? Yes. Unit tests and a scripted session pass. Comfort in the headset is for the author to confirm.
- Human correction needed: found only by a person walking in a real room; the scripted participant
  and the simulator could not show it.
- Notes: the first real headset run of a full session surfaced this; the comparison the author
  remembered as pine and steel was the first task, 1 kg pine and aluminum, per the session log.

---

## Template (copy for each new entry)

### YYYY-MM-DD HH:MM — <short task title>
- Task given:
- Produced:
- Built? (yes/no/N/A):  how verified:
- Human correction needed:
- Notes:
