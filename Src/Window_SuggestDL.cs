using Parkitect.UI;

using UnityEngine;

namespace MaterialPainter2
{
    public class Window_SuggestDL : UIWindow
    {
        public static Window_SuggestDL Instance { get; private set; }
        private UI_PushButton button_download;
        private UI_PushButton button_neveragain;
        private static UIWindowFrame windowSuggestInstance;

        public static Window_SuggestDL Window { get; private set; }

        public static Vector2 SuggestWindowSize = new Vector2(300, 200);

        public static Window_SuggestDL ConstructWindowPrefab()
        {
            MP2.MPDebug("ConstructWindowPrefab FFMPEG");
            var WindowPrefab = new GameObject(MP2.Instance.getName());
            WindowPrefab.SetActive(false);

            var rect = WindowPrefab.AddComponent<RectTransform>();
            rect.sizeDelta = SuggestWindowSize;

            //WindowPrefab.AddComponent<CanvasRenderer>();
            Window = WindowPrefab.AddComponent<Window_SuggestDL>();
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

            float dpi_scale = MP2.get_dpi();

            float left_offset = 28f * dpi_scale;
            float top_offset = 45.0f * dpi_scale;

            float button_width = 76f * dpi_scale * 1.25f;
            float button_height = 24f * dpi_scale * 1.25f;

            ToolTipper tt = GetComponent<ToolTipper>();
            tt.tooltip = "";

            GUIStyle text_guiStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(14 * dpi_scale),
                alignment = TextAnchor.UpperLeft
            };

            GUI.color = new Color(153, 168, 166);
            GUI.Label(new Rect(transform.parent.parent.position.x + left_offset, Screen.height - transform.parent.parent.position.y + top_offset, SuggestWindowSize.x * dpi_scale * 1.3f - left_offset * 2, SuggestWindowSize.y * dpi_scale * 1.3f),
                "(Probably Windows OSes only.) Material Painter can use ffmpeg for automatic creation of thumbnails in the Video Paints tab. " +
                "I can download it for you right now, if you'd like, or you could supply your own copy of the " +
                "exe and place it in 'Parkitect/Mods/MaterialPainter2/Tools/ffmpeg.exe'. If you want to generate " +
                "thumbs on your own, or just use it without them, you can also just ignore this suggestion.",
                text_guiStyle);

            GUI.color = Color.white;

            top_offset = SuggestWindowSize.y * dpi_scale + button_height;

            button_download.SetSize(button_width, button_height);
            button_download.SetPosition(transform.parent.parent.position.x + left_offset, transform.parent.parent.position.y - top_offset);
            button_download.DrawButton();

            button_neveragain.SetSize(button_width, button_height);
            button_neveragain.SetPosition(transform.parent.parent.position.x + SuggestWindowSize.x * dpi_scale * 1.3f - left_offset - button_width, transform.parent.parent.position.y - top_offset);
            button_neveragain.DrawButton();

            if (tt.tooltip != "")
            {
                GUIStyle tooltip_guiStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(14 * dpi_scale),
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

            button_download = new UI_PushButton(parent: gameObject, text: "Download", tooltip_text: "Automatically downloads from https://github.com/BtbN/FFmpeg-Builds", onMouseClick: () => { MP2.download_ffmpeg(); windowSuggestInstance.close(); windowSuggestInstance = null; });
            button_neveragain = new UI_PushButton(parent: gameObject, text: "Ignore", tooltip_text: "Stops reminding you about this until you delete 'Mods/MaterialPainter2/_ignore_ffmpeg'", onMouseClick: () => { MP2.ignore_ffmpeg(); windowSuggestInstance.close(); windowSuggestInstance = null; });

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
