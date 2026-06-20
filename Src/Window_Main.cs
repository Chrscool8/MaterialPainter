using Parkitect.UI;

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace MaterialPainter2
{
    public class Window_Main : UIWindow
    {
        private const float GRID_WIDTH = 260f;
        private const float MINIMUM_GRID_HEIGHT = 155f;
        private const float MINIMUM_WINDOW_HEIGHT = 150f;

        public static Window_Main Instance { get; private set; }

        public static List<UI_Button> buttons_elements;
        public static List<UI_Button> buttons_videos;
        public static List<UI_Button> buttons_images;
        public static List<UI_Button> buttons_all;
        private List<UI_Button> buttons_wild;

        private UI_Grid_Buttons grid_elements;
        private UI_Grid_Buttons grid_videos;
        private UI_Grid_Buttons grid_images;
        private UI_Grid_Buttons grid_settings;
        private UI_Tab_Bar tab_bar_types;

        private UI_CheckBox checkbox_multiselect;
        private UI_CheckBox checkbox_targetsupports;
        private UI_CheckBox checkbox_gowild;
        private UI_PushButton button_reload;
        private UI_Button button_none;

        UI_Tab settings_tab = null;
        private bool pipeActivated;
        private float lastAppliedWindowHeight = -1f;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Fugeddabouddit.")]
        private void OnGUI()
        {
            if (OptionsMenu.instance != null)
                return;

            float num = Screen.dpi;
            if (Mathf.Approximately(num, 0f))
            {
                num = 72f;
            }
            float dpi_scale = DPIHelper.scaleDPI(num / 72f) / 1.3f * Settings.Instance.uiScale;

            float left_offset = 7f * dpi_scale;
            float top_offset = 34.0f * dpi_scale;

            float tab_height = 43f;

            ToolTipper tt = GetComponent<ToolTipper>();
            tt.tooltip = "";

            tab_bar_types.SetWidth(260);
            tab_bar_types.tab_size = new Vector2(tab_height, tab_height);
            tab_bar_types.SetPosition(left_offset + transform.parent.parent.position.x, transform.parent.parent.position.y - (top_offset));
            tab_bar_types.DrawTabs(dpi_scale);

            float elementsGridHeight = GetGridHeight(grid_elements);
            float videosGridHeight = GetGridHeight(grid_videos);
            float imagesGridHeight = GetGridHeight(grid_images);
            float settingsGridHeight = GetGridHeight(grid_settings);
            ApplyWindowHeight(GetActiveGridHeight(elementsGridHeight, videosGridHeight, imagesGridHeight, settingsGridHeight));

            grid_elements.SetSize(GRID_WIDTH, elementsGridHeight);
            grid_elements.SetPosition(left_offset + transform.parent.parent.position.x, transform.parent.parent.position.y - (tab_height) * dpi_scale - (top_offset) + 2);
            grid_elements.DrawGrid(dpi_scale);

            grid_videos.SetSize(GRID_WIDTH, videosGridHeight);
            grid_videos.SetPosition(left_offset + transform.parent.parent.position.x, transform.parent.parent.position.y - (tab_height) * dpi_scale - (top_offset) + 2);
            grid_videos.DrawGrid(dpi_scale);

            grid_images.SetSize(GRID_WIDTH, imagesGridHeight);
            grid_images.SetPosition(left_offset + transform.parent.parent.position.x, transform.parent.parent.position.y - (tab_height) * dpi_scale - (top_offset) + 2);
            grid_images.DrawGrid(dpi_scale);

            grid_settings.SetSize(GRID_WIDTH, settingsGridHeight);
            grid_settings.SetPosition(left_offset + transform.parent.parent.position.x, transform.parent.parent.position.y - (tab_height) * dpi_scale - (top_offset) + 2);
            grid_settings.DrawGrid(dpi_scale);

            float checkbox_size = 24f * dpi_scale;
            checkbox_multiselect.SetTileSize(checkbox_size, checkbox_size);
            checkbox_multiselect.SetPosition(left_offset + transform.parent.parent.position.x + 16 * dpi_scale, transform.parent.parent.position.y - (tab_height * dpi_scale) - (top_offset) + 2 - 16 * dpi_scale);
            checkbox_multiselect.DrawSprite(dpi_scale);

            checkbox_targetsupports.SetTileSize(checkbox_size, checkbox_size);
            checkbox_targetsupports.SetPosition(left_offset + transform.parent.parent.position.x + 16 * dpi_scale, transform.parent.parent.position.y - (tab_height * dpi_scale) - (top_offset) + 2 - (checkbox_size * 1.5f) - 16 * dpi_scale);
            checkbox_targetsupports.DrawSprite(dpi_scale);

            checkbox_gowild.SetTileSize(checkbox_size, checkbox_size);
            checkbox_gowild.SetPosition(left_offset + transform.parent.parent.position.x + 16 * dpi_scale, transform.parent.parent.position.y - (tab_height * dpi_scale) - (top_offset) + 2 - (checkbox_size * 3f) - 16 * dpi_scale);
            checkbox_gowild.DrawSprite(dpi_scale);

            button_reload.SetSize(checkbox_size * 8f, checkbox_size);
            button_reload.SetPosition(left_offset + transform.parent.parent.position.x + 16 * dpi_scale, transform.parent.parent.position.y - (tab_height * dpi_scale) - (top_offset) + 2 - (checkbox_size * 4.5f) - 16 * dpi_scale);

            if (settings_tab != null && settings_tab.selected)
                button_reload.DrawButton();

            if (tt.tooltip != "")
            {
                GUIStyle tooltip_guiStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter
                };
                Vector2 size_ = tooltip_guiStyle.CalcSize(new GUIContent(tt.tooltip));

                Rect tooltip_rect = new Rect(Input.mousePosition.x + 35, Screen.height - Input.mousePosition.y, (size_.x + 20), size_.y);

                GUI.color = new Color(92f / 255f, 105f / 255f, 110f / 255f, .9f);
                GUI.Box(tooltip_rect, GUIContent.none);

                GUI.color = Color.white;
                GUI.Label(tooltip_rect, tt.tooltip, tooltip_guiStyle);
                GUI.color = Color.white;
            }

            MP2_Controller.wms?.OnGui();
        }

        private float GetGridHeight(UI_Grid_Buttons grid)
        {
            if (grid == null)
                return MINIMUM_GRID_HEIGHT;

            return Mathf.Max(MINIMUM_GRID_HEIGHT, grid.GetContentHeight(GRID_WIDTH));
        }

        private float GetActiveGridHeight(float elementsGridHeight, float videosGridHeight, float imagesGridHeight, float settingsGridHeight)
        {
            if (grid_elements != null && grid_elements.visible)
                return elementsGridHeight;
            if (grid_videos != null && grid_videos.visible)
                return videosGridHeight;
            if (grid_images != null && grid_images.visible)
                return imagesGridHeight;
            if (grid_settings != null && grid_settings.visible)
                return settingsGridHeight;

            return MINIMUM_GRID_HEIGHT;
        }

        private void ApplyWindowHeight(float gridHeight)
        {
            float targetHeight = MINIMUM_WINDOW_HEIGHT + Mathf.Max(0f, gridHeight - MINIMUM_GRID_HEIGHT);
            if (Mathf.Approximately(lastAppliedWindowHeight, targetHeight))
                return;

            lastAppliedWindowHeight = targetHeight;

            SetRectTransformHeight(rectTransform, targetHeight);
            ApplyContentLayoutHeight(targetHeight);

            if (windowFrame == null)
                return;

            if (windowFrame.contentHolderTransform != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(windowFrame.contentHolderTransform);

            if (windowFrame.rectTransform != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(windowFrame.rectTransform);
                windowFrame.rectTransform.ForceUpdateRectTransforms();
            }

            windowFrame.limitToScreenBounds();
        }

        private void ApplyContentLayoutHeight(float height)
        {
            LayoutElement layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
                return;

            layoutElement.minHeight = MINIMUM_WINDOW_HEIGHT;
            layoutElement.preferredHeight = height;
        }

        private void SetRectTransformHeight(RectTransform targetRectTransform, float height)
        {
            if (targetRectTransform == null)
                return;

            targetRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            Vector2 sizeDelta = targetRectTransform.sizeDelta;
            sizeDelta.y = height;
            targetRectTransform.sizeDelta = sizeDelta;
        }

        public void SetWindowTitle(string title)
        {
            UIWindowSettings ws = GetComponent<UIWindowSettings>();
            if (ws != null)
            {
                ws.title = MP2.GetWindowTitle(title);
            }
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

            GameObject[] allObjects = FindObjectsOfType<GameObject>();

            foreach (GameObject obj in allObjects)
            {
                if (obj != null)
                {
                    ChangedMarker cm = obj.GetComponent<ChangedMarker>();
                    if (cm != null)
                    {
                        if (cm.GetCurrentBrush() == (int)MaterialBrush.Invisible)
                            MP2.controller.SetMaterial(obj.transform, (int)MaterialBrush.InvisiblePreview);
                    }
                }
            }

            //

            ToolTipper tt = gameObject.AddComponent<ToolTipper>();

            buttons_elements = new List<UI_Button>();
            buttons_wild = new List<UI_Button>();
            button_none = CreateElementButton(MaterialBrush.None, "icon_none", "None");
            CreateElementButton(MaterialBrush.Water, "icon_water", "Water");
            CreateElementButton(MaterialBrush.Lava, "icon_lava", "Lava");
            CreateElementButton(MaterialBrush.Glass, "icon_glass", "Glass");
            CreateElementButton(MaterialBrush.TriplanarTerrainGrass, "icon_terrain_grass", "Grass");
            CreateElementButton(MaterialBrush.TriplanarTerrainDirt, "icon_terrain_dirt", "Dirt");
            CreateElementButton(MaterialBrush.TriplanarTerrainStone, "icon_terrain_stone", "Stone");
            CreateElementButton(MaterialBrush.TriplanarTerrainSnow, "icon_terrain_snow", "Snow");
            CreateElementButton(MaterialBrush.TriplanarTerrainSand, "icon_terrain_sand", "Sand");
            CreateElementButton(MaterialBrush.TriplanarTerrainIce, "icon_terrain_ice", "Ice");
            CreateElementButton(MaterialBrush.TriplanarTerrainLavarock, "icon_terrain_lavarock", "Lava Rock");
            CreateElementButton(MaterialBrush.TriplanarTerrainAsh, "icon_terrain_ash", "Ash");
            CreateElementButton(MaterialBrush.TriplanarTerrainCrackedSoil, "icon_terrain_cracked_soil", "Cracked Soil");
            CreateElementButton(MaterialBrush.TriplanarTerrainDeadGrass, "icon_terrain_dead_grass", "Dead Grass");
            CreateElementButton(MaterialBrush.TriplanarTerrainBlueprint, "icon_terrain_blueprint", "Blueprint");
            CreateElementButton(MaterialBrush.Terrain, "tex_question", "Terrain", true);
            CreateElementButton(MaterialBrush.UndergroundTerrain, "tex_question", "Underground Terrain", true);
            CreateElementButton(MaterialBrush.DataView, "tex_question", "Data View", true);
            CreateElementButton(MaterialBrush.Selected, "tex_question", "Selected", true);
            CreateElementButton(MaterialBrush.Deleted, "tex_question", "Deleted", true);
            CreateElementButton(MaterialBrush.SceneGhost, "tex_question", "Scene Ghost", true);
            CreateElementButton(MaterialBrush.DecoGlow, "tex_question", "Deco Glow", true);
            CreateElementButton(MaterialBrush.CollisionGlow, "tex_question", "Collision Glow", true);
            CreateElementButton(MaterialBrush.RideLight, "tex_question", "Ride Light", true);
            CreateElementButton(MaterialBrush.CoasterStats, "tex_question", "Coaster Stats", true);
            CreateElementButton(MaterialBrush.Waterfall, "icon_effect_waterfall", "Waterfall", true);
            CreateElementButton(MaterialBrush.BlackHoleCore, "icon_effect_black_hole_core", "Black Hole", true);
            CreateElementButton(MaterialBrush.BlackHoleDisc, "icon_effect_black_hole_disc", "Accretion Disc", true);
            CreateElementButton(MaterialBrush.InvisiblePreview, "icon_invisible", "Invisible");

            grid_elements = new UI_Grid_Buttons();
            grid_elements.AddButtons(buttons_elements);

            //

            buttons_images = new List<UI_Button>
            {
                button_none,
            };

            foreach (MaterialType mt in MP2.material_brushes_images.Values)
            {
                UI_Button butt = new UI_Button(parent: gameObject, button_image_sprite: mt.name, button_image_sprite_highlight: "icon_highlight",
                    onMouseClick: () =>
                    {
                        MP2.selected_brush = mt.id; MP2.selected_brush_custom = mt.id_string; MP2.MPDebug(mt.id + " " + mt.id_string);
                    }, tooltip_text: mt.id_string);
                buttons_images.Add(butt);
            }

            grid_images = new UI_Grid_Buttons();
            grid_images.AddButtons(buttons_images);

            buttons_videos = new List<UI_Button>
            {
                button_none,
            };

            foreach (MaterialType mt in MP2.material_brushes_videos.Values)
            {
                UI_Button butt = new UI_Button(parent: gameObject, button_image_sprite: mt.name, button_image_sprite_highlight: "icon_highlight",
                    onMouseClick: () =>
                    {
                        MP2.selected_brush = mt.id; MP2.selected_brush_custom = mt.id_string; MP2.MPDebug(mt.id + " " + mt.id_string);
                    }, tooltip_text: mt.name);
                buttons_videos.Add(butt);
            }


            grid_videos = new UI_Grid_Buttons();
            grid_videos.AddButtons(buttons_videos);

            //

            grid_settings = new UI_Grid_Buttons();

            //

            buttons_all = new List<UI_Button>();
            buttons_all.AddRange(buttons_elements);
            buttons_all.AddRange(buttons_images);
            buttons_all.AddRange(buttons_videos);

            foreach (UI_Button button in buttons_all)
            {
                button.SetButtonFamily(buttons_all);
            }

            ApplyWildBrushVisibility();
            SelectNoneBrush();

            //

            checkbox_multiselect = new UI_CheckBox(parent: gameObject, text: "Multi-Select", onMouseClick: () => { MP2._setting_drag_select = !MP2._setting_drag_select; }, check_state: MP2._setting_drag_select, tooltip_text: "Clicking and dragging will continuously paint objects the brush crosses over.");
            checkbox_targetsupports = new UI_CheckBox(parent: gameObject, text: "Target Supports", onMouseClick: () => { MP2._setting_target_supports = !MP2._setting_target_supports; }, check_state: MP2._setting_target_supports, tooltip_text: "Selectively paints support structures. Note, not all supports are currently recognized due to how the game was programmed.");
            checkbox_gowild = new UI_CheckBox(parent: gameObject, text: "Go Wild", onMouseClick: () => { MP2._setting_go_wild = !MP2._setting_go_wild; ApplyWildBrushVisibility(); }, check_state: MP2._setting_go_wild, tooltip_text: "Shows experimental material brushes borrowed from internal game effects.");
            button_reload = new UI_PushButton(parent: gameObject, text: "Reload From Save", tooltip_text: "If you find that paints aren't being applied when loading a park, try this. Sometimes if a park loads too long, it needs another try.", onMouseClick: () => { MP2.Instance.ReassignMaterialsAfterLoadingSave(); });

            //

            tab_bar_types = new UI_Tab_Bar(gameObject);
            UI_Tab tab0 = tab_bar_types.AddTab(icon_sprite: "icon_elements", object_to_control_: grid_elements, tab_name: "Elements");
            tab_bar_types.AddTab(icon_sprite: "icon_camera", object_to_control_: grid_images, tab_name: "Images");
            tab_bar_types.AddTab(icon_sprite: "icon_videocamera", object_to_control_: grid_videos, tab_name: "Videos");
            settings_tab = tab_bar_types.AddTab(icon_sprite: "icon_toolbox", object_to_control_: new List<UI_Item> { grid_settings, checkbox_multiselect, checkbox_targetsupports, checkbox_gowild }, tab_name: "Settings");

            foreach (UI_Tab ui_tab in tab_bar_types.tabs)
            {
                ui_tab.mp2window = this;
            }

            tab0.Select();
        }

        private UI_Button CreateElementButton(MaterialBrush brush, string iconSprite, string tooltipText, bool wild = false)
        {
            UI_Button button = new UI_Button(parent: gameObject, button_image_sprite: iconSprite, button_image_sprite_highlight: "icon_highlight", onMouseClick: () =>
            {
                MP2.selected_brush = (int)brush;
                MP2.selected_brush_custom = "";
            }, tooltip_text: tooltipText);
            buttons_elements.Add(button);

            if (wild)
                buttons_wild.Add(button);

            return button;
        }

        private void ApplyWildBrushVisibility()
        {
            if (buttons_wild != null)
            {
                foreach (UI_Button button in buttons_wild)
                {
                    if (button != null)
                        button.visible = MP2._setting_go_wild;
                }
            }

            if (!MP2.IsMaterialBrushVisible(MP2.selected_brush))
                SelectNoneBrush();
        }

        private void SelectNoneBrush()
        {
            if (button_none == null)
                return;

            if (button_none.selected && MP2.selected_brush == (int)MaterialBrush.None)
                return;

            button_none.Select();
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

            GameObject[] allObjects = FindObjectsOfType<GameObject>();

            foreach (GameObject obj in allObjects)
            {
                if (obj != null)
                {
                    ChangedMarker cm = obj.GetComponent<ChangedMarker>();
                    if (cm != null)
                    {
                        if (cm.GetCurrentBrush() == (int)MaterialBrush.InvisiblePreview)
                            MP2.controller.SetMaterial(obj.transform, (int)MaterialBrush.Invisible);
                    }
                }
            }

            List<UI_Button> buttons = new List<UI_Button>();
            if (buttons_elements != null)
                buttons.AddRange(buttons_elements);
            if (buttons_videos != null)
                buttons.AddRange(buttons_videos);
            if (buttons_images != null)
                buttons.AddRange(buttons_images);

            foreach (UI_Button button in buttons)
            {
                if (button != null)
                {
                    //destroy nicely
                }
            }

            buttons = null;
            buttons_elements = null;
            buttons_videos = null;
            buttons_images = null;
            buttons_all = null;
            buttons_wild = null;

            grid_elements = null;
            grid_videos = null;
            grid_images = null;
            grid_settings = null;

            tab_bar_types = null;

            checkbox_multiselect = null;
            checkbox_targetsupports = null;
            checkbox_gowild = null;
            button_reload = null;
            button_none = null;

            ToolTipper tt = GetComponent<ToolTipper>();
            if (tt != null)
                Destroy(tt);

            MP2_Controller.wms?.close();
        }
    }
}
