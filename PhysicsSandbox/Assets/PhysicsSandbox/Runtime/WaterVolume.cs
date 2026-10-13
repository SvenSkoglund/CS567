using System.Collections.Generic;
using UnityEngine;

namespace PhysicsSandbox
{
    /// <summary>
    /// Archimedes buoyancy for <see cref="PhysicsBlock"/>s inside a box of fluid. The upward
    /// force is fluid density x g x submerged volume, so a block floats with the fraction
    /// density / fluid density below the surface, or sinks when it is denser than the fluid.
    /// Displacement of the fluid level is not modelled.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class WaterVolume : MonoBehaviour
    {
        [SerializeField] float m_FluidDensityKgPerM3 = MaterialLibrary.WaterDensityKgPerM3;

        [Tooltip("Velocity damping per second for a fully submerged body; stands in for viscous drag.")]
        [SerializeField] float m_Damping = 4f;

        BoxCollider m_Box;

        public float FluidDensityKgPerM3
        {
            get => m_FluidDensityKgPerM3;
            set => m_FluidDensityKgPerM3 = value;
        }

        /// <summary>Blocks currently in the fluid and the fraction of each that is submerged.</summary>
        public readonly Dictionary<PhysicsBlock, float> Submerged = new Dictionary<PhysicsBlock, float>();

        void Awake() => m_Box = GetComponent<BoxCollider>();

        void FixedUpdate()
        {
            Submerged.Clear();
            var water = m_Box.bounds;
            var surfaceY = water.max.y;

            foreach (var block in PhysicsBlock.All)
            {
                var bounds = block.Collider.bounds;
                var center = bounds.center;
                if (center.x < water.min.x || center.x > water.max.x || center.z < water.min.z || center.z > water.max.z)
                    continue;
                if (bounds.min.y >= surfaceY || bounds.max.y < water.min.y)
                    continue;

                var fraction = Mathf.Clamp01((surfaceY - bounds.min.y) / bounds.size.y);
                Submerged[block] = fraction;

                if (block.IsHeld)
                    continue;

                var body = block.Rigidbody;
                var buoyancy = m_FluidDensityKgPerM3 * block.VolumeM3 * fraction * -Physics.gravity;
                body.AddForce(buoyancy, ForceMode.Force);

                var keep = Mathf.Clamp01(1f - m_Damping * fraction * Time.fixedDeltaTime);
                body.linearVelocity *= keep;
                body.angularVelocity *= keep;
            }
        }
    }
}
