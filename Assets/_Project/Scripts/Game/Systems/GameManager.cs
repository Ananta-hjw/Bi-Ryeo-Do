using BiRyeoDo.Core;
using UnityEngine;

namespace BiRyeoDo.Game.Systems
{
    /// <summary>Owns the SaveData for the session and autosaves when the app is backgrounded.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public SaveData Save { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Save = SaveService.Load();
            EnsureStarterArt();
        }

        private void EnsureStarterArt()
        {
            if (Save.LearnedArts.Count == 0) Save.LearnedArts.Add(ContentDatabase.StarterArtId);
            if (Save.EquippedArts.Count == 0) Save.EquippedArts.Add(ContentDatabase.StarterArtId);
        }

        public void SaveNow() { SaveService.Save(Save); }

        private void OnApplicationPause(bool paused) { if (paused) SaveNow(); }

        private void OnApplicationQuit() { SaveNow(); }
    }
}
