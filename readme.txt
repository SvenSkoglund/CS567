CS 567 — Physics You Can Feel: A Pseudo-Haptic VR Sandbox
Sven Skoglund (group CS567_SVEN)
Checkpoints 4 and 5 — Short demo video and long experiment video

====================================================================
REQUIRED LINKS  (confirm each opens in a private/incognito tab)
====================================================================

Checkpoint 4, short demo video (5 min or less, YouTube unlisted):
  PASTE-SHORT-VIDEO-LINK-HERE

Checkpoint 5, long video, slides then experiment (YouTube unlisted):
  PASTE-LONG-VIDEO-LINK-HERE

====================================================================
OTHER LINKS
====================================================================

GitHub repository:
  https://github.com/SvenSkoglund/CS567

Checkpoint 3 prototype video (YouTube unlisted):
  https://youtu.be/BbeIqTUpn2Y

Checkpoint 2 status video (YouTube unlisted):
  https://youtu.be/ywl5PZBO41Y

Paper source (past tense, updated for this checkpoint):
  CS567-Proposal-Template-LaTeX/main.tex

====================================================================
WHAT THE PROTOTYPE DOES
====================================================================
One Unity scene (PhysicsSandbox/Assets/PhysicsSandbox/Scenes/Sandbox.unity) with a
workbench holding:

- A same-mass block set (pine, ice, aluminum, steel, 1 kg each). Size alone shows
  density: the steel cube is 5 cm, the pine cube 12.6 cm.
- A same-size block set (1 litre each, 0.5 kg to 7.85 kg). The pseudo-haptic weight
  cue tells them apart: when lifted, a heavier block rises less than the hand
  (a lower control-to-display gain) and lags more. The cue tracks mass, not
  density.
- A lever with centimetre marks. The panel shows the torque on each side and
  reports when the plank balances.
- A water tank. Blocks float or sink by density, at the depth Archimedes predicts.
- A readout panel with the held block's properties, the live cue parameters, and
  the lever, tank and ramp outcomes.
- A ramp on a side table (new). Friction decides what slides: at 12.5 degrees ice
  runs to the bottom, while the heavier aluminum and steel hold when set down square.
- Experiment mode (new). It runs the study as a participant sees it: enter a
  participant ID, the counterbalanced condition order follows from it, then
  training and two blocks of pre-test, four scenes, six heaviness trials, post-test
  and questionnaire. Everything is logged to file.

Checked against closed-form physics in Play mode: displayed lift follows the gain
mapping (a 30 cm hand lift leaves steel 12.7 cm below the hand and pine 2.5 cm),
submerged fraction equals density ratio (pine 0.500, ice 0.917), the lever balances
when the torques match, and ramp acceleration stays within 3.1% of the closed form at
three slopes (validation/ramp-check-2026-11-01.txt). 28 unit tests cover cue
monotonicity and the study design. A scripted participant completed full sessions
through the real grab pipeline (validation/sample-selftest/ holds one, clearly marked
run_type=selftest).

Not yet built: a further concept (pulley or launcher). Hand tracking and a standalone
Quest build were set aside; the prototype runs from the Unity editor through Quest
Link, or with the keyboard-and-mouse XR simulator.

====================================================================
RUNNING THE PROTOTYPE
====================================================================
1. Open the PhysicsSandbox folder in Unity 6000.4.7f1 (Unity Hub -> Add).
2. Open Assets/PhysicsSandbox/Scenes/Sandbox.unity and press Play.
3. With a headset connected through Quest Link the scene runs in VR. Without one,
   the XR Interaction Simulator starts automatically.

Controls:
  Grab a block            grip button            (simulator: hold G)
  Toggle the weight cue   right A button         (keyboard: P)
  Toggle the offset line  right B button         (keyboard: O)
  Reset the blocks        left X button          (keyboard: Backspace)
  Scripted demo           simulator only         (keyboard: N to start or stop)
  Simulator movement      W A S D, Q/E down/up, right mouse button to look,
                          Tab to switch between head and controllers

To regenerate the scene from code: menu CS567 -> Build Sandbox Scene.

Experiment mode: point at "Start experiment mode" and squeeze the grip or pull the
trigger (keyboard: F1). Enter the participant ID with the buttons or the number keys.
IDs 1-12 are study participants; 90-99 are logged as pilot runs. Answer by pointing
and squeezing (keyboard: F1 to F7). Experimenter keys: F10 skips a task, F12 aborts.
Data goes to PhysicsSandbox/StudyData/ (not committed).
  python analysis/summarize.py     prints the dependent variables from the logs

====================================================================
FILES
====================================================================
  readme.txt                                  this file
  AGENT_DEV_LOG.md                            agentic-AI development log
  analysis/summarize.py                       logs to dependent variables
  validation/                                 ramp check, sample self-test session
  video/checkpoint4-5-recording-guide.md      shot lists and recording setup
  PhysicsSandbox/                             Unity project
  PhysicsSandbox/Assets/PhysicsSandbox/       our code, scene, materials and tests
    Runtime/                                  cue, blocks, lever, buoyancy, ramp, panel,
                                              experiment mode, study design, logging
    Editor/                                   scene builder, self-test, test runner
    AgentBridge/                              lets the AI coding agent drive the editor
    Tests/EditMode/                           unit tests
  CS567-Proposal-Template-LaTeX/              paper source and PDFs

Building the paper: see CS567-Proposal-Template-LaTeX/README.txt.
