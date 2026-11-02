# Recording guide: Checkpoint 4 (short video) and Checkpoint 5 (long video)

Both videos are due **Mon, Nov 2, 11:59 PM Mountain**. Both also go in the Teams
Demos channel by **Tue, Nov 3, 3:00 PM Mountain**.

The Checkpoint 3 feedback asked for three things these videos must fix: show a person
doing the task in the Quest 2, show the trial flow and logging, and **show your face**.

## Setup (do this once)

You need your screen and your webcam in one video. OBS Studio is the simplest free way:

1. Put on the headset, start **Quest Link**, and confirm the Link home screen works.
2. In Unity, open `Assets/PhysicsSandbox/Scenes/Sandbox.unity`. Press **Play**. The Game
   view mirrors the headset. Link must be on before you press Play, or the keyboard
   simulator starts instead.
3. In OBS, add a **Window Capture** of the Unity editor (crop it to the Game view), and a
   **Video Capture Device** for your webcam, as a corner picture-in-picture. Frame the
   webcam so your face is visible and, once the headset is on, the headset and your hands
   are too. That is the shot that shows "a person doing the task in the Quest 2."
4. Record a 20-second test. Check the audio and that the Game view is not cropped.

The Game view shows an orange **EXPERIMENTER VIEW** strip during experiment mode. It is
drawn on the desktop only, so it appears in the video but not in the headset. It tells the
viewer which condition is running.

## Do this first: a 5-minute headset check

This is the first time the project has run on a real headset. Before recording, check:

- [ ] You can grab a block with the **grip**, near and from a distance.
- [ ] In free play, **A** toggles the cue, **B** the line, **X** resets the blocks.
- [ ] Heavy blocks visibly lag and hang below your hand with the cue on.
- [ ] Press **Start experiment mode**: pointing and squeezing the grip (or pulling the
      trigger) presses buttons.
- [ ] Set the ID to **90** and start. You are moved to each station and can reach the blocks.
- [ ] The panel text is readable and comfortable to look at.

If anything is off (reach, panel distance, text size), tell Claude what you saw; these are
quick changes.

## Checkpoint 4: short video (5 minutes or less, no slides, no experiment)

Show it running, in free play, with your face in frame. This is what you would show a visitor.

| Time | Do this | Say something like |
|---|---|---|
| 0:00 | Face on camera, headset in hand | "I'm Sven. This is Physics You Can Feel, running live on a Quest 2 over Link." |
| 0:20 | Headset on, look across the bench | "Since Checkpoint 3 I added the friction ramp and an experiment mode, and it now runs in the headset instead of a simulator." |
| 0:40 | Grab same-size pine, then steel. Cue **ON** (panel shows it) | "Same size, very different mass. With the weight cue on, steel hangs below my hand and lags. Pine stays with it." |
| 1:30 | Press **A**: cue **OFF**. Lift pine and steel again | "This is the other condition. Cue off: everything tracks my hand one to one, and they feel the same." |
| 2:10 | Press **A**: cue **ON**. Press **B** to hide the orange line | "A participant sees this: the cue, without the debug line." |
| 2:30 | Lift the 1 kg blocks | "Same mass, different size. They get the same cue, because it follows mass. Size shows density." |
| 3:00 | Put pine and steel in the tank | "Less dense than water floats, denser sinks, at the depth Archimedes predicts." |
| 3:30 | Same-size ice, then aluminum, on the ramp's green line | "New: the ramp. Ice runs all the way down. Aluminum, though it's heavier, barely moves. Friction decides, not weight." |
| 4:10 | Aluminum then ice on the lever | "The lever balances when mass times distance match on both sides. The panel shows the torques." |
| 4:40 | Press **Start experiment mode** and stop at the ID screen | "Experiment mode runs this as a study. That's in my long video. Thanks." |

Requirements check: running prototype, features and interaction, **both conditions (cue on and
off)**, under 5 minutes, no slides, no full experiment.

## Checkpoint 5: long video (two slides, then the experiment; under 15 to 20 minutes)

### Part 1: two slides (about 2 minutes), face on camera

Open the slide deck, full screen. Speaker notes on each slide are a full script.

### Part 2: run the experiment as a participant

Use a **pilot ID, 90 to 99**. Those runs are marked `pilot` in the data, so your demo can't be
confused with study data. A full session takes roughly 12 to 15 minutes when played by a person, so
it fits. If you run long, finishing one complete block and then pressing **F12** is acceptable; the
assignment allows "one complete condition followed by the data it saved."

1. **Participant ID.** "I enter a participant ID. The order comes from the ID. Four orders cross cue
   order with which scenario set goes with the cue, so twelve participants fill each order three
   times." Change the ID with the buttons to show the order changing, then settle on your pilot ID.
2. **Training.** Point and press a button, then place the gray block on the green pad.
3. **Block 1.** Say which condition is running (the strip shows it), then do the quiz, the four tasks
   (compare, tank, ramp, lever), the six lifting pairs, the quiz again, and the five questions. Say what
   each records: "Each answer is logged with its response time."
4. **Block 2.** The other condition and the other scenario set. Note that the strip changed. Talk less
   here, since the structure repeats.
5. **Summary screen.** It shows the scores and the saved file names.

### Part 3: show the data (required)

Take off the headset, and with your face in frame, show the files. They are in
`PhysicsSandbox/StudyData/`, named `P90_<date-time>_responses.csv`, `_interactions.csv`, `_session.txt`.

1. Open `responses.csv`. "One row per answer: condition, set, phase, choice, correct or not, response
   time. Task rows add measures, like the lever's final arm against the target."
2. Open `interactions.csv`. "One row per grab: mass, hold time, how far the hand rose, the peak offset,
   gain and lag."
3. Open `session.txt`. "The order, the device, and the frame rate per block."
4. Run this and show the output: "This turns the log into my dependent variables."

```bash
python analysis/summarize.py
```

## After recording

1. Upload each video to YouTube as **Unlisted**. Any other kind of link loses 20 points.
2. Open each link in a private/incognito window to confirm it plays.
3. Paste the links into `readme.txt` (Checkpoint 4 and Checkpoint 5 sections), and submit that
   readme in **each** of the two Canvas assignments.
4. **Online:** post both videos in the Teams **Demos** channel by Tue, Nov 3, 3:00 PM.
5. **Participation:** on both Tue, Nov 3 and Thu, Nov 5, answer questions on your post and comment on
   at least one other group's demo. Missing a day costs 5 points.
