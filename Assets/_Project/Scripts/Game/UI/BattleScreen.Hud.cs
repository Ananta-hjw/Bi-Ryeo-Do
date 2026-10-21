using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    public partial class BattleScreen
    {
        private static readonly Color HudInk = new Color(.035f, .075f, .08f, .96f);
        private static readonly Color ThreatRed = new Color(1f, .55f, .40f, 1f);
        private readonly List<Image> _turnPortraits = new List<Image>();
        private readonly List<Text> _turnGlyphs = new List<Text>();
        private readonly List<Image> _turnEdges = new List<Image>();
        private readonly List<Image> _phaseMarks = new List<Image>();
        private Text _focusName, _focusDetail, _focusHealth, _focusTag;
        private Text _actorName, _actorResources, _commandHint, _turnNotice;
        private string _actorAction;
        private Image _actorPortrait, _focusAccent;
        private Text _actorGlyph;
        private GameObject _commandHeader;
        private Combatant _hudActor;
        private UiFactory.Bar _focusHp;

        private void BuildBattleHud(RectTransform root)
        {
            var top = UiFactory.Panel("BattleHeader", root, new Vector2(0f, .923f), Vector2.one, HudInk);
            UiFactory.Panel("HeaderRule", top.transform, Vector2.zero, new Vector2(1f, .018f), UiFactory.GoldDim);
            _stageLabel = UiFactory.Label("Stage", top.transform, new Vector2(.035f, .06f), new Vector2(.70f, .96f), "", 28, UiFactory.PaperText, TextAnchor.MiddleLeft);
            var speed = UiFactory.Button("Speed", top.transform, new Vector2(.715f, .05f), new Vector2(.845f, .95f), "속도 x1", UiFactory.InkSoft, OnSpeedPressed, 23);
            _speedLabel = speed.GetComponentInChildren<Text>();
            UiFactory.Button("Flee", top.transform, new Vector2(.855f, .05f), new Vector2(.985f, .95f), "포기", HudInk, OnFleePressed, 23);

            var timeline = UiFactory.Panel("TurnRibbon", root, new Vector2(FieldLeft, .84f), new Vector2(FieldRight, .918f), HudInk);
            UiFactory.Label("TurnTitle", timeline.transform, new Vector2(.012f, .5f), new Vector2(.105f, .96f), "행동", 22, UiFactory.Gold);
            UiFactory.Label("TurnSubtitle", timeline.transform, new Vector2(.012f, .08f), new Vector2(.105f, .53f), "순서", 22, UiFactory.PaperText);
            for (int i = 0; i < 7; i++)
            {
                float x = .12f + i * .125f;
                var chip = UiFactory.Panel("Turn" + i, timeline.transform, new Vector2(x, .04f), new Vector2(x + .114f, .96f), HudInk);
                var edge = UiFactory.Panel("TeamEdge", chip.transform, Vector2.zero, new Vector2(.025f, 1f), UiFactory.Jade);
                UiFactory.Label("Position", chip.transform, new Vector2(.1f, .77f), new Vector2(.9f, .98f), i == 0 ? "현재" : (i + 1).ToString("00"), 17, i == 0 ? UiFactory.Gold : UiFactory.Dim);
                var pic = UiFactory.Panel("Portrait", chip.transform, new Vector2(.27f, .27f), new Vector2(.73f, .77f), Color.white);
                pic.preserveAspect = true;
                var glyph = UiFactory.Label("Fallback", chip.transform, new Vector2(.12f, .27f), new Vector2(.88f, .77f), "", 29, UiFactory.Gold);
                var name = UiFactory.Label("Name", chip.transform, new Vector2(.05f, .02f), new Vector2(.95f, .29f), "", 20, UiFactory.PaperText);
                chip.gameObject.SetActive(false);   // empty until the first RefreshTurnOrder, so no white placeholder squares show before a fight (and during the story)
                _turnChips.Add(chip); _turnLabels.Add(name); _turnPortraits.Add(pic); _turnGlyphs.Add(glyph); _turnEdges.Add(edge);
            }

            var focus = UiFactory.Panel("EnemyFocus", root, new Vector2(FieldLeft, .738f), new Vector2(FieldRight, .833f), HudInk);
            UiFactory.Frame(focus.transform, UiFactory.GoldDim, 1.5f);
            _focusAccent = UiFactory.Panel("Seal", focus.transform, new Vector2(.012f, .22f), new Vector2(.106f, .88f), UiFactory.Red);
            _focusTag = UiFactory.Label("SealText", _focusAccent.transform, Vector2.zero, Vector2.one, "敵", 26, UiFactory.PaperText);
            _focusName = UiFactory.Label("Name", focus.transform, new Vector2(.13f, .70f), new Vector2(.69f, .99f), "", 26, UiFactory.Gold, TextAnchor.MiddleLeft);
            _focusHealth = UiFactory.Label("Health", focus.transform, new Vector2(.70f, .70f), new Vector2(.97f, .99f), "", 20, UiFactory.PaperText, TextAnchor.MiddleRight);
            _focusDetail = UiFactory.Label("Intent", focus.transform, new Vector2(.13f, .15f), new Vector2(.97f, .69f), "", 21, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
            UiFactory.Body(_focusDetail, 1.1f);
            _focusHp = UiFactory.MakeBar("BossHealth", focus.transform, new Vector2(.025f, .045f), new Vector2(.975f, .11f), Color.black, UiFactory.Red);
            for (int i = 0; i < 2; i++)
                _phaseMarks.Add(UiFactory.Panel("PhaseMark" + i, _focusHp.Root, new Vector2(.5f, 0f), new Vector2(.502f, 1f), UiFactory.Gold));

            var log = UiFactory.Panel("BattleLog", root, new Vector2(FieldLeft, .012f), new Vector2(FieldRight, .13f), HudInk);
            UiFactory.Panel("LogAccent", log.transform, Vector2.zero, new Vector2(.004f, 1f), UiFactory.Gold);
            UiFactory.Label("LogTitle", log.transform, new Vector2(.025f, .03f), new Vector2(.12f, .47f), "전황", 21, UiFactory.Gold);
            _log = UiFactory.Label("Log", log.transform, new Vector2(.15f, .03f), new Vector2(.975f, .47f), "무공을 고른 뒤 대상을 선택해 주세요.", 25, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);

            UiFactory.Label("CurrentTurnTitle", log.transform, new Vector2(.025f, .52f), new Vector2(.12f, .98f), "차례", 22, UiFactory.Gold);
            _turnNotice = UiFactory.Label("CurrentTurn", log.transform, new Vector2(.15f, .51f), new Vector2(.975f, .99f), "", 30, UiFactory.Gold, TextAnchor.MiddleLeft);

            var deck = UiFactory.Panel("CommandDeck", root, new Vector2(.725f, 0f), new Vector2(1f, .923f), HudInk);
            UiFactory.Panel("DeckRule", deck.transform, new Vector2(0f, .994f), Vector2.one, UiFactory.GoldDim);
            UiFactory.Glyph("halo", deck.transform, new Vector2(.66f, -.22f), new Vector2(1.18f, .68f), new Color(.83f, .72f, .48f, .07f));
            var header = UiFactory.Rect("ActingMember", root, new Vector2(.738f, .755f), new Vector2(.985f, .91f));
            _commandHeader = header.gameObject;
            var portraitFrame = UiFactory.Panel("PortraitBack", header, new Vector2(0f, .48f), new Vector2(.18f, .98f), UiFactory.InkSoft);
            UiFactory.Frame(portraitFrame.transform, UiFactory.GoldDim, 1.5f);
            _actorPortrait = UiFactory.Panel("Portrait", portraitFrame.transform, new Vector2(.07f, .03f), new Vector2(.93f, .97f), Color.white);
            _actorPortrait.preserveAspect = true;
            _actorGlyph = UiFactory.Label("Fallback", portraitFrame.transform, Vector2.zero, Vector2.one, "", 48, UiFactory.Gold);
            _actorName = UiFactory.Heading(UiFactory.Label("Actor", header, new Vector2(.21f, .49f), new Vector2(.99f, 1f), "", 34, UiFactory.PaperText, TextAnchor.MiddleLeft));
            _actorResources = UiFactory.Label("Resources", header, new Vector2(.01f, .25f), new Vector2(.99f, .49f), "", 24, UiFactory.Jade, TextAnchor.MiddleLeft);
            _commandHint = UiFactory.Label("Hint", header, new Vector2(.01f, .01f), new Vector2(.99f, .25f), "", 22, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);

            for (int i = 0; i < _commands.Length; i++)
            {
                int index = i;
                var btn = UiFactory.Button("Command" + i, root, Vector2.zero, Vector2.one, "", UiFactory.InkSoft, () => OnCommandPressed(index), 32);
                var label = btn.GetComponentInChildren<Text>();
                label.resizeTextMinSize = 28;
                var detail = UiFactory.Label("Detail", btn.transform, Vector2.zero, Vector2.one, "", 23, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal);
                detail.resizeTextMinSize = 22;
                var icon = UiFactory.Panel("Icon", btn.transform, Vector2.zero, Vector2.one, Color.white);
                icon.preserveAspect = true;
                icon.gameObject.SetActive(false);
                var glyph = UiFactory.Glyph(CommandGlyph(i), btn.transform, Vector2.zero, Vector2.one, UiFactory.Gold);
                _commands[i] = new CommandButton { Button = btn, Image = btn.GetComponent<Image>(), Label = label, Detail = detail, Root = btn.gameObject, Icon = icon, Fallback = glyph };
                btn.gameObject.AddComponent<LongPress>().OnLong = () => ShowCommandTip(index);
            }
            LayoutCommands(false);
        }

        private static string CommandGlyph(int slot)
        {
            return slot == 5 ? "guard" : slot == 6 ? "fortune" : slot == 7 ? "bag" : slot == 8 ? "party" : slot == 9 ? "lightning" : "arts";
        }

        private static void PlaceHud(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }

        private void LayoutCommands(bool picker)
        {
            // Two columns keep all ten commands reachable without covering the field.
            int[] order = { 0, 9, 1, 2, 3, 4, 5, 6, 7, 8 };
            for (int i = 0; i < _commands.Length; i++)
            {
                var command = _commands[i];
                int position = picker ? i : System.Array.IndexOf(order, i);
                int row = position / 2, column = position % 2;
                float left = .738f + column * .128f, top = .737f - row * .14f;
                PlaceHud((RectTransform)command.Root.transform, left, top - .13f, left + .119f, top);
                PlaceHud(command.Icon.rectTransform, .39f, .735f, .61f, .97f);
                PlaceHud(command.Fallback.rectTransform, .39f, .735f, .61f, .97f);
                PlaceHud(command.Label.rectTransform, .035f, .33f, .965f, .73f);
                PlaceHud(command.Detail.rectTransform, .035f, .025f, .965f, .325f);
            }
        }

        private Sprite UnitPortrait(Combatant unit)
        {
            var card = CardOf(unit.Id);
            string key = card != null ? card.Data.ArtKey : unit.Id;
            return ArtLibrary.Portrait(unit.Name) ?? ArtLibrary.Enemy(key) ?? BattleFigure(key);
        }

        // The simulation has already advanced to the next input turn during event playback.
        // Use the event actor for every visual cue until playback finishes.
        private Combatant VisibleActor
        {
            get
            {
                if (_battle == null) return null;
                if (_animating) return _hudActor;
                return !_battle.IsOver && _battle.IsPlayerTurn ? _battle.Current : null;
            }
        }

        private static Text BuildActionBadge(RectTransform root)
        {
            var badge = UiFactory.Panel("ActionBadge", root, new Vector2(.10f, .385f), new Vector2(.90f, .525f), HudInk);
            UiFactory.Frame(badge.transform, UiFactory.Gold, 2f);
            var label = UiFactory.Label("Action", badge.transform, new Vector2(.03f, .02f), new Vector2(.97f, .98f), "", 24, UiFactory.Gold);
            badge.gameObject.SetActive(false);
            return label;
        }

        private void PresentAction(BattleEvent ev)
        {
            // Status ticks and reactive phase changes are consequences, not a new action turn.
            if (ev.Kind == EventKind.Status || ev.Kind == EventKind.Phase) return;
            var ally = PanelOf(ev.ActorId);
            var enemy = CardOf(ev.ActorId);
            _hudActor = ally != null ? ally.Unit : enemy != null ? enemy.Unit : null;
            _actorAction = ev.Kind == EventKind.Attack ? "공격 중" : ev.Kind == EventKind.Guard ? "방어 중"
                : ev.Kind == EventKind.Meditate ? "운기조식 중" : ev.Kind == EventKind.Charge ? "기 모으는 중"
                : ev.Kind == EventKind.Heal ? "회복 중" : ev.Kind == EventKind.Item ? "아이템 사용 중"
                : ev.Kind == EventKind.Evade ? "회피 태세" : ev.Kind == EventKind.StunSkip ? "행동 불능" : "";
            RefreshParty();
            RefreshCards();
            RefreshTurnOrder();
        }

        private void RefreshActorHud()
        {
            if (_actorName == null || _battle == null) return;
            var me = VisibleActor;
            if (me == null)
            {
                _actorName.text = ""; _actorResources.text = ""; _commandHint.text = "";
                _actorPortrait.gameObject.SetActive(false); _actorGlyph.text = "";
                if (_turnNotice != null) _turnNotice.text = "";
                return;
            }
            var panel = PanelOf(me.Id);
            int qi = panel != null ? panel.ShownQi : me.Qi, spirit = panel != null ? panel.ShownSpirit : me.Spirit;
            string action = _animating ? _actorAction ?? "" : "내 차례 · 행동 선택";
            _actorName.text = me.Name + (action.Length > 0 ? " · " + action : "");
            if (_turnNotice != null)
            {
                _turnNotice.text = (_animating ? (me.Team == Team.Player ? "아군 · " : "적 · ") : "내 차례 · ") + me.Name + (_animating && action.Length > 0 ? " · " + action : "");
                _turnNotice.color = me.Team == Team.Player ? UiFactory.Gold : ThreatRed;
            }
            _actorResources.text = "내공 " + qi + " / " + me.Stats.MaxQi + "    기세 " + spirit + " / " + InteractiveBattle.MaxSpirit;
            _commandHint.text = _animating ? "전황을 확인해 주세요." : Aiming ? "금색으로 표시된 적을 선택해 주세요." : AimingAlly ? "회복할 아군을 선택해 주세요."
                : _itemMode ? "사용할 아이템을 선택해 주세요." : _jointMode ? "함께 공격할 동료를 선택해 주세요." : "무공 선택 → 대상 지정 · 길게 눌러 설명";
            var pic = UnitPortrait(me);
            _actorPortrait.sprite = pic;
            _actorPortrait.gameObject.SetActive(pic != null);
            _actorGlyph.text = pic == null ? FirstGlyph(me.Name) : "";
        }

        private void RefreshFocusHud()
        {
            if (_focusName == null) return;
            EnemyCard focus = null;
            foreach (var c in _cards)
            {
                if (c.ShownHp <= 0) continue;
                if (focus == null || c.Unit.AwakeningStrike != null || c.Unit.Charging || (c.Unit.IsBoss && !focus.Unit.Charging)) focus = c;
                if (c.Unit.Charging || c.Unit.AwakeningStrike != null) break;
            }
            if (focus == null)
            {
                _focusName.text = "적 진형"; _focusDetail.text = "전투 결과를 확인해 주세요."; _focusHealth.text = "";
                _focusTag.text = "敵"; _focusHp.SetPct(0f);
                foreach (var mark in _phaseMarks) mark.gameObject.SetActive(false);
                return;
            }
            var u = focus.Unit;
            bool threat = u.Charging || u.AwakeningStrike != null;
            _focusAccent.color = threat ? UiFactory.Red : UiFactory.InkSoft;
            _focusTag.text = u.AwakeningStrike != null ? "각성" : threat ? "위험" : u.IsBoss ? "보스" : "敵";
            _focusName.text = u.Name;
            _focusHealth.text = focus.ShownHp.ToString("N0") + " / " + u.Stats.MaxHp.ToString("N0");
            _focusHp.SetPct(focus.ShownHp / (float)System.Math.Max(1, u.Stats.MaxHp));
            string phase = u.Phases != null && u.Phases.Count > 0 ? "  ·  단계 " + (focus.ShownPhase + 1) + "/" + (u.Phases.Count + 1) : "";
            _focusDetail.text = u.AwakeningStrike != null ? "⚡다음 행동: " + BossPhases.AwakeningName + " (기존 강타의 " + (u.PhaseIndex == 1 ? "70%" : "100%") + ") · 방어·회피·점혈로 대비하세요."
                : threat ? u.PeekMove().Name + (u.ChargeTarget != null ? " → " + u.ChargeTarget.Name : "") + "  ·  방어·회피·중단으로 대비하세요."
                : (u.Stunned ? "행동 불능" : "다음: " + u.PeekMove().Name) + phase + (u.Statuses.Count > 0 ? "  ·  " + Statuses.Line(u) : "");
            _focusDetail.color = threat ? ThreatRed : UiFactory.Dim;
            for (int i = 0; i < _phaseMarks.Count; i++)
            {
                bool show = u.Phases != null && i < u.Phases.Count;
                _phaseMarks[i].gameObject.SetActive(show);
                if (show)
                {
                    float threshold = u.Phases[i].HpPct;
                    PlaceHud(_phaseMarks[i].rectTransform, threshold - .0015f, 0f, threshold + .0015f, 1f);
                }
            }
        }
    }
}
