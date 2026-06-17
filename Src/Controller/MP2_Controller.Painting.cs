using Parkitect.Mods.AssetPacks;

using System;
using System.Globalization;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.Video;

namespace MaterialPainter2
{
    public partial class MP2_Controller
    {
        private const string MAIN_TEXTURE_PROPERTY_NAME = "_MainTex";
        private const string BASE_MAP_PROPERTY_NAME = "_BaseMap";
        private const float MIN_ATLAS_REMAP_UV_SIZE = 0.0001f;
        private static readonly int mainTexturePropertyID = Shader.PropertyToID("_MainTex");
        private static readonly int baseMapPropertyID = Shader.PropertyToID("_BaseMap");
        private static readonly int colorPropertyID = Shader.PropertyToID("_Color");

        public void SetMaterial(Transform tf, int brush_type = -1, string brush_type_custom = "")
        {
            int selected_brush = MP2.selected_brush;
            string selected_brush_custom = MP2.selected_brush_custom;

            if (brush_type != -1)
                selected_brush = brush_type;

            if (brush_type_custom != "")
                selected_brush_custom = brush_type_custom;

            MP2.MPDebug($"Painting {tf.gameObject.name} with Brush {selected_brush} ({brush_type}), {brush_type_custom}");

            ChangedMarker cm1 = tf.gameObject.GetComponent<ChangedMarker>();
            if (cm1 != null)
            {
                if (cm1.GetCurrentBrush() == selected_brush && cm1.GetCurrentBrushString() == selected_brush_custom)
                {
                    MP2.MPDebug("Same Brush");
                    return;
                }
            }

            if (tf.gameObject.GetComponent<ChangedMarker>() != null)
            {
                RevertMaterial(tf.gameObject);
            }

            if (selected_brush != (int)MaterialBrush.None)
            {
                if (tf.gameObject.GetComponent<ChangedMarker>() == null)
                {
                    BackupMaterial(tf.gameObject);
                }

                ChangedMarker cm = tf.gameObject.GetComponent<ChangedMarker>();

                if (cm == null)
                {
                    MP2.MPDebug("CM NULL??");
                    return;
                }

                cm.SetCurrentBrush(selected_brush);
                if (selected_brush == (int)MaterialBrush.Video || selected_brush == (int)MaterialBrush.Image)
                {
                    cm.SetCurrentBrush(selected_brush, selected_brush_custom);
                }

                // Set Material

                Renderer renderer = tf.GetComponent<Renderer>();
                if (renderer == null)
                {
                    MP2.MPDebug("RENDERER NULL??");
                    return;
                }

                switch (selected_brush)
                {
                    case (int)MaterialBrush.Water:
                        {
                            MP2.MPDebug("Water");
                            renderer.enabled = true;

                            WaterBody waterBody = new WaterBody();

                            Color c = WaterBody.defaultWaterColor;
                            if (tf.gameObject.GetComponent<CustomColors>() != null)
                                c = tf.gameObject.GetComponent<CustomColors>().getColors()[0];

                            waterBody.color = c;

                            waterBody.bodyType = WaterBody.BodyType.WATER;

                            Material[] shares = renderer.materials;

                            for (var i = 0; i < shares.Count(); i++)
                            {
                                Material material_old = shares[i];
                                if (material_old != null)
                                {
                                    Material material_new = new Material(waterBody.getMaterial());
                                    //material_new.SetTexture("_MainTex", material_old.GetTexture("_MainTex"));
                                    material_new.enableInstancing = true;
                                    shares[i] = material_new;
                                }
                            }

                            renderer.materials = shares;

                            c.a = 0.392156869f;

                            MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
                            renderer.GetPropertyBlock(materialPropertyBlock);
                            materialPropertyBlock.SetColor(Shader.PropertyToID("_Color"), c);
                            Color.RGBToHSV(c, out float H, out float S, out float V);
                            c = Color.HSVToRGB(H, S, V / 2f);
                            c.a = 0.9019608f;
                            materialPropertyBlock.SetColor(Shader.PropertyToID("_Color2"), c);
                            materialPropertyBlock.SetColor(Shader.PropertyToID("_ShoreColor"), Color.HSVToRGB(H, S / 3f, V));
                            renderer.SetPropertyBlock(materialPropertyBlock);
                        }
                        break;

                    case (int)MaterialBrush.Lava:
                        {
                            MP2.MPDebug("Lava");
                            renderer.enabled = true;

                            if (renderer != null)
                            {
                                WaterBody waterBody = new WaterBody();

                                Color c = new Color(1, 1, 1, 1);
                                if (tf.gameObject.GetComponent<CustomColors>() != null)
                                    c = tf.gameObject.GetComponent<CustomColors>().getColors()[0];

                                waterBody.color = c;
                                waterBody.bodyType = WaterBody.BodyType.LAVA;

                                Material[] shares = renderer.materials;

                                for (var i = 0; i < shares.Count(); i++)
                                {
                                    Material material_old = shares[i];
                                    if (material_old != null)
                                    {
                                        Material material_new = new Material(waterBody.getMaterial());
                                        material_new.enableInstancing = true;
                                        shares[i] = material_new;
                                    }
                                }

                                renderer.materials = shares;

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
                        }
                        break;

                    case (int)MaterialBrush.Glass:
                        {
                            MaterialDecorator materialDecorator = new MaterialDecorator();
                            Material snag = new Material(ScriptableSingleton<AssetManager>.Instance.seeThroughMaterial);

                            if (snag == null)
                            {
                                MP2.MPDebug("No CustomColorsTransparent in Material list?");
                                return;
                            }

                            Material[] shares = renderer.materials;

                            for (var i = 0; i < shares.Count(); i++)
                            {
                                Material material_old = shares[i];
                                if (material_old != null)
                                {
                                    Material material_new = snag;
                                    material_new.enableInstancing = true;
                                    shares[i] = material_new;
                                }
                            }

                            renderer.materials = shares;

                            Color c = new Color(1, 1, 1, 1);
                            if (tf.gameObject.GetComponent<CustomColors>() != null)
                                c = tf.gameObject.GetComponent<CustomColors>().getColors()[0];
                            c.a = .5f;

                            MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
                            renderer.GetPropertyBlock(materialPropertyBlock);
                            materialPropertyBlock.SetColor(Shader.PropertyToID("_Color"), c);
                            renderer.SetPropertyBlock(materialPropertyBlock);
                        }
                        break;

                    case (int)MaterialBrush.Terrain:
                        {
                            MaterialDecorator materialDecorator = new MaterialDecorator();
                            Material snag = new Material(ScriptableSingleton<AssetManager>.Instance.terrainMaterial);
                            //Material snag = ScriptableSingleton<AssetManager>.Instance.decoVisibilityHighlightOverlayMaterial;

                            if (snag == null)
                            {
                                MP2.MPDebug("No terrainMaterial in Material list?");
                                return;
                            }

                            Material[] shares = renderer.materials;

                            for (var i = 0; i < shares.Count(); i++)
                            {
                                Material material_old = shares[i];
                                if (material_old != null)
                                {
                                    Material material_new = snag;
                                    material_new.enableInstancing = true;
                                    shares[i] = material_new;
                                }
                            }

                            renderer.materials = shares;
                        }
                        break;

                    case (int)MaterialBrush.Invisible:
                        {
                            MP2.MPDebug("Invisible");

                            MaterialDecorator materialDecorator = new MaterialDecorator();
                            Material snag = new Material(ScriptableSingleton<AssetManager>.Instance.multiplayerBuildPreviewGhostMaterial);

                            if (snag == null)
                            {
                                MP2.MPDebug("No CustomColorsTransparent in Material list?");
                                return;
                            }

                            Material[] shares = renderer.materials;

                            for (var i = 0; i < shares.Count(); i++)
                            {
                                Material material_old = shares[i];
                                if (material_old != null)
                                {
                                    Material material_new = snag;
                                    material_new.enableInstancing = true;
                                    shares[i] = material_new;
                                }
                            }

                            renderer.materials = shares;

                            renderer.enabled = false;
                        }
                        break;

                    case (int)MaterialBrush.InvisiblePreview:
                        {
                            MP2.MPDebug("Invisible Preview");
                            renderer.enabled = true;

                            MaterialDecorator materialDecorator = new MaterialDecorator();
                            Material snag = new Material(ScriptableSingleton<AssetManager>.Instance.multiplayerBuildPreviewGhostMaterial);

                            if (snag == null)
                            {
                                MP2.MPDebug("No CustomColorsTransparent in Material list?");
                                return;
                            }

                            Material[] shares = renderer.materials;

                            for (var i = 0; i < shares.Count(); i++)
                            {
                                Material material_old = shares[i];
                                if (material_old != null)
                                {
                                    Material material_new = snag;
                                    material_new.enableInstancing = true;
                                    shares[i] = material_new;
                                }
                            }

                            renderer.materials = shares;
                        }
                        break;

                    case (int)MaterialBrush.Image:
                        {
                            MP2.MPDebug($"Image, {selected_brush_custom}");
                            renderer.enabled = true;

                            Texture2D image_texture = MP2.GetCustomImageTexture(selected_brush_custom);
                            if (image_texture != null)
                            {
                                bool useAtlasRemap = PrepareMediaPaintUv(tf, renderer, "Image", selected_brush_custom, out Vector2 uvMin, out Vector2 uvSize);
                                ApplyMediaMaterial(renderer, image_texture, useAtlasRemap, uvMin, uvSize);
                            }
                            else
                            {
                                MP2.MPDebug($"Couldn't load image {selected_brush_custom}.");
                            }
                        }
                        break;

                    case (int)MaterialBrush.Video:
                        {
                            MP2.MPDebug($"Video, {selected_brush_custom}");
                            renderer.enabled = true;

                            var video_url = MP2.GetCustomVideoFilePath(selected_brush_custom);
                            if (!File.Exists(video_url))
                            {
                                MP2.MPDebug($"Couldn't find {video_url}.");

                                string fallback = MP2.GetFallbackVideoPath();
                                if (File.Exists(fallback))
                                {
                                    video_url = fallback;
                                }
                                else
                                {
                                    video_url = null;
                                }
                            }

                            MP2.MPDebug("Loading video: " + video_url);

                            if (video_url != null)
                            {
                                bool useAtlasRemap = PrepareMediaPaintUv(tf, renderer, "Video", selected_brush_custom, out Vector2 uvMin, out Vector2 uvSize);
                                RenderTexture videoTexture = CreateVideoRenderTexture(16, 16, selected_brush_custom);
                                cm.SetVideoTexture(videoTexture);
                                ApplyMediaMaterial(renderer, videoTexture, useAtlasRemap, uvMin, uvSize);

                                /*if (MP2.cached_videos.ContainsKey(url))
                                {
                                    video_player = MP2.cached_videos[url];
                                    tf.gameObject.AddComponent<VideoPlayer>(video_player);
                                }
                                else
                                {
                                    video_player = tf.gameObject.AddComponent<VideoPlayer>();
                                    MP2.cached_videos.Add(url, video_player);
                                }*/


                                VideoPlayer video_player = tf.gameObject.AddComponent<VideoPlayer>();
                                AudioSource audio_source = tf.gameObject.AddComponent<AudioSource>();
                                cm.SetVideoPlayer(video_player);
                                cm.SetAudioSource(audio_source);

                                //video_player.audioOutputMode = VideoAudioOutputMode.None;

                                video_player.audioOutputMode = VideoAudioOutputMode.AudioSource;
                                video_player.SetTargetAudioSource(0, audio_source);

                                //audio_source.spatialize = true;
                                //audio_source.spatialBlend = 1.0f;

                                audio_source.volume = 0;

                                video_player.url = video_url;
                                video_player.isLooping = true;
                                video_player.renderMode = VideoRenderMode.RenderTexture;
                                video_player.targetTexture = videoTexture;
                                video_player.Prepare();
                                video_player.prepareCompleted += OnVideoPrepared;
                            }
                            else
                            {
                                MP2.MPDebug($"Couldn't load {video_url}.");
                            }
                        }
                        break;
                }

                ShockwaveController.Instance.addShockwave(tf.position, .5f, true);
                Instantiate(ScriptableSingleton<AssetManager>.Instance.employeeLevelUpParticlesGO).transform.position = tf.position;
                Instantiate(ScriptableSingleton<AssetManager>.Instance.plopParticlesGO).transform.position = tf.position;
                ScriptableSingleton<SoundAssetManager>.Instance.recolorObject.play2D();
            }
        }

        private void ApplyImageTexture(Renderer renderer, Texture texture)
        {
            MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(materialPropertyBlock);
            materialPropertyBlock.SetTexture(mainTexturePropertyID, texture);
            materialPropertyBlock.SetTexture(baseMapPropertyID, texture);
            renderer.SetPropertyBlock(materialPropertyBlock);
        }

        private bool PrepareMediaPaintUv(Transform tf, Renderer renderer, string mediaType, string mediaName, out Vector2 uvMin, out Vector2 uvSize)
        {
            PrintMediaPaintUvDebug(tf, renderer, mediaType, mediaName);

            bool useAtlasRemap = TryGetSingleSubmeshUvRemap(renderer, out uvMin, out uvSize, out string remapReason);
            if (useAtlasRemap)
            {
                Vector2 textureScale = GetAtlasRemapScale(uvSize);
                Vector2 textureOffset = GetAtlasRemapOffset(uvMin, uvSize);
                MP2.MPDebug($"[MediaPaintUV] type='{mediaType}' atlasRemap=enabled scale=({FormatFloat(textureScale.x)}, {FormatFloat(textureScale.y)}) offset=({FormatFloat(textureOffset.x)}, {FormatFloat(textureOffset.y)})", always_show: true);
            }
            else
            {
                MP2.MPDebug($"[MediaPaintUV] type='{mediaType}' atlasRemap=disabled reason='{remapReason}'", always_show: true);
                bool useBoxProjection = TryApplyBoxProjectedUvMesh(renderer, out string projectionReason);
                if (useBoxProjection)
                    MP2.MPDebug($"[MediaPaintUV] type='{mediaType}' boxProjection=enabled", always_show: true);
                else
                    MP2.MPDebug($"[MediaPaintUV] type='{mediaType}' boxProjection=disabled reason='{projectionReason}'", always_show: true);
            }

            return useAtlasRemap;
        }

        private bool TryGetSingleSubmeshUvRemap(Renderer renderer, out Vector2 uvMin, out Vector2 uvSize, out string reason)
        {
            uvMin = Vector2.zero;
            uvSize = Vector2.one;
            reason = "";

            try
            {
                Mesh mesh = GetRendererSharedMesh(renderer);
                if (mesh == null)
                {
                    reason = "no mesh";
                    return false;
                }

                if (mesh.subMeshCount != 1)
                {
                    reason = "submesh count is " + mesh.subMeshCount;
                    return false;
                }

                Vector2[] uvs = mesh.uv;
                if (uvs == null || uvs.Length == 0)
                {
                    reason = "no uvs";
                    return false;
                }

                if (!TryGetSubmeshUvBounds(mesh, uvs, 0, out uvMin, out Vector2 uvMax))
                {
                    reason = "could not read submesh uv bounds";
                    return false;
                }

                uvSize = uvMax - uvMin;
                if (Mathf.Abs(uvSize.x) < MIN_ATLAS_REMAP_UV_SIZE || Mathf.Abs(uvSize.y) < MIN_ATLAS_REMAP_UV_SIZE)
                {
                    reason = "uv bounds are too small";
                    return false;
                }
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                return false;
            }

            return true;
        }

        private void PrintMediaPaintUvDebug(Transform tf, Renderer renderer, string mediaType, string mediaName)
        {
            try
            {
                Mesh mesh = GetRendererSharedMesh(renderer);
                if (mesh == null)
                {
                    MP2.MPDebug($"[MediaPaintUV] type='{mediaType}' object='{tf.gameObject.name}' media='{mediaName}' renderer='{renderer.GetType().Name}' mesh=<none>", always_show: true);
                    return;
                }

                Vector2[] uvs = mesh.uv;
                if (uvs == null || uvs.Length == 0)
                {
                    MP2.MPDebug($"[MediaPaintUV] type='{mediaType}' object='{tf.gameObject.name}' media='{mediaName}' mesh='{mesh.name}' uvs=<none>", always_show: true);
                    return;
                }

                Material[] materials = renderer.sharedMaterials;
                MP2.MPDebug($"[MediaPaintUV] type='{mediaType}' object='{tf.gameObject.name}' media='{mediaName}' renderer='{renderer.GetType().Name}' mesh='{mesh.name}' vertices={mesh.vertexCount} uvs={uvs.Length} subMeshes={mesh.subMeshCount} materials={materials.Length}", always_show: true);

                for (var submeshIndex = 0; submeshIndex < mesh.subMeshCount; submeshIndex++)
                {
                    int[] triangles = mesh.GetTriangles(submeshIndex);
                    if (triangles == null || triangles.Length == 0)
                    {
                        MP2.MPDebug($"[MediaPaintUV] type='{mediaType}' submesh={submeshIndex} triangles=<none>", always_show: true);
                        continue;
                    }

                    Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                    Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                    bool foundUv = false;

                    foreach (int vertexIndex in triangles)
                    {
                        if (vertexIndex < 0 || vertexIndex >= uvs.Length)
                            continue;

                        Vector2 uv = uvs[vertexIndex];
                        min.x = Mathf.Min(min.x, uv.x);
                        min.y = Mathf.Min(min.y, uv.y);
                        max.x = Mathf.Max(max.x, uv.x);
                        max.y = Mathf.Max(max.y, uv.y);
                        foundUv = true;
                    }

                    if (!foundUv)
                    {
                        MP2.MPDebug($"[MediaPaintUV] type='{mediaType}' submesh={submeshIndex} uvBounds=<none>", always_show: true);
                        continue;
                    }

                    Material material = submeshIndex < materials.Length ? materials[submeshIndex] : null;
                    Texture texture = GetMaterialMainTexture(material);
                    string materialName = material != null ? material.name : "<none>";
                    string textureName = texture != null ? texture.name : "<none>";
                    string textureSize = texture != null ? $"{texture.width}x{texture.height}" : "<none>";
                    string pixelRect = texture != null
                        ? $"pixelRect=({FormatFloat(min.x * texture.width)}, {FormatFloat(min.y * texture.height)}, {FormatFloat((max.x - min.x) * texture.width)}, {FormatFloat((max.y - min.y) * texture.height)})"
                        : "pixelRect=<none>";

                    MP2.MPDebug($"[MediaPaintUV] type='{mediaType}' submesh={submeshIndex} material='{materialName}' texture='{textureName}' textureSize={textureSize} uvMin=({FormatFloat(min.x)}, {FormatFloat(min.y)}) uvMax=({FormatFloat(max.x)}, {FormatFloat(max.y)}) uvSize=({FormatFloat(max.x - min.x)}, {FormatFloat(max.y - min.y)}) {pixelRect}", always_show: true);
                }
            }
            catch (Exception ex)
            {
                MP2.MPDebug("[MediaPaintUV] failed: " + ex.Message, always_show: true);
            }
        }

        private bool TryGetSubmeshUvBounds(Mesh mesh, Vector2[] uvs, int submeshIndex, out Vector2 min, out Vector2 max)
        {
            min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            int[] triangles = mesh.GetTriangles(submeshIndex);
            if (triangles == null || triangles.Length == 0)
                return false;

            bool foundUv = false;
            foreach (int vertexIndex in triangles)
            {
                if (vertexIndex < 0 || vertexIndex >= uvs.Length)
                    continue;

                Vector2 uv = uvs[vertexIndex];
                min.x = Mathf.Min(min.x, uv.x);
                min.y = Mathf.Min(min.y, uv.y);
                max.x = Mathf.Max(max.x, uv.x);
                max.y = Mathf.Max(max.y, uv.y);
                foundUv = true;
            }

            return foundUv;
        }

        private bool TryApplyBoxProjectedUvMesh(Renderer renderer, out string reason)
        {
            reason = "";

            if (renderer is SkinnedMeshRenderer)
            {
                reason = "skinned renderer";
                return false;
            }

            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                reason = "no mesh filter";
                return false;
            }

            Mesh sourceMesh = meshFilter.sharedMesh;
            if (sourceMesh == null)
            {
                reason = "no mesh";
                return false;
            }

            if (sourceMesh.subMeshCount != 1)
            {
                reason = "submesh count is " + sourceMesh.subMeshCount;
                return false;
            }

            try
            {
                Vector3[] vertices = sourceMesh.vertices;
                if (vertices == null || vertices.Length == 0)
                {
                    reason = "no vertices";
                    return false;
                }

                Mesh projectedMesh = UnityEngine.Object.Instantiate(sourceMesh);
                projectedMesh.name = sourceMesh.name + "_MP2BoxProjection";

                Vector3[] normals = sourceMesh.normals;
                if (normals == null || normals.Length != vertices.Length)
                {
                    projectedMesh.RecalculateNormals();
                    normals = projectedMesh.normals;
                }

                Bounds bounds = sourceMesh.bounds;
                Vector2[] projectedUvs = new Vector2[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 normal = normals != null && i < normals.Length ? normals[i] : Vector3.up;
                    projectedUvs[i] = GetBoxProjectedUv(vertices[i], normal, bounds);
                }

                projectedMesh.uv = projectedUvs;
                meshFilter.sharedMesh = projectedMesh;

                ChangedMarker changedMarker = renderer.GetComponent<ChangedMarker>();
                if (changedMarker != null)
                    changedMarker.SetGeneratedMesh(projectedMesh);

                MP2.MPDebug($"[MediaPaintUV] boxProjection mesh='{projectedMesh.name}' boundsSize=({FormatFloat(bounds.size.x)}, {FormatFloat(bounds.size.y)}, {FormatFloat(bounds.size.z)})", always_show: true);
                return true;
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                return false;
            }
        }

