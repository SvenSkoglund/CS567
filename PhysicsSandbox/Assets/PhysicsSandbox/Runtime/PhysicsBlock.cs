using System.Collections.Generic;
using TMPro;
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

        const float k_FallenBelowM = 0.3f;

        [SerializeField] string m_MaterialName = "Pine";
        [SerializeField] float m_DensityKgPerM3 = 500f;
        [SerializeField] float m_MassKg = 1f;

        Rigidbody m_Rigidbody;
        Collider m_Collider;
        Vector3 m_HomePosition;
        Quaternion m_HomeRotation;
        Vector3 m_ReturnPosition;
        Quaternion m_ReturnRotation;

        public string MaterialName => m_MaterialName;
        public float DensityKgPerM3 => m_DensityKgPerM3;
        public float MassKg => m_MassKg;
        public float VolumeM3 => PhysicsFormulas.VolumeM3(m_MassKg, m_DensityKgPerM3);
        public float SideM => PhysicsFormulas.CubeSideM(m_MassKg, m_DensityKgPerM3);
        public Rigidbody Rigidbody => m_Rigidbody != null ? m_Rigidbody : m_Rigidbody = GetComponent<Rigidbody>();
        public Collider Collider => m_Collider != null ? m_Collider : m_Collider = GetComponent<Collider>();

        public Vector3 HomePosition => m_HomePosition;

        /// <summary>How many times the block has fallen to the floor and been returned.</summary>
        public int FallCount { get; private set; }

        /// <summary>Friction values the physics engine uses for this block.</summary>
        public float StaticFriction => Collider.sharedMaterial != null ? Collider.sharedMaterial.staticFriction : 0.6f;
        public float DynamicFriction => Collider.sharedMaterial != null ? Collider.sharedMaterial.dynamicFriction : 0.6f;

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
            m_ReturnPosition = m_HomePosition;
            m_ReturnRotation = m_HomeRotation;
        }

        void OnEnable() => All.Add(this);

        void OnDisable() => All.Remove(this);

        public void ResetToHome() => MoveTo(m_HomePosition, m_HomeRotation);

        public void MoveTo(Vector3 position, Quaternion rotation)
        {
            if (IsHeld)
                return;

            Rigidbody.linearVelocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            Rigidbody.position = position;
            Rigidbody.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);

            // Moving a sleeping body does not wake it, and it would hang where it was put.
            Rigidbody.WakeUp();
            m_ReturnPosition = position;
            m_ReturnRotation = rotation;
        }

        /// <summary>Sets the mass of a block whose size stays fixed, as for the unmarked study blocks.</summary>
        public void SetMassKeepingSize(float massKg)
        {
            m_DensityKgPerM3 = massKg / VolumeM3;
            m_MassKg = massKg;
            Rigidbody.mass = massKg;
        }

        /// <summary>The study hides the mass when it is what the participant is asked to judge.</summary>
        public void ShowMassInLabel(bool show)
        {
            var label = GetComponentInChildren<TextMeshPro>(true);
            if (label != null)
                label.text = show ? $"{m_MaterialName}\n{m_MassKg:0.00} kg" : m_MaterialName;
        }

        // A block that lands on the floor is out of reach, so it comes back to its last placement.
        void FixedUpdate()
        {
            if (IsHeld || transform.position.y >= k_FallenBelowM)
                return;

            FallCount++;
            MoveTo(m_ReturnPosition, m_ReturnRotation);
        }
    }
}
