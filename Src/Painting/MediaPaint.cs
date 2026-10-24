using System.Globalization;

using UnityEngine;
using UnityEngine.Video;

namespace MaterialPainter2
{
    /// <summary>
    /// Image and video paints. Materials, projected meshes and video players are shared between every
    /// object showing the same media, so painting many objects stays cheap and videos stay in sync.
    /// </summary>
    public static class MediaPaint
    {
        private const string MAIN_TEXTURE_PROPERTY_NAME = "_MainTex";
        private const float MIN_UV_SIZE = 0.0001f;
        private const int VIDEO_TEXTURE_SIZE = 1024;

        private class SharedVideo
        {
            public GameObject gameObject;
            public VideoPlayer videoPlayer;
            public RenderTexture renderTexture;
        }

        private static readonly SharedCache<string, Material> materials = new SharedCache<string, Material>(Object.Destroy);
        private static readonly SharedCache<int, Mesh> projectedMeshes = new SharedCache<int, Mesh>(Object.Destroy);
        private static readonly SharedCache<string, SharedVideo> videos = new SharedCache<string, SharedVideo>(DestroyVideo);
        private static Shader shader;

        public static void Apply(Renderer renderer, Texture texture, PaintMarker marker)
        {
            Vector2 scale = Vector2.one;
            Vector2 offset = Vector2.zero;

            // A single-submesh model usually samples one atlas region; stretch the media over that region.
            // Otherwise give the mesh box-projected UVs so the media covers it once.
            if (TryGetUvBounds(renderer, out Vector2 uvMin, out Vector2 uvSize))
            {
                scale = new Vector2(1f / uvSize.x, 1f / uvSize.y);
                offset = new Vector2(-uvMin.x / uvSize.x, -uvMin.y / uvSize.y);
            }
            else
            {
                TryApplyBoxProjection(renderer, marker);
            }

            string key = string.Join("|", texture.GetInstanceID().ToString(CultureInfo.InvariantCulture),
                FormatKey(scale.x), FormatKey(scale.y), FormatKey(offset.x), FormatKey(offset.y));

            Material material = materials.Acquire(key, () => CreateMaterial(texture, scale, offset));
            marker.AddRelease(() => materials.Release(key));

            Material[] slots = renderer.sharedMaterials;
            for (int i = 0; i < slots.Length; i++)
                slots[i] = material;

            renderer.sharedMaterials = slots;
            renderer.SetPropertyBlock(new MaterialPropertyBlock());
        }

        public static void ApplyVideo(Renderer renderer, string videoPath, string videoName, PaintMarker marker)
        {
            SharedVideo video = videos.Acquire(videoPath, () => CreateVideo(videoPath, videoName));
            marker.AddRelease(() => videos.Release(videoPath));
            Apply(renderer, video.renderTexture, marker);
        }

        public static void ClearCaches()
        {
            materials.Clear();
            projectedMeshes.Clear();
            videos.Clear();
        }

        private static Shader GetShader()
        {
            if (shader == null)
                shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");

            return shader;
        }

        private static Material CreateMaterial(Texture texture, Vector2 scale, Vector2 offset)
        {
            Material material = new Material(GetShader())
            {
                name = "MP2Media_" + texture.name,
                mainTexture = texture,
                mainTextureScale = scale,
                mainTextureOffset = offset,
                enableInstancing = true
            };

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", Color.white);

            return material;
        }

        private static bool TryGetUvBounds(Renderer renderer, out Vector2 uvMin, out Vector2 uvSize)
        {
            uvMin = Vector2.zero;
            uvSize = Vector2.one;

            Mesh mesh = GetSharedMesh(renderer);
            if (mesh == null || mesh.subMeshCount != 1)
                return false;

            Vector2[] uvs = mesh.uv;
            if (uvs == null || uvs.Length == 0)
                return false;

            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            bool found = false;
            foreach (int index in mesh.GetTriangles(0))
            {
                if (index < 0 || index >= uvs.Length)
                    continue;

                min = Vector2.Min(min, uvs[index]);
                max = Vector2.Max(max, uvs[index]);
                found = true;
            }

            if (!found || max.x - min.x < MIN_UV_SIZE || max.y - min.y < MIN_UV_SIZE)
                return false;

            uvMin = min;
            uvSize = max - min;
            return true;
        }

