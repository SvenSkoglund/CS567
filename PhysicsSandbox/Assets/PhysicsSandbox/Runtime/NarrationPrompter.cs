#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.XR.CompositionLayers;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PhysicsSandbox
{
    /// <summary>
    /// Shows narration cue cards in the headset for recording the demo videos. The cards are
    /// an OpenXR composition layer, which the headset compositor adds after Unity has rendered
    /// the eyes, so they never reach the Game view or the video. Press Y for the next card and
    /// hold it for the previous one. Hold the left menu button to start or stop OBS recording;
    /// the card shows whether it is recording. Off by default, and off again after an editor
    /// restart, so a participant can't see it.
    /// </summary>
    public class NarrationPrompter : MonoBehaviour
    {
        const string k_CuesKey = "PhysicsSandbox.PrompterCues";
        const string k_Checkpoint4 = "cues-checkpoint4.txt";
        const string k_Checkpoint5 = "cues-checkpoint5.txt";
        const int k_RenderLayer = 31;
        const int k_TextureWidth = 1536;
        const int k_TextureHeight = 512;
        const float k_PanelWidth = 0.84f;
        const float k_PanelHeight = 0.28f;
        const float k_Distance = 1.2f;
        const float k_DegreesAboveGaze = 19f;
        const float k_FollowRate = 8f;
        const float k_HoldForPreviousSeconds = 0.6f;
        const float k_HoldToRecordSeconds = 1f;

        static NarrationPrompter s_Instance;

        readonly List<(string action, string say)> m_Cards = new();

        Camera m_RenderCamera;
        TextMeshPro m_Text;
        RenderTexture m_Texture;
        Transform m_Layer;
        InputAction m_Next;
        InputAction m_Previous;
        InputAction m_Record;
        ObsRemote m_Obs;
        float m_PressTime;
        float m_RecordPressTime;
        bool m_RecordArmed;
        string m_ShownStatus;
        int m_Card;

        [MenuItem("CS567/Narration Prompter/Checkpoint 4")]
        static void UseCheckpoint4() => SetCues(k_Checkpoint4);

        [MenuItem("CS567/Narration Prompter/Checkpoint 5")]
        static void UseCheckpoint5() => SetCues(k_Checkpoint5);

        [MenuItem("CS567/Narration Prompter/Off")]
        static void TurnOff() => SetCues("");

        public static string SetCues(string fileName)
        {
            SessionState.SetString(k_CuesKey, fileName);
            Debug.Log(string.IsNullOrEmpty(fileName)
                ? "Narration prompter off."
                : $"Narration prompter on: {fileName}. It appears the next time you press Play.");
            return "prompter: " + (string.IsNullOrEmpty(fileName) ? "off" : fileName);
        }

        public static string Status()
        {
            return s_Instance == null
                ? "not running; cues=" + SessionState.GetString(k_CuesKey, "")
                : $"card {s_Instance.m_Card + 1}/{s_Instance.m_Cards.Count}; {s_Instance.m_ShownStatus}";
        }

        public static string ToggleRecording()
        {
            if (s_Instance == null)
                return "not running";

            s_Instance.m_Obs.ToggleRecording();
            return Status();
        }

        public static string Step(string cards)
        {
            if (s_Instance == null)
                return "not running";

            s_Instance.Show(s_Instance.m_Card + int.Parse(cards));
            return Status();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Spawn()
        {
            var fileName = SessionState.GetString(k_CuesKey, "");
            if (string.IsNullOrEmpty(fileName))
                return;

            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../video", fileName));
            if (!File.Exists(path))
            {
                Debug.LogError("Narration prompter: no cue file at " + path);
                return;
            }

            var prompter = new GameObject("Narration Prompter").AddComponent<NarrationPrompter>();
            prompter.Load(path);
        }

        void Load(string path)
        {
            m_Cards.Clear();
            var card = new List<string>();
            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (line.StartsWith("#"))
                    continue;

                if (line.Length > 0)
                    card.Add(line);
                else
                    AddCard(card);
            }

            AddCard(card);
            Show(0);
        }

        void AddCard(List<string> lines)
        {
            if (lines.Count > 0)
                m_Cards.Add((lines[0], string.Join(" ", lines.GetRange(1, lines.Count - 1))));
            lines.Clear();
        }

        void Awake()
        {
            s_Instance = this;

            m_Texture = new RenderTexture(k_TextureWidth, k_TextureHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            m_Texture.Create();

            // Rendered on demand far from the scene, and active only during that one render.
            var renderRoot = new GameObject("Prompter Render") { layer = k_RenderLayer };
            renderRoot.transform.SetParent(transform);
            renderRoot.transform.position = new Vector3(0f, -5000f, 0f);

            m_RenderCamera = renderRoot.AddComponent<Camera>();
            m_RenderCamera.enabled = false;
            m_RenderCamera.orthographic = true;
            m_RenderCamera.orthographicSize = 0.5f;
            m_RenderCamera.nearClipPlane = 0.1f;
            m_RenderCamera.farClipPlane = 5f;
            m_RenderCamera.cullingMask = 1 << k_RenderLayer;
            m_RenderCamera.clearFlags = CameraClearFlags.SolidColor;
            m_RenderCamera.backgroundColor = new Color(0.04f, 0.06f, 0.09f, 1f);
            m_RenderCamera.targetTexture = m_Texture;

            var textObject = new GameObject("Prompter Text") { layer = k_RenderLayer };
            textObject.transform.SetParent(renderRoot.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0f, 2f);
            m_Text = textObject.AddComponent<TextMeshPro>();
            m_Text.rectTransform.sizeDelta = new Vector2(2.84f, 0.88f);
            m_Text.alignment = TextAlignmentOptions.MidlineLeft;
            m_Text.textWrappingMode = TextWrappingModes.Normal;
            m_Text.enableAutoSizing = true;
            m_Text.fontSizeMin = 0.8f;
            m_Text.fontSizeMax = 1.5f;
            m_Text.color = Color.white;
            textObject.SetActive(false);

            var layerObject = new GameObject("Prompter Layer");
            layerObject.SetActive(false);
            layerObject.transform.SetParent(transform);
            var layer = layerObject.AddComponent<CompositionLayer>();
            layer.ChangeLayerDataType<QuadLayerData>();
            ((QuadLayerData)layer.LayerData).Size = new Vector2(k_PanelWidth, k_PanelHeight);
            layerObject.AddComponent<TexturesExtension>().LeftTexture = m_Texture;
            layerObject.SetActive(true);
            layer.Order = 10;
            m_Layer = layerObject.transform;

            m_Next = new InputAction("Prompter Next", InputActionType.Button);
            m_Next.AddBinding("<XRController>{LeftHand}/secondaryButton");
            m_Next.AddBinding("<Keyboard>/pageDown");
            m_Previous = new InputAction("Prompter Previous", InputActionType.Button, "<Keyboard>/pageUp");
            m_Record = new InputAction("Prompter Record", InputActionType.Button, "<XRController>{LeftHand}/menuButton");
            m_Next.Enable();
            m_Previous.Enable();
            m_Record.Enable();

            m_Obs = new ObsRemote();
        }

        void Update()
        {
            if (m_Next.WasPressedThisFrame())
                m_PressTime = Time.unscaledTime;

            if (m_Next.WasReleasedThisFrame())
                Show(m_Card + (Time.unscaledTime - m_PressTime >= k_HoldForPreviousSeconds ? -1 : 1));

            if (m_Previous.WasPressedThisFrame())
                Show(m_Card - 1);

            if (m_Record.WasPressedThisFrame())
            {
                m_RecordPressTime = Time.unscaledTime;
                m_RecordArmed = true;
            }

            if (m_RecordArmed && !m_Record.IsPressed())
                m_RecordArmed = false;

            if (m_RecordArmed && Time.unscaledTime - m_RecordPressTime >= k_HoldToRecordSeconds)
            {
                m_RecordArmed = false;
                m_Obs.ToggleRecording();
            }

            if (StatusLine() != m_ShownStatus)
                Show(m_Card);
        }

        string StatusLine()
        {
            var cue = PseudoHapticSettings.CueEnabled ? "<color=#7CD992>cue ON</color>" : "<color=#FFD060>cue OFF</color>";
            return $"{RecordingStatus()}   {cue}";
        }

        string RecordingStatus()
        {
            if (!m_Obs.Connected)
                return "<color=#8FA0B0>OBS not connected</color>";
            if (m_RecordArmed)
                return $"<color=#FFD060>hold to {(m_Obs.Recording ? "stop" : "record")}...</color>";
            if (!m_Obs.Recording)
                return "<color=#8FA0B0>not recording</color>";

            var seconds = m_Obs.RecordingSeconds;
            return $"<color=#FF5A5A>REC {seconds / 60:00}:{seconds % 60:00}</color>";
        }

        void LateUpdate()
        {
            var head = Camera.main;
            if (head == null)
                return;

            var gaze = head.transform.rotation * Quaternion.Euler(-k_DegreesAboveGaze, 0f, 0f);
            var targetPosition = head.transform.position + gaze * Vector3.forward * k_Distance;
            var blend = 1f - Mathf.Exp(-k_FollowRate * Time.unscaledDeltaTime);
            var position = Vector3.Lerp(m_Layer.position, targetPosition, blend);
            m_Layer.SetPositionAndRotation(position, Quaternion.LookRotation(position - head.transform.position, head.transform.up));
        }

        void Show(int card)
        {
            if (m_Cards.Count == 0)
                return;

            m_Card = Mathf.Clamp(card, 0, m_Cards.Count - 1);
            var (action, say) = m_Cards[m_Card];
            m_ShownStatus = StatusLine();
            m_Text.text = $"<size=72%>{m_ShownStatus}   <color=#F0B060>{m_Card + 1}/{m_Cards.Count}  {action}</color></size>\n{say}";

            m_Text.gameObject.SetActive(true);
            m_Text.ForceMeshUpdate();
            m_RenderCamera.Render();
            m_Text.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            s_Instance = null;
            m_Next?.Dispose();
            m_Previous?.Dispose();
            m_Record?.Dispose();
            m_Obs?.Dispose();
            if (m_Texture != null)
                m_Texture.Release();
        }
    }
}
#endif
