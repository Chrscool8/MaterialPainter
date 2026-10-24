using System;
using System.Collections.Generic;

using UnityEngine;

namespace MaterialPainter2
{
    /// <summary>Lives on a painted renderer: remembers its original look and the shared resources the paint holds.</summary>
    [DisallowMultipleComponent]
    public class PaintMarker : MonoBehaviour
    {
        public static readonly HashSet<PaintMarker> All = new HashSet<PaintMarker>();

        public Brush brush { get; private set; }
        public string detail { get; private set; } = "";

        private Renderer target;
        private Material[] originalMaterials;
        private MaterialPropertyBlock originalBlock;
        private bool originalEnabled;
        private MeshFilter meshFilter;
        private Mesh originalMesh;
        private ChunkedMesh chunkedMesh;
        private bool chunkedMeshWasEnabled;
        private readonly List<Action> releases = new List<Action>();

        public static PaintMarker Capture(Renderer renderer)
        {
            PaintMarker marker = renderer.gameObject.AddComponent<PaintMarker>();
            marker.target = renderer;
            marker.originalMaterials = renderer.sharedMaterials;
            marker.originalBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(marker.originalBlock);
            marker.originalEnabled = renderer.enabled;

            marker.meshFilter = renderer.GetComponent<MeshFilter>();
            if (marker.meshFilter != null)
                marker.originalMesh = marker.meshFilter.sharedMesh;

            // Chunked meshes are merged into shared chunks, so the renderer only shows our materials once unchunked.
            marker.chunkedMesh = renderer.GetComponent<ChunkedMesh>();
            if (marker.chunkedMesh != null)
            {
                marker.chunkedMeshWasEnabled = marker.chunkedMesh.enabled;
                marker.chunkedMesh.enabled = false;
            }

            All.Add(marker);
            return marker;
        }

        public bool Matches(MaterialBrush id, string detail)
        {
            return brush != null && brush.Id == id && this.detail == (detail ?? "");
        }

        public void SetPaint(Brush brush, string detail)
        {
            this.brush = brush;
            this.detail = detail ?? "";
        }

        /// <summary>Registers cleanup for a shared resource; runs on restore or if the object is destroyed while painted.</summary>
        public void AddRelease(Action release)
        {
            releases.Add(release);
        }

        public void Restore()
        {
            if (target != null)
            {
                target.sharedMaterials = originalMaterials;
                target.SetPropertyBlock(originalBlock);
                target.enabled = originalEnabled;
            }

            if (meshFilter != null && originalMesh != null)
                meshFilter.sharedMesh = originalMesh;

            if (chunkedMesh != null)
                chunkedMesh.enabled = chunkedMeshWasEnabled;

            RunReleases();
            DestroyImmediate(this);
        }

        private void RunReleases()
        {
            foreach (Action release in releases)
                release();

            releases.Clear();
        }

        private void OnDestroy()
        {
            RunReleases();
            All.Remove(this);
        }
    }
}
