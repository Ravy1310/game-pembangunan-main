#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using CozyGlade.Core;
using CozyGlade.CameraControls;
using CozyGlade.Tools;
using CozyGlade.Input;

namespace CozyGlade.EditorTools
{
    public static class CozySceneBuilder
    {
        const string P = "Assets/_Project/";
        const string ScenesDir = P + "Scenes/";
        const string SettingsDir = P + "Settings/";
        const string MatDir = P + "Art/Materials/";

        static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString("#" + h, out var c);
            return c;
        }

        [MenuItem("Cozy Glade/Create Sandbox Scene")]
        public static void CreateSandboxScene()
        {
            if (!Directory.Exists(ScenesDir))
            {
                Directory.CreateDirectory(ScenesDir);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Ground Plane (60x60m)
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(6f, 1f, 6f); // 10x10 default plane * 6 = 60x60m

            var groundMat = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "M_Grass_Ground.mat");
            if (groundMat != null)
            {
                ground.GetComponent<Renderer>().sharedMaterial = groundMat;
            }

            // 2. Directional Light (Warm sunlight)
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Hex("FFF1D6");
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.9f;
            lightGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            // 3. Environment & Fog
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.64f, 0.8f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Hex("CFE8F2");
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 90f;

            // 4. Volume Profile (PP_Cozy)
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(SettingsDir + "PP_Cozy.asset");
            if (profile == null)
            {
                profile = CozyAssetSetup.BuildPostProcessProfile();
            }

            var volumeGo = new GameObject("Global Volume");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            // 5. Camera + CameraRig
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 38f;
            cam.backgroundColor = Hex("BFE3F2");
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<AudioListener>();

            var uacData = camGo.AddComponent<UniversalAdditionalCameraData>();
            uacData.renderPostProcessing = true;

            var pivotTarget = new GameObject("CameraPivot");
            pivotTarget.transform.position = Vector3.zero;

            var cameraRig = camGo.AddComponent<CameraRig>();

            // 6. Game Manager & Systems
            var systemsGo = new GameObject("--- Systems ---");
            var gameManager = systemsGo.AddComponent<GameManager>();
            var toolManager = systemsGo.AddComponent<ToolManager>();

            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(SettingsDir + "CozyInput.inputactions");
            var inputReader = ScriptableObject.CreateInstance<InputReader>();
            inputReader.name = "InputReader_Runtime";

            // Save scene
            string scenePath = ScenesDir + "Sandbox.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Cozy Glade] Sandbox scene successfully created at: " + scenePath);
        }
    }
}
#endif
