using System;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Scrollable campus prototype built in the existing safe-area canvas.</summary>
    public sealed class AcademyCampus : MonoBehaviour
    {
        private AcademyWalk _walk;
        private RectTransform _viewport, _world, _figure;
        private AcademyJoystick _stick;
        private Button _interact;
        private Text _hint, _action;
        private Action<int> _enter;
        private readonly string[] _names = { "수강신청 · 수업", "동호회", "단 대항전" };
        private readonly ExplorationMotion _motion = new ExplorationMotion();
        private Image _playerArt;
        private Sprite[] _walkFrames;
        private int _shownFrame = -1;

        public void Build(RectTransform root, AcademyWalk walk, Action<int> enter)
        {
            _walk = walk; _enter = enter;
            _viewport = UiFactory.Rect("CampusViewport", root, new Vector2(0f, .07f), new Vector2(1f, .88f));
            _viewport.gameObject.AddComponent<RectMask2D>();
            _world = UiFactory.Rect("CampusWorld", _viewport, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            _world.sizeDelta = new Vector2(AcademyWalk.Width, AcademyWalk.Height);
            var ground = UiFactory.Panel("Ground", _world, Vector2.zero, Vector2.one, new Color(.36f, .39f, .25f));
            ground.sprite = AcademyScenery.Ground;
            ground.raycastTarget = false;
            if (ground.sprite != null) ground.color = Color.white;
            else
            {
                Block("Path", 0f, 350f, 2400f, 250f, new Color(.65f, .56f, .36f));
                Block("PathNorth", 250f, 850f, 1900f, 280f, new Color(.65f, .56f, .36f));
                Block("WestPath", 370f, 400f, 260f, 600f, new Color(.65f, .56f, .36f));
                Block("EastPath", 1770f, 400f, 260f, 600f, new Color(.65f, .56f, .36f));
                Block("Garden", 880f, 650f, 640f, 150f, UiFactory.Jade);
            }
            for (int i = 0; i < 3; i++)
            {
                float x = AcademyWalk.FacilityX[i];
                var building = Block("Facility" + i, x - 200f, 1150f, 400f, 300f, UiFactory.Ink);
                var facade = building.GetComponent<Image>();
                facade.sprite = AcademyScenery.Facility(i);
                // Fit the facade to its collision footprint, including the front step.
                if (facade.sprite != null) facade.color = Color.white;
                else UiFactory.Frame(building, UiFactory.Gold, 3f);
                var sign = Block("FacilitySign" + i, x - 230f, 1470f, 460f, 54f, UiFactory.HeaderInk);
                UiFactory.Frame(sign, UiFactory.GoldDim, 2f);
                UiFactory.Body(UiFactory.Label("Name", sign, Vector2.zero, Vector2.one,
                    _names[i], 30, UiFactory.Gold, TextAnchor.MiddleCenter)).raycastTarget = false;
                Block("Entrance" + i, x - 70f, 1070f, 140f, 8f, UiFactory.Gold);
                var entrance = Block("EntranceLabel" + i, x - 60f, 1010f, 120f, 45f, UiFactory.HeaderInk);
                UiFactory.Label("Text", entrance, Vector2.zero, Vector2.one, "입구", 24,
                    UiFactory.Gold, TextAnchor.MiddleCenter).raycastTarget = false;
            }
            _figure = UiFactory.Rect("Player", _world, Vector2.zero, Vector2.zero);
            _figure.pivot = new Vector2(.5f, 0f);
            _figure.sizeDelta = new Vector2(AcademyWalk.FigureWidth, AcademyWalk.FigureHeight);
            var art = UiFactory.Panel("SD", _figure, Vector2.zero, Vector2.one, Color.white);
            _playerArt = art;
            _walkFrames = ExplorationSprites.Player();
            art.sprite = ArtLibrary.Exploration("player"); art.preserveAspect = true; art.raycastTarget = false;
            ApplyMotion();
            if (art.sprite == null) UiFactory.Label("Fallback", _figure, Vector2.zero, Vector2.one, "비", 64, UiFactory.Gold, TextAnchor.MiddleCenter);
            UiFactory.Label("PlayerName", _figure, new Vector2(-.3f, 1f), new Vector2(1.3f, 1.3f), "비류연", 25, UiFactory.Gold, TextAnchor.MiddleCenter).raycastTarget = false;
            var stick = UiFactory.Panel("MoveStick", root, new Vector2(.02f, .09f), new Vector2(.02f, .09f), UiFactory.Ink);
            stick.rectTransform.pivot = Vector2.zero; stick.rectTransform.sizeDelta = new Vector2(210f, 210f);
            _stick = stick.gameObject.AddComponent<AcademyJoystick>();
            var knob = UiFactory.Panel("Knob", stick.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), UiFactory.GoldDim);
            knob.rectTransform.sizeDelta = new Vector2(72f, 72f); knob.raycastTarget = false; _stick.Knob = knob.rectTransform;
            _interact = UiFactory.Button("Interact", root, new Vector2(.78f, .10f), new Vector2(.97f, .24f), "다가가 주세요", UiFactory.Red, Interact, 30);
            _action = _interact.GetComponentInChildren<Text>();
            _hint = UiFactory.Body(UiFactory.Label("WalkHint", root, new Vector2(.18f, .01f), new Vector2(.98f, .065f), "", 25, UiFactory.PaperText, TextAnchor.MiddleCenter));
            RefreshView();
        }

        private RectTransform Block(string name, float x, float y, float w, float h, Color color)
        {
            var image = UiFactory.Panel(name, _world, Vector2.zero, Vector2.zero, color);
            var rect = image.rectTransform; rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(w, h);
            image.raycastTarget = false; return rect;
        }

        private void Update()
        {
            if (_walk == null) return;
            if (HelpPanel.Instance != null && HelpPanel.Instance.IsOpen)
            {
                _motion.Stop(); ApplyMotion(); return;
            }
            var input = _stick.Direction;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) input.y -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) input.y += 1f;
            float previousX = _walk.X, previousY = _walk.Y;
            _walk.Move(input.x, input.y, Time.unscaledDeltaTime);
            _motion.Advance(_walk.X - previousX, _walk.Y - previousY, Time.unscaledDeltaTime);
            ApplyMotion();
            RefreshView();
            if (Input.GetKeyDown(KeyCode.E)) Interact();
        }

        private void ApplyMotion()
        {
            if (_walkFrames == null || _playerArt == null || _shownFrame == _motion.AtlasIndex) return;
            _shownFrame = _motion.AtlasIndex;
            var sprite = _walkFrames[_shownFrame];
            _playerArt.sprite = sprite;
            // Use a shared pixel scale rather than fitting every crop to the same box.
            // This keeps the feet grounded and avoids size pumping between poses.
            var rect = _playerArt.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = sprite.rect.size * .49f;
        }

        private void OnDisable() { _motion.Stop(); _shownFrame = -1; }

        private void RefreshView()
        {
            _figure.anchoredPosition = new Vector2(_walk.X, _walk.Y);
            _world.anchoredPosition = new Vector2(
                ExplorationInput.CameraOffset(_walk.X, _viewport.rect.width, AcademyWalk.Width),
                ExplorationInput.CameraOffset(_walk.Y, _viewport.rect.height, AcademyWalk.Height));
            int nearby = _walk.NearbyFacility();
            _interact.interactable = nearby >= 0;
            _action.text = nearby >= 0 ? _names[nearby] + " 입장" : "다가가 주세요";
            _hint.text = nearby >= 0 ? "입장 버튼 또는 E키를 눌러 주세요." : "조이스틱 또는 WASD·방향키로 이동해 북쪽 시설 입구에 다가가 주세요.";
        }

        private void Interact()
        {
            int nearby = _walk.NearbyFacility();
            if (nearby >= 0) _enter(nearby);
        }
    }
}
