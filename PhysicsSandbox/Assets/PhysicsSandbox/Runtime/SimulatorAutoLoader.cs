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
            if (XRSettings.isDeviceActive || m_SimulatorPrefab == null)
                return;

            var simulator = Instantiate(m_SimulatorPrefab);
            simulator.name = m_SimulatorPrefab.name;
            SimulatorActive = true;
        }
    }
}
