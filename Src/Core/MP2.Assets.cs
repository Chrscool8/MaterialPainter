using System;
using System.IO;

using UnityEngine;
using UnityEngine.Video;

namespace MaterialPainter2
{
    public partial class MP2
    {
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

        public static void RefreshBrushesVideos()
        {
            material_brushes_videos.Clear();

            //material_brushes_videos["None"] = (new MaterialType("icon_none", get_sprite("icon_none"), (int)MaterialBrush.None));

            string video_directory = GetCustomVideoDirectory();
            if (!Directory.Exists(video_directory))
            {
                MPDebug($"Video directory not found: {video_directory}", always_show: true);
                return;
            }

            string[] files = Directory.GetFiles(video_directory, "*.mp4", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                MPDebug($"Processing: {file}");

                string outputFilePath = System.IO.Path.ChangeExtension(file, ".png");
                string image_name = System.IO.Path.GetFileNameWithoutExtension(outputFilePath);
                bool needsThumbnailGeneration = false;

                if (File.Exists(outputFilePath))
                {
                    if (!LoadSpriteFromImageFile(outputFilePath, image_name, true, true))
                    {
                        EnsureVideoFallbackSprite(image_name);
                        needsThumbnailGeneration = true;
                    }
                }
                else
                {
                    EnsureVideoFallbackSprite(image_name);
                    needsThumbnailGeneration = true;
                }

                if (needsThumbnailGeneration)
                {
                    CoroutineManager.Instance.StartCoroutine(VideoThumbnailGenerator.GenerateThumbnail(file, outputFilePath, (path, texture) =>
                    {
                        RegisterGeneratedVideoThumbnail(image_name, path, texture);
                    }));
                }

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

        private static void EnsureVideoFallbackSprite(string name)
        {
            if (sprites.ContainsKey(name))
                return;

            Sprite fallback = get_sprite("icon_video1", get_sprite("tex_question"));
            if (fallback != null)
                sprites.Add(name, fallback);
        }

        private static bool LoadSpriteFromImageFile(string filePath, string spriteName, bool replaceExisting = false, bool rejectEmptyGeneratedThumbnail = false)
        {
            if (!File.Exists(filePath))
                return false;

            if (sprites.ContainsKey(spriteName) && !replaceExisting)
                return true;

            try
            {
                Texture2D texture = new Texture2D(2, 2);
                byte[] bytes = File.ReadAllBytes(filePath);
                if (!texture.LoadImage(bytes))
                {
                    MPDebug("Texture not found at path: " + filePath);
                    return false;
                }

                if (rejectEmptyGeneratedThumbnail && IsEmptyGeneratedThumbnail(texture))
                {
                    MPDebug("Ignoring empty generated thumbnail cache: " + filePath);
                    return false;
                }

                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                sprite.name = spriteName;
                SetSprite(spriteName, sprite);
                return true;
            }
            catch (Exception ex)
            {
                MPDebug("Thumbnail load failed for " + filePath);
                MPDebug("An error occurred: " + ex.Message);
                MPDebug("Stack Trace: " + ex.StackTrace);
                return false;
            }
        }

        private static bool IsEmptyGeneratedThumbnail(Texture2D texture)
        {
            return VideoThumbnailGenerator.IsEmptyThumbnail(texture);
        }

        private static void RegisterGeneratedVideoThumbnail(string spriteName, string filePath, Texture2D texture)
        {
            if (sprites == null || texture == null)
                return;

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            sprite.name = spriteName;
            SetSprite(spriteName, sprite);

            if (material_brushes_videos != null && material_brushes_videos.ContainsKey(spriteName))
                material_brushes_videos[spriteName].preview = sprite;

            MPDebug("Generated native thumbnail: " + filePath);
        }

        private static void SetSprite(string spriteName, Sprite sprite)
        {
            if (sprites.ContainsKey(spriteName))
                sprites[spriteName] = sprite;
            else
                sprites.Add(spriteName, sprite);
        }
    }
}
