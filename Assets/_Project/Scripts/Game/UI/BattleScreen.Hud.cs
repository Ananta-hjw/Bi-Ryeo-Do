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
        private readonly List<Combatant> _turnUnits = new List<Combatant>();
        private EnemyCard _focusedEnemy;
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
            var top = UiFactory.Panel("BattleHeader", root, new Vector2(0f, .92f), Vector2.one, HudInk);
            UiFactory.Panel("HeaderRule", top.transform, Vector2.zero, new Vector2(1f, .018f), UiFactory.GoldDim);
            _stageLabel = UiFactory.Label("Stage", top.transform, new Vector2(.035f, .06f), new Vector2(.40f, .96f), "", 32, UiFactory.PaperText, TextAnchor.MiddleLeft);
            var speed = UiFactory.Button("Speed", top.transform, new Vector2(.715f, .05f), new Vector2(.845f, .95f), "속도 x1", UiFactory.InkSoft, OnSpeedPressed, 28);
            _speedLabel = speed.GetComponentInChildren<Text>();
            UiFactory.Button("Flee", top.transform, new Vector2(.855f, .05f), new Vector2(.985f, .95f), "포기", HudInk, OnFleePressed, 28);

            var timeline = UiFactory.Panel("TurnRibbon", root, new Vector2(FieldLeft, .808f), new Vector2(.505f, .913f), HudInk);
            UiFactory.Label("TurnTitle", timeline.transform, new Vector2(.012f, .5f), new Vector2(.105f, .96f), "행동", 22, UiFactory.Gold);
            UiFactory.Label("TurnSubtitle", timeline.transform, new Vector2(.012f, .08f), new Vector2(.105f, .53f), "순서", 22, UiFactory.PaperText);
            for (int i = 0; i < 5; i++)
            {
                float x = .13f + i * .173f;
                var chip = UiFactory.Panel("Turn" + i, timeline.transform, new Vector2(x, .04f), new Vector2(x + .161f, .96f), HudInk);
                var edge = UiFactory.Panel("TeamEdge", chip.transform, Vector2.zero, new Vector2(.025f, 1f), UiFactory.Jade);
                UiFactory.Label("Position", chip.transform, new Vector2(.1f, .77f), new Vector2(.9f, .98f), i == 0 ? "현재" : (i + 1).ToString("00"), 20, i == 0 ? UiFactory.Gold : UiFactory.Dim);
                var pic = UiFactory.Panel("Portrait", chip.transform, new Vector2(.22f, .28f), new Vector2(.78f, .76f), Color.white);
                pic.preserveAspect = true;
                var glyph = UiFactory.Label("Fallback", chip.transform, new Vector2(.12f, .27f), new Vector2(.88f, .77f), "", 29, UiFactory.Gold);
                var name = UiFactory.Label("Name", chip.transform, new Vector2(.05f, .02f), new Vector2(.95f, .29f), "", 24, UiFactory.PaperText);
                int turnIndex = i;
                chip.raycastTarget = true;
                var inspect = chip.gameObject.AddComponent<Button>();
                inspect.targetGraphic = chip;
                inspect.transition = Selectable.Transition.None;
                inspect.onClick.AddListener(() => InspectTurnUnit(turnIndex));
                _turnUnits.Add(null);
                chip.gameObject.SetActive(false);   // empty until the first RefreshTurnOrder, so no white placeholder squares show before a fight (and during the story)
                _turnChips.Add(chip); _turnLabels.Add(name); _turnPortraits.Add(pic); _turnGlyphs.Add(glyph); _turnEdges.Add(edge);
            }

            var focus = UiFactory.Panel("EnemyFocus", root, new Vector2(.52f, .808f), new Vector2(FieldRight, .913f), HudInk);
            UiFactory.Frame(focus.transform, UiFactory.GoldDim, 1.5f);
            focus.raycastTarget = true;
            var focusInfo = focus.gameObject.AddComponent<Button>();
            focusInfo.targetGraphic = focus;
            focusInfo.transition = Selectable.Transition.None;
            focusInfo.onClick.AddListener(() => { if (_focusedEnemy != null) ShowEnemyTip(_focusedEnemy); });
            _focusAccent = UiFactory.Panel("Seal", focus.transform, new Vector2(.012f, .22f), new Vector2(.106f, .88f), UiFactory.Red);
            _focusTag = UiFactory.Label("SealText", _focusAccent.transform, Vector2.zero, Vector2.one, "敵", 26, UiFactory.PaperText);
            _focusName = UiFactory.Label("Name", focus.transform, new Vector2(.13f, .70f), new Vector2(.65f, .99f), "", 30, UiFactory.Gold, TextAnchor.MiddleLeft);
            _focusHealth = UiFactory.Label("Health", focus.transform, new Vector2(.66f, .70f), new Vector2(.97f, .99f), "", 24, UiFactory.PaperText, TextAnchor.MiddleRight);
            _focusDetail = UiFactory.Label("Intent", focus.transform, new Vector2(.13f, .18f), new Vector2(.97f, .70f), "", 24, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
            UiFactory.Body(_focusDetail, 1.1f);
            _focusHp = UiFactory.MakeBar("BossHealth", focus.transform, new Vector2(.025f, .055f), new Vector2(.975f, .15f), Color.black, UiFactory.Red);
            for (int i = 0; i < 2; i++)
                _phaseMarks.Add(UiFactory.Panel("PhaseMark" + i, _focusHp.Root, new Vector2(.5f, 0f), new Vector2(.502f, 1f), UiFactory.Gold));

            // The board runs down to the command icons, which sit on the scene over a light shade (no separate deck band).
            var deck = UiFactory.Panel("CommandDeck", root, Vector2.zero, new Vector2(1f, .22f), Color.white);
            deck.sprite = UiFactory.BottomShade;
            deck.raycastTarget = false;
            // The acting member's name, 내공 and 기세 live in the top bar, next to the stage name.
            var header = UiFactory.Rect("ActingMember", top.transform, new Vector2(.41f, .06f), new Vector2(.70f, .94f));
            _commandHeader = header.gameObject;
            var portraitFrame = UiFactory.Panel("PortraitBack", header, new Vector2(0f, .5f), new Vector2(0f, .5f), UiFactory.InkSoft);
            portraitFrame.rectTransform.pivot = new Vector2(0f, .5f);
            portraitFrame.rectTransform.sizeDelta = new Vector2(68f, 68f);
            UiFactory.Frame(portraitFrame.transform, UiFactory.GoldDim, 1.5f);
            _actorPortrait = UiFactory.Panel("Portrait", portraitFrame.transform, new Vector2(.07f, .03f), new Vector2(.93f, .97f), Color.white);
            _actorPortrait.preserveAspect = true;
            _actorGlyph = UiFactory.Label("Fallback", portraitFrame.transform, Vector2.zero, Vector2.one, "", 48, UiFactory.Gold);
            _actorName = UiFactory.Heading(UiFactory.Label("Actor", header, new Vector2(.13f, .48f), new Vector2(.99f, 1f), "", 32, UiFactory.Gold, TextAnchor.MiddleLeft));
            _actorResources = UiFactory.Label("Resources", header, new Vector2(0f, .01f), new Vector2(.99f, .48f), "", 24, UiFactory.Jade, TextAnchor.MiddleLeft);
            _actorName.rectTransform.anchorMin = new Vector2(0f, .48f);
            _actorName.rectTransform.offsetMin = new Vector2(84f, 0f);
            _actorResources.rectTransform.offsetMin = new Vector2(84f, 0f);

            // Kept for the 전황 text the code still writes, but hidden: no turn or aiming notices over the board.
            var log = UiFactory.Panel("BattleLog", root, new Vector2(.47f, .218f), new Vector2(.985f, .29f), Color.clear);
            log.gameObject.SetActive(false);
            _turnNotice = UiFactory.Label("CurrentTurn", log.transform, new Vector2(.025f, .57f), new Vector2(.975f, 1f), "", 28, UiFactory.Gold, TextAnchor.MiddleLeft);
            log.raycastTarget = true;
            var logInfo = log.gameObject.AddComponent<Button>();
            logInfo.targetGraphic = log;
            logInfo.transition = Selectable.Transition.None;
            logInfo.onClick.AddListener(() => { if (!string.IsNullOrEmpty(_log.text)) ShowTip("전황", _log.text); });
            UiFactory.Label("Inspect", log.transform, new Vector2(.88f, .02f), new Vector2(.985f, .56f), "상세", 22, UiFactory.Gold);
            _log = UiFactory.Label("Log", log.transform, new Vector2(.025f, .02f), new Vector2(.86f, .56f), "무공을 고른 뒤 대상을 선택해 주세요.", 24, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
            // Keep target guidance at the top of the command area, separate from combat feedback.
            _commandHint = UiFactory.Label("CommandHint", root, new Vector2(.025f, .284f), new Vector2(.975f, .314f), "", 26, UiFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Normal);

            foreach (var text in new[] { _actorName, _actorResources, _turnNotice, _log, _commandHint })
                OutlineCommandText(text);

            for (int i = 0; i < _commands.Length; i++)
            {
                int index = i;
                var btn = UiFactory.Button("Command" + i, root, Vector2.zero, Vector2.one, "", Color.clear, () => OnCommandPressed(index), 24);
                var label = btn.GetComponentInChildren<Text>();
                label.fontSize = label.resizeTextMaxSize = 24;
                label.resizeTextMinSize = 22;
                var detail = UiFactory.Label("Detail", btn.transform, Vector2.zero, Vector2.one, "", 22, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal);
                detail.resizeTextMinSize = 20;
                var clip = UiFactory.Rect("IconClip", btn.transform, Vector2.zero, Vector2.one);
                var silhouette = clip.gameObject.AddComponent<Image>();
                silhouette.sprite = UiFactory.CircleMask;
                silhouette.raycastTarget = false;
                silhouette.preserveAspect = true;
                clip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                var icon = UiFactory.Panel("Icon", clip, Vector2.zero, Vector2.one, Color.white);
                icon.preserveAspect = true;
                icon.gameObject.SetActive(false);
                var glyph = UiFactory.Glyph(CommandGlyph(i), clip, Vector2.zero, Vector2.one, UiFactory.Gold);
                var rim = UiFactory.Glyph("icon_rim", clip, Vector2.zero, Vector2.one, UiFactory.GoldDim);
                btn.targetGraphic = rim;
                OutlineCommandText(label); OutlineCommandText(detail);
                _commands[i] = new CommandButton { Button = btn, Image = btn.GetComponent<Image>(), Label = label, Detail = detail,
                    Root = btn.gameObject, Icon = icon, Fallback = glyph, Rim = rim };
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

        private const float IconSize = 128f;   // command icons; the name and cost lines sit right under them

        private void LayoutCommands(bool picker)
        {
            // Large icons share one compact row; transparent full buttons retain generous hit areas.
            int[] order = { 0, 1, 2, 3, 4, 9, 8, 5, 6, 7 };
            for (int i = 0; i < _commands.Length; i++)
            {
                var command = _commands[i];
                int position = picker ? i : System.Array.IndexOf(order, i);
                float left = .012f + position * .098f;
                PlaceHud((RectTransform)command.Root.transform, left, .008f, left + .092f, .213f);
                var clip = (RectTransform)command.Icon.transform.parent;
                PlaceHud(clip, .5f, 1f, .5f, 1f);
                clip.pivot = new Vector2(.5f, 1f);
                clip.sizeDelta = new Vector2(IconSize, IconSize);
                clip.anchoredPosition = new Vector2(0f, -4f);
                PlaceHud(command.Icon.rectTransform, 0f, 0f, 1f, 1f);
                PlaceHud(command.Fallback.rectTransform, 0f, 0f, 1f, 1f);
                PlaceHud(command.Rim.rectTransform, 0f, 0f, 1f, 1f);
                CommandCaption(command.Label.rectTransform, IconSize + 2f, 52f);
                CommandCaption(command.Detail.rectTransform, IconSize + 54f, 24f);
            }
        }

        private static void CommandCaption(RectTransform rect, float top, float height)
        {
            PlaceHud(rect, .01f, 1f, .99f, 1f);
            rect.offsetMin = new Vector2(0f, -top - height);
            rect.offsetMax = new Vector2(0f, -top);
        }

        private static void OutlineCommandText(Text text)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, .95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private static void FixedLabelHeight(Text label, float center, float height)
        {
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(rect.anchorMin.x, center);
            rect.anchorMax = new Vector2(rect.anchorMax.x, center);
            rect.offsetMin = new Vector2(rect.offsetMin.x, -height / 2f);
            rect.offsetMax = new Vector2(rect.offsetMax.x, height / 2f);
        }

        private Sprite UnitPortrait(Combatant unit)
        {
            var card = CardOf(unit.Id);
            string key = card != null ? card.Data.ArtKey : unit.Id;
            return BattleFigure(key);
        }

        private void InspectTurnUnit(int index)
        {
            if (index < 0 || index >= _turnUnits.Count) return;
            var unit = _turnUnits[index];
            if (unit == null) return;
            var enemy = CardOf(unit.Id);
            if (enemy != null) ShowEnemyTip(enemy);
            else ShowAllyTip(unit);
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
            var badgeRect = badge.rectTransform;
            badgeRect.anchorMin = new Vector2(.10f, .455f);
            badgeRect.anchorMax = new Vector2(.90f, .455f);
            badgeRect.sizeDelta = new Vector2(0f, 32f);
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
            _actorName.text = me.Name;
            if (_turnNotice != null)
            {
                _turnNotice.text = "";   // no turn line: the acting member is marked on the board (green ▶) and in the top bar
                _turnNotice.color = me.Team == Team.Player ? UiFactory.Gold : ThreatRed;
            }
            _actorResources.text = "내공 " + qi + " / " + me.Stats.MaxQi + "    기세 " + spirit + " / " + InteractiveBattle.MaxSpirit;
            _commandHint.text = "";   // the board's gold/red floor and the 취소 button already say what to do
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
                // The first boss, else the first living enemy; a wind-up does not pull the panel to its enemy (that would reveal it).
                if (focus == null || (c.Unit.IsBoss && !focus.Unit.IsBoss)) focus = c;
            }
            _focusedEnemy = focus;
            if (focus == null)
            {
                _focusName.text = "적 진형"; _focusDetail.text = "전투 결과를 확인해 주세요."; _focusHealth.text = "";
                _focusTag.text = "敵"; _focusHp.SetPct(0f);
                foreach (var mark in _phaseMarks) mark.gameObject.SetActive(false);
                return;
            }
            var u = focus.Unit;
            _focusAccent.color = u.IsBoss ? UiFactory.Red : UiFactory.InkSoft;
            _focusTag.text = u.IsBoss ? "보스" : "敵";
            _focusName.text = u.Name;
            _focusHealth.text = focus.ShownHp.ToString("N0") + " / " + u.Stats.MaxHp.ToString("N0");
            _focusHp.SetPct(focus.ShownHp / (float)System.Math.Max(1, u.Stats.MaxHp));
            string phase = u.Phases != null && u.Phases.Count > 0 ? "  ·  단계 " + (focus.ShownPhase + 1) + "/" + (u.Phases.Count + 1) : "";
            // Only 행동 불능, the boss phase and 상태 이상: nothing about the next move or a coming big blow.
            string state = (u.Stunned ? "행동 불능" : "") + phase + (u.Statuses.Count > 0 ? "  ·  " + Statuses.Line(u) : "");
            _focusDetail.text = state.TrimStart(' ', '·');
            _focusDetail.color = UiFactory.Dim;
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
