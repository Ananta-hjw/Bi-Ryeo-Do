using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>Everything persisted between sessions. Public fields so Unity's JsonUtility can serialize it.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public int Life = 1;            // 제N생: reincarnation count
        public int Level = 1;           // 경지/수련 단계 within the current life
        public long Exp;
        public long Doo;                // 두: main currency
        public int Stage = 1;           // highest stage reached this life
        public List<string> LearnedArts = new List<string>();
        public List<string> EquippedArts = new List<string>();
    }
}
