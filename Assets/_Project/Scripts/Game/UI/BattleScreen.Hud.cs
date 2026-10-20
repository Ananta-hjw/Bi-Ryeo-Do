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
        private Text _actorName, _actorResources, _commandHint;
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

            var timeline = UiFactory.Panel("TurnRibbon", root, new Vector2(.02f, .855f), new Vector2(.98f, .918f), HudInk);
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

            var focus = UiFactory.Panel("EnemyFocus", root, new Vector2(.025f, .804f), new Vector2(.975f, .847f), HudInk);
            UiFactory.Frame(focus.transform, UiFactory.GoldDim, 1.5f);
            _focusAccent = UiFactory.Panel("Seal", focus.transform, new Vector2(.012f, .22f), new Vector2(.106f, .88f), UiFactory.Red);
            _focusTag = UiFactory.Label("SealText", _focusAccent.transform, Vector2.zero, Vector2.one, "敵", 26, UiFactory.PaperText);
            _focusName = UiFactory.Label("Name", focus.transform, new Vector2(.13f, .53f), new Vector2(.69f, .99f), "", 26, UiFactory.Gold, TextAnchor.MiddleLeft);
            _focusHealth = UiFactory.Label("Health", focus.transform, new Vector2(.70f, .53f), new Vector2(.97f, .99f), "", 20, UiFactory.PaperText, TextAnchor.MiddleRight);
            _focusDetail = UiFactory.Label("Intent", focus.transform, new Vector2(.13f, .17f), new Vector2(.97f, .55f), "", 21, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
            _focusHp = UiFactory.MakeBar("BossHealth", focus.transform, new Vector2(.025f, .045f), new Vector2(.975f, .11f), Color.black, UiFactory.Red);
            for (int i = 0; i < 2; i++)
                _phaseMarks.Add(UiFactory.Panel("PhaseMark" + i, _focusHp.Root, new Vector2(.5f, 0f), new Vector2(.502f, 1f), UiFactory.Gold));

            var log = UiFactory.Panel("BattleLog", root, new Vector2(.025f, .332f), new Vector2(.975f, .372f), HudInk);
            UiFactory.Panel("LogAccent", log.transform, Vector2.zero, new Vector2(.004f, 1f), UiFactory.Gold);
            UiFactory.Label("LogTitle", log.transform, new Vector2(.025f, .05f), new Vector2(.12f, .95f), "전황", 21, UiFactory.Gold);
            _log = UiFactory.Label("Log", log.transform, new Vector2(.15f, .05f), new Vector2(.975f, .95f), "무공을 고른 뒤 대상을 선택해 주세요.", 25, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);

            var deck = UiFactory.Panel("CommandDeck", root, Vector2.zero, new Vector2(1f, .321f), HudInk);
            UiFactory.Panel("DeckRule", deck.transform, new Vector2(0f, .994f), Vector2.one, UiFactory.GoldDim);
            UiFactory.Glyph("halo", deck.transform, new Vector2(.66f, -.22f), new Vector2(1.18f, .68f), new Color(.83f, .72f, .48f, .07f));
            var header = UiFactory.Rect("ActingMember", root, new Vector2(.025f, .231f), new Vector2(.735f, .312f));
            _commandHeader = header.gameObject;
            var portraitFrame = UiFactory.Panel("PortraitBack", header, new Vector2(0f, .04f), new Vector2(.16f, .96f), UiFactory.InkSoft);
            UiFactory.Frame(portraitFrame.transform, UiFactory.GoldDim, 1.5f);
            _actorPortrait = UiFactory.Panel("Portrait", portraitFrame.transform, new Vector2(.07f, .03f), new Vector2(.93f, .97f), Color.white);
            _actorPortrait.preserveAspect = true;
            _actorGlyph = UiFactory.Label("Fallback", portraitFrame.transform, Vector2.zero, Vector2.one, "", 48, UiFactory.Gold);
            _actorName = UiFactory.Heading(UiFactory.Label("Actor", header, new Vector2(.19f, .58f), new Vector2(.98f, 1f), "", 34, UiFactory.PaperText, TextAnchor.MiddleLeft));
            _actorResources = UiFactory.Label("Resources", header, new Vector2(.19f, .31f), new Vector2(.98f, .59f), "", 28, UiFactory.Jade, TextAnchor.MiddleLeft);
            _commandHint = UiFactory.Label("Hint", header, new Vector2(.19f, .02f), new Vector2(.98f, .30f), "", 26, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);

            for (int i = 0; i < _commands.Length; i++)
            {
                int index = i;
                var btn = UiFactory.Button("Command" + i, root, Vector2.zero, Vector2.one, "", UiFactory.InkSoft, () => OnCommandPressed(index), 32);
                var label = btn.GetComponentInChildren<Text>();
                label.resizeTextMinSize = 28;
                var detail = UiFactory.Label("Detail", btn.transform, Vector2.zero, Vector2.one, "", 28, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal);
                detail.resizeTextMinSize = 26;
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
            for (int i = 0; i < _commands.Length; i++)
            {
                var c = _commands[i];
                var rt = (RectTransform)c.Root.transform;
                bool art = !picker && i >= 1 && i <= 4;
                if (picker)
                {
                    int row = i / 4, col = i % 4;
                    float x = .025f + col * .241f, top = .222f - row * .106f;
                    PlaceHud(rt, x, top - .099f, x + .227f, top);
                }
                else if (art)
                {
                    float x = .025f + (i - 1) * .241f;
                    PlaceHud(rt, x, .117f, x + .227f, .222f);
                }
                else if (i == 9) PlaceHud(rt, .755f, .231f, .975f, .312f);
                else
                {
                    int column = i == 0 ? 0 : i - 4;
                    float x = .025f + column * .192f;
                    PlaceHud(rt, x, .021f, x + .18f, .105f);
                }
                if (art || picker)
                {
                    PlaceHud(c.Icon.rectTransform, .37f, .73f, .63f, .95f);
                    PlaceHud(c.Fallback.rectTransform, .37f, .73f, .63f, .95f);
                    PlaceHud(c.Label.rectTransform, .04f, .24f, .96f, .72f);
                    PlaceHud(c.Detail.rectTransform, .03f, .025f, .97f, .235f);
                }
                else if (i == 9)
                {
                    PlaceHud(c.Icon.rectTransform, .045f, .08f, .22f, .35f);
                    PlaceHud(c.Fallback.rectTransform, .045f, .08f, .22f, .35f);
                    PlaceHud(c.Label.rectTransform, .05f, .40f, .95f, .96f);
                    PlaceHud(c.Detail.rectTransform, .24f, .04f, .97f, .38f);
                }
                else
                {
                    PlaceHud(c.Icon.rectTransform, .06f, .57f, .29f, .89f);
                    PlaceHud(c.Fallback.rectTransform, .06f, .57f, .29f, .89f);
                    PlaceHud(c.Label.rectTransform, .31f, .54f, .95f, .96f);
                    PlaceHud(c.Detail.rectTransform, .04f, .02f, .96f, .51f);
                }
            }
        }

        private Sprite UnitPortrait(Combatant unit)
        {
            var card = CardOf(unit.Id);
            string key = card != null ? card.Data.ArtKey : unit.Id;
            return ArtLibrary.Portrait(unit.Name) ?? ArtLibrary.Enemy(key) ?? BattleFigure(key);
        }

        private void RefreshActorHud()
        {
            if (_actorName == null || _battle == null) return;
            var me = _hudActor ?? _battle.Current;
            if (me == null) return;
            var panel = PanelOf(me.Id);
            int qi = panel != null ? panel.ShownQi : me.Qi, spirit = panel != null ? panel.ShownSpirit : me.Spirit;
            _actorName.text = me.Name + (_animating ? " · 초식 전개" : " · 행동 선택");
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
                if (focus == null || c.Unit.Charging || (c.Unit.IsBoss && !focus.Unit.Charging)) focus = c;
                if (c.Unit.Charging) break;
            }
            if (focus == null)
            {
                _focusName.text = "적 진형"; _focusDetail.text = "전투 결과를 확인해 주세요."; _focusHealth.text = "";
                _focusTag.text = "敵"; _focusHp.SetPct(0f);
                foreach (var mark in _phaseMarks) mark.gameObject.SetActive(false);
                return;
            }
            var u = focus.Unit;
            bool threat = u.Charging;
            _focusAccent.color = threat ? UiFactory.Red : UiFactory.InkSoft;
            _focusTag.text = threat ? "위험" : u.IsBoss ? "보스" : "敵";
            _focusName.text = u.Name;
            _focusHealth.text = focus.ShownHp.ToString("N0") + " / " + u.Stats.MaxHp.ToString("N0");
            _focusHp.SetPct(focus.ShownHp / (float)System.Math.Max(1, u.Stats.MaxHp));
            string phase = u.Phases != null && u.Phases.Count > 0 ? "  ·  단계 " + (focus.ShownPhase + 1) + "/" + (u.Phases.Count + 1) : "";
            _focusDetail.text = threat ? u.PeekMove().Name + (u.ChargeTarget != null ? " → " + u.ChargeTarget.Name : "") + "  ·  방어·회피·중단으로 대비하세요."
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
