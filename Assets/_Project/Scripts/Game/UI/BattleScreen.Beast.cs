using System.Collections;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>
    /// 영수 (Core <see cref="Beasts"/>): the 「영수」 button above the command row calls the fight's 영수 on 비류연's turn as a free action.
    /// The rules live in <see cref="InteractiveBattle.CallBeast"/>; this only draws the button, the entrance and the support floats.
    /// </summary>
    public sealed partial class BattleScreen
    {
        private CommandButton _beastButton;
        private bool _beastMode;              // the 영수's skills are listed on the command row
        private BeastSkill _aimBeast;         // a 영수 skill waiting for its enemy
        private BeastLoadout _beastForFight;  // handed to the next InteractiveBattle (stage fights only)
        private bool _trialRunFight;          // the stage being played is a 사부의 시련 관문 (giving up ends the run)

        private void BuildBeastButton(RectTransform root)
        {
            var btn = UiFactory.Button("BeastCall", root, new Vector2(.835f, .222f), new Vector2(.985f, .282f), "", new Color(.10f, .13f, .12f, .82f), OnBeastPressed, 24);
            UiFactory.Frame(btn.transform, UiFactory.GoldDim, 1.5f);
            var label = btn.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            label.fontSize = label.resizeTextMaxSize = 24;
            label.resizeTextMinSize = 20;
            PlaceHud(label.rectTransform, .30f, .45f, .98f, 1f);
            var detail = UiFactory.Label("Detail", btn.transform, new Vector2(.30f, 0f), new Vector2(.98f, .5f), "", 20, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
            var clip = UiFactory.Rect("IconClip", btn.transform, new Vector2(.02f, .05f), new Vector2(.27f, .95f));
            var icon = UiFactory.Panel("Icon", clip, Vector2.zero, Vector2.one, Color.white);
            icon.preserveAspect = true;
            var glyph = UiFactory.Glyph("fortune", clip, Vector2.zero, Vector2.one, UiFactory.Gold);
            var rim = UiFactory.Glyph("icon_rim", clip, Vector2.zero, Vector2.one, UiFactory.GoldDim);
            OutlineCommandText(label); OutlineCommandText(detail);
            _beastButton = new CommandButton { Button = btn, Image = btn.GetComponent<Image>(), Label = label, Detail = detail, Root = btn.gameObject, Icon = icon, Fallback = glyph, Rim = rim };
            btn.gameObject.AddComponent<LongPress>().OnLong = ShowBeastTip;
            _beastButton.Root.SetActive(false);
        }

        /// <summary>Shown on 비류연's turn when a 영수 came along; greyed while it rests.</summary>
        private void RefreshBeastButton()
        {
            if (_beastButton == null) return;
            var beast = _battle != null ? _battle.Beast : null;
            bool show = beast != null && _battle.Current != null && _battle.Current == _battle.Player && _battle.Player.Id == Inventory.Hero
                && !Aiming && !AimingAlly && !_itemMode && !_jointMode && !_beastMode;
            _beastButton.Root.SetActive(show);
            if (!show) return;
            bool any = false;
            foreach (var s in beast.Skills) if (_battle.CanUseBeastSkill(s)) any = true;
            bool ready = _battle.CanCallBeast && any;
            var art = ArtLibrary.Beast(beast.Data);
            _beastButton.Icon.gameObject.SetActive(art != null);
            _beastButton.Icon.sprite = art;
            _beastButton.Icon.color = ready ? Color.white : new Color(1f, 1f, 1f, .35f);
            _beastButton.Fallback.gameObject.SetActive(art == null);
            _beastButton.Fallback.color = ready ? UiFactory.Gold : new Color(.65f, .68f, .70f, .4f);
            _beastButton.Rim.color = ready ? UiFactory.Gold : new Color(.5f, .52f, .54f, .45f);
            _beastButton.Label.text = "영수 · " + beast.Data.Name;
            _beastButton.Label.color = ready ? UiFactory.PaperText : UiFactory.Dim;
            _beastButton.Detail.text = _battle.BeastCooldownLeft > 0 ? "휴식 " + _battle.BeastCooldownLeft + "턴" : ready ? "부르기 · 차례 소모 없음" : "닿는 대상 없음";
            _beastButton.Detail.color = ready ? UiFactory.Gold : new Color(.72f, .54f, .47f, 1f);
            _beastButton.Button.interactable = ready && !_hasCommand;
        }

        private void OnBeastPressed()
        {
            if (LongPress.JustFired) return;
            if (_battle == null || !_battle.IsPlayerTurn || _hasCommand || !_battle.CanCallBeast) return;
            var skills = _battle.Beast.Skills;
            SoundManager.Sfx("ui_click");
            if (skills.Count == 1) { StartBeastSkill(skills[0]); return; }
            _beastMode = true;
            _log.text = _battle.Beast.Data.Name + " — 쓸 기술을 고르세요.";
            RefreshCommands();
        }

        private void StartBeastSkill(BeastSkill skill)
        {
            if (!_battle.CanUseBeastSkill(skill))
            {
                _log.text = "지금은 " + skill.Name + "을(를) 쓸 수 없습니다.";
                RefreshCommands();
                return;
            }
            if (skill.NeedsEnemy)
            {
                var aim = new SkillSpec { Id = skill.Id, Name = skill.Name, Power = skill.Power, Range = skill.Target == BeastTarget.FrontEnemy ? 1 : 5 };
                BeginAim(aim, null);
                _aimBeast = skill;
                return;
            }
            if (skill.NeedsAlly)
            {
                var picked = skill;
                BeginAllyAim(skill.Name, ally => PlayerCommand.CallBeast(picked, null).WithAlly(ally));
                return;
            }
            Issue(PlayerCommand.CallBeast(skill, null));
        }

        /// <summary>Runs the call in Core and plays it; the turn stays 비류연's.</summary>
        private IEnumerator CallBeast(PlayerCommand cmd)
        {
            SetCommandsInteractable(false);
            List<BattleEvent> events;
            try { events = _battle.CallBeast(cmd); }
            catch (System.InvalidOperationException) { events = new List<BattleEvent>(); }
            _statsCommand = cmd;
            yield return PlayEvents(events);
        }

        private static string BeastArrival(List<BeastData> beasts)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var b in beasts) sb.Append(b.Name + " (" + b.Kind + ")\n" + b.Description + "\n\n");
            sb.Append("협객 화면의 「영수」에서 데려갈 영수를 고르고 먹이를 줄 수 있습니다. 전투에서는 비류연 차례에 「영수」 버튼으로 부르며, 차례를 쓰지 않습니다.");
            return sb.ToString();
        }

        private void ShowBeastTip()
        {
            if (_battle == null || _battle.Beast == null) return;
            var l = _battle.Beast;
            var sb = new System.Text.StringBuilder();
            sb.Append(l.Data.Kind + " · 친밀도 " + l.Bond + "/" + Beasts.MaxBond + "\n");
            sb.Append("비류연 차례에 부르며 차례를 쓰지 않습니다. 부른 뒤 비류연 차례 " + l.Cooldown + "번 동안 쉽니다.\n\n");
            foreach (var s in l.Data.Skills)
            {
                bool open = l.Skills.Contains(s);
                sb.Append("<color=#D9B86A>" + s.Name + "</color>" + "  " + Beasts.TargetText(s) + (open ? "" : "  (친밀도 " + Beasts.SecondSkillBond + "에 열림)") + "\n");
                sb.Append(s.Description + "\n" + Beasts.EffectText(s, l.Bond) + "\n\n");
            }
            ShowTip(l.Data.Name, sb.ToString().TrimEnd(), ArtLibrary.Beast(l.Data));
        }

        /// <summary>The 영수 bounds in from the party's side with its name and the skill.</summary>
        private IEnumerator BeastEntrance(BattleEvent ev)
        {
            var beast = Beasts.OfUnit(ev.ActorId);
            if (beast == null) yield break;
            var skill = Beasts.FindSkill(ev.SkillId);
            SoundManager.Sfx("charge");
            var rt = UiFactory.Rect("BeastEntrance", _fx, new Vector2(.04f, .36f), new Vector2(.34f, .74f));
            var art = ArtLibrary.Beast(beast);
            Image figure = null;
            if (art != null)
            {
                figure = UiFactory.Panel("Figure", rt, new Vector2(0f, .18f), new Vector2(1f, 1f), Color.white);
                figure.sprite = art;
                figure.preserveAspect = true;
                figure.raycastTarget = false;
            }
            var name = UiFactory.Label("Name", rt, new Vector2(-.2f, 0f), new Vector2(1.2f, .2f), beast.Name + (skill != null ? " · " + skill.Name : ""), 46, UiFactory.Gold);
            OutlineCommandText(name);
            float t = 0f, dur = Wait(.55f);
            var start = new Vector2(-260f, 0f);
            while (t < dur && !_fled)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / (dur * .45f));
                rt.anchoredPosition = Vector2.Lerp(start, Vector2.zero, 1f - (1f - k) * (1f - k));
                float fade = t > dur * .75f ? 1f - (t - dur * .75f) / (dur * .25f) : 1f;
                if (figure != null) figure.color = new Color(1f, 1f, 1f, fade);
                name.color = new Color(UiFactory.Gold.r, UiFactory.Gold.g, UiFactory.Gold.b, fade);
                yield return null;
            }
            Destroy(rt.gameObject);
        }

        /// <summary>A support skill: its tag floats over everyone it reached (one ally, the whole party, or the scouted enemy).</summary>
        private IEnumerator BeastSupport(BattleEvent ev)
        {
            SoundManager.Sfx(ev.Tag == "정찰" ? "ui_click" : "guard");
            _log.text = ev.Message;
            var skill = Beasts.FindSkill(ev.SkillId);
            bool party = skill != null && skill.Target == BeastTarget.AllAllies || skill != null && skill.Effect == BeastEffect.Roar;
            if (party) { foreach (var p in _panels) if (p.Unit.IsAlive) Float(p.Center, ev.Tag, GuardBlue, 50); }
            else
            {
                var panel = PanelOf(ev.TargetId);
                var card = CardOf(ev.TargetId);
                if (panel != null) Float(panel.Center, ev.Tag, GuardBlue, 52);
                else if (card != null) Float(card.Center + new Vector2(0f, .08f), ev.Tag, UiFactory.Gold, 52);
            }
            RefreshParty();
            RefreshCards();
            yield return new WaitForSeconds(Wait(SmallDelay));
        }
    }
}
