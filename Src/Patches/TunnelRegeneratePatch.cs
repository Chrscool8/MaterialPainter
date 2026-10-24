using HarmonyLib;

using UnityEngine;

namespace MaterialPainter2
{
    /// <summary>
    /// The game rebuilds coaster tunnels after a park loads (and on nearby terraforming) without registering the new
    /// tunnel meshes for mouse clicks, so they can't be painted. Register them like freshly built tunnels are, and
    /// re-apply the segment's saved paints to the new tunnels.
    /// </summary>
    [HarmonyPatch(typeof(TrackSegment4), nameof(TrackSegment4.regenerateTunnels))]
    public class TunnelRegeneratePatch
    {
        private const string TUNNEL_TAG = "Tunnel";

        [HarmonyPostfix]
        private static void Postfix(TrackSegment4 __instance)
        {
            if (__instance.isPreview)
                return;

            Safe.Run(nameof(TunnelRegeneratePatch), () =>
            {
                foreach (Transform child in __instance.transform)
                {
                    if (child.CompareTag(TUNNEL_TAG))
                        MouseCollisions.Instance.addColliders(__instance, child.gameObject);
                }

                PaintStore.Restore(__instance);
            });
        }
    }
}
