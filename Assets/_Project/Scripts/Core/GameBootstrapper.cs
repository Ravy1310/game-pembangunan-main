using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using CozyGlade.CameraControls;
using CozyGlade.Input;
using CozyGlade.Tools;
using CozyGlade.Building;
using CozyGlade.World;
using CozyGlade.UI;

namespace CozyGlade.Core
{
    public static class GameBootstrapper
    {
        private static bool _isBootstrapped;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _isBootstrapped = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void AutoBootstrap()
        {
            if (_isBootstrapped) return;
            _isBootstrapped = true;

            Debug.Log("[Cozy Glade] Starting Auto-Bootstrap in scene: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

            // 0. Clean up any stale or temporary objects from editor/screenshot tests
            CleanStaleObjects();

            // 1. Environment & Lighting
            EnsureEnvironment();

            // 2. Camera & CameraRig
            var camera = EnsureCamera();

            // 3. World Data & WorldView
            var worldData = new WorldData();
            EnsureWorldView();

            // 4. Input System
            var inputReader = EnsureInput();

            // 5. Tool Manager & Tools
            var toolManager = EnsureToolManager(camera, inputReader, worldData);

            // Connect CameraRig
            var cameraRig = camera.GetComponent<CameraRig>();
            if (cameraRig != null && inputReader != null)
            {
                cameraRig.SetInputReader(inputReader);
            }

            // 6. Toolbar UI
            EnsureToolbarUI(toolManager);

            Debug.Log("[Cozy Glade] Auto-Bootstrap complete. Ready to play!");
        }

        private static void CleanStaleObjects()
        {
            // 1. Destroy stale capture cameras
            var allCams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            foreach (var c in allCams)
            {
                if (c == null) continue;
                if (c.name.Contains("Capture") || c.name.Contains("Screenshot") || c.name == "CaptureCamera")
                {
                    Debug.Log($"[Cozy Glade] Destroying stale capture camera: {c.name}");
                    Object.DestroyImmediate(c.gameObject);
                }
            }

            // 2. Destroy stale uppercase SYSTEMS
            var staleSys = GameObject.Find("--- SYSTEMS ---");
            if (staleSys != null)
            {
                Object.DestroyImmediate(staleSys);
            }

            // 3. Clean up stale test walls if present from editor scripts
            string[] testWallNames = { "Wall_single_4m", "Wall_l_shaped", "Wall_box_4x3" };
            foreach (var wName in testWallNames)
            {
                var w = GameObject.Find(wName);
                if (w != null)
                {
                    Object.DestroyImmediate(w);
                }
            }

            // 4. Clean up any existing ToolbarUI so it will be freshly created and bound
            var existingUI = Object.FindObjectsByType<ToolbarUI>(FindObjectsInactive.Include);
            foreach (var ui in existingUI)
            {
                if (ui != null)
                {
                    Object.DestroyImmediate(ui.gameObject);
                }
            }
            var staleUIGo = GameObject.Find("--- ToolbarUI ---");
            if (staleUIGo != null)
            {
                Object.DestroyImmediate(staleUIGo);
            }

            // 5. Clean up duplicate DirectionalLights
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
            bool keptOne = false;
            foreach (var l in lights)
            {
                if (l != null && l.type == LightType.Directional)
                {
                    if (!keptOne)
                    {
                        keptOne = true;
                    }
                    else
                    {
                        Object.DestroyImmediate(l.gameObject);
                    }
                }
            }
        }

        private static void EnsureEnvironment()
        {
            // Ground
            if (GameObject.Find("Ground") == null)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(6f, 1f, 6f); // 60x60m

#if UNITY_EDITOR
                var groundMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Grass_Ground.mat");
                if (groundMat != null)
                {
                    ground.GetComponent<Renderer>().sharedMaterial = groundMat;
                }
#endif
            }

            // Directional Light
            var lights = Object.FindObjectsByType<Light>();
            bool hasDirLight = false;
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional) { hasDirLight = true; break; }
            }

