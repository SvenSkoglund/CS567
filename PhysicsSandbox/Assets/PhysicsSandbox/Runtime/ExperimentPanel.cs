using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PhysicsSandbox
{
    /// <summary>
    /// The panel the participant reads and answers on. Shows a title, a body and up to seven
    /// buttons. F1 to F7 press the buttons too, for the simulator and for the experimenter.
    /// </summary>
    public class ExperimentPanel : MonoBehaviour
    {
        const float k_ColumnWidth = 1.02f;
        const float k_ColumnHeight = 0.082f;
        const float k_ColumnPitch = 0.094f;
        const float k_ColumnTopY = -0.04f;
        const float k_RowY = -0.20f;
        const float k_RowPitchMax = 0.30f;
        const float k_RowHeight = 0.11f;
        const float k_IgnorePressSeconds = 0.25f;

        [SerializeField] TextMeshPro m_Title;
        [SerializeField] TextMeshPro m_Body;
        [SerializeField] PanelButton[] m_Buttons;

        InputAction m_Trigger;
        int m_Choice = -1;
        int m_OptionCount;
        bool m_InputEnabled;
        float m_ShownAt;
        float m_BaseY;

        public TextMeshPro Title
        {
            get => m_Title;
            set => m_Title = value;
        }

        public TextMeshPro Body
        {
            get => m_Body;
            set => m_Body = value;
        }

        public PanelButton[] Buttons
        {
            get => m_Buttons;
            set => m_Buttons = value;
        }

        public int OptionCount => m_OptionCount;
        public bool InputEnabled => m_InputEnabled;
        public string TitleText => m_Title.text;
        public string BodyText => m_Body.text;

        void Awake()
        {
            m_BaseY = transform.position.y;
            m_Trigger = new InputAction("Panel Trigger", InputActionType.Button);
            m_Trigger.AddBinding("<XRController>{RightHand}/{TriggerButton}");
            m_Trigger.AddBinding("<XRController>{LeftHand}/{TriggerButton}");

            for (var i = 0; i < m_Buttons.Length; i++)
            {
                var index = i;
                m_Buttons[i].Pressed += () => Press(index);
            }
        }

        void OnEnable() => m_Trigger.Enable();

        void OnDisable() => m_Trigger.Disable();

        /// <summary>Moves the panel behind the station being used.</summary>
        public void MoveToStation(float x, float z) =>
            transform.position = new Vector3(x, m_BaseY, z);

        public void Show(string title, string body, IReadOnlyList<string> options, bool inARow = false, bool inputEnabled = true)
        {
            m_Title.text = title;
            m_Body.text = body;
            m_Choice = -1;
            m_OptionCount = options.Count;
            m_InputEnabled = inputEnabled;
            m_ShownAt = Time.unscaledTime;

            for (var i = 0; i < m_Buttons.Length; i++)
            {
                if (i >= options.Count)
                {
                    m_Buttons[i].gameObject.SetActive(false);
                    continue;
                }

                if (inARow)
                {
                    var pitch = Mathf.Min(k_RowPitchMax, k_ColumnWidth / options.Count);
                    var x = (i - (options.Count - 1) * 0.5f) * pitch;
                    m_Buttons[i].Setup(options[i], new Vector2(x, k_RowY), new Vector2(pitch - 0.02f, k_RowHeight), inputEnabled);
                }
                else
                {
                    m_Buttons[i].Setup(options[i], new Vector2(0f, k_ColumnTopY - i * k_ColumnPitch), new Vector2(k_ColumnWidth, k_ColumnHeight), inputEnabled);
                }
            }
        }

        public void SetBody(string body) => m_Body.text = body;

        public void SetInputEnabled(bool value)
        {
            m_InputEnabled = value;
            for (var i = 0; i < m_OptionCount; i++)
                m_Buttons[i].SetInputEnabled(value);
        }

        public void Press(int index)
        {
            if (!m_InputEnabled || index < 0 || index >= m_OptionCount || Time.unscaledTime < m_ShownAt + k_IgnorePressSeconds)
                return;
            m_Choice = index;
        }

        /// <summary>Returns the pressed button once, or false if none has been pressed.</summary>
        public bool TryTakeChoice(out int index)
        {
            index = m_Choice;
            m_Choice = -1;
            return index >= 0;
        }

        void Update()
        {
            if (m_Trigger.WasPressedThisFrame())
            {
                for (var i = 0; i < m_OptionCount; i++)
                {
                    if (m_Buttons[i].IsHovered)
                        Press(i);
                }
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.f1Key.wasPressedThisFrame) Press(0);
            if (keyboard.f2Key.wasPressedThisFrame) Press(1);
            if (keyboard.f3Key.wasPressedThisFrame) Press(2);
            if (keyboard.f4Key.wasPressedThisFrame) Press(3);
            if (keyboard.f5Key.wasPressedThisFrame) Press(4);
            if (keyboard.f6Key.wasPressedThisFrame) Press(5);
            if (keyboard.f7Key.wasPressedThisFrame) Press(6);
        }
    }
}
