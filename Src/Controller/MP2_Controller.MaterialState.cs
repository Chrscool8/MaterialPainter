using UnityEngine;

namespace MaterialPainter2
{
    public partial class MP2_Controller
    {
        private void BackupMaterial(GameObject game_object)
        {
            //MP2.MPDebug("BACKUP!");

            Renderer renderer = game_object.GetComponent<Renderer>();

            if (renderer != null)
            {
                ChangedMarker changed_marker = game_object.AddComponent<ChangedMarker>();

                ChunkedMesh chunked_mesh = game_object.GetComponent<ChunkedMesh>();
                if (chunked_mesh != null)
                {
                    changed_marker.SetChunkedMeshState(chunked_mesh.enabled);
                    chunked_mesh.enabled = false;
                }

                changed_marker.SetMaterials(renderer.materials);
                MaterialPropertyBlock old_block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(old_block);
                changed_marker.SetMaterialPropertyBlock(old_block);
                changed_marker.SetEnabled(renderer.enabled);

                MeshFilter mesh_filter = game_object.GetComponent<MeshFilter>();
                if (mesh_filter != null)
                    changed_marker.SetSharedMesh(mesh_filter.sharedMesh);
            }
        }

        private void RevertMaterial(GameObject game_object)
        {
            //MP2.MPDebug("REVERT!");

            ChangedMarker changed_marker = game_object.GetComponent<ChangedMarker>();
            if (changed_marker == null)
            {
                MP2.MPDebug($"Can't revert {game_object}.");
                return;
            }

            Renderer renderer = game_object.GetComponent<Renderer>();

            if (renderer)
            {
                renderer.materials = changed_marker.GetMaterials();
                renderer.SetPropertyBlock(changed_marker.GetMaterialPropertyBlock());
                renderer.enabled = (changed_marker.WasEnabled());
            }

            MeshFilter mesh_filter = game_object.GetComponent<MeshFilter>();
            if (mesh_filter != null && changed_marker.GetSharedMesh() != null)
                mesh_filter.sharedMesh = changed_marker.GetSharedMesh();

            foreach (string media_material_key in changed_marker.GetMediaMaterialKeys())
            {
                ReleaseSharedMediaMaterial(media_material_key);
            }

            Mesh generated_mesh = changed_marker.GetGeneratedMesh();
            int generated_mesh_key = changed_marker.GetGeneratedMeshKey();
            if (generated_mesh_key != 0)
                ReleaseSharedProjectedMesh(generated_mesh_key);
            else if (generated_mesh != null)
                DestroyImmediate(generated_mesh);

            ReleaseSharedVideoPaint(changed_marker.GetSharedVideoKey());

            ChunkedMesh chunkedMeshes = game_object.GetComponent<ChunkedMesh>();
            if (chunkedMeshes != null && changed_marker.HasChunkedMeshState())
                chunkedMeshes.enabled = changed_marker.WasChunkedMeshEnabled();

            DestroyImmediate(changed_marker);
        }
    }
}
