using UnityEngine;
using UnityEngine.Video;

namespace MaterialPainter2
{
    public class ChangedMarker : MonoBehaviour
    {
        private Material[] materials = null;
        private MaterialPropertyBlock material_property_block = null;
        private VideoPlayer video_player = null;
        private AudioSource audio_source = null;
        private bool _was_enabled = true;
        private int current_brush = -1;
        private string custom_string = "";

        public Material[] GetMaterials()
        { return materials; }

        public void SetMaterials(Material[] materials)
        { this.materials = materials; }

        public MaterialPropertyBlock GetMaterialPropertyBlock()
        { return material_property_block; }

        public void SetMaterialPropertyBlock(MaterialPropertyBlock material_property_block)
        { this.material_property_block = material_property_block; }

        public bool WasEnabled()
        { return _was_enabled; }

        public void SetEnabled(bool _was_enabled)
        { this._was_enabled = _was_enabled; }

        public void SetCurrentBrush(int brush, string custom_data = "")
        {
            current_brush = brush;
            custom_string = custom_data;
        }

        public int GetCurrentBrush()
        { return current_brush; }

        public string GetCurrentBrushString()
        { return custom_string; }

        public void SetVideoPlayer(VideoPlayer video_player)
        { this.video_player = video_player; }

        public VideoPlayer GetVideoPlayer()
        { return video_player; }

        public void SetAudioSource(AudioSource audio_source)
        { this.audio_source = audio_source; }

        public AudioSource GetAudioSource()
        { return audio_source; }
    }
}