        private Vector2 GetBoxProjectedUv(Vector3 vertex, Vector3 normal, Bounds bounds)
        {
            Vector3 absNormal = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
            float x = InverseLerpSafe(bounds.min.x, bounds.max.x, vertex.x);
            float y = InverseLerpSafe(bounds.min.y, bounds.max.y, vertex.y);
            float z = InverseLerpSafe(bounds.min.z, bounds.max.z, vertex.z);

            if (absNormal.y >= absNormal.x && absNormal.y >= absNormal.z)
            {
                if (normal.y < 0f)
                    z = 1f - z;

                return new Vector2(x, z);
            }

            if (absNormal.x >= absNormal.z)
            {
                if (normal.x > 0f)
                    z = 1f - z;

                return new Vector2(z, y);
            }

            if (normal.z < 0f)
                x = 1f - x;

            return new Vector2(x, y);
        }

        private float InverseLerpSafe(float min, float max, float value)
        {
            if (Mathf.Abs(max - min) < MIN_ATLAS_REMAP_UV_SIZE)
                return 0.5f;

            return Mathf.InverseLerp(min, max, value);
        }

        private Mesh GetRendererSharedMesh(Renderer renderer)
        {
            SkinnedMeshRenderer skinnedMeshRenderer = renderer as SkinnedMeshRenderer;
            if (skinnedMeshRenderer != null)
                return skinnedMeshRenderer.sharedMesh;

            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            return meshFilter != null ? meshFilter.sharedMesh : null;
        }

