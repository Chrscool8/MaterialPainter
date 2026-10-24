using System;
using System.Collections.Generic;
using System.Linq;

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
        private Material[] paintedMaterials;
        private Mesh paintedMesh;
        private readonly List<Action> releases = new List<Action>();

        public static PaintMarker Capture(Renderer renderer)
        {
            // Added first: while it exists, ChunkedMeshPatch keeps the part out of the game's mesh chunks.
            PaintMarker marker = renderer.gameObject.AddComponent<PaintMarker>();
            marker.target = renderer;
            RechunkMesh(renderer.gameObject);

            marker.originalMaterials = renderer.sharedMaterials;
            marker.originalBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(marker.originalBlock);
            marker.originalEnabled = renderer.enabled;

            marker.meshFilter = renderer.GetComponent<MeshFilter>();
            if (marker.meshFilter != null)
                marker.originalMesh = marker.meshFilter.sharedMesh;

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
            paintedMaterials = target != null ? target.sharedMaterials : null;
            paintedMesh = meshFilter != null ? meshFilter.sharedMesh : null;
        }

        /// <summary>
        /// True when the game has since replaced the painted materials with its own (e.g. track pieces get their real
        /// material after loading), so the paint no longer shows and the saved originals are stale. Not counted while
        /// the game temporarily swaps materials itself (underground view, selection).
        /// </summary>
        public bool IsOverridden
        {
            get
            {
                if (target == null || paintedMaterials == null)
                    return false;

                MaterialReplacementManager manager = target.GetComponent<MaterialReplacementManager>();
                if (manager != null && manager.getActiveReplacementEntry() != null)
                    return false;

                return !target.sharedMaterials.SequenceEqual(paintedMaterials);
            }
        }

        /// <summary>Registers cleanup for a shared resource; runs on restore or if the object is destroyed while painted.</summary>
        public void AddRelease(Action release)
        {
            releases.Add(release);
        }

        public void Restore()
        {
            // Only undo what's still ours: if the game has replaced the materials or mesh since, its version is the
            // real look now and the saved originals may be placeholders or destroyed.
            if (target != null)
            {
                if (!IsOverridden)
                {
                    target.sharedMaterials = originalMaterials;
                    target.SetPropertyBlock(originalBlock);
                }
                target.enabled = originalEnabled;
            }

            if (meshFilter != null && originalMesh != null && meshFilter.sharedMesh == paintedMesh)
                meshFilter.sharedMesh = originalMesh;

            RunReleases();
            GameObject owner = gameObject;
            DestroyImmediate(this);
            RechunkMesh(owner);
        }

        /// <summary>
        /// Takes the part out of its mesh chunk, or puts it back once the marker is gone, by cycling its ChunkedMesh.
        /// The chunk may have been added after painting, so it's looked up fresh each time. A chunk the game has
        /// switched off itself (e.g. during its own material swaps) is left alone.
        /// </summary>
        private static void RechunkMesh(GameObject gameObject)
        {
            ChunkedMesh chunkedMesh = gameObject.GetComponent<ChunkedMesh>();
            if (chunkedMesh == null || !chunkedMesh.enabled)
                return;

            chunkedMesh.enabled = false;
            chunkedMesh.enabled = true;
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
