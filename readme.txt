CS 567 — Physics You Can Feel: A Pseudo-Haptic VR Sandbox
Sven Skoglund (group CS567_SVEN)
Checkpoint 3 — Prototype video

====================================================================
REQUIRED LINK  (confirm it opens in a private/incognito tab)
====================================================================

Prototype video (5-10 min, YouTube unlisted):
  <PASTE_PROTOTYPE_VIDEO_LINK>

====================================================================
OTHER LINKS
====================================================================

GitHub repository:
  https://github.com/SvenSkoglund/CS567

Checkpoint 2 status video (YouTube unlisted):
  https://youtu.be/ywl5PZBO41Y

Paper PDF (unchanged since Checkpoint 2):
  CS567-Proposal-Template-LaTeX/CS567-Checkpoint2-Skoglund.pdf

====================================================================
WHAT THE PROTOTYPE DOES
====================================================================
One Unity scene (PhysicsSandbox/Assets/PhysicsSandbox/Scenes/Sandbox.unity) with a
workbench holding:

- A same-mass block set (pine, ice, aluminum, steel, 1 kg each). Size alone shows
  density: the steel cube is 5 cm, the pine cube 12.6 cm.
- A same-size block set (1 litre each, 0.5 kg to 7.85 kg). The pseudo-haptic weight
  cue tells them apart: a heavier block follows the hand with a lower
  control-to-display gain and more lag. The cue tracks mass, not density.
- A lever with centimetre marks. The panel shows the torque on each side and
  reports when the plank balances.
- A water tank. Blocks float or sink by density, at the depth Archimedes predicts.
- A readout panel with the held block's properties, the live cue parameters, and
  the lever and tank outcomes.

Checked against closed-form physics in Play mode: displayed lift follows the gain
mapping (steel, gain 0.39: a 5 cm hand lift moves the block 2 cm), submerged
fraction equals density ratio (pine 0.500, ice 0.917), and the lever balances when
the torques match. 15 unit tests cover cue monotonicity.

The video shows the scripted demo: a scripted hand drives the same grab pipeline a
person would, in the Unity editor with the XR simulator. It was captured from the
Game view by Editor/SandboxRecorder.cs.

Not in this checkpoint: the ramp, the pulley, hand tracking, file logging, and a
standalone Quest build. The prototype runs in the Unity editor, either through
Quest Link or with the keyboard-and-mouse XR simulator.

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
  Toggle the hand ghost   right B button         (keyboard: O)
  Reset the blocks        left X button          (keyboard: Backspace)
  Scripted demo           simulator only         (keyboard: N to start or stop)
  Simulator movement      W A S D, Q/E down/up, right mouse button to look,
                          Tab to switch between head and controllers

To regenerate the scene from code: menu CS567 -> Build Sandbox Scene.

====================================================================
FILES
====================================================================
  readme.txt                                  this file
  AGENT_DEV_LOG.md                            agentic-AI development log
  PhysicsSandbox/                             Unity project
  PhysicsSandbox/Assets/PhysicsSandbox/       our code, scene, materials and tests
    Runtime/                                  cue, blocks, lever, buoyancy, panel
    Editor/                                   scene builder, self-test, test runner
    AgentBridge/                              lets the AI coding agent drive the editor
    Tests/EditMode/                           unit tests
  CS567-Proposal-Template-LaTeX/              paper source and PDFs

Building the paper: see CS567-Proposal-Template-LaTeX/README.txt.
