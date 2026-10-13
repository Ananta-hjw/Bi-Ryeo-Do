using System.Collections;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Data;
using BiRyeoDo.Game.Systems;
using UnityEngine;

namespace BiRyeoDo.Game.Battle
{
    /// <summary>
    /// Bridges the pure-C# BattleEngine to the scene: resolves the fight, then plays the events back with delays.
    /// The UI listens to <see cref="EventPlayed"/> / <see cref="BattleFinished"/>; this class never touches UI.
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        public float secondsPerAction = 0.6f;
        public System.Action<BattleEvent> EventPlayed;
        public System.Action<BattleResult> BattleFinished;

        public Combatant Player { get; private set; }
        public Combatant Enemy { get; private set; }
        public bool IsBusy { get; private set; }

        public void StartBattle(EnemyDef enemy, IEnumerable<MartialArtDef> equippedArts)
        {
            if (IsBusy) return;
            var save = GameManager.Instance.Save;
            var specs = new List<SkillSpec>();
            foreach (var a in equippedArts) if (a != null) specs.Add(a.ToSpec());

            Player = new Combatant("player", "비류연", Team.Player, Progression.StatsFor(save.Level, save.Life), specs);
            Enemy = enemy.Create();
            var engine = new BattleEngine(new[] { Player }, new[] { Enemy }, new SeededRandom(System.Environment.TickCount));
            IsBusy = true;
            StartCoroutine(Play(engine.Run(), enemy));
        }

        public string SkillName(Combatant who, string skillId)
        {
            foreach (var s in who.Skills) if (s.Id == skillId) return s.Name;
            return skillId;
        }

        private IEnumerator Play(BattleResult result, EnemyDef enemy)
        {
            foreach (var ev in result.Events)
            {
                // Replay is precomputed, so HP shown to the UI comes from the event, not from the live combatants.
                if (EventPlayed != null) EventPlayed(ev);
                yield return new WaitForSeconds(secondsPerAction);
            }

            if (result.PlayerWon)
            {
                var save = GameManager.Instance.Save;
                save.Doo += enemy.dooReward;
                Progression.AddExp(save, enemy.expReward);
                GameManager.Instance.SaveNow();
            }
            IsBusy = false;
            if (BattleFinished != null) BattleFinished(result);
        }
    }
}
