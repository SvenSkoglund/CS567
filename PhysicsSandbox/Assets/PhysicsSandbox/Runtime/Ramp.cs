using System.Collections.Generic;
using UnityEngine;

namespace PhysicsSandbox
{
    /// <summary>
    /// One release of a block on the ramp: where it started, how far it slid and for how long.
    /// </summary>
    public class SlideRun
    {
        public PhysicsBlock Block;
        public float StartAlongM;
        public float EndAlongM;
        public float MovingSeconds;
        public bool ReachedStop;

        public float DistanceM => Mathf.Max(0f, EndAlongM - StartAlongM);

        /// <summary>Mean acceleration over the slide, from distance = a t^2 / 2.</summary>
        public float MeasuredAcceleration => MovingSeconds > 0.05f ? 2f * DistanceM / (MovingSeconds * MovingSeconds) : 0f;
    }

    /// <summary>
    /// A fixed incline for the friction lesson. Tracks each block released on it and reports
    /// how far it slid, so the outcome can be compared with the closed-form prediction: a
    /// block slides when tan(angle) exceeds the static friction, at g (sin - mu cos).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class Ramp : MonoBehaviour
    {
        const float k_ProbeHeight = 0.03f;
        const float k_MovingSpeed = 0.015f;
        const float k_RestSpeed = 0.02f;
        const float k_RestSeconds = 0.5f;
        const float k_StopPlateM = 0.02f;

        readonly Dictionary<PhysicsBlock, SlideRun> m_Running = new Dictionary<PhysicsBlock, SlideRun>();
        readonly Dictionary<PhysicsBlock, float> m_RestTime = new Dictionary<PhysicsBlock, float>();
        readonly HashSet<PhysicsBlock> m_Touching = new HashSet<PhysicsBlock>();
        readonly List<PhysicsBlock> m_Scratch = new List<PhysicsBlock>();
        readonly Collider[] m_Hits = new Collider[16];
        BoxCollider m_Box;

        /// <summary>The most recent finished run of each block.</summary>
        public readonly Dictionary<PhysicsBlock, SlideRun> Finished = new Dictionary<PhysicsBlock, SlideRun>();

        /// <summary>The latest finished run of any block, for the readout panel.</summary>
        public SlideRun LastRun { get; private set; }

        public float AngleDeg => Vector3.Angle(transform.up, Vector3.up);
        public float LengthM => Size.x;

        // The surface is a scaled unit cube, so its real dimensions come from the transform.
        Vector3 Size => Vector3.Scale(m_Box.size, transform.lossyScale);
        public float SurfaceFrictionStatic => m_Box.sharedMaterial != null ? m_Box.sharedMaterial.staticFriction : 0.6f;
        public float SurfaceFrictionDynamic => m_Box.sharedMaterial != null ? m_Box.sharedMaterial.dynamicFriction : 0.6f;

        /// <summary>Distance of a world position along the slope, measured down from the top edge.</summary>
        public float AlongSlope(Vector3 worldPosition) =>
            Vector3.Dot(worldPosition - transform.position, transform.right) + Size.x * 0.5f;

        /// <summary>World position of a block of the given size resting on the slope.</summary>
        public Vector3 PointOnSurface(float alongM, float blockSideM) =>
            transform.position + transform.right * (alongM - Size.x * 0.5f) + transform.up * (Size.y * 0.5f + blockSideM * 0.5f);

        public bool PredictsSlide(PhysicsBlock block) =>
            PhysicsFormulas.WillSlide(AngleDeg, PhysicsFormulas.CombinedFriction(block.StaticFriction, SurfaceFrictionStatic));

        public float PredictedAcceleration(PhysicsBlock block) => PredictsSlide(block)
            ? PhysicsFormulas.SlideAcceleration(AngleDeg, PhysicsFormulas.CombinedFriction(block.DynamicFriction, SurfaceFrictionDynamic), Physics.gravity.magnitude)
            : 0f;

        public void Forget(PhysicsBlock block)
        {
            m_Running.Remove(block);
            m_RestTime.Remove(block);
            Finished.Remove(block);
        }

        void Awake() => m_Box = GetComponent<BoxCollider>();

        // As on the lever, an overlap query finds what rests on the surface, because resting
        // bodies fall asleep and stop reporting contacts.
        void FixedUpdate()
        {
            var size = Size;
            var center = transform.position + transform.up * (size.y * 0.5f + k_ProbeHeight * 0.5f);
            var halfExtents = new Vector3(size.x * 0.5f, k_ProbeHeight * 0.5f, size.z * 0.5f);
            var hits = Physics.OverlapBoxNonAlloc(center, halfExtents, m_Hits, transform.rotation, ~0, QueryTriggerInteraction.Ignore);

            m_Touching.Clear();
            for (var i = 0; i < hits; i++)
            {
                var body = m_Hits[i].attachedRigidbody;
                if (body != null && body.TryGetComponent<PhysicsBlock>(out var block) && !block.IsHeld)
                    m_Touching.Add(block);
            }

            foreach (var block in m_Touching)
            {
                if (m_Running.ContainsKey(block))
                    continue;

                // A block already reported stays reported until it is picked up again.
                var along = AlongSlope(block.transform.position);
                if (Finished.TryGetValue(block, out var done) && Mathf.Abs(along - done.EndAlongM) < 0.01f)
                    continue;

                m_Running[block] = new SlideRun { Block = block, StartAlongM = along, EndAlongM = along };
                m_RestTime[block] = 0f;
            }

            m_Scratch.Clear();
            m_Scratch.AddRange(m_Running.Keys);
            foreach (var block in m_Scratch)
                Advance(block);
        }

        void Advance(PhysicsBlock block)
        {
            var run = m_Running[block];

            // Picked up or knocked off before coming to rest: the run does not count.
            if (block.IsHeld || !block.isActiveAndEnabled || !m_Touching.Contains(block))
            {
                m_Running.Remove(block);
                m_RestTime.Remove(block);
                return;
            }

            // Timing stops at the stop plate, so a settling bounce does not count as sliding.
            run.EndAlongM = AlongSlope(block.transform.position);
            run.ReachedStop |= run.EndAlongM + block.SideM * 0.5f > Size.x - k_StopPlateM - 0.002f;
            var speed = block.Rigidbody.linearVelocity.magnitude;
            if (speed > k_MovingSpeed && !run.ReachedStop)
                run.MovingSeconds += Time.fixedDeltaTime;

            m_RestTime[block] = speed < k_RestSpeed ? m_RestTime[block] + Time.fixedDeltaTime : 0f;
            if (m_RestTime[block] < k_RestSeconds)
                return;

            Finished[block] = run;
            LastRun = run;
            m_Running.Remove(block);
            m_RestTime.Remove(block);
        }
    }
}
