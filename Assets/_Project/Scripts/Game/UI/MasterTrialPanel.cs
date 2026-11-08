using System;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>
    /// 사부의 시련 overlay (강호 ▸ 도전 ▸ 사부의 시련): tier choice, the run's path (one 관문 at a time), 가르침 picks, 휴식 and 기연.
    /// Rules live in Core <see cref="MasterTrial"/>; fights go through the normal 출전 준비 and <see cref="BattleScreen"/>.
    /// </summary>
    public sealed class MasterTrialPanel : OverlayPanel
    {
        private readonly Action<StageData> _start;
        private static readonly Color[] RarityColors = { new Color(.80f, .80f, .74f), new Color(.45f, .70f, .95f), new Color(.93f, .76f, .38f) };
        private static readonly string[] RarityHex = { "#CCCCBD", "#73B3F2", "#EDC261" };

        public MasterTrialPanel(RectTransform root, Action onChange, Action<StageData> start) : base(root, onChange) { _start = start; }

        private static Text T(string name, Transform parent, float x0, float y0, float x1, float y1, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var t = UiFactory.Body(UiFactory.Label(name, parent, new Vector2(x0, y0), new Vector2(x1, y1), text, size, color, anchor, FontStyle.Normal), 1.15f);
            t.supportRichText = true;
            return t;
        }

        public void Open(string toast = null)
        {
            var save = GameManager.Instance.Save;
            Begin();
            UiFactory.Label("Title", Root, new Vector2(.05f, .92f), new Vector2(.95f, .99f), "사부의 시련", 44, UiFactory.Gold);
            if (!MasterTrial.Unlocked(save))
            {
                T("Locked", Root, .05f, .45f, .95f, .75f, "일반 스테이지 " + MasterTrial.UnlockStage + "을 돌파하면 노사부의 시련이 열립니다.", 34, UiFactory.PaperText, TextAnchor.MiddleCenter);
                AddClose();
                return;
            }
            string abandoned = MasterTrial.FailAbandoned(save);   // the game was closed during a 관문 fight
            if (abandoned != null) { GameManager.Instance.SaveNow(); toast = abandoned; }
            var run = MasterTrial.Run(save);
            T("Summary", Root, .05f, .865f, .95f, .92f, "최고 " + save.MasterTrialBest + "단계 · 마친 시련 " + save.MasterTrialRuns + "번" + (run.Active ? "  ·  진행 중: " + run.Tier + "단계 " + Math.Min(run.Floor, MasterTrial.Floors) + " / " + MasterTrial.Floors + "관문" : ""), 28, UiFactory.PaperText, TextAnchor.MiddleCenter);
            T("Toast", Root, .04f, .015f, .96f, .075f, toast ?? Hint(run), 26, toast != null ? UiFactory.Gold : UiFactory.Dim, TextAnchor.MiddleCenter);

            var content = CodexPanel.Scroller(Root, .08f, .855f);
            var grid = new ReadingGrid(content, 3, 300f);
            if (!run.Active) DrawStart(save, run, grid);
            else if (run.Pending.StartsWith("pick", StringComparison.Ordinal)) DrawPick(save, run, grid);
            else if (run.Pending == "rest") DrawRest(save, run, grid);
            else if (run.Pending.StartsWith("event:", StringComparison.Ordinal)) DrawEvent(save, run, grid);
            else DrawPath(save, run, grid);
            if (run.Active) DrawRunState(save, run, grid);

            UiFactory.HelpButton(Root, () => { if (HelpPanel.Instance != null) HelpPanel.Instance.Show("master_trial"); });
            AddClose();
            if (!Tutorial.IsDone(save, "master_trial") && HelpPanel.Instance != null) HelpPanel.Instance.Show("master_trial", true);
        }

        private static string Hint(MasterTrialRun run)
        {
            if (!run.Active) return "단계를 골라 시련을 시작합니다. 쓰러지면 그 판이 끝나고, 우두머리를 이기면 다음 단계가 열립니다.";
            if (run.Pending.StartsWith("pick", StringComparison.Ordinal)) return "가르침은 이번 시련에서만 적용됩니다. 같은 것을 다시 고르면 겹쳐서 강해집니다.";
            if (run.Pending == "rest") return "회복하거나, 가진 가르침 하나를 한 단계 강하게 할 수 있습니다.";
            return "체력 · 내공 · 기세는 관문 사이에 이어집니다. 휴식을 언제 고를지 정해 주세요.";
        }

        private static void Save() { GameManager.Instance.SaveNow(); }

        // ------------------------------------------------------------------ start

        private void DrawStart(SaveData save, MasterTrialRun run, ReadingGrid grid)
        {
            if (run.Pending == "done" || run.Pending == "failed")
            {
                grid.Heading("지난 시련");
                var card = grid.Add("Last");
                bool won = run.Pending == "done";
                T("Name", card, .05f, .72f, .95f, .95f, run.Tier + "단계 " + (won ? "돌파" : "실패"), 32, won ? UiFactory.Gold : UiFactory.Red);
                T("Info", card, .05f, .30f, .95f, .72f, "돌파한 관문 " + run.Wins + "개 · 얻은 가르침 " + run.Blessings.Count + "개" + (won ? "\n단계 보상 은자 " + run.EarnedDoo.ToString("N0") : ""), 24, UiFactory.PaperText);
                UiFactory.Button("Close", card, new Vector2(.08f, .05f), new Vector2(.92f, .27f), "확인", UiFactory.Red, () => { MasterTrial.Close(save); Save(); Open(); }, 28);
            }
            grid.Heading("단계 고르기");
            int open = MasterTrial.OpenTier(save);
            for (int tier = 1; tier <= MasterTrial.MaxTier; tier++)
            {
                int t = tier;
                var card = grid.Add("Tier" + tier);
                bool locked = tier > open, cleared = tier <= save.MasterTrialBest;
                T("Name", card, .05f, .78f, .95f, .97f, tier + "단계" + (cleared ? "  <size=22>· 돌파</size>" : ""), 32, locked ? UiFactory.Dim : UiFactory.Gold);
                T("Info", card, .05f, .30f, .95f, .78f,
                    "적 체력 +" + Math.Round((MasterTrial.TierHp(tier) - 1f) * 100f) + "% · 공격 +" + Math.Round((MasterTrial.TierAtk(tier) - 1f) * 100f) + "%\n휴식 회복 " + Math.Round(MasterTrial.RestHeal(tier) * 100f) + "%\n돌파 보상 은자 " + (cleared ? Income.Boost(MasterTrial.ClearDoo(tier) / 2).ToString("N0") + " (다시 돌파 · 관문 보상도 절반)" : Income.Boost(MasterTrial.ClearDoo(tier) * 2).ToString("N0") + " (첫 돌파)") + (MasterTrial.ClearInsight(tier) > 0 ? " · 심득석" : ""),
                    22, locked ? UiFactory.Dim : UiFactory.PaperText);
                var go = UiFactory.Button("Go", card, new Vector2(.08f, .05f), new Vector2(.92f, .26f), locked ? (tier - 1) + "단계 돌파 필요" : "도전", locked ? UiFactory.InkSoft : UiFactory.Red, () =>
                {
                    if (MasterTrial.Run(save).Pending == "done" || MasterTrial.Run(save).Pending == "failed") MasterTrial.Close(save);
                    if (MasterTrial.Start(save, t, Environment.TickCount)) { Save(); Open("사부: " + t + "단계라. 어디 얼마나 버티나 보자."); }
                }, 28);
                go.interactable = !locked;
            }
        }

        // ------------------------------------------------------------------ the path

        private static string EnemyText(TrialNode node)
        {
            var st = EarlyGameContent.FindStage(node.Stage);
            if (st == null) return "";
            var names = new List<string>();
            foreach (var id in st.Encounters[node.Fight])
            {
                var e = EarlyGameContent.FindEnemy(id);
                if (e != null && !names.Contains(e.Name)) names.Add(e.Name);
            }
            return "적: " + string.Join(", ", names) + " (" + st.Encounters[node.Fight].Length + "명)\n" + st.Name;
        }

        private void DrawPath(SaveData save, MasterTrialRun run, ReadingGrid grid)
        {
            grid.Heading(run.Tier + "단계 · " + run.Floor + "관문 — 다음 길을 고르세요");
            foreach (var node in MasterTrial.Options(save))
            {
                var n = node;
                var card = grid.Add("Node_" + node.Code);
                bool fight = node.Kind == TrialNodeKind.Fight || node.Kind == TrialNodeKind.Elite || node.Kind == TrialNodeKind.Boss;
                var color = node.Kind == TrialNodeKind.Boss || node.Kind == TrialNodeKind.Elite ? UiFactory.Red : node.Kind == TrialNodeKind.Rest ? UiFactory.Jade : UiFactory.Gold;
                T("Name", card, .05f, .78f, .95f, .97f, node.KindName, 34, color);
                string info = node.Kind == TrialNodeKind.Rest ? "체력 " + Math.Round(MasterTrial.RestHeal(run.Tier) * 100f) + "% · 내공 회복, 또는 가르침 하나 강화"
                    : node.Kind == TrialNodeKind.Event ? MasterTrial.EventTitle(node.EventId) + "\n무슨 일이 생길지 모릅니다."
                    : EnemyText(node) + (node.Kind == TrialNodeKind.Elite ? "\n더 센 적 · 희귀 이상 가르침이 나오기 쉽습니다." : node.Kind == TrialNodeKind.Boss ? "\n이기면 시련 돌파" : "\n이기면 가르침 고르기");
                T("Info", card, .05f, .28f, .95f, .78f, info, 22, UiFactory.PaperText);
                UiFactory.Button("Go", card, new Vector2(.08f, .05f), new Vector2(.92f, .25f), fight ? "싸운다" : "간다", UiFactory.Red, () =>
                {
                    var stage = MasterTrial.Choose(save, n);
                    Save();
                    if (stage != null) { _start(stage); return; }
                    Open();
                }, 28);
            }
            var quit = grid.Add("GiveUp");
            T("Name", quit, .05f, .55f, .95f, .95f, "시련 포기", 30, UiFactory.Dim);
            T("Info", quit, .05f, .30f, .95f, .55f, "지금까지 받은 관문 보상은 남습니다.", 22, UiFactory.Dim);
            UiFactory.Button("Quit", quit, new Vector2(.08f, .05f), new Vector2(.92f, .25f), "포기", UiFactory.InkSoft, () =>
                UiFactory.Confirm(Root, "시련을 포기할까요?", "이번 시련이 끝납니다. 앞 관문에서 받은 보상은 남습니다.", "포기", () => { MasterTrial.Fail(save); Save(); Open(); }), 28);
        }

        // ------------------------------------------------------------------ 가르침

        private void BlessingCard(ReadingGrid grid, MasterTrialRun run, string key, string button, UnityEngine.Events.UnityAction action, string extra = null)
        {
            var card = grid.Add("Bless_" + key);
            int rarity = MasterTrial.RarityOf(key), have = 0;
            foreach (var e in run.Blessings) { string id, arg; int s; MasterTrial.Parse(e, out id, out arg, out s); if (MasterTrial.KeyOf(e) == key) have = s; }
            UiFactory.Frame(card, RarityColors[rarity], 3f);
            bool cost = MasterTrial.IsCost(key);
            T("Name", card, .05f, .78f, .95f, .97f, MasterTrial.NameOf(key) + "  <size=22><color=" + RarityHex[rarity] + ">" + MasterTrial.RarityNames[rarity] + "</color>" + (cost ? " · <color=#E07A6A>대가</color>" : "") + "</size>", 30, UiFactory.Gold);
            T("Info", card, .05f, .30f, .95f, .78f, (have > 0 ? "보유 " + have + "단계 → " + (have + 1) + "단계\n" : "") + MasterTrial.Describe(key, have + 1) + (extra ?? ""), 23, cost ? new Color(.93f, .62f, .55f) : UiFactory.PaperText);
            UiFactory.Button("Take", card, new Vector2(.08f, .05f), new Vector2(.92f, .25f), button, UiFactory.Red, action, 28);
        }

        private void DrawPick(SaveData save, MasterTrialRun run, ReadingGrid grid)
        {
            grid.Heading("가르침 고르기");
            for (int i = 0; i < run.Offer.Count; i++)
            {
                int index = i;
                BlessingCard(grid, run, run.Offer[i], "받기", () => { MasterTrial.Pick(save, index); Save(); Open(MasterTrial.NameOf(run.Blessings[run.Blessings.Count - 1]) + "을(를) 익혔습니다."); });
            }
            var skip = grid.Add("Skip");
            T("Name", skip, .05f, .55f, .95f, .95f, "받지 않기", 30, UiFactory.Dim);
            UiFactory.Button("Skip", skip, new Vector2(.08f, .05f), new Vector2(.92f, .25f), "다음 관문으로", UiFactory.InkSoft, () => { MasterTrial.Pick(save, -1); Save(); Open(); }, 28);
        }

        private void DrawRest(SaveData save, MasterTrialRun run, ReadingGrid grid)
        {
            grid.Heading("휴식");
            var heal = grid.Add("Heal");
            float share = MasterTrial.RestHeal(run.Tier) * (1f + .5f * MasterTrial.Stacks(run, "rest_plus"));
            T("Name", heal, .05f, .72f, .95f, .95f, "몸을 추스른다", 32, UiFactory.Jade);
            T("Info", heal, .05f, .30f, .95f, .72f, "아군 모두 체력 " + Math.Round(share * 100f) + "% · 내공 " + Math.Round(share * 150f) + "% 회복", 24, UiFactory.PaperText);
            UiFactory.Button("Go", heal, new Vector2(.08f, .05f), new Vector2(.92f, .25f), "쉰다", UiFactory.Red, () => { MasterTrial.RestHealAll(save); Save(); Open("기운을 되찾았습니다."); }, 28);
            if (run.Blessings.Count == 0) return;
            grid.Heading("또는 수련한다 (가르침 하나 +1단계)");
            foreach (var e in run.Blessings)
            {
                string key = MasterTrial.KeyOf(e);
                BlessingCard(grid, run, key, "수련", () => { MasterTrial.RestTrain(save, key); Save(); Open(MasterTrial.NameOf(key) + "이(가) 강해졌습니다."); });
            }
        }

        private void DrawEvent(SaveData save, MasterTrialRun run, ReadingGrid grid)
        {
            string id = run.Pending.Substring(6);
            grid.Heading("기연 · " + MasterTrial.EventTitle(id));
            var line = StoryLine.Parse(MasterTrial.EventText(id));
            var scene = grid.Add("Scene");
            T("Text", scene, .05f, .10f, .95f, .95f, (string.IsNullOrEmpty(line.Speaker) ? "" : "<color=#E8C46A>" + line.Speaker + "</color>\n") + line.Text, 26, UiFactory.PaperText);
            var options = MasterTrial.EventOptions(id);
            for (int i = 0; i < options.Length; i++)
            {
                int option = i;
                var card = grid.Add("Option" + i);
                T("Info", card, .05f, .30f, .95f, .95f, options[i], 25, UiFactory.PaperText);
                UiFactory.Button("Go", card, new Vector2(.08f, .05f), new Vector2(.92f, .25f), "고른다", UiFactory.Red, () =>
                {
                    string note = MasterTrial.ChooseEvent(save, option, new SeededRandom(Environment.TickCount));
                    if (note == null) { Open("은자가 부족합니다."); return; }
                    Save();
                    Open(note);
                }, 28);
            }
        }

        // ------------------------------------------------------------------ the run so far

        private static string MemberName(string id)
        {
            if (id == Inventory.Hero) return "비류연";
            var c = EarlyGameContent.FindCompanion(id);
            return c != null ? c.Name : id;
        }

        private void DrawRunState(SaveData save, MasterTrialRun run, ReadingGrid grid)
        {
            grid.Heading("일행 상태 · 얻은 가르침 " + run.Blessings.Count + "개");
            var party = grid.Add("Party");
            var lines = new List<string>();
            foreach (var m in MasterTrial.PartyState(run))
                lines.Add(MemberName(m.Id) + "  체력 " + Math.Round(m.HpPermil / 10.0) + "% · 내공 " + Math.Round(m.QiPermil / 10.0) + "% · 기세 " + m.Spirit);
            T("Info", party, .05f, .05f, .95f, .95f, lines.Count > 0 ? string.Join("\n", lines) : "모두 온전합니다.", 23, UiFactory.PaperText);
            foreach (var e in run.Blessings)
            {
                string id, arg; int s;
                MasterTrial.Parse(e, out id, out arg, out s);
                int rarity = MasterTrial.RarityOf(e);
                var card = grid.Add("Own_" + e);
                UiFactory.Frame(card, RarityColors[rarity], 2f);
                T("Name", card, .05f, .66f, .95f, .95f, MasterTrial.NameOf(e) + "  <size=22>" + s + "단계</size>", 28, UiFactory.Gold);
                T("Info", card, .05f, .08f, .95f, .66f, MasterTrial.Describe(e), 23, MasterTrial.IsCost(e) ? new Color(.93f, .62f, .55f) : UiFactory.PaperText);
            }
        }
    }
}
