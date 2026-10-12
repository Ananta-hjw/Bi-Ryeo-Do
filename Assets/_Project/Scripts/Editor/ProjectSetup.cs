using System.IO;
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
            CreateSampleData();
            CreateBootScene();
            AssetDatabase.SaveAssets();
            Debug.Log("BiRyeoDo setup done.");
        }

        private static void ConfigureAndroid()
        {
            PlayerSettings.companyName = "BiRyeoDo";
            PlayerSettings.productName = "비려도";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.biryeodo.game");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            EditorUserBuildSettings.buildAppBundle = false; // sideload APK; personal use only
        }

        private static T Make<T>(string name, System.Action<T> init) where T : ScriptableObject
        {
            string path = DataDir + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var so = ScriptableObject.CreateInstance<T>();
            init(so);
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        private static void CreateSampleData()
        {
            Directory.CreateDirectory(DataDir);
            AssetDatabase.Refresh();
            var basic = Make<MartialArtDef>("Art_BasicSword", a => { a.id = "sword_basic"; a.displayName = "기본 검법"; a.power = 1.2f; });
            Make<MartialArtDef>("Art_OgwaSword", a => { a.id = "sword_ogwa"; a.displayName = "오과검술"; a.power = 2f; a.qiCost = 15; a.cooldown = 2; a.learnCost = 100; });

            var boar = Make<EnemyDef>("Enemy_Boar", e =>
            {
                e.id = "boar"; e.displayName = "맷돼지 괴물";
                e.stats = new BiRyeoDo.Core.CombatStats { MaxHp = 365, MaxQi = 0, Attack = 8, Defense = 2, Speed = 90, CritRate = 0.01f, CritDamage = 1.5f };
            });
            var ape = Make<EnemyDef>("Boss_WhiteApe", e =>
            {
                e.id = "white_ape"; e.displayName = "백원왕"; e.isBoss = true; e.expReward = 100; e.dooReward = 80;
                e.stats = new BiRyeoDo.Core.CombatStats { MaxHp = 2500, MaxQi = 0, Attack = 25, Defense = 10, Speed = 105, CritRate = 0.05f, CritDamage = 1.5f };
            });
            var guard = Make<EnemyDef>("Enemy_Guard", e =>
            {
                e.id = "guard"; e.displayName = "갑사";
                e.stats = new BiRyeoDo.Core.CombatStats { MaxHp = 600, MaxQi = 0, Attack = 15, Defense = 8, Speed = 95, CritRate = 0.02f, CritDamage = 1.5f };
            });
            Make<StageDef>("Stage_03_MistBamboo", s =>
            {
                s.number = 3; s.displayName = "안개 대숲";
                s.storyText = "추격자를 피해 이 숲에 들어왔다. (스토리는 추후 교체)";
                s.enemies = new[] { boar }; s.boss = ape;
            });
            Make<StageDef>("Stage_04_ChuFortress", s =>
            {
                s.number = 4; s.displayName = "제나라 요새";
                s.storyText = "제나라의 요새 문은 굳게 닫혔고, 수비군은 성벽에 접근하는 모든 난민을 무차별 사살하고 있다.";
                s.enemies = new[] { guard }; s.boss = ape; s.killsToBoss = 12;
            });
            Debug.Log("Sample data ready (" + basic.displayName + " ...)");
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
