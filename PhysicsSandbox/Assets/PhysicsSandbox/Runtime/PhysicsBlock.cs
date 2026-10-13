using System.Collections.Generic;
using UnityEngine;

namespace PhysicsSandbox
{
    /// <summary>
    /// A cube of a given material and mass. Its volume, and so its size, follows from
    /// mass / density, which is how the sandbox makes density visible.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsBlock : MonoBehaviour
    {
        public static readonly List<PhysicsBlock> All = new List<PhysicsBlock>();

        [SerializeField] string m_MaterialName = "Pine";
        [SerializeField] float m_DensityKgPerM3 = 500f;
        [SerializeField] float m_MassKg = 1f;

        Rigidbody m_Rigidbody;
        Collider m_Collider;
        Vector3 m_HomePosition;
        Quaternion m_HomeRotation;

        public string MaterialName => m_MaterialName;
        public float DensityKgPerM3 => m_DensityKgPerM3;
        public float MassKg => m_MassKg;
        public float VolumeM3 => PhysicsFormulas.VolumeM3(m_MassKg, m_DensityKgPerM3);
        public float SideM => PhysicsFormulas.CubeSideM(m_MassKg, m_DensityKgPerM3);
        public Rigidbody Rigidbody => m_Rigidbody != null ? m_Rigidbody : m_Rigidbody = GetComponent<Rigidbody>();
        public Collider Collider => m_Collider != null ? m_Collider : m_Collider = GetComponent<Collider>();

        public Vector3 HomePosition => m_HomePosition;

        /// <summary>True while a hand holds the block (the grab makes it kinematic).</summary>
        public bool IsHeld => Rigidbody.isKinematic;

        public void Configure(MaterialDef material, float massKg)
        {
            m_MaterialName = material.name;
            m_DensityKgPerM3 = material.densityKgPerM3;
            m_MassKg = massKg;
            Rigidbody.mass = massKg;
        }

        void Awake()
        {
            Rigidbody.mass = m_MassKg;
            m_HomePosition = transform.position;
            m_HomeRotation = transform.rotation;
        }

        void OnEnable() => All.Add(this);

        void OnDisable() => All.Remove(this);

        public void ResetToHome()
        {
            if (IsHeld)
                return;

            Rigidbody.linearVelocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            Rigidbody.position = m_HomePosition;
            Rigidbody.rotation = m_HomeRotation;
            transform.SetPositionAndRotation(m_HomePosition, m_HomeRotation);
        }
    }
}
