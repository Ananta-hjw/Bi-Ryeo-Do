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
    /// Hook <see cref="EventPlayed"/> to drive animations, damage numbers and HP bars.
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        public float secondsPerAction = 0.6f;
        public System.Action<BattleEvent> EventPlayed;
        public System.Action<BattleResult> BattleFinished;

        public void StartBattle(EnemyDef enemy, IEnumerable<MartialArtDef> equippedArts)
        {
            var save = GameManager.Instance.Save;
            var specs = new List<SkillSpec>();
            foreach (var a in equippedArts) specs.Add(a.ToSpec());

            var player = new Combatant("player", "협객", Team.Player, Progression.StatsFor(save.Level, save.Life), specs);
            var engine = new BattleEngine(new[] { player }, new[] { enemy.Create() }, new SeededRandom(System.Environment.TickCount));
            StartCoroutine(Play(engine.Run(), enemy));
        }

        private IEnumerator Play(BattleResult result, EnemyDef enemy)
        {
            foreach (var ev in result.Events)
            {
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
            if (BattleFinished != null) BattleFinished(result);
        }
    }
}
