﻿using System.Collections.Generic;

using Parkitect.UI;

using UnityEngine;

namespace MaterialPainter2
{
    public class Window_Main : UIWindow
    {
        public static Window_Main Instance { get; private set; }

        public static List<UI_Button> buttons_elements;
        public static List<UI_Button> buttons_videos;
        public static List<UI_Button> buttons_images;
        public static List<UI_Button> buttons_all;

        private UI_Grid_Buttons grid_elements;
        private UI_Grid_Buttons grid_videos;
        private UI_Grid_Buttons grid_settings;
        private UI_Tab_Bar tab_bar_types;

        private UI_CheckBox checkbox_multiselect;
        private UI_CheckBox checkbox_targetsupports;
        private UI_PushButton button_reload;

        UI_Tab settings_tab = null;

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

            grid_elements.SetSize(260, 155);
            grid_elements.SetPosition(left_offset + transform.parent.parent.position.x, transform.parent.parent.position.y - (tab_height) * dpi_scale - (top_offset) + 2);
            grid_elements.DrawGrid(dpi_scale);

            grid_videos.SetSize(260, 155);
            grid_videos.SetPosition(left_offset + transform.parent.parent.position.x, transform.parent.parent.position.y - (tab_height) * dpi_scale - (top_offset) + 2);
            grid_videos.DrawGrid(dpi_scale);

            grid_settings.SetSize(260, 155);
            grid_settings.SetPosition(left_offset + transform.parent.parent.position.x, transform.parent.parent.position.y - (tab_height) * dpi_scale - (top_offset) + 2);
            grid_settings.DrawGrid(dpi_scale);

            float checkbox_size = 24f * dpi_scale;
            checkbox_multiselect.SetTileSize(checkbox_size, checkbox_size);
            checkbox_multiselect.SetPosition(left_offset + transform.parent.parent.position.x + 16 * dpi_scale, transform.parent.parent.position.y - (tab_height * dpi_scale) - (top_offset) + 2 - 16 * dpi_scale);
            checkbox_multiselect.DrawSprite(dpi_scale);

            checkbox_targetsupports.SetTileSize(checkbox_size, checkbox_size);
            checkbox_targetsupports.SetPosition(left_offset + transform.parent.parent.position.x + 16 * dpi_scale, transform.parent.parent.position.y - (tab_height * dpi_scale) - (top_offset) + 2 - (checkbox_size * 1.5f) - 16 * dpi_scale);
            checkbox_targetsupports.DrawSprite(dpi_scale);

            button_reload.SetSize(checkbox_size * 8f, checkbox_size);
            button_reload.SetPosition(left_offset + transform.parent.parent.position.x + 16 * dpi_scale, transform.parent.parent.position.y - (tab_height * dpi_scale) - (top_offset) + 2 - (checkbox_size * 3f) - 16 * dpi_scale);

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

                GUI.color = new Color(92, 105, 110, .9f);
                GUI.Box(tooltip_rect, GUIContent.none);

                GUI.color = new Color(153, 168, 166);
                GUI.Label(tooltip_rect, tt.tooltip, tooltip_guiStyle);
                GUI.color = Color.white;
            }

