using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>
    /// Root of the UI: builds the Canvas (1080x1920 portrait, safe-area aware), the five-tab bottom bar and one panel per tab.
    /// Only 전투 and 무공 are implemented; the other tabs are placeholders until their screens exist.
    /// </summary>
    [RequireComponent(typeof(TabRouter))]
    public class MainScreen : MonoBehaviour
    {
        private static readonly string[] TabNames = { "무공", "경영", "전투", "기화", "행낭" };
        private const float TabBarHeight = 170f;

        private TabRouter _router;
        private Image[] _tabButtons;

        private void Start()
        {
            _router = GetComponent<TabRouter>();
            EnsureEventSystem();

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Safe area (notches / gesture bar)
            var safe = UiFactory.Rect("SafeArea", canvasGo.transform, Vector2.zero, Vector2.one);
            var sa = Screen.safeArea;
            safe.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            safe.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);

            UiFactory.Panel("Backdrop", safe, Vector2.zero, Vector2.one, UiFactory.Parchment);

            // Content area above the tab bar
            var content = UiFactory.Rect("Content", safe, Vector2.zero, Vector2.one);
            content.offsetMin = new Vector2(0f, TabBarHeight);

            var panels = new GameObject[TabNames.Length];
            for (int i = 0; i < TabNames.Length; i++)
            {
                var panel = UiFactory.Rect("Panel_" + TabNames[i], content, Vector2.zero, Vector2.one);
                panels[i] = panel.gameObject;
                if (i == (int)TabRouter.Tab.Battle) BuildBattle(panel);
                else if (i == (int)TabRouter.Tab.MartialArts) gameObject.AddComponent<MartialArtsScreen>().Build(panel);
                else UiFactory.Label("Soon", panel, new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.6f), TabNames[i] + " — 준비 중", 64, UiFactory.Ink);
            }
            _router.panels = panels;

            BuildTabBar(safe);
            Select((int)_router.startTab);
        }

        private void BuildBattle(RectTransform panel)
        {
            var screen = gameObject.AddComponent<BattleScreen>();
            screen.Build(panel);
        }

        private void BuildTabBar(RectTransform parent)
        {
            var bar = UiFactory.Rect("TabBar", parent, Vector2.zero, new Vector2(1f, 0f));
            bar.offsetMin = Vector2.zero;
            bar.offsetMax = new Vector2(0f, TabBarHeight);
            UiFactory.Panel("TabBarBg", bar, Vector2.zero, Vector2.one, UiFactory.Ink);

            _tabButtons = new Image[TabNames.Length];
            float w = 1f / TabNames.Length;
            for (int i = 0; i < TabNames.Length; i++)
            {
                int index = i; // capture
                var btn = UiFactory.Button("Tab_" + TabNames[i], bar, new Vector2(i * w + 0.005f, 0.08f), new Vector2((i + 1) * w - 0.005f, 0.92f),
                    TabNames[i], new Color(0f, 0f, 0f, 0f), () => Select(index), 44);
                _tabButtons[i] = btn.GetComponent<Image>();
            }
        }

        private void Select(int index)
        {
            _router.Show(index);
            for (int i = 0; i < _tabButtons.Length; i++)
                _tabButtons[i].color = i == index ? new Color(0.78f, 0.16f, 0.12f, 0.9f) : new Color(0f, 0f, 0f, 0f);
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
