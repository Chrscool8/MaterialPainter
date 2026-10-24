using System.Linq;

using UnityEngine;

namespace MaterialPainter2
{
    public static class Painter
    {
        /// <summary>While true (painter window open), invisible paints render as ghosts so they can be found.</summary>
        public static bool PreviewInvisible { get; private set; }

        /// <summary>
        /// Paints the renderer on <paramref name="gameObject"/> and records it in the owning object's save data.
        /// <paramref name="userAction"/> plays the paint effects; restores from saves pass false.
        /// </summary>
        public static bool Paint(GameObject gameObject, MaterialBrush id, string detail, bool userAction)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer == null)
            {
                MP2.MPDebug($"Nothing to paint on {gameObject.name}.");
                return false;
            }

            if (id == MaterialBrush.None)
            {
                Clear(renderer);
                return true;
            }

            Brush brush = BrushRegistry.Get(id);
            if (brush == null)
            {
                MP2.MPDebug($"Unknown brush {(int)id}.");
                return false;
            }

            // Leave the part (and its saved paint) alone; it shows again where the brush is available.
            if (!brush.IsAvailable)
            {
                MP2.MPDebug(brush.UnavailableReason);
                return false;
            }

            detail = detail ?? "";
            PaintMarker marker = renderer.GetComponent<PaintMarker>();
            // Already painted, unless the game has since swapped its own materials back in; then repaint over them.
            if (marker != null && marker.Matches(brush.Id, detail) && !marker.IsOverridden)
                return true;

            bool applied = false;
            UnderReplacement(renderer, () =>
            {
                if (marker != null)
                    marker.Restore();

                marker = PaintMarker.Capture(renderer);
                applied = brush.Apply(renderer, detail, marker);
                if (!applied)
                    marker.Restore();
            });

            if (!applied)
            {
                if (userAction)
                    PaintStore.Forget(renderer);
                return false;
            }

            marker.SetPaint(brush, detail);
            PaintStore.Record(renderer, brush.Id, detail);

            if (userAction)
                PlayEffects(renderer.transform.position);

            LogState(renderer, "painted " + brush.Name);
            return true;
        }

        public static void Clear(Renderer renderer)
        {
            PaintMarker marker = renderer.GetComponent<PaintMarker>();
            if (marker != null)
                UnderReplacement(renderer, marker.Restore);

            PaintStore.Forget(renderer);
            LogState(renderer, "cleared");
        }

        // With the mp_debug flag: what the renderer looks like right after a paint, and again a second later in case
        // something in the game changes it afterwards.
        private static void LogState(Renderer renderer, string action)
        {
            if (!MP2.DebugEnabled)
                return;

            MP2.MPDebug($"[PaintState] {action} now: {DescribeState(renderer)}");
            CoroutineManager.DelayAction(1f, () =>
            {
                if (renderer != null)
                    MP2.MPDebug($"[PaintState] {action} +1s: {DescribeState(renderer)}");
            });
        }

        private static string DescribeState(Renderer renderer)
        {
            string materials = string.Join(", ", renderer.sharedMaterials.Select(material =>
                material == null ? "<null/destroyed>" : $"{material.name} [{(material.shader != null ? material.shader.name : "<no shader>")}]"));
            ChunkedMesh chunkedMesh = renderer.GetComponent<ChunkedMesh>();
            MaterialReplacementManager manager = renderer.GetComponent<MaterialReplacementManager>();
            return $"object='{renderer.name}' enabled={renderer.enabled} materials=[{materials}]"
                + $" chunk={(chunkedMesh == null ? "none" : chunkedMesh.enabled + "/handles " + chunkedMesh.instancedChunkHandles.Count)}"
                + $" replacement={(manager?.getActiveReplacementEntry()?.replacementTag ?? "none")}";
        }

        public static void OnColorsChanged(Renderer renderer, Color[] colors)
        {
            PaintMarker marker = renderer.GetComponent<PaintMarker>();
            if (marker != null && marker.brush != null)
                marker.brush.OnColorsChanged(renderer, colors);
        }

        public static void SetInvisiblePreview(bool preview)
        {
            PreviewInvisible = preview;
            foreach (PaintMarker marker in PaintMarker.All)
            {
                if (marker.brush is InvisibleBrush)
                {
                    Renderer renderer = marker.GetComponent<Renderer>();
                    UnderReplacement(renderer, () => renderer.enabled = preview);
                }
            }
        }

        /// <summary>Puts every painted renderer back to normal without touching save data (used when the mod unloads).</summary>
        public static void RestoreAll()
        {
            foreach (PaintMarker marker in PaintMarker.All.ToList())
                UnderReplacement(marker.GetComponent<Renderer>(), marker.Restore);
        }

        /// <summary>
        /// The game temporarily swaps an object's materials for things like underground view (tunnels), then puts back
        /// whatever it saw when the swap started. Lift the swap, change the real look, and let the game cover it again
        /// so it remembers the painted look instead.
        /// </summary>
        private static void UnderReplacement(Renderer renderer, System.Action change)
        {
            MaterialReplacementManager manager = renderer != null ? renderer.GetComponent<MaterialReplacementManager>() : null;
            bool covered = manager != null && manager.getActiveReplacementEntry() != null;
            if (covered)
                manager.onKill();

            try
            {
                change();
            }
            finally
            {
                if (covered)
                    manager.apply();
            }
        }

        private static void PlayEffects(Vector3 position)
        {
            AssetManager assetManager = ScriptableSingleton<AssetManager>.Instance;
            ShockwaveController.Instance.addShockwave(position, .5f, true);
            Object.Instantiate(assetManager.employeeLevelUpParticlesGO).transform.position = position;
            Object.Instantiate(assetManager.plopParticlesGO).transform.position = position;
            ScriptableSingleton<SoundAssetManager>.Instance.recolorObject.play2D();
        }
    }
}
