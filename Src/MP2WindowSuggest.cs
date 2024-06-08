﻿using Parkitect.UI;

using UnityEngine;

namespace MaterialPainter2
{
    public class MP2WindowSuggest : UIWindow
    {
        public static MP2WindowSuggest Instance { get; private set; }
        private UI_PushButton button_download;
        private UI_PushButton button_neveragain;
        private static UIWindowFrame windowSuggestInstance;


        public static MP2WindowSuggest Window { get; private set; }

        public static Vector2 SuggestWindowSize = new Vector2(200, 150);

        public static MP2WindowSuggest ConstructWindowPrefab()
        {
            MP2.MPDebug("ConstructWindowPrefab FFMPEG");
            var WindowPrefab = new GameObject(MP2.Instance.getName());
            WindowPrefab.SetActive(false);

            var rect = WindowPrefab.AddComponent<RectTransform>();
            rect.sizeDelta = SuggestWindowSize;

            //WindowPrefab.AddComponent<CanvasRenderer>();
            Window = WindowPrefab.AddComponent<MP2WindowSuggest>();
            //WindowPrefab.AddComponent<Canvas>();

            var windowSettings = WindowPrefab.AddComponent<UIWindowSettings>();
            windowSettings.closable = true;
            windowSettings.defaultWindowPosition = new Vector2(0.5f, 0.5f);
            windowSettings.title = "Material Painter - ffmpeg not found";
            windowSettings.uniqueTag = MP2.Instance.getName() + "ffmpeg";
            windowSettings.uniqueTagString = MP2.Instance.getName() + "ffmpeg";
            windowSettings.resizeability = 0;

            WindowPrefab.SetActive(true);

            var prefab = Window;
            if (prefab == null)
                MP2.MPDebug("Window prefab is null");

            if (ScriptableSingleton<UIAssetManager>.Instance.uiWindowFrameGO == null)
                MP2.MPDebug("uiWindowFrameGO is null, what?");

            windowSuggestInstance = UIWindowsController.Instance.spawnWindow(prefab, null);

            return Window;
        }

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
            float top_offset = 80.0f * dpi_scale;

            float button_width = 76 * dpi_scale;
            float button_height = 32 * dpi_scale;

            ToolTipper tt = GetComponent<ToolTipper>();
            tt.tooltip = "";

            button_download.SetSize(button_width, button_height);
            button_download.SetPosition(left_offset + transform.parent.parent.position.x, transform.parent.parent.position.y - (top_offset));
            button_download.DrawButton();

            top_offset += button_height * 1.5f;

            button_neveragain.SetSize(button_width, button_height);
            button_neveragain.SetPosition(left_offset + transform.parent.parent.position.x, transform.parent.parent.position.y - (top_offset));
            button_neveragain.DrawButton();

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
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Instance = this;
            MP2.MPDebug("Showing FFMPEG Window");

            button_download = new UI_PushButton(parent: gameObject, text: "Download", tooltip_text: "Automatically downloads from https://github.com/ffbinaries/ffbinaries-prebuilt");
            button_neveragain = new UI_PushButton(parent: gameObject, text: "Ignore", tooltip_text: "Stops reminding you about this unless you delete 'Mods/MaterialPainter2/_ignore_ffmpeg'");

            gameObject.AddComponent<ToolTipper>();

        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Instance = null;
            MP2.MPDebug("Hiding FFMPEG Window");

            button_download = null;
            button_neveragain = null;

            ToolTipper tt = GetComponent<ToolTipper>();
            if (tt != null)
                Destroy(tt);
        }
    }
}