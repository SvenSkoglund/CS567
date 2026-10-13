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

        /// <summary>Lift height over which the gain shortfall levels off.</summary>
        public const float OffsetSaturationM = 0.25f;

        /// <summary>Lag time constant applied to the heaviest object; the lightest gets 0.</summary>
        public const float MaxLagSeconds = 0.30f;

        public static float Heaviness01(float massKg) =>
            Mathf.Clamp01(Mathf.Log(Mathf.Max(massKg, 1e-4f) / MinMassKg) / Mathf.Log(MaxMassKg / MinMassKg));

        /// <summary>Displayed lift per unit of hand lift, near the grab point.</summary>
        public static float Gain(float massKg) => Mathf.Lerp(1f, MinGain, Heaviness01(massKg));

        /// <summary>Time constant of the first-order lag between hand and displayed object.</summary>
        public static float LagSeconds(float massKg) => Mathf.Lerp(0f, MaxLagSeconds, Heaviness01(massKg));

        /// <summary>
        /// Where the object is displayed, relative to where it was grabbed, for a given hand
        /// displacement. Weight acts downward, so only lifting is scaled: the object hangs
        /// below the hand by a shortfall that starts as a plain control-to-display gain and
        /// levels off at (1 - gain) x <see cref="OffsetSaturationM"/>. Sideways and downward
        /// motion is one-to-one, so the object ends level with the hand once the lag settles.
        /// </summary>
        public static Vector3 DisplayedDisplacement(Vector3 handDisplacement, float massKg)
        {
            var lift = handDisplacement.y;
            if (lift <= 0f)
                return handDisplacement;

            var shortfall = (1f - Gain(massKg)) * OffsetSaturationM * (float)System.Math.Tanh(lift / OffsetSaturationM);
            return new Vector3(handDisplacement.x, lift - shortfall, handDisplacement.z);
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

        /// <summary>Debug overlay: a line from the tracked hand position to the displayed object.</summary>
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
