using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StageProgressTests
    {
        private static Combatant Member(string id, int hp)
        {
            return new Combatant(id, id, Team.Player, new CombatStats { MaxHp = hp, MaxQi = 10, Attack = 1, Defense = 0, Speed = 1, CritDamage = 1.5f }, null);
        }

        [Test]
        public void StoresAndRestoresPartyHp()
        {
            var save = new SaveData();
            var hero = Member("player", 100); hero.Hp = 40;
            var friend = Member("hyo_ryong", 80); friend.Hp = 80;
            StageProgress.Store(save, 7, 2, new[] { hero, friend });
            Assert.IsTrue(StageProgress.Has(save, 7));
            Assert.IsFalse(StageProgress.Has(save, 8));
            var fresh = new[] { Member("player", 100), Member("hyo_ryong", 80) };
            StageProgress.Restore(save, fresh);
            Assert.AreEqual(40, fresh[0].Hp);
            Assert.AreEqual(80, fresh[1].Hp);
            StageProgress.Clear(save);
            Assert.IsFalse(StageProgress.Has(save, 7));
        }

        [Test]
        public void RestoreNeverExceedsMaxOrKills()
        {
            var save = new SaveData();
            StageProgress.Store(save, 3, 1, new[] { Member("player", 500) });
            save.ProgressHp[0].Hp = 0;
            var a = Member("player", 100);
            StageProgress.Restore(save, new[] { a });
            Assert.AreEqual(1, a.Hp);
            save.ProgressHp[0].Hp = 999;
            StageProgress.Restore(save, new[] { a });
            Assert.AreEqual(100, a.Hp);
        }

        [Test]
        public void NewcomerStartsAtTheAverageWoundLevel()
        {
            var save = new SaveData();
            var hero = Member("player", 100); hero.Hp = 50;
            StageProgress.Store(save, 9, 1, new[] { hero });
            var party = new[] { Member("player", 100), Member("jang_hong", 200) };
            StageProgress.Restore(save, party);
            Assert.AreEqual(50, party[0].Hp);
            Assert.AreEqual(100, party[1].Hp);   // 50% of 200, not full health
        }
    }
}