        private Texture GetMaterialMainTexture(Material material)
        {
            if (material == null)
                return null;

            if (material.HasProperty(mainTexturePropertyID))
                return material.GetTexture(mainTexturePropertyID);

            if (material.HasProperty(baseMapPropertyID))
                return material.GetTexture(baseMapPropertyID);

            return material.mainTexture;
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        private Vector2 GetAtlasRemapScale(Vector2 uvSize)
        {
            return new Vector2(1f / uvSize.x, 1f / uvSize.y);
        }

        private Vector2 GetAtlasRemapOffset(Vector2 uvMin, Vector2 uvSize)
        {
            return new Vector2(-uvMin.x / uvSize.x, -uvMin.y / uvSize.y);
        }

        private void ApplyMediaMaterial(Renderer renderer, Texture texture, bool useAtlasRemap, Vector2 uvMin, Vector2 uvSize)
        {
            Shader imageShader = Shader.Find("Unlit/Texture");
            if (imageShader == null)
                imageShader = Shader.Find("Standard");

            if (imageShader == null)
            {
                MP2.MPDebug("No media material shader found; falling back to texture override.", always_show: true);
                if (texture != null)
                    ApplyImageTexture(renderer, texture);
                return;
            }

            MP2.MPDebug("Media material shader: " + imageShader.name);

            Vector2 textureScale = Vector2.one;
            Vector2 textureOffset = Vector2.zero;
            if (useAtlasRemap)
            {
                textureScale = GetAtlasRemapScale(uvSize);
                textureOffset = GetAtlasRemapOffset(uvMin, uvSize);
            }

            Material[] shares = renderer.materials;
            for (var i = 0; i < shares.Count(); i++)
            {
                Material material_new = new Material(imageShader);
                if (texture != null)
                    material_new.mainTexture = texture;

                if (material_new.HasProperty(mainTexturePropertyID))
                {
                    if (texture != null)
                        material_new.SetTexture(mainTexturePropertyID, texture);

                    if (useAtlasRemap)
                    {
                        material_new.SetTextureScale(MAIN_TEXTURE_PROPERTY_NAME, textureScale);
                        material_new.SetTextureOffset(MAIN_TEXTURE_PROPERTY_NAME, textureOffset);
                    }
                }

                if (material_new.HasProperty(baseMapPropertyID))
                {
                    if (texture != null)
                        material_new.SetTexture(baseMapPropertyID, texture);

                    if (useAtlasRemap)
                    {
                        material_new.SetTextureScale(BASE_MAP_PROPERTY_NAME, textureScale);
                        material_new.SetTextureOffset(BASE_MAP_PROPERTY_NAME, textureOffset);
                    }
                }

                if (material_new.HasProperty(colorPropertyID))
                    material_new.SetColor(colorPropertyID, Color.white);

                material_new.enableInstancing = true;
                shares[i] = material_new;
            }

            renderer.materials = shares;
            renderer.SetPropertyBlock(new MaterialPropertyBlock());
        }

        private RenderTexture CreateVideoRenderTexture(int width, int height, string name)
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);

