using Parkitect.UI;

using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace MaterialPainter2
{
    /// <summary>
    /// Sliders under the Elements grid for the selected brush's material settings. They set what the next paint gets;
    /// parts already painted keep the settings they were painted with.
    /// </summary>
    public class TuningPanel
    {
        private const float LABEL_WIDTH = 64f;
        private const float VALUE_WIDTH = 30f;
        private const float RESET_SIZE = 13f;

        private readonly RectTransform box;
        private readonly TextMeshProUGUI title;
        private readonly List<Row> rows = new List<Row>();
        private SharedMaterialBrush shownBrush;

        private class Row
        {
            public GameObject root;
            public Slider slider;
            public TextMeshProUGUI value;
            public Button reset;
            public Image resetImage;
            public UITooltip resetTooltip;
        }

        public TuningPanel(Transform parent)
        {
            box = NativeUI.CreateBox(parent);
            box.name = "Tuning";

            RectTransform header = NativeUI.CreateRow(box, NativeUI.ROW_HEIGHT + 2f);
            title = NativeUI.CreateText(header, "", bold: true);
            Button resetAll = NativeUI.CreateButton(header, "Reset", "Back to this brush's starting look.", width: 50f);
            resetAll.onClick.AddListener(() => Safe.Run("tuning reset", () =>
            {
                if (shownBrush != null)
                    SetSettings(shownBrush, shownBrush.Preset);
            }));

            box.gameObject.SetActive(false);
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

        /// <summary>Shows the selected brush's sliders, or hides the panel. True when what's shown changed size.</summary>
        public bool Refresh()
        {
            SharedMaterialBrush brush = SelectedBrush;
            if (brush == shownBrush)
            {
                UpdateValues();
                return false;
            }

            shownBrush = brush;
            foreach (Row row in rows)
                Object.Destroy(row.root);
            rows.Clear();

            box.gameObject.SetActive(brush != null);
            if (brush == null)
                return true;

            title.text = brush.Name;
            IReadOnlyList<BrushParam> parameters = brush.Params;
            for (int i = 0; i < parameters.Count; i++)
                rows.Add(CreateRow(parameters[i], i));

            UpdateValues();
            return true;
        }

        private Row CreateRow(BrushParam param, int index)
        {
            Row row = new Row { root = NativeUI.CreateRow(box).gameObject };
            NativeUI.SetTooltip(row.root, param.Tooltip);
            // Rows only catch the pointer (for the tooltip) when something in them can.
            row.root.AddComponent<Image>().color = Color.clear;

            NativeUI.CreateText(row.root.transform, param.Label, LABEL_WIDTH);

            row.slider = NativeUI.CreateSlider(row.root.transform);
            row.slider.minValue = param.Min;
            row.slider.maxValue = param.Max;
            row.slider.onValueChanged.AddListener(value => Safe.Run("tuning slider", () => OnSliderChanged(index, value)));

            row.value = NativeUI.CreateText(row.root.transform, "", VALUE_WIDTH, TextAlignmentOptions.MidlineRight);

            row.reset = NativeUI.CreateIconButton(row.root.transform, "icon_reset", RESET_SIZE);
            row.resetImage = row.reset.GetComponent<Image>();
            row.resetTooltip = row.reset.gameObject.AddComponent<UITooltip>();
            row.reset.onClick.AddListener(() => Safe.Run("tuning slider reset", () =>
            {
                if (shownBrush != null)
                    OnSliderChanged(index, shownBrush.Preset[index]);
            }));

            return row;
        }

        private void OnSliderChanged(int index, float value)
        {
            if (shownBrush == null)
                return;

            IReadOnlyList<BrushParam> parameters = shownBrush.Params;
            if (parameters[index].Normalize(value) != shownBrush.Current[index])
                SetSettings(shownBrush, shownBrush.Current.With(parameters, index, value));
            else
                UpdateValues();
        }

        private void UpdateValues()
        {
            if (shownBrush == null)
                return;

            IReadOnlyList<BrushParam> parameters = shownBrush.Params;
            BrushSettings settings = shownBrush.Current;
            BrushSettings preset = shownBrush.Preset;
            for (int i = 0; i < rows.Count && i < parameters.Count; i++)
            {
                Row row = rows[i];
                // Snaps the handle to the slider's step, without calling back into here.
                if (row.slider.value != settings[i])
                    row.slider.SetValueWithoutNotify(settings[i]);
                row.value.text = parameters[i].Format(settings[i]);

                bool changed = settings[i] != preset[i];
                row.reset.interactable = changed;
                row.resetImage.color = new Color(1f, 1f, 1f, changed ? 1f : 0.25f);
                row.resetTooltip.text = "Back to " + parameters[i].Format(preset[i]) + ".";
            }
        }

        private void SetSettings(SharedMaterialBrush brush, BrushSettings settings)
        {
            brush.Current = settings;
            if (BrushRegistry.Get((MaterialBrush)MP2.selected_brush) == brush)
                MP2.selected_brush_custom = brush.CurrentDetail;
            UpdateValues();
        }
    }
}
