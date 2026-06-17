using Parkitect.UI;

using System;
using System.IO;
using System.Reflection;

using UnityEngine;

namespace MaterialPainter2
{
    public partial class MP2
    {
        public const string VERSION_NUMBER = "260617";
        public const string MOD_IDENTIFIER = "MaterialPainter";
        public const string MOD_DISPLAY_NAME = "Material Painter";
        public const string MOD_FOLDER_NAME = "MaterialPainter2";
        public const string SAVE_DATA_KEY = "MaterialPainter2";
        public const string SAVE_SCHEMA_KEY = "_schema";
        public const int CURRENT_SAVE_SCHEMA_VERSION = 3;
        public const string VIDEO_FILE_EXTENSION = ".mp4";

        private const string LOG_FILE_NAME = "MaterialPainterLog.txt";
        private const string LOG_PREFIX = MOD_DISPLAY_NAME + ": ";
        private const string DEBUG_FLAG_FILE_NAME = "mp_debug";
        private const string CUSTOM_FOLDER_NAME = "Custom";
        private const string VIDEOS_FOLDER_NAME = "Videos";
        private const string IMAGES_FOLDER_NAME = "Images";
        private const string RES_FOLDER_NAME = "Res";
        private const string ASSET_BUNDLE_FILE_NAME = "materialpainter.assets";
        private const string DEFAULT_VIDEO_PREFIX = "video-default-";
        private const string CUSTOM_VIDEO_PREFIX = "video-";
        private const string FALLBACK_VIDEO_NAME = "video-0";
        private const string CONTROLLER_GAME_OBJECT_NAME = "MP2 GameObject";

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
                UnityEngine.Debug.LogWarning(LOG_PREFIX + debug_string);

                if (_local_mods_directory != "")
                    File.AppendAllText(GetLogFilePath(), LOG_PREFIX + debug_string + "\n");
            }
        }

        public static string GetWindowTitle(string title)
        {
            return MOD_DISPLAY_NAME + " - " + title;
        }

        public static string GetCustomVideoFilePath(string videoName)
        {
            return CombineLocalModsPath(MOD_FOLDER_NAME, CUSTOM_FOLDER_NAME, VIDEOS_FOLDER_NAME, videoName + VIDEO_FILE_EXTENSION);
        }

        public static string GetFallbackVideoPath()
        {
            return GetCustomVideoFilePath(FALLBACK_VIDEO_NAME);
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

        private static string GetCustomDirectory()
        {
            return CombineLocalModsPath(MOD_FOLDER_NAME, CUSTOM_FOLDER_NAME);
        }

        private static string GetCustomVideoDirectory()
        {
            return CombineLocalModsPath(MOD_FOLDER_NAME, CUSTOM_FOLDER_NAME, VIDEOS_FOLDER_NAME);
        }

        private static string GetCustomImageDirectory()
        {
            return CombineLocalModsPath(MOD_FOLDER_NAME, CUSTOM_FOLDER_NAME, IMAGES_FOLDER_NAME);
        }

        private static string GetDefaultCustomVideoPath(int index)
        {
            return GetCustomVideoFilePath(CUSTOM_VIDEO_PREFIX + index);
        }

        private static string GetBundledDefaultVideoPath(int index)
        {
            return CombinePath(GetAssemblyDirectory(), RES_FOLDER_NAME, VIDEOS_FOLDER_NAME, DEFAULT_VIDEO_PREFIX + index + VIDEO_FILE_EXTENSION);
        }

        private static string GetAssetBundlePath()
        {
            return CombinePath(_material_painter_directory, RES_FOLDER_NAME, ASSET_BUNDLE_FILE_NAME);
        }

        private static string GetLogFilePath()
        {
            return CombinePath(_local_mods_directory, LOG_FILE_NAME);
        }

        private static string GetDebugFlagPath()
        {
            return CombinePath(_local_mods_directory, DEBUG_FLAG_FILE_NAME);
        }

        private static string GetParkitectModsDirectory()
        {
            string documentsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return EnsureTrailingSlash(CombinePath(documentsDirectory, "Parkitect", "Mods"));
        }

        private static string GetAssemblyDirectory()
        {
            return System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        private static string CombineLocalModsPath(params string[] parts)
        {
            string[] pathParts = new string[parts.Length + 1];
            pathParts[0] = _local_mods_directory;
            Array.Copy(parts, 0, pathParts, 1, parts.Length);
            return CombinePath(pathParts);
        }

        private static string CombinePath(params string[] parts)
        {
            return NormalizePath(System.IO.Path.Combine(parts));
        }

        private static string EnsureTrailingSlash(string path)
        {
            return path.EndsWith("/") ? path : path + "/";
        }

        private static string NormalizePath(string path)
        {
            return path.Replace("\\", "/");
        }
    }
}
