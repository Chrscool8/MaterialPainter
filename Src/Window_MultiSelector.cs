using Parkitect.UI;

using System;
using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaterialPainter2
{
    /// <summary>Right-click list of every part under the mouse, labelled "Owner > Part · Paint".</summary>
    public class Window_MultiSelector
    {
        private const float MIN_BUTTON_WIDTH = 220f;
        private const float BUTTON_HEIGHT = NativeUI.ROW_HEIGHT + 4f;
        private const float LABEL_PADDING = 24f;

        private readonly GameObject root;
        private readonly EscapeHierarchy.OnEscapeHandler escape_handler;
        private bool alive;

        public Window_MultiSelector(List<GameObject> objs, Vector2 click_position)
        {
            Canvas canvas = UIWindowsController.Instance.GetComponentInParent<Canvas>().rootCanvas;
            RectTransform layer = canvas.transform as RectTransform;

            // Covers the screen behind the list: a click anywhere else closes the list instead of painting.
            root = NativeUI.CreateLayoutObject("Material Painter Part Picker", layer).gameObject;
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;
            root.AddComponent<Image>().color = Color.clear;
            root.AddComponent<ClickCatcher>().onClick = close;
            root.transform.SetAsLastSibling();

            RectTransform list = NativeUI.CreateBox(root.transform, padding: 3, spacing: 2f);
            list.pivot = new Vector2(0f, 1f);
            list.anchorMin = list.anchorMax = new Vector2(0.5f, 0.5f);
            ContentSizeFitter fitter = list.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            float width = MIN_BUTTON_WIDTH;
            List<Button> buttons = new List<Button>();
            foreach (GameObject obj in objs)
            {
                GameObject target = obj;
                Button button = NativeUI.CreateButton(list, "", height: BUTTON_HEIGHT);
                button.onClick.AddListener(() => Safe.Run("part picker", () => clicked(target)));

                TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
                if (text != null)
                {
                    text.text = GetLabel(obj, text.font);
                    text.alignment = TextAlignmentOptions.MidlineLeft;
                    text.margin = new Vector4(6f, 0f, 6f, 0f);
                    // The button is sized to fit the whole label, so it never needs cutting off.
                    text.overflowMode = TextOverflowModes.Overflow;
                    width = Mathf.Max(width, text.GetPreferredValues(text.text).x + LABEL_PADDING);
                }
                buttons.Add(button);
            }

            foreach (Button button in buttons)
                NativeUI.SetSize(button, width: width, height: BUTTON_HEIGHT);

            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, click_position, camera, out Vector2 local);
            LayoutRebuilder.ForceRebuildLayoutImmediate(list);
            Rect bounds = layer.rect;
            local.x = Mathf.Clamp(local.x, bounds.xMin, Mathf.Max(bounds.xMin, bounds.xMax - list.rect.width));
            local.y = Mathf.Clamp(local.y, Mathf.Min(bounds.yMax, bounds.yMin + list.rect.height), bounds.yMax);
            list.localPosition = local;

            escape_handler = close;
            EscapeHierarchy.Instance.push(escape_handler);
            alive = true;
        }

        public void close()
        {
            if (!alive)
                return;

            alive = false;
            EscapeHierarchy.Instance.remove(escape_handler);
            if (root != null)
                UnityEngine.Object.Destroy(root);
            if (MP2_Controller.wms == this)
                MP2_Controller.wms = null;
        }

        private void clicked(GameObject target)
        {
            if (target != null)
                MP2.controller.OnObjectClicked(target);
            close();
        }

        private static string GetLabel(GameObject obj, TMP_FontAsset font)
        {
            // Only characters the game's own font has: one that comes from a fallback font cuts the label off there.
            const string partSeparator = " > ";
            string paintSeparator = font == null || font.HasCharacter('·') ? " · " : " - ";

            SerializedMonoBehaviour owner = obj.GetComponentInParent<SerializedMonoBehaviour>();
            // Some objects (walls, generated parts) have no display name; use their object name instead.
            string ownerName = owner != null ? owner.getName() : null;
            string label = !string.IsNullOrEmpty(ownerName) ? ownerName : TidyName((owner != null ? owner.gameObject : obj).name);

            if (owner != null && owner.gameObject != obj)
                label += partSeparator + TidyName(obj.name, PaintStore.GetPath(owner.transform, obj.transform));

            PaintMarker marker = obj.GetComponent<PaintMarker>();
            // "Overridden": the game swapped its own material back in (e.g. a path style change); the paint is still
            // saved and comes back on reload or repaint.
            if (marker != null && marker.brush != null)
                label += paintSeparator + GetPaintLabel(marker) + (marker.IsOverridden ? " (overridden)" : "");

            // Names come from saves and other mods; don't let them pass for markup.
            return "<noparse>" + label + "</noparse>";
        }

        // Custom media shows the file it was painted with, even if this player is seeing a fallback for it.
        private static string GetPaintLabel(PaintMarker marker)
        {
            string path;
            if (marker.brush is ImageBrush)
                path = MP2.GetCustomImageFilePath(marker.detail);
            else if (marker.brush is VideoBrush)
                path = MP2.GetCustomVideoFilePath(marker.detail);
            else
                return marker.brush.Name + (marker.brush is SharedMaterialBrush && marker.detail != "" ? " (tuned)" : "");

            string fileName = System.IO.Path.GetFileName(path);
            return System.IO.File.Exists(path) ? fileName : fileName + " (missing)";
        }

        // "Fence(Clone)" + path ".../Fence(Clone)#3" -> "Fence #3"
        private static string TidyName(string name, string path = null)
        {
            string tidy = name.Replace("(Clone)", "").Trim();
            if (path == null)
                return tidy;

            string segment = path.Substring(path.LastIndexOf('/') + 1);
            int mark = segment.LastIndexOf('#');
            return mark >= 0 && segment.Substring(0, mark) == name ? tidy + " " + segment.Substring(mark) : tidy;
        }

        /// <summary>Closes the list on a click outside it. Acts on release, so the click doesn't also reach the brush.</summary>
        private class ClickCatcher : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            public Action onClick;

            public void OnPointerDown(PointerEventData eventData) { }

            public void OnPointerUp(PointerEventData eventData) => onClick?.Invoke();
        }
    }
}
