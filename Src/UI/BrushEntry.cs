using Parkitect.UI;

using System;

using UnityEngine;
using UnityEngine.UI;

namespace MaterialPainter2
{
    /// <summary>
    /// One icon in a brush grid. Keeps its icon current (video thumbnails arrive after the window opens) and crosses
    /// itself out while its brush can't be used.
    /// </summary>
    public class BrushEntry : MonoBehaviour
    {
        private static readonly Color unavailableTint = new Color(1f, 1f, 1f, 0.45f);
        private static readonly Color crossColor = new Color(0.85f, 0.1f, 0.1f, 0.9f);

        public Toggle Toggle { get; private set; }
        public MaterialBrush Brush { get; private set; }

        /// <summary>The image or video the entry paints with; null for Elements brushes.</summary>
        public string Custom { get; private set; }

        private Image icon;
        private GameObject cross;
        private UITooltip tooltip;
        private Func<Sprite> sprite;
        private Func<string> unavailableReason;
        private string label;
        private string shownTooltip;

        public void Set(MaterialBrush brush, string custom, string label, Func<Sprite> sprite, Func<string> unavailableReason = null)
        {
            Brush = brush;
            Custom = custom;
            this.label = label;
            this.sprite = sprite;
            this.unavailableReason = unavailableReason;
            name = label;

            Toggle = GetComponent<Toggle>();
            Transform iconTransform = transform.Find("Icon");
            icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            tooltip = gameObject.AddComponent<UITooltip>();
            cross = CreateCross();
            Refresh();
        }

        private void Update() => Safe.Run("brush entry", Refresh);

        private void Refresh()
        {
            Sprite current = sprite?.Invoke();
            if (icon != null && icon.sprite != current)
                icon.sprite = current;

            string reason = unavailableReason?.Invoke();
            cross.SetActive(reason != null);
            if (icon != null)
                icon.color = reason != null ? unavailableTint : Color.white;

            string text = reason != null ? label + " - " + reason : label;
            if (text != shownTooltip)
            {
                tooltip.text = text;
                shownTooltip = text;
            }
        }

        private GameObject CreateCross()
        {
            RectTransform holder = NativeUI.CreateLayoutObject("Unavailable", transform);
            holder.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            holder.anchorMin = Vector2.zero;
            holder.anchorMax = Vector2.one;
            holder.sizeDelta = Vector2.zero;

            float length = NativeUI.GRID_CELL_SIZE * 1.1f;
            foreach (float angle in new[] { 45f, -45f })
            {
                RectTransform bar = NativeUI.CreateLayoutObject("Bar", holder);
                Image image = bar.gameObject.AddComponent<Image>();
                image.color = crossColor;
                image.raycastTarget = false;
                bar.sizeDelta = new Vector2(length, 2.5f);
                bar.localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            holder.gameObject.SetActive(false);
            return holder.gameObject;
        }
    }
}
