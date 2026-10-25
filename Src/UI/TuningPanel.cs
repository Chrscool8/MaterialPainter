using System.Collections.Generic;

using UnityEngine;

namespace MaterialPainter2
{
    /// <summary>
    /// Sliders under the Elements grid for the selected brush's material settings. They set what the next paint gets;
    /// parts already painted keep the settings they were painted with.
    /// </summary>
    public class UI_TuningPanel : UI_Item
    {
        private const float PADDING = 8f;
        private const float TITLE_HEIGHT = 22f;
        private const float ROW_HEIGHT = 24f;
        private const float BUTTON_HEIGHT = 24f;
        private const float BUTTON_GAP = 4f;
        private const float LABEL_WIDTH = 84f;
        private const float VALUE_WIDTH = 40f;
        private const float RESET_SIZE = 16f;

        private static readonly Color outer_color = new Color(218f / 255f, 218f / 255f, 224f / 255f);
        private static readonly Color inner_color = new Color(170f / 255f, 170f / 255f, 170f / 255f);

        private readonly UI_PushButton button_reset;
        private Vector2 position;
        private float width = 100f;

        public UI_TuningPanel(GameObject parent)
        {
            this.parent = parent;
            button_reset = new UI_PushButton(parent: parent, text: "Reset", tooltip_text: "Back to this brush's starting look.", onMouseClick: () =>
            {
                SharedMaterialBrush brush = SelectedBrush;
                if (brush != null)
                    SetSettings(brush, brush.Preset);
            });
        }

        /// <summary>The selected brush, if it has sliders and its material can be made.</summary>
        public static SharedMaterialBrush SelectedBrush
        {
            get
            {
                SharedMaterialBrush brush = BrushRegistry.Get((MaterialBrush)MP2.selected_brush) as SharedMaterialBrush;
                return brush != null && brush.IsTunable && brush.IsAvailable && brush.Preset != null ? brush : null;
            }
        }

        /// <summary>Unscaled height the window makes room for; 0 when nothing is selected that has sliders.</summary>
        public static float GetHeight()
        {
            SharedMaterialBrush brush = SelectedBrush;
            return brush == null ? 0f : PADDING + TITLE_HEIGHT + brush.Params.Count * ROW_HEIGHT + BUTTON_GAP + BUTTON_HEIGHT + PADDING;
        }

        /// <summary>Position of the top-left corner, in the bottom-up coordinates the other UI items use.</summary>
        public void SetPosition(float x, float y)
        {
            position = new Vector2(x, y);
        }

        public void SetWidth(float width)
        {
            this.width = width;
        }

        public void Draw(float scale)
        {
            SharedMaterialBrush brush = SelectedBrush;
            if (!visible || brush == null)
                return;

            float top = Screen.height - position.y;
            Rect area = new Rect(position.x, top, width * scale, GetHeight() * scale);
            DrawRect(area, outer_color);
            DrawRect(new Rect(area.x + 4f * scale, area.y + 4f * scale, area.width - 8f * scale, area.height - 8f * scale), inner_color);

            float x = area.x + PADDING * scale;
            float innerWidth = area.width - 2f * PADDING * scale;
            float y = area.y + PADDING * scale;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(15 * scale), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            DrawLabel(new Rect(x, y, innerWidth, TITLE_HEIGHT * scale), brush.Name, titleStyle);
            y += TITLE_HEIGHT * scale;

            IReadOnlyList<BrushParam> parameters = brush.Params;
            BrushSettings settings = brush.Current;
            for (int i = 0; i < parameters.Count; i++)
            {
                float value = DrawRow(ref y, x, innerWidth, scale, parameters[i], settings[i], brush.Preset[i]);
                if (parameters[i].Normalize(value) != settings[i])
                    settings = settings.With(parameters, i, value);
            }
            if (!settings.Equals(brush.Current))
                SetSettings(brush, settings);

            y += BUTTON_GAP * scale;
            button_reset.SetSize(innerWidth * 0.4f, BUTTON_HEIGHT * scale);
            button_reset.SetPosition(x + innerWidth - button_reset.size.x, Screen.height - y);
            button_reset.DrawButton();
        }

        private float DrawRow(ref float y, float x, float innerWidth, float scale, BrushParam param, float value, float presetValue)
        {
            Rect row = new Rect(x, y, innerWidth, ROW_HEIGHT * scale);
            y += row.height;

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(14 * scale), alignment = TextAnchor.MiddleLeft };
            float labelWidth = LABEL_WIDTH * scale;
            float valueWidth = VALUE_WIDTH * scale;
            float resetSize = RESET_SIZE * scale;
            Rect reset = new Rect(row.xMax - resetSize, row.y + (row.height - resetSize) / 2f, resetSize, resetSize);
            DrawLabel(new Rect(row.x, row.y, labelWidth, row.height), param.Label, labelStyle);
            DrawLabel(new Rect(reset.x - 4f * scale - valueWidth, row.y, valueWidth, row.height), param.Format(value), labelStyle);

            // Sliders sit in the middle of their row.
            Rect slider = new Rect(row.x + labelWidth, row.y + (row.height - 12f) / 2f, row.width - labelWidth - valueWidth - resetSize - 10f * scale, 12f);
            float result = GUI.HorizontalSlider(slider, value, param.Min, param.Max);

            bool changed = value != presetValue;
            if (row.Contains(Event.current.mousePosition))
                setToolTip(changed && reset.Contains(Event.current.mousePosition) ? "Back to " + param.Format(presetValue) + "." : param.Tooltip);

            return DrawResetButton(reset, changed) ? presetValue : result;
        }

        /// <summary>The little per-slider reset arrow; faded while the slider is already at its starting value. True when clicked.</summary>
        private static bool DrawResetButton(Rect rect, bool enabled)
        {
            bool hovered = enabled && rect.Contains(Event.current.mousePosition);
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, enabled ? 1f : 0.25f);
            if (hovered)
                GUI.DrawTexture(rect, MP2.get_sprite("icon_highlight").texture, ScaleMode.StretchToFill);
            GUI.DrawTexture(rect, MP2.get_sprite("icon_reset").texture, ScaleMode.ScaleToFit);
            GUI.color = previous;

            if (!hovered || !MP2.IsCoolDownReady() || !Input.GetMouseButtonUp(0))
                return false;

            MP2.ResetCountdown();
            return true;
        }

        private static void SetSettings(SharedMaterialBrush brush, BrushSettings settings)
        {
            brush.Current = settings;
            if (BrushRegistry.Get((MaterialBrush)MP2.selected_brush) == brush)
                MP2.selected_brush_custom = brush.CurrentDetail;
        }

        private static void DrawLabel(Rect rect, string text, GUIStyle style)
        {
            GUI.color = Color.black;
            GUI.Label(rect, text, style);
            GUI.color = Color.white;
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        public override void ShowContent()
        {
            visible = true;
        }

        public override void HideContent()
        {
            visible = false;
        }
    }
}
