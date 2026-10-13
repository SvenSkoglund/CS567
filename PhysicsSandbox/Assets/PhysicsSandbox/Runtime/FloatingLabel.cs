using UnityEngine;

namespace PhysicsSandbox
{
    /// <summary>
    /// Keeps a text label hovering above its parent and turned toward the viewer,
    /// whatever the parent's rotation.
    /// </summary>
    public class FloatingLabel : MonoBehaviour
    {
        [SerializeField] float m_HeightAboveParent = 0.1f;

        Transform m_Camera;

        public float HeightAboveParent
        {
            get => m_HeightAboveParent;
            set => m_HeightAboveParent = value;
        }

        void LateUpdate()
        {
            var parent = transform.parent;
            if (parent == null)
                return;

            if (m_Camera == null)
            {
                var main = Camera.main;
                if (main == null)
                    return;
                m_Camera = main.transform;
            }

            var position = parent.position + Vector3.up * m_HeightAboveParent;
            var away = position - m_Camera.position;
            away.y = 0f;
            var rotation = away.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(away, Vector3.up) : transform.rotation;
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
