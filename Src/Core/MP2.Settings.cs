using UnityEngine;

namespace MaterialPainter2
{
    // The window's options and last brush, kept between games in the game's own preferences.
    public partial class MP2
    {
        private const string PREFS_PREFIX = MOD_IDENTIFIER + ".";

        public static void LoadSettings()
        {
            _setting_drag_select = PlayerPrefs.GetInt(PREFS_PREFIX + "drag_select", 0) != 0;
            _setting_target_supports = PlayerPrefs.GetInt(PREFS_PREFIX + "target_supports", 0) != 0;
            _setting_go_wild = PlayerPrefs.GetInt(PREFS_PREFIX + "go_wild", 0) != 0;
            last_brush = PlayerPrefs.GetInt(PREFS_PREFIX + "last_brush", (int)MaterialBrush.None);
            last_brush_custom = PlayerPrefs.GetString(PREFS_PREFIX + "last_brush_custom", "");
        }

        public static void SaveSettings()
        {
            PlayerPrefs.SetInt(PREFS_PREFIX + "drag_select", _setting_drag_select ? 1 : 0);
            PlayerPrefs.SetInt(PREFS_PREFIX + "target_supports", _setting_target_supports ? 1 : 0);
            PlayerPrefs.SetInt(PREFS_PREFIX + "go_wild", _setting_go_wild ? 1 : 0);
            PlayerPrefs.SetInt(PREFS_PREFIX + "last_brush", last_brush);
            PlayerPrefs.SetString(PREFS_PREFIX + "last_brush_custom", last_brush_custom ?? "");
            PlayerPrefs.Save();
        }
    }
}
