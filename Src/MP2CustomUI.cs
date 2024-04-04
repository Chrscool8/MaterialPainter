using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using VLB;

namespace MaterialPainter2
{
    public class UI_Button
    {
        public Vector2 position;
        public Vector2 size;

        public string _button_image_sprite = "checkbox_uncheck";
        public string _button_image_sprite_highlight = "icon_highlight";
        public string _button_image_sprite_select = "checkbox_check";

        public List<UI_Button> button_family;

        public delegate void OnMouseHover();

        private OnMouseHover on_mouse_hover;

        public delegate void OnMouseClick();

        private OnMouseClick on_mouse_click;

        public bool selected = false;

        public bool auto_invert_y = true;

        public UI_Button(GameObject parent = null, float x = 0, float y = 0, float size_x = 32, float size_y = 32,
            string button_image_sprite = "", string button_image_sprite_highlight = "", string button_image_sprite_select = "", OnMouseHover onMouseHover = null, OnMouseClick onMouseClick = null)
        {
            this.Setup(parent, x, y, size_x, size_y, button_image_sprite, button_image_sprite_highlight, button_image_sprite_select, onMouseHover, onMouseClick);
        }

        public UI_Button Setup(GameObject parent = null, float x = 0, float y = 0, float size_x = 32, float size_y = 32,
            string button_image_sprite = "", string button_image_sprite_highlight = "", string button_image_sprite_select = "", OnMouseHover onMouseHover = null, OnMouseClick onMouseClick = null)
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

            return this;
        }

        public void default_on_mouse_hover()
        {
        }

        public void default_on_mouse_click()
        {
        }

        public void draw_sprite()
        {
            float yy = position.y;
            if (auto_invert_y)
                yy = Screen.height - position.y;

            GUI.DrawTexture(new Rect(position.x, yy, size.x, size.y), MP2.get_sprite(_button_image_sprite).texture, ScaleMode.ScaleToFit);

            if (Utils.PointInRectangle(Input.mousePosition, new Rect(position.x, position.y - size.y, size.x, size.y)))
            {
                GUI.DrawTexture(new Rect(position.x, yy, size.x, size.y), MP2.get_sprite(_button_image_sprite_highlight).texture, ScaleMode.ScaleToFit);
            }

            if (selected)
            {
                GUI.DrawTexture(new Rect(position.x, yy, size.x, size.y), MP2.get_sprite(_button_image_sprite_select).texture, ScaleMode.ScaleToFit);
            }
        }

        public void set_position(float x, float y)
        {
            position.x = x;
            position.y = y;
        }

        public void set_tile_size(float x, float y)
        {
            size.x = x;
            size.y = y;
        }
    }

    public class UI_Grid_Buttons
    {
        public List<UI_Button> buttons;
        private Vector2 position = new Vector2();
        private Vector2 size = new Vector2(100, 100);
        private Vector2 tile_size = new Vector2(32, 32);
        private Vector2 spacing = new Vector2(8, 8);

        public UI_Grid_Buttons()
        { buttons = new List<UI_Button>(); }

        public void add_button(UI_Button button)
        {
            buttons.Add(button);
        }

        public void add_buttons(List<UI_Button> new_buttons)
        {
            buttons.AddRange(new_buttons);
        }

        public void set_position(Vector2 pos)
        {
            position = pos;
        }

        public void set_position(float x, float y)
        {
            position = new Vector2(x, y);
        }

        public void set_size(Vector2 grid_size)
        {
            size = grid_size;
        }

        public void set_size(float width, float height)
        {
            size = new Vector2(width, height);
        }

        public void draw_grid(float scale = 1)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.gray);
            texture.Apply();
            GUI.DrawTexture(new Rect(position.x, Screen.height - position.y, size.x * scale, size.y * scale), texture);

            int index = 0;
            foreach (var button in buttons)
            {
                button.set_position(
                    position.x + (spacing.x * .5f + ((index * tile_size.x) + (index * spacing.x)) * scale),
                    position.y - (spacing.y * .5f * scale)
                    );
                button.set_tile_size(tile_size.x * scale, tile_size.y * scale);
                button.draw_sprite();
                index += 1;
            }
        }
    }
}