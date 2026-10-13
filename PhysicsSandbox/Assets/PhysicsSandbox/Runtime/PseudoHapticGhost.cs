using UnityEngine;

namespace PhysicsSandbox
{
    /// <summary>
    /// Debug overlay for demonstrations: while a block is held, draws a line from where the
    /// tracked hand really has it to where it is displayed, so the offset the cue introduces
    /// is visible on a flat recording. Off in the study builds.
    /// </summary>
    [RequireComponent(typeof(PseudoHapticGrabTransformer))]
    public class PseudoHapticGhost : MonoBehaviour
    {
        const float k_MinVisibleOffsetM = 0.004f;
        const float k_LineWidthM = 0.006f;

        [SerializeField] Material m_LineMaterial;

        PseudoHapticGrabTransformer m_Transformer;
        LineRenderer m_Line;

        public Material LineMaterial
        {
            get => m_LineMaterial;
            set => m_LineMaterial = value;
        }

        void Awake()
        {
            m_Transformer = GetComponent<PseudoHapticGrabTransformer>();

            var go = new GameObject(name + " Offset Line");
            m_Line = go.AddComponent<LineRenderer>();
            m_Line.sharedMaterial = m_LineMaterial;
            m_Line.widthMultiplier = k_LineWidthM;
            m_Line.numCapVertices = 4;
            m_Line.useWorldSpace = true;
            m_Line.positionCount = 2;
            m_Line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_Line.receiveShadows = false;
            go.SetActive(false);
        }

        void OnDestroy()
        {
            if (m_Line != null)
                Destroy(m_Line.gameObject);
        }

        void LateUpdate()
        {
            var show = PseudoHapticSettings.GhostEnabled && m_Transformer.IsHeld && m_Transformer.OffsetM > k_MinVisibleOffsetM;
            if (m_Line.gameObject.activeSelf != show)
                m_Line.gameObject.SetActive(show);

            if (!show)
                return;

            m_Line.SetPosition(0, m_Transformer.RawPose.position);
            m_Line.SetPosition(1, transform.position);
        }
    }
}
