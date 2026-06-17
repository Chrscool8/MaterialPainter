using HarmonyLib;

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Video;

namespace MaterialPainter2
{
    public enum MaterialBrush
    {
        None = 0,
        Water = 1,
        Lava = 2,
        Glass = 3,
        Invisible = 4,
        Terrain = 5,
        InvisiblePreview = 6,
        Video = 7,
        Image = 8,
        //Texture = ?,
    }

    public class MaterialType
    {
        public string name { get; set; }
        public Sprite preview { get; set; }
        public int id { get; set; }
        public string id_string { get; set; }

        public MaterialType(string name, Sprite preview, int id, string id_string = "")
        {
            this.name = name;
            this.preview = preview;
            this.id = id;
            this.id_string = id_string;
        }
    }

    public partial class MP2 : AbstractMod, IModSettings
    {
        public override string getIdentifier() => MOD_IDENTIFIER;

        public override string getName() => MOD_DISPLAY_NAME;

        public override string getDescription() => @"The long awaited mod is here! Transform the materials of most objects into water, lava, and glass, make them invisible, or more! Lightly a work in progress. Have fun!";

        public override string getVersionNumber() => VERSION_NUMBER;

        public override bool isMultiplayerModeCompatible() => true;

        public override bool isRequiredByAllPlayersInMultiplayerMode() => false;

        public GameObject go { get; private set; }
        public static MP2 Instance;
        private Harmony _harmony;

        //private KeybindManager _keys;
        public static List<MaterialType> material_brushes { get; set; }
        public static Dictionary<string, MaterialType> material_brushes_images { get; set; } = new Dictionary<string, MaterialType>();
        public static Dictionary<string, MaterialType> material_brushes_videos { get; set; } = new Dictionary<string, MaterialType>();

        public static MP2_Controller controller { get; set; }
        public static ToolbarButton window_button { get; set; }
        public ConstructWindowToggle construct_window_toggle { get; set; }
        private static Dictionary<string, Sprite> sprites { get; set; }
        private static Dictionary<string, VideoClip> videos { get; set; }
        private static Dictionary<string, Texture2D> custom_images { get; set; }

        public static int selected_brush { get; set; }
        public static string selected_brush_custom { get; set; }

        private static bool debug_mode = false;
        public static float cooldownDuration = .01f;
        private static float lastExecutionTime = -1;
        public static string current_file_path = "";
        public static bool MOD_ENABLED = false;

        public static string _local_mods_directory = "";
        public static string _material_painter_directory = "";

        public static bool _setting_drag_select = false;
        public static bool _setting_target_supports = false;
        public static bool pipe_active = false;
    }
}
