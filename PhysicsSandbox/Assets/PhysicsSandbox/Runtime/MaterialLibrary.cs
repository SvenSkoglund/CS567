using UnityEngine;

namespace PhysicsSandbox
{
    /// <summary>
    /// A physical material the learner can spawn. Density is the only quantity that
    /// distinguishes materials for the lessons; friction feeds the physics engine.
    /// </summary>
    public readonly struct MaterialDef
    {
        public readonly string name;
        public readonly float densityKgPerM3;
        public readonly float friction;
        public readonly Color color;
        public readonly float metallic;
        public readonly float smoothness;

        public MaterialDef(string name, float densityKgPerM3, float friction, Color color, float metallic, float smoothness)
        {
            this.name = name;
            this.densityKgPerM3 = densityKgPerM3;
            this.friction = friction;
            this.color = color;
            this.metallic = metallic;
            this.smoothness = smoothness;
        }
    }

    public static class MaterialLibrary
    {
        public const float WaterDensityKgPerM3 = 1000f;

        public static readonly MaterialDef Pine = new MaterialDef("Pine", 500f, 0.5f, new Color(0.80f, 0.62f, 0.38f), 0f, 0.2f);
        public static readonly MaterialDef Ice = new MaterialDef("Ice", 917f, 0.1f, new Color(0.72f, 0.90f, 0.98f), 0f, 0.9f);
        public static readonly MaterialDef Aluminum = new MaterialDef("Aluminum", 2700f, 0.4f, new Color(0.80f, 0.82f, 0.85f), 0.35f, 0.55f);
        public static readonly MaterialDef Steel = new MaterialDef("Steel", 7850f, 0.5f, new Color(0.42f, 0.45f, 0.50f), 0.35f, 0.6f);

        public static readonly MaterialDef[] All = { Pine, Ice, Aluminum, Steel };
    }

    /// <summary>
    /// Closed-form relations the simulation is validated against.
    /// </summary>
    public static class PhysicsFormulas
    {
        public static float VolumeM3(float massKg, float densityKgPerM3) => massKg / densityKgPerM3;

        public static float CubeSideM(float massKg, float densityKgPerM3) => Mathf.Pow(VolumeM3(massKg, densityKgPerM3), 1f / 3f);

        /// <summary>Fraction of a floating body's volume below the surface at rest; 1 means it sinks.</summary>
        public static float SubmergedFractionAtRest(float densityKgPerM3, float fluidDensityKgPerM3) =>
            Mathf.Min(1f, densityKgPerM3 / fluidDensityKgPerM3);

        /// <summary>Distance from the pivot at which <paramref name="massB"/> balances massA placed at armA.</summary>
        public static float BalancingArmM(float massA, float armA, float massB) => massA * armA / massB;
    }
}
