using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PhysicsSandbox
{
    /// <summary>
    /// The side panel from the concept sketch: shows the properties of whatever is held,
    /// the live cue parameters, and the measured outcome at the lever and the tank.
    /// Also owns the demo controls (toggle cue, toggle ghost, reset blocks).
    /// </summary>
    public class SandboxHud : MonoBehaviour
    {
        [SerializeField] TextMeshPro m_Text;
        [SerializeField] Lever m_Lever;
        [SerializeField] WaterVolume m_Water;

        InputAction m_ToggleCue;
        InputAction m_ToggleGhost;
        InputAction m_Reset;
        readonly StringBuilder m_Builder = new StringBuilder(512);

        public TextMeshPro Text
        {
            get => m_Text;
            set => m_Text = value;
        }

        public Lever Lever
        {
            get => m_Lever;
            set => m_Lever = value;
        }

        public WaterVolume Water
        {
            get => m_Water;
            set => m_Water = value;
        }

        void Awake()
        {
            // Small, light objects need a finer physics step than the 50 Hz default.
            Time.fixedDeltaTime = 1f / 90f;
            Physics.defaultSolverIterations = 16;
            Physics.defaultSolverVelocityIterations = 4;

            m_ToggleCue = CreateButton("Toggle Cue", "<Keyboard>/p", "<XRController>{RightHand}/primaryButton");
            m_ToggleGhost = CreateButton("Toggle Ghost", "<Keyboard>/o", "<XRController>{RightHand}/secondaryButton");
            m_Reset = CreateButton("Reset Blocks", "<Keyboard>/backspace", "<XRController>{LeftHand}/primaryButton");
        }

        static InputAction CreateButton(string actionName, params string[] bindings)
        {
            var action = new InputAction(actionName, InputActionType.Button);
            foreach (var binding in bindings)
                action.AddBinding(binding);
            return action;
        }

        void OnEnable()
        {
            m_ToggleCue.Enable();
            m_ToggleGhost.Enable();
            m_Reset.Enable();
        }

        void OnDisable()
        {
            m_ToggleCue.Disable();
            m_ToggleGhost.Disable();
            m_Reset.Disable();
        }

        void Update()
        {
            if (m_ToggleCue.WasPressedThisFrame())
                PseudoHapticSettings.CueEnabled = !PseudoHapticSettings.CueEnabled;

            if (m_ToggleGhost.WasPressedThisFrame())
                PseudoHapticSettings.GhostEnabled = !PseudoHapticSettings.GhostEnabled;

            if (m_Reset.WasPressedThisFrame())
            {
                foreach (var block in PhysicsBlock.All)
                    block.ResetToHome();
            }

            if (m_Text != null)
                m_Text.text = BuildText();
        }

        string BuildText()
        {
            var sb = m_Builder;
            sb.Clear();

            sb.Append("<b>PHYSICS YOU CAN FEEL</b>  <size=70%>prototype</size>\n");
            sb.Append("Weight cue: ").Append(OnOff(PseudoHapticSettings.CueEnabled)).Append("   <size=70%>[P / A button]</size>\n");
            sb.Append("Hand ghost: ").Append(OnOff(PseudoHapticSettings.GhostEnabled)).Append("   <size=70%>[O / B button]</size>\n");
            sb.Append("<size=70%>Reset blocks: [Backspace / X button]");
            if (SimulatorAutoLoader.SimulatorActive)
                sb.Append("   Scripted demo: [N]");
            sb.Append("</size>\n\n");

            sb.Append("<b>Holding</b>\n");
            var holding = false;
            foreach (var block in PhysicsBlock.All)
            {
                if (!block.TryGetComponent<PseudoHapticGrabTransformer>(out var cue) || !cue.IsHeld)
                    continue;

                holding = true;
                sb.Append(block.MaterialName)
                    .Append(":  ").Append(block.MassKg.ToString("0.00")).Append(" kg,  ")
                    .Append(block.DensityKgPerM3.ToString("0")).Append(" kg/m³,  ")
                    .Append((block.VolumeM3 * 1e6f).ToString("0")).Append(" cm³\n");
                sb.Append("  gain ").Append(cue.CurrentGain.ToString("0.00"))
                    .Append("   lag ").Append((cue.CurrentLagSeconds * 1000f).ToString("0")).Append(" ms")
                    .Append("   offset ").Append((cue.OffsetM * 100f).ToString("0.0")).Append(" cm\n");
            }

            if (!holding)
                sb.Append("<color=#9AA5B1>nothing</color>\n");

            if (m_Lever != null)
            {
                sb.Append("\n<b>Lever</b>\n");
                sb.Append("left ").Append(m_Lever.LeftTorqueNm.ToString("0.00")).Append(" N·m   right ")
                    .Append(m_Lever.RightTorqueNm.ToString("0.00")).Append(" N·m\n");
                sb.Append("tilt ").Append(m_Lever.TiltDeg.ToString("0.0")).Append("°");
                if (m_Lever.IsBalanced)
                    sb.Append("   <color=#7CD992>BALANCED</color>");
                sb.Append('\n');
            }

            if (m_Water != null)
            {
                sb.Append("\n<b>Water tank</b>\n");
                var any = false;
                foreach (var pair in m_Water.Submerged)
                {
                    if (pair.Key.IsHeld)
                        continue;

                    any = true;
                    var floats = pair.Key.DensityKgPerM3 < m_Water.FluidDensityKgPerM3;
                    sb.Append(pair.Key.MaterialName).Append(floats ? " floats, " : " sinks, ")
                        .Append((pair.Value * 100f).ToString("0")).Append("% under\n");
                }

                if (!any)
                    sb.Append("<color=#9AA5B1>empty</color>\n");
            }

            return sb.ToString();
        }

        static string OnOff(bool on) => on ? "<color=#7CD992>ON</color>" : "<color=#F2A65A>OFF</color>";
    }
}
