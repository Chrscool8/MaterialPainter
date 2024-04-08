﻿using HarmonyLib;
using MiniJSON;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
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
        Video_1 = 7,
        Video_2 = 8,
        Video_3 = 9,
        //Texture = ?,
    }

    public class MaterialType
    {
        public string name { get; set; }
        public Sprite preview { get; set; }
        public int id { get; set; }

        public MaterialType(string name, Sprite preview, int id)
        {
            this.name = name;
            this.preview = preview;
            this.id = id;
        }
    }

    public class MP2 : AbstractMod, IModSettings
    {
        public const string VERSION_NUMBER = "240407";

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

        public static MP2Controller controller { get; set; }
        public static MP2WindowButton window_button { get; set; }
        public ConstructWindowToggle construct_window_toggle { get; set; }
        private static Dictionary<string, Sprite> sprites { get; set; }
        private static Dictionary<string, VideoClip> videos { get; set; }

        public static int selected_brush { get; set; }
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

        public static bool IsCoolDownReady()
        {
            return (Time.time - lastExecutionTime >= cooldownDuration || lastExecutionTime == -1);
        }

        public static void ResetCountdown()
        {
            lastExecutionTime = Time.time;
        }

        public static void MPDebug(string debug_string, bool always_show = false)
        {
            if (debug_mode || always_show)
            {
                Debug.LogWarning("Material Painter: " + debug_string);

                if (_local_mods_directory != "")
                    File.AppendAllText(_local_mods_directory + "/MaterialPainterLog.txt", "Material Painter: " + debug_string + "\n");
            }
        }

        public static Sprite get_sprite(string name)
        {
            if (sprites.ContainsKey(name))
            {
                return sprites[name];
            }
            else
            {
                MPDebug("Couldn't load sprite: '" + name + "'");
                return null;
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

        public MP2()
        {
            _local_mods_directory = GameController.modsPath; //NormalizePath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Parkitect/Mods/");
            Directory.CreateDirectory(_local_mods_directory + "MaterialPainter2/Custom/");

            for (int i = 1; i <= 3; i++)
            {
                var default_video = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $"/Res/Videos/video-default-{i}.mp4";
                var custom_video = _local_mods_directory + $"MaterialPainter2/Custom/video-{i}.mp4";

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
            var loadedAB = AssetBundle.LoadFromFile(_material_painter_directory + "/res/materialpainter.assets");

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
            controller = go.AddComponent<MP2Controller>();
            construct_window_toggle = go.AddComponent<ConstructWindowToggle>();

            material_brushes = new List<MaterialType>
            {
                new MaterialType("None", get_sprite("icon_none"), (int)MaterialBrush.None),
                new MaterialType("Water", get_sprite("icon_water"), (int)MaterialBrush.Water),
                new MaterialType("Lava", get_sprite("icon_lava"), (int)MaterialBrush.Lava),
                new MaterialType("Glass", get_sprite("icon_glass"), (int)MaterialBrush.Glass),
                new MaterialType("Invisible", get_sprite("icon_invisible"), (int)MaterialBrush.InvisiblePreview),
                new MaterialType("Video 1", get_sprite("icon_video1"), (int)MaterialBrush.Video_1),
                new MaterialType("Video 2", get_sprite("icon_video2"), (int)MaterialBrush.Video_2),
                new MaterialType("Video 3", get_sprite("icon_video3"), (int)MaterialBrush.Video_3),
            };

            loadedAB.Unload(false);
            MPDebug("Loaded assetbundle!");

            EventManager.Instance.OnStartPlayingPark += new EventManager.OnStartPlayingParkHandler(PrepReassignMaterialsAfterLoadingSave);
            //EventManager.Instance.OnGameSaved += new EventManager.OnGameSavedHandler(PostSaveHooked);
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
            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            MPDebug($"Number of GOs: {allObjects.Length}");
            MPDebug($"Numbers of Serials: {GameController.Instance.getSerializedObjects().Count}");

            if (current_file_path == null || current_file_path == "")
            {
                MP2.MPDebug("Bad current_file_path");
                return;
            }

            if (File.Exists(current_file_path + ".mat"))
            {
                MP2.MPDebug("Legacy Load");

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
            }
            else
            {
                MPDebug("Modern Load");

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
                    Dictionary<string, int> myDictionary = new Dictionary<string, int>();

                    if (dictionary != null && dictionary.ContainsKey("MaterialPainter2"))
                    {
                        string inner_json = (string)dictionary["MaterialPainter2"];

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
                        selected_brush = 0;
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
    public class StringIntDictionary
    {
        public List<StringIntPair> pairs = new List<StringIntPair>();
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

            Dictionary<string, int> myDictionary = new Dictionary<string, int>();

            foreach (GameObject obj in objectsWithChangedMarker)
            {
                MP2.MPDebug($"{obj.GetInstanceID()}");
                string key = obj.name + ":" + obj.transform.position.ToString();
                int value = obj.GetComponent<ChangedMarker>().GetCurrentBrush();
                if (value == (int)MaterialBrush.InvisiblePreview)
                {
                    value = (int)MaterialBrush.Invisible;
                }
                myDictionary[key] = value;
            }

            StringIntDictionary serializableDictionary = new StringIntDictionary();
            foreach (var kvp in myDictionary)
            {
                serializableDictionary.pairs.Add(new StringIntPair { key = kvp.Key, value = kvp.Value });
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
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(Loader), "loadSavegame", parameters: new Type[] { typeof(string), typeof(GameController.GameMode), typeof(ParkSettings), typeof(bool), typeof(bool), typeof(bool), typeof(OnParkLoadedHandler), typeof(SerializationContext.Context) });

        [HarmonyPrefix]
        public static void Prefix(string filePath, GameController.GameMode gameMode, ParkSettings settings, bool rememberFilePath, bool newPark, bool showNewParkUI = true, GameController.OnParkLoadedHandler onParkLoadedHandler = null, SerializationContext.Context context = (SerializationContext.Context)0)
        {
            MP2.MPDebug(filePath);
            MP2.current_file_path = filePath;
        }
    }

    public class CoroutineManager : MonoBehaviour
    {
        private static CoroutineManager instance;

        private static CoroutineManager Instance
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

        private static IEnumerator DelayCoroutine(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}