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
    /// shows is the real cue, not an animation. The narrated variant adds title cards and
    /// longer holds so a voice-over fits.
    /// </summary>
    public class DemoDirector : MonoBehaviour
    {
        const float k_LiftHeightM = 0.30f;
        const float k_ServoRate = 6f;
        const string k_Bullet = "•  ";
        const string k_BulletGap = "\n<size=40%> </size>\n";

        [SerializeField] Lever m_Lever;
        [SerializeField] WaterVolume m_Water;

        readonly List<Behaviour> m_Suspended = new List<Behaviour>();
        readonly List<string> m_Timeline = new List<string>();
        InputAction m_Toggle;
        Coroutine m_Run;
        NearFarInteractor m_Interactor;
        Transform m_Hand;
        Transform m_Camera;
        PhysicsBlock m_Held;
        bool m_Narrated;
        float m_StartTime;
        string m_Caption = "";
        string m_CardTitle;
        string m_CardBody;
        GUIStyle m_CaptionStyle;
        GUIStyle m_ReadoutStyle;
        GUIStyle m_TitleStyle;
        GUIStyle m_BodyStyle;
        GUIStyle m_FooterStyle;
        Texture2D m_CardBackground;
        Texture2D m_Accent;

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

        /// <summary>True from the start until control is handed back, including the end card.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>True once the last step of the sequence has played.</summary>
        public bool SequenceDone { get; private set; }

        /// <summary>"seconds, tab, label" for each segment, for writing a voice-over against.</summary>
        public IReadOnlyList<string> Timeline => m_Timeline;

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
                StartDemo(false);
        }

        public void StartDemo(bool narrated)
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

            m_Narrated = narrated;
            m_Hand = m_Interactor.transform.parent;
            m_Camera = camera.transform;
            Suspend(m_Hand);
            Suspend(m_Camera);

            // A recording should show the sandbox, not the idle left hand or the simulator's
            // on-screen help.
            if (narrated)
            {
                foreach (var interactor in FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None))
                {
                    var parent = interactor.transform.parent;
                    if (parent == null || parent == m_Hand)
                        continue;

                    Suspend(parent);
                    parent.position = new Vector3(0f, -10f, 0f);
                }

                foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (canvas.enabled && canvas.renderMode != RenderMode.WorldSpace)
                    {
                        canvas.enabled = false;
                        m_Suspended.Add(canvas);
                    }
                }
            }

            foreach (var block in PhysicsBlock.All)
                block.ResetToHome();

            m_Timeline.Clear();
            m_StartTime = Time.time;
            SequenceDone = false;
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
            foreach (var behaviour in m_Suspended)
            {
                if (behaviour != null)
                    behaviour.enabled = true;
            }

            m_Suspended.Clear();
            m_Caption = "";
            m_CardTitle = null;
        }

        // Tracking is switched off for the hand and camera so this script can pose them.
        void Suspend(Transform target)
        {
            foreach (var driver in target.GetComponents<TrackedPoseDriver>())
            {
                if (!driver.enabled)
                    continue;

                driver.enabled = false;
                m_Suspended.Add(driver);
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

        float Pick(float quick, float narrated) => m_Narrated ? narrated : quick;

        void Mark(string label) => m_Timeline.Add($"{Time.time - m_StartTime:0.0}\t{label}");

        IEnumerator Run()
        {
            var rest = new Vector3(0.1f, 1.25f, 0.3f);
            var wide = new Vector3(-0.10f, 1.62f, -0.62f);
            var wideLook = new Vector3(-0.10f, 1.00f, 0.60f);
            m_Hand.SetPositionAndRotation(rest, Quaternion.identity);
            m_Camera.SetPositionAndRotation(wide, Quaternion.LookRotation(wideLook - wide));

            if (m_Narrated)
            {
                Mark("Title card");
                yield return Card("Physics You Can Feel",
                    "A VR sandbox where virtual objects feel heavy\n\n" +
                    "<color=#F2A65A>CS 567  ·  Checkpoint 3 prototype</color>\n" +
                    "Sven Skoglund", 7f);

                Mark("Status card");
                yield return Card("Where things stand", Bullets(
                    "The proposal and study design are done",
                    "This is the first working prototype",
                    "It runs in the Unity editor with a simulated headset",
                    "Next: run it on the Quest 2"), 14f);

                Mark("Scene overview");
                m_Caption = "Same mass: 1 kg each. Denser materials make smaller blocks.";
                yield return Frame(new Vector3(-0.80f, 1.52f, -0.18f), new Vector3(-0.78f, 0.98f, 0.50f), 1.6f);
                yield return new WaitForSeconds(3.5f);
                m_Caption = "Same size: 1 litre each. Very different masses.";
                yield return Frame(new Vector3(-0.10f, 1.52f, -0.18f), new Vector3(-0.10f, 0.98f, 0.50f), 1.6f);
                yield return new WaitForSeconds(3.5f);
                if (m_Lever != null)
                {
                    m_Caption = "A lever.";
                    yield return Frame(new Vector3(-0.38f, 1.48f, 0.10f), m_Lever.transform.position, 1.6f);
                    yield return new WaitForSeconds(2.5f);
                }

                if (m_Water != null)
                {
                    m_Caption = "A water tank.";
                    yield return Frame(new Vector3(0.50f, 1.60f, -0.15f), m_Water.transform.position, 1.6f);
                    yield return new WaitForSeconds(2.5f);
                }

                m_Caption = "A panel that shows what is happening.";
                yield return Frame(new Vector3(0.36f, 1.55f, 0.15f), new Vector3(0.45f, 1.60f, 1.35f), 1.6f);
                yield return new WaitForSeconds(3.5f);
            }

            // 1. Same size, different mass: the cue alone tells the blocks apart.
            Mark("Cue ON: four same-size blocks lifted (pine, ice, aluminum, steel)");
            yield return Frame(new Vector3(-0.10f, 1.52f, -0.28f), new Vector3(-0.10f, 1.02f, 0.52f), 1.2f);
            m_Caption = "Same size, different mass. The hand lifts each block 30 cm.\n" +
                "The orange line is the gap between the hand and the block. Heavier blocks lag more.";
            yield return new WaitForSeconds(Pick(2.5f, 4f));
            foreach (var material in MaterialLibrary.All)
                yield return LiftAndLower(FindLitre(material));

            // 2. Cue off: the control condition of the study.
            Mark("Cue OFF: the same lifts, one-to-one");
            m_Caption = "Weight cue OFF. Now every block follows the hand exactly.";
            PseudoHapticSettings.CueEnabled = false;
            yield return new WaitForSeconds(Pick(1.5f, 3f));
            if (m_Narrated)
            {
                foreach (var material in MaterialLibrary.All)
                    yield return LiftAndLower(FindLitre(material));
            }
            else
            {
                yield return LiftAndLower(FindLitre(MaterialLibrary.Steel));
            }

            PseudoHapticSettings.CueEnabled = true;

            // 3. Same mass, different size: the cue must not change with density.
            Mark("Same mass, different size: all get the same cue");
            yield return Frame(new Vector3(-0.72f, 1.50f, -0.22f), new Vector3(-0.76f, 1.02f, 0.52f), 1.2f);
            m_Caption = "Same mass, different size. They all get the same cue,\nbecause the cue follows mass, not size.";
            yield return new WaitForSeconds(Pick(2.5f, 4f));
            foreach (var material in MaterialLibrary.All)
            {
                if (m_Narrated || material.name == MaterialLibrary.Pine.name || material.name == MaterialLibrary.Steel.name)
                    yield return LiftAndLower(Find(material, 1f));
            }

            // 4. Buoyancy.
            if (m_Water != null)
            {
                Mark("Water tank: pine and ice float, aluminum and steel sink");
                var water = m_Water.GetComponent<Collider>().bounds;
                yield return Frame(new Vector3(water.center.x - 0.25f, 1.62f, -0.20f), new Vector3(water.center.x - 0.10f, water.center.y, water.center.z), 1.2f);
                m_Caption = "Water tank. Less dense than water floats. Denser sinks.";
                yield return new WaitForSeconds(Pick(0f, 2f));

                var drops = m_Narrated
                    ? new[] { MaterialLibrary.Pine, MaterialLibrary.Ice, MaterialLibrary.Aluminum, MaterialLibrary.Steel }
                    : new[] { MaterialLibrary.Pine, MaterialLibrary.Ice, MaterialLibrary.Steel };
                var offsets = m_Narrated ? new[] { -0.165f, -0.035f, 0.075f, 0.165f } : new[] { -0.155f, 0f, 0.155f };
                for (var i = 0; i < drops.Length; i++)
                {
                    var block = Find(drops[i], 1f);
                    yield return CarryTo(block, new Vector3(water.center.x + offsets[i], water.max.y + block.SideM * 0.5f + 0.02f, water.center.z));
                }

                yield return new WaitForSeconds(Pick(4f, 7f));
            }

            // 5. Law of the lever.
            if (m_Lever != null)
            {
                Mark("Lever 1: aluminum left, ice too close, then ice balances");
                var pivot = m_Lever.transform.position;
                var top = pivot.y + 0.01f;
                yield return Frame(new Vector3(pivot.x, 1.50f, 0.02f), new Vector3(pivot.x, top, pivot.z), 1.2f);

                var aluminum = FindLitre(MaterialLibrary.Aluminum);
                var ice = FindLitre(MaterialLibrary.Ice);
                var steel = FindLitre(MaterialLibrary.Steel);
                const float aluminumArm = 0.10f;
                var iceArm = PhysicsFormulas.BalancingArmM(aluminum.MassKg, aluminumArm, ice.MassKg);

                Vector3 OnPlank(PhysicsBlock block, float arm) =>
                    new Vector3(pivot.x + arm, top + block.SideM * 0.5f + 0.035f, pivot.z);

                m_Caption = "Lever. Aluminum, 2.7 kg, goes 10 cm left of the middle. It tips.";
                yield return new WaitForSeconds(Pick(0f, 2f));
                yield return CarryTo(aluminum, OnPlank(aluminum, -aluminumArm));
                yield return new WaitForSeconds(Pick(2.5f, 3.5f));

                if (m_Narrated)
                {
                    m_Caption = "Ice, 0.92 kg, at 20 cm on the right. Not far enough.";
                    yield return CarryTo(ice, OnPlank(ice, 0.20f));
                    yield return new WaitForSeconds(3f);
                }

                m_Caption = $"Ice at {iceArm * 100f:0.0} cm. Balanced: mass x distance is equal on both sides.";
                yield return CarryTo(ice, OnPlank(ice, iceArm));
                yield return new WaitForSeconds(Pick(5f, 7f));

                if (m_Narrated)
                {
                    Mark("Lever 2: aluminum right, heavy steel close to the middle");
                    const float steelArm = 0.05f;
                    var farArm = PhysicsFormulas.BalancingArmM(steel.MassKg, steelArm, aluminum.MassKg);

                    m_Caption = "One more. The ice goes back.";
                    yield return CarryTo(ice, ice.HomePosition + Vector3.up * 0.02f);
                    m_Caption = $"Aluminum moves to {farArm * 100f:0.0} cm on the right. It tips the other way.";
                    yield return CarryTo(aluminum, OnPlank(aluminum, farArm));
                    yield return new WaitForSeconds(3f);
                    m_Caption = "Steel, 7.85 kg, goes just 5 cm left of the middle. Balanced again.";
                    yield return CarryTo(steel, OnPlank(steel, -steelArm));
                    yield return new WaitForSeconds(9f);
                }
            }

            yield return MoveHand(rest, 0.8f);

            if (m_Narrated)
            {
                Mark("Wide shot");
                m_Caption = "The prototype so far.";
                yield return Frame(wide, wideLook, 1.5f);
                yield return new WaitForSeconds(4f);
                m_Caption = "";

                Mark("Results card");
                yield return Card("It matches the physics", Bullets(
                    "Heavier blocks always lag more",
                    "Floating depth matches density",
                    "The lever balances when mass × distance is equal"), 12f);

                Mark("AI agent card");
                yield return Card("Built with an AI coding agent", Bullets(
                    "The agent wrote the code and built the scene",
                    "It ran and tested the prototype by itself",
                    "I directed it and logged every task"), 14f);

                Mark("Next steps card");
                yield return Card("Next steps", Bullets(
                    "Run it on the Quest 2 headset",
                    "Add a ramp for friction",
                    "Run the user study"), 10f);
                Mark("End");
            }
            else
            {
                m_Caption = "End of the scripted demo. Press N to take the controls back.";
            }

            m_Run = null;
            SequenceDone = true;
        }

        static string Bullets(params string[] lines) => k_Bullet + string.Join(k_BulletGap + k_Bullet, lines);

        IEnumerator Card(string title, string body, float seconds)
        {
            m_CardTitle = title;
            m_CardBody = body;
            yield return new WaitForSeconds(seconds);
            m_CardTitle = null;
        }

        IEnumerator LiftAndLower(PhysicsBlock block)
        {
            if (block == null)
                yield break;

            yield return Grab(block);
            var start = m_Hand.position;
            yield return MoveHand(start + Vector3.up * k_LiftHeightM, Pick(1.0f, 1.3f));
            yield return new WaitForSeconds(Pick(1.2f, 3.6f));
            yield return MoveHand(start, Pick(1.0f, 1.3f));
            yield return new WaitForSeconds(Pick(0.7f, 0.9f));
            Release();
            yield return new WaitForSeconds(Pick(0.4f, 0.7f));
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
            yield return ServoBlock(block, new Vector3(target.x, clearance, target.z), Pick(1.4f, 1.8f));
            yield return ServoBlock(block, target, 0.9f);
            Release();
            yield return new WaitForSeconds(Pick(0.6f, 1.0f));
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

        IEnumerator Frame(Vector3 position, Vector3 lookAt, float duration)
        {
            var startPosition = m_Camera.position;
            var startRotation = m_Camera.rotation;
            var rotation = Quaternion.LookRotation(lookAt - position, Vector3.up);
            for (var t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                var s = Mathf.SmoothStep(0f, 1f, t);
                m_Camera.SetPositionAndRotation(Vector3.Lerp(startPosition, position, s), Quaternion.Slerp(startRotation, rotation, s));
                yield return null;
            }

            m_Camera.SetPositionAndRotation(position, rotation);
        }

        void OnGUI()
        {
            if (m_CardTitle == null && string.IsNullOrEmpty(m_Caption))
                return;

            EnsureStyles();
            var scale = Screen.height / 720f;

            if (m_CardTitle != null)
            {
                DrawCard(scale);
                return;
            }

            m_CaptionStyle.fontSize = Mathf.RoundToInt(21f * scale);
            m_ReadoutStyle.fontSize = Mathf.RoundToInt(19f * scale);

            var width = Screen.width * 0.82f;
            var x = (Screen.width - width) * 0.5f;
            GUI.Box(new Rect(x, 14f * scale, width, 78f * scale), m_Caption, m_CaptionStyle);

            var readout = BuildReadout();
            if (readout.Length > 0)
                GUI.Box(new Rect(x, 98f * scale, width, 36f * scale), readout, m_ReadoutStyle);
        }

        void DrawCard(float scale)
        {
            m_TitleStyle.fontSize = Mathf.RoundToInt(46f * scale);
            m_BodyStyle.fontSize = Mathf.RoundToInt(31f * scale);
            m_FooterStyle.fontSize = Mathf.RoundToInt(15f * scale);

            var margin = Screen.width * 0.09f;
            var width = Screen.width - 2f * margin;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), m_CardBackground);
            GUI.Label(new Rect(margin, Screen.height * 0.11f, width, 70f * scale), m_CardTitle, m_TitleStyle);
            GUI.DrawTexture(new Rect(margin, Screen.height * 0.11f + 74f * scale, 110f * scale, 5f * scale), m_Accent);
            GUI.Label(new Rect(margin, Screen.height * 0.11f + 108f * scale, width, Screen.height * 0.68f), m_CardBody, m_BodyStyle);
            GUI.Label(new Rect(margin, Screen.height - 46f * scale, width, 30f * scale),
                "Physics You Can Feel  ·  CS 567 Checkpoint 3  ·  CS567_SVEN", m_FooterStyle);
        }

        void EnsureStyles()
        {
            if (m_CaptionStyle != null)
                return;

            m_CardBackground = SolidTexture(new Color(0.055f, 0.07f, 0.10f, 1f));
            m_Accent = SolidTexture(new Color(0.95f, 0.65f, 0.35f, 1f));

            m_CaptionStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, wordWrap = true, richText = true };
            m_CaptionStyle.normal.background = SolidTexture(new Color(0.05f, 0.06f, 0.08f, 0.88f));
            m_CaptionStyle.normal.textColor = Color.white;
            m_ReadoutStyle = new GUIStyle(m_CaptionStyle);
            m_ReadoutStyle.normal.textColor = new Color(1f, 0.72f, 0.35f);

            m_TitleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, fontStyle = FontStyle.Bold, richText = true };
            m_TitleStyle.normal.textColor = Color.white;
            m_BodyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, wordWrap = true, richText = true };
            m_BodyStyle.normal.textColor = new Color(0.88f, 0.90f, 0.93f);
            m_FooterStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft };
            m_FooterStyle.normal.textColor = new Color(0.55f, 0.60f, 0.66f);
        }

        static Texture2D SolidTexture(Color color)
        {
            var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
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