            if (!hasDirLight)
            {
                var lightGo = new GameObject("Directional Light");
                var l = lightGo.AddComponent<Light>();
                l.type = LightType.Directional;
                l.color = new Color(1f, 0.945f, 0.839f); // #FFF1D6
                l.intensity = 1.25f;
                l.shadows = LightShadows.Soft;
                l.shadowStrength = 0.9f;
                lightGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.64f, 0.8f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.81f, 0.91f, 0.95f); // #CFE8F2
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 90f;
        }

        private static Camera EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();

                var uac = camGo.AddComponent<UniversalAdditionalCameraData>();
                uac.renderPostProcessing = true;
            }

            cam.tag = "MainCamera";
            cam.depth = 0;
            cam.fieldOfView = 38f;
            cam.backgroundColor = new Color(0.75f, 0.89f, 0.95f); // #BFE3F2
            cam.clearFlags = CameraClearFlags.SolidColor;

            var rig = cam.GetComponent<CameraRig>();
            if (rig == null)
            {
                rig = cam.gameObject.AddComponent<CameraRig>();
            }

            return cam;
        }

        private static void EnsureWorldView()
        {
            if (WorldView.Instance == null)
            {
                var wvGo = new GameObject("--- WorldView ---");
                wvGo.AddComponent<WorldView>();
            }
        }

        private static InputReader EnsureInput()
        {
            var inputReader = ScriptableObject.CreateInstance<InputReader>();
            inputReader.name = "InputReader_Runtime";

            InputActionAsset asset = null;
#if UNITY_EDITOR
            asset = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/CozyInput.inputactions");
#endif
            if (asset == null)
            {
                // Dynamic fallback Input Action Map so input always works
                asset = ScriptableObject.CreateInstance<InputActionAsset>();
                var map = new InputActionMap("Gameplay");
                map.AddAction("Pointer", InputActionType.Value, "<Pointer>/position");
                map.AddAction("PointerDelta", InputActionType.Value, "<Pointer>/delta");
                map.AddAction("Click", InputActionType.Button, "<Mouse>/leftButton");
                map.AddAction("RightClick", InputActionType.Button, "<Mouse>/rightButton");
                map.AddAction("MiddleClick", InputActionType.Button, "<Mouse>/middleButton");
                map.AddAction("Scroll", InputActionType.Value, "<Mouse>/scroll");
                map.AddAction("PanModifier", InputActionType.Button, "<Keyboard>/shift");
                map.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
                map.AddAction("Confirm", InputActionType.Button, "<Keyboard>/enter");
                map.AddAction("Undo", InputActionType.Button, "<Keyboard>/z");
                map.AddAction("Redo", InputActionType.Button, "<Keyboard>/y");
                for (int i = 1; i <= 5; i++)
                {
                    map.AddAction($"ToolSlot{i}", InputActionType.Button, $"<Keyboard>/{i}");
                }
                asset.AddActionMap(map);
            }

            inputReader.Initialize(asset);
            return inputReader;
        }

        private static ToolManager EnsureToolManager(Camera camera, InputReader inputReader, WorldData worldData)
        {
            var sysGo = GameObject.Find("--- Systems ---");
            if (sysGo == null)
            {
                sysGo = new GameObject("--- Systems ---");
            }

            var tm = sysGo.GetComponent<ToolManager>();
            if (tm == null)
            {
                tm = sysGo.AddComponent<ToolManager>();
            }
            tm.SetInputReader(inputReader);

            // Wall Tool
            var wallTool = sysGo.GetComponent<WallTool>();
            if (wallTool == null)
            {
                wallTool = sysGo.AddComponent<WallTool>();
            }
            wallTool.Initialize(worldData, tm);
            wallTool.SetInputReader(inputReader);
            tm.RegisterTool(wallTool);

            // Road Tool (stone road painting)
            var roadTool = sysGo.GetComponent<StoneRoadTool>();
            if (roadTool == null)
            {
                roadTool = sysGo.AddComponent<StoneRoadTool>();
            }
            roadTool.Initialize(worldData, tm);
            tm.RegisterTool(roadTool);

            // Fence Tool
            var fenceTool = sysGo.GetComponent<FenceTool>();
            if (fenceTool == null)
            {
                fenceTool = sysGo.AddComponent<FenceTool>();
            }
            fenceTool.Initialize(worldData, tm);
            tm.RegisterTool(fenceTool);

            // Path Tool
            var pathTool = sysGo.GetComponent<PathTool>();
            if (pathTool == null)
            {
                pathTool = sysGo.AddComponent<PathTool>();
            }
            pathTool.Initialize(worldData, tm);
            tm.RegisterTool(pathTool);

            // Register stub tools for remaining Toolbar slots (Prop, Erase)
            tm.RegisterTool(new StandbyTool("Prop"));
            tm.RegisterTool(new StandbyTool("Erase"));

            // WallTool is active by default
            tm.SelectTool(wallTool);

            return tm;
        }

        private static void EnsureToolbarUI(ToolManager toolManager)
        {
            if (ToolbarUI.Instance == null)
            {
                ToolbarUI.Create(toolManager);
            }
        }

        private class StandbyTool : ITool
        {
            public string Id { get; }
            public StandbyTool(string id) => Id = id;
            public void OnEnter() => Debug.Log($"[ToolManager] Switched to {Id} tool (available in next slice).");
            public void OnExit() { }
            public void Tick(Vector3 pointerWorld) { }
            public void OnPressed(Vector3 pointerWorld) { }
            public void OnReleased(Vector3 pointerWorld) { }
            public void OnCancel() { }
        }
    }
}
