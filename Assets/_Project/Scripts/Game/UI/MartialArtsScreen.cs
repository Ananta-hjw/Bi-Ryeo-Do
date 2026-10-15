using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>무공 탭. Learn arts with 두 once their stage is unlocked, then equip up to <see cref="Progression.MaxEquippedArts"/> for battle.</summary>
    public class MartialArtsScreen : MonoBehaviour
    {
        private sealed class Row
        {
            public ArtData Art;
            public Text Title, Detail, ButtonLabel, MasteryLabel;
            public Button Button, MasteryButton;
            public Image ButtonImage, MasteryImage, Icon;
            public Text IconGlyph;
        }

        private readonly List<Row> _rows = new List<Row>();
        private Text _header, _hint;
        private bool _built;

        public void Build(RectTransform root)
        {
            if (_built) return;
            _built = true;

            UiFactory.Frame(UiFactory.Panel("HeaderBand", root, new Vector2(0.04f, 0.925f), new Vector2(0.96f, 0.985f), UiFactory.Ink).transform, UiFactory.GoldDim, 2f);
            _header = UiFactory.Label("Header", root, new Vector2(0.06f, 0.925f), new Vector2(0.94f, 0.985f), "", 42, UiFactory.Gold);
            _hint = UiFactory.Label("Hint", root, new Vector2(0.05f, 0.885f), new Vector2(0.95f, 0.925f), "", 28, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal);

            // The list scrolls: the protagonist has far more arts than fit on one screen.
            var viewport = UiFactory.Rect("Viewport", root, Vector2.zero, new Vector2(1f, 0.88f));
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UiFactory.Rect("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f));
            content.pivot = new Vector2(0.5f, 1f);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var arts = EarlyGameContent.Arts.Where(a => a.OwnerId == null).ToArray();
            const float rowH = 170f, gap = 12f;
            content.sizeDelta = new Vector2(0f, arts.Length * (rowH + gap) + gap);
            for (int i = 0; i < arts.Length; i++)
            {
                var art = arts[i];
                var row = new Row { Art = art };
                var rt = UiFactory.Rect("Row" + i, content, new Vector2(0.04f, 1f), new Vector2(0.96f, 1f));
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -(gap + i * (rowH + gap)));
                rt.sizeDelta = new Vector2(0f, rowH);
                var bg = rt.gameObject.AddComponent<Image>();
                bg.color = UiFactory.Ink;
                bg.raycastTarget = true;   // lets a drag that starts on a row still scroll
                UiFactory.Frame(rt, UiFactory.GoldDim, 2f);
                row.Icon = UiFactory.Panel("Icon", rt, new Vector2(0.025f, 0.14f), new Vector2(0.135f, 0.86f), IconColor(art));
                UiFactory.Frame(row.Icon.transform, UiFactory.Gold, 2f);
                row.IconGlyph = UiFactory.Label("Glyph", row.Icon.transform, Vector2.zero, Vector2.one, art.Name.Substring(0, 1), 50, UiFactory.PaperText);
                row.Title = UiFactory.Label("Title", rt, new Vector2(0.15f, 0.50f), new Vector2(0.51f, 0.97f), "", 32, UiFactory.PaperText, TextAnchor.MiddleLeft);
                row.Detail = UiFactory.Label("Detail", rt, new Vector2(0.15f, 0.03f), new Vector2(0.51f, 0.50f), "", 20, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                row.Button = UiFactory.Button("Btn" + i, rt, new Vector2(0.52f, 0.12f), new Vector2(0.73f, 0.88f), "", UiFactory.Red, () => OnPressed(art), 28);
                row.ButtonImage = row.Button.GetComponent<Image>();
                row.ButtonLabel = row.Button.GetComponentInChildren<Text>();
                row.MasteryButton = UiFactory.Button("Mastery" + i, rt, new Vector2(0.75f, 0.12f), new Vector2(0.97f, 0.88f), "", UiFactory.InkSoft, () => OnMastery(art), 24);
                row.MasteryImage = row.MasteryButton.GetComponent<Image>();
                row.MasteryLabel = row.MasteryButton.GetComponentInChildren<Text>();
                _rows.Add(row);
            }
            Refresh();
        }

        private void OnEnable() { if (_built) Refresh(); }

        private void OnPressed(ArtData art)
        {
            var save = GameManager.Instance.Save;
            if (!save.LearnedArts.Contains(art.Id))
            {
                if (save.HighestUnlockedStage < art.UnlockStage || save.Doo < art.LearnCost) return;
                save.Doo -= art.LearnCost;
                save.LearnedArts.Add(art.Id);
                if (save.EquippedArts.Count < Progression.MaxEquippedArts) save.EquippedArts.Add(art.Id);
                _hint.text = art.Name + " 습득!";
            }
            else if (save.EquippedArts.Contains(art.Id))
            {
                save.EquippedArts.Remove(art.Id);
                _hint.text = art.Name + " 장착 해제";
            }
            else if (save.EquippedArts.Count < Progression.MaxEquippedArts)
            {
                save.EquippedArts.Add(art.Id);
                _hint.text = art.Name + " 장착";
            }
            else
            {
                _hint.text = "장착 한도(" + Progression.MaxEquippedArts + "개) 초과 — 먼저 하나를 해제하세요.";
            }
            GameManager.Instance.SaveNow();
            Refresh();
        }

        /// <summary>Icon colour by kind of art: 비기 gold, area purple, evade/guard blue, heal green, stun teal, otherwise red.</summary>
        private static Color IconColor(ArtData a)
        {
            if (a.SpiritCost > 0) return new Color(0.62f, 0.44f, 0.12f, 1f);
            if (a.AllEnemies) return new Color(0.38f, 0.22f, 0.55f, 1f);
            if (a.Effect == SkillEffect.Evade || a.Effect == SkillEffect.GuardParty) return new Color(0.18f, 0.34f, 0.58f, 1f);
            if (a.Effect == SkillEffect.HealAlly) return new Color(0.20f, 0.46f, 0.30f, 1f);
            if (a.Effect == SkillEffect.Stun) return new Color(0.16f, 0.46f, 0.50f, 1f);
            return new Color(0.55f, 0.18f, 0.18f, 1f);
        }

        private static string Stars(int level)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < Mastery.Thresholds.Length; i++) sb.Append(i <= level ? "★" : "☆");
            return sb.ToString();
        }

        private void OnMastery(ArtData art)
        {
            var save = GameManager.Instance.Save;
            if (!save.LearnedArts.Contains(art.Id)) return;
            if (Mastery.Level(save, art.Id) < Mastery.VariantLevel)
            {
                _hint.text = art.Name + ": " + Mastery.LevelNames[Mastery.VariantLevel] + "(" + Mastery.Thresholds[Mastery.VariantLevel] + "회 사용)에 변초가 열린다.";
            }
            else if (Mastery.AvailableVariants(art).Count == 0)
            {
                _hint.text = art.Name + "은(는) 변초가 없는 무공이다.";
            }
            else
            {
                var v = Mastery.CycleVariant(save, art);
                _hint.text = art.Name + " — " + Mastery.VariantHint(v);
                GameManager.Instance.SaveNow();
            }
            Refresh();
        }

        private static string Describe(ArtData a)
        {
            string cost = a.SpiritCost > 0 ? "기세 " + a.SpiritCost : a.QiCost > 0 ? "내공 " + a.QiCost : "내공 0";
            string power = a.Effect == SkillEffect.Evade ? "회피" : "위력 ×" + a.Power.ToString("0.0") + (a.Hits > 1 ? " ×" + a.Hits + "타" : "") + (a.AllEnemies ? " 전체" : "");
            string cd = a.Cooldown > 0 ? " · 재사용 " + a.Cooldown + "턴" : "";
            return power + " · " + cost + cd;
        }

        private void Refresh()
        {
            var save = GameManager.Instance.Save;
            _header.text = "무공 · 장착 " + save.EquippedArts.Count + "/" + Progression.MaxEquippedArts + " · " + save.Doo.ToString("N0") + "두";
            if (string.IsNullOrEmpty(_hint.text)) _hint.text = "숙련(사용 횟수): " + Mastery.ConditionText();

            foreach (var r in _rows)
            {
                var a = r.Art;
                bool learned = save.LearnedArts.Contains(a.Id);
                bool equipped = save.EquippedArts.Contains(a.Id);
                bool unlocked = save.HighestUnlockedStage >= a.UnlockStage;

                int mLevel = Mastery.Level(save, a.Id);
                var variant = Mastery.VariantOf(save, a.Id);
                r.Icon.color = (learned || unlocked) ? IconColor(a) : new Color(0.2f, 0.2f, 0.22f, 1f);
                r.Title.text = a.Name + (variant != ArtVariant.None ? "(" + Mastery.VariantName(variant) + ")" : "") + (equipped ? "  [장착]" : "");
                bool canVariant = Mastery.AvailableVariants(a).Count > 0;
                r.MasteryButton.gameObject.SetActive(learned);
                r.MasteryLabel.text = Mastery.LevelNames[mLevel] + (mLevel >= Mastery.VariantLevel && canVariant ? "\n변초 바꾸기" : "\n" + Mastery.Uses(save, a.Id) + "회");
                r.MasteryImage.color = mLevel >= Mastery.VariantLevel && canVariant ? UiFactory.Gold : UiFactory.InkSoft;
                r.Detail.text = learned || unlocked ? (learned && a.OwnerId == null ? Stars(mLevel) + " " + (Mastery.UsesToNext(save, a.Id) < 0 ? "극의" : Mastery.Uses(save, a.Id) + "/" + Mastery.Thresholds[mLevel + 1] + "회") + "  " : "") + Describe(a) : "스테이지 " + a.UnlockStage + "에 도달하면 열린다";

                if (learned)
                {
                    r.ButtonLabel.text = equipped ? "해제" : "장착";
                    r.ButtonImage.color = equipped ? UiFactory.InkSoft : UiFactory.Red;
                    r.Button.interactable = true;
                }
                else if (!unlocked)
                {
                    r.ButtonLabel.text = "잠김";
                    r.ButtonImage.color = UiFactory.InkSoft;
                    r.Button.interactable = false;
                }
                else
                {
                    bool can = save.Doo >= a.LearnCost;
                    r.ButtonLabel.text = "습득 " + a.LearnCost.ToString("N0") + "두";
                    r.ButtonImage.color = can ? UiFactory.Red : UiFactory.InkSoft;
                    r.Button.interactable = can;
                }
            }
        }
    }
}
