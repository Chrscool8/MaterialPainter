using System.Collections.Generic;

using UnityEngine;

namespace MaterialPainter2
{
    public class Window_MultiSelector : MonoBehaviour
    {
        Dictionary<UI_PushButton, GameObject> buttons = new Dictionary<UI_PushButton, GameObject>();
        Vector2 click_position = Vector2.zero;
        bool alive = false;
        public Window_MultiSelector(List<GameObject> objs, Vector2 click_position)
        {
            MP2.MPDebug("Started WMS");
            MP2.MPDebug($"{objs.Count}");
            MP2.MPDebug($"{click_position}");


            this.click_position = click_position;

            foreach (var (index, obj) in objs.Enumerate())
            {
                if (obj == null)
                    continue;

                UI_PushButton button = new UI_PushButton(text: $"{index}: {obj.name}");
                button.on_mouse_click = () => { this.clicked(button); };
                buttons.Add(button, obj);
            }

            alive = true;
        }

        public void OnGui()
        {
            if (!alive)
                return;

            foreach (var (index, key, value) in buttons.Enumerate())
            {
                if (value == null)
                    continue;
                if (!alive)
                    return;
                Vector2 butt_position = click_position + new Vector2(0, index * key.size.y);
                key.SetSize(300 * MP2.get_dpi(), 30 * MP2.get_dpi());
                key.SetPosition(butt_position.x, butt_position.y);
                key.DrawButton();
            }
        }

        void clicked(UI_PushButton button)
        {

            if (buttons[button] != null)
                MP2.controller.OnObjectClicked(buttons[button]);
            close();
        }

        public void close()
        {
            MP2.MPDebug("closing wms");
            alive = false;
            //buttons.Clear();
            MP2_Controller.wms = null;
        }
    }
}
