using HarmonyLib;

using System;
using System.Reflection;

using UnityEngine;

namespace MaterialPainter2
{
    [HarmonyPatch]
    public class CustomColorApplicationPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(CustomColorsUtility), "apply", parameters: new Type[] {
    typeof(Color[]), typeof(Renderer),typeof(bool), typeof(bool), typeof(int)});

        [HarmonyPrefix]
        private static bool apply(Color[] colors, Renderer renderer, bool forceApplyToAllMaterials, bool applyToParticleSystems = false, int slotIndexOffset = 0)
        {
            ChangedMarker cm = renderer.gameObject.GetComponent<ChangedMarker>();
            if (cm != null)
            {
                if (cm.GetCurrentBrush() == (int)MaterialBrush.Water || cm.GetCurrentBrush() == (int)MaterialBrush.Lava)
                {
                    //MP2.MPDebug("APPLY!");

                    Color c = colors[0];
                    c.a = 0.392156869f;

                    MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(materialPropertyBlock);
                    materialPropertyBlock.SetColor(Shader.PropertyToID("_Color"), c);
                    float H;
                    float S;
                    float V;
                    Color.RGBToHSV(c, out H, out S, out V);
                    c = Color.HSVToRGB(H, S, V / 2f);
                    c.a = 0.9019608f;
                    materialPropertyBlock.SetColor(Shader.PropertyToID("_Color2"), c);
                    materialPropertyBlock.SetColor(Shader.PropertyToID("_ShoreColor"), Color.HSVToRGB(H, S / 3f, V));
                    renderer.SetPropertyBlock(materialPropertyBlock);
                }
                else if (cm.GetCurrentBrush() == (int)MaterialBrush.Glass)
                {
                    //MP2.MPDebug("APPLY!");

                    Color c = colors[0];
                    c.a = 0.5f;

                    MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(materialPropertyBlock);
                    materialPropertyBlock.SetColor(Shader.PropertyToID("_Color"), c);
                    renderer.SetPropertyBlock(materialPropertyBlock);
                }
            }

            return true;
        }
    }
}
