﻿using Parkitect.UI;
using UnityEngine;

namespace MaterialPainter2
{
    public class MP2Window : UIWindow
    {
        public static MP2Window Instance { get; private set; }

        private void OnGUI()
        {
            if (OptionsMenu.instance != null)
                return;

            float scale = Settings.Instance.uiScale;
            float cell_size = 40;
            float cell_width = cell_size * scale;
            float cell_height = cell_size * scale;
            float cell_width_space = 10 * scale;
            float cell_height_space = 10 * scale;

            float left_offset = 15;
            float top_offset = 40 * scale;

            var screen_width = Screen.currentResolution.width;
            var screen_height = Screen.currentResolution.height;

            Rect tooltip = new Rect();
            string tt_string = "";
            GUIStyle tooltip_guiStyle = new GUIStyle(GUI.skin.label);
            tooltip_guiStyle.fontSize = ((int)(16 * scale));

            for (var i = 0; i < MP2.material_brushes.Count; i++)
            {
                MaterialType entry = MP2.material_brushes[i];

                if (!entry.preview.texture)
                {
                    MP2.MPDebug("Texture Missing: " + entry.name);
                    continue;
                }

                var xx = left_offset + (transform.parent.parent.position.x) + (cell_width + cell_width_space + 1) * i;
                var yy = -top_offset + (transform.parent.parent.position.y);

                GUI.DrawTexture(new Rect(xx, screen_height - yy, cell_width, cell_height), entry.preview.texture, ScaleMode.ScaleToFit);

                if ((Input.mousePosition.x) > xx && (Input.mousePosition.x) < xx + cell_width)
                {
                    if ((screen_height - Input.mousePosition.y) < screen_height - yy + cell_height && (screen_height - Input.mousePosition.y) > screen_height - yy)
                    {
                        if (Input.GetMouseButtonDown(0) && MP2.IsCoolDownReady())
                        {
                            MP2.selected_brush = i;
                            MP2.ResetCountdown();
                            MP2.controller.ActivatePipe();
                        }

                        GUI.DrawTexture(new Rect(xx, screen_height - yy, cell_width, cell_height), MP2.get_sprite("icon_highlight").texture, ScaleMode.ScaleToFit);

                        //

                        tt_string = entry.name;
                        Vector2 size = tooltip_guiStyle.CalcSize(new GUIContent(tt_string));

                        tooltip = new Rect(Input.mousePosition.x + 35, screen_height - Input.mousePosition.y, (size.x + 20), size.y);
                    }
                }

                if (MP2.selected_brush == i)
                    GUI.DrawTexture(new Rect(xx, screen_height - yy, cell_width, cell_height), MP2.get_sprite("icon_selection").texture, ScaleMode.ScaleToFit);
            }

            if (tt_string != "")
            {
                GUI.color = Color.gray;
                GUI.Box(tooltip, GUIContent.none);

                GUI.color = Color.white;
                tooltip.x += 10;
                GUI.Label(tooltip, tt_string, tooltip_guiStyle);
                GUI.color = Color.white;
            }

            return;

            //////
            /*
            Texture tex = MP2.get_sprite("checkbox_uncheck").texture;
            if (MP2.controller.OnlyBuildables)
                tex = MP2.get_sprite("checkbox_check").texture;

            float check_size = 26 * scale;

            Rect check_rect = new Rect(
                (transform.parent.parent.position.x) + left_offset,
                screen_height - (transform.parent.parent.position.y) + top_offset + cell_height_space + cell_height,
                check_size,
                check_size);

            GUI.DrawTexture(check_rect, tex, ScaleMode.ScaleToFit);

            Rect check_rect_text = new Rect((transform.parent.parent.position.x) + left_offset + check_size + cell_width_space,
                screen_height - (transform.parent.parent.position.y) + top_offset + cell_height_space + cell_height + 4 * scale,
                200 * scale,
                30 * scale);

            GUIStyle guiStyle = new GUIStyle(GUI.skin.label);
            guiStyle.fontSize = ((int)(16 * scale));

            GUI.color = Color.black;
            GUI.Label(check_rect_text, "Only Buildables", guiStyle);
            GUI.color = Color.white;

            if (MP2.IsCoolDownReady())
            {
                if (Input.GetMouseButtonUp(0))
                {
                    if (PointInRectangle(new Vector2(Input.mousePosition.x, screen_height - Input.mousePosition.y), check_rect))
                    {
                        MP2.controller.OnlyBuildables = !MP2.controller.OnlyBuildables;
                        MP2.ResetCountdown();
                    }
                }
            }*/

            //////
            Texture tex = MP2.get_sprite("checkbox_uncheck").texture;
            if (MP2.controller.TargetSupports)
                tex = MP2.get_sprite("checkbox_check").texture;

            float check_size = 26 * scale;

            Rect check_rect = new Rect(
                (transform.parent.parent.position.x) + left_offset,
                screen_height - (transform.parent.parent.position.y) + top_offset + cell_height_space + cell_height + check_size + cell_height_space,
                check_size,
                check_size);

            GUI.DrawTexture(check_rect, tex, ScaleMode.ScaleToFit);

            Rect check_rect_text = new Rect((transform.parent.parent.position.x) + left_offset + check_size + cell_width_space,
                screen_height - (transform.parent.parent.position.y) + top_offset + cell_height_space + cell_height + 4 * scale + check_size + cell_height_space,
                200 * scale,
                30 * scale);

            GUIStyle guiStyle = new GUIStyle(GUI.skin.label);
            guiStyle.fontSize = ((int)(16 * scale));

            GUI.color = Color.black;
            GUI.Label(check_rect_text, "Target Only Supports", guiStyle);
            GUI.color = Color.white;

            if (MP2.IsCoolDownReady())
            {
                if (Input.GetMouseButtonUp(0))
                {
                    if (PointInRectangle(new Vector2(Input.mousePosition.x, screen_height - Input.mousePosition.y), check_rect))
                    {
                        MP2.controller.TargetSupports = !MP2.controller.TargetSupports;
                        MP2.ResetCountdown();

                        if (MP2.controller.TargetSupports) { MP2.controller.IncludeDescendants = true; }
                    }
                }
            }

            //////

            tex = MP2.get_sprite("checkbox_uncheck").texture;
            if (MP2.controller.IncludeDescendants)
                tex = MP2.get_sprite("checkbox_check").texture;

            check_rect = new Rect(
                (transform.parent.parent.position.x) + left_offset,
                screen_height - (transform.parent.parent.position.y) + top_offset + cell_height_space + cell_height,
                check_size,
                check_size);

            GUI.DrawTexture(check_rect, tex, ScaleMode.ScaleToFit);

            check_rect_text = new Rect((transform.parent.parent.position.x) + left_offset + check_size + cell_width_space,
                screen_height - (transform.parent.parent.position.y) + top_offset + cell_height_space + cell_height + 4 * scale,
                200 * scale,
                30 * scale);

            guiStyle = new GUIStyle(GUI.skin.label);
            guiStyle.fontSize = ((int)(16 * scale));

            GUI.color = Color.black;
            GUI.Label(check_rect_text, "Include Descendants", guiStyle);
            GUI.color = Color.white;

            if (MP2.IsCoolDownReady())
            {
                if (Input.GetMouseButtonUp(0))
                {
                    if (PointInRectangle(new Vector2(Input.mousePosition.x, screen_height - Input.mousePosition.y), check_rect))
                    {
                        MP2.controller.IncludeDescendants = !MP2.controller.IncludeDescendants;
                        MP2.ResetCountdown();

                        if (!MP2.controller.IncludeDescendants) { MP2.controller.TargetSupports = false; }
                    }
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Instance = this;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Instance = null;
        }

        private bool PointInRectangle(Vector2 point, Rect rectangle)
        {
            return point.x >= rectangle.x && point.x <= rectangle.x + rectangle.width &&
                   point.y >= rectangle.y && point.y <= rectangle.y + rectangle.height;
        }
    }
}