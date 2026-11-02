using System.IO;
using SlotGame.Core.Session;
using SlotGame.Data;
using SlotGame.Gameplay;
using SlotGame.View;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace SlotGame.EditorTools
{
    /// <summary>
    /// Tools > Slot Game menu. Everything here is optional helpers; the game runs
    /// by just opening Assets/Scenes/SlotMachine.unity and pressing Play.
    /// </summary>
    public static class SlotGameEditorMenu
    {
        private const string MenuRoot = "Tools/Slot Game/";
        private const string ScenePath = "Assets/Scenes/SlotMachine.unity";
        private const string PrefabPath = "Assets/Prefabs/SlotMachine.prefab";
        private const string WebGlBuildPath = "Build/WebGL";

        [MenuItem(MenuRoot + "Open Slot Machine Scene", priority = 0)]
        public static void OpenScene()
        {
            if (!File.Exists(ScenePath))
            {
                RebuildScene();
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
        }

        /// <summary>Creates a clean scene with a camera, an EventSystem and the bootstrap object.</summary>
        [MenuItem(MenuRoot + "Rebuild Scene", priority = 1)]
        public static void RebuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.05f, 0.2f, 1f);
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            var bootstrap = new GameObject("SlotGame").AddComponent<SlotGameBootstrap>();
            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("config").objectReferenceValue = LoadConfig();
            var prefab = AssetDatabase.LoadAssetAtPath<SlotMachineView>(PrefabPath);
            serialized.FindProperty("machineViewPrefab").objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            Debug.Log($"Saved {ScenePath}.");
        }

        /// <summary>
        /// Builds the machine with the same factory the game uses at runtime and
        /// saves it as a prefab, then points the scene's bootstrap at it.
        /// After this the machine can be edited visually in the prefab.
        /// </summary>
        [MenuItem(MenuRoot + "Generate Machine Prefab", priority = 2)]
        public static void GenerateMachinePrefab()
        {
            SlotMachineConfig config = LoadConfig();
            if (config == null) return;

            SlotMachineView view = SlotMachineViewFactory.Build(config);
            view.gameObject.SetActive(true);

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath) ?? "Assets/Prefabs");
            PrefabUtility.SaveAsPrefabAsset(view.gameObject, PrefabPath, out bool success);
            Object.DestroyImmediate(view.gameObject);

            if (!success)
            {
                Debug.LogError("Could not save the machine prefab.");
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Saved {PrefabPath}.");

            // Point the open scene's bootstrap at the new prefab, if there is one.
            var bootstrap = Object.FindObjectOfType<SlotGameBootstrap>();
            if (bootstrap != null)
            {
                var serialized = new SerializedObject(bootstrap);
                serialized.FindProperty("machineViewPrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<SlotMachineView>(PrefabPath);
                serialized.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
                EditorSceneManager.SaveScene(bootstrap.gameObject.scene);
                Debug.Log("The scene now uses the machine prefab.");
            }
        }

        [MenuItem(MenuRoot + "Simulate RTP (1,000,000 spins)", priority = 20)]
        public static void SimulateRtp()
        {
            SlotMachineConfig config = LoadConfig();
            if (config == null) return;

            RtpSimulator.Report report = SlotGameFactory.Simulate(config, 1000000);
            Debug.Log($"RTP simulation: {report}");
            EditorUtility.DisplayDialog("RTP simulation", report.ToString().Replace(" | ", "\n"), "OK");
        }

        /// <summary>
        /// Builds the WebGL player into Build/WebGL with settings that work on
        /// GitHub Pages (gzip + decompression fallback, because Pages does not
        /// send the Content-Encoding headers Unity's Brotli builds need).
        /// </summary>
        [MenuItem(MenuRoot + "Build WebGL", priority = 40)]
        public static void BuildWebGl()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                EditorUtility.DisplayDialog("WebGL module missing",
                    "Install the WebGL Build Support module for this Unity version in Unity Hub, then try again.", "OK");
                return;
            }

            ApplyWebGlPlayerSettings();
            AddSceneToBuildSettings();

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = WebGlBuildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"WebGL build done: {WebGlBuildPath} ({report.summary.totalSize / (1024 * 1024)} MB)");
                EditorUtility.RevealInFinder(WebGlBuildPath);
            }
            else
            {
                Debug.LogError($"WebGL build failed: {report.summary.result}");
            }
        }

        public static void ApplyWebGlPlayerSettings()
        {
            PlayerSettings.companyName = "Prajwal Ghagre";
            PlayerSettings.productName = "Lucky Slots";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.runInBackground = true;
        }

        private static void AddSceneToBuildSettings()
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static SlotMachineConfig LoadConfig()
        {
            var config = Resources.Load<SlotMachineConfig>(SlotMachineConfig.ResourcesPath);
            if (config == null)
            {
                Debug.LogError($"Config not found at Resources/{SlotMachineConfig.ResourcesPath}.");
            }

            return config;
        }
    }
}
