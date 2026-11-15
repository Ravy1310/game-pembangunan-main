using UnityEngine;
using UnityEngine.InputSystem;
using CozyGlade.Input;
using CozyGlade.CameraControls;
using CozyGlade.Tools;

namespace CozyGlade.Core
{
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

#pragma warning disable 0649
        [Header("Configuration")]
        [SerializeField] private GameConfig config;
        [SerializeField] private InputActionAsset inputActionsAsset;

        [Header("System References")]
        [SerializeField] private InputReader inputReader;
        [SerializeField] private CameraRig cameraRig;
        [SerializeField] private ToolManager toolManager;
#pragma warning restore 0649

        public GameConfig Config => config;
        public InputReader InputReader => inputReader;
        public CameraRig CameraRig => cameraRig;
        public ToolManager ToolManager => toolManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (inputReader == null)
            {
                inputReader = ScriptableObject.CreateInstance<InputReader>();
            }

            if (inputActionsAsset != null)
            {
                inputReader.Initialize(inputActionsAsset);
            }

            if (cameraRig != null && inputReader != null)
            {
                cameraRig.SetInputReader(inputReader);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
