# Checkpoint 3 voice-over script

For `checkpoint3-demo-silent.mp4` (5:24, 1080p, no audio).

Each section starts when the screen changes to what the heading describes. The
word counts leave a few seconds of slack at a normal speaking pace, so there is no
need to rush. If you fall behind, drop the sentences in *italics*.

---

## 0:00 – 0:22 · Title card

Hi, I'm Sven Skoglund, and this is the Checkpoint 3 prototype video for my CS 567
project, Physics You Can Feel. It's a VR sandbox that uses pseudo-haptic cues, so
virtual objects feel like they have weight even though the controller never gets
heavier.

## 0:22 – 1:00 · "Where the project stands"

Quick status first. Checkpoints one and two covered the proposal, the related
work, and the study design. This checkpoint is the first working prototype. It's
built in Unity 6 with the XR Interaction Toolkit and OpenXR. What you'll see is
running live in the Unity editor, with Unity's XR simulator standing in for the
headset and controllers. Getting the same scene onto the Quest 2 is the next step.
*I built it with an AI coding agent, and I'll come back to that at the end.*

## 1:00 – 1:30 · Camera pans across the workbench

Here's the sandbox. On the left is a same-mass set: one kilogram each of pine,
ice, aluminum and steel, so the denser materials are smaller. Next to it is a
same-size set, one litre each, from half a kilogram up to almost eight. Behind
them is a lever, then a water tank, and a panel that shows what's being held and
what the physics is doing.

## 1:30 – 2:07 · Four lifts, cue on (pine, ice, aluminum, steel)

This is the main idea. A scripted hand lifts each same-size block by thirty
centimetres, using the same grab code a person would. The orange wireframe is
where the hand really is, and the solid block is what you see. Pine nearly keeps
up. Ice and aluminum fall a bit behind. Steel rises the least and lags the most.
Your eyes see the block trailing your hand, and that reads as weight.

## 2:07 – 2:19 · Steel again, cue off

With the cue switched off, the same steel block follows the hand exactly. That's
the control condition in my study.

## 2:19 – 2:41 · Same-mass set: pine, then steel

Now the same-mass set. Pine and steel are very different sizes, but both are one
kilogram, so they get the same cue. The cue follows mass, not density. Density
shows up as size instead.

## 2:41 – 3:10 · Water tank

Density also decides what floats. Each one-kilogram block goes into the tank. Pine
floats half under water, ice floats about ninety-two percent under, and steel
sinks. Those fractions match each material's density divided by the density of
water.

## 3:10 – 3:40 · Lever

Last, the lever. Two point seven kilograms of aluminum goes ten centimetres left
of the pivot, and the plank tips. Then about nine tenths of a kilogram of ice goes
at twenty-nine centimetres on the right. Mass times distance now matches on both
sides, and the panel reports that it's balanced.

## 3:40 – 4:16 · "Checked against closed-form physics"

I checked each of these against the textbook formula. For steel the gain is zero
point three nine, so a five centimetre hand lift moves the block two centimetres.
With the cue off, tracking is one to one. The floating depths match the density
ratios to three decimal places. The lever balanced with the two torques within
about one percent. *There are also fifteen unit tests that make sure a heavier
object never gets a weaker cue.*

## 4:16 – 4:54 · "How it was built: agentic AI"

The agentic AI part of this project is how it was built. An AI coding agent,
Claude Code, wrote the C sharp scripts and generates the whole scene from code. It
drives my open Unity editor through a small bridge, so it can compile, enter Play
mode, take screenshots and read numbers back. That let it test its own work, and
it caught three of its own mistakes. I decided the project setup, where to run it,
and what to cut. *Every task is in the development log, and the agent captured
this recording too.*

## 4:54 – 5:24 · "Next steps"

Next, I'll run this on the Quest 2, first through Link and then as a standalone
build at seventy-two hertz. I'll add the ramp for friction and a cue on the lever
that depends on its load, tune the cue strength with pilot users, and add logging.
After that comes the study: cue on against cue off, with twelve participants.
Thanks for watching.
