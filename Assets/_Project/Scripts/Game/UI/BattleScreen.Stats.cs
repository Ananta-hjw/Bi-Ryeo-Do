using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>
    /// 전투 통계: every unit's damage dealt and taken in the fight that just ended, opened from the 승전 기록 card or the defeat panel.
    /// Left, the party and the foes with two bars each; right, the chosen unit's breakdown by art (basic attack, each art, 합공,
    /// statuses it inflicted), what hit it, its healing and its defence. Numbers come from Core <see cref="BattleStats"/>.
    /// </summary>
    public partial class BattleScreen
    {
        private static readonly Color DealtColor = new Color(.96f, .62f, .24f, 1f);
        private static readonly Color TakenColor = new Color(.86f, .30f, .25f, 1f);
        private static readonly Color HealColor = new Color(.40f, .78f, .52f, 1f);

        private const float StatRowH = 132f;
        private const float StatLineH = 104f;
        private const float StatGap = 10f;

        private BattleStats _fightStats;
        private readonly Dictionary<string, Combatant> _fightUnits = new Dictionary<string, Combatant>();
        private PlayerCommand? _statsCommand;
        private RectTransform _statsWindow;
        private GameObject _panelStats;

        /// <summary>단 대항전 bout names: 선봉전 · 중견전 · 대장전.</summary>
        private static string LeagueBoutName(int fight) { return fight == 0 ? "선봉전" : fight == 1 ? "중견전" : "대장전"; }

        /// <summary>Starts the statistics of a new fight (called once its units exist).</summary>
        private void BeginFightStats()
        {
            var enemies = _cards.ConvertAll(c => c.Unit);
            _fightStats = new BattleStats(_party, enemies);
            _fightUnits.Clear();
            foreach (var u in _party) _fightUnits[u.Id] = u;
            foreach (var u in enemies) _fightUnits[u.Id] = u;
            _statsCommand = null;
        }

        private void RecordFightStats(List<BattleEvent> events)
        {
            if (_fightStats != null) _fightStats.Add(events, _statsCommand);
            _statsCommand = null;
        }

        /// <summary>The small 「전투 통계」 button for a result card (nothing when no fight was recorded).</summary>
        private void AddStatsButton(Transform card, Vector2 min, Vector2 max)
        {
            if (_fightStats == null) return;
            UiFactory.Button("Stats", card, min, max, "전투 통계", UiFactory.InkSoft, () => OpenFightStats(card), 28);
        }

        private void CloseFightStats()
        {
            if (_statsWindow != null) Destroy(_statsWindow.gameObject);
            _statsWindow = null;
        }

        private void OpenFightStats(Transform host)
        {
            if (_fightStats == null) return;
            CloseFightStats();
            // over everything the result card sits on, so the card's buttons cannot be pressed through it
            var parent = host.parent != null ? host.parent : host;
            _statsWindow = UiFactory.Rect("FightStats", parent, Vector2.zero, Vector2.one);
            UiFactory.Panel("Dim", _statsWindow, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, .82f)).raycastTarget = true;
            var card = UiFactory.Panel("StatsCard", _statsWindow, new Vector2(.03f, .04f), new Vector2(.97f, .96f), UiFactory.ModalInk);
            card.raycastTarget = true;
            UiFactory.Frame(card.transform, UiFactory.GoldDim, 3f);
            UiFactory.BackButton(card.transform, new Vector2(.012f, .905f), new Vector2(.08f, .985f), CloseFightStats);
            UiFactory.Label("Title", card.transform, new Vector2(.10f, .905f), new Vector2(.60f, .985f), "전투 통계", 44, UiFactory.Gold, TextAnchor.MiddleLeft);
            RewardText("Totals", card.transform, new Vector2(.45f, .905f), new Vector2(.985f, .985f),
                "총 공격량  아군 " + _fightStats.TotalDealt(Team.Player).ToString("N0") + "  ·  적 " + _fightStats.TotalDealt(Team.Enemy).ToString("N0"),
                26, UiFactory.Dim, TextAnchor.MiddleRight);
            UiFactory.Panel("Rule", card.transform, new Vector2(.015f, .895f), new Vector2(.985f, .898f), UiFactory.GoldDim);

            var listArea = UiFactory.Rect("UnitList", card.transform, new Vector2(.012f, .02f), new Vector2(.405f, .885f));
            UiFactory.Panel("ColumnRule", card.transform, new Vector2(.412f, .03f), new Vector2(.413f, .88f), UiFactory.GoldDim);
            var detailArea = UiFactory.Rect("UnitDetail", card.transform, new Vector2(.42f, .02f), new Vector2(.988f, .885f));
            var listContent = CodexPanel.Scroller(listArea, 0f, 1f);
            var detailContent = CodexPanel.Scroller(detailArea, 0f, 1f);

            var party = _fightStats.Party;
            var enemies = _fightStats.Enemies;
            long scale = 1;
            foreach (var s in _fightStats.All) scale = System.Math.Max(scale, System.Math.Max(s.Dealt, s.Taken));
            var mvp = _fightStats.Mvp;

            var rows = new Dictionary<string, Image>();
            System.Action<UnitStats> select = null;
            select = unit =>
            {
                foreach (var kv in rows) kv.Value.color = kv.Key == unit.Id ? UiFactory.GoldDim : Color.clear;
                DrawUnitDetail(detailContent, unit);
            };

            float y = 0f;
            y = StatHeader(listContent, y, "아군");
            foreach (var s in party) y = UnitRow(listContent, y, s, scale, s == mvp, rows, select);
            y = StatHeader(listContent, y + StatGap, "적");
            foreach (var s in enemies) y = UnitRow(listContent, y, s, scale, false, rows, select);
            listContent.sizeDelta = new Vector2(0f, y + StatGap);

            var first = mvp ?? (party.Count > 0 ? party[0] : enemies.Count > 0 ? enemies[0] : null);
            if (first != null) select(first);
            _statsWindow.SetAsLastSibling();
        }

        private static float StatHeader(RectTransform content, float y, string text)
        {
            var label = RewardText("Section", content, new Vector2(0f, 1f), new Vector2(1f, 1f), text, 30, UiFactory.Gold);
            var rt = label.rectTransform;
            rt.pivot = new Vector2(.5f, 1f);
            rt.offsetMin = new Vector2(14f, 0f);
            rt.offsetMax = new Vector2(-14f, 0f);
            rt.anchoredPosition = new Vector2(0f, -y);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, 50f);
            return y + 54f;
        }

        private static RectTransform StatBand(RectTransform content, string name, float y, float height)
        {
            var row = UiFactory.Rect(name, content, new Vector2(0f, 1f), new Vector2(1f, 1f));
            row.pivot = new Vector2(.5f, 1f);
            row.offsetMin = new Vector2(8f, 0f);
            row.offsetMax = new Vector2(-14f, 0f);
            row.sizeDelta = new Vector2(row.sizeDelta.x, height);
            row.anchoredPosition = new Vector2(0f, -y);
            return row;
        }

        private float UnitRow(RectTransform content, float y, UnitStats s, long scale, bool mvp, Dictionary<string, Image> rows, System.Action<UnitStats> select)
        {
            var row = StatBand(content, "Unit_" + s.Id, y, StatRowH);
            var bg = UiFactory.Button("Pick", row, Vector2.zero, Vector2.one, "", HudInk, () => select(s), 28);
            var frame = UiFactory.Panel("Selected", row, Vector2.zero, Vector2.one, Color.clear);
            frame.raycastTarget = false;
            frame.rectTransform.offsetMin = new Vector2(-3f, -3f);
            frame.rectTransform.offsetMax = new Vector2(3f, 3f);
            frame.transform.SetSiblingIndex(0);   // a gold rim behind the row
            rows[s.Id] = frame;

            var portrait = UiFactory.Rect("Portrait", bg.transform, new Vector2(0f, 0f), new Vector2(0f, 1f));
            portrait.pivot = new Vector2(0f, .5f);
            portrait.sizeDelta = new Vector2(112f, -12f);
            portrait.anchoredPosition = new Vector2(8f, 0f);
            Combatant unit;
            var sprite = _fightUnits.TryGetValue(s.Id, out unit) ? UnitPortrait(unit) : null;
            if (sprite != null)
            {
                var img = portrait.gameObject.AddComponent<Image>();
                img.sprite = sprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }
            else UiFactory.Label("Initial", portrait, Vector2.zero, Vector2.one, string.IsNullOrEmpty(s.Name) ? "?" : s.Name.Substring(0, 1), 44, UiFactory.PaperText);

            var body = UiFactory.Rect("Body", bg.transform, Vector2.zero, Vector2.one);
            body.offsetMin = new Vector2(132f, 8f);
            body.offsetMax = new Vector2(-12f, -6f);
            string badge = mvp ? "  <color=#E8C46A>MVP</color>" : s.IsBoss ? "  <color=#E07A5F>보스</color>" : "";
            var name = RewardText("Name", body, new Vector2(0f, .64f), new Vector2(1f, 1f), s.Name + badge, 28, UiFactory.PaperText);
            name.supportRichText = true;
            StatBar(body, "Dealt", .34f, .62f, "공격", s.Dealt, scale, DealtColor);
            StatBar(body, "Taken", .02f, .30f, "피해", s.Taken, scale, TakenColor);
            return y + StatRowH + StatGap;
        }

        /// <summary>One labelled bar: 「공격 ▇▇▇▇ 12,345」.</summary>
        private static void StatBar(Transform parent, string name, float y0, float y1, string label, long value, long scale, Color color)
        {
            var band = UiFactory.Rect(name, parent, new Vector2(0f, y0), new Vector2(1f, y1));
            RewardText("Label", band, new Vector2(0f, 0f), new Vector2(.14f, 1f), label, 22, UiFactory.Dim);
            var track = UiFactory.Panel("Track", band, new Vector2(.15f, .2f), new Vector2(.70f, .8f), new Color(1f, 1f, 1f, .07f));
            track.raycastTarget = false;
            float pct = scale > 0 ? Mathf.Clamp01((float)value / scale) : 0f;
            var fill = UiFactory.Panel("Fill", track.transform, Vector2.zero, new Vector2(Mathf.Max(pct, value > 0 ? .01f : 0f), 1f), color);
            fill.raycastTarget = false;
            RewardText("Value", band, new Vector2(.71f, 0f), new Vector2(1f, 1f), value.ToString("N0"), 24, color, TextAnchor.MiddleRight);
        }

        private void DrawUnitDetail(RectTransform content, UnitStats s)
        {
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
            float y = 0f;

            long sideDealt = _fightStats.TotalDealt(s.Team);
            string share = sideDealt > 0 ? "  (" + (s.Team == Team.Player ? "아군" : "적") + " 공격의 " + Mathf.RoundToInt(100f * s.Dealt / sideDealt) + "%)" : "";
            var head = StatBand(content, "Head", y, 150f);
            RewardText("Name", head, new Vector2(.01f, .55f), new Vector2(.99f, 1f), s.Name + (s.Team == Team.Player ? "" : "  · 적"), 36, UiFactory.Gold);
            var summary = RewardText("Summary", head, new Vector2(.01f, 0f), new Vector2(.99f, .56f),
                Colored("공격 " + s.Dealt.ToString("N0"), DealtColor) + share + "   " + Colored("피해 " + s.Taken.ToString("N0"), TakenColor)
                + "   " + Colored("치유 " + s.HealDone.ToString("N0"), HealColor) + "   처치 " + s.Kills, 26, UiFactory.PaperText);
            summary.supportRichText = true;
            y += 160f;

            y = StatHeader(content, y, "공격 내역");
            y = StatLines(content, y, s.DealtBy, s.Dealt, DealtColor, "준 피해가 없습니다.");

            y = StatHeader(content, y + StatGap, "받은 피해");
            y = StatLines(content, y, s.TakenFrom, s.Taken, TakenColor, "받은 피해가 없습니다.");

            if (s.HealsBy.Count > 0 || s.HealTaken > 0)
            {
                y = StatHeader(content, y + StatGap, "치유");
                y = StatLines(content, y, s.HealsBy, s.HealDone, HealColor, "직접 치유하지 않았습니다.");
                if (s.HealTaken > 0) y = StatNote(content, y, "받은 치유 " + s.HealTaken.ToString("N0"));
            }

            y = StatHeader(content, y + StatGap, "방어·기타");
            y = StatNote(content, y, "방어로 받은 타격 " + s.Guarded + "번 · 회피 " + s.Dodged + "번 · 간파 " + s.Reads + "번 (맞은 타격 " + s.HitsTaken + "번)");
            y = StatNote(content, y, "상태 이상: 건 횟수 " + s.Inflicted + "번 (지속 피해 " + s.StatusDamage.ToString("N0") + ") · 걸린 횟수 " + s.Afflicted + "번");
            y = StatNote(content, y, "치명타 " + s.Crits + "번 (치명타율 " + Mathf.RoundToInt(s.CritRate * 100f) + "%) · 처치 " + s.Kills);
            if (s.FriendlyFire > 0) y = StatNote(content, y, "주화입마로 같은 편에 준 피해 " + s.FriendlyFire.ToString("N0") + " (공격량에 넣지 않습니다)");
            content.sizeDelta = new Vector2(0f, y + StatGap);
            content.anchoredPosition = Vector2.zero;
        }

        private static string Colored(string text, Color color)
        {
            return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";
        }

        private static float StatNote(RectTransform content, float y, string text)
        {
            var row = StatBand(content, "Note", y, 56f);
            RewardText("Text", row, new Vector2(.01f, 0f), new Vector2(.99f, 1f), text, 24, UiFactory.PaperText);
            return y + 60f;
        }

        /// <summary>One row per source: icon, name, amount and share bar, then 「사용 3번 · 9타 · 치명 2 · 최대 1,234」.</summary>
        private static float StatLines(RectTransform content, float y, List<BattleStatLine> lines, long total, Color color, string empty)
        {
            if (lines.Count == 0) return StatNote(content, y, empty);
            var sorted = new List<BattleStatLine>(lines);
            sorted.Sort((a, b) => b.Amount.CompareTo(a.Amount));
            foreach (var l in sorted)
            {
                var row = StatBand(content, "Line", y, StatLineH);
                UiFactory.Panel("Back", row, Vector2.zero, Vector2.one, HudInk).raycastTarget = false;
                var icon = UiFactory.Rect("Icon", row, new Vector2(0f, .5f), new Vector2(0f, .5f));
                icon.sizeDelta = new Vector2(80f, 80f);
                icon.anchoredPosition = new Vector2(52f, 0f);
                var sprite = l.IsStatus ? null : ArtLibrary.Art(l.ArtId) ?? ArtLibrary.Command(l.ArtId);
                if (sprite != null)
                {
                    var img = icon.gameObject.AddComponent<Image>();
                    img.sprite = sprite;
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                }
                else
                {
                    UiFactory.Panel("Swatch", icon, new Vector2(.1f, .1f), new Vector2(.9f, .9f), new Color(color.r * .45f, color.g * .45f, color.b * .45f, 1f)).raycastTarget = false;
                    UiFactory.Label("Glyph", icon, Vector2.zero, Vector2.one, string.IsNullOrEmpty(l.Name) ? "?" : l.Name.Substring(0, 1), 34, UiFactory.PaperText);
                }

                var body = UiFactory.Rect("Body", row, Vector2.zero, Vector2.one);
                body.offsetMin = new Vector2(104f, 6f);
                body.offsetMax = new Vector2(-12f, -6f);
                RewardText("Name", body, new Vector2(0f, .52f), new Vector2(.62f, 1f), l.Name, 26, UiFactory.PaperText);
                int pct = total > 0 ? Mathf.RoundToInt(100f * l.Amount / total) : 0;
                RewardText("Amount", body, new Vector2(.62f, .52f), new Vector2(1f, 1f), l.Amount.ToString("N0") + "  (" + pct + "%)", 26, color, TextAnchor.MiddleRight);
                var track = UiFactory.Panel("Track", body, new Vector2(0f, .40f), new Vector2(1f, .50f), new Color(1f, 1f, 1f, .07f));
                track.raycastTarget = false;
                UiFactory.Panel("Fill", track.transform, Vector2.zero, new Vector2(total > 0 ? Mathf.Clamp01((float)l.Amount / total) : 0f, 1f), color).raycastTarget = false;
                string detail = l.IsStatus
                    ? l.Hits + "번 · 최대 " + l.MaxHit.ToString("N0")
                    : "사용 " + l.Uses + "번 · " + l.Hits + "타" + (l.Crits > 0 ? " · 치명 " + l.Crits : "") + " · 최대 " + l.MaxHit.ToString("N0");
                RewardText("Detail", body, new Vector2(0f, 0f), new Vector2(1f, .38f), detail, 22, UiFactory.Dim);
                y += StatLineH + 6f;
            }
            return y;
        }
    }
}
