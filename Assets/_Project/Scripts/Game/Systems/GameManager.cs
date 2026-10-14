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
            Inventory.EnsureStartKit(Save);
        }

        /// <summary>Every art that costs nothing is known from the start and equipped (up to the limit).</summary>
        private void EnsureStarterArt()
        {
            foreach (var art in EarlyGameContent.Arts)
            {
                if (art.OwnerId != null || art.LearnCost > 0) continue;
                if (!Save.LearnedArts.Contains(art.Id)) Save.LearnedArts.Add(art.Id);
            }
            if (Save.EquippedArts.Count == 0)
                foreach (var id in Save.LearnedArts)
                    if (Save.EquippedArts.Count < Progression.MaxEquippedArts) Save.EquippedArts.Add(id);
        }

        public void SaveNow() { SaveService.Save(Save); }

        private void OnApplicationPause(bool paused) { if (paused) SaveNow(); }

        private void OnApplicationQuit() { SaveNow(); }
    }
}
