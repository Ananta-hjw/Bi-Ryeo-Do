using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// Who is in the party when companions arrive: a newcomer never pushes anyone out. He joins on the bench (대기) and the player
    /// decides in 파티 편성 whether and where he fights.
    /// </summary>
    public static class Roster
    {
        /// <summary>The fixed story party of a fight, or null when the player's party fights (always from <see cref="ReenactFrom"/> on).</summary>
        public static string[] StoryActorsFor(StageData stage, int encounter)
        {
            return Reenacted(stage) ? null : CanonActorsFor(stage, encounter);
        }

        /// <summary>Who fought this fight in the original text (the stage's <see cref="StageData.StoryActors"/>), whether or not the game fixes the party.</summary>
        public static string[] CanonActorsFor(StageData stage, int encounter)
        {
            return stage != null && !stage.IsQuest && !stage.IsTower && stage.StoryActors != null
                && encounter >= 0 && encounter < stage.StoryActors.Length ? stage.StoryActors[encounter] : null;
        }

        /// <summary>From this stage on every story fight is reenacted by the player's party (2부: 남궁상·모용휘 have joined, the party is four).</summary>
        public static int ReenactFrom = 31;   // a field so the balance probes can compare with the old fixed story parties

        /// <summary>A story stage whose fights are reenacted by the player's party instead of a fixed story party.</summary>
        public static bool Reenacted(StageData stage) { return stage != null && stage.Number >= ReenactFrom && !stage.IsQuest && !stage.IsTower; }

        /// <summary>
        /// Who a story actor is in the player's party: 비류연's scene bodies (연비, 왼손 우산, 정신봉, 탈출 중 …) are 비류연, the story bodies of
        /// people who later became companions (남궁상, 모용휘) are those companions; anyone else is the id itself (not in the party).
        /// </summary>
        public static string CompanionIdOf(string actorId)
        {
            switch (actorId)
            {
                case "story_namgung_sang": return "namgung_sang";
                case "story_mo_yonghwi": return "mo_yonghwi";
                case "story_yeonbi": case "story_young_yeonbi": case "story_yeonbi_left": case "story_biry_staff": case "story_biry_search":
                case "story_biry_half": case "story_biry_rescue": case "story_biry_escape":
                    return Inventory.Hero;
                default: return actorId;
            }
        }

        /// <summary>Companions the story needs in this fight of a reenacted stage: they always fight, whatever the player's picks or bench say.</summary>
        public static List<CompanionData> RequiredFor(StageData stage, int encounter)
        {
            var list = new List<CompanionData>();
            if (!Reenacted(stage)) return list;
            var actors = CanonActorsFor(stage, encounter);
            if (actors == null) return list;
            foreach (var id in actors)
            {
                var c = EarlyGameContent.FindCompanion(CompanionIdOf(id));
                if (c != null && c.JoinStage <= stage.Number && !list.Contains(c) && list.Count < EarlyGameContent.MaxCompanions) list.Add(c);
            }
            return list;
        }

        private static bool HasSceneTechnique(StageData stage, int encounter)
        {
            var trial = stage.TrialFor(encounter);
            return trial != null && trial.Kind == TrialKind.Technique;
        }

        /// <summary>40 「은빛 눈보라」의 마지막 공방: 나예린's unfinished new sword (scene only) takes her 백혼검뢰 line's place.</summary>
        private static void SilverSnow(StageData stage, int encounter, IList<Combatant> party)
        {
            if (stage.Number != 40 || encounter != 3) return;
            Combatant heroine = null;
            foreach (var m in party) if (m.Id == "na_yerin") heroine = m;
            if (heroine == null) return;
            heroine.Skills.RemoveAll(skill => skill.Id == "ny_baekhon" || CompanionTraining.Succeeds(skill.Id, "ny_baekhon"));
            heroine.Skills.Insert(1, new SkillSpec { Id = "scene_silver_snow", Name = "새 검기", Description = "비설보를 바탕으로 펼치는 미완의 검기. 이 공방에서만 사용합니다.", Power = 6f, AllEnemies = true, Range = 3, QiCost = 24, Cooldown = 1 });
        }

        /// <summary>True when the companion is needed in any fight of the stage (it cannot be benched for it).</summary>
        public static bool RequiredInStage(StageData stage, string companionId)
        {
            if (!Reenacted(stage) || stage.Encounters == null) return false;
            for (int i = 0; i < stage.Encounters.Length; i++)
                foreach (var c in RequiredFor(stage, i)) if (c.Id == companionId) return true;
            return false;
        }

        /// <summary>
        /// The companions who fight this fight: on a reenacted stage the required ones first, then the player's own picks up to
        /// <see cref="EarlyGameContent.MaxCompanions"/>; elsewhere just the player's picks.
        /// </summary>
        public static List<CompanionData> CompanionsFor(StageData stage, int encounter, SaveData save)
        {
            var picks = EarlyGameContent.SelectCompanionsFor(PartyStage(stage, save), save);
            var result = RequiredFor(stage, encounter);
            foreach (var c in picks) if (!result.Contains(c) && result.Count < EarlyGameContent.MaxCompanions) result.Add(c);
            return result;
        }

        /// <summary>A reenacted stage keeps the same party between fights unless the required companions change; then the wounds carry over (<see cref="CarryWounds"/>).</summary>
        public static bool SamePerspective(StageData stage, int first, int second)
        {
            if (Reenacted(stage))
            {
                if (HasSceneTechnique(stage, first) || HasSceneTechnique(stage, second)) return false;   // rebuilt so the borrowed slot comes back
                var x = RequiredFor(stage, first); var y = RequiredFor(stage, second);
                if (x.Count != y.Count) return false;
                for (int i = 0; i < x.Count; i++) if (x[i] != y[i]) return false;
                return true;
            }
            var a = StoryActorsFor(stage, first); var b = StoryActorsFor(stage, second);
            if (a == null || b == null) return a == b;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        public static bool IsStoryActor(string id) { return id != null && id.StartsWith("story_", System.StringComparison.Ordinal); }
        public static string StoryArtKey(Combatant unit) { return unit.StoryArtId ?? StoryArtKey(unit.Id); }
        public static string StoryArtKey(string id) { return StoryEscape.ArtKey(id) ?? StoryRescue.ArtKey(id) ?? StoryTournament.ArtKey(id) ?? (IsStoryActor(id) ? id.Substring(6) : id); }

        public static string StoryName(string id)
        {
            switch (id)
            {
                case "player": return "비류연";
                case "story_yeonbi": return "연비";
                case "story_young_yeonbi": return "어린 연비";
                case "story_ma_haryeong": return "마하령";
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
            return companion != null ? companion.Name : StoryEscape.ActorName(id) ?? StoryRescue.ActorName(id) ?? StoryTournament.ActorName(id);
        }

        /// <summary>
        /// For the prep window of a reenacted stage: per fight, who fought it in the original text and which companions must come along.
        /// </summary>
        public static string ReenactSummary(StageData stage)
        {
            if (!Reenacted(stage)) return "";
            var lines = new List<string>();
            for (int i = 0; i < stage.Encounters.Length; i++)
            {
                var actors = CanonActorsFor(stage, i);
                if (actors == null) continue;
                var names = new List<string>();
                foreach (var id in actors) names.Add(StoryName(id));
                var required = new List<string>();
                foreach (var c in RequiredFor(stage, i)) required.Add(c.Name);
                lines.Add("전투 " + (i + 1) + ": 원작 " + string.Join(" · ", names) + (required.Count > 0 ? " → 필수 " + string.Join(" · ", required) : ""));
            }
            return string.Join("\n", lines);
        }

        /// <summary>After the party is rebuilt for another fight, the members who fought before keep their 체력·내공·기세.</summary>
        public static void CarryWounds(IList<Combatant> before, IList<Combatant> after)
        {
            if (before == null || after == null) return;
            foreach (var m in after)
                foreach (var old in before)
                {
                    if (old.Id != m.Id) continue;
                    m.Hp = Math.Min(m.Stats.MaxHp, Math.Max(old.IsAlive ? 1 : 0, (int)Math.Round(m.Stats.MaxHp * (double)old.Hp / Math.Max(1, old.Stats.MaxHp))));
                    m.Qi = Math.Min(m.Stats.MaxQi, (int)Math.Round(m.Stats.MaxQi * (double)old.Qi / Math.Max(1, old.Stats.MaxQi)));
                    m.Spirit = old.Spirit;
                }
        }

        /// <summary>
        /// A reenacted fight whose trial asks for a companion's scene technique (50 염천빙지 일원합, 52 기맥 관조, 64 비홍관운, 44 윤미의 칠매검):
        /// the companion keeps its own arts and gets only that technique (and the scene's name, 윤미), as the story body would have had it.
        /// Techniques of an earlier fight are taken away first.
        /// </summary>
        public static void AddSceneTechniques(StageData stage, int encounter, IList<Combatant> party)
        {
            if (!Reenacted(stage) || party == null) return;
            foreach (var m in party) m.Skills.RemoveAll(skill => skill.Id.StartsWith("scene_", StringComparison.Ordinal));
            SilverSnow(stage, encounter, party);
            var trial = stage.TrialFor(encounter);
            var actors = CanonActorsFor(stage, encounter);
            if (trial == null || trial.Kind != TrialKind.Technique || actors == null) return;
            var wanted = new HashSet<string>();
            if (trial.TechniqueSequence != null) foreach (var id in trial.TechniqueSequence) wanted.Add(id);
            else if (trial.TechniqueId != null) wanted.Add(trial.TechniqueId);
            foreach (var actor in actors)
            {
                Combatant member = null;
                foreach (var m in party) if (m.Id == CompanionIdOf(actor)) member = m;
                if (member == null) continue;
                var body = new Combatant(actor, member.Name, Team.Player, member.Stats, null);
                var one = new List<Combatant> { body };
                StoryTournament.PrepareParty(stage, encounter, one);
                StoryRescue.PrepareParty(stage, encounter, one);
                StoryEscape.PrepareParty(stage, encounter, one);
                int slots = member.Id == Inventory.Hero ? Progression.MaxEquippedArts : Progression.MaxEquippedArts + 1;
                foreach (var skill in body.Skills)
                {
                    if (!wanted.Contains(skill.Id) || member.Skills.Exists(x => x.Id == skill.Id)) continue;
                    // the command bar holds four ordinary arts for 비류연 and five for a companion: the scene technique takes the last one's place for this fight
                    int ordinary = member.Skills.FindAll(x => x.Id != "basic" && x.SpiritCost <= 0).Count;
                    if (ordinary >= slots)
                    {
                        int last = member.Skills.FindLastIndex(x => x.Id != "basic" && x.SpiritCost <= 0 && !wanted.Contains(x.Id));
                        if (last >= 0) member.Skills.RemoveAt(last);
                    }
                    member.Skills.Add(skill);
                }
                if (member.Id == Inventory.Hero) continue;   // 비류연 stays himself (연비's dress and names are scene presentation)
                if (body.Name != member.Name) member.Name = body.Name;
                if (body.StoryArtId != null) member.StoryArtId = body.StoryArtId;
            }
        }

        public static string StoryPartySummary(StageData stage)
        {
            if (stage == null || stage.IsQuest || stage.IsTower || stage.StoryActors == null || Reenacted(stage)) return "";
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

        /// <summary>Present-day temporary actors borrow the hero's prepared stats; childhood flashbacks use fixed stats. Real companions keep their own growth.</summary>
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
                    bool past = id == "story_young_yeonbi";
                    var stats = past ? Progression.HeroStatsAtCurve(30 + StageStars.LevelAdd(stage.Number, stage.Difficulty)) : reference.Stats;
                    result.Add(new Combatant(id, name, Team.Player, stats, StorySkills(id, encounter), past));
                }
            }
            SilverSnow(stage, encounter, result);
            StoryTournament.PrepareParty(stage, encounter, result);
            StoryRescue.PrepareParty(stage, encounter, result);
            StoryEscape.PrepareParty(stage, encounter, result);
            foreach (var member in result) member.StoryControlled = true;
            Formation.Apply(result, null);   // Fixed scenes never rewrite the saved regular-party formation.
            return result;
        }

        /// <summary>A new technique in the same flashback changes commands without replacing the wounded body.</summary>
        public static void PrepareStoryEncounter(StageData stage, int encounter, IEnumerable<Combatant> party)
        {
            if (Reenacted(stage)) { AddSceneTechniques(stage, encounter, new List<Combatant>(party)); return; }
            if (StoryActorsFor(stage, encounter) == null) return;
            StoryTournament.PrepareParty(stage, encounter, party);
            StoryRescue.PrepareParty(stage, encounter, party);
            StoryEscape.PrepareParty(stage, encounter, party);
            foreach (var member in party)
            {
                if (member.Id != "story_young_yeonbi") continue;
                member.Skills.RemoveAll(skill => skill.Id != "basic");
                member.Skills.AddRange(StorySkills(member.Id, encounter));
            }
        }

        private static IEnumerable<SkillSpec> StorySkills(string id, int encounter)
        {
            var arts = new List<SkillSpec>();
            if (id == "story_young_yeonbi")
            {
                if (encounter == 0) arts.Add(new SkillSpec { Id = "scene_palhwa", Name = "팔화난무", Power = .7f, Hits = 4, QiCost = 16, Cooldown = 1 });
                else arts.Add(new SkillSpec { Id = "scene_ssangseom", Name = "충각 쌍섬일격뢰", Power = 8f, QiCost = 24, Cooldown = 1 });
                arts.Add(new SkillSpec { Id = "scene_young_evade", Name = "비늘의 틈 읽기", Effect = SkillEffect.Evade, Power = 0, QiCost = 12, Cooldown = 2 });
                return arts;
            }
            if (id == "story_yeonbi")
            {
                arts.Add(new SkillSpec { Id = "scene_umbrella", Name = "현천은린 · 타격", Power = 2.5f, Hits = 2, QiCost = 18, Cooldown = 1 });
                arts.Add(new SkillSpec { Id = "scene_sanmak", Name = "산막", Effect = SkillEffect.GuardParty, Power = 0, QiCost = 16, Cooldown = 2 });
                arts.Add(new SkillSpec { Id = "scene_sambok", Name = "삼복구타산", Power = 1.8f, Hits = 4, QiCost = 24, Cooldown = 2 });
                return arts;
            }
            if (id == "story_ma_haryeong")
            {
                arts.Add(new SkillSpec { Id = "scene_taesanbong", Name = "붕결 일도광 태산봉", Power = 7f, QiCost = 24, Cooldown = 1 });
                arts.Add(new SkillSpec { Id = "scene_pyoru", Name = "표류무상도법", Power = 3f, QiCost = 16, Cooldown = 1 });
                return arts;
            }
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

        /// <summary>
        /// The stage number whose companions travel along: a 일반 story stage uses its own; quests, tower floors and the 어려움 / 극악 stages
        /// use everyone who has joined so far (on a harder difficulty the early solo stages are fought with the party the player already leads).
        /// </summary>
        public static int PartyStage(StageData stage, SaveData save)
        {
            if (stage.IsQuest || stage.IsTower) return System.Math.Max(1, save.HighestUnlockedStage);
            return stage.Difficulty > 0 ? System.Math.Max(stage.Number, save.HighestUnlockedStage) : stage.Number;
        }
    }
}