            MP2_Controller.wms?.OnGui();
        }


        public void SetWindowTitle(string title)
        {
            UIWindowSettings ws = GetComponent<UIWindowSettings>();
            if (ws != null)
            {
                ws.title = "Material Painter - " + title;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Instance = this;
            MP2.MPDebug("On");

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
            UI_Button button_none = new UI_Button(parent: gameObject, button_image_sprite: "icon_none", button_image_sprite_highlight: "icon_highlight", onMouseClick: () => { MP2.selected_brush = (int)MaterialBrush.None; }, tooltip_text: "None");
            buttons_elements.Add(button_none);
            UI_Button button_water = new UI_Button(parent: gameObject, button_image_sprite: "icon_water", button_image_sprite_highlight: "icon_highlight", onMouseClick: () => { MP2.selected_brush = (int)MaterialBrush.Water; }, tooltip_text: "Water");
            buttons_elements.Add(button_water);
            UI_Button button_lava = new UI_Button(parent: gameObject, button_image_sprite: "icon_lava", button_image_sprite_highlight: "icon_highlight", onMouseClick: () => { MP2.selected_brush = (int)MaterialBrush.Lava; }, tooltip_text: "Lava");
            buttons_elements.Add(button_lava);
            UI_Button button_glass = new UI_Button(parent: gameObject, button_image_sprite: "icon_glass", button_image_sprite_highlight: "icon_highlight", onMouseClick: () => { MP2.selected_brush = (int)MaterialBrush.Glass; }, tooltip_text: "Glass");
            buttons_elements.Add(button_glass);
            UI_Button button_invisible = new UI_Button(parent: gameObject, button_image_sprite: "icon_invisible", button_image_sprite_highlight: "icon_highlight", onMouseClick: () => { MP2.selected_brush = (int)MaterialBrush.InvisiblePreview; }, tooltip_text: "Invisible");
            buttons_elements.Add(button_invisible);

            grid_elements = new UI_Grid_Buttons();
            grid_elements.AddButtons(buttons_elements);

            //

            buttons_images = new List<UI_Button>();

            //

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
            buttons_all.AddRange(buttons_videos);

            foreach (UI_Button button in buttons_all)
            {
                button.SetButtonFamily(buttons_all);
            }

            button_none.Select();

            //

            checkbox_multiselect = new UI_CheckBox(parent: gameObject, text: "Multi-Select", onMouseClick: () => { MP2._setting_drag_select = !MP2._setting_drag_select; }, check_state: MP2._setting_drag_select, tooltip_text: "Clicking and dragging will continuously paint objects the brush crosses over.");
            checkbox_targetsupports = new UI_CheckBox(parent: gameObject, text: "Target Supports", onMouseClick: () => { MP2._setting_target_supports = !MP2._setting_target_supports; }, check_state: MP2._setting_target_supports, tooltip_text: "Selectively paints support structures. Note, not all supports are currently recognized due to how the game was programmed.");
            button_reload = new UI_PushButton(parent: gameObject, text: "Reload From Save", tooltip_text: "If you find that paints are being applied when loading a park, try this. Sometimes if a park loads too long, it needs another try.", onMouseClick: () => { MP2.Instance.ReassignMaterialsAfterLoadingSave(); });

            //

            tab_bar_types = new UI_Tab_Bar(gameObject);
            UI_Tab tab0 = tab_bar_types.AddTab(icon_sprite: "icon_elements", object_to_control_: grid_elements, tab_name: "Elements");
            tab_bar_types.AddTab(icon_sprite: "icon_camera", object_to_control_: grid_settings, tab_name: "Images");
            tab_bar_types.AddTab(icon_sprite: "icon_videocamera", object_to_control_: grid_videos, tab_name: "Videos");
            settings_tab = tab_bar_types.AddTab(icon_sprite: "icon_toolbox", object_to_control_: new List<UI_Item> { grid_settings, checkbox_multiselect, checkbox_targetsupports }, tab_name: "Settings");

            foreach (UI_Tab ui_tab in tab_bar_types.tabs)
            {
                ui_tab.mp2window = this;
            }

            tab0.Select();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Instance = null;
            MP2.MPDebug("Off");

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
            buttons.AddRange(buttons_elements);
            buttons.AddRange(buttons_videos);
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

            grid_elements = null;
            grid_settings = null;

            tab_bar_types = null;

            checkbox_multiselect = null;
            checkbox_targetsupports = null;

            ToolTipper tt = GetComponent<ToolTipper>();
            if (tt != null)
                Destroy(tt);

            MP2_Controller.wms?.close();
        }
    }
}