using HarmonyLib;

namespace MaterialPainter2
{
    /// <summary>
    /// The game merges decorations into instanced chunks, hides their own renderer and draws the chunk instead, which
    /// ignores our materials. Chunking can switch on after a paint is applied (child parts get chunked as their object
    /// starts, and the game re-enables chunking after its own material swaps), so keep painted parts out of chunks.
    /// </summary>
    [HarmonyPatch(typeof(ChunkedMesh), "enableChunking")]
    public class ChunkedMeshPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ChunkedMesh __instance)
        {
            return __instance == null || __instance.GetComponent<PaintMarker>() == null;
        }
    }
}
