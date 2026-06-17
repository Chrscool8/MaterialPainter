using UnityEngine;
using UnityEngine.Video;

namespace MaterialPainter2
{
    public partial class MP2_Controller
    {
        private void BackupMaterial(GameObject game_object)
        {
            //MP2.MPDebug("BACKUP!");

            ChunkedMesh chunked_meshes = game_object.GetComponent<ChunkedMesh>();
            if (chunked_meshes != null)
                chunked_meshes.enabled = false;

            Renderer renderer = game_object.GetComponent<Renderer>();
            VideoPlayer video_player = game_object.GetComponent<VideoPlayer>();

            if (renderer != null || video_player != null)
            {
                ChangedMarker changed_marker = game_object.AddComponent<ChangedMarker>();

                if (renderer != null)
                {
                    changed_marker.SetMaterials(renderer.materials);
                    MaterialPropertyBlock old_block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(old_block);
                    changed_marker.SetMaterialPropertyBlock(old_block);
                    changed_marker.SetEnabled(renderer.enabled);
                }
                else
                    MP2.MPDebug("No Renderer For: " + game_object.name);

                if (video_player != null)
                {
                    changed_marker.SetVideoPlayer(video_player);
                }
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

            VideoPlayer video_player = changed_marker.GetVideoPlayer();
            if (video_player != null)
            {
                //game_object.AddComponent<VideoPlayer>(video_player);
                DestroyImmediate(video_player);
            }

            DestroyImmediate(changed_marker);

            ChunkedMesh chunkedMeshes = game_object.GetComponent<ChunkedMesh>();
            if (chunkedMeshes != null)
                chunkedMeshes.enabled = true;
        }
    }
}