            RenderTexture renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            renderTexture.name = "MP2VideoTexture_" + name;
            renderTexture.wrapMode = TextureWrapMode.Clamp;
            renderTexture.filterMode = FilterMode.Point;
            renderTexture.Create();
            return renderTexture;
        }

        private void ResizePreparedVideoRenderTexture(VideoPlayer videoPlayer)
        {
            int width = Mathf.Max(1, (int)videoPlayer.width);
            int height = Mathf.Max(1, (int)videoPlayer.height);
            RenderTexture oldTexture = videoPlayer.targetTexture;

            if (oldTexture != null && oldTexture.width == width && oldTexture.height == height)
                return;

            RenderTexture newTexture = CreateVideoRenderTexture(width, height, videoPlayer.gameObject.name);
            videoPlayer.targetTexture = newTexture;

            ChangedMarker changedMarker = videoPlayer.GetComponent<ChangedMarker>();
            if (changedMarker != null)
                changedMarker.SetVideoTexture(newTexture);

            Renderer renderer = videoPlayer.GetComponent<Renderer>();
            if (renderer != null)
                SetRendererMediaTexture(renderer, newTexture);

            if (oldTexture != null)
            {
                oldTexture.Release();
                DestroyImmediate(oldTexture);
            }
        }

        private void SetRendererMediaTexture(Renderer renderer, Texture texture)
        {
            Material[] materials = renderer.materials;
            for (var i = 0; i < materials.Count(); i++)
            {
                Material material = materials[i];
                if (material == null)
                    continue;

                material.mainTexture = texture;

                if (material.HasProperty(mainTexturePropertyID))
                    material.SetTexture(mainTexturePropertyID, texture);

                if (material.HasProperty(baseMapPropertyID))
                    material.SetTexture(baseMapPropertyID, texture);
            }

            renderer.materials = materials;
        }

        void OnVideoPrepared(VideoPlayer vp)
        {
            ResizePreparedVideoRenderTexture(vp);
            // Play the video when it is prepared
            vp.Play();
        }
    }
}
