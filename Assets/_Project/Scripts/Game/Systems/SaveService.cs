using System;
using System.IO;
using System.Text;
using BiRyeoDo.Core;
using UnityEngine;

namespace BiRyeoDo.Game.Systems
{
    /// <summary>
    /// Local JSON save (single player, no server). A save is written to a temp file first and then swapped in, keeping the previous save as a backup,
    /// so there is never a moment without a good file on disk. Loading falls back to the temp file (a swap that was cut short) and then the backup.
    /// </summary>
    public static class SaveService
    {
        private static string Path_ { get { return Path.Combine(Application.persistentDataPath, "save.json"); } }
        private static string TempPath { get { return Path_ + ".tmp"; } }
        private static string BackupPath { get { return Path_ + ".bak"; } }

        public static SaveData Load()
        {
            // the main file first; a temp file left by a swap that was cut short is newer than the backup
            foreach (var path in new[] { Path_, TempPath, BackupPath })
            {
                var data = TryRead(path);
                if (data == null) continue;
                if (path != Path_) Debug.LogWarning("Save restored from " + System.IO.Path.GetFileName(path));
                return data;
            }
            return new SaveData();
        }

        private static SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null || data.Version != SaveData.CurrentVersion) return null;
                Gear.EnsureProgression(data);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Save load failed (" + System.IO.Path.GetFileName(path) + "): " + e.Message);
                return null;
            }
        }

        public static void Save(SaveData data)
        {
            try
            {
                var bytes = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(data));
                using (var fs = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush(true);   // on disk before the swap, so a power cut cannot leave an empty file behind
                }
                if (!File.Exists(Path_)) { File.Move(TempPath, Path_); return; }
                try
                {
                    File.Replace(TempPath, Path_, BackupPath);   // one step: the old save becomes the backup
                }
                catch (Exception)
                {
                    // platforms without File.Replace: keep a backup copy first, so the main file is never the only copy while it is swapped
                    File.Copy(Path_, BackupPath, true);
                    File.Delete(Path_);
                    File.Move(TempPath, Path_);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Save failed: " + e.Message);
            }
        }
    }
}
