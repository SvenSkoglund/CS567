using System;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PhysicsSandbox
{
    /// <summary>
    /// A world-space answer button: point a controller at it and squeeze the grip, the same
    /// gesture that grabs a block, or pull the trigger. Built on a plain interactable with a
    /// collider, so it works identically with the headset and the desktop simulator.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable), typeof(BoxCollider))]
    public class PanelButton : MonoBehaviour
    {
        static readonly int s_BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly Color s_Idle = new Color(0.17f, 0.24f, 0.33f);
        static readonly Color s_Hover = new Color(0.20f, 0.52f, 0.72f);
        static readonly Color s_Disabled = new Color(0.12f, 0.13f, 0.15f);

        [SerializeField] TextMeshPro m_Label;
        [SerializeField] Renderer m_Background;

        MaterialPropertyBlock m_Block;
        XRSimpleInteractable m_Interactable;
        BoxCollider m_Collider;
        Color m_LastColor;
        bool m_InputEnabled = true;

        public event Action Pressed;

        public XRSimpleInteractable Interactable => m_Interactable;
        public bool IsHovered => m_InputEnabled && m_Interactable.isHovered;

        public TextMeshPro Label
        {
            get => m_Label;
            set => m_Label = value;
        }

        public Renderer Background
        {
            get => m_Background;
            set => m_Background = value;
        }

        void Awake()
        {
            m_Block = new MaterialPropertyBlock();
            m_Interactable = GetComponent<XRSimpleInteractable>();
            m_Collider = GetComponent<BoxCollider>();
            m_Interactable.selectEntered.AddListener(_ =>
            {
                if (m_InputEnabled)
                    Pressed?.Invoke();
            });
        }

        public void Setup(string text, Vector2 localPosition, Vector2 size, bool inputEnabled)
        {
            gameObject.SetActive(true);
            transform.localPosition = new Vector3(localPosition.x, localPosition.y, -0.01f);
            m_Background.transform.localScale = new Vector3(size.x, size.y, 1f);
            m_Collider.size = new Vector3(size.x, size.y, 0.02f);
            m_Label.rectTransform.sizeDelta = size - new Vector2(0.03f, 0.01f);
            m_Label.text = text;
            SetInputEnabled(inputEnabled);
        }

        public void SetInputEnabled(bool value)
        {
            m_InputEnabled = value;
            m_Label.color = value ? Color.white : new Color(0.45f, 0.48f, 0.52f);
        }

        void Update()
        {
            var color = !m_InputEnabled ? s_Disabled : m_Interactable.isHovered ? s_Hover : s_Idle;
            if (color == m_LastColor)
                return;

            m_LastColor = color;
            m_Background.GetPropertyBlock(m_Block);
            m_Block.SetColor(s_BaseColor, color);
            m_Background.SetPropertyBlock(m_Block);
        }
    }
}
