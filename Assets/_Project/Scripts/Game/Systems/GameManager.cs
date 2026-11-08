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
            Mastery.MigrateForms(Save);   // arts folded into a line's 무공 변화, or taken out
            EnsureStarterArt();
            Inventory.EnsureStartKit(Save);
            Discovery.Sync(Save);
            gameObject.AddComponent<SoundManager>();
        }

        /// <summary>Throws the whole save away and starts a fresh game (options are kept).</summary>
        public void ResetSave()
        {
            var settings = Save.Settings;
            Save = new SaveData { Settings = settings ?? new GameSettings() };
            EnsureStarterArt();
            Inventory.EnsureStartKit(Save);
            Discovery.Sync(Save);
            SaveNow();
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
                {
                    string replaced;
                    Progression.EquipArt(Save, EarlyGameContent.FindArt(id), out replaced);
                }
        }

        private bool _dirty;

        public void SaveNow() { _dirty = false; SaveService.Save(Save); }

        /// <summary>Notes a change that can wait (in-battle 숙련 counts, a used item): written by <see cref="SaveIfDirty"/>, the next <see cref="SaveNow"/> or when the app is paused.</summary>
        public void MarkDirty() { _dirty = true; }

        public void SaveIfDirty() { if (_dirty) SaveNow(); }

        private void OnApplicationPause(bool paused) { if (paused) SaveNow(); }

        private void OnApplicationQuit() { SaveNow(); }
    }
}
