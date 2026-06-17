using System;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.Video;

namespace MaterialPainter2
{
    public partial class MP2
    {
        private const string CUSTOM_IMAGE_SPRITE_PREFIX = "image-";
        private const string FALLBACK_IMAGE_SPRITE_NAME = "tex_question";
        private static readonly string[] IMAGE_FILE_EXTENSIONS = { ".png", ".jpg", ".jpeg" };

        public static Sprite get_sprite(string name, Sprite def = null)
        {
            if (sprites != null && sprites.ContainsKey(name))
            {
                return sprites[name];
            }
            else
            {
                MPDebug("Couldn't load sprite: '" + name + "'");
                if (def != null)
                    return def;

                if (sprites != null && name != FALLBACK_IMAGE_SPRITE_NAME && sprites.ContainsKey(FALLBACK_IMAGE_SPRITE_NAME))
                    return sprites[FALLBACK_IMAGE_SPRITE_NAME];

                return def;
            }
        }

        public static Texture2D GetCustomImageTexture(string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
                return null;

            if (custom_images != null && custom_images.ContainsKey(imageName))
                return custom_images[imageName];

            string imagePath = ResolveCustomImageFilePath(imageName);
            Texture2D texture = LoadTextureFromImageFile(imagePath);
            if (texture == null)
            {
                MPDebug("Couldn't load image: '" + imageName + "', using fallback.");
                return GetFallbackImageTexture(imageName);
            }

            if (custom_images == null)
                custom_images = new System.Collections.Generic.Dictionary<string, Texture2D>();

            texture.name = GetCustomImageSpriteName(imageName);
            custom_images[imageName] = texture;
            RegisterImageSprite(texture.name, texture);
            return texture;
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

        public static void RefreshBrushesImages()
        {
            material_brushes_images.Clear();

            if (custom_images == null)
                custom_images = new System.Collections.Generic.Dictionary<string, Texture2D>();
            else
                custom_images.Clear();

            string image_directory = GetCustomImageDirectory();
            if (!Directory.Exists(image_directory))
            {
                MPDebug($"Image directory not found: {image_directory}", always_show: true);
                return;
            }

            string[] files = Directory.GetFiles(image_directory, "*.*", SearchOption.AllDirectories);
            foreach (string file in files.Where(IsSupportedImageFile).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                MPDebug($"Processing: {file}");

                string image_name = GetRelativeCustomFilePath(image_directory, file);
                string sprite_name = GetCustomImageSpriteName(image_name);
                Texture2D texture = LoadTextureFromImageFile(file);
                if (texture == null)
                    continue;

                texture.name = sprite_name;
                custom_images[image_name] = texture;
                Sprite sprite = RegisterImageSprite(sprite_name, texture);

                MaterialType new_type = new MaterialType(name: sprite_name, preview: sprite, id: (int)MaterialBrush.Image, id_string: image_name);
                if (!material_brushes_images.ContainsKey(image_name))
                {
                    MPDebug($"Adding image brush: {image_name}");
                    material_brushes_images.Add(image_name, new_type);
                }
                else
                {
                    MPDebug($"{image_name} already in image brushes");
                }
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
                Texture2D texture = LoadTextureFromImageFile(filePath);
                if (texture == null)
                    return false;

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

            RegisterImageSprite(spriteName, texture);

            if (material_brushes_videos != null && material_brushes_videos.ContainsKey(spriteName))
                material_brushes_videos[spriteName].preview = get_sprite(spriteName);

            MPDebug("Generated native thumbnail: " + filePath);
        }

        private static Texture2D LoadTextureFromImageFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            try
            {
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                byte[] bytes = File.ReadAllBytes(filePath);
                if (!texture.LoadImage(bytes))
                {
                    MPDebug("Texture not found at path: " + filePath);
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }

                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Point;
                texture.anisoLevel = 0;
                return texture;
            }
            catch (Exception ex)
            {
                MPDebug("Image load failed for " + filePath);
                MPDebug("An error occurred: " + ex.Message);
                MPDebug("Stack Trace: " + ex.StackTrace);
                return null;
            }
        }

        private static Texture2D GetFallbackImageTexture(string imageName)
        {
            if (custom_images != null && custom_images.ContainsKey(FALLBACK_IMAGE_FILE_NAME))
                return custom_images[FALLBACK_IMAGE_FILE_NAME];

            Texture2D fallbackTexture = LoadTextureFromImageFile(GetFallbackImagePath());
            if (fallbackTexture != null)
            {
                if (custom_images == null)
                    custom_images = new System.Collections.Generic.Dictionary<string, Texture2D>();

                fallbackTexture.name = GetCustomImageSpriteName(FALLBACK_IMAGE_FILE_NAME);
                custom_images[FALLBACK_IMAGE_FILE_NAME] = fallbackTexture;
                RegisterImageSprite(fallbackTexture.name, fallbackTexture);
                return fallbackTexture;
            }

            Sprite fallbackSprite = get_sprite(FALLBACK_IMAGE_SPRITE_NAME);
            if (fallbackSprite != null && fallbackSprite.texture != null)
                return fallbackSprite.texture;

            MPDebug("Couldn't load fallback image for: " + imageName);
            return null;
        }

        private static Sprite RegisterImageSprite(string spriteName, Texture2D texture)
        {
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            sprite.name = spriteName;
            SetSprite(spriteName, sprite);
            return sprite;
        }

        private static bool IsSupportedImageFile(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath);
            return IMAGE_FILE_EXTENSIONS.Any(imageExtension => imageExtension.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetCustomImageSpriteName(string imageName)
        {
            return CUSTOM_IMAGE_SPRITE_PREFIX + NormalizePath(imageName);
        }

        private static string ResolveCustomImageFilePath(string imageName)
        {
            string imagePath = GetCustomImageFilePath(imageName);
            if (File.Exists(imagePath))
                return imagePath;

            if (!System.IO.Path.HasExtension(imageName))
            {
                foreach (string extension in IMAGE_FILE_EXTENSIONS)
                {
                    string candidatePath = GetCustomImageFilePath(imageName + extension);
                    if (File.Exists(candidatePath))
                        return candidatePath;
                }
            }

            return imagePath;
        }

        private static string GetRelativeCustomFilePath(string rootDirectory, string filePath)
        {
            string root = EnsureTrailingSlash(NormalizePath(rootDirectory));
            string normalizedFilePath = NormalizePath(filePath);
            if (normalizedFilePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return normalizedFilePath.Substring(root.Length);

            return System.IO.Path.GetFileName(filePath);
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
