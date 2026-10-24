using HarmonyLib;

using System;
using System.Reflection;

using UnityEngine;

namespace MaterialPainter2
{
    /// <summary>Lets painted objects follow the game's recolor tool.</summary>
    [HarmonyPatch]
    public class CustomColorApplicationPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(CustomColorsUtility), "apply", parameters: new Type[] {
            typeof(Color[]), typeof(Renderer), typeof(bool), typeof(bool), typeof(int)});

        [HarmonyPostfix]
        private static void Postfix(Color[] colors, Renderer renderer)
        {
            if (renderer != null && colors != null && colors.Length > 0)
                Painter.OnColorsChanged(renderer, colors);
        }
    }
}
