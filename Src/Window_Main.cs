using Parkitect.UI;

using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

namespace MaterialPainter2
{
    /// <summary>
    /// The Material Painter window: the game's tabbed tool window (see <see cref="NativeUI"/>) with a brush grid on
    /// the Elements, Images and Videos tabs and the mod's options on the Settings tab.
    /// </summary>
    public class Window_Main : UIWindow
    {
        /// <summary>Smallest size of the window's content; the player can make it bigger.</summary>
        public static readonly Vector2 MINIMUM_SIZE = new Vector2(230f, 220f);
        private const float MINIMUM_GRID_HEIGHT = 70f;

        public static readonly (string title, string icon)[] TABS =
        {
            ("Elements", "icon_elements"),
            ("Images", "icon_camera"),
            ("Videos", "icon_videocamera"),
            ("Settings", "icon_toolbox"),
        };

        public static Window_Main Instance { get; private set; }

        private readonly List<BrushEntry> entries = new List<BrushEntry>();
        private readonly List<BrushEntry> wildEntries = new List<BrushEntry>();
        private readonly List<RectTransform> panels = new List<RectTransform>();
        private RectTransform tabBar;
        private RectTransform content;
        private UITabGroup tabGroup;
        private readonly List<Button> pipetteButtons = new List<Button>();
        private TuningPanel tuning;
        private Toggle goWild;
        private Button convertButton;
        private bool pipeActivated;

        private const string PIPETTE_TOOLTIP = "Pick up a paint: click this, then a painted part. Its brush and sliders get selected. Also the game's Pipette key.";
        private static readonly Color pipetteActiveColor = new Color(0.45f, 0.7f, 0.95f, 1f);
        private Color pipetteColor = Color.white;

        /// <summary>A media tab's grid; rebuilt by its Refresh button.</summary>
        private class MediaTab
        {
            public MaterialBrush brush;
            public RectTransform grid;
            public System.Func<IEnumerable<MaterialType>> media;
            public System.Func<MaterialType, string> label;
            public readonly List<BrushEntry> entries = new List<BrushEntry>();
        }

        /// <summary>Makes the window that the bottom menu button opens a copy of. Null if the game's UI has changed.</summary>
        public static Window_Main CreateTemplate(Transform menuContent, Transform parent)
        {
            GameObject window = NativeUI.CreateWindowTemplate(menuContent, parent, TABS);
            if (window == null)
                return null;

            window.name = MP2.Instance.getName();
            window.GetComponent<RectTransform>().sizeDelta = MINIMUM_SIZE;

            UIWindowSettings settings = window.GetComponent<UIWindowSettings>();
            if (settings == null)
                settings = window.AddComponent<UIWindowSettings>();
            settings.title = MP2.MOD_DISPLAY_NAME;
            settings.uniqueTag = MP2.Instance.getName();
            settings.uniqueTagString = MP2.Instance.getName();
            settings.closable = true;
            settings.pinnable = true;
            settings.destroyWhenClosed = true;
            settings.mask = 0;
            settings.isBuilderWindow = false;
            settings.contentDeterminesWindowSize = false;
            settings.resizeability = UIWindowSettings.Resizeability.Horizontal | UIWindowSettings.Resizeability.Vertical;
            settings.spawnLocation = UIWindowSettings.SpawnLocation.BottomMenu;

            return window.AddComponent<Window_Main>();
        }

        protected override void Awake()
        {
            base.Awake();
            Safe.Run("window setup", Build);
        }

        private void Build()
        {
            tabBar = transform.Find("Tabs") as RectTransform;
            content = transform.Find("Content") as RectTransform;
            foreach (var tab in TABS)
                panels.Add(content.Find(tab.title) as RectTransform);

            BuildElementsTab(panels[0]);
            BuildMediaTab(panels[1], new MediaTab { brush = MaterialBrush.Image, media = () => MP2.material_brushes_images.Values, label = mt => mt.id_string });
            BuildMediaTab(panels[2], new MediaTab { brush = MaterialBrush.Video, media = () => MP2.material_brushes_videos.Values, label = mt => mt.name });
            BuildSettingsTab(panels[3]);

            foreach (BrushEntry entry in wildEntries)
                entry.gameObject.SetActive(MP2._setting_go_wild);

            // Picks up where the player left off, unless that brush is gone (a deleted image, Go Wild turned off).
            BrushEntry remembered = entries.Find(entry => entry.gameObject.activeSelf
                && (int)entry.Brush == MP2.last_brush && (entry.Custom == null || entry.Custom == MP2.last_brush_custom));
            if (remembered != null)
                Select(remembered.Brush, remembered.Custom);
            else
                Select(MaterialBrush.None, "");

            tabGroup = GetComponent<UITabGroup>();
            if (tabGroup != null)
                tabGroup.OnTabChanged += OnTabChanged;
        }

