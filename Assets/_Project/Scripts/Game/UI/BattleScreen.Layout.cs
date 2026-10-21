using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    public partial class BattleScreen
    {
        /// <summary>Shared reward content stays intact; summary and detail occupy separate landscape columns.</summary>
        private void LayoutRewardCard(RectTransform card)
        {
            int valueIndex = 0;
            foreach (Transform child in card)
            {
                var rect = child as RectTransform;
                switch (child.name)
                {
                    case "Stars": PlaceHud(rect, .035f, .45f, .405f, .82f); break;
                    case "RewardValue":
                        float left = valueIndex++ == 0 ? .035f : .225f;
                        PlaceHud(rect, left, .23f, left + .175f, .43f);
                        break;
                    case "LootTitle":
                    case "SummaryTitle":
                    case "PartyTitle": PlaceHud(rect, .435f, .79f, .975f, .84f); break;
                    case "Viewport": PlaceHud(rect, .435f, .17f, .975f, .79f); break;
                    case "GrowthSummary": PlaceHud(rect, .035f, .45f, .405f, .82f); break;
                    case "Progress": PlaceHud(rect, .035f, .57f, .405f, .82f); break;
                    case "NextFight":
                        if (child.GetComponent<Text>() != null) PlaceHud(rect, .035f, .43f, .405f, .56f);
                        break;
                    case "SaveHint": PlaceHud(rect, .035f, .17f, .405f, .42f); break;
                }
            }
            Canvas.ForceUpdateCanvases();
            var list = card.Find("Viewport/Content");
            if (list != null)
            {
                var layout = list.GetComponent<ReadingList>();
                if (layout != null) layout.Reflow();
            }
        }
    }
}
