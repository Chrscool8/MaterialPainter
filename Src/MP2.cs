using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;

using HarmonyLib;

using MiniJSON;

using Newtonsoft.Json;

using Parkitect.UI;

using UnityEngine;
using UnityEngine.Video;

using static GameController;

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

    public class MP2 : AbstractMod, IModSettings
    {
        public const string VERSION_NUMBER = "241203";

        public override string getIdentifier() => "MaterialPainter";

        public override string getName() => "Material Painter";

        public override string getDescription() => @"The long awaited mod is here! Transform the materials of most objects into water, lava, and glass, make them invisible, or more! Heavily a work in progress. Have fun!";

        public override string getVersionNumber() => VERSION_NUMBER;

        public override bool isMultiplayerModeCompatible() => true;

        public override bool isRequiredByAllPlayersInMultiplayerMode() => false;

        public GameObject go { get; private set; }
        public static MP2 Instance;
        private Harmony _harmony;

        //private KeybindManager _keys;
        public static List<MaterialType> material_brushes { get; set; }
        public static List<MaterialType> material_brushes_images { get; set; } = new List<MaterialType>();
        public static Dictionary<string, MaterialType> material_brushes_videos { get; set; } = new Dictionary<string, MaterialType>();

        public static MP2_Controller controller { get; set; }
        public static ToolbarButton window_button { get; set; }
        public ConstructWindowToggle construct_window_toggle { get; set; }
        private static Dictionary<string, Sprite> sprites { get; set; }
        private static Dictionary<string, VideoClip> videos { get; set; }

        public static int selected_brush { get; set; }
        public static string selected_brush_custom { get; set; }

        private static bool debug_mode = false;
        public static float cooldownDuration = .01f;
        private static float lastExecutionTime = -1;
        public static string current_file_path = "";
        public static bool MOD_ENABLED = false;

        public static Dictionary<string, VideoPlayer> cached_videos;

        public static string _local_mods_directory = "";
        public static string _material_painter_directory = "";

        public static bool _setting_drag_select = false;
        public static bool _setting_target_supports = false;
        public static bool pipe_active = false;

        public static bool IsCoolDownReady()
        {
            return (Time.unscaledTime - lastExecutionTime >= cooldownDuration || lastExecutionTime == -1);
        }

        public static void ResetCountdown()
        {
            lastExecutionTime = Time.unscaledTime;
        }

        public static void MPDebug(string debug_string, bool always_show = false)
        {
            if (debug_mode || always_show)
            {
                UnityEngine.Debug.LogWarning("Material Painter: " + debug_string);

                if (_local_mods_directory != "")
                    File.AppendAllText(_local_mods_directory + "/MaterialPainterLog.txt", "Material Painter: " + debug_string + "\n");
            }
        }

        public static Sprite get_sprite(string name, Sprite def = null)
        {
            if (sprites.ContainsKey(name))
            {
                return sprites[name];
            }
            else
            {
                MPDebug("Couldn't load sprite: '" + name + "'");
                return def;
            }
        }

        public static VideoClip get_video(string name)
        {
            if (videos.ContainsKey(name))
            {
                MPDebug("Loading asset video: '" + name + "'");
                return videos[name];
            }
            else
            {
                MPDebug("Couldn't load video: '" + name + "'");
                return null;
            }
        }

        public static float get_dpi()
        {
            float num = Screen.dpi;
            if (Mathf.Approximately(num, 0f))
            {
                num = 72f;
            }
            float dpi_scale = DPIHelper.scaleDPI(num / 72f) / 1.3f * Settings.Instance.uiScale;

            return dpi_scale;
        }
        public MP2()
        {
            _local_mods_directory = GameController.modsPath; //NormalizePath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Parkitect/Mods/");
            Directory.CreateDirectory(_local_mods_directory + "MaterialPainter2/Custom/");
            Directory.CreateDirectory(_local_mods_directory + "MaterialPainter2/Custom/Videos/");
            Directory.CreateDirectory(_local_mods_directory + "MaterialPainter2/Custom/Images/");

            for (int i = 1; i <= 3; i++)
            {
                var default_video = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $"/Res/Videos/video-default-{i}.mp4";
                var custom_video = _local_mods_directory + $"MaterialPainter2/Custom/Videos/video-{i}.mp4";

                if (!File.Exists(custom_video) && File.Exists(default_video))
                {
                    File.Copy(default_video, custom_video, false);
                }
            }

            if (File.Exists(_local_mods_directory + "/mp_debug"))
            {
                debug_mode = true;
                MPDebug("Found debug flag file.");
            }

            if (!MOD_ENABLED)
            {
                _harmony = new Harmony(getIdentifier());
                _harmony.PatchAll();
                MOD_ENABLED = true;
                MPDebug(debug_string: "ENABLING MP2", always_show: true);
            }

            sprites = new Dictionary<string, Sprite>();
            videos = new Dictionary<string, VideoClip>();
            cached_videos = new Dictionary<string, VideoPlayer>();
        }

        public override void onEnabled()
        {
            Instance = this;

            if (!MOD_ENABLED)
            {
                _harmony = new Harmony(getIdentifier());
                _harmony.PatchAll();
                MOD_ENABLED = true;
                MPDebug(debug_string: "ENABLING MP2", always_show: true);
            }

            MPDebug("Modspath: " + GameController.modsPath, always_show: true);
            MPDebug("ModspathRel: " + GameController.modsPathRelative, always_show: true);

            RegisterHotkeys();

            _material_painter_directory = ModManager.Instance.getMod(getIdentifier()).path;
            var loadedAB = AssetBundle.LoadFromFile(_material_painter_directory + "/Res/materialpainter.assets");

            UnityEngine.Object[] objects = loadedAB.LoadAllAssets();
            for (int i = 0; i < objects.Length; i++)
            {
                var obj = objects[i];
                if (obj == null)
                {
                    MPDebug($"Tried to load null asset.");
                }
                else if (obj.GetType().ToString() == "UnityEngine.Texture2D")
                {
                    Texture2D texture = obj as Texture2D;
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    sprite.name = obj.name;
                    //sprite.texture.filterMode = FilterMode.Point;

                    MPDebug($"AB Load: ({sprite.GetType()}) {sprite.name}");

                    sprites.Add(sprite.name, sprite);
                }
                else if (obj.GetType().ToString() == "UnityEngine.Video.VideoClip")
                {
                    VideoClip clip = obj as VideoClip;

                    MPDebug($"AB Load: ({clip.GetType()}) {clip.name}");

                    videos.Add(obj.name, clip);
                }
                else
                {
                    MPDebug($"Asset Not Loaded. Name: '{obj.name}', Type: {obj.GetType()}");
                }
            }

            go = new GameObject();
            go.name = "MP2 GameObject";
            controller = go.AddComponent<MP2_Controller>();
            construct_window_toggle = go.AddComponent<ConstructWindowToggle>();

            material_brushes = new List<MaterialType>
            {
                new MaterialType("None", get_sprite("icon_none"), (int)MaterialBrush.None),
                new MaterialType("Water", get_sprite("icon_water"), (int)MaterialBrush.Water),
                new MaterialType("Lava", get_sprite("icon_lava"), (int)MaterialBrush.Lava),
                new MaterialType("Glass", get_sprite("icon_glass"), (int)MaterialBrush.Glass),
                new MaterialType("Invisible", get_sprite("icon_invisible"), (int)MaterialBrush.InvisiblePreview),
                new MaterialType("Video", get_sprite("icon_video1"), (int)MaterialBrush.Video),
                new MaterialType("Image", get_sprite("icon_video2"), (int)MaterialBrush.Image),
            };

            loadedAB.Unload(false);
            MPDebug("Loaded assetbundle!");

            EventManager.Instance.OnStartPlayingPark += new EventManager.OnStartPlayingParkHandler(PrepReassignMaterialsAfterLoadingSave);
            //EventManager.Instance.OnGameSaved += new EventManager.OnGameSavedHandler(PostSaveHooked);

            RefreshBrushesVideos();

            if (!File.Exists(_local_mods_directory + $"MaterialPainter2/Tools/ffmpeg.exe"))
            {
                MPDebug("ffmpeg not found.");

                if (!File.Exists(_local_mods_directory + $"MaterialPainter2/_ignore_ffmpeg"))
                    Window_SuggestDL.ConstructWindowPrefab();
            }
            else
                MPDebug("ffmpeg found.");
        }
        public static void download_ffmpeg()
        {
            if (FileDownloader.Instance != null)
            {
                string ffmpegDownloadUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-n7.0-latest-win64-lgpl-7.0.zip";
                var savePath = _local_mods_directory + $"MaterialPainter2/Tools/file.zip";

                CoroutineManager.Instance.StartCoroutine(FileDownloader.Instance.DownloadFile(ffmpegDownloadUrl, savePath, true, action_on_complete: move_ffmpeg));
            }
            else
            {
                MP2.MPDebug("FileDownloader instance is not available.");
            }
        }

        public static void move_ffmpeg()
        {
            string original_loc = _local_mods_directory + $"MaterialPainter2/Tools/ffmpeg-n7.0-latest-win64-lgpl-7.0/bin/ffmpeg.exe";
            if (File.Exists(original_loc))
            {
                File.Move(original_loc, _local_mods_directory + $"MaterialPainter2/Tools/ffmpeg.exe");
            }

            if (Directory.Exists(_local_mods_directory + $"MaterialPainter2/Tools/ffmpeg-n7.0-latest-win64-lgpl-7.0"))
            {
                // Delete the folder and all its contents
                Directory.Delete(_local_mods_directory + $"MaterialPainter2/Tools/ffmpeg-n7.0-latest-win64-lgpl-7.0", true);
            }

            RefreshBrushesVideos();
        }

        public static void ignore_ffmpeg()
        {
            FileInfo fileInfo = new FileInfo(_local_mods_directory + $"MaterialPainter2/_ignore_ffmpeg");
            using (FileStream fs = fileInfo.Create()) { }
        }

        public static void RefreshBrushesVideos()
        {
            string ffmpeg_path = _local_mods_directory + $"MaterialPainter2/Tools/ffmpeg.exe";

            material_brushes_videos.Clear();

            //material_brushes_videos["None"] = (new MaterialType("icon_none", get_sprite("icon_none"), (int)MaterialBrush.None));

            string[] files = Directory.GetFiles(_local_mods_directory + "MaterialPainter2/Custom/Videos/", "*.mp4", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                MPDebug($"Processing: {file}");

                ////////////////

                string outputFilePath = file.Replace(".mp4", ".png");
                string image_name = System.IO.Path.GetFileNameWithoutExtension(outputFilePath);

                if (File.Exists(ffmpeg_path))
                {
                    MPDebug("Found ffmpeg.");
                    string inputFilePath = file;
                    string timePosition = "00:00:01"; // one second
                    string ffmpegArgs = $"-i \"{inputFilePath}\" -ss {timePosition} -vframes 1 \"{outputFilePath}\"";

                    if (!File.Exists(outputFilePath))
                    {
                        MPDebug("Creating Thumbnail.");

                        try
                        {
                            ProcessStartInfo processStartInfo = new ProcessStartInfo
                            {
                                FileName = "ffmpeg",
                                Arguments = ffmpegArgs,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };

                            using (Process process = new Process())
                            {
                                process.StartInfo = processStartInfo;
                                process.OutputDataReceived += (sender, eventArgs) => Console.WriteLine(eventArgs.Data);
                                process.ErrorDataReceived += (sender, eventArgs) => Console.WriteLine(eventArgs.Data);

                                process.Start();
                                process.BeginOutputReadLine();
                                process.BeginErrorReadLine();

                                process.WaitForExit();
                            }
                        }
                        catch (Exception ex)
                        {
                            MP2.MPDebug("FFMPEG Messed up");
                            MP2.MPDebug("An error occurred: " + ex.Message);
                            MP2.MPDebug("Stack Trace: " + ex.StackTrace);
                        }
                    }
                }

                if (File.Exists(outputFilePath))
                {
                    MPDebug("Found thumb.");
                    if (!sprites.ContainsKey(image_name))
                    {
                        MPDebug("Loading as sprite.");
                        Texture2D texture = new Texture2D(2, 2);
                        byte[] bytes = System.IO.File.ReadAllBytes(outputFilePath);
                        texture.LoadImage(bytes);

                        if (texture == null)
                        {
                            MPDebug("Texture not found at path: " + outputFilePath);
                            return;
                        }
                        MPDebug("Loaded tex.");
                        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        MPDebug("Putting in list.");
                        sprites.Add(image_name, sprite);
                    }
                }
                else
                    MPDebug($"Didn't find {outputFilePath}");

                MaterialType new_type = new MaterialType(name: image_name, preview: sprites.GetValueOrDefault(image_name, null), id: (int)MaterialBrush.Video, id_string: image_name);
                if (!material_brushes_videos.ContainsKey(image_name))
                {
                    MPDebug($"Adding video brush: {image_name}");
                    material_brushes_videos.Add(image_name, new_type);
                }
                else
                {
                    MPDebug($"{image_name} already in mat brushes");
                }
            }
        }

        private string NormalizePath(string path)
        {
            // You may need a more complex normalization depending on your specific requirements
            return path.Replace("\\", "/").ToLowerInvariant();
        }

        public override void onDisabled()
        {
            //_keys.UnregisterAll();
            UnityEngine.Object.DestroyImmediate(go);
            sprites.Clear();
            videos.Clear();
            material_brushes.Clear();

            if (MOD_ENABLED)
            {
                _harmony.UnpatchAll(getIdentifier());
                MOD_ENABLED = false;
                MPDebug(debug_string: "DISABLING MP2", always_show: true);
            }

            EventManager.Instance.OnStartPlayingPark -= new EventManager.OnStartPlayingParkHandler(PrepReassignMaterialsAfterLoadingSave);
            //EventManager.Instance.OnGameSaved -= new EventManager.OnGameSavedHandler(PostSaveHooked);
        }

        public void RegisterHotkeys()
        {
            //_keys = new KeybindManager("MP2_KEYS", "Material Painter");
            //_keys.AddKeybind("toggleMPWindow", "Toggle MP Window", "Show or Hide the Material Painter Window", KeyCode.Y);
            //_keys.RegisterAll();
        }

        public void onDrawSettingsUI()
        {
        }

        public void onSettingsClosed()
        {
        }

        public void onSettingsOpened()
        {
        }

        public void PostSaveHooked()
        {
            MP2.MPDebug($"Post Save! {GameController.filename}");
        }

        public void PrepReassignMaterialsAfterLoadingSave()
        {
            CoroutineManager.DelayAction(3f, () =>
            {
                ReassignMaterialsAfterLoadingSave();
            });

            CoroutineManager.DelayAction(2.5f, () =>
            {
                ConvertLegacySpellsToMP();
            });
        }

        private List<string> LoadGZippedTextFile(string filePath)
        {
            List<string> lines = new List<string>();

            using (FileStream fs = File.OpenRead(filePath))
            using (GZipStream gzStream = new GZipStream(fs, CompressionMode.Decompress))
            using (StreamReader reader = new StreamReader(gzStream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            return lines;
        }

        [Serializable]
        public class UserData
        {
            public string name;
            public string value;
        }

        public void ReassignMaterialsAfterLoadingSave()
        {

            MP2.MPDebug("Assigning Materials From Save.");

            try
            {
                List<GameObject> allObjects = Utils.GetAllObjectsInScene();
                MPDebug($"Number of GOs: {allObjects.Count}");
                MPDebug($"Numbers of Serials: {GameController.Instance.getSerializedObjects().Count}");

                if (current_file_path == null || current_file_path == "")
                {
                    MPDebug("Bad current_file_path");
                    return;
                }

                bool set_any_new = false;

                {
                    string file_path = current_file_path;
                    MPDebug(file_path);

                    bool compressed = !System.IO.Path.GetExtension(current_file_path).Equals(".txt");

                    if (!compressed)
                    {
                        MPDebug("Uncompressed is not yet supported.");
                        return;
                    }

                    List<string> file_lines = LoadGZippedTextFile(file_path);
                    foreach (string line in file_lines)
                    {
                        Dictionary<string, object> dictionary = (Dictionary<string, object>)Json.Deserialize(line);

                        if (dictionary != null && dictionary.ContainsKey("MaterialPainter2"))
                        {
                            set_any_new = true;

                            string inner_json = (string)dictionary["MaterialPainter2"];

                            if (inner_json.Contains("_schema"))
                            {
                                Dictionary<string, string> myDictionary = new Dictionary<string, string>();
                                StringStringDictionary serializedDictionary = JsonConvert.DeserializeObject<StringStringDictionary>(inner_json);

                                foreach (var pair in serializedDictionary.pairs)
                                {
                                    myDictionary[pair.key] = pair.value;
                                }

                                MPDebug($"File Schema V{myDictionary["_schema"]}");

                                if (int.TryParse(myDictionary["_schema"], out int schema) && schema == 3)
                                {
                                    foreach (var obj in allObjects)
                                    {
                                        string key = obj.name + ":" + obj.transform.position.ToString();
                                        if (myDictionary.ContainsKey(key))
                                        {
                                            string brush_data = myDictionary[key];
                                            MPDebug($"{key}: {brush_data}");


                                            if (brush_data.Contains(":")) // has subbrush
                                            {
                                                string[] parts = brush_data.Split(':');
                                                if (int.TryParse(parts[0], out int brush_type) && parts[1] != "")
                                                {
                                                    controller.SetMaterial(obj.transform, brush_type, parts[1]);
                                                }
                                                else
                                                {
                                                    MPDebug($"Brush detail empty.");
                                                }
                                            }
                                            else //basic brush
                                            {
                                                if (int.TryParse(brush_data, out int value))
                                                {
                                                    controller.SetMaterial(obj.transform, value);
                                                }
                                                else
                                                {
                                                    MPDebug($"Invalid Brush: {value}");
                                                }
                                            }

                                            myDictionary.Remove(key);
                                        }
                                    }

                                    myDictionary.Remove("_schema");

                                    if (myDictionary.Count == 0)
                                        MP2.MPDebug("Every key in the dictionary was assigned!");
                                    else
                                    {
                                        MP2.MPDebug("The following items were not reassigned materials:");
                                        foreach (var (index, key, value) in myDictionary.Enumerate())
                                        {
                                            MP2.MPDebug($"{index} {key} {value}");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                MPDebug("File Schema V2");

                                Dictionary<string, int> myDictionary = new Dictionary<string, int>();
                                StringIntDictionary serializedDictionary = JsonConvert.DeserializeObject<StringIntDictionary>(inner_json);

                                foreach (var pair in serializedDictionary.pairs)
                                {
                                    myDictionary[pair.key] = pair.value;
                                }

                                foreach (var obj in allObjects)
                                {
                                    string key = obj.name + ":" + obj.transform.position.ToString();
                                    if (myDictionary.ContainsKey(key))
                                    {
                                        int previous_brush = myDictionary[key];
                                        controller.SetMaterial(obj.transform, previous_brush);
                                    }
                                }
                            }

                            selected_brush = 0;
                            selected_brush_custom = "";
                        }
                    }
                }

                if (!set_any_new && File.Exists(current_file_path + ".mat"))
                {
                    MPDebug("File Schema V1");

                    string file_path = current_file_path + ".mat";

                    MPDebug(file_path);
                    Dictionary<string, int> myDictionary = new Dictionary<string, int>();
                    if (file_path != "" && File.Exists(file_path))
                    {
                        string json = File.ReadAllText(file_path);
                        StringIntDictionary serializedDictionary = JsonConvert.DeserializeObject<StringIntDictionary>(json);
                        foreach (var pair in serializedDictionary.pairs)
                        {
                            myDictionary[pair.key] = pair.value;
                        }
                    }
                    foreach (var obj in allObjects)
                    {
                        string key = obj.name + ":" + obj.transform.position.ToString();
                        if (myDictionary.ContainsKey(key))
                        {
                            int previous_brush = myDictionary[key];
                            controller.SetMaterial(obj.transform, (int)MaterialBrush.None);
                            controller.SetMaterial(obj.transform, previous_brush);
                        }
                    }
                    selected_brush = 0;
                    selected_brush_custom = "";
                }
            }
            catch (Exception ex)
            {
                MP2.MPDebug("Loading Messed up!");
                MP2.MPDebug("An error occurred: " + ex.Message);
                MP2.MPDebug("Stack Trace: " + ex.StackTrace);
            }
        }
        public void ConvertLegacySpellsToMP()
        {
            MPDebug("Converting Legacy Spells to MP");

            List<GameObject> allObjects = Utils.GetAllObjectsInScene();
            MPDebug($"Number of GOs: {allObjects.Count}");

            MPDebug($"Numbers of Serials: {GameController.Instance.getSerializedObjects().Count}");

            foreach (GameObject _obj in allObjects)
            {
                GameObject obj = _obj;

                if (obj != null)
                {
                    CustomColors cc = obj.GetComponent<CustomColors>();

                    if (cc != null)
                    {
                        if (cc.getColors().Length > 0)
                        {
                            int brush = -1;
                            Color c = cc.getColors()[0];
                            if (Utils.ColorCloseToEqual(c, new Color(11.151f, 17.15f, 12.18f), .2f))
                            {
                                MPDebug("INVISIBLE SPELL LOCATED");
                                brush = (int)MaterialBrush.Invisible;
                            }
                            else if (Utils.ColorCloseToEqual(c, new Color(5.15f, 15.15f, 18.2f), .2f))
                            {
                                MPDebug("WATER SPELL LOCATED");
                                brush = (int)MaterialBrush.Water;
                            }
                            else if (Utils.ColorCloseToEqual(c, new Color(11.15f, 2.57f, 1.2f), .2f))
                            {
                                MPDebug("LAVA SPELL LOCATED");
                                brush = (int)MaterialBrush.Lava;
                            }
                            else if (Utils.ColorCloseToEqual(c, new Color(55.27f, 55.28f, 55.29f), .2f))
                            {
                                MPDebug("GLASS SPELL LOCATED");
                                brush = (int)MaterialBrush.Glass;
                            }
                            else if (c[0] > 1.0 || c[1] > 1.0 || c[2] > 1.0)
                            {
                                MPDebug($">1 - {obj.name} {c[0]} {c[1]} {c[2]} ");
                            }

                            if (brush != -1)
                            {
                                cc.setColor(new Color(1.0f, 1.0f, 1.0f, c.a), 0);

                                foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>())
                                {
                                    controller.SetMaterial(renderer.gameObject.transform, brush);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    [Serializable]
    public struct StringIntPair
    {
        public string key;
        public int value;
    }
    [Serializable]
    public struct StringStringPair
    {
        public string key;
        public String value;
    }

    [Serializable]
    public class StringIntDictionary
    {
        public List<StringIntPair> pairs = new List<StringIntPair>();
    }
    [Serializable]
    public class StringStringDictionary
    {
        public List<StringStringPair> pairs = new List<StringStringPair>();
    }

    [HarmonyPatch]
    public class CustomColorApplicationPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(CustomColorsUtility), "apply", parameters: new Type[] {
    typeof(Color[]), typeof(Renderer),typeof(bool), typeof(bool), typeof(int)});

        [HarmonyPrefix]
        private static bool apply(Color[] colors, Renderer renderer, bool forceApplyToAllMaterials, bool applyToParticleSystems = false, int slotIndexOffset = 0)
        {
            ChangedMarker cm = renderer.gameObject.GetComponent<ChangedMarker>();
            if (cm != null)
            {
                if (cm.GetCurrentBrush() == (int)MaterialBrush.Water || cm.GetCurrentBrush() == (int)MaterialBrush.Lava)
                {
                    //MP2.MPDebug("APPLY!");

                    Color c = colors[0];
                    c.a = 0.392156869f;

                    MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(materialPropertyBlock);
                    materialPropertyBlock.SetColor(Shader.PropertyToID("_Color"), c);
                    float H;
                    float S;
                    float V;
                    Color.RGBToHSV(c, out H, out S, out V);
                    c = Color.HSVToRGB(H, S, V / 2f);
                    c.a = 0.9019608f;
                    materialPropertyBlock.SetColor(Shader.PropertyToID("_Color2"), c);
                    materialPropertyBlock.SetColor(Shader.PropertyToID("_ShoreColor"), Color.HSVToRGB(H, S / 3f, V));
                    renderer.SetPropertyBlock(materialPropertyBlock);
                }
                else if (cm.GetCurrentBrush() == (int)MaterialBrush.Glass)
                {
                    //MP2.MPDebug("APPLY!");

                    Color c = colors[0];
                    c.a = 0.5f;

                    MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(materialPropertyBlock);
                    materialPropertyBlock.SetColor(Shader.PropertyToID("_Color"), c);
                    renderer.SetPropertyBlock(materialPropertyBlock);
                }
            }

            return true;
        }
    }

    [HarmonyPatch]
    public class SaveInjectorPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(GameController), "storeSavegameData", parameters: new Type[]
        {
            typeof(List<Dictionary<string, object>>), typeof(string), typeof(bool), typeof(bool)
        });

        [HarmonyPrefix]
        public static bool storeSavegameData(List<Dictionary<string, object>> data, string filePath, bool compress, bool isTemporaryFile)
        {
            MP2.MPDebug("Injection Point");
            MP2.MPDebug($"{filePath}");

            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            List<GameObject> objectsWithChangedMarker = new List<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.GetComponent<ChangedMarker>() != null)
                    objectsWithChangedMarker.Add(obj);
            }

            Dictionary<string, string> myDictionary = new Dictionary<string, string>();
            myDictionary["_schema"] = "3";

            foreach (GameObject obj in objectsWithChangedMarker)
            {
                MP2.MPDebug($"{obj.GetInstanceID()}");
                string key = obj.name + ":" + obj.transform.position.ToString();
                int value = obj.GetComponent<ChangedMarker>().GetCurrentBrush();
                if (value == (int)MaterialBrush.InvisiblePreview)
                {
                    value = (int)MaterialBrush.Invisible;
                }

                string detail = obj.GetComponent<ChangedMarker>().GetCurrentBrushString();
                if (detail == "")
                    myDictionary[key] = value.ToString();
                else
                    myDictionary[key] = value.ToString() + ":" + detail;


            }

            StringStringDictionary serializableDictionary = new StringStringDictionary();
            foreach (var kvp in myDictionary)
            {
                serializableDictionary.pairs.Add(new StringStringPair { key = kvp.Key, value = kvp.Value });
            }

            string json = JsonConvert.SerializeObject(serializableDictionary);

            Dictionary<string, object> dictionary = new Dictionary<string, object>
            {
                { "MaterialPainter2", json }
            };

            data.Add(dictionary);

            return true;
        }
    }

    [HarmonyPatch]
    public class LoadGetFileNamePatch
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Sall Good")]
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(Loader), "loadSavegame", parameters: new Type[] { typeof(string), typeof(GameController.GameMode), typeof(ParkSettings), typeof(bool), typeof(bool), typeof(bool), typeof(OnParkLoadedHandler),typeof(OnParkDeserializedHandler), typeof(SerializationContext.Context) });

        [HarmonyPrefix]
        public static void Prefix(string filePath, GameMode gameMode, ParkSettings settings, bool rememberFilePath, bool newPark, bool showNewParkUI = true, GameController.OnParkLoadedHandler onParkLoadedHandler = null, GameController.OnParkDeserializedHandler onParkDeserializedHandler = null, SerializationContext.Context context = (SerializationContext.Context)0)
        {
            MP2.MPDebug(filePath);
            MP2.current_file_path = filePath;
        }
    }

    public class CoroutineManager : MonoBehaviour
    {
        public static CoroutineManager instance;

        public static CoroutineManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("CoroutineManager");
                    instance = obj.AddComponent<CoroutineManager>();
                }
                return instance;
            }
        }

        public static void DelayAction(float delay, Action action)
        {
            Instance.StartCoroutine(DelayCoroutine(delay, action));
        }

        public static IEnumerator DelayCoroutine(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}

public static class DictionaryExtensions
{
    public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
    {
        return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
    }
}