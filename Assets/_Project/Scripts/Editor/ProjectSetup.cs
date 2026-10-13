using System.IO;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Data;
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
        private const string DataDir = "Assets/_Project/Resources/Data"; // loaded at runtime by ContentDatabase
        private const string BootScene = "Assets/_Project/Scenes/Boot.unity";

        [MenuItem("BiRyeoDo/Setup Project")]
        public static void Run()
        {
            ConfigureAndroid();
            CreateStoryContent();
            CreateBootScene();
            AssetDatabase.SaveAssets();
            Debug.Log("BiRyeoDo setup done.");
        }

        private static void ConfigureAndroid()
        {
            PlayerSettings.companyName = "BiRyeoDo";
            PlayerSettings.productName = "비뢰도";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.biryeodo.game");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            EditorUserBuildSettings.buildAppBundle = false; // sideload APK; personal use only
        }

        [MenuItem("BiRyeoDo/Rebuild Story Content")]
        public static void RebuildContent()
        {
            CreateStoryContent();
            AssetDatabase.SaveAssets();
            Debug.Log("BiRyeoDo story content rebuilt.");
        }

        private static T Create<T>(string name, System.Action<T> init) where T : ScriptableObject
        {
            var so = ScriptableObject.CreateInstance<T>();
            init(so);
            AssetDatabase.CreateAsset(so, DataDir + "/" + name + ".asset");
            return so;
        }

        /// <summary>Regenerates every art/enemy/stage asset from <see cref="EarlyGameContent"/> (volumes 1-3). Old assets in the folder are replaced.</summary>
        private static void CreateStoryContent()
        {
            Directory.CreateDirectory(DataDir);
            AssetDatabase.Refresh();
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { DataDir }))
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));

            foreach (var a in EarlyGameContent.Arts)
            {
                var d = a;
                Create<MartialArtDef>("Art_" + d.Id, x =>
                {
                    x.id = d.Id; x.displayName = d.Name; x.description = d.Description; x.power = d.Power;
                    x.qiCost = d.QiCost; x.cooldown = d.Cooldown; x.learnCost = d.LearnCost; x.unlockStage = d.UnlockStage;
                });
            }

            var enemies = new System.Collections.Generic.Dictionary<string, EnemyDef>();
            foreach (var e in EarlyGameContent.Enemies)
            {
                var d = e;
                enemies[d.Id] = Create<EnemyDef>("Enemy_" + d.Id, x =>
                {
                    x.id = d.Id; x.displayName = d.Name; x.isBoss = d.IsBoss; x.stats = d.BuildStats();
                    x.expReward = d.ExpReward; x.dooReward = d.DooReward;
                });
            }

            foreach (var s in EarlyGameContent.Stages)
            {
                var d = s;
                Create<StageDef>("Stage_" + d.Number.ToString("00"), x =>
                {
                    x.number = d.Number; x.displayName = d.Name; x.storyText = d.Story; x.killsToBoss = d.KillsToBoss;
                    x.boss = enemies[d.BossId];
                    x.enemies = System.Array.ConvertAll(d.EnemyIds, id => enemies[id]);
                });
            }
            AssetDatabase.Refresh();
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
