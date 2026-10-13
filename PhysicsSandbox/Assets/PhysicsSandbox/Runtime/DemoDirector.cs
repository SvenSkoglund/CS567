using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace PhysicsSandbox
{
    /// <summary>
    /// Scripted walkthrough for desktop recordings. Press N in simulator mode and the right
    /// hand performs each lesson on its own, with a caption explaining what is shown. It
    /// drives the same interactor and grab pipeline a person would, so what the recording
    /// shows is the real cue, not an animation.
    /// </summary>
    public class DemoDirector : MonoBehaviour
    {
        const float k_LiftHeightM = 0.30f;
        const float k_ServoRate = 6f;

        [SerializeField] Lever m_Lever;
        [SerializeField] WaterVolume m_Water;

        readonly List<Behaviour> m_SuspendedDrivers = new List<Behaviour>();
        InputAction m_Toggle;
        Coroutine m_Run;
        NearFarInteractor m_Interactor;
        Transform m_Hand;
        Transform m_Camera;
        PhysicsBlock m_Held;
        string m_Caption = "";
        GUIStyle m_CaptionStyle;
        GUIStyle m_ReadoutStyle;

        public Lever Lever
        {
            get => m_Lever;
            set => m_Lever = value;
        }

        public WaterVolume Water
        {
            get => m_Water;
            set => m_Water = value;
        }

        /// <summary>True from the first N press until control is handed back, including the end card.</summary>
        public bool IsRunning { get; private set; }

        void Awake()
        {
            m_Toggle = new InputAction("Scripted Demo", InputActionType.Button, "<Keyboard>/n");
        }

        void OnEnable() => m_Toggle.Enable();

        void OnDisable()
        {
            m_Toggle.Disable();
            StopDemo();
        }

        void Update()
        {
            if (!m_Toggle.WasPressedThisFrame())
                return;

            if (IsRunning)
                StopDemo();
            else
                StartDemo();
        }

        public void StartDemo()
        {
            if (IsRunning)
                return;

            if (XRSettings.isDeviceActive)
            {
                m_Caption = "The scripted demo only runs in simulator mode.";
                return;
            }

            m_Interactor = FindRightInteractor();
            var camera = Camera.main;
            if (m_Interactor == null || camera == null)
            {
                m_Caption = "Hands are not ready yet. Try again in a moment.";
                return;
            }

            m_Hand = m_Interactor.transform.parent;
            m_Camera = camera.transform;
            Suspend(m_Hand);
            Suspend(m_Camera);

            foreach (var block in PhysicsBlock.All)
                block.ResetToHome();

            IsRunning = true;
            m_Run = StartCoroutine(Run());
        }

        public void StopDemo()
        {
            if (m_Run != null)
                StopCoroutine(m_Run);
            m_Run = null;
            IsRunning = false;

            Release();
            PseudoHapticSettings.CueEnabled = true;
            foreach (var driver in m_SuspendedDrivers)
            {
                if (driver != null)
                    driver.enabled = true;
            }

            m_SuspendedDrivers.Clear();
            m_Caption = "";
        }

        // Tracking is switched off for the hand and camera so this script can pose them.
        void Suspend(Transform target)
        {
            foreach (var driver in target.GetComponents<TrackedPoseDriver>())
            {
                if (!driver.enabled)
                    continue;

                driver.enabled = false;
                m_SuspendedDrivers.Add(driver);
            }
        }

        static NearFarInteractor FindRightInteractor()
        {
            foreach (var interactor in FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None))
            {
                var parent = interactor.transform.parent;
                if (parent != null && parent.name.Contains("Right"))
                    return interactor;
            }

            return null;
        }

        static PhysicsBlock Find(MaterialDef material, float massKg)
        {
            foreach (var block in PhysicsBlock.All)
            {
                if (block.MaterialName == material.name && Mathf.Abs(block.MassKg - massKg) < 0.01f)
                    return block;
            }

            return null;
        }

        static PhysicsBlock FindLitre(MaterialDef material) => Find(material, material.densityKgPerM3 * 0.001f);

        IEnumerator Run()
        {
            var rest = new Vector3(0.1f, 1.25f, 0.3f);
            m_Hand.SetPositionAndRotation(rest, Quaternion.identity);

            // 1. Same size, different mass: the cue alone tells the blocks apart.
            yield return Frame(new Vector3(-0.10f, 1.52f, -0.28f), new Vector3(-0.10f, 1.02f, 0.52f));
            m_Caption = "Four blocks of the same size, 1 litre each. The hand lifts each one 30 cm.\n" +
                "The orange wireframe is where the hand really is. Heavier blocks rise less and lag more.";
            yield return new WaitForSeconds(2.5f);
            foreach (var material in MaterialLibrary.All)
                yield return LiftAndLower(FindLitre(material));

            // 2. Cue off: the control condition of the study.
            m_Caption = "Weight cue OFF (the study's control condition).\nThe same 7.85 kg steel block now follows the hand one-to-one.";
            PseudoHapticSettings.CueEnabled = false;
            yield return new WaitForSeconds(1.5f);
            yield return LiftAndLower(FindLitre(MaterialLibrary.Steel));
            PseudoHapticSettings.CueEnabled = true;

            // 3. Same mass, different size: the cue must not change with density.
            yield return Frame(new Vector3(-0.72f, 1.50f, -0.22f), new Vector3(-0.76f, 1.02f, 0.52f));
            m_Caption = "Four blocks of the same mass, 1 kg each. Denser material, smaller block.\n" +
                "Pine and steel get the same cue, because the cue tracks mass, not density.";
            yield return new WaitForSeconds(2.5f);
            yield return LiftAndLower(Find(MaterialLibrary.Pine, 1f));
            yield return LiftAndLower(Find(MaterialLibrary.Steel, 1f));

            // 4. Buoyancy.
            if (m_Water != null)
            {
                var water = m_Water.GetComponent<Collider>().bounds;
                yield return Frame(new Vector3(water.center.x - 0.25f, 1.62f, -0.20f), new Vector3(water.center.x - 0.10f, water.center.y, water.center.z));
                m_Caption = "Water tank. Each 1 kg block is carried over and released.\n" +
                    "Pine floats half under, ice floats 92% under, steel sinks: the submerged fraction equals the density ratio.";
                var drops = new[] { MaterialLibrary.Pine, MaterialLibrary.Ice, MaterialLibrary.Steel };
                for (var i = 0; i < drops.Length; i++)
                {
                    var block = Find(drops[i], 1f);
                    var target = new Vector3(water.center.x + (i - 1) * 0.155f, water.max.y + block.SideM * 0.5f + 0.02f, water.center.z);
                    yield return CarryTo(block, target);
                }

                yield return new WaitForSeconds(4f);
            }

            // 5. Law of the lever.
            if (m_Lever != null)
            {
                var pivot = m_Lever.transform.position;
                var top = pivot.y + 0.01f;
                yield return Frame(new Vector3(pivot.x, 1.50f, 0.02f), new Vector3(pivot.x, top, pivot.z));

                var aluminum = FindLitre(MaterialLibrary.Aluminum);
                var ice = FindLitre(MaterialLibrary.Ice);
                const float aluminumArm = 0.10f;
                var iceArm = PhysicsFormulas.BalancingArmM(aluminum.MassKg, aluminumArm, ice.MassKg);

                m_Caption = "Lever. 2.7 kg of aluminum goes 10 cm left of the pivot, and the plank tips.";
                yield return CarryTo(aluminum, new Vector3(pivot.x - aluminumArm, top + aluminum.SideM * 0.5f + 0.025f, pivot.z));
                yield return new WaitForSeconds(2.5f);

                m_Caption = $"0.92 kg of ice balances it at {iceArm * 100f:0.0} cm on the right,\n" +
                    $"because 2.7 kg x 10 cm = 0.917 kg x {iceArm * 100f:0.0} cm.";
                yield return CarryTo(ice, new Vector3(pivot.x + iceArm, top + ice.SideM * 0.5f + 0.035f, pivot.z));
                yield return new WaitForSeconds(5f);
            }

            m_Caption = "End of the scripted demo. Press N to take the controls back.";
            yield return MoveHand(rest, 0.8f);
            m_Run = null;
        }

        IEnumerator LiftAndLower(PhysicsBlock block)
        {
            if (block == null)
                yield break;

            yield return Grab(block);
            var start = m_Hand.position;
            yield return MoveHand(start + Vector3.up * k_LiftHeightM, 1.0f);
            yield return new WaitForSeconds(1.2f);
            yield return MoveHand(start, 1.0f);
            yield return new WaitForSeconds(0.7f);
            Release();
            yield return new WaitForSeconds(0.4f);
        }

        // Carries a block so that the block itself, not the hand, ends up at the target.
        IEnumerator CarryTo(PhysicsBlock block, Vector3 target)
        {
            if (block == null)
                yield break;

            yield return Grab(block);
            var clearance = Mathf.Max(block.transform.position.y, target.y) + 0.14f;
            var from = block.transform.position;
            yield return ServoBlock(block, new Vector3(from.x, clearance, from.z), 0.8f);
            yield return ServoBlock(block, new Vector3(target.x, clearance, target.z), 1.4f);
            yield return ServoBlock(block, target, 0.9f);
            Release();
            yield return new WaitForSeconds(0.6f);
        }

        IEnumerator Grab(PhysicsBlock block)
        {
            var distance = Vector3.Distance(m_Hand.position, block.transform.position);
            yield return MoveHand(block.transform.position, Mathf.Clamp(distance / 0.9f, 0.35f, 1.2f));
            yield return new WaitForSeconds(0.15f);

            var grab = block.GetComponent<XRGrabInteractable>();
            grab.interactionManager.SelectEnter((IXRSelectInteractor)m_Interactor, grab);
            m_Held = block;
            yield return new WaitForSeconds(0.25f);
        }

        void Release()
        {
            if (m_Held == null)
                return;

            var grab = m_Held.GetComponent<XRGrabInteractable>();
            if (grab != null && grab.isSelected && m_Interactor != null)
                grab.interactionManager.SelectExit((IXRSelectInteractor)m_Interactor, grab);
            m_Held = null;
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

        // The cue puts the block somewhere other than the hand, so placing it precisely
        // means steering the hand on the block's displayed position, as a person would by eye.
        IEnumerator ServoBlock(PhysicsBlock block, Vector3 target, float duration)
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

        IEnumerator Frame(Vector3 position, Vector3 lookAt)
        {
            var startPosition = m_Camera.position;
            var startRotation = m_Camera.rotation;
            var rotation = Quaternion.LookRotation(lookAt - position, Vector3.up);
            for (var t = 0f; t < 1f; t += Time.deltaTime / 1.2f)
            {
                var s = Mathf.SmoothStep(0f, 1f, t);
                m_Camera.SetPositionAndRotation(Vector3.Lerp(startPosition, position, s), Quaternion.Slerp(startRotation, rotation, s));
                yield return null;
            }

            m_Camera.SetPositionAndRotation(position, rotation);
        }

        void OnGUI()
        {
            if (string.IsNullOrEmpty(m_Caption))
                return;

            if (m_CaptionStyle == null)
            {
                var background = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                background.SetPixel(0, 0, new Color(0.05f, 0.06f, 0.08f, 0.88f));
                background.Apply();

                m_CaptionStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, wordWrap = true, richText = true };
                m_CaptionStyle.normal.background = background;
                m_CaptionStyle.normal.textColor = Color.white;
                m_ReadoutStyle = new GUIStyle(m_CaptionStyle);
                m_ReadoutStyle.normal.textColor = new Color(1f, 0.72f, 0.35f);
            }

            var scale = Screen.height / 720f;
            m_CaptionStyle.fontSize = Mathf.RoundToInt(21f * scale);
            m_ReadoutStyle.fontSize = Mathf.RoundToInt(19f * scale);

            var width = Screen.width * 0.82f;
            var x = (Screen.width - width) * 0.5f;
            GUI.Box(new Rect(x, 14f * scale, width, 78f * scale), m_Caption, m_CaptionStyle);

            var readout = BuildReadout();
            if (readout.Length > 0)
                GUI.Box(new Rect(x, 98f * scale, width, 36f * scale), readout, m_ReadoutStyle);
        }

        string BuildReadout()
        {
            if (m_Held != null && m_Held.TryGetComponent<PseudoHapticGrabTransformer>(out var cue) && cue.IsHeld)
            {
                return $"{m_Held.MaterialName} {m_Held.MassKg:0.00} kg    cue {(PseudoHapticSettings.CueEnabled ? "ON" : "OFF")}    " +
                    $"gain {cue.CurrentGain:0.00}    lag {cue.CurrentLagSeconds * 1000f:0} ms    hand-to-block offset {cue.OffsetM * 100f:0.0} cm";
            }

            if (m_Lever != null && m_Lever.LoadCount > 0)
            {
                return $"left {m_Lever.LeftTorqueNm:0.00} N·m    right {m_Lever.RightTorqueNm:0.00} N·m    tilt {m_Lever.TiltDeg:0.0}°" +
                    (m_Lever.IsBalanced ? "    BALANCED" : "");
            }

            if (m_Water != null && m_Water.Submerged.Count > 0)
            {
                var text = "";
                foreach (var pair in m_Water.Submerged)
                {
                    if (!pair.Key.IsHeld)
                        text += $"{pair.Key.MaterialName} {pair.Value * 100f:0}% under    ";
                }

                return text.TrimEnd();
            }

            return "";
        }
    }
}
