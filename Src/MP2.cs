using HarmonyLib;
using MaterialPainter2;
using ModIO;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Video;
using static GameController;
using static MaterialPainter2.TackSavePatch;

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
        public const string VERSION_NUMBER = "240324";

        public override string getIdentifier() => "MaterialPainter";

        public override string getName() => "Material Painter";

        public override string getDescription() => @"The long awaited mod is sort-of here! Transform the materials of most objects into water, lava, and glass, or make them invisible. The game wasn't designed for this. Things WILL break (visually). Have fun!";

        public override string getVersionNumber() => VERSION_NUMBER;

        public override bool isMultiplayerModeCompatible() => true;

        public override bool isRequiredByAllPlayersInMultiplayerMode() => false;

        public GameObject go { get; private set; }
        public static string _modPath = "";
        public static MP2 Instance;
        private Harmony _harmony;

        //private KeybindManager _keys;
        public static List<MaterialType> material_brushes { get; set; }

        public static MP2Controller controller { get; set; }
        public static MP2WindowButton window_button { get; set; }
        public ConstructWindowToggle construct_window_toggle { get; set; }
        private static Dictionary<string, Sprite> sprites { get; set; }
        public static int selected_brush { get; set; }
        private static bool debug_mode = false;
        public static float cooldownDuration = .01f;
        private static float lastExecutionTime = -1;
        public static string current_file_path = "";
        public static bool MOD_ENABLED = false;

        public static Dictionary<string, VideoPlayer> cached_videos;

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

                if (_modPath != "")
                    File.AppendAllText(_modPath + "/MaterialPainterLog.txt", "Material Painter: " + debug_string + "\n");
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

        public MP2()
        {
            if (File.Exists(GameController.modsPath + "/mp_debug"))
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

            RegisterHotkeys();

            _modPath = ModManager.Instance.getMod(getIdentifier()).path;
            var loadedAB = AssetBundle.LoadFromFile(_modPath + "/res/materialpainter.assets");

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

                    MPDebug('"' + sprite.name + "\"");

                    sprites.Add(sprite.name, sprite);
                }
                /*else if (obj.GetType().ToString() == "UnityEngine.Video.VideoClip")
                {
                    VideoClip clip = obj as VideoClip;

                    MPDebug('"' + obj.name + "\"");

                    videos.Add(obj.name, clip);
                }*/
                else
                {
                    MPDebug($"Asset Not Loaded. Name: '{obj.name}', Type: {obj.GetType().ToString()}");
                }
            }

            foreach (KeyValuePair<string, Sprite> kvp in sprites)
            {
                MPDebug("Key: " + kvp.Key + ", Value: " + kvp.Value.rect.ToString());
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
        }

        public override void onDisabled()
        {
            //_keys.UnregisterAll();
            UnityEngine.Object.DestroyImmediate(go);
            _modPath = "";
            sprites.Clear();
            material_brushes.Clear();

            if (MOD_ENABLED)
            {
                _harmony.UnpatchAll(getIdentifier());
                MOD_ENABLED = false;
                MPDebug(debug_string: "DISABLING MP2", always_show: true);
            }

            EventManager.Instance.OnStartPlayingPark -= new EventManager.OnStartPlayingParkHandler(PrepReassignMaterialsAfterLoadingSave);
            // EventManager.Instance.OnGameSaved might be a good alternative for the other patch.
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

        public void PrepReassignMaterialsAfterLoadingSave()
        {
            CoroutineManager.DelayAction(3f, () =>
            {
                ReassignMaterialsAfterLoadingSave();
            });
        }

        public void ReassignMaterialsAfterLoadingSave()
        {
            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            MPDebug($"Number of GOs: {allObjects.Length}");
            MPDebug($"Numbers of Serials: {GameController.Instance.getSerializedObjects().Count}");

            string file_path = MP2.current_file_path + ".mat";

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
                    MP2.controller.SetMaterial(obj.transform, previous_brush);
                }
            }
            MP2.selected_brush = 0;
        }
    }

    [HarmonyPatch]
    public class TackSavePatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(GameController), "saveGame", parameters: new Type[]
        {
           typeof( string ),typeof( bool ), typeof(bool ),typeof( SerializationContext.Context ),typeof( bool ),typeof( OnSaveCompleted )
        });

        [System.Serializable]
        public struct StringIntPair
        {
            public string key;
            public int value;
        }

        [System.Serializable]
        public class StringIntDictionary
        {
            public List<StringIntPair> pairs = new List<StringIntPair>();
        }

        [HarmonyPrefix]
        public static bool Prefix(string filePath, bool async = true, bool rememberAsCurrentlyLoadedSavegame = true, SerializationContext.Context context = (SerializationContext.Context)0, bool isTemporaryFile = false, OnSaveCompleted onSaveCompleted = null)
        {
            MP2.MPDebug("Prefix");

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
                string key = obj.name + ":" + obj.transform.position.ToString();
                int value = obj.GetComponent<ChangedMarker>().get_current_brush();
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
            File.WriteAllText(filePath + ".mat", json);

            return true;
        }
    }

    [HarmonyPatch]
    public class LoadGetFileNamePatch
    {
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

        public static void DelayAction(float delay, System.Action action)
        {
            Instance.StartCoroutine(DelayCoroutine(delay, action));
        }

        private static IEnumerator DelayCoroutine(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}