        // After every Awake, so the tab group is ready: opens the tab the selected brush is on.
        protected override void Start()
        {
            base.Start();
            Safe.Run("opening brush tab", OpenSelectedTab);
        }

        private void OpenSelectedTab()
        {
            if (tabGroup == null || MP2.selected_brush == (int)MaterialBrush.None)
                return;

            int tab = panels.FindIndex(panel => panel != null && panel.GetComponentsInChildren<BrushEntry>(true)
                .Any(entry => entry.gameObject.activeSelf && IsSelected(entry)));
            if (tab >= 0 && tab != tabGroup.getTabIndex())
                tabGroup.openTab(tab);
        }

        /// <summary>The row of tool buttons above a brush grid: the eyedropper, then whatever else the tab adds.</summary>
        private RectTransform CreateToolbar(RectTransform panel)
        {
            RectTransform toolbar = NativeUI.CreateRow(panel, NativeUI.PIPETTE_SIZE);
            Button pipette = NativeUI.CreatePipetteButton(toolbar, PIPETTE_TOOLTIP);
            if (pipette != null)
            {
                // The game's gray; only tinted while picking.
                if (pipette.targetGraphic != null)
                    pipetteColor = pipette.targetGraphic.color;
                pipette.onClick.AddListener(() => Safe.Run("eyedropper", TogglePicking));
                pipetteButtons.Add(pipette);
            }
            return toolbar;
        }

        private void BuildElementsTab(RectTransform panel)
        {
            CreateToolbar(panel);
            RectTransform grid = NativeUI.CreateScrollGrid(panel, MINIMUM_GRID_HEIGHT);
            AddNoneEntry(grid);
            foreach (Brush brush in BrushRegistry.All)
            {
                // Images and videos have their own tabs.
                if (brush is ImageBrush || brush is VideoBrush)
                    continue;

                Brush registered = brush;
                Sprite icon = MP2.get_sprite(brush.Icon, MP2.get_sprite("icon_question"));
                BrushEntry entry = AddEntry(grid, brush.Id, null, brush.Name, () => icon, () => registered.UnavailableReason);
                if (brush.Wild)
                    wildEntries.Add(entry);
            }

            tuning = new TuningPanel(panel);
        }

        private void BuildMediaTab(RectTransform panel, MediaTab tab)
        {
            bool videos = tab.brush == MaterialBrush.Video;
            string folder = "Custom/" + (videos ? "Videos" : "Images");

            RectTransform toolbar = CreateToolbar(panel);
            Button refresh = NativeUI.CreateButton(toolbar, "Refresh", "Shows files added to or removed from " + folder + ".");
            refresh.onClick.AddListener(() => Safe.Run("refreshing media", () => RefreshMediaTab(tab)));
            Button openFolder = NativeUI.CreateButton(toolbar, "Open Folder", "Opens " + folder + ". Drop " + (videos ? ".mp4 files" : "images") + " in, then press Refresh.");
            openFolder.onClick.AddListener(() => Safe.Run("opening media folder", () => MP2.OpenCustomFolder(videos)));

            tab.grid = NativeUI.CreateScrollGrid(panel, MINIMUM_GRID_HEIGHT);
            AddNoneEntry(tab.grid);
            AddMediaEntries(tab);
        }

        private void AddMediaEntries(MediaTab tab)
        {
            foreach (MaterialType mt in tab.media())
            {
                MaterialType type = mt;
                // Looked up each time: video thumbnails replace their placeholder once they're made.
                tab.entries.Add(AddEntry(tab.grid, tab.brush, type.id_string, tab.label(type), () => MP2.get_sprite(type.name, MP2.get_sprite("icon_question"))));
            }
        }

