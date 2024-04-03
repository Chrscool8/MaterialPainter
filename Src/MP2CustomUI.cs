using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VLB;

namespace MaterialPainter2
{
    public class Custom_UI : MonoBehaviour
    {
    }

    public class UI_Button : MonoBehaviour
    {
        public Vector2 position;
        public Vector2 size;

        public Sprite tex_tab_clicked;
        public Sprite tex_tab_unclicked;

        public UI_Button(float x = 0, float y = 0, float size_x = 32, float size_y = 32, Sprite tex_tab_clicked_ = null, Sprite tex_tab_unclicked_ = null)
        {
            this.Setup(x, y, size_x, size_y, tex_tab_clicked_, tex_tab_unclicked_);
        }

        public UI_Button Setup(float x = 0, float y = 0, float size_x = 32, float size_y = 32, Sprite tex_tab_clicked_ = null, Sprite tex_tab_unclicked_ = null)
        {
            position = new Vector2(x, y);
            size = new Vector2(size_x, size_y);

            tex_tab_clicked = tex_tab_clicked_;
            tex_tab_unclicked = tex_tab_unclicked_;

            return this;
        }

        public void OnGUI()
        {
            if (tex_tab_clicked == null)
            {
                tex_tab_clicked = MP2.get_sprite("ui_rounded_tabs");
                MP2.MPDebug($"clicked:");
                MP2.MPDebug($"{tex_tab_clicked}");
            }
            if (tex_tab_unclicked == null)
            {
                tex_tab_unclicked = MP2.get_sprite("ui_rounded_tabs_fade");
            }

            RectTransform canvas_rect = gameObject.GetComponent<RectTransform>();
            MP2.MPDebug($"{canvas_rect.position}, {canvas_rect.sizeDelta}");

            Rect rect = new Rect(new Vector2(canvas_rect.position.x, canvas_rect.position.y) + position, size);
            GUI.DrawTexture(rect, tex_tab_clicked.texture, ScaleMode.ScaleToFit);
        }
    }
}