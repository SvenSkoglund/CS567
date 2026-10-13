using System;
using UnityEngine;

namespace PhysicsSandbox
{
    /// <summary>
    /// Maps an object's mass onto the two pseudo-haptic cue parameters: control-to-display
    /// gain (Dominjon et al. 2005) and visual lag (Rietzler et al. 2018). Both are monotonic
    /// in mass. The mapping is logarithmic so equal mass ratios give equal cue steps.
    /// </summary>
    public static class PseudoHapticMapping
    {
        public const float MinMassKg = 0.25f;
        public const float MaxMassKg = 10f;

        /// <summary>Gain applied to the heaviest object; the lightest gets 1 (no scaling).</summary>
        public const float MinGain = 0.35f;

        /// <summary>Hand travel over which the gain shortfall levels off.</summary>
        public const float OffsetSaturationM = 0.25f;

        /// <summary>Lag time constant applied to the heaviest object; the lightest gets 0.</summary>
        public const float MaxLagSeconds = 0.30f;

        public static float Heaviness01(float massKg) =>
            Mathf.Clamp01(Mathf.Log(Mathf.Max(massKg, 1e-4f) / MinMassKg) / Mathf.Log(MaxMassKg / MinMassKg));

        /// <summary>Displayed displacement per unit of hand displacement.</summary>
        public static float Gain(float massKg) => Mathf.Lerp(1f, MinGain, Heaviness01(massKg));

        /// <summary>Time constant of the first-order lag between hand and displayed object.</summary>
        public static float LagSeconds(float massKg) => Mathf.Lerp(0f, MaxLagSeconds, Heaviness01(massKg));

        /// <summary>
        /// Where the object is displayed, relative to where it was grabbed, for a given hand
        /// displacement. Near the grab point this is a plain control-to-display gain. Over a
        /// long carry the shortfall levels off at (1 - gain) x <see cref="OffsetSaturationM"/>,
        /// so a heavy object trails the hand by a bounded distance instead of drifting away.
        /// </summary>
        public static Vector3 DisplayedDisplacement(Vector3 handDisplacement, float massKg)
        {
            var distance = handDisplacement.magnitude;
            if (distance < 1e-6f)
                return handDisplacement;

            var shortfall = (1f - Gain(massKg)) * OffsetSaturationM * (float)System.Math.Tanh(distance / OffsetSaturationM);
            return handDisplacement * ((distance - shortfall) / distance);
        }
    }

    /// <summary>
    /// Run-time switches for the cue. The study's cue-on and cue-off builds differ only in
    /// <see cref="CueEnabled"/>.
    /// </summary>
    public static class PseudoHapticSettings
    {
        static bool s_CueEnabled = true;
        static bool s_GhostEnabled = true;

        public static event Action Changed;

        public static bool CueEnabled
        {
            get => s_CueEnabled;
            set
            {
                if (s_CueEnabled == value)
                    return;
                s_CueEnabled = value;
                Changed?.Invoke();
            }
        }

        /// <summary>Debug overlay showing where the tracked hand really holds the object.</summary>
        public static bool GhostEnabled
        {
            get => s_GhostEnabled;
            set
            {
                if (s_GhostEnabled == value)
                    return;
                s_GhostEnabled = value;
                Changed?.Invoke();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_CueEnabled = true;
            s_GhostEnabled = true;
            Changed = null;
        }
    }
}
