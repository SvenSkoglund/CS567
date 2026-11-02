using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PhysicsSandbox
{
    /// <summary>
    /// Experiment mode: runs the study exactly as a participant sees it. Takes a participant
    /// ID, derives the counterbalanced block order, then runs training and two blocks of
    /// pre-test, four scenes, heaviness trials, post-test and questionnaire, logging every
    /// answer and grab. The condition is shown only on the experimenter's desktop view.
    /// </summary>
    public class ExperimentSession : MonoBehaviour
    {
        const float k_TableTop = 0.90f;
        const float k_PanelZ = 1.12f;
        const float k_LiftedM = 0.05f;
        const float k_TaskTimeoutSeconds = 150f;
        const float k_LeverTolerance = 0.10f;
        const float k_LeverHoldSeconds = 1f;
        const float k_RampStartAlongM = 0.12f;
        const float k_RampStartToleranceM = 0.14f;
        const float k_SettleSpeed = 0.03f;
        const float k_FeedbackSeconds = 3.5f;
        const int k_FrameBudgetHz = 72;

        // Where the participant stands for each part: x, z.
        static readonly Vector2 s_StandQuestions = new Vector2(0f, 0.05f);
        static readonly Vector2 s_StandBlocks = new Vector2(-0.10f, 0.05f);
        static readonly Vector2 s_StandTank = new Vector2(0.50f, 0.05f);
        static readonly Vector2 s_StandLever = new Vector2(-0.38f, 0.15f);
        static readonly Vector2 s_StandRamp = new Vector2(1.60f, 0.05f);

        [SerializeField] ExperimentPanel m_Panel;
        [SerializeField] Lever m_Lever;
        [SerializeField] WaterVolume m_Water;
        [SerializeField] Ramp m_Ramp;
        [SerializeField] PhysicsBlock m_UnmarkedLeft;
        [SerializeField] PhysicsBlock m_UnmarkedRight;
        [SerializeField] GameObject[] m_HideDuringStudy;
        [SerializeField] GameObject m_TrainingPad;

        readonly List<PhysicsBlock> m_Blocks = new List<PhysicsBlock>();
        readonly Dictionary<PhysicsBlock, GrabRecord> m_Grabs = new Dictionary<PhysicsBlock, GrabRecord>();
        readonly Dictionary<string, BlockScore> m_Scores = new Dictionary<string, BlockScore>();
        readonly List<string> m_BlockOrder = new List<string>();

        Coroutine m_Run;
        StudyLog m_Log;
        XROrigin m_Origin;
        GameObject m_Locomotion;
        GUIStyle m_StripStyle;
        Vector3 m_IdlePanelPosition;
        int m_ParticipantId = Counterbalance.FirstPilotId;
        string m_RunType = "study";
        int m_LastChoice;
        float m_LastRtMs;

        int m_BlockNumber;
        bool m_BlockCue;
        ScenarioSet m_BlockSet;
        string m_Phase = "";
        string m_Item = "";
        string m_TaskOutcome = "";
        float m_SessionStart;
        DateTime m_ClockStart;
        float m_ClockStartRealtime;

        int m_FpsFrames;
        float m_FpsSeconds;
        int m_FpsOverBudget;

        public static bool IsActive { get; private set; }

        /// <summary>What the participant must physically do right now, for the self-test.</summary>
        public string TaskKind { get; private set; } = "";
        public PhysicsBlock[] TaskBlocks { get; private set; } = Array.Empty<PhysicsBlock>();
        public Vector3 TaskTarget { get; private set; }

        public ExperimentPanel Panel
        {
            get => m_Panel;
            set => m_Panel = value;
        }

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

        public Ramp Ramp
        {
            get => m_Ramp;
            set => m_Ramp = value;
        }

        public PhysicsBlock UnmarkedLeft
        {
            get => m_UnmarkedLeft;
            set => m_UnmarkedLeft = value;
        }

        public PhysicsBlock UnmarkedRight
        {
            get => m_UnmarkedRight;
            set => m_UnmarkedRight = value;
        }

        public GameObject[] HideDuringStudy
        {
            get => m_HideDuringStudy;
            set => m_HideDuringStudy = value;
        }

        public GameObject TrainingPad
        {
            get => m_TrainingPad;
            set => m_TrainingPad = value;
        }

        public string LastSessionPrefix { get; private set; } = "";

        /// <summary>The self-test marks its runs so they cannot be mistaken for study data.</summary>
        public void MarkAsSelfTest() => m_RunType = "selftest";

        class GrabRecord
        {
            public float StartTime;
            public float StartHandY;
            public float MaxLift;
            public float MaxOffset;
            public float LiftedThisTask;
        }

        class BlockScore
        {
            public int PreCorrect;
            public int PostCorrect;
            public int HeavinessCorrect;
            public int HeavinessTotal;
            public float PresenceSum;
            public int PresenceCount;
            public float Comfort;
            public float TaskMs;
        }

        void Awake() => TrainingPadVisible(false);

        void TrainingPadVisible(bool visible)
        {
            if (m_TrainingPad != null)
                m_TrainingPad.SetActive(visible);
        }

        void Start()
        {
            m_IdlePanelPosition = m_Panel.transform.position;
            ShowIdle();
        }

        void ShowIdle()
        {
            m_Panel.transform.position = m_IdlePanelPosition;
            m_Panel.Show("Experiment mode", "Runs the study as a participant sees it.", new[] { "Start experiment mode" });
        }

        void Update()
        {
            if (!IsActive)
            {
                if (m_Panel.TryTakeChoice(out _))
                    Begin();
                return;
            }

            m_FpsFrames++;
            m_FpsSeconds += Time.unscaledDeltaTime;
            if (Time.unscaledDeltaTime > 1.1f / k_FrameBudgetHz)
                m_FpsOverBudget++;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f12Key.wasPressedThisFrame)
                End("aborted by experimenter");
        }

        void OnDisable()
        {
            if (IsActive)
                End("play mode ended");
        }

        public void Begin()
        {
            if (IsActive)
                return;

            IsActive = true;
            m_SessionStart = Time.time;
            m_Blocks.Clear();
            m_Blocks.AddRange(FindObjectsByType<PhysicsBlock>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            m_Run = StartCoroutine(RunSession());
        }

        IEnumerator RunSession()
        {
            PseudoHapticSettings.GhostEnabled = false;
            PseudoHapticSettings.CueEnabled = false;
            yield return EnterParticipant();

            m_ClockStart = DateTime.Now;
            m_ClockStartRealtime = Time.realtimeSinceStartup;
            m_Log = new StudyLog(m_ParticipantId);
            if (m_RunType == "study" && Counterbalance.IsPilot(m_ParticipantId))
                m_RunType = "pilot";
            LastSessionPrefix = m_Log.Prefix;
            WriteSessionHeader();
            PrepareEnvironment();

            yield return Training();

            var plan = Counterbalance.Plan(m_ParticipantId);
            for (var i = 0; i < plan.Length; i++)
                yield return RunBlock(i + 1, plan[i], plan.Length);

            yield return ShowSummary();
            End("completed");
        }

        // ---------------------------------------------------------------- setup

        IEnumerator EnterParticipant()
        {
            m_Phase = "setup";
            var keyboard = Keyboard.current;
            var typed = "";
            string[] buttons = { "ID - 1", "ID + 1", "Start session" };

            ShowParticipantScreen(buttons);
            while (true)
            {
                if (keyboard != null)
                {
                    var changed = ReadDigit(keyboard, ref typed);
                    if (keyboard.backspaceKey.wasPressedThisFrame && typed.Length > 0)
                    {
                        typed = typed.Substring(0, typed.Length - 1);
                        changed = true;
                    }

                    if (changed && typed.Length > 0)
                        m_ParticipantId = Mathf.Clamp(int.Parse(typed), 1, 99);
                    if (changed)
                        ShowParticipantScreenText();
                    if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                        break;
                }

                if (m_Panel.TryTakeChoice(out var choice))
                {
                    if (choice == 2)
                        break;

                    typed = "";
                    m_ParticipantId = Mathf.Clamp(m_ParticipantId + (choice == 0 ? -1 : 1), 1, 99);
                    ShowParticipantScreenText();
                }

                yield return null;
            }
        }

        /// <summary>Sets the ID without typing, for the self-test.</summary>
        public void SetParticipantId(int id)
        {
            m_ParticipantId = id;
            ShowParticipantScreenText();
        }

        static bool ReadDigit(Keyboard keyboard, ref string typed)
        {
            var keys = new[]
            {
                keyboard.digit0Key, keyboard.digit1Key, keyboard.digit2Key, keyboard.digit3Key, keyboard.digit4Key,
                keyboard.digit5Key, keyboard.digit6Key, keyboard.digit7Key, keyboard.digit8Key, keyboard.digit9Key,
            };

            for (var digit = 0; digit < keys.Length; digit++)
            {
                if (!keys[digit].wasPressedThisFrame)
                    continue;

                typed = typed.Length >= 2 ? digit.ToString() : typed + digit;
                return true;
            }

            return false;
        }

        void ShowParticipantScreen(string[] buttons)
        {
            m_Panel.transform.position = StationPanelPosition(s_StandQuestions.x);
            m_Panel.Show("Experiment mode", ParticipantText(), buttons, inARow: true);
        }

        void ShowParticipantScreenText() => m_Panel.SetBody(ParticipantText());

        string ParticipantText()
        {
            var plan = Counterbalance.Plan(m_ParticipantId);
            var kind = Counterbalance.IsPilot(m_ParticipantId) ? "pilot run, not study data" : "study participant";
            var warning = StudyLog.HasData(m_ParticipantId) ? "\n<color=#F2A65A>This ID already has data. A new file is written.</color>" : "";
            return $"<b>Participant ID: {m_ParticipantId:00}</b>  <size=75%>({kind})</size>\n" +
                $"Condition order {Counterbalance.OrderIndex(m_ParticipantId) + 1} of {Counterbalance.OrderCount}\n" +
                $"  Block 1: {plan[0]}\n  Block 2: {plan[1]}{warning}";
        }

        void WriteSessionHeader()
        {
            var plan = Counterbalance.Plan(m_ParticipantId);
            m_Log.WriteSession($"participant: {StudyLog.ParticipantTag(m_ParticipantId)}");
            m_Log.WriteSession($"run_type: {m_RunType}");
            m_Log.WriteSession($"order: {Counterbalance.OrderIndex(m_ParticipantId) + 1} of {Counterbalance.OrderCount} ({plan[0]}, then {plan[1]})");
            m_Log.WriteSession($"started: {Clock():yyyy-MM-dd HH:mm:ss}");
            m_Log.WriteSession($"device: {(XRSettings.isDeviceActive ? XRSettings.loadedDeviceName + " / " + XRSettings.eyeTextureWidth + "x" + XRSettings.eyeTextureHeight : "desktop simulator")}");
            m_Log.WriteSession($"unity: {Application.unityVersion}");
        }

        void PrepareEnvironment()
        {
            m_Origin = FindAnyObjectByType<XROrigin>();
            if (m_Origin != null)
            {
                var locomotion = m_Origin.transform.Find("Locomotion");
                m_Locomotion = locomotion != null ? locomotion.gameObject : null;
                if (m_Locomotion != null)
                    m_Locomotion.SetActive(false);
            }

            if (m_HideDuringStudy != null)
            {
                foreach (var go in m_HideDuringStudy)
                {
                    if (go != null)
                        go.SetActive(false);
                }
            }

            foreach (var block in m_Blocks)
            {
                var grab = block.GetComponent<XRGrabInteractable>();
                grab.selectEntered.AddListener(OnGrabbed);
                grab.selectExited.AddListener(OnReleased);
            }

            Stage(s_StandQuestions);
        }

        // ---------------------------------------------------------------- stations

        Vector3 StationPanelPosition(float x) => new Vector3(x, m_Panel.transform.position.y, k_PanelZ);

        /// <summary>Puts the participant at a station, shows only the given blocks and clears the rest.</summary>
        void Stage(Vector2 stand, params (PhysicsBlock block, Vector3 position)[] blocks)
        {
            foreach (var block in m_Blocks)
                block.gameObject.SetActive(false);

            foreach (var (block, position) in blocks)
            {
                block.gameObject.SetActive(true);
                block.MoveTo(position, Quaternion.identity);
                block.GetComponent<XRGrabInteractable>().enabled = true;
                block.ShowMassInLabel(true);
                if (m_Ramp != null)
                    m_Ramp.Forget(block);
                if (m_Grabs.TryGetValue(block, out var record))
                    record.LiftedThisTask = 0f;
            }

            m_Panel.transform.position = StationPanelPosition(stand.x);
            if (m_Origin != null && m_Origin.Camera != null)
            {
                var head = m_Origin.Camera.transform.position;
                m_Origin.MoveCameraToWorldLocation(new Vector3(stand.x, head.y, stand.y));
            }

            SetTask("", Vector3.zero);
        }

        void SetTask(string kind, Vector3 target, params PhysicsBlock[] blocks)
        {
            TaskKind = kind;
            TaskTarget = target;
            TaskBlocks = blocks;
        }

        static Vector3 OnBench(float x, float z, PhysicsBlock block) =>
            new Vector3(x, k_TableTop + block.SideM * 0.5f + 0.002f, z);

        PhysicsBlock Find(MaterialDef material, float massKg)
        {
            foreach (var block in m_Blocks)
            {
                if (block != m_UnmarkedLeft && block != m_UnmarkedRight && block.MaterialName == material.name && Mathf.Abs(block.MassKg - massKg) < 0.01f)
                    return block;
            }

            throw new InvalidOperationException($"No {material.name} block of {massKg} kg in the scene.");
        }

        PhysicsBlock Litre(MaterialDef material) => Find(material, material.densityKgPerM3 * 0.001f);

        // ---------------------------------------------------------------- grabs

        void OnGrabbed(SelectEnterEventArgs args)
        {
            if (!args.interactableObject.transform.TryGetComponent<PhysicsBlock>(out var block))
                return;

            if (!m_Grabs.TryGetValue(block, out var record))
                m_Grabs[block] = record = new GrabRecord();

            record.StartTime = Time.time;
            record.StartHandY = args.interactorObject.transform.position.y;
            record.MaxLift = 0f;
            record.MaxOffset = 0f;
        }

        void OnReleased(SelectExitEventArgs args)
        {
            if (m_Log == null || !args.interactableObject.transform.TryGetComponent<PhysicsBlock>(out var block) || !m_Grabs.TryGetValue(block, out var record))
                return;

            var cue = block.GetComponent<PseudoHapticGrabTransformer>();
            m_Log.WriteInteraction(Stamp(), StudyLog.ParticipantTag(m_ParticipantId), m_RunType, m_BlockNumber, CueText, m_BlockNumber > 0 ? m_BlockSet.ToString() : "", m_Phase, m_Item,
                block.name, block.MassKg, Mathf.RoundToInt((Time.time - record.StartTime) * 1000f), record.MaxLift, record.MaxOffset, cue.CurrentGain, Mathf.RoundToInt(cue.CurrentLagSeconds * 1000f));
        }

        void LateUpdate()
        {
            if (!IsActive)
                return;

            foreach (var pair in m_Grabs)
            {
                var block = pair.Key;
                if (!block.isActiveAndEnabled || !block.TryGetComponent<PseudoHapticGrabTransformer>(out var cue) || !cue.IsHeld)
                    continue;

                var record = pair.Value;
                record.MaxLift = Mathf.Max(record.MaxLift, cue.RawPose.position.y - record.StartHandY);
                record.MaxOffset = Mathf.Max(record.MaxOffset, cue.OffsetM);
                record.LiftedThisTask = Mathf.Max(record.LiftedThisTask, record.MaxLift);
            }
        }

        bool WasLifted(PhysicsBlock block) => m_Grabs.TryGetValue(block, out var record) && record.LiftedThisTask >= k_LiftedM;

        // ---------------------------------------------------------------- generic steps

        string CueText => m_BlockNumber == 0 ? "" : m_BlockCue ? "on" : "off";

        // The wall clock is read once, then advanced by a monotonic timer, so that rows in a
        // session stay in order whatever the system clock or time zone data do mid-session.
        DateTime Clock() => m_ClockStart.AddSeconds(Time.realtimeSinceStartup - m_ClockStartRealtime);

        string Stamp() => Clock().ToString("HH:mm:ss.fff");

        void Record(string prompt, string choice, string correctChoice, bool? correct, float rtMs, float completionMs, int falls, string outcome, string measures)
        {
            m_Log.WriteResponse(Stamp(), StudyLog.ParticipantTag(m_ParticipantId), m_RunType, Counterbalance.OrderIndex(m_ParticipantId) + 1,
                m_BlockNumber, CueText, m_BlockNumber > 0 ? m_BlockSet.ToString() : "", m_Phase, m_Item, prompt, choice, correctChoice,
                correct, Mathf.RoundToInt(rtMs), Mathf.RoundToInt(completionMs), falls, outcome, measures);
        }

        IEnumerator Ask(string title, string body, IReadOnlyList<string> options, bool inARow = false, bool inputEnabled = true)
        {
            m_Panel.Show(title, body, options, inARow, inputEnabled);
            var start = Time.time;
            while (!m_Panel.TryTakeChoice(out m_LastChoice))
                yield return null;
            m_LastRtMs = (Time.time - start) * 1000f;
        }

        IEnumerator Message(string title, string body)
        {
            yield return Ask(title, body, new[] { "Continue" });
        }

        IEnumerator Feedback(string title, string body)
        {
            m_Panel.Show(title, body, Array.Empty<string>());
            yield return new WaitForSeconds(k_FeedbackSeconds);
        }

        /// <summary>
        /// Waits for a physical task. F10 lets the experimenter move on if someone is stuck;
        /// the outcome column records whether the task was done, skipped or timed out.
        /// </summary>
        IEnumerator WaitForTask(Func<bool> done)
        {
            var start = Time.time;
            m_TaskOutcome = "done";
            while (!done())
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.f10Key.wasPressedThisFrame)
                {
                    m_TaskOutcome = "skipped";
                    yield break;
                }

                if (Time.time - start > k_TaskTimeoutSeconds)
                {
                    m_TaskOutcome = "timeout";
                    yield break;
                }

                yield return null;
            }
        }

        static int Falls(params PhysicsBlock[] blocks)
        {
            var falls = 0;
            foreach (var block in blocks)
                falls += block.FallCount;
            return falls;
        }

        // ---------------------------------------------------------------- training

        IEnumerator Training()
        {
            m_BlockNumber = 0;
            m_Phase = "training";
            m_Item = "buttons";
            Stage(s_StandBlocks);
            yield return Message("Welcome", "You will handle blocks and answer questions.\n\n" +
                "To answer, point a controller at a button and squeeze the grip or pull the trigger.\nTry it now.");
            Record("press a button", "Continue", "", null, m_LastRtMs, m_LastRtMs, 0, "done", "");

            m_Item = "grab_and_place";
            var block = m_UnmarkedLeft;
            block.gameObject.SetActive(true);
            block.SetMassKeepingSize(1f);
            var pad = m_TrainingPad != null ? m_TrainingPad.transform.position : OnBench(0.05f, 0.5f, block);
            Stage(s_StandBlocks, (block, OnBench(-0.28f, 0.5f, block)));
            TrainingPadVisible(true);

            var fallsBefore = Falls(block);
            var start = Time.time;
            var target = new Vector3(pad.x, k_TableTop + block.SideM * 0.5f, pad.z);
            m_Panel.Show("Training", "Reach out to the gray block, squeeze the grip to hold it, and lift it.\nSet it down on the green pad, then let go.", Array.Empty<string>());
            SetTask("place", target, block);
            yield return WaitForTask(() => !block.IsHeld && WasLifted(block) && Horizontal(block.transform.position, target) < 0.08f && block.Rigidbody.linearVelocity.magnitude < k_SettleSpeed);
            TrainingPadVisible(false);
            var ms = (Time.time - start) * 1000f;
            Record("place the block on the pad", "", "", null, ms, ms, Falls(block) - fallsBefore, m_TaskOutcome, "");
            SetTask("", Vector3.zero);
            yield return Message("Training done", "That is all you need.\nThe study now begins. Take your time, and answer as best you can.");
        }

        static float Horizontal(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }

        // ---------------------------------------------------------------- blocks

        IEnumerator RunBlock(int number, StudyBlockPlan plan, int total)
        {
            m_BlockNumber = number;
            m_BlockCue = plan.CueOn;
            m_BlockSet = plan.Set;
            PseudoHapticSettings.CueEnabled = plan.CueOn;
            ResetFps();

            var key = plan.ToString();
            m_Scores[key] = new BlockScore();
            m_BlockOrder.Add(key);
            var score = m_Scores[key];
            var random = new System.Random(m_ParticipantId * 1000 + number);

            Stage(s_StandQuestions);
            m_Phase = "break";
            m_Item = "";
            yield return Message($"Part {number} of {total}", "This part has a short quiz, four small tasks, some lifting, the quiz again, and a few questions about how it felt.");

            yield return RunTest("pretest", plan.Set, random, correct => score.PreCorrect += correct);

            var started = Time.time;
            yield return SameMassScene(plan.Set);
            yield return BuoyancyScene(plan.Set);
            yield return RampScene(plan.Set);
            yield return LeverScene(plan.Set);
            score.TaskMs = (Time.time - started) * 1000f;

            yield return HeavinessTrials(random, score);
            yield return RunTest("posttest", plan.Set, random, correct => score.PostCorrect += correct);
            yield return Presence(score);

            WriteFps(number, key);
        }

        IEnumerator RunTest(string phase, ScenarioSet set, System.Random random, Action<int> addCorrect)
        {
            m_Phase = phase;
            Stage(s_StandQuestions);
            var items = StudyDesign.Test(set);
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                m_Item = item.Id;
                var options = StudyDesign.ShuffledOptions(item, random, out var correctIndex);
                yield return Ask($"Quiz  ·  question {i + 1} of {items.Length}", item.Prompt, options);
                var correct = m_LastChoice == correctIndex;
                addCorrect(correct ? 1 : 0);
                Record(item.Prompt, options[m_LastChoice], options[correctIndex], correct, m_LastRtMs, m_LastRtMs, 0, "answered", "");
            }
        }

        // Equal mass, different size: the misconception test made physical. The mass is hidden.
        IEnumerator SameMassScene(ScenarioSet set)
        {
            m_Phase = "scene_same_mass";
            m_Item = set == ScenarioSet.A ? "pine_vs_aluminum" : "ice_vs_steel";
            var a = Find(set == ScenarioSet.A ? MaterialLibrary.Pine : MaterialLibrary.Ice, 1f);
            var b = Find(set == ScenarioSet.A ? MaterialLibrary.Aluminum : MaterialLibrary.Steel, 1f);
            Stage(s_StandBlocks, (a, OnBench(-0.28f, 0.5f, a)), (b, OnBench(0.08f, 0.5f, b)));
            a.ShowMassInLabel(false);
            b.ShowMassInLabel(false);

            var fallsBefore = Falls(a, b);
            var start = Time.time;
            string[] options = { a.MaterialName, b.MaterialName, "They weigh the same" };
            m_Panel.Show("Task  ·  compare", "Lift both blocks. Which one is heavier?", options, inputEnabled: false);
            SetTask("lift", Vector3.zero, a, b);
            yield return WaitForTask(() => WasLifted(a) && WasLifted(b) && !a.IsHeld && !b.IsHeld);
            var outcome = m_TaskOutcome;
            SetTask("", Vector3.zero);

            yield return Ask("Task  ·  compare", "Which one is heavier?", options);
            var total = (Time.time - start) * 1000f;
            Record("which is heavier", options[m_LastChoice], options[2], m_LastChoice == 2, m_LastRtMs, total, Falls(a, b) - fallsBefore, outcome,
                $"size_a_cm={a.SideM * 100f:0.0};size_b_cm={b.SideM * 100f:0.0}");

            a.ShowMassInLabel(true);
            b.ShowMassInLabel(true);
            yield return Feedback("Result", $"Both blocks have a mass of 1 kg.\nThe denser {b.MaterialName.ToLowerInvariant()} is smaller for the same mass.");
        }

        IEnumerator BuoyancyScene(ScenarioSet set)
        {
            m_Phase = "scene_buoyancy";
            var first = Litre(set == ScenarioSet.A ? MaterialLibrary.Ice : MaterialLibrary.Pine);
            var second = Litre(set == ScenarioSet.A ? MaterialLibrary.Aluminum : MaterialLibrary.Steel);
            Stage(s_StandTank, (first, OnBench(0.16f, 0.46f, first)), (second, OnBench(0.31f, 0.46f, second)));
            yield return BuoyancyTrial(first);
            yield return BuoyancyTrial(second);
        }

        IEnumerator BuoyancyTrial(PhysicsBlock block)
        {
            var name = block.MaterialName.ToLowerInvariant();
            m_Item = name;
            var fallsBefore = Falls(block);
            var start = Time.time;
            string[] options = { "It will float", "It will sink" };
            yield return Ask("Task  ·  water tank", $"You will put the {name} block in the water. What will it do?", options);
            var predicted = m_LastChoice;
            var rt = m_LastRtMs;

            var settled = 0f;
            m_Panel.Show("Task  ·  water tank", $"Pick up the {name} block and put it in the water.", Array.Empty<string>());
            var water = m_Water.GetComponent<Collider>().bounds;
            SetTask("place", new Vector3(water.center.x, water.max.y + block.SideM, water.center.z), block);
            yield return WaitForTask(() =>
            {
                var inWater = !block.IsHeld && m_Water.Submerged.ContainsKey(block);
                settled = inWater && block.Rigidbody.linearVelocity.magnitude < k_SettleSpeed ? settled + Time.deltaTime : 0f;
                return settled > 1f;
            });
            SetTask("", Vector3.zero);

            var expected = PhysicsFormulas.SubmergedFractionAtRest(block.DensityKgPerM3, m_Water.FluidDensityKgPerM3);
            var floats = block.DensityKgPerM3 < m_Water.FluidDensityKgPerM3;
            m_Water.Submerged.TryGetValue(block, out var submerged);
            var total = (Time.time - start) * 1000f;
            Record($"will the {name} block float or sink", options[predicted], options[floats ? 0 : 1], predicted == (floats ? 0 : 1), rt, total, Falls(block) - fallsBefore, m_TaskOutcome,
                $"submerged={submerged:0.000};expected={expected:0.000}");

            yield return Feedback("Result", floats
                ? $"The {name} block floats, {submerged * 100f:0}% under the surface.\nIt is less dense than water."
                : $"The {name} block sinks.\nIt is denser than water.");
        }

        IEnumerator RampScene(ScenarioSet set)
        {
            m_Phase = "scene_ramp";
            var ice = Litre(MaterialLibrary.Ice);
            var other = Litre(set == ScenarioSet.A ? MaterialLibrary.Steel : MaterialLibrary.Aluminum);
            var otherName = other.MaterialName.ToLowerInvariant();
            m_Item = "ice_vs_" + otherName;
            Stage(s_StandRamp, (ice, OnBench(1.42f, 0.44f, ice)), (other, OnBench(1.74f, 0.44f, other)));

            var fallsBefore = Falls(ice, other);
            var start = Time.time;
            string[] options = { "The ice block", $"The {otherName} block", "They slide the same distance" };
            yield return Ask("Task  ·  ramp", $"Ice weighs {ice.MassKg:0.00} kg and {otherName} weighs {other.MassKg:0.00} kg. Let go of each at the top of the ramp. Which one will slide farther?", options);
            var predicted = m_LastChoice;
            var rt = m_LastRtMs;

            yield return RampRun(other);
            other.gameObject.SetActive(false);
            var otherDistance = m_Ramp.Finished.TryGetValue(other, out var otherRun) ? otherRun.DistanceM : 0f;
            var outcome = m_TaskOutcome;

            yield return RampRun(ice);
            var iceDistance = m_Ramp.Finished.TryGetValue(ice, out var iceRun) ? iceRun.DistanceM : 0f;
            if (m_TaskOutcome != "done")
                outcome = m_TaskOutcome;

            var total = (Time.time - start) * 1000f;
            Record("which slides farther", options[predicted], options[0], predicted == 0, rt, total, Falls(ice, other) - fallsBefore, outcome,
                $"ice_slid_cm={iceDistance * 100f:0.0};{otherName}_slid_cm={otherDistance * 100f:0.0};ice_seconds={(iceRun != null ? iceRun.MovingSeconds : 0f):0.00};ice_predicted_accel={m_Ramp.PredictedAcceleration(ice):0.00};ice_measured_accel={(iceRun != null ? iceRun.MeasuredAcceleration : 0f):0.00}");

            yield return Feedback("Result", $"Ice slid {iceDistance * 100f:0} cm. {other.MaterialName} slid {otherDistance * 100f:0} cm.\nThe slipperier surface slides farther. Being heavier does not help.");
        }

        IEnumerator RampRun(PhysicsBlock block)
        {
            var name = block.MaterialName.ToLowerInvariant();
            var attempts = 0;
            while (true)
            {
                attempts++;
                m_Ramp.Forget(block);
                m_Panel.Show("Task  ·  ramp", attempts == 1
                    ? $"Set the {name} block on the green line at the top of the ramp, then let go."
                    : $"That was too far from the green line. Put the {name} block on the green line, then let go.", Array.Empty<string>());
                SetTask("ramp", m_Ramp.PointOnSurface(k_RampStartAlongM, block.SideM), block);
                yield return WaitForTask(() => !block.IsHeld && m_Ramp.Finished.ContainsKey(block));
                SetTask("", Vector3.zero);

                if (m_TaskOutcome != "done" || !m_Ramp.Finished.TryGetValue(block, out var run) || run.StartAlongM < k_RampStartAlongM + k_RampStartToleranceM)
                    yield break;
            }
        }

        IEnumerator LeverScene(ScenarioSet set)
        {
            m_Phase = "scene_lever";
            var fixedBlock = Litre(set == ScenarioSet.A ? MaterialLibrary.Aluminum : MaterialLibrary.Steel);
            var moving = Litre(set == ScenarioSet.A ? MaterialLibrary.Ice : MaterialLibrary.Aluminum);
            var fixedArm = set == ScenarioSet.A ? 0.10f : 0.05f;
            var target = PhysicsFormulas.BalancingArmM(fixedBlock.MassKg, fixedArm, moving.MassKg);
            m_Item = $"{moving.MaterialName.ToLowerInvariant()}_vs_{fixedBlock.MaterialName.ToLowerInvariant()}";

            var pivot = m_Lever.transform.position;
            var top = pivot.y + 0.012f;
            Stage(s_StandLever,
                (fixedBlock, new Vector3(pivot.x - fixedArm, top + fixedBlock.SideM * 0.5f + 0.005f, pivot.z)),
                (moving, OnBench(pivot.x + 0.20f, 0.5f, moving)));
            fixedBlock.GetComponent<XRGrabInteractable>().enabled = false;

            var fallsBefore = Falls(moving);
            var start = Time.time;
            string[] options = set == ScenarioSet.A ? new[] { "10 cm", "20 cm", "30 cm" } : new[] { "5 cm", "15 cm", "25 cm" };
            var correctIndex = set == ScenarioSet.A ? 2 : 1;
            yield return Ask("Task  ·  lever",
                $"{fixedBlock.MaterialName} ({fixedBlock.MassKg:0.00} kg) sits {fixedArm * 100f:0} cm left of the pivot. How far to the right will {moving.MaterialName.ToLowerInvariant()} ({moving.MassKg:0.00} kg) balance it?", options);
            var predicted = m_LastChoice;
            var rt = m_LastRtMs;

            var heldStill = 0f;
            var attempts = 0;
            var wasHeld = false;
            m_Panel.Show("Task  ·  lever", $"Put the {moving.MaterialName.ToLowerInvariant()} block on the right side of the plank. Move it until the lever balances.", Array.Empty<string>());
            SetTask("lever", new Vector3(pivot.x + target, top + moving.SideM * 0.5f + 0.03f, pivot.z), moving);
            yield return WaitForTask(() =>
            {
                if (wasHeld && !moving.IsHeld)
                    attempts++;
                wasHeld = moving.IsHeld;
                heldStill = !moving.IsHeld && m_Lever.TorquesMatch(k_LeverTolerance) ? heldStill + Time.deltaTime : 0f;
                return heldStill > k_LeverHoldSeconds;
            });
            SetTask("", Vector3.zero);

            var arm = Vector3.Dot(moving.transform.position - pivot, m_Lever.transform.right);
            var total = (Time.time - start) * 1000f;
            Record("where will the block balance", options[predicted], options[correctIndex], predicted == correctIndex, rt, total, Falls(moving) - fallsBefore, m_TaskOutcome,
                $"final_arm_cm={arm * 100f:0.0};target_arm_cm={target * 100f:0.0};attempts={attempts};left_Nm={m_Lever.LeftTorqueNm:0.00};right_Nm={m_Lever.RightTorqueNm:0.00};tilt_deg={m_Lever.TiltDeg:0.0}");

            yield return Feedback("Result", m_TaskOutcome == "done"
                ? $"Balanced at {arm * 100f:0} cm.\nMass times distance is equal on both sides."
                : $"It balances at {target * 100f:0} cm, where mass times distance is equal on both sides.");
            fixedBlock.GetComponent<XRGrabInteractable>().enabled = true;
        }

        IEnumerator HeavinessTrials(System.Random random, BlockScore score)
        {
            m_Phase = "heaviness";
            var trials = StudyDesign.HeavinessTrials(random);
            string[] options = { "Left block", "Right block" };
            for (var i = 0; i < trials.Count; i++)
            {
                var trial = trials[i];
                m_Item = $"trial_{i + 1}";
                var left = m_UnmarkedLeft;
                var right = m_UnmarkedRight;
                left.gameObject.SetActive(true);
                right.gameObject.SetActive(true);
                left.SetMassKeepingSize(trial.LeftKg);
                right.SetMassKeepingSize(trial.RightKg);
                Stage(s_StandBlocks, (left, OnBench(-0.28f, 0.5f, left)), (right, OnBench(0.08f, 0.5f, right)));

                var fallsBefore = Falls(left, right);
                var start = Time.time;
                m_Panel.Show($"Lifting  ·  pair {i + 1} of {trials.Count}", "These blocks look identical. Lift both. Which one felt heavier?", options, inputEnabled: false);
                SetTask("lift", Vector3.zero, left, right);
                yield return WaitForTask(() => WasLifted(left) && WasLifted(right) && !left.IsHeld && !right.IsHeld);
                var outcome = m_TaskOutcome;
                SetTask("", Vector3.zero);

                yield return Ask($"Lifting  ·  pair {i + 1} of {trials.Count}", "Which one felt heavier?", options);
                var correct = m_LastChoice == trial.HeavierIndex;
                score.HeavinessTotal++;
                if (correct)
                    score.HeavinessCorrect++;

                var total = (Time.time - start) * 1000f;
                Record("which felt heavier", options[m_LastChoice], options[trial.HeavierIndex], correct, m_LastRtMs, total, Falls(left, right) - fallsBefore, outcome,
                    $"left_kg={trial.LeftKg:0.##};right_kg={trial.RightKg:0.##}");
            }
        }

        IEnumerator Presence(BlockScore score)
        {
            m_Phase = "presence";
            Stage(s_StandQuestions);
            var options = new string[StudyDesign.LikertPoints];
            for (var i = 0; i < options.Length; i++)
                options[i] = (i + 1).ToString();

            for (var i = 0; i < StudyDesign.PresenceItems.Length; i++)
            {
                m_Item = $"presence_{i + 1}";
                var prompt = StudyDesign.PresenceItems[i];
                yield return Ask($"How it felt  ·  {i + 1} of {StudyDesign.PresenceItems.Length}", $"{prompt}\n\n<size=80%>1 = strongly disagree     7 = strongly agree</size>", options, inARow: true);
                var value = m_LastChoice + 1;
                Record(prompt, value.ToString(), "", null, m_LastRtMs, m_LastRtMs, 0, "answered", "");

                if (i < StudyDesign.PresenceItems.Length - 1)
                {
                    score.PresenceSum += value;
                    score.PresenceCount++;
                }
                else
                {
                    score.Comfort = value;
                }
            }
        }

        // ---------------------------------------------------------------- end of session

        void ResetFps()
        {
            m_FpsFrames = 0;
            m_FpsSeconds = 0f;
            m_FpsOverBudget = 0;
        }

        void WriteFps(int number, string key)
        {
            var mean = m_FpsSeconds > 0f ? m_FpsFrames / m_FpsSeconds : 0f;
            m_Log.WriteSession($"block {number} ({key}): mean {mean:0.0} fps over {m_FpsSeconds:0} s, {m_FpsOverBudget} of {m_FpsFrames} frames over the {k_FrameBudgetHz} Hz budget, refresh {XRDevice.refreshRate:0} Hz");
        }

        IEnumerator ShowSummary()
        {
            m_Phase = "summary";
            m_Item = "";
            Stage(s_StandQuestions);
            var sb = new StringBuilder();
            foreach (var key in m_BlockOrder)
            {
                var s = m_Scores[key];
                var presence = s.PresenceCount > 0 ? s.PresenceSum / s.PresenceCount : 0f;
                sb.Append("<b>").Append(key).Append("</b>:  quiz ").Append(s.PreCorrect).Append(" then ").Append(s.PostCorrect).Append(" of 6,  heaviness ")
                    .Append(s.HeavinessCorrect).Append(" of ").Append(s.HeavinessTotal).Append(",  presence ").Append(presence.ToString("0.0")).Append('\n');
                m_Log.WriteSession($"summary {key}: pretest {s.PreCorrect}/6, posttest {s.PostCorrect}/6, heaviness {s.HeavinessCorrect}/{s.HeavinessTotal}, presence {presence:0.00}, comfort {s.Comfort:0}, task time {s.TaskMs / 1000f:0.0} s");
            }

            sb.Append("<size=75%>Saved ").Append(m_Log.ResponseRows).Append(" responses and ").Append(m_Log.InteractionRows).Append(" grabs:\n")
                .Append(m_Log.Folder).Append('\n').Append(m_Log.Prefix).Append("_*</size>");
            yield return Ask("Session complete. Thank you!", sb.ToString(), new[] { "Finish" });
        }

        void End(string reason)
        {
            if (m_Run != null)
                StopCoroutine(m_Run);
            m_Run = null;

            if (m_Log != null)
            {
                m_Log.WriteSession($"ended: {Clock():yyyy-MM-dd HH:mm:ss} ({reason}), {Time.time - m_SessionStart:0} s, {m_Log.ResponseRows} responses, {m_Log.InteractionRows} grabs");
                m_Log.Dispose();
                m_Log = null;
            }

            foreach (var block in m_Blocks)
            {
                if (block == null)
                    continue;

                var grab = block.GetComponent<XRGrabInteractable>();
                grab.selectEntered.RemoveListener(OnGrabbed);
                grab.selectExited.RemoveListener(OnReleased);
                grab.enabled = true;

                var unmarked = block == m_UnmarkedLeft || block == m_UnmarkedRight;
                block.gameObject.SetActive(!unmarked);
                if (!unmarked)
                {
                    block.ResetToHome();
                    block.ShowMassInLabel(true);
                }
            }

            if (m_Locomotion != null)
                m_Locomotion.SetActive(true);

            if (m_HideDuringStudy != null)
            {
                foreach (var go in m_HideDuringStudy)
                {
                    if (go != null)
                        go.SetActive(true);
                }
            }

            TrainingPadVisible(false);
            PseudoHapticSettings.CueEnabled = true;
            PseudoHapticSettings.GhostEnabled = true;
            SetTask("", Vector3.zero);
            m_Grabs.Clear();
            m_Scores.Clear();
            m_BlockOrder.Clear();
            m_BlockNumber = 0;
            m_Phase = "";
            m_Item = "";
            m_RunType = "study";
            IsActive = false;

            if (isActiveAndEnabled)
                ShowIdle();
        }

        // Drawn on the desktop only: OnGUI does not reach the headset, so the experimenter and
        // the video can see the condition while the participant cannot.
        void OnGUI()
        {
            if (!IsActive)
                return;

            if (m_StripStyle == null)
            {
                m_StripStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
                var background = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                background.SetPixel(0, 0, new Color(0.05f, 0.06f, 0.08f, 0.88f));
                background.Apply();
                m_StripStyle.normal.background = background;
                m_StripStyle.normal.textColor = new Color(1f, 0.82f, 0.55f);
            }

            var scale = Screen.height / 720f;
            m_StripStyle.fontSize = Mathf.RoundToInt(15f * scale);
            var plan = m_Phase == "setup" ? "ID entry" : m_BlockNumber == 0 ? "training" :
                $"block {m_BlockNumber} of 2  |  cue {(m_BlockCue ? "ON" : "OFF")}  |  set {m_BlockSet}";
            var text = $"  EXPERIMENTER VIEW  |  {StudyLog.ParticipantTag(m_ParticipantId)}  |  order {Counterbalance.OrderIndex(m_ParticipantId) + 1}/{Counterbalance.OrderCount}  |  {plan}  |  {m_Phase} {m_Item}  |  F10 skip task, F12 abort";
            GUI.Box(new Rect(0f, 0f, Screen.width, 28f * scale), text, m_StripStyle);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => IsActive = false;
    }
}
