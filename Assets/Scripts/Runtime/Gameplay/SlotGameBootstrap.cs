using System.Collections.Generic;
using SlotGame.Audio;
using SlotGame.Core.Random;
using SlotGame.Core.Reels;
using SlotGame.Core.Session;
using SlotGame.Data;
using SlotGame.View;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SlotGame.Gameplay
{
    /// <summary>
    /// Composition root: the one object in the scene. On Awake it loads the
    /// config, creates the game logic, builds (or instantiates) the view and
    /// connects everything. Nothing else in the project uses singletons or
    /// FindObjectOfType, so all dependencies are visible right here.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class SlotGameBootstrap : MonoBehaviour
    {
        [Tooltip("Leave empty to load Resources/SlotGame/SlotMachineConfig.")]
        [SerializeField] private SlotMachineConfig config;

        [Tooltip("Optional. If empty, the machine is built from code by SlotMachineViewFactory.")]
        [SerializeField] private SlotMachineView machineViewPrefab;

        public SlotMachineController Controller { get; private set; }
        public SlotGameSession Session { get; private set; }

        private void Awake()
        {
            if (config == null)
            {
                config = Resources.Load<SlotMachineConfig>(SlotMachineConfig.ResourcesPath);
            }

            if (config == null)
            {
                Debug.LogError($"SlotGameBootstrap: no config found at Resources/{SlotMachineConfig.ResourcesPath}.", this);
                enabled = false;
                return;
            }

            List<string> problems = config.Validate();
            if (problems.Count > 0)
            {
                foreach (string problem in problems)
                {
                    Debug.LogError($"Slot machine config problem: {problem}", config);
                }

                enabled = false;
                return;
            }

            EnsureCamera();
            EnsureEventSystem();

            SlotMachineView view = machineViewPrefab != null
                ? Instantiate(machineViewPrefab)
                : SlotMachineViewFactory.Build(config);
            view.name = "SlotMachine";

            // Game logic (plain C#).
            List<ReelStrip> strips = SlotGameFactory.BuildStrips(config);
            IRandomProvider spinRandom = SlotGameFactory.CreateSpinRandom(config);
            Session = SlotGameFactory.CreateSession(config, strips, spinRandom);

            // Presentation.
            var events = new GameEvents();
            var audioManager = GetOrAddComponent<AudioManager>();
            audioManager.Initialize(config.Sounds, events);

            Controller = GetOrAddComponent<SlotMachineController>();
            Controller.Initialize(config, view, Session, strips, events, new SystemRandomProvider());

            view.gameObject.SetActive(true);

            if (spinRandom is SystemRandomProvider systemRandom)
            {
                Debug.Log($"Slot game ready. Spin RNG seed: {systemRandom.Seed} (tick 'Use Fixed Spin Seed' in the config to replay).");
            }
        }

        private T GetOrAddComponent<T>() where T : Component
        {
            T component = GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null) return;

            // The UI is a Screen Space Overlay canvas, but a camera still clears the screen.
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.05f, 0.2f, 1f);
            camera.orthographic = true;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindObjectOfType<EventSystem>() != null) return;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        /// <summary>
        /// Safety net: if someone opens an empty scene and presses Play, the game
        /// still starts. With the provided SlotMachine scene this does nothing.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateIfMissing()
        {
            if (FindObjectOfType<SlotGameBootstrap>() != null) return;
            if (Resources.Load<SlotMachineConfig>(SlotMachineConfig.ResourcesPath) == null) return;

            Debug.Log("No SlotGameBootstrap in the scene, creating one automatically.");
            new GameObject("SlotGame").AddComponent<SlotGameBootstrap>();
        }
    }
}
