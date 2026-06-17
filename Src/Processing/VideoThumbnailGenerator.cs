using System;
using System.Collections;
using System.IO;

using UnityEngine;
using UnityEngine.Video;

namespace MaterialPainter2
{
    public static class VideoThumbnailGenerator
    {
        public const int ThumbnailSize = 256;
        private const float PrepareTimeoutSeconds = 10f;
        private const float FrameTimeoutSeconds = 5f;

        public static IEnumerator GenerateThumbnail(string videoPath, string outputFilePath, Action<string, Texture2D> onThumbnailReady)
        {
            GameObject thumbnailObject = new GameObject("MP2 Video Thumbnail Generator")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            VideoPlayer videoPlayer = thumbnailObject.AddComponent<VideoPlayer>();
            RenderTexture renderTexture = new RenderTexture(ThumbnailSize, ThumbnailSize, 0, RenderTextureFormat.ARGB32);
            string videoError = null;
            bool frameReady = false;
            bool seekCompleted = false;

            videoPlayer.errorReceived += (source, message) =>
            {
                videoError = message;
            };

            videoPlayer.playOnAwake = false;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.isLooping = false;
            videoPlayer.sendFrameReadyEvents = true;
            videoPlayer.frameReady += (source, frameIdx) =>
            {
                frameReady = true;
            };
            videoPlayer.seekCompleted += source =>
            {
                seekCompleted = true;
            };
            videoPlayer.url = videoPath;

            videoPlayer.Prepare();

            float deadline = Time.realtimeSinceStartup + PrepareTimeoutSeconds;
            while (!videoPlayer.isPrepared && string.IsNullOrEmpty(videoError) && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (!videoPlayer.isPrepared || !string.IsNullOrEmpty(videoError))
            {
                MP2.MPDebug($"Could not prepare video thumbnail for {videoPath}: {videoError}");
                Cleanup(thumbnailObject, renderTexture);
                yield break;
            }

            if (videoPlayer.canSetTime)
            {
                double captureTime = videoPlayer.length > 0d ? Math.Min(1d, videoPlayer.length * 0.5d) : 0d;
                if (captureTime > 0d)
                {
                    seekCompleted = false;
                    videoPlayer.time = captureTime;

                    deadline = Time.realtimeSinceStartup + FrameTimeoutSeconds;
                    while (!seekCompleted && string.IsNullOrEmpty(videoError) && Time.realtimeSinceStartup < deadline)
                        yield return null;
                }
            }

            videoPlayer.Play();

            deadline = Time.realtimeSinceStartup + FrameTimeoutSeconds;
            while (!frameReady && string.IsNullOrEmpty(videoError) && Time.realtimeSinceStartup < deadline)
                yield return null;

            yield return new WaitForEndOfFrame();

            videoPlayer.Pause();

            if (!string.IsNullOrEmpty(videoError) || !frameReady)
            {
                MP2.MPDebug($"Could not render video thumbnail for {videoPath}: {videoError}");
                Cleanup(thumbnailObject, renderTexture);
                yield break;
            }

            Texture2D thumbnail = CaptureRenderTexture(renderTexture);
            if (thumbnail == null)
            {
                MP2.MPDebug($"Could not capture video thumbnail for {videoPath}");
                Cleanup(thumbnailObject, renderTexture);
                yield break;
            }

            if (IsEmptyThumbnail(thumbnail))
            {
                MP2.MPDebug($"Captured empty video thumbnail for {videoPath}");
                UnityEngine.Object.Destroy(thumbnail);
                Cleanup(thumbnailObject, renderTexture);
                yield break;
            }

            TryWriteThumbnail(outputFilePath, thumbnail);
            onThumbnailReady?.Invoke(outputFilePath, thumbnail);

            Cleanup(thumbnailObject, renderTexture);
        }

        private static Texture2D CaptureRenderTexture(RenderTexture renderTexture)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;

            try
            {
                Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                texture.Apply();
                return texture;
            }
            catch (Exception ex)
            {
                MP2.MPDebug("Thumbnail capture failed.");
                MP2.MPDebug("An error occurred: " + ex.Message);
                MP2.MPDebug("Stack Trace: " + ex.StackTrace);
                return null;
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        private static void TryWriteThumbnail(string outputFilePath, Texture2D thumbnail)
        {
            try
            {
                string directory = System.IO.Path.GetDirectoryName(outputFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllBytes(outputFilePath, thumbnail.EncodeToPNG());
            }
            catch (Exception ex)
            {
                MP2.MPDebug("Thumbnail cache write failed for " + outputFilePath);
                MP2.MPDebug("An error occurred: " + ex.Message);
                MP2.MPDebug("Stack Trace: " + ex.StackTrace);
            }
        }

        public static bool IsEmptyThumbnail(Texture2D texture)
        {
            if (texture.width != ThumbnailSize || texture.height != ThumbnailSize)
                return false;

            foreach (Color32 pixel in texture.GetPixels32())
            {
                if (pixel.a > 4 && (pixel.r > 4 || pixel.g > 4 || pixel.b > 4))
                    return false;
            }

            return true;
        }

        private static void Cleanup(GameObject thumbnailObject, RenderTexture renderTexture)
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                UnityEngine.Object.Destroy(renderTexture);
            }

            if (thumbnailObject != null)
                UnityEngine.Object.Destroy(thumbnailObject);
        }
    }
}
