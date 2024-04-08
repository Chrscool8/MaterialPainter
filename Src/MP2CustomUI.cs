using ExCSS;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialPainter2
{
    public class UI_Item
    {
        public bool visible = true;
        public string tooltip_text = "";
        public GameObject parent;

        public virtual void ShowContent()
        {
            throw new NotImplementedException();
        }

        public virtual void HideContent()
        {
            throw new NotImplementedException();
        }

        public void setToolTip(string text)
        {
            if (parent != null)
            {
                ToolTipper tt = parent.GetComponent<ToolTipper>();
                if (tt != null)
                {
                    tt.tooltip = text;
                }
            }
        }
    }

    public class UI_Button : UI_Item
    {
        public Vector2 position;
        public Vector2 size;

        public string _button_image_sprite = "checkbox_uncheck";
        public string _button_image_sprite_highlight = "icon_highlight";
        public string _button_image_sprite_select = "icon_selection";

        public List<UI_Button> button_family;

        public delegate void OnMouseHover();

        private OnMouseHover on_mouse_hover;

        public delegate void OnMouseClick();

        private OnMouseClick on_mouse_click;

        public bool selected = false;
        public bool auto_invert_y = true;

        public UI_Button(GameObject parent = null,
            float x = 0, float y = 0, float size_x = 32, float size_y = 32,
            string button_image_sprite = "", string button_image_sprite_highlight = "", string button_image_sprite_select = "",
            OnMouseHover onMouseHover = null, OnMouseClick onMouseClick = null, string tooltip_text = "")
        {
            Setup(parent, x, y, size_x, size_y, button_image_sprite, button_image_sprite_highlight, button_image_sprite_select, onMouseHover, onMouseClick, tooltip_text);
        }

        public UI_Button Setup(GameObject parent = null,
            float x = 0, float y = 0, float size_x = 32, float size_y = 32,
            string button_image_sprite = "", string button_image_sprite_highlight = "", string button_image_sprite_select = "",
            OnMouseHover onMouseHover = null, OnMouseClick onMouseClick = null, string tooltip_text = "")
        {
            position = new Vector2(x, y);
            size = new Vector2(size_x, size_y);

            if (button_image_sprite != "")
                _button_image_sprite = button_image_sprite;
            if (button_image_sprite_highlight != "")
                _button_image_sprite_highlight = button_image_sprite_highlight;
            if (button_image_sprite_select != "")
                _button_image_sprite_select = button_image_sprite_select;

            if (onMouseHover == null)
                on_mouse_hover = default_on_mouse_hover;
            else
                on_mouse_hover = onMouseHover;

            if (onMouseClick == null)
                on_mouse_click = default_on_mouse_click;
            else
                on_mouse_click = onMouseClick;

            this.tooltip_text = tooltip_text;
            this.parent = parent;

            return this;
        }

        public void default_on_mouse_hover()
        {
        }

        public void default_on_mouse_click()
        {
        }

        public void DrawSprite()
        {
            if (!visible)
                return;

            float yy = position.y;
            if (auto_invert_y)
                yy = Screen.height - position.y;

            GUI.DrawTexture(new Rect(position.x, yy, size.x, size.y), MP2.get_sprite(_button_image_sprite).texture, ScaleMode.ScaleToFit);

            if (Utils.PointInRectangle(Input.mousePosition, new Rect(position.x, position.y - size.y, size.x, size.y)))
            {
                GUI.DrawTexture(new Rect(position.x, yy, size.x, size.y), MP2.get_sprite(_button_image_sprite_highlight).texture, ScaleMode.ScaleToFit);

                if (MP2.IsCoolDownReady() && Input.GetMouseButtonUp(0))
                {
                    MP2.ResetCountdown();
                    Select();
                }

                setToolTip(tooltip_text);
            }

            if (selected)
            {
                GUI.DrawTexture(new Rect(position.x, yy, size.x, size.y), MP2.get_sprite(_button_image_sprite_select).texture, ScaleMode.ScaleToFit);
            }
        }

        public void Select()
        {
            foreach (var button in button_family)
            {
                button.selected = false;
            }

            selected = true;
            on_mouse_click?.Invoke();
        }

        public void SetPosition(float x, float y)
        {
            position.x = x;
            position.y = y;
        }

        public void SetTileSize(float x, float y)
        {
            size.x = x;
            size.y = y;
        }

        public void SetButtonFamily(List<UI_Button> family)
        {
            button_family = family;
        }
    }

    public class UI_CheckBox : UI_Item
    {
        public Vector2 position;
        public Vector2 size;

        public string _button_image_unchecked = "checkbox_uncheck";
        public string _button_image_checked = "checkbox_check";

        public delegate void OnMouseHover();

        private OnMouseHover on_mouse_hover;

        public delegate void OnMouseClick();

        private OnMouseClick on_mouse_click;

        public bool selected = false;
        public bool auto_invert_y = true;

        public string text;

        public UI_CheckBox(GameObject parent, float x = 0, float y = 0, float size_x = 32, float size_y = 32, string text = null, OnMouseClick onMouseClick = null, bool check_state = false, string tooltip_text = "")
        {
            Setup(parent, x, y, size_x, size_y, text, onMouseClick, check_state, tooltip_text);
        }

        public UI_CheckBox Setup(GameObject parent, float x = 0, float y = 0, float size_x = 32, float size_y = 32, string text = null, OnMouseClick onMouseClick = null, bool check_state = false, string tooltip_text = "")
        {
            position = new Vector2(x, y);
            size = new Vector2(size_x, size_y);

            if (onMouseClick == null)
                on_mouse_click = default_on_mouse_click;
            else
                on_mouse_click = onMouseClick;

            this.text = text;
            this.tooltip_text = tooltip_text;

            selected = check_state;

            this.parent = parent;

            return this;
        }

        public void default_on_mouse_hover()
        {
        }

        public void default_on_mouse_click()
        {
        }

        public void DrawSprite(float dpi_scale = 1)
        {
            if (!visible)
                return;

            float yy = position.y;
            if (auto_invert_y)
                yy = Screen.height - position.y;

            if (!selected)
            {
                GUI.DrawTexture(new Rect(position.x, yy, size.x, size.y), MP2.get_sprite(_button_image_unchecked).texture, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.DrawTexture(new Rect(position.x, yy, size.x, size.y), MP2.get_sprite(_button_image_checked).texture, ScaleMode.ScaleToFit);
            }

            if (Utils.PointInRectangle(Input.mousePosition, new Rect(position.x, position.y - size.y, size.x, size.y)))
            {
                setToolTip(tooltip_text);

                if (MP2.IsCoolDownReady() && Input.GetMouseButtonUp(0))
                {
                    MP2.ResetCountdown();
                    Select();
                }
            }

            Rect check_rect_text = new Rect(position.x + size.x + size.x * .5f, yy, 200 * dpi_scale, size.y);
            GUIStyle guiStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = (int)(16 * dpi_scale),
                alignment = TextAnchor.MiddleLeft
            };
            GUI.color = Color.black;
            GUI.Label(check_rect_text, text, guiStyle);
            GUI.color = Color.white;
        }

        public void Select()
        {
            selected = !selected;
            on_mouse_click?.Invoke();
        }

        public void SetPosition(float x, float y)
        {
            position.x = x;
            position.y = y;
        }

        public void SetTileSize(float x, float y)
        {
            size.x = x;
            size.y = y;
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

    public class UI_Grid_Buttons : UI_Item
    {
        public List<UI_Button> buttons;
        private Vector2 position = new Vector2();
        private Vector2 size = new Vector2(100, 100);
        private Vector2 tile_size = new Vector2(32, 32);
        private Vector2 spacing = new Vector2(8, 8);

        public UI_Grid_Buttons()
        {
            buttons = new List<UI_Button>();
        }

        public void AddButton(UI_Button button)
        {
            buttons.Add(button);
        }

        public void AddButtons(List<UI_Button> new_buttons)
        {
            foreach (var button in new_buttons)
            {
                AddButton(button);
            }
        }

        public void SetPosition(Vector2 pos)
        {
            position = pos;
        }

        public void SetPosition(float x, float y)
        {
            position = new Vector2(x, y);
        }

        public void SetSize(Vector2 grid_size)
        {
            size = grid_size;
        }

        public void SetSize(float width, float height)
        {
            size = new Vector2(width, height);
        }

        public void DrawGrid(float scale = 1)
        {
            if (!visible)
                return;

            Texture2D texture = new Texture2D(1, 1);
            Color color = new Color(218f / 255f, 218f / 255f, 224f / 255f);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            GUI.DrawTexture(new Rect(position.x, Screen.height - position.y, size.x * scale, size.y * scale), texture);
            UnityEngine.Object.Destroy(texture);

            texture = new Texture2D(1, 1);
            color = new Color(170f / 255f, 170f / 255f, 170f / 255f);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            GUI.DrawTexture(new Rect(position.x + 4f * scale, Screen.height - position.y + 4f * scale, size.x * scale - 8f * scale, size.y * scale - 8f * scale), texture);
            UnityEngine.Object.Destroy(texture);

            int index = 0;
            foreach (var button in buttons)
            {
                button.SetPosition(
                    position.x + (spacing.x * scale + ((index * tile_size.x) + (index * spacing.x)) * scale),
                    position.y - (spacing.y * scale)
                    );
                button.SetTileSize(tile_size.x * scale, tile_size.y * scale);
                button.DrawSprite();
                index += 1;
            }
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

    public class UI_Tab : UI_Item
    {
        public bool selected = false;
        public List<UI_Item> objects_to_control;
        public Vector2 tab_size = new Vector2(64, 64);
        public Vector2 position;
        public bool auto_invert_y = true;

        public string tab_selected = "ui_rounded_tabs";
        public string tab_unselected = "ui_rounded_tabs_fade";
        public string sprite = "";

        public delegate void OnMouseClick();

        public string my_tab_name;
        public MP2Window mp2window { get; set; }

        public UI_Tab_Bar my_tab_bar;

        public void SetPosition(float x, float y)
        {
            position.x = x;
            position.y = y;
        }

        public void SetTileSize(float x, float y)
        {
            tab_size.x = x;
            tab_size.y = y;
        }

        public void DrawSprite()
        {
            if (!visible)
                return;

            float yy = position.y;
            if (auto_invert_y)
                yy = Screen.height - position.y;

            Rect tab_rect = new Rect(position.x, yy, tab_size.x, tab_size.y);
            Rect select_rect = new Rect(position.x, position.y - tab_size.y, tab_size.x, tab_size.y);

            if (Utils.PointInRectangle(Input.mousePosition, select_rect))
            {
                setToolTip(my_tab_name);
            }

            if (selected || Utils.PointInRectangle(Input.mousePosition, select_rect))
            {
                GUI.DrawTexture(tab_rect, MP2.get_sprite(tab_selected).texture, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.DrawTexture(tab_rect, MP2.get_sprite(tab_unselected).texture, ScaleMode.ScaleToFit);
            }

            if (sprite != "")
                GUI.DrawTexture(new Rect(position.x + tab_size.x * .1f, yy + tab_size.y * .1f, tab_size.x * .8f, tab_size.y * .8f), MP2.get_sprite(sprite).texture, ScaleMode.ScaleToFit);

            if (MP2.IsCoolDownReady() && Utils.PointInRectangle(Input.mousePosition, select_rect) && Input.GetMouseButtonUp(0))
            {
                MP2.ResetCountdown();
                Select();
            }
        }

        public void Select()
        {
            if (my_tab_bar != null)
            {
                foreach (UI_Tab ui_tab in my_tab_bar.tabs)
                {
                    ui_tab.selected = false;
                    ui_tab.HideContent();
                }
            }

            selected = true;
            ShowContent();

            if (mp2window != null && my_tab_name != "")
            {
                // mp2window.SetWindowTitle(my_tab_name);
            }
        }

        public override void HideContent()
        {
            if (objects_to_control != null)
            {
                foreach (var obj in objects_to_control)
                {
                    obj.HideContent();
                }
            }
        }

        public override void ShowContent()
        {
            if (objects_to_control != null)
            {
                foreach (var obj in objects_to_control)
                {
                    obj.ShowContent();
                }
            }
        }
    }

    public class UI_Tab_Bar : UI_Item
    {
        public List<UI_Tab> tabs = new List<UI_Tab>();
        public UI_Tab selected_index;
        public Vector2 tab_size = new Vector2(64, 64);
        public Vector2 position;
        public float width = 200f;

        public UI_Tab_Bar(GameObject parent)
        {
            this.parent = parent;
        }

        public UI_Tab GetTab(int index)
        {
            return tabs[index];
        }

        public UI_Tab AddTab(string icon_sprite, List<UI_Item> object_to_control_, string tab_name = "")
        {
            UI_Tab tab = new UI_Tab
            {
                sprite = icon_sprite,
                my_tab_bar = this,
                my_tab_name = tab_name,
            };
            tab.parent = this.parent;

            tab.objects_to_control = new List<UI_Item>();
            tab.objects_to_control.AddRange(object_to_control_);

            tabs.Add(tab);
            return tab;
        }

        public UI_Tab AddTab(string icon_sprite, UI_Item object_to_control_, string tab_name = "")
        {
            UI_Tab tab = new UI_Tab
            {
                sprite = icon_sprite,
                my_tab_bar = this,
                my_tab_name = tab_name,
                objects_to_control = new List<UI_Item>
            {
                object_to_control_
            }
            };
            tab.parent = this.parent;

            tabs.Add(tab);
            return tab;
        }

        public void SetPosition(float x, float y)
        {
            SetPosition(new Vector2(x, y));
        }

        public void SetPosition(Vector2 position)
        {
            this.position = position;
        }

        public void SetWidth(float width)
        {
            this.width = width;
        }

        public void DrawTabs(float scale)
        {
            if (!visible)
                return;

            //Texture2D texture = new Texture2D(1, 1);
            //UnityEngine.Color color = new UnityEngine.Color(218f / 255f, 218f / 255f, 224f / 255f);
            //texture.SetPixel(0, 0, color);
            //texture.Apply();
            //GUI.DrawTexture(new Rect(position.x, Screen.height - position.y, this.width * scale, this.tab_size.y * scale), texture);

            int index = 0;
            foreach (UI_Tab tab in tabs)
            {
                tab.SetPosition(
                    position.x + ((index * tab_size.x) * scale) + (index * 1 * scale),
                    position.y
                    );
                tab.SetTileSize(tab_size.x * scale, tab_size.y * scale);
                tab.DrawSprite();
                index += 1;
            }
        }
    }
}