        private void RefreshMediaTab(MediaTab tab)
        {
            if (tab.brush == MaterialBrush.Video)
                MP2.RefreshBrushesVideos();
            else
                MP2.RefreshBrushesImages();

            foreach (BrushEntry entry in tab.entries)
            {
                entries.Remove(entry);
                Destroy(entry.gameObject);
            }
            tab.entries.Clear();
            AddMediaEntries(tab);

            // A selected file that's gone falls back to None.
            bool selectedGone = MP2.selected_brush == (int)tab.brush && !tab.entries.Any(IsSelected);
            Select(selectedGone ? MaterialBrush.None : (MaterialBrush)MP2.selected_brush, selectedGone ? "" : MP2.selected_brush_custom);
        }

        private void TogglePicking()
        {
            if (MaterialBrushTool.Picking)
                MaterialBrushTool.StopPicking();
            else
                MaterialBrushTool.StartPicking(part => Safe.Run("eyedropper pick", () => PickFrom(part)));
        }

        /// <summary>Selects the paint on a part, with the sliders it was painted with. An unpainted part picks None.</summary>
        private void PickFrom(GameObject part)
        {
            PaintMarker marker = part != null ? part.GetComponent<PaintMarker>() : null;
            Brush brush = marker != null ? marker.brush : null;
            if (brush == null)
            {
                Select(MaterialBrush.None, "");
                return;
            }

            string custom = null;
            if (brush is ImageBrush || brush is VideoBrush)
                custom = marker.detail;
            else if (brush is SharedMaterialBrush shared && shared.IsTunable && shared.Preset != null)
                shared.Current = BrushSettings.Decode(marker.detail, shared.Params, shared.Preset);

            // Shows the Go Wild brushes, so the picked one is visible in the grid.
            if (brush.Wild && !MP2._setting_go_wild && goWild != null)
                goWild.isOn = true;

            // A missing image or video still gets selected: painting with it keeps the file name, which shows for
            // anyone who has the file.
            Select(brush.Id, custom);
            OpenSelectedTab();
        }

        private void BuildSettingsTab(RectTransform panel)
        {
            RectTransform box = NativeUI.CreateBox(panel, spacing: 4f);

            Toggle multiSelect = NativeUI.CreateCheckbox(box, "Multi-Select", "Clicking and dragging will continuously paint objects the brush crosses over.");
            multiSelect.SetIsOnWithoutNotify(MP2._setting_drag_select);
            multiSelect.onValueChanged.AddListener(on =>
            {
                MP2._setting_drag_select = on;
                MP2.SaveSettings();
            });

            Toggle targetSupports = NativeUI.CreateCheckbox(box, "Target Supports", "Selectively paints support structures. Note, not all supports are currently recognized due to how the game was programmed.");
            targetSupports.SetIsOnWithoutNotify(MP2._setting_target_supports);
            targetSupports.onValueChanged.AddListener(on =>
            {
                MP2._setting_target_supports = on;
                MP2.SaveSettings();
            });

            goWild = NativeUI.CreateCheckbox(box, "Go Wild", "Shows experimental material brushes borrowed from internal game effects.");
            goWild.SetIsOnWithoutNotify(MP2._setting_go_wild);
            goWild.onValueChanged.AddListener(on => Safe.Run("go wild", () =>
            {
                MP2._setting_go_wild = on;
                MP2.SaveSettings();
                ApplyWildBrushVisibility();
            }));

            convertButton = NativeUI.CreateButton(panel, "Convert Old Paints", "This park has paints from an older version of the mod that haven't been converted yet. Try again once the park has finished loading.");
            convertButton.onClick.AddListener(() => Safe.Run("convert old paints", MP2.Instance.ConvertOldPaints));
            convertButton.gameObject.SetActive(LegacySaveImporter.HasPending);
        }

        private void AddNoneEntry(RectTransform grid)
        {
            Sprite icon = MP2.get_sprite("icon_none", MP2.get_sprite("icon_question"));
            AddEntry(grid, MaterialBrush.None, null, "None", () => icon);
        }

        private BrushEntry AddEntry(RectTransform grid, MaterialBrush brush, string custom, string label, System.Func<Sprite> sprite, System.Func<string> unavailableReason = null)
        {
            BrushEntry entry = NativeUI.CreateEntry(grid);
            entry.Set(brush, custom, label, sprite, unavailableReason);
            entry.Toggle.onValueChanged.AddListener(on => Safe.Run("brush selection", () => OnEntryToggled(entry, on)));
            entries.Add(entry);
            return entry;
        }

