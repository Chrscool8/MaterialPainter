using System.Collections.Generic;

using UnityEngine;

namespace MaterialPainter2
{
    /// <summary>Right-click list of every part under the mouse, labelled "Owner › Part · Paint".</summary>
    public class Window_MultiSelector
    {
        private const float MIN_BUTTON_WIDTH = 300f;
        private const float BUTTON_HEIGHT = 30f;
        private const float LABEL_PADDING = 24f;

        private readonly Dictionary<UI_PushButton, GameObject> buttons = new Dictionary<UI_PushButton, GameObject>();
        private readonly Vector2 click_position;
        private readonly EscapeHierarchy.OnEscapeHandler escape_handler;
        private float button_width = -1f;
        private bool alive = false;

        public Window_MultiSelector(List<GameObject> objs, Vector2 click_position)
        {
            this.click_position = click_position;

            foreach (GameObject obj in objs)
            {
                UI_PushButton button = new UI_PushButton(text: GetLabel(obj));
                button.on_mouse_click = () => clicked(button);
                buttons.Add(button, obj);
            }

            escape_handler = close;
            EscapeHierarchy.Instance.push(escape_handler);
            alive = true;
        }

        public void OnGui()
        {
            if (!alive)
                return;

            float dpi = MP2.get_dpi();
            if (button_width < 0f)
                button_width = MeasureButtonWidth(dpi);

            int index = 0;
            foreach (KeyValuePair<UI_PushButton, GameObject> entry in buttons)
            {
                if (!alive)
                    return;

                UI_PushButton button = entry.Key;
                button.SetSize(button_width, BUTTON_HEIGHT * dpi);
                button.SetPosition(click_position.x, click_position.y + index * button.size.y);
                index++;

                if (entry.Value != null)
                    button.DrawButton();
            }
        }

        public void close()
        {
            alive = false;
            EscapeHierarchy.Instance.remove(escape_handler);
            if (MP2_Controller.wms == this)
                MP2_Controller.wms = null;
        }

        private void clicked(UI_PushButton button)
        {
            if (buttons[button] != null)
                MP2.controller.OnObjectClicked(buttons[button]);
            close();
        }

        private float MeasureButtonWidth(float dpi)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(14 * dpi) };
            float width = MIN_BUTTON_WIDTH * dpi;
            foreach (UI_PushButton button in buttons.Keys)
                width = Mathf.Max(width, style.CalcSize(new GUIContent(button.text)).x + LABEL_PADDING * dpi);
            return width;
        }

        private static string GetLabel(GameObject obj)
        {
            SerializedMonoBehaviour owner = obj.GetComponentInParent<SerializedMonoBehaviour>();
            // Some objects (walls, generated parts) have no display name; use their object name instead.
            string ownerName = owner != null ? owner.getName() : null;
            string label = !string.IsNullOrEmpty(ownerName) ? ownerName : TidyName((owner != null ? owner.gameObject : obj).name);

            if (owner != null && owner.gameObject != obj)
                label += " › " + TidyName(obj.name, PaintStore.GetPath(owner.transform, obj.transform));

            PaintMarker marker = obj.GetComponent<PaintMarker>();
            // "Overridden": the game swapped its own material back in (e.g. a path style change); the paint is still
            // saved and comes back on reload or repaint.
            if (marker != null && marker.brush != null)
                label += " · " + GetPaintLabel(marker) + (marker.IsOverridden ? " (overridden)" : "");

            return label;
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
    }
}
