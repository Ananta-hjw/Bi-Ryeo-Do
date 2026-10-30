using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    /// <summary>전투 인원 확장: nameless followers fill out fights and pay a half share each, and harder difficulties field the player's party.</summary>
    public class FightRosterTests
    {
        [Test]
        public void FollowersJoinTheOriginalLineUpAndPayAHalfShare()
        {
            foreach (var st in EarlyGameContent.Stages.Where(s => s.RewardEncounters != null))
            {
                Assert.AreEqual(st.Encounters.Length, st.RewardEncounters.Length, st.Name);
                for (int k = 0; k < st.Encounters.Length; k++)
                {
                    if (st.EventFor(k) != null) { Assert.IsEmpty(st.Encounters[k]); continue; }
                    Assert.GreaterOrEqual(st.Encounters[k].Length, st.RewardEncounters[k].Length, st.Name + " " + k);
                    // every story opponent is still there (stages 3 and 4 swap one 산적 for the 도부수)
                    foreach (var id in st.RewardEncounters[k].Distinct())
                        if (!id.EndsWith("_bandit")) Assert.GreaterOrEqual(st.Encounters[k].Count(x => x == id), st.RewardEncounters[k].Count(x => x == id), st.Name + " " + id);
                }
            }
            foreach (var e in EarlyGameContent.Enemies.Where(e => e.IsMinion))
                Assert.IsFalse(EarlyGameContent.Drops.Any(d => d.EnemyId == e.Id), e.Id + " drops nothing");
            var stage3 = EarlyGameContent.FindStage(3);
            CollectionAssert.AreEqual(new[] { 3, 4, 3 }, stage3.Encounters.Select(x => x.Length));
            long exp = 0;
            for (int k = 0; k < stage3.Encounters.Length; k++) exp += EarlyGameContent.RewardIds(stage3, k).Sum(id => EarlyGameContent.ExpReward(stage3, id));
            long old = stage3.RewardEncounters.SelectMany(x => x).Sum(id => EarlyGameContent.ExpReward(stage3, id));
            Assert.AreEqual(old, exp, "the original line-up keeps its shares");
            // each added follower pays half an ordinary enemy on top
            Assert.AreEqual(1, EarlyGameContent.FollowerCount(stage3, 0));
            long one = EarlyGameContent.ExpReward(stage3, "hogol_bandit");
            Assert.That(EarlyGameContent.FollowerExp(stage3, 0), Is.EqualTo(one * EarlyGameContent.FollowerRewardShare).Within(1));
            Recommend.FightReward(new SaveData(), stage3, 0, false, out long paid, out long _);
            Assert.AreEqual(EarlyGameContent.RewardIds(stage3, 0).Sum(id => EarlyGameContent.ExpReward(stage3, id)) + EarlyGameContent.FollowerExp(stage3, 0), paid);
        }

        [Test]
        public void HarderDifficultiesFieldTheJoinedPartyAndDressEarlyEnemies()
        {
            var save = new SaveData { ClearedStage = 25 };
            var stage = EarlyGameContent.FindStage(3);
            Assert.AreEqual(3, Roster.PartyStage(stage.WithDifficulty(0), save));
            Assert.AreEqual(save.HighestUnlockedStage, Roster.PartyStage(stage.WithDifficulty(1), save));
            Assert.Greater(EarlyGameContent.SelectCompanionsFor(Roster.PartyStage(stage.WithDifficulty(2), save), save).Count, 0);
            var bandit = EarlyGameContent.FindEnemy("hogol_bandit");
            Assert.IsNull(StageStars.StoryEnemy(bandit, 3, 0).Gear, "일반 1~6 enemies stay bare");
            Assert.IsNotNull(StageStars.StoryEnemy(bandit, 3, 1).Gear);
            Assert.IsNotNull(StageStars.StoryEnemy(bandit, 3, 2).Gear);
        }
    }
}
