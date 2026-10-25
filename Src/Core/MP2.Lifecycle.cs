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

        // Each step runs on its own, so one failure is logged and the rest still happen, instead of leaving the mod
        // half enabled (or half disabled, which then breaks the next enable).
        public override void onEnabled()
        {
            Instance = this;

            Safe.Run("patching", () =>
            {
                if (!MOD_ENABLED)
                {
                    _harmony = new Harmony(getIdentifier());
                    _harmony.PatchAll();
                    MOD_ENABLED = true;
                    MPDebug(debug_string: "ENABLING MP2", always_show: true);
                }
            });

            MPDebug("Modspath: " + GameController.modsPath, always_show: true);
            MPDebug("ModspathRel: " + GameController.modsPathRelative, always_show: true);

            Safe.Run("registering hotkeys", RegisterHotkeys);
            Safe.Run("loading settings", LoadSettings);
            Safe.Run("registering paint sync", PaintSync.Register);
            Safe.Run("loading asset bundle", LoadAssetBundle);

            Safe.Run("creating controller", () =>
            {
                go = new GameObject();
                go.name = CONTROLLER_GAME_OBJECT_NAME;
                controller = go.AddComponent<MP2_Controller>();
                construct_window_toggle = go.AddComponent<ConstructWindowToggle>();
            });

            Safe.Run("subscribing to park events", () =>
                EventManager.Instance.OnStartPlayingPark += new EventManager.OnStartPlayingParkHandler(OnStartPlayingPark));

            Safe.Run("loading videos", RefreshBrushesVideos);
            Safe.Run("loading images", RefreshBrushesImages);
        }

        private void LoadAssetBundle()
        {
            _material_painter_directory = ModManager.Instance.getMod(getIdentifier()).path;
            AssetBundle loadedAB = AssetBundle.LoadFromFile(GetAssetBundlePath());
            if (loadedAB == null)
            {
                MPDebug("Couldn't load asset bundle: " + GetAssetBundlePath(), always_show: true);
                return;
            }

            try
            {
                foreach (UnityEngine.Object obj in loadedAB.LoadAllAssets())
                {
                    if (obj is Texture2D texture)
                    {
                        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        sprite.name = obj.name;
                        sprites[sprite.name] = sprite;
                    }
                    else if (obj is VideoClip clip)
                    {
                        videos[obj.name] = clip;
                    }
                    else if (obj != null)
                    {
                        MPDebug($"Asset Not Loaded. Name: '{obj.name}', Type: {obj.GetType()}");
                    }
                }
            }
            finally
            {
                loadedAB.Unload(false);
            }

            MPDebug("Loaded assetbundle!");
        }

        public override void onDisabled()
        {
            // Unpatch first so the game stops calling into the mod while it's being torn down.
            Safe.Run("unpatching", () =>
            {
                if (MOD_ENABLED)
                {
                    _harmony.UnpatchAll(getIdentifier());
                    MOD_ENABLED = false;
                    MPDebug(debug_string: "DISABLING MP2", always_show: true);
                }
            });

            Safe.Run("unsubscribing from park events", () =>
                EventManager.Instance.OnStartPlayingPark -= new EventManager.OnStartPlayingParkHandler(OnStartPlayingPark));
            Safe.Run("unregistering paint sync", PaintSync.Unregister);
            Safe.Run("unregistering hotkeys", UnregisterHotkeys);
            Safe.Run("restoring painted objects", Painter.RestoreAll);
            Safe.Run("releasing brush materials", BrushRegistry.ReleaseCaches);
            Safe.Run("releasing media", MediaPaint.ClearCaches);
            Safe.Run("destroying controller", () => UnityEngine.Object.DestroyImmediate(go));
            Safe.Run("clearing assets", () =>
            {
                sprites.Clear();
                videos.Clear();
                custom_images.Clear();
                bundled_textures.Clear();
                material_brushes_images.Clear();
                material_brushes_videos.Clear();
            });
        }

        // Listed under the mod's name in the game's Controls settings, unbound until the player picks a key. The bottom
        // menu button listens for it (see ConstructWindowToggle).
        private void RegisterHotkeys()
        {
            toggleWindowKey = new KeyMapping(HOTKEY_TOGGLE_WINDOW, KeyCode.None, KeyCode.None)
            {
                keyName = "Open " + MOD_DISPLAY_NAME,
                keyDescription = "Opens or closes the " + MOD_DISPLAY_NAME + " window.",
            };
            ScriptableSingleton<InputManager>.Instance.registerKeyMapping(toggleWindowKey);
        }

        private void UnregisterHotkeys()
        {
            if (toggleWindowKey != null)
                ScriptableSingleton<InputManager>.Instance.unregisterKeyMapping(toggleWindowKey);
            toggleWindowKey = null;
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

        // Paints restore as each object starts (PaintRestorePatch). This later pass catches renderers that only
        // exist once the park has finished building, such as supports, and converts paints from older saves.
        public void OnStartPlayingPark()
        {
            // The path is only set by loading a save; take it so a later new park doesn't scan this save again.
            string savePath = current_file_path;
            current_file_path = "";
            LegacySaveImporter.Clear();

            // Other mods and the game also listen to this event; an error here would stop them from hearing it.
            Safe.Run("scheduling post-load pass", () => CoroutineManager.DelayAction(5f, () =>
            {
                Safe.Run("restoring paints", PaintStore.RestoreAll);
                Safe.Run("scanning for old paints", () => LegacySaveImporter.Scan(savePath));
                ConvertOldPaints();
            }));
        }

        public void ConvertOldPaints()
        {
            Safe.Run("converting old paints", LegacySaveImporter.Import);
            Safe.Run("converting old spells", LegacySaveImporter.ImportSpells);
        }
    }
}
