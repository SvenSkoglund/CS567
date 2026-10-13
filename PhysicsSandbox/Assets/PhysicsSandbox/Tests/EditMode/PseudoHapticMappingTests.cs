using NUnit.Framework;
using UnityEngine;

namespace PhysicsSandbox.Tests
{
    /// <summary>
    /// The paper's validation requires the cue to be monotonic: a heavier object must never
    /// feel lighter. These tests sweep the full mass range of the sandbox.
    /// </summary>
    public class PseudoHapticMappingTests
    {
        static float[] MassSweep()
        {
            var masses = new float[200];
            for (var i = 0; i < masses.Length; i++)
                masses[i] = Mathf.Lerp(0.05f, 20f, i / (masses.Length - 1f));
            return masses;
        }

        [Test]
        public void Gain_NeverIncreasesWithMass()
        {
            var previous = float.MaxValue;
            foreach (var mass in MassSweep())
            {
                var gain = PseudoHapticMapping.Gain(mass);
                Assert.LessOrEqual(gain, previous, $"gain rose at {mass} kg");
                Assert.That(gain, Is.InRange(PseudoHapticMapping.MinGain, 1f));
                previous = gain;
            }
        }

        [Test]
        public void Lag_NeverDecreasesWithMass()
        {
            var previous = float.MinValue;
            foreach (var mass in MassSweep())
            {
                var lag = PseudoHapticMapping.LagSeconds(mass);
                Assert.GreaterOrEqual(lag, previous, $"lag fell at {mass} kg");
                Assert.That(lag, Is.InRange(0f, PseudoHapticMapping.MaxLagSeconds));
                previous = lag;
            }
        }

        [Test]
        public void DisplayedLift_NeverIncreasesWithMass([Values(0.02f, 0.1f, 0.3f, 1f)] float handLiftM)
        {
            var previous = float.MaxValue;
            foreach (var mass in MassSweep())
            {
                var shown = PseudoHapticMapping.DisplayedDisplacement(Vector3.up * handLiftM, mass).y;
                Assert.LessOrEqual(shown, previous + 1e-6f, $"displayed lift rose at {mass} kg");
                Assert.That(shown, Is.InRange(0f, handLiftM + 1e-6f));
                previous = shown;
            }
        }

        [Test]
        public void DisplayedLift_GrowsWithHandLift([Values(0.5f, 2.7f, 7.85f)] float massKg)
        {
            var previous = 0f;
            for (var lift = 0.01f; lift <= 1.5f; lift += 0.01f)
            {
                var shown = PseudoHapticMapping.DisplayedDisplacement(Vector3.up * lift, massKg).y;
                Assert.Greater(shown, previous, $"object moved backwards at hand lift {lift} m");
                previous = shown;
            }
        }

        [Test]
        public void Offset_StaysBoundedOnLongCarries([Values(0.5f, 2.7f, 7.85f, 10f)] float massKg)
        {
            var bound = (1f - PseudoHapticMapping.Gain(massKg)) * PseudoHapticMapping.OffsetSaturationM;
            var hand = new Vector3(3f, 1f, -2f);
            var offset = (hand - PseudoHapticMapping.DisplayedDisplacement(hand, massKg)).magnitude;
            Assert.LessOrEqual(offset, bound + 1e-5f);
        }

        [Test]
        public void EqualMassBlocks_AreSmallerWhenDenser()
        {
            var previousSide = float.MaxValue;
            foreach (var material in MaterialLibrary.All)
            {
                var side = PhysicsFormulas.CubeSideM(1f, material.densityKgPerM3);
                Assert.Less(side, previousSide, material.name + " should be smaller than the less dense material before it");
                Assert.AreEqual(1f / material.densityKgPerM3, side * side * side, 1e-7f);
                previousSide = side;
            }
        }

        [Test]
        public void ClosedForms_MatchTextbookValues()
        {
            Assert.AreEqual(0.5f, PhysicsFormulas.SubmergedFractionAtRest(500f, 1000f), 1e-6f);
            Assert.AreEqual(1f, PhysicsFormulas.SubmergedFractionAtRest(7850f, 1000f), 1e-6f);
            Assert.AreEqual(0.2944f, PhysicsFormulas.BalancingArmM(2.7f, 0.1f, 0.917f), 1e-3f);
        }
    }
}
