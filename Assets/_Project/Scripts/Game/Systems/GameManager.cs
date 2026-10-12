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
        }

        public void SaveNow() { SaveService.Save(Save); }

        private void OnApplicationPause(bool paused) { if (paused) SaveNow(); }

        private void OnApplicationQuit() { SaveNow(); }
    }
}