        private void OnEntryToggled(BrushEntry entry, bool on)
        {
            // Entries act as one group across all tabs; clicking the selected one keeps it selected.
            if (!on)
            {
                if (IsSelected(entry))
                    entry.Toggle.SetIsOnWithoutNotify(true);
                return;
            }

            Select(entry.Brush, entry.Custom);
        }

        private void Select(MaterialBrush brush, string custom)
        {
            MP2.selected_brush = (int)brush;
            // Tunable brushes paint with their sliders' settings.
            MP2.selected_brush_custom = custom ?? (BrushRegistry.Get(brush) is SharedMaterialBrush shared ? shared.CurrentDetail : "");
            MP2.MPDebug("Selected brush " + brush + " " + MP2.selected_brush_custom);

            MP2.last_brush = (int)brush;
            MP2.last_brush_custom = custom ?? "";
            MP2.SaveSettings();

            foreach (BrushEntry entry in entries)
                entry.Toggle.SetIsOnWithoutNotify(IsSelected(entry));

            if (tuning != null && tuning.Refresh())
                FitWindowToContent();
        }

        private static bool IsSelected(BrushEntry entry)
        {
            if ((int)entry.Brush != MP2.selected_brush)
                return false;

            return entry.Custom == null || entry.Custom == MP2.selected_brush_custom;
        }

        private void ApplyWildBrushVisibility()
        {
            foreach (BrushEntry entry in wildEntries)
                entry.gameObject.SetActive(MP2._setting_go_wild);

            if (!BrushRegistry.IsVisible((MaterialBrush)MP2.selected_brush))
                Select(MaterialBrush.None, "");
        }

        private void Update()
        {
            if (convertButton != null && convertButton.gameObject.activeSelf != LegacySaveImporter.HasPending)
                convertButton.gameObject.SetActive(LegacySaveImporter.HasPending);

            // The game's Pipette key works here like in its own windows: while this window is in front.
            if (pipetteButtons.Count > 0 && windowFrame != null && InputManager.getKeyDown("PipetteTool")
                && !UIUtility.isInputFieldFocused() && UIWindowsController.Instance.isForemost(windowFrame))
                Safe.Run("eyedropper", TogglePicking);

            Color shown = MaterialBrushTool.Picking ? pipetteActiveColor : pipetteColor;
            foreach (Button pipette in pipetteButtons)
            {
                if (pipette.targetGraphic != null && pipette.targetGraphic.color != shown)
                    pipette.targetGraphic.color = shown;
            }
        }

        private void OnTabChanged() => Safe.Run("tab change", FitWindowToContent);

        public override void onAttachedToWindowFrame(UIWindowFrame windowFrame)
        {
            base.onAttachedToWindowFrame(windowFrame);
            Safe.Run("window sizing", FitWindowToContent);
        }

        /// <summary>Grows the window when the open tab needs more room than it has (e.g. a brush with many sliders).</summary>
        private void FitWindowToContent()
        {
            LayoutElement layout = GetComponent<LayoutElement>();
            if (windowFrame == null || layout == null || tabBar == null || content == null)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            float needed = Mathf.Ceil(LayoutUtility.GetMinHeight(tabBar) + LayoutUtility.GetMinHeight(content));
            layout.minHeight = Mathf.Max(MINIMUM_SIZE.y, needed);
            if (layout.preferredHeight >= layout.minHeight)
                return;

            layout.preferredHeight = layout.minHeight;
            if (windowFrame.rectTransform != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(windowFrame.rectTransform);
            windowFrame.limitToScreenBounds();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Instance = this;
            MP2.MPDebug("On");

            if (MP2.controller != null && GameController.Instance != null)
            {
                MP2.controller.ActivatePipe();
                pipeActivated = true;
            }

            Painter.SetInvisiblePreview(true);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (Instance == this)
                Instance = null;

            MP2.MPDebug("Off");

            if (pipeActivated && MP2.controller != null && GameController.Instance != null)
            {
                MP2.controller.DeactivatePipe();
                pipeActivated = false;
            }

            Painter.SetInvisiblePreview(false);
            MP2_Controller.wms?.close();
            MaterialBrushTool.StopPicking();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (tabGroup != null)
                tabGroup.OnTabChanged -= OnTabChanged;
        }
    }
}
