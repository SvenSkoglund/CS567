using UnityEngine;

namespace PhysicsSandbox
{
    /// <summary>
    /// Debug overlay for demonstrations: while a block is held, draws a wireframe cube where
    /// the tracked hand really has it and a tether to where it is displayed, so the offset
    /// the cue introduces is visible on a flat recording. Off in the study builds.
    /// </summary>
    [RequireComponent(typeof(PseudoHapticGrabTransformer))]
    public class PseudoHapticGhost : MonoBehaviour
    {
        const float k_MinVisibleOffsetM = 0.004f;

        [SerializeField] Material m_LineMaterial;

        PseudoHapticGrabTransformer m_Transformer;
        PhysicsBlock m_Block;
        GameObject m_Root;
        LineRenderer m_Wire;
        LineRenderer m_Tether;

        public Material LineMaterial
        {
            get => m_LineMaterial;
            set => m_LineMaterial = value;
        }

        void Awake()
        {
            m_Transformer = GetComponent<PseudoHapticGrabTransformer>();
            m_Block = GetComponent<PhysicsBlock>();

            m_Root = new GameObject(name + " Ghost");
            m_Wire = CreateLine("Wire", 0.003f);
            m_Wire.useWorldSpace = false;
            m_Wire.positionCount = k_CubePath.Length;
            m_Wire.SetPositions(k_CubePath);

            m_Tether = CreateLine("Tether", 0.004f);
            m_Tether.useWorldSpace = true;
            m_Tether.positionCount = 2;

            m_Root.SetActive(false);
        }

        void OnDestroy()
        {
            if (m_Root != null)
                Destroy(m_Root);
        }

        LineRenderer CreateLine(string lineName, float width)
        {
            var go = new GameObject(lineName);
            go.transform.SetParent(m_Root.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = m_LineMaterial;
            line.widthMultiplier = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        void LateUpdate()
        {
            var show = PseudoHapticSettings.GhostEnabled && m_Transformer.IsHeld && m_Transformer.OffsetM > k_MinVisibleOffsetM;
            if (m_Root.activeSelf != show)
                m_Root.SetActive(show);

            if (!show)
                return;

            var raw = m_Transformer.RawPose;
            var side = m_Block != null ? m_Block.SideM : 0.1f;
            m_Wire.transform.SetPositionAndRotation(raw.position, raw.rotation);
            m_Wire.transform.localScale = Vector3.one * side;
            m_Tether.SetPosition(0, raw.position);
            m_Tether.SetPosition(1, transform.position);
        }

        // One continuous path that covers all twelve edges of a unit cube.
        static readonly Vector3[] k_CubePath =
        {
            new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
        };
    }
}
