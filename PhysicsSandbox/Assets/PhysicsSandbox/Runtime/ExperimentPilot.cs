#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace PhysicsSandbox
{
    /// <summary>
    /// Checks experiment mode without a person: the coding agent calls this through the
    /// AgentBridge in Play mode. It works through a whole session using the real interactor,
    /// grab and button pipeline, and its data is marked run_type=selftest. It also measures
    /// the ramp against the closed-form predictions. Results go to Library/AgentBridge.
    /// </summary>
    public class ExperimentPilot : MonoBehaviour
    {
        const float k_ServoRate = 6f;

        static readonly string s_Dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/AgentBridge"));

        readonly List<string> m_Errors = new List<string>();
        readonly StringBuilder m_Report = new StringBuilder();
        ExperimentSession m_Session;
        NearFarInteractor m_Interactor;
        Transform m_Hand;
        System.Random m_Random;
        int m_Hovered;
        int m_Presses;

        /// <summary>"id": runs one full session as that participant.</summary>
        public static string RunSession(string id)
        {
            var pilot = new GameObject("Experiment Pilot").AddComponent<ExperimentPilot>();
            pilot.StartCoroutine(pilot.Session(int.Parse(id.Trim())));
            return "pilot started as P" + id.Trim();
        }

        public static string RunRampCheck()
        {
            var pilot = new GameObject("Ramp Check").AddComponent<ExperimentPilot>();
            pilot.StartCoroutine(pilot.RampCheck());
            return "ramp check started";
        }

        void OnEnable() => Application.logMessageReceived += OnLog;

        void OnDisable() => Application.logMessageReceived -= OnLog;

        void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
                m_Errors.Add(message);
        }

        void Write(string file, string text) => File.WriteAllText(Path.Combine(s_Dir, file), text);

        bool FindHand()
        {
            foreach (var interactor in FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None))
            {
                var parent = interactor.transform.parent;
                if (parent == null || !parent.name.Contains("Right"))
                    continue;

                m_Interactor = interactor;
                m_Hand = parent;
                foreach (var driver in m_Hand.GetComponents<TrackedPoseDriver>())
                    driver.enabled = false;
                return true;
            }

            return false;
        }

        IEnumerator Session(int id)
        {
            Write("pilot.txt", "running");
            m_Random = new System.Random(id);
            yield return new WaitForSeconds(2f);
            m_Session = FindAnyObjectByType<ExperimentSession>();
            while (!FindHand())
                yield return new WaitForSeconds(0.5f);

            yield return PressButton(0);
            yield return new WaitForSeconds(0.5f);
            m_Session.MarkAsSelfTest();
            m_Session.SetParticipantId(id);
            yield return PressButton(2);

            var lastTitle = "";
            while (ExperimentSession.IsActive)
            {
                var panel = m_Session.Panel;
                if (panel.TitleText != lastTitle)
                {
                    lastTitle = panel.TitleText;
                    m_Report.AppendLine($"{Time.time:0.0}  {lastTitle}");
                    Write("pilot.txt", "running\n" + m_Report);
                }

                if (m_Session.TaskKind.Length > 0)
                {
                    yield return DoTask();
                }
                else if (panel.OptionCount > 0 && panel.InputEnabled)
                {
                    yield return new WaitForSeconds(0.35f);
                    yield return PressButton(m_Random.Next(panel.OptionCount));
                    yield return new WaitForSeconds(0.2f);
                }
                else
                {
                    yield return null;
                }
            }

            Write("pilot.txt", $"done {m_Session.LastSessionPrefix}; pointer hovered the button on {m_Hovered} of {m_Presses} presses; errors {m_Errors.Count}\n{string.Join("\n", m_Errors)}\n{m_Report}");
            Destroy(gameObject);
        }

        // Aims the controller at the button the way a participant would, to confirm the ray
        // reaches it, then presses it through the interaction manager.
        IEnumerator PressButton(int index)
        {
            var button = m_Session.Panel.Buttons[index];
            var aimFrom = m_Session.Panel.transform.position + new Vector3(0.1f, -0.25f, -1.0f);
            var rotation = Quaternion.LookRotation(button.transform.position - aimFrom, Vector3.up);
            m_Hand.SetPositionAndRotation(aimFrom, rotation);

            var hovered = false;
            // The rig smooths its aim, so the pointer takes a moment to arrive after a jump.
            for (var waited = 0f; waited < 1.5f && !hovered; waited += Time.deltaTime)
            {
                yield return null;
                hovered = button.IsHovered;
            }

            m_Presses++;
            if (hovered)
                m_Hovered++;

            var manager = button.Interactable.interactionManager;
            manager.SelectEnter((IXRSelectInteractor)m_Interactor, button.Interactable);
            yield return null;
            if (button.Interactable.isSelected)
                manager.SelectExit((IXRSelectInteractor)m_Interactor, button.Interactable);
            yield return null;
            m_Hand.rotation = Quaternion.identity;
        }

        IEnumerator DoTask()
        {
            var kind = m_Session.TaskKind;
            var blocks = m_Session.TaskBlocks;
            var target = m_Session.TaskTarget;

            if (kind == "lift")
            {
                foreach (var block in blocks)
                {
                    yield return Grab(block);
                    var start = m_Hand.position;
                    yield return MoveHand(start + Vector3.up * 0.2f, 0.5f);
                    yield return MoveHand(start, 0.5f);
                    yield return new WaitForSeconds(0.2f);
                    Release(block);
                    yield return new WaitForSeconds(0.2f);
                }
            }
            else
            {
                var block = blocks[0];
                yield return Grab(block);
                var clearance = Mathf.Max(block.transform.position.y, target.y) + 0.14f;
                var from = block.transform.position;
                yield return Servo(block, new Vector3(from.x, clearance, from.z), 0.6f);
                yield return Servo(block, new Vector3(target.x, clearance, target.z), 1f);
                yield return Servo(block, target + Vector3.up * 0.01f, 0.7f);
                Release(block);
            }

            // Wait for the session to move on before reading the next task.
            var waited = 0f;
            while (m_Session.TaskKind == kind && m_Session.TaskBlocks == blocks && waited < 6f && ExperimentSession.IsActive)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }

        IEnumerator Grab(PhysicsBlock block)
        {
            m_Hand.rotation = Quaternion.identity;
            yield return MoveHand(block.transform.position, 0.4f);
            var grab = block.GetComponent<XRGrabInteractable>();
            grab.interactionManager.SelectEnter((IXRSelectInteractor)m_Interactor, grab);
            yield return new WaitForSeconds(0.2f);
        }

        void Release(PhysicsBlock block)
        {
            var grab = block.GetComponent<XRGrabInteractable>();
            if (grab.isSelected)
                grab.interactionManager.SelectExit((IXRSelectInteractor)m_Interactor, grab);
        }

        IEnumerator MoveHand(Vector3 target, float duration)
        {
            var start = m_Hand.position;
            for (var t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                m_Hand.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            m_Hand.position = target;
        }

        IEnumerator Servo(PhysicsBlock block, Vector3 target, float duration)
        {
            var start = block.transform.position;
            for (var t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                var want = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
                m_Hand.position += (want - block.transform.position) * Mathf.Min(1f, k_ServoRate * Time.deltaTime);
                yield return null;
            }

            for (var settle = 0f; settle < 1.5f && Vector3.Distance(block.transform.position, target) > 0.004f; settle += Time.deltaTime)
            {
                m_Hand.position += (target - block.transform.position) * Mathf.Min(1f, k_ServoRate * Time.deltaTime);
                yield return null;
            }
        }

        // Releases each material from rest at several slopes and compares with the closed form.
        IEnumerator RampCheck()
        {
            Write("ramp.txt", "running");
            yield return new WaitForSeconds(1f);
            var ramp = FindAnyObjectByType<Ramp>();
            var root = ramp.transform.parent;
            var pivot = ramp.transform.position;
            var builtAngle = ramp.AngleDeg;
            var startPosition = root.position;
            var startRotation = root.rotation;

            var blocks = new List<PhysicsBlock>();
            foreach (var material in MaterialLibrary.All)
            {
                foreach (var block in PhysicsBlock.All)
                {
                    if (block.MaterialName == material.name && Mathf.Abs(block.MassKg - material.densityKgPerM3 * 0.001f) < 0.01f)
                        blocks.Add(block);
                }
            }

            var sb = new StringBuilder("angle material mass_kg mu_s mu_d predicts_slide a_predicted a_measured error_pct distance_cm seconds\n");
            foreach (var angle in new[] { builtAngle, 20f, 25f })
            {
                // Raised so that the steeper settings do not push the low end through the table.
                root.SetPositionAndRotation(startPosition, startRotation);
                root.RotateAround(pivot, Vector3.forward, builtAngle - angle);
                root.position += Vector3.up * (angle > builtAngle ? 0.25f : 0f);
                yield return new WaitForFixedUpdate();

                foreach (var block in blocks)
                {
                    ramp.Forget(block);
                    block.MoveTo(ramp.PointOnSurface(0.08f, block.SideM) + ramp.transform.up * 0.001f, ramp.transform.rotation);
                    var waited = 0f;
                    var path = new StringBuilder("  path");
                    var nextSample = 0f;
                    while (!ramp.Finished.ContainsKey(block) && waited < 8f)
                    {
                        if (waited >= nextSample)
                        {
                            path.Append($" {ramp.AlongSlope(block.transform.position) * 100f:0.0}");
                            nextSample += 0.2f;
                        }

                        waited += Time.deltaTime;
                        yield return null;
                    }

                    ramp.Finished.TryGetValue(block, out var run);
                    var predicted = ramp.PredictedAcceleration(block);
                    var measured = run != null ? run.MeasuredAcceleration : -1f;
                    var error = predicted > 0f ? 100f * (measured - predicted) / predicted : 0f;
                    var muS = PhysicsFormulas.CombinedFriction(block.StaticFriction, ramp.SurfaceFrictionStatic);
                    var muD = PhysicsFormulas.CombinedFriction(block.DynamicFriction, ramp.SurfaceFrictionDynamic);
                    sb.AppendLine($"{ramp.AngleDeg:0.0} {block.MaterialName} {block.MassKg:0.00} {muS:0.000} {muD:0.000} {ramp.PredictsSlide(block)} {predicted:0.000} {measured:0.000} {error:0.0} {(run != null ? run.DistanceM * 100f : -1f):0.0} {(run != null ? run.MovingSeconds : -1f):0.00}");
                    sb.AppendLine(path.ToString());
                    block.ResetToHome();
                    yield return new WaitForFixedUpdate();
                }
            }

            root.SetPositionAndRotation(startPosition, startRotation);
            Write("ramp.txt", "done\n" + sb);
            Destroy(gameObject);
        }
    }
}
#endif
