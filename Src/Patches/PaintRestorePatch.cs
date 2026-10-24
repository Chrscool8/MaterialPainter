using HarmonyLib;

namespace MaterialPainter2
{
    /// <summary>Applies saved paints as soon as an object starts: loaded parks, placed blueprints and multiplayer spawns.</summary>
    [HarmonyPatch(typeof(SerializedMonoBehaviour), nameof(SerializedMonoBehaviour.eventStart))]
    public class PaintRestorePatch
    {
        [HarmonyPostfix]
        private static void Postfix(SerializedMonoBehaviour __instance)
        {
            Safe.Run(nameof(PaintRestorePatch), () => PaintStore.Restore(__instance));
        }
    }
}
