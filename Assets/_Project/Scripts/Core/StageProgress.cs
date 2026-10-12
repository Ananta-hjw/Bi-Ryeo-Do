using System;

namespace BiRyeoDo.Core
{
    /// <summary>Tracks normal-enemy kills in the current stage; unlocks the boss challenge after enough kills.</summary>
    public sealed class StageProgress
    {
        public int Kills { get; private set; }
        public int KillsToBoss { get; private set; }

        public bool BossReady { get { return Kills >= KillsToBoss; } }

        public StageProgress(int killsToBoss)
        {
            KillsToBoss = Math.Max(1, killsToBoss);
        }

        public void OnNormalWin()
        {
            if (Kills < KillsToBoss) Kills++;
        }

        /// <summary>Call after the boss is beaten; the caller then loads the next stage.</summary>
        public void Reset(int killsToBoss)
        {
            Kills = 0;
            KillsToBoss = Math.Max(1, killsToBoss);
        }
    }
}
