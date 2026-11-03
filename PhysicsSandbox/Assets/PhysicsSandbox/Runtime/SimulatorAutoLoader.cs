using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace PhysicsSandbox
{
    /// <summary>
    /// Spawns the keyboard-and-mouse XR simulator when no headset is driving the scene, so
    /// the same scene runs through Quest Link or on a desktop without changing settings.
    /// </summary>
    public class SimulatorAutoLoader : MonoBehaviour
    {
        [SerializeField] GameObject m_SimulatorPrefab;

        public GameObject SimulatorPrefab
        {
            get => m_SimulatorPrefab;
            set => m_SimulatorPrefab = value;
        }

        public static bool SimulatorActive { get; private set; }

        void Start()
        {
            SimulatorActive = false;
            if (HeadsetConnected() || m_SimulatorPrefab == null)
                return;

            var simulator = Instantiate(m_SimulatorPrefab);
            simulator.name = m_SimulatorPrefab.name;
            SimulatorActive = true;
        }

        // XRSettings.isDeviceActive stays false until the session is running, which can be after
        // Start. The display subsystem only exists once the runtime has found a headset.
        static bool HeadsetConnected()
        {
            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            return displays.Count > 0;
        }
    }
}
