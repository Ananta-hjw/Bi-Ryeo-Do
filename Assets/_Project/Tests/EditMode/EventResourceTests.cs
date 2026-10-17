using System.Linq;
using NUnit.Framework;
using BiRyeoDo.Core;

namespace BiRyeoDo.Tests
{
    public class EventResourceTests
    {
        [Test]
        public void EventsCarryTheResourcesNeededToAnimateBars()
        {
            var st = EarlyGameContent.FindStage(1);
            var party = SimpleBot.BuildParty(1, st.RecommendedLevel);
            var enemies = st.Encounters[0].Select((id, i) => EarlyGameContent.FindEnemy(id).CreateCombatant(id + i)).ToList();
            var battle = new InteractiveBattle(party, enemies, new SeededRandom(5));
            battle.Begin();
            var hero = party[0];
            var art = hero.Skills.First(s => s.Id != "basic" && s.QiCost > 0 && hero.CanUse(s));
            int qiBefore = hero.Qi, spiritBefore = hero.Spirit;
            var events = battle.PlayerAct(new PlayerCommand { Kind = CommandKind.Skill, Skill = art, Target = battle.Enemies[0] });

            var own = events.First(e => e.ActorId == hero.Id);
            Assert.AreEqual(qiBefore - art.QiCost, own.ActorQiAfter);
            Assert.AreEqual(spiritBefore + InteractiveBattle.SpiritOnDealt + hero.SpiritDealtBonus, own.ActorSpiritAfter);

            var reply = events.Where(e => e.TargetId == hero.Id && e.Kind == EventKind.Attack).ToList();
            Assert.IsNotEmpty(reply);
            Assert.AreEqual(hero.Spirit, reply.Last().TargetSpiritAfter);
            Assert.Greater(reply.Last().TargetSpiritAfter, own.ActorSpiritAfter);
        }
    }
}
