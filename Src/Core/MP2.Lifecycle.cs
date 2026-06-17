using HarmonyLib;

using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.Video;

namespace MaterialPainter2
{
    public partial class MP2
    {
        public MP2()
        {
            try
            {
                MPDebug("LAUNCHING MP2", always_show: true);
                _local_mods_directory = GetParkitectModsDirectory(); // GameController.modsPath;

                string[] folders =
                        {
            GetCustomDirectory(),
            GetCustomVideoDirectory(),
            GetCustomImageDirectory()
        };

                foreach (var folder in folders)
                {
                    try
                    {
                        DirectoryInfo dirInfo = Directory.CreateDirectory(folder);
                        MPDebug($"[FolderSetup] Verified/created: {dirInfo.FullName}", always_show: true);
                    }
                    catch (System.Exception ex)
                    {
                        MPDebug($"Failed to create folder {folder}: {ex.Message}", always_show: true);
                    }
                }

                foreach (var folder in folders)
                {
                    if (!Directory.Exists(folder))
                    {
                        MPDebug($"Folder {folder} does not exist!", always_show: true);
                    }
                }


                for (int i = 1; i <= 3; i++)
                {
                    var default_video = GetBundledDefaultVideoPath(i);
                    var custom_video = GetDefaultCustomVideoPath(i);

                    if (!File.Exists(custom_video) && File.Exists(default_video))
                    {
                        File.Copy(default_video, custom_video, false);
                    }
                }

                var fallback_image = GetBundledFallbackImagePath();
                var custom_fallback_image = GetFallbackImagePath();
                if (!File.Exists(custom_fallback_image) && File.Exists(fallback_image))
                {
                    File.Copy(fallback_image, custom_fallback_image, false);
                }

                if (File.Exists(GetDebugFlagPath()))
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
                custom_images = new Dictionary<string, Texture2D>();
                bundled_textures = new Dictionary<string, Texture2D>();
            }
            catch (System.Exception ex)
            {
                MPDebug($"Something went terribly wrong at startup: {ex.Message}", always_show: true);
            }
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
            var loadedAB = AssetBundle.LoadFromFile(GetAssetBundlePath());

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
            go.name = CONTROLLER_GAME_OBJECT_NAME;
            controller = go.AddComponent<MP2_Controller>();
            construct_window_toggle = go.AddComponent<ConstructWindowToggle>();

            Sprite questionSprite = get_sprite("tex_question");

            material_brushes = new List<MaterialType>
            {
                new MaterialType("None", get_sprite("icon_none"), (int)MaterialBrush.None),
                new MaterialType("Water", get_sprite("icon_water"), (int)MaterialBrush.Water),
                new MaterialType("Lava", get_sprite("icon_lava"), (int)MaterialBrush.Lava),
                new MaterialType("Glass", get_sprite("icon_glass"), (int)MaterialBrush.Glass),
                new MaterialType("Grass", get_sprite("icon_terrain_grass"), (int)MaterialBrush.TriplanarTerrainGrass),
                new MaterialType("Dirt", get_sprite("icon_terrain_dirt"), (int)MaterialBrush.TriplanarTerrainDirt),
                new MaterialType("Stone", get_sprite("icon_terrain_stone"), (int)MaterialBrush.TriplanarTerrainStone),
                new MaterialType("Snow", get_sprite("icon_terrain_snow"), (int)MaterialBrush.TriplanarTerrainSnow),
                new MaterialType("Sand", get_sprite("icon_terrain_sand"), (int)MaterialBrush.TriplanarTerrainSand),
                new MaterialType("Ice", get_sprite("icon_terrain_ice"), (int)MaterialBrush.TriplanarTerrainIce),
                new MaterialType("Lava Rock", get_sprite("icon_terrain_lavarock"), (int)MaterialBrush.TriplanarTerrainLavarock),
                new MaterialType("Ash", get_sprite("icon_terrain_ash"), (int)MaterialBrush.TriplanarTerrainAsh),
                new MaterialType("Cracked Soil", get_sprite("icon_terrain_cracked_soil"), (int)MaterialBrush.TriplanarTerrainCrackedSoil),
                new MaterialType("Dead Grass", get_sprite("icon_terrain_dead_grass"), (int)MaterialBrush.TriplanarTerrainDeadGrass),
                new MaterialType("Blueprint", get_sprite("icon_terrain_blueprint"), (int)MaterialBrush.TriplanarTerrainBlueprint),
                new MaterialType("Terrain", questionSprite, (int)MaterialBrush.Terrain),
                new MaterialType("Underground", questionSprite, (int)MaterialBrush.UndergroundTerrain),
                new MaterialType("Data View", questionSprite, (int)MaterialBrush.DataView),
                new MaterialType("Selected", questionSprite, (int)MaterialBrush.Selected),
                new MaterialType("Deleted", questionSprite, (int)MaterialBrush.Deleted),
                new MaterialType("Scene Ghost", questionSprite, (int)MaterialBrush.SceneGhost),
                new MaterialType("Deco Glow", questionSprite, (int)MaterialBrush.DecoGlow),
                new MaterialType("Collision Glow", questionSprite, (int)MaterialBrush.CollisionGlow),
                new MaterialType("Ride Light", questionSprite, (int)MaterialBrush.RideLight),
                new MaterialType("Coaster Stats", questionSprite, (int)MaterialBrush.CoasterStats),
                new MaterialType("Waterfall", get_sprite("icon_effect_waterfall"), (int)MaterialBrush.Waterfall),
                new MaterialType("Black Hole", get_sprite("icon_effect_black_hole_core"), (int)MaterialBrush.BlackHoleCore),
                new MaterialType("Accretion Disc", get_sprite("icon_effect_black_hole_disc"), (int)MaterialBrush.BlackHoleDisc),
                new MaterialType("Invisible", get_sprite("icon_invisible"), (int)MaterialBrush.InvisiblePreview),
                new MaterialType("Video", get_sprite("icon_video1"), (int)MaterialBrush.Video),
                new MaterialType("Image", get_sprite("icon_video2"), (int)MaterialBrush.Image),
            };

            loadedAB.Unload(false);
            MPDebug("Loaded assetbundle!");

            EventManager.Instance.OnStartPlayingPark += new EventManager.OnStartPlayingParkHandler(PrepReassignMaterialsAfterLoadingSave);
            //EventManager.Instance.OnGameSaved += new EventManager.OnGameSavedHandler(PostSaveHooked);

            RefreshBrushesVideos();
            RefreshBrushesImages();
        }

        public override void onDisabled()
        {
            //_keys.UnregisterAll();
            if (controller != null)
                controller.ClearSharedMediaCaches();

            UnityEngine.Object.DestroyImmediate(go);
            sprites.Clear();
            videos.Clear();
            custom_images.Clear();
            bundled_textures.Clear();
            material_brushes.Clear();
            material_brushes_images.Clear();
            material_brushes_videos.Clear();

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
    }
}
