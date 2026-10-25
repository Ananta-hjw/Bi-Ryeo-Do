using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Shared geometry for landscape menu workspaces and their item inspectors.</summary>
    public static class LandscapeMenu
    {
        public static void Place(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = new Vector2(left, bottom);
            rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        public static void Clear(RectTransform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                child.SetActive(false);
                child.name = "Retired_" + child.name;
                if (Application.isPlaying) Object.Destroy(child);
                else Object.DestroyImmediate(child);
            }
        }

        public static void Inspector(RectTransform card)
        {
            bool hasClose = card.Find("Close") != null;
            float actionBottom = hasClose ? .20f : .03f;
            float actionTop = hasClose ? .34f : .17f;
            foreach (Transform child in card)
            {
                var rect = child as RectTransform;
                switch (child.name)
                {
                    case "Picture": Place(rect, .04f, .74f, .30f, .97f); break;
                    case "Title":
                        var title = child.GetComponent<Text>();
                        Place(rect, .34f, .82f, .96f, .98f);
                        UiFactory.ScrollBody(title, 30);
                        break;
                    case "Kind":
                        Place(rect, .34f, .71f, .96f, .82f);
                        UiFactory.ScrollBody(child.GetComponent<Text>(), 25);
                        break;
                    case "KindIcon": child.gameObject.SetActive(false); break;
                    case "BodyViewport":
                        Place(rect, .025f, actionTop + .02f, .975f, .70f);
                        child.GetComponentInChildren<Text>().fontSize = 28;
                        break;
                    case "Breakthrough":
                    case "Main": Place(rect, .04f, actionBottom, .49f, actionTop); break;
                    case "Dismantle":
                    case "Variant": Place(rect, .51f, actionBottom, .96f, actionTop); break;
                    case "Read":
                    case "Fuse":
                    case "FuseMaterial":
                    case "Sell": Place(rect, .04f, actionBottom, .96f, actionTop); break;
                    case "CountBar": Place(rect, .04f, actionBottom + .02f, .49f, actionTop - .02f); break;
                    case "FuseMaterialHalf": Place(rect, .51f, actionBottom, .96f, actionTop); break;
                    case "Close": Place(rect, .25f, .025f, .75f, .17f); break;
                }
            }
        }
    }
}