        private static void TryApplyBoxProjection(Renderer renderer, PaintMarker marker)
        {
            if (renderer is SkinnedMeshRenderer)
                return;

            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            Mesh source = meshFilter != null ? meshFilter.sharedMesh : null;
            if (source == null || source.subMeshCount != 1 || source.vertexCount == 0)
                return;

            int key = source.GetInstanceID();
            meshFilter.sharedMesh = projectedMeshes.Acquire(key, () => CreateBoxProjectedMesh(source));
            marker.AddRelease(() => projectedMeshes.Release(key));
        }

        private static Mesh CreateBoxProjectedMesh(Mesh source)
        {
            Mesh mesh = Object.Instantiate(source);
            mesh.name = source.name + "_MP2BoxProjection";

            Vector3[] vertices = source.vertices;
            Vector3[] normals = source.normals;
            if (normals == null || normals.Length != vertices.Length)
            {
                mesh.RecalculateNormals();
                normals = mesh.normals;
            }

            Bounds bounds = source.bounds;
            Vector2[] uvs = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                uvs[i] = GetBoxProjectedUv(vertices[i], normals[i], bounds);

            mesh.uv = uvs;
            return mesh;
        }

        private static Vector2 GetBoxProjectedUv(Vector3 vertex, Vector3 normal, Bounds bounds)
        {
            float x = InverseLerpSafe(bounds.min.x, bounds.max.x, vertex.x);
            float y = InverseLerpSafe(bounds.min.y, bounds.max.y, vertex.y);
            float z = InverseLerpSafe(bounds.min.z, bounds.max.z, vertex.z);
            Vector3 abs = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));

            if (abs.y >= abs.x && abs.y >= abs.z)
                return new Vector2(x, normal.y < 0f ? 1f - z : z);

            if (abs.x >= abs.z)
                return new Vector2(normal.x > 0f ? 1f - z : z, y);

            return new Vector2(normal.z < 0f ? 1f - x : x, y);
        }

        private static float InverseLerpSafe(float min, float max, float value)
        {
            return Mathf.Abs(max - min) < MIN_UV_SIZE ? 0.5f : Mathf.InverseLerp(min, max, value);
        }

        private static Mesh GetSharedMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
                return skinned.sharedMesh;

            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            return meshFilter != null ? meshFilter.sharedMesh : null;
        }

        private static SharedVideo CreateVideo(string videoPath, string videoName)
        {
            GameObject gameObject = new GameObject("MP2 Shared Video - " + videoName);
            if (MP2.Instance != null && MP2.Instance.go != null)
                gameObject.transform.SetParent(MP2.Instance.go.transform, false);

            RenderTexture renderTexture = new RenderTexture(VIDEO_TEXTURE_SIZE, VIDEO_TEXTURE_SIZE, 0, RenderTextureFormat.ARGB32)
            {
                name = "MP2VideoTexture_" + videoName,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            renderTexture.Create();

            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = 0;

            VideoPlayer videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, audioSource);
            videoPlayer.url = videoPath;
            videoPlayer.isLooping = true;
            videoPlayer.aspectRatio = VideoAspectRatio.FitOutside;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.prepareCompleted += player => player.Play();
            videoPlayer.Prepare();

            return new SharedVideo { gameObject = gameObject, videoPlayer = videoPlayer, renderTexture = renderTexture };
        }

        private static void DestroyVideo(SharedVideo video)
        {
            if (video.videoPlayer != null)
                video.videoPlayer.Stop();

            if (video.gameObject != null)
                Object.Destroy(video.gameObject);

            if (video.renderTexture != null)
            {
                video.renderTexture.Release();
                Object.Destroy(video.renderTexture);
            }
        }

        private static string FormatKey(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
