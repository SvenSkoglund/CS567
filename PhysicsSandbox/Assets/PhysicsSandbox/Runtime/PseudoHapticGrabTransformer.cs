using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace PhysicsSandbox
{
    /// <summary>
    /// Grab transformer that renders weight without force hardware. The base class computes
    /// where the object would be under one-to-one tracking; this class then displays it at
    /// a position whose displacement is scaled by a mass-dependent control-to-display gain
    /// (levelling off over long carries) and smoothed by a mass-dependent lag, so a heavier
    /// object trails the hand more.
    /// With the cue disabled the object tracks the hand exactly.
    /// </summary>
    public class PseudoHapticGrabTransformer : XRGeneralGrabTransformer
    {
        XRGrabInteractable m_Grab;
        PhysicsBlock m_Block;
        Rigidbody m_Rigidbody;

        bool m_NeedsAnchor;
        Vector3 m_AnchorRaw;
        Vector3 m_AnchorDisplayed;
        Pose m_Displayed;

        /// <summary>Pose under one-to-one tracking, i.e. where the hand really holds the object.</summary>
        public Pose RawPose { get; private set; }

        /// <summary>Pose actually shown to the learner.</summary>
        public Pose DisplayedPose => m_Displayed;

        public bool IsHeld => m_Grab != null && m_Grab.isSelected;
        public float CurrentGain { get; private set; } = 1f;
        public float CurrentLagSeconds { get; private set; }
        public float OffsetM => IsHeld ? Vector3.Distance(RawPose.position, m_Displayed.position) : 0f;

        float MassKg => m_Block != null ? m_Block.MassKg : m_Rigidbody != null ? m_Rigidbody.mass : 1f;

        protected void OnEnable() => PseudoHapticSettings.Changed += OnSettingsChanged;

        protected void OnDisable() => PseudoHapticSettings.Changed -= OnSettingsChanged;

        // Re-anchor so that switching the cue mid-grab never makes the object jump.
        void OnSettingsChanged() => m_NeedsAnchor = true;

        public override void OnLink(XRGrabInteractable grabInteractable)
        {
            base.OnLink(grabInteractable);
            m_Grab = grabInteractable;
            m_Block = grabInteractable.GetComponent<PhysicsBlock>();
            m_Rigidbody = grabInteractable.GetComponent<Rigidbody>();
        }

        public override void OnGrab(XRGrabInteractable grabInteractable)
        {
            base.OnGrab(grabInteractable);
            m_NeedsAnchor = true;
        }

        public override void OnGrabCountChanged(XRGrabInteractable grabInteractable, Pose targetPose, Vector3 localScale)
        {
            base.OnGrabCountChanged(grabInteractable, targetPose, localScale);
            m_NeedsAnchor = true;
        }

        public override void Process(XRGrabInteractable grabInteractable, XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
        {
            base.Process(grabInteractable, updatePhase, ref targetPose, ref localScale);

            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic &&
                updatePhase != XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender)
                return;

            if (!grabInteractable.isSelected)
                return;

            RawPose = targetPose;

            if (m_NeedsAnchor)
            {
                m_NeedsAnchor = false;
                m_AnchorRaw = targetPose.position;
                m_AnchorDisplayed = grabInteractable.transform.position;
                m_Displayed = new Pose(m_AnchorDisplayed, grabInteractable.transform.rotation);
            }

            // Advance the filter once per frame; OnBeforeRender reuses the result.
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                var cueOn = PseudoHapticSettings.CueEnabled;
                CurrentGain = cueOn ? PseudoHapticMapping.Gain(MassKg) : 1f;
                CurrentLagSeconds = cueOn ? PseudoHapticMapping.LagSeconds(MassKg) : 0f;

                var handDisplacement = targetPose.position - m_AnchorRaw;
                var desired = m_AnchorDisplayed + (cueOn ? PseudoHapticMapping.DisplayedDisplacement(handDisplacement, MassKg) : handDisplacement);
                var blend = CurrentLagSeconds > 1e-4f ? 1f - Mathf.Exp(-Time.deltaTime / CurrentLagSeconds) : 1f;
                m_Displayed.position = Vector3.Lerp(m_Displayed.position, desired, blend);
                m_Displayed.rotation = Quaternion.Slerp(m_Displayed.rotation, targetPose.rotation, blend);
            }

            targetPose = m_Displayed;
        }
    }
}
