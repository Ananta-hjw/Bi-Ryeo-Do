using System;
using System.IO;
using BiRyeoDo.Core;
using UnityEngine;

namespace BiRyeoDo.Game.Systems
{
    /// <summary>Local JSON save (single player, no server). Writes via temp file so a crash can't corrupt the save.</summary>
    public static class SaveService
    {
        private static string Path_ { get { return Path.Combine(Application.persistentDataPath, "save.json"); } }

        public static SaveData Load()
        {
            try
            {
                if (File.Exists(Path_))
                {
                    var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(Path_));
                    if (data != null) return data;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Save load failed, starting fresh: " + e.Message);
            }
            return new SaveData();
        }

        public static void Save(SaveData data)
        {
            try
            {
                string tmp = Path_ + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(data));
                if (File.Exists(Path_)) File.Delete(Path_);
                File.Move(tmp, Path_);
            }
            catch (Exception e)
            {
                Debug.LogError("Save failed: " + e.Message);
            }
        }
    }
}
