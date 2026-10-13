using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BiRyeoDo.Editor
{
    /// <summary>
    /// Headless Android build used by CI (and menu BiRyeoDo/Build Android APK).
    /// Runs the project setup first so a fresh checkout (no generated assets or scene) still builds.
    /// </summary>
    public static class BuildScript
    {
        public const string OutputPath = "build/Android/BiRyeoDo.apk";

        [MenuItem("BiRyeoDo/Build Android APK")]
        public static void BuildAndroid()
        {
            ProjectSetup.Run();

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            EditorUserBuildSettings.buildAppBundle = false;
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_Project/Scenes/Boot.unity" },
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("Android build failed: " + report.summary.result);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            Debug.Log("APK written to " + Path.GetFullPath(OutputPath) + " (" + report.summary.totalSize / (1024 * 1024) + " MB)");
        }
    }
}
