using System.Globalization;
using System.Text;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace PhysicsSandbox.Editor
{
    /// <summary>
    /// Play-mode probes the coding agent calls through the AgentBridge to check behavior
    /// without a person in the loop: grab a block with the right-hand interactor, move the
    /// rig (and so the hand) by a known amount, and read back what the cue displayed.
    /// </summary>
    public static class SandboxSelfTest
    {
        static PhysicsBlock FindBlock(string blockName)
        {
            foreach (var block in PhysicsBlock.All)
            {
                if (block.name == blockName)
                    return block;
            }

            return null;
        }

        static NearFarInteractor FindRightInteractor()
        {
            foreach (var interactor in Object.FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None))
            {
                if (interactor.name.Contains("Right") || interactor.transform.parent.name.Contains("Right"))
                    return interactor;
            }

            return null;
        }

        public static string Grab(string blockName)
        {
            var block = FindBlock(blockName);
            if (block == null)
                return "no block named " + blockName;

            var interactor = FindRightInteractor();
            if (interactor == null)
                return "no right NearFarInteractor";

            var grab = block.GetComponent<XRGrabInteractable>();
            grab.interactionManager.SelectEnter((IXRSelectInteractor)interactor, grab);
            return $"grabbed {blockName} with {interactor.name}; selected={grab.isSelected}";
        }

        // Takes over the right hand from the simulator so this class positions it directly.
        public static string ManualHands()
        {
            // The simulator stays alive so its devices keep the controller objects active;
            // only the pose driver is switched off so this class can place the hand.
            var interactor = FindRightInteractor();
            if (interactor == null)
                return "right interactor not active yet";

            var hand = interactor.transform.parent;
            foreach (var driver in hand.GetComponents<TrackedPoseDriver>())
                driver.enabled = false;

            return $"manual control of '{hand.name}' at {hand.position:F3}";
        }

        public static string MoveHand(string position)
        {
            var hand = FindRightInteractor().transform.parent;
            hand.SetPositionAndRotation(ParseVector(position), Quaternion.identity);
            return $"hand at {hand.position:F3}";
        }

        // "dx dy dz seconds": moves the hand at constant speed, like a steady lift.
        public static string GlideHand(string args)
        {
            var parts = args.Trim().Split(' ');
            var delta = ParseVector(args);
            var duration = float.Parse(parts[3], CultureInfo.InvariantCulture);
            var hand = FindRightInteractor().transform.parent;
            var start = hand.position;
            var startTime = Time.time;

            void Step()
            {
                if (!Application.isPlaying || hand == null)
                {
                    EditorApplication.update -= Step;
                    return;
                }

                var t = Mathf.Clamp01((Time.time - startTime) / duration);
                hand.position = start + delta * t;
                if (t >= 1f)
                    EditorApplication.update -= Step;
            }

            EditorApplication.update += Step;
            return $"gliding hand by {delta:F3} over {duration:F2} s";
        }

        // "g down" / "g up": drives the simulator's own key bindings, to exercise the real input path.
        public static string Key(string args)
        {
            var parts = args.Trim().Split(' ');
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return "no keyboard";

            var key = (Key)System.Enum.Parse(typeof(Key), parts[0], true);
            InputSystem.QueueStateEvent(keyboard, parts[1] == "down" ? new KeyboardState(key) : new KeyboardState());
            return $"{key} {parts[1]}";
        }

        public static string Release()
        {
            var count = 0;
            foreach (var block in PhysicsBlock.All)
            {
                var grab = block.GetComponent<XRGrabInteractable>();
                if (!grab.isSelected)
                    continue;

                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
                count++;
            }

            return $"released {count}";
        }

        public static string MoveOrigin(string delta)
        {
            var origin = Object.FindAnyObjectByType<XROrigin>();
            origin.transform.position += ParseVector(delta);
            return "origin at " + origin.transform.position.ToString("F3");
        }

        public static string Place(string args)
        {
            var split = args.LastIndexOf('|');
            var block = FindBlock(args.Substring(0, split));
            if (block == null)
                return "no block named " + args.Substring(0, split);

            var position = ParseVector(args.Substring(split + 1));
            block.Rigidbody.linearVelocity = Vector3.zero;
            block.Rigidbody.angularVelocity = Vector3.zero;
            block.Rigidbody.position = position;
            block.Rigidbody.rotation = Quaternion.identity;
            block.transform.SetPositionAndRotation(position, Quaternion.identity);
            return $"placed {block.name} at {position:F3}";
        }

        public static string SetCue(string state)
        {
            PseudoHapticSettings.CueEnabled = state.Trim() == "on";
            return "cue " + (PseudoHapticSettings.CueEnabled ? "on" : "off");
        }

        public static string ResetBlocks()
        {
            foreach (var block in PhysicsBlock.All)
                block.ResetToHome();
            return "reset";
        }

        public static string Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"cue={(PseudoHapticSettings.CueEnabled ? "on" : "off")} fixedDt={Time.fixedDeltaTime:F4} fps={1f / Time.smoothDeltaTime:F0}");

            foreach (var block in PhysicsBlock.All)
            {
                var cue = block.GetComponent<PseudoHapticGrabTransformer>();
                sb.Append($"{block.name,-18} pos={block.transform.position:F3} held={cue.IsHeld}");
                if (cue.IsHeld)
                    sb.Append($" raw={cue.RawPose.position:F3} gain={cue.CurrentGain:F3} lag={cue.CurrentLagSeconds:F3} offset={cue.OffsetM:F3}");
                sb.AppendLine();
            }

            var lever = Object.FindAnyObjectByType<Lever>();
            if (lever != null)
                sb.AppendLine($"lever left={lever.LeftTorqueNm:F3} right={lever.RightTorqueNm:F3} tilt={lever.TiltDeg:F2} loads={lever.LoadCount} balanced={lever.IsBalanced}");

            var water = Object.FindAnyObjectByType<WaterVolume>();
            if (water != null)
            {
                foreach (var pair in water.Submerged)
                {
                    var expected = PhysicsFormulas.SubmergedFractionAtRest(pair.Key.DensityKgPerM3, water.FluidDensityKgPerM3);
                    sb.AppendLine($"water {pair.Key.name,-18} submerged={pair.Value:F3} expected={expected:F3} vy={pair.Key.Rigidbody.linearVelocity.y:F3}");
                }
            }

            return sb.ToString();
        }

        static Vector3 ParseVector(string text)
        {
            var parts = text.Trim().Split(' ');
            return new Vector3(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture));
        }
    }
}
