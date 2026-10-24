using HarmonyLib;

using System.Collections.Generic;

using UnityEngine;

namespace MaterialPainter2
{
    /// <summary>
    /// Flat ride platform tiles rebuild their fences (toggling "disable fences", neighbouring tile changes) after
    /// they've registered for mouse clicks, so the new fences can't be painted. Register them, and re-apply the
    /// tile's saved paints. Fences on the ride itself only exist on placement previews.
    /// </summary>
    [HarmonyPatch(typeof(FlatRidePlatform), nameof(FlatRidePlatform.spawnFences))]
    public class PlatformFencePatch
    {
        private static readonly AccessTools.FieldRef<FlatRidePlatform, List<GameObject>> fences =
            AccessTools.FieldRefAccess<FlatRidePlatform, List<GameObject>>("fences");

        [HarmonyPostfix]
        private static void Postfix(FlatRidePlatform __instance)
        {
            // Before the tile starts, its own start-up registers everything (and PaintRestorePatch paints it).
            List<GameObject> spawned = fences(__instance);
            if (__instance.isPreview || !__instance.hasBeenInitialized || spawned == null)
                return;

            foreach (GameObject fence in spawned)
            {
                if (fence != null)
                    MouseCollisions.Instance.addColliders(__instance, fence);
            }

            PaintStore.Restore(__instance);
        }
    }
}
