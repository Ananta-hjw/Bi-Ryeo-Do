using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>
    /// Landscape UI root with a live safe area, management workspaces and battle/story overlays.
    /// </summary>
    [RequireComponent(typeof(TabRouter))]
    public class MainScreen : MonoBehaviour
    {
        private static readonly string[] TabNames = { "강호", "무공", "행낭", "뽑기", "기록", "협객", "장비" };
        private static readonly string[] TabSceneryKeys = { "world", "arts", "bag", "gacha", "record", "companions", "equipment" };
        // No bottom navigation: every screen is reached from the 강호 lobby cards, and every other screen
        // has the same top bar with a 돌아가기 arrow back to 강호 (the Android back key does the same).
        private const float BackBarHeight = 104f;

        private TabRouter _router;
        private int _currentTab;
        private float _dotTimer;
        private GameObject _battleOverlay, _realmModal;
        private Text _realmTitle, _realmBody;
        private WorldScreen _world;
        private GameObject _worldModal;
        private LandscapeHome _home;
        private CompanionScreen _companions;
        private EquipmentScreen _equipment;
        private RecordScreen _record;
        private BattleScreen _battle;

        private void Start()
        {
            _router = GetComponent<TabRouter>();
            EnsureEventSystem();
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            // Paint behind the entire display; only interactive UI is inset around cutouts.
            UiFactory.Panel("Backdrop", canvasGo.transform, Vector2.zero, Vector2.one, UiFactory.Parchment);

            var safe = UiFactory.Rect("SafeArea", canvasGo.transform, Vector2.zero, Vector2.one);
            safe.gameObject.AddComponent<SafeAreaLayout>();

            var content = UiFactory.Rect("Content", safe, Vector2.zero, Vector2.one);

            var panels = new GameObject[TabNames.Length];
            for (int i = 0; i < TabNames.Length; i++)
            {
                var panel = UiFactory.Rect("Panel_" + TabNames[i], content, Vector2.zero, Vector2.one);
                panels[i] = panel.gameObject;
                UiFactory.MenuBackdrop(panel, TabSceneryKeys[i]);
                if (i != (int)TabRouter.Tab.World)
                {
                    panel.gameObject.AddComponent<Image>().color = UiFactory.Parchment;
                    panel.offsetMax = new Vector2(0f, -BackBarHeight);
                    BuildBackBar(panel, TabNames[i]);
                }
            }
            _router.panels = panels;

            var worldRoot = (RectTransform)panels[(int)TabRouter.Tab.World].transform;
            var homeRoot = UiFactory.Rect("LandscapeHome", worldRoot, Vector2.zero, Vector2.one);
            _home = homeRoot.gameObject.AddComponent<LandscapeHome>();
            var modal = UiFactory.Panel("WorldMenus", worldRoot, Vector2.zero, Vector2.one, UiFactory.Parchment);
            _worldModal = modal.gameObject;
            modal.raycastTarget = true;
            var worldMenuRoot = UiFactory.Rect("WorldMenusContent", modal.transform, Vector2.zero, Vector2.one);
            _world = worldMenuRoot.gameObject.AddComponent<WorldScreen>();

            var artsRoot = UiFactory.Rect("ArtsLandscape", panels[(int)TabRouter.Tab.MartialArts].transform, Vector2.zero, Vector2.one);
            var arts = artsRoot.gameObject.AddComponent<MartialArtsScreen>();
            arts.Build(artsRoot);
            var bagRoot = UiFactory.Rect("BagLandscape", panels[(int)TabRouter.Tab.Bag].transform, Vector2.zero, Vector2.one);
            bagRoot.gameObject.AddComponent<BagScreen>().Build(bagRoot);
            var gachaRoot = UiFactory.Rect("GachaLandscape", panels[(int)TabRouter.Tab.Gacha].transform, Vector2.zero, Vector2.one);
            gachaRoot.gameObject.AddComponent<GachaScreen>().Build(gachaRoot);
            var recordRoot = UiFactory.Rect("RecordLandscape", panels[(int)TabRouter.Tab.Record].transform, Vector2.zero, Vector2.one);
            _record = recordRoot.gameObject.AddComponent<RecordScreen>();
            var equipmentRoot = (RectTransform)panels[(int)TabRouter.Tab.Equipment].transform;
            _equipment = equipmentRoot.gameObject.AddComponent<EquipmentScreen>();
            _equipment.Build(equipmentRoot);
            var companionRoot = (RectTransform)panels[(int)TabRouter.Tab.Companions].transform;
            _companions = companionRoot.gameObject.AddComponent<CompanionScreen>();
            _companions.Build(companionRoot, () => { arts.OpenPage(0); Select((int)TabRouter.Tab.MartialArts); },
                () => { arts.OpenPage(1); Select((int)TabRouter.Tab.MartialArts); });

            // The battle host blocks navigation throughout combat and story playback.
            var overlay = UiFactory.Panel("BattleOverlay", safe, Vector2.zero, Vector2.one, UiFactory.Parchment).rectTransform;
            overlay.GetComponent<Image>().raycastTarget = true;
            var battleRoot = UiFactory.Rect("BattleLandscape", overlay, Vector2.zero, Vector2.one);
            _battle = battleRoot.gameObject.AddComponent<BattleScreen>();
            _battle.Build(battleRoot);
            overlay.gameObject.SetActive(false);
            _battleOverlay = overlay.gameObject;

            _world.Build(worldMenuRoot, stage =>
            {
                _world.CloseOverlays();
                _worldModal.SetActive(false);
                _battleOverlay.SetActive(true);
                _battle.Open(stage, ReturnFromBattle);
            }, offer =>
            {
                _world.CloseOverlays();
                _worldModal.SetActive(false);
                _battleOverlay.SetActive(true);
                _battle.OpenPromotion(offer, ReturnFromBattle);
            }, () => Select((int)TabRouter.Tab.Companions));

            _record.Build(recordRoot, (title, lines, done) =>
            {
                _battleOverlay.SetActive(true);
                _battle.OpenStory(title, lines, () =>
                {
                    _battleOverlay.SetActive(false);
                    done();
                });
            }, () => Select((int)TabRouter.Tab.Companions));
            _home.Build(homeRoot, _world, OpenWorldMenu, tab => Select((int)tab));
            // Screens that moved their header into the top bar grew upward under it: keep the bar drawn on top.
            foreach (var panel in panels)
            {
                var bar = panel.transform.Find("BackBar");
                if (bar != null) bar.SetAsLastSibling();
            }
            _worldModal.SetActive(false);

            // Help card sits above everything (tutorial hints and the help list)
            var helpHost = UiFactory.Rect("HelpHost", safe, Vector2.zero, Vector2.one);
            helpHost.gameObject.AddComponent<HelpPanel>().Build(helpHost);

            BuildRealmModal(safe);
            BuildMasteryModal(safe);

            Select((int)_router.startTab);
            SoundManager.Bgm("lobby");
            StartCoroutine(HelpPanel.Instance.ShowOnce("intro"));
        }

        private void OpenWorldMenu(WorldScreen.Menu menu)
        {
            // OnEnable refreshes the legacy view, so activate it before opening its overlay.
            _worldModal.SetActive(true);
            _world.OpenMenu(menu);
        }

        private void ReturnFromBattle()
        {
            _battleOverlay.SetActive(false);
            _world.AfterBattle();
            _worldModal.SetActive(_world.HasOpenOverlay);
            _home.Refresh();
        }

        /// <summary>The top bar of every screen but 강호: a thin gold back arrow and the screen's name. It sits just above the panel, so the panel keeps the height the old tab bar left it.</summary>
        private void BuildBackBar(RectTransform panel, string title)
        {
            var bar = UiFactory.Rect("BackBar", panel, new Vector2(0f, 1f), Vector2.one);
            bar.offsetMin = Vector2.zero;
            bar.offsetMax = new Vector2(0f, BackBarHeight);
            UiFactory.Panel("BackBarBg", bar, Vector2.zero, Vector2.one, UiFactory.HeaderInk);
            UiFactory.Panel("BackBarRule", bar, Vector2.zero, new Vector2(1f, .018f), UiFactory.Gold);
            var back = UiFactory.Button("Back", bar, new Vector2(0f, .5f), new Vector2(0f, .5f), "", Color.clear, GoBack, 28);
            var rect = (RectTransform)back.transform;
            rect.pivot = new Vector2(0f, .5f);
            rect.anchoredPosition = new Vector2(16f, 0f);
            rect.sizeDelta = new Vector2(BackBarHeight * 1.3f, BackBarHeight - 8f);
            var arrow = UiFactory.Glyph("back", back.transform, new Vector2(.12f, .1f), new Vector2(.88f, .9f), UiFactory.Gold);
            arrow.raycastTarget = false;
            int tabIndex = System.Array.IndexOf(TabNames, title);
            string iconKey = tabIndex >= 0 ? TabSceneryKeys[tabIndex] : title;
            if (iconKey == "companions") iconKey = "companion";
            float iconLeft = 16f + BackBarHeight * 1.3f + 12f;
            var icon = UiFactory.Glyph(iconKey, bar, new Vector2(0f, .5f), new Vector2(0f, .5f), UiFactory.Gold);
            icon.rectTransform.pivot = new Vector2(0f, .5f);
            icon.rectTransform.anchoredPosition = new Vector2(iconLeft, 0f);
            icon.rectTransform.sizeDelta = new Vector2(64f, 64f);
            float titleLeft = iconLeft + 80f, titleWidth = 150f;
            var name = UiFactory.Label("Title", bar, new Vector2(0f, 0f), new Vector2(0f, 1f), title, 36, UiFactory.Gold, TextAnchor.MiddleLeft);
            name.rectTransform.offsetMin = new Vector2(titleLeft, 0f);
            name.rectTransform.offsetMax = new Vector2(titleLeft + titleWidth, 0f);
            // Each page adds live information and controls here; the common icon and name stay above.
            var info = UiFactory.Rect("Info", bar, Vector2.zero, Vector2.one);
            info.offsetMin = new Vector2(titleLeft + titleWidth + 16f, 6f);
            info.offsetMax = new Vector2(-16f, -6f);
        }

        /// <summary>돌아가기: back to the 강호 lobby.</summary>
        private void GoBack()
        {
            if (_currentTab != (int)TabRouter.Tab.World) Select((int)TabRouter.Tab.World);
        }

        private void Update()
        {
            if (_worldModal != null && _worldModal.activeSelf && !_world.HasOpenOverlay)
                _worldModal.SetActive(false);
            // The Android back key leaves a screen for 강호, unless a battle, help card or notice is up.
            if (Input.GetKeyDown(KeyCode.Escape) && !(_battleOverlay != null && _battleOverlay.activeSelf)
                && !(HelpPanel.Instance != null && HelpPanel.Instance.IsOpen) && !(_realmModal != null && _realmModal.activeSelf)
                && !(_masteryModal != null && _masteryModal.activeSelf))
                GoBack();
            _dotTimer -= Time.unscaledDeltaTime;
            if (_dotTimer > 0f || _realmModal == null || GameManager.Instance == null) return;
            _dotTimer = 0.4f;
            var save = GameManager.Instance.Save;

            // 무공 경지 rose: tell the player once the battle overlay and any help card are out of the way.
            bool busy = (_battleOverlay != null && _battleOverlay.activeSelf) || (HelpPanel.Instance != null && HelpPanel.Instance.IsOpen) || (_realmModal != null && _realmModal.activeSelf)
                || (_masteryModal != null && _masteryModal.activeSelf);
            int from, to;
            if (!busy && Realm.PollRise(save, out from, out to))
            {
                GameManager.Instance.SaveNow();
                int gained = to - from;
                _realmTitle.text = "경지 돌파";
                _realmBody.text = "<color=#9AA3B2>" + Realm.Label(from) + "</color>   ▶   <size=46><color=#E8C860>" + Realm.Label(to) + "</color></size>"
                    + "\n\n막혀 있던 벽이 소리 없이 무너지고,\n기혈이 새로운 길을 연다."
                    + "\n\n<color=#9ADB9F>체력 · 내공 · 공격력 · 방어력 · 속도  각 +" + Mathf.RoundToInt(Realm.BonusPerStep * gained * 100f) + "%</color>"
                    + "\n(지금까지 누적 +" + Mathf.RoundToInt(Realm.Bonus(to) * 100f) + "%)";
                _realmModal.transform.SetAsLastSibling();
                _realmModal.SetActive(true);
                SoundManager.Sfx("levelup");
                return;
            }

            // 무공 변화: an art grew into the next technique of its line (숙련 + stage + level).
            if (busy) return;
            // 무공 숙련 / 무공 변화: everything the last battle (or a stage / level gate) changed, as one card per art.
            var forms = Mastery.PollNewForms(save);
            var notes = Mastery.PollReport(save);
            if (forms.Count > 0 || notes.Count > 0)
            {
                GameManager.Instance.SaveNow();
                ShowMasteryReport(save, forms, notes);
                return;
            }

            // A companion's 경지 rose or it learned a new art (from its own level, gear, 인연 or 수련): one card for everyone at once.
            var rises = CompanionGrowth.PollRises(save);
            if (rises.Count == 0) return;
            GameManager.Instance.SaveNow();
            bool anyRealm = false, anyArt = false;
            var blocks = new List<string>();
            foreach (var r in rises)
            {
                anyRealm |= r.RealmRose;
                anyArt |= r.NewArts.Count > 0;
                var lines = new List<string> { "<size=38><color=#E8C860>" + r.Companion.Name + "</color></size>" };
                if (r.RealmRose)
                    lines.Add("무공 경지  <color=#9AA3B2>" + Realm.Label(r.RealmFrom) + "</color>  ▶  <color=#E8C860>" + Realm.Label(r.RealmTo) + "</color>");
                foreach (var a in r.NewArts)
                    lines.Add("<color=#9ADB9F>새 무공 「" + a.Name + "」 습득!</color>\n<size=26>" + a.Description + "</size>");
                blocks.Add(string.Join("\n", lines));
            }
            _realmTitle.text = anyRealm && anyArt ? "동료 성장" : anyRealm ? "동료 경지 돌파" : "동료 새 무공";
            _realmBody.text = string.Join("\n\n", blocks)
                + (anyArt ? "\n\n<color=#9AA3B2>새 무공은 다음 전투부터 쓸 수 있습니다.</color>" : "");
            _realmModal.transform.SetAsLastSibling();
            _realmModal.SetActive(true);
            SoundManager.Sfx("levelup");
        }

        private GameObject _masteryModal;
        private Text _masteryTitle;
        private RectTransform _masteryList;

        /// <summary>The after-battle 「무공 숙련」 report: one card per art (picture, step change, what it gives, what the next step needs), then steps waiting for their gate.</summary>
        private void BuildMasteryModal(RectTransform parent)
        {
            var rt = UiFactory.Rect("MasteryModal", parent, Vector2.zero, Vector2.one);
            _masteryModal = rt.gameObject;
            rt.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);   // blocks everything underneath
            var card = UiFactory.Panel("Card", rt, new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.94f), UiFactory.ModalInk);
            UiFactory.Frame(card.transform, UiFactory.Gold, 3f);
            _masteryTitle = UiFactory.Label("Title", card.transform, new Vector2(0.05f, 0.89f), new Vector2(0.95f, 0.98f), "무공 숙련", 46, UiFactory.Gold);
            _masteryList = CodexPanel.Scroller((RectTransform)card.transform, 0.15f, 0.88f);
            UiFactory.Button("Ok", card.transform, new Vector2(0.30f, 0.03f), new Vector2(0.70f, 0.13f), "확인", UiFactory.Red, CloseMasteryReport, 36);
            _masteryModal.SetActive(false);
        }

        private void CloseMasteryReport()
        {
            _masteryModal.SetActive(false);
            if (HelpPanel.Instance != null && !Tutorial.IsDone(GameManager.Instance.Save, "mastery")) HelpPanel.Instance.Show("mastery", true);
        }

        private const float MasteryCardHeight = 250f, MasteryCardGap = 14f;

        private void ShowMasteryReport(SaveData save, List<string> forms, List<MasteryNote> notes)
        {
            for (int i = _masteryList.childCount - 1; i >= 0; i--) Destroy(_masteryList.GetChild(i).gameObject);
            _masteryTitle.text = forms.Count > 0 && notes.Count == 0 ? "무공 변화" : "무공 숙련";
            float y = MasteryCardGap;
            foreach (var f in forms) y = MasteryLine(y, "<color=#E8C860>" + f + "</color>\n<size=26><color=#9AA3B2>같은 칸에서 바뀐 무공을 그대로 씁니다. 숙련도 이어집니다.</color></size>", 130f);
            bool waitingHeader = false;
            foreach (var n in notes)
            {
                if (n.Waiting && !waitingHeader)
                {
                    waitingHeader = true;
                    y = MasteryLine(y, "<color=#E8C860>횟수 달성 — 조건이 남았습니다</color>\n<size=26><color=#9AA3B2>아래 조건을 채우면 바로 다음 단계에 오릅니다.</color></size>", 110f);
                }
                y = MasteryCard(save, n, y);
            }
            _masteryList.sizeDelta = new Vector2(0f, y);
            _masteryModal.transform.SetAsLastSibling();
            _masteryModal.SetActive(true);
            SoundManager.Sfx("levelup");
        }

        private float MasteryLine(float y, string text, float height)
        {
            var row = UiFactory.Rect("Line", _masteryList, new Vector2(0.03f, 1f), new Vector2(0.97f, 1f));
            row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(0f, height);
            row.anchoredPosition = new Vector2(0f, -y);
            UiFactory.Body(UiFactory.Label("Text", row, Vector2.zero, Vector2.one, text, 32, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal));
            return y + height + MasteryCardGap;
        }

        private float MasteryCard(SaveData save, MasteryNote n, float y)
        {
            var art = n.Art;
            var shown = Mastery.CurrentForm(save, art);
            var row = UiFactory.Rect("Art_" + art.Id, _masteryList, new Vector2(0.03f, 1f), new Vector2(0.97f, 1f));
            row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(0f, MasteryCardHeight);
            row.anchoredPosition = new Vector2(0f, -y);
            row.gameObject.AddComponent<Image>().color = UiFactory.Ink;
            UiFactory.Frame(row, n.Waiting ? UiFactory.GoldDim : UiFactory.Gold, 2f);

            var icon = UiFactory.Panel("Icon", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Color.white);
            icon.rectTransform.sizeDelta = new Vector2(190f, 190f);
            icon.rectTransform.anchoredPosition = new Vector2(115f, 0f);
            icon.sprite = ArtLibrary.Art(shown.Id) ?? ArtLibrary.Art(art.Id);
            icon.preserveAspect = true;
            if (icon.sprite == null)
            {
                icon.color = UiFactory.InkSoft;
                UiFactory.Label("Glyph", icon.transform, Vector2.zero, Vector2.one, shown.Name.Substring(0, 1), 80, UiFactory.Gold);
            }

            var text = UiFactory.Rect("Text", row, Vector2.zero, Vector2.one);
            text.offsetMin = new Vector2(240f, 12f);
            text.offsetMax = new Vector2(-20f, -12f);
            string from = Mastery.LevelNames[n.From], to = Mastery.LevelNames[n.To];
            string step = n.Waiting
                ? "<color=#E8C860>" + to + "</color> 사용 횟수 달성"
                : "<color=#9AA3B2>" + from + "</color>   ▶   <color=#E8C860>" + to + "</color>";
            string middle, bottom;
            if (n.Waiting)
            {
                middle = "<color=#FFB27A>남은 조건: " + Mastery.GateText(art, n.To) + "</color>";
                bottom = "<color=#9AA3B2>조건을 채우면 바로 " + to + "에 오릅니다.</color>";
            }
            else
            {
                string effect = Mastery.TierEffect(art, n.To);
                middle = "<color=#9ADB9F>효과: " + (effect.Length > 0 ? effect : "기본") + "</color>";
                bottom = n.To >= Mastery.MasterLevel
                    ? "<color=#9AA3B2>최고 단계에 올랐습니다.</color>"
                    : "<color=#9AA3B2>다음 " + Mastery.LevelNames[n.To + 1] + ": 사용 " + Mastery.Uses(save, art.Id) + " / " + Mastery.ThresholdsFor(art)[n.To + 1] + "회 · " + Mastery.GateText(art, n.To + 1) + "</color>";
            }
            UiFactory.Label("Name", text, new Vector2(0f, 0.74f), new Vector2(1f, 1f), shown.Name, 36, UiFactory.Gold, TextAnchor.MiddleLeft);
            UiFactory.Label("Step", text, new Vector2(0f, 0.50f), new Vector2(1f, 0.74f), step, 32, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
            UiFactory.Label("Effect", text, new Vector2(0f, 0.26f), new Vector2(1f, 0.50f), middle, 28, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
            UiFactory.Label("Next", text, new Vector2(0f, 0f), new Vector2(1f, 0.26f), bottom, 26, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
            return y + MasteryCardHeight + MasteryCardGap;
        }

        private void BuildRealmModal(RectTransform parent)
        {
            var rt = UiFactory.Rect("RealmModal", parent, Vector2.zero, Vector2.one);
            _realmModal = rt.gameObject;
            var dim = rt.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.7f);   // blocks everything underneath
            var card = UiFactory.Panel("Card", rt, new Vector2(0.05f, 0.17f), new Vector2(0.95f, 0.83f), UiFactory.ModalInk);
            UiFactory.Frame(card.transform, UiFactory.Gold, 3f);
            _realmTitle = UiFactory.Label("Title", card.transform, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.97f), "", 46, UiFactory.Gold);
            _realmBody = UiFactory.Body(UiFactory.ScrollBody(UiFactory.Label("Body", card.transform, new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.82f), "", 30, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal), 36));
            UiFactory.Button("Ok", card.transform, new Vector2(0.30f, 0.05f), new Vector2(0.70f, 0.20f), "확인", UiFactory.Red, () => _realmModal.SetActive(false), 36);
            _realmModal.SetActive(false);
        }

        private void Select(int index)
        {
            if (index < 0 || index >= TabNames.Length) index = (int)TabRouter.Tab.World;
            _currentTab = index;
            _router.Show(index);
            if (index == (int)TabRouter.Tab.Gacha && HelpPanel.Instance != null && !Tutorial.IsDone(GameManager.Instance.Save, "gacha"))
                HelpPanel.Instance.Show("gacha", true);
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
