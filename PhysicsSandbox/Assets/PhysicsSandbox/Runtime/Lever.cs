using System.Collections.Generic;
using UnityEngine;

namespace PhysicsSandbox
{
    /// <summary>
    /// A plank on a hinge. Reports the torque each side's load applies about the pivot
    /// (mass x g x horizontal arm) so the learner can compare the numbers with what the
    /// plank does: it balances when the two torques match.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public class Lever : MonoBehaviour
    {
        const float k_BalancedAngleDeg = 1.5f;
        const float k_ProbeHeight = 0.03f;

        [SerializeField] Collider[] m_IgnoreCollisionWith;

        readonly HashSet<PhysicsBlock> m_Loads = new HashSet<PhysicsBlock>();
        readonly Collider[] m_Hits = new Collider[16];
        BoxCollider m_Box;

        public float LeftTorqueNm { get; private set; }
        public float RightTorqueNm { get; private set; }
        public int LoadCount { get; private set; }

        /// <summary>Tilt about the hinge in degrees; positive when the right end is down.</summary>
        public float TiltDeg { get; private set; }

        public bool IsBalanced => LoadCount > 0 && Mathf.Abs(TiltDeg) < k_BalancedAngleDeg;

        public Collider[] IgnoreCollisionWith
        {
            get => m_IgnoreCollisionWith;
            set => m_IgnoreCollisionWith = value;
        }

        void Awake()
        {
            m_Box = GetComponent<BoxCollider>();
            if (m_IgnoreCollisionWith == null)
                return;

            foreach (var other in m_IgnoreCollisionWith)
            {
                if (other != null)
                    Physics.IgnoreCollision(m_Box, other);
            }
        }

        // Loads are found with an overlap query just above the plank rather than collision
        // callbacks, because resting bodies fall asleep and stop reporting contacts.
        void FixedUpdate()
        {
            var right = transform.right;
            right.y = 0f;
            right.Normalize();

            var left = 0f;
            var rightTorque = 0f;
            var count = 0;
            var g = Physics.gravity.magnitude;

            var size = m_Box.size;
            var center = transform.TransformPoint(m_Box.center + Vector3.up * (size.y * 0.5f + k_ProbeHeight * 0.5f));
            var halfExtents = new Vector3(size.x * 0.5f, k_ProbeHeight * 0.5f, size.z * 0.5f);
            var hits = Physics.OverlapBoxNonAlloc(center, halfExtents, m_Hits, transform.rotation, ~0, QueryTriggerInteraction.Ignore);

            m_Loads.Clear();
            for (var i = 0; i < hits; i++)
            {
                var body = m_Hits[i].attachedRigidbody;
                if (body == null || !body.TryGetComponent<PhysicsBlock>(out var block) || block.IsHeld || !m_Loads.Add(block))
                    continue;

                var arm = Vector3.Dot(body.worldCenterOfMass - transform.position, right);
                var torque = block.MassKg * g * Mathf.Abs(arm);
                if (arm < 0f)
                    left += torque;
                else
                    rightTorque += torque;
                count++;
            }

            LeftTorqueNm = left;
            RightTorqueNm = rightTorque;
            LoadCount = count;
            TiltDeg = -Mathf.DeltaAngle(0f, transform.eulerAngles.z);
        }
    }
}
