using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// Who is in the party when companions arrive: a newcomer never pushes anyone out. He joins on the bench (대기) and the player
    /// decides in 파티 편성 whether and where he fights.
    /// </summary>
    public static class Roster
    {
        public static string[] StoryActorsFor(StageData stage, int encounter)
        {
            return stage != null && !stage.IsQuest && !stage.IsTower && stage.StoryActors != null
                && encounter >= 0 && encounter < stage.StoryActors.Length ? stage.StoryActors[encounter] : null;
        }

        public static bool SamePerspective(StageData stage, int first, int second)
        {
            var a = StoryActorsFor(stage, first); var b = StoryActorsFor(stage, second);
            if (a == null || b == null) return a == b;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        public static bool IsStoryActor(string id) { return id != null && id.StartsWith("story_", System.StringComparison.Ordinal); }
        public static string StoryArtKey(string id) { return IsStoryActor(id) ? id.Substring(6) : id; }

        public static string StoryName(string id)
        {
            switch (id)
            {
                case "player": return "비류연";
                case "story_yu_jaeryong": return "유재룡";
                case "story_jang_uyang": return "장우양";
                case "story_jang_ugyeong": return "장우경";
                case "story_namgung_sang": return "남궁상";
                case "story_mo_yonghwi": return "모용휘";
                case "story_gongson_jeolhwi": return "공손절휘";
                case "story_yu_eunseong": return "유은성";
                case "story_yu_gyeongyeong": return "유경영";
                case "story_i_jinseol": return "이진설";
            }
            var companion = EarlyGameContent.FindCompanion(id);
            return companion != null ? companion.Name : null;
        }

        public static string StoryPartySummary(StageData stage)
        {
            if (stage == null || stage.IsQuest || stage.IsTower || stage.StoryActors == null) return "";
            var lines = new List<string>();
            for (int i = 0; i < stage.Encounters.Length; i++)
            {
                var actors = StoryActorsFor(stage, i);
                if (actors == null) continue;
                var names = new List<string>();
                foreach (var id in actors) names.Add(StoryName(id));
                lines.Add("전투 " + (i + 1) + ": " + string.Join(" · ", names));
            }
            return string.Join("\n", lines);
        }

        /// <summary>Temporary actors borrow the hero's prepared stats, not his arts or persistent identity. Real companions keep their own growth.</summary>
        public static List<Combatant> CreateStoryParty(StageData stage, int encounter, SaveData save)
        {
            var ids = StoryActorsFor(stage, encounter);
            if (ids == null) return null;
            if (ids.Length < 1 || ids.Length > EarlyGameContent.MaxCompanions + 1) throw new System.ArgumentException("Invalid story party size");
            var result = new List<Combatant>();
            var unique = new HashSet<string>();
            foreach (var id in ids) if (!unique.Add(id)) throw new System.ArgumentException("Duplicate story actor: " + id);
            var reference = Formation.BuildHero(save);
            foreach (var id in ids)
            {
                var companion = EarlyGameContent.FindCompanion(id);
                if (id == Inventory.Hero) result.Add(reference);
                else if (companion != null) result.Add(CompanionGrowth.Create(save, companion));
                else
                {
                    string name = StoryName(id);
                    if (!IsStoryActor(id) || name == null) throw new System.ArgumentException("Unknown story actor: " + id);
                    result.Add(new Combatant(id, name, Team.Player, reference.Stats, StorySkills(id)));
                }
            }
            foreach (var member in result) member.StoryControlled = true;
            Formation.Apply(result, null);   // Fixed scenes never rewrite the saved regular-party formation.
            return result;
        }

        private static IEnumerable<SkillSpec> StorySkills(string id)
        {
            var arts = new List<SkillSpec>();
            if (id == "story_yu_gyeongyeong")
            {
                arts.Add(new SkillSpec { Id = "story_hide", Name = "몸을 숨긴다", Description = "동생을 감싸고 위험을 피합니다.", Effect = SkillEffect.Evade, Power = 0, QiCost = 12, Cooldown = 1 });
                return arts;
            }
            string sword = id == "story_yu_jaeryong" ? "청룡검" : id == "story_namgung_sang" ? "뇌전검" : id == "story_mo_yonghwi" ? "은하유성검" : id == "story_gongson_jeolhwi" ? "지존검" : id == "story_yu_eunseong" ? "낙일검" : id == "story_i_jinseol" ? "쌍검" : "호위 공방";
            arts.Add(new SkillSpec { Id = "story_strike", Name = sword + " 연격", Description = "빈틈을 노려 두 번 공격합니다.", Power = 2.4f, Hits = 2, QiCost = 18, Cooldown = 1 });
            arts.Add(new SkillSpec { Id = "story_sweep", Name = sword + " 횡격", Description = "앞을 가로막은 상대들을 공격합니다.", Power = 2.6f, AllEnemies = true, Range = 3, QiCost = 24, Cooldown = 2 });
            arts.Add(new SkillSpec { Id = "story_footwork", Name = id == "story_namgung_sang" ? "일촌간파" : "틈새 보법", Description = "움직임을 읽고 다음 공세를 피합니다.", Effect = SkillEffect.Evade, Power = 0, QiCost = 16, Cooldown = 2 });
            return arts;
        }

        /// <summary>Companions who become available when the furthest stage reached grows from <paramref name="oldHighest"/> to <paramref name="newHighest"/>.</summary>
        public static List<CompanionData> NewJoiners(int oldHighest, int newHighest)
        {
            var list = new List<CompanionData>();
            foreach (var c in EarlyGameContent.Companions)
                if (c.JoinStage > oldHighest && c.JoinStage <= newHighest) list.Add(c);
            return list;
        }

        /// <summary>Seats the newcomers on the bench so the current party and its formation stay exactly as the player set them.</summary>
        public static void BenchNewcomers(SaveData save, IEnumerable<CompanionData> joined)
        {
            foreach (var c in joined)
                if (!save.BenchedCompanions.Contains(c.Id) && !save.ActiveCompanions.Contains(c.Id)) save.BenchedCompanions.Add(c.Id);
        }

        /// <summary>The stage number whose companions travel along: a story stage uses its own, quests and tower floors use everyone who has joined so far.</summary>
        public static int PartyStage(StageData stage, SaveData save)
        {
            return stage.IsQuest || stage.IsTower ? System.Math.Max(1, save.HighestUnlockedStage) : stage.Number;
        }
    }
}
