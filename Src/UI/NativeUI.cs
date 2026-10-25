using Parkitect.UI;

using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace MaterialPainter2
{
    /// <summary>
    /// Builds the mod's UI out of the game's own widgets. The window is a stripped copy of the game's Terraformer
    /// window (tab buttons, content panel, resizing); grids, sliders, checkboxes and buttons are copies of the game's.
    /// The copies are kept inactive under one holder and cloned whenever a window needs one.
    /// </summary>
    public static class NativeUI
    {
        public const float GRID_CELL_SIZE = 30f;
        public const float GRID_SPACING = 3f;
        public const float ROW_HEIGHT = 16f;
        public const float FONT_SIZE = 11f;
        public const float PIPETTE_SIZE = 24f;

        private static Transform widgets;
        private static GameObject scrollGridTemplate;
        private static GameObject entryTemplate;
        private static GameObject sliderTemplate;
        private static GameObject textTemplate;
        private static GameObject checkboxTemplate;
        private static GameObject buttonTemplate;
        private static GameObject pipetteTemplate;
        private static Sprite boxSprite;
        private static Color boxColor = Color.white;

        /// <summary>
        /// Makes the window template: the Terraformer window with its tools taken out and one empty panel per tab,
        /// named after the tab. Also makes the widget templates. Null when the game's window can't be found or has
        /// changed shape.
        /// </summary>
        public static GameObject CreateWindowTemplate(Transform menuContent, Transform parent, IList<(string title, string icon)> tabs)
        {
            TerrafomWindow source = FindTerraformerWindow(menuContent);
            if (source == null)
            {
                MP2.MPDebug("Native UI: the game's Terraformer window wasn't found.", always_show: true);
                return null;
            }

            // Copied under an inactive parent, so none of its scripts wake up before they're removed.
            GameObject window = Object.Instantiate(source.gameObject, parent, false);
            Transform tabBar = window.transform.Find("Tabs");
            Transform content = window.transform.Find("Content");
            TerrainTypeTool terrainTool = window.GetComponentInChildren<TerrainTypeTool>(true);
            Transform scrollView = terrainTool != null ? terrainTool.transform.Find("Scroll View") : null;
            Transform settings = content != null ? content.Find("TerraformerTabPanel/TerraformSettings") : null;
            Transform slider = settings != null ? settings.Find("ToolSizeSlider") : null;
            Transform sliderText = slider != null ? slider.Find("ToolSizeText") : null;

            if (tabBar == null || content == null || terrainTool == null || terrainTool.terrainTypeEntryGO == null || scrollView == null || slider == null || sliderText == null)
            {
                MP2.MPDebug("Native UI: the game's Terraformer window has changed; can't build the window from it.", always_show: true);
                Object.DestroyImmediate(window);
                return null;
            }

            widgets = new GameObject("Material Painter Widgets", typeof(RectTransform)).transform;
            widgets.SetParent(parent, false);

            Image settingsImage = settings.GetComponent<Image>();
            if (settingsImage != null)
            {
                boxSprite = settingsImage.sprite;
                boxColor = settingsImage.color;
            }

            textTemplate = Keep(sliderText);
            sliderTemplate = Keep(slider);
            scrollGridTemplate = Keep(scrollView);
            entryTemplate = MakeEntryTemplate(terrainTool.terrainTypeEntryGO.gameObject);
            checkboxTemplate = MakeCheckboxTemplate();
            buttonTemplate = MakeButtonTemplate();
            pipetteTemplate = MakePipetteTemplate(menuContent);

            if (!AreTemplatesComplete)
            {
                MP2.MPDebug("Native UI: some of the game's widgets weren't found; can't build the window.", always_show: true);
                Object.DestroyImmediate(window);
                Object.DestroyImmediate(widgets.gameObject);
                widgets = null;
                return null;
            }

            PrepareText(textTemplate.GetComponent<TextMeshProUGUI>());
            sliderTemplate.GetComponent<Slider>().onValueChanged = new Slider.SliderEvent();

            for (int i = content.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(content.GetChild(i).gameObject);

            foreach (TerrafomWindow controller in window.GetComponents<TerrafomWindow>())
                Object.DestroyImmediate(controller);
            foreach (UIWindow oldWindow in window.GetComponents<UIWindow>())
                Object.DestroyImmediate(oldWindow);

            SetUpTabs(tabBar, content, tabs);
            return window;
        }

        private static TerrafomWindow FindTerraformerWindow(Transform menuContent)
        {
            if (menuContent != null)
            {
                foreach (UIMenuWindowButton button in menuContent.GetComponentsInChildren<UIMenuWindowButton>(true))
                {
                    TerrafomWindow window = button.windowContentGO != null ? button.windowContentGO.GetComponent<TerrafomWindow>() : null;
                    if (window != null)
                        return window;
                }
            }

            // Not in the bottom menu (some game modes): the prefab is loaded anyway.
            foreach (TerrafomWindow window in Resources.FindObjectsOfTypeAll<TerrafomWindow>())
            {
                if (!window.gameObject.scene.IsValid())
                    return window;
            }

            return null;
        }

        private static void SetUpTabs(Transform tabBar, Transform content, IList<(string title, string icon)> tabs)
        {
            List<Button> buttons = new List<Button>(tabBar.GetComponentsInChildren<Button>(true));
            while (buttons.Count > tabs.Count)
            {
                Object.DestroyImmediate(buttons[buttons.Count - 1].gameObject);
                buttons.RemoveAt(buttons.Count - 1);
            }
            while (buttons.Count < tabs.Count)
                buttons.Add(Object.Instantiate(buttons[0].gameObject, tabBar, false).GetComponent<Button>());

            for (int i = 0; i < tabs.Count; i++)
            {
                Button button = buttons[i];
                button.gameObject.SetActive(true);
                button.onClick = new Button.ButtonClickedEvent();

                Image icon = button.transform.Find("Image")?.GetComponent<Image>();
                if (icon != null)
                {
                    icon.sprite = MP2.get_sprite(tabs[i].icon, MP2.get_sprite("icon_question"));
                    icon.preserveAspect = true;
                    icon.rectTransform.sizeDelta = new Vector2(26f, 26f);
                }

                // The tab group pairs buttons with panels by order and names each button's tooltip after its panel.
                RectTransform panel = CreateLayoutObject(tabs[i].title, content);
                VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 4f;
                SetChildControl(layout, expandWidth: true);
                LayoutElement element = panel.gameObject.AddComponent<LayoutElement>();
                element.flexibleWidth = 1f;
                element.flexibleHeight = 1f;
                panel.gameObject.AddComponent<UITabSettings>().title = tabs[i].title;
            }
        }

        private static GameObject Keep(Transform original)
        {
            original.SetParent(widgets, false);
            foreach (I18NText translator in original.GetComponentsInChildren<I18NText>(true))
                Object.DestroyImmediate(translator);
            return original.gameObject;
        }

        // The terrain type entry, with its icon swapped for an Image so our sprites keep their aspect.
        private static GameObject MakeEntryTemplate(GameObject source)
        {
            GameObject entry = Object.Instantiate(source, widgets, false);
            entry.name = "Entry";
            foreach (TerrainTypeEntry terrainEntry in entry.GetComponents<TerrainTypeEntry>())
                Object.DestroyImmediate(terrainEntry);

            Toggle toggle = entry.GetComponent<Toggle>();
            toggle.onValueChanged = new Toggle.ToggleEvent();
            toggle.isOn = false;

            RawImage rawImage = entry.GetComponentInChildren<RawImage>(true);
            if (rawImage != null)
            {
                GameObject iconObject = rawImage.gameObject;
                Object.DestroyImmediate(rawImage);
                Image icon = iconObject.AddComponent<Image>();
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                iconObject.name = "Icon";
            }

            return entry;
        }

        private static GameObject MakeCheckboxTemplate()
        {
            AttractionSettingToggleUI source = ScriptableSingleton<UIAssetManager>.Instance.attractionSettingToggleUIGO;
            if (source == null)
                return null;

            GameObject checkbox = Keep(Object.Instantiate(source.gameObject, widgets, false).transform);
            foreach (AttractionSettingToggleUI setting in checkbox.GetComponents<AttractionSettingToggleUI>())
                Object.DestroyImmediate(setting);

            Toggle toggle = checkbox.GetComponentInChildren<Toggle>(true);
            toggle.onValueChanged = new Toggle.ToggleEvent();
            LayoutElement element = toggle.GetComponent<LayoutElement>();
            if (element != null)
            {
                element.preferredWidth = -1f;
                element.flexibleWidth = 1f;
                // Stretching tall pins the box to the top and leaves the label floating in the middle.
                element.flexibleHeight = 0f;
            }

            PrepareText(checkbox.GetComponentInChildren<TextMeshProUGUI>(true));
            return checkbox;
        }

        private static GameObject MakeButtonTemplate()
        {
            ConfirmationWindow source = ScriptableSingleton<UIAssetManager>.Instance.confirmationWindowGO;
            Transform okButton = source != null ? source.transform.Find("Options/OKButton") : null;
            if (okButton == null)
                return null;

            GameObject button = Keep(Object.Instantiate(okButton.gameObject, widgets, false).transform);
            button.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
            foreach (LayoutElement element in button.GetComponents<LayoutElement>())
                Object.DestroyImmediate(element);

            PrepareText(button.GetComponentInChildren<TextMeshProUGUI>(true));
            return button;
        }

        // The pipette button from the game's Painter window. Optional: without it the window just has no eyedropper.
        private static GameObject MakePipetteTemplate(Transform menuContent)
        {
            ColorBrushToolWindow painter = null;
            if (menuContent != null)
            {
                foreach (UIMenuWindowButton button in menuContent.GetComponentsInChildren<UIMenuWindowButton>(true))
                {
                    painter = button.windowContentGO != null ? button.windowContentGO.GetComponent<ColorBrushToolWindow>() : null;
                    if (painter != null)
                        break;
                }
            }

            Transform source = painter != null ? painter.transform.Find("PipetteButton") : null;
            if (source == null || source.GetComponent<Button>() == null)
            {
                MP2.MPDebug("Native UI: the game's pipette button wasn't found; no eyedropper.", always_show: true);
                return null;
            }

            GameObject pipette = Keep(Object.Instantiate(source.gameObject, widgets, false).transform);
            pipette.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
            foreach (MonoBehaviour behaviour in pipette.GetComponentsInChildren<MonoBehaviour>(true))
            {
                // Anything of the game's beyond the button's own graphics would act on the game's painter.
                if (!(behaviour is Graphic) && !(behaviour is Selectable) && !(behaviour is UITooltip))
                    Object.DestroyImmediate(behaviour);
            }
            return pipette;
        }

        private static void PrepareText(TextMeshProUGUI text)
        {
            if (text == null)
                return;

            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
        }

        // --- Pieces ---

        public static RectTransform CreateLayoutObject(string name, Transform parent)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        public static void SetChildControl(HorizontalOrVerticalLayoutGroup layout, bool expandWidth = false, bool expandHeight = false)
        {
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = expandWidth;
            layout.childForceExpandHeight = expandHeight;
        }

        public static LayoutElement SetSize(Component component, float width = -1f, float height = -1f, float flexibleWidth = -1f, float flexibleHeight = -1f)
        {
            LayoutElement element = GetOrAdd<LayoutElement>(component.gameObject);
            element.minWidth = width;
            element.preferredWidth = width;
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
            return element;
        }

        public static void SetTooltip(GameObject target, string text)
        {
            UITooltip tooltip = GetOrAdd<UITooltip>(target);
            tooltip.text = text ?? "";
        }

        /// <summary>A row laying its children out left to right, vertically centered.</summary>
        public static RectTransform CreateRow(Transform parent, float height = ROW_HEIGHT, float spacing = 4f)
        {
            RectTransform row = CreateLayoutObject("Row", parent);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            SetChildControl(layout);
            SetSize(row, height: height);
            return row;
        }

        /// <summary>A recessed box, like the one around the Terraformer's settings, stacking its children.</summary>
        public static RectTransform CreateBox(Transform parent, int padding = 6, float spacing = 2f)
        {
            RectTransform box = CreateLayoutObject("Box", parent);
            Image background = box.gameObject.AddComponent<Image>();
            background.sprite = boxSprite;
            background.color = boxColor;
            background.type = Image.Type.Sliced;

            VerticalLayoutGroup layout = box.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            SetChildControl(layout, expandWidth: true);
            return box;
        }

        /// <summary>A scrolling grid of icons, filling the space it's given. Returns the grid entries go in.</summary>
        public static RectTransform CreateScrollGrid(Transform parent, float minHeight)
        {
            GameObject scrollView = Object.Instantiate(scrollGridTemplate, parent, false);
            LayoutElement element = GetOrAdd<LayoutElement>(scrollView);
            element.minHeight = minHeight;
            element.flexibleWidth = 1f;
            element.flexibleHeight = 1f;

            ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
            RectTransform grid = scrollRect.content;
            GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(GRID_CELL_SIZE, GRID_CELL_SIZE);
            layout.spacing = new Vector2(GRID_SPACING, GRID_SPACING);
            return grid;
        }

        public static BrushEntry CreateEntry(Transform grid)
        {
            GameObject entry = Object.Instantiate(entryTemplate, grid, false);
            return entry.AddComponent<BrushEntry>();
        }

        public static TextMeshProUGUI CreateText(Transform parent, string text, float width = -1f, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft, bool bold = false)
        {
            TextMeshProUGUI label = Object.Instantiate(textTemplate, parent, false).GetComponent<TextMeshProUGUI>();
            label.name = "Text";
            label.text = text;
            label.fontSize = FONT_SIZE;
            label.alignment = alignment;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            SetSize(label, width: width, height: ROW_HEIGHT, flexibleWidth: width < 0f ? 1f : -1f);
            return label;
        }

        public static Slider CreateSlider(Transform parent)
        {
            Slider slider = Object.Instantiate(sliderTemplate, parent, false).GetComponent<Slider>();
            slider.wholeNumbers = false;
            LayoutElement element = SetSize(slider, height: 12.7f, flexibleWidth: 1f);
            element.minWidth = 40f;
            element.preferredWidth = 80f;
            return slider;
        }

        /// <summary>The game's checkbox, labelled.</summary>
        public static Toggle CreateCheckbox(Transform parent, string label, string tooltip)
        {
            GameObject checkbox = Object.Instantiate(checkboxTemplate, parent, false);
            checkbox.name = label;
            SetSize(checkbox.transform, height: ROW_HEIGHT + 2f, flexibleWidth: 1f, flexibleHeight: 0f);

            TextMeshProUGUI text = checkbox.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = label;
                text.fontSize = FONT_SIZE;
            }

            Toggle toggle = checkbox.GetComponentInChildren<Toggle>(true);
            SetTooltip(toggle.gameObject, tooltip);
            return toggle;
        }

        public static Button CreateButton(Transform parent, string label, string tooltip = null, float width = -1f, float height = ROW_HEIGHT + 2f)
        {
            Button button = Object.Instantiate(buttonTemplate, parent, false).GetComponent<Button>();
            button.name = label;
            SetSize(button, width: width, height: height, flexibleWidth: width < 0f ? 1f : -1f);

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = label;
                text.fontSize = FONT_SIZE;
            }

            if (!string.IsNullOrEmpty(tooltip))
                SetTooltip(button.gameObject, tooltip);
            return button;
        }

        /// <summary>The game's pipette button, or null if it couldn't be found.</summary>
        public static Button CreatePipetteButton(Transform parent, string tooltip)
        {
            if (pipetteTemplate == null)
                return null;

            Button button = Object.Instantiate(pipetteTemplate, parent, false).GetComponent<Button>();
            SetSize(button, width: PIPETTE_SIZE, height: PIPETTE_SIZE);
            SetTooltip(button.gameObject, tooltip);
            return button;
        }

        public static Button CreateIconButton(Transform parent, string sprite, float size)
        {
            RectTransform rect = CreateLayoutObject(sprite, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = MP2.get_sprite(sprite, MP2.get_sprite("icon_question"));
            image.preserveAspect = true;
            SetSize(rect, width: size, height: size);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static bool AreTemplatesComplete => scrollGridTemplate != null && entryTemplate != null && sliderTemplate != null
            && textTemplate != null && checkboxTemplate != null && buttonTemplate != null;
    }
}
