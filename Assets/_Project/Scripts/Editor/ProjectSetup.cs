using BiRyeoDo.Game.Systems;
using BiRyeoDo.Game.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BiRyeoDo.Editor
{
    /// <summary>One-click setup. Run "BiRyeoDo/Setup Project" once after opening the project in Unity.</summary>
    public static class ProjectSetup
    {
        private const string BootScene = "Assets/_Project/Scenes/Boot.unity";

        [MenuItem("BiRyeoDo/Setup Project")]
        public static void Run()
        {
            ConfigureAndroid();
            CreateBootScene();
            AssetDatabase.SaveAssets();
            Debug.Log("BiRyeoDo setup done.");
        }

        private static void ConfigureAndroid()
        {
            PlayerSettings.companyName = "BiRyeoDo";
            PlayerSettings.productName = "비뢰도";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.biryeodo.game");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            EditorUserBuildSettings.buildAppBundle = false; // sideload APK; personal use only
        }

        private static void CreateBootScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            new GameObject("GameManager").AddComponent<GameManager>();
            var ui = new GameObject("UI");
            ui.AddComponent<TabRouter>();
            ui.AddComponent<MainScreen>(); // builds Canvas, tab bar and battle screen at runtime
            EditorSceneManager.SaveScene(scene, BootScene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootScene, true) };
        }
    }
}
