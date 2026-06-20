using System.Collections.Generic;

using UnityEngine;
namespace MaterialPainter2
{
    public class ChangedMarker : MonoBehaviour
    {
        private Material[] materials = null;
        private MaterialPropertyBlock material_property_block = null;
        private Mesh shared_mesh = null;
        private Mesh generated_mesh = null;
        private int generated_mesh_key = 0;
        private string shared_video_key = "";
        private List<string> media_material_keys = new List<string>();
        private bool has_chunked_mesh_state = false;
        private bool chunked_mesh_was_enabled = false;
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

        public Mesh GetSharedMesh()
        { return shared_mesh; }

        public void SetSharedMesh(Mesh shared_mesh)
        { this.shared_mesh = shared_mesh; }

        public Mesh GetGeneratedMesh()
        { return generated_mesh; }

        public void SetGeneratedMesh(Mesh generated_mesh)
        { this.generated_mesh = generated_mesh; }

        public int GetGeneratedMeshKey()
        { return generated_mesh_key; }

        public void SetGeneratedMeshKey(int generated_mesh_key)
        { this.generated_mesh_key = generated_mesh_key; }

        public string GetSharedVideoKey()
        { return shared_video_key; }

        public void SetSharedVideoKey(string shared_video_key)
        { this.shared_video_key = shared_video_key; }

        public List<string> GetMediaMaterialKeys()
        { return media_material_keys; }

        public void AddMediaMaterialKey(string media_material_key)
        { media_material_keys.Add(media_material_key); }

        public bool HasChunkedMeshState()
        { return has_chunked_mesh_state; }

        public bool WasChunkedMeshEnabled()
        { return chunked_mesh_was_enabled; }

        public void SetChunkedMeshState(bool was_enabled)
        {
            has_chunked_mesh_state = true;
            chunked_mesh_was_enabled = was_enabled;
        }

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

    }
}
