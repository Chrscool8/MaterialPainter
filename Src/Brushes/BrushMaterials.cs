using System;
using System.Collections.Generic;

using UnityEngine;

namespace MaterialPainter2
{
    /// <summary>Factories for brush materials. Each call creates a new material owned by the caller.</summary>
    public static class BrushMaterials
    {
        private const string TRIPLANAR_SHADER_NAME = "Rollercoaster/Triplanar";
        private const string WATERFALL_SHADER_NAME = "Rollercoaster/CustomColorWaterFountainFalling3";
        private const string BLACK_HOLE_CORE_SHADER_NAME = "Rollercoaster/BlackHoleCore";
        private const string BLACK_HOLE_DISC_SHADER_NAME = "Rollercoaster/BlackHoleAccretionDisc";
        private const string FALLBACK_SHADER_NAME = "Standard";
        private const string REFLECTION_SOURCE_MATERIAL_NAME = "CustomColorsSpecular";
        private const float MISSING_MATERIAL_RECHECK_SECONDS = 5f;

        /// <summary>
        /// Game materials the plain metals (Chrome, Polished, Glossy) are built from, first one loaded wins. They use the
        /// game's masked custom-color shader, which takes per-part colors; the plain custom-color one doesn't.
        /// </summary>
        public static readonly string[] PLAIN_METAL_SOURCES = { "Hangar_Metal_Mat", "metalroof_mat" };

        private static readonly Dictionary<string, Material> gameMaterials = new Dictionary<string, Material>();
        private static readonly Dictionary<string, float> missingMaterialCheckedAt = new Dictionary<string, float>();
        private static Texture2D colorMaskTexture;
        private static Texture2D flatNormalTexture;

        public static Material Clone(Material source)
        {
            return source != null ? new Material(source) : null;
        }

        // Looked up directly: going through WaterBody would silently fall back to water when lava's DLC is off.
        public static Material CloneWaterBody(WaterBody.BodyType bodyType)
        {
            WaterBodyType type = FindWaterBodyType(bodyType);
            return Clone(type != null ? type.material : null);
        }

        public static WaterBodyType FindWaterBodyType(WaterBody.BodyType bodyType)
        {
            WaterBodyType[] types = ScriptableSingleton<AssetManager>.Instance.waterBodyTypes;
            return types != null ? Array.Find(types, type => type.bodyType == bodyType) : null;
        }

        public static Material CreateTerrain(string terrainTypeName, string materialName, Color color, float textureScale)
        {
            TerrainType terrainType = FindTerrainType(terrainTypeName);
            Texture texture = terrainType != null ? terrainType.groundTexture : null;
            if (texture == null)
            {
                MP2.MPDebug($"Terrain texture not found for '{terrainTypeName}', using white texture for '{materialName}'.");
                texture = Texture2D.whiteTexture;
            }

            Material material = Create(TRIPLANAR_SHADER_NAME, "MP2_" + materialName)
                ?? Clone(terrainType != null ? terrainType.cliffMaterial : null);
            if (material == null)
                return null;

            SetMainTexture(material, texture);
            SetColor(material, "_Color", color);
            SetFloat(material, "_TextureScale", textureScale);
            SetFloat(material, "_TriplanarBlendSharpness", 1f);
            return material;
        }

        public static Material CreateWaterfall()
        {
            Material material = Create(WATERFALL_SHADER_NAME, "MP2_Waterfall");
            if (material == null)
                return null;

            Texture flowPattern = MP2.GetBundledSpriteTexture("effect_water_flowing_pattern");
            Color waterColor = new Color(0.32941177f, 0.76862746f, 0.96862745f, 0.22352941f);
            SetTexture(material, "_BumpMap", MP2.GetBundledSpriteTexture("effect_water_normal"));
            SetTexture(material, "_Edge", MP2.GetBundledSpriteTexture("effect_water_flowing_edge"));
            SetTexture(material, "_FlowPattern", flowPattern);
            SetTexture(material, "_Noise", MP2.GetBundledSpriteTexture("effect_fountain_water_transparency_noise"));
            SetMainTexture(material, flowPattern);
            SetColor(material, "_Color", waterColor);
            SetColor(material, "_CustomColor1", waterColor);
            SetColor(material, "_WaterColor", waterColor);
            SetColor(material, "_SpecularColor", new Color(0.1838235f, 0.1838235f, 0.1838235f, 1f));
            SetFloat(material, "_CustomColors", 0f);
            SetFloat(material, "_Distortions", 1f);
            SetFloat(material, "_BumpStrength", 0.38f);
            SetFloat(material, "_BumpVerticalSpeed", 1.35f);
            SetFloat(material, "_BumpHorizontalSpeed", 0.1f);
            SetFloat(material, "_DistortionStrength", 1f);
            SetFloat(material, "_EdgeFuzzyness", 0.059f);
            SetFloat(material, "_FlowPatternTransparency", 0.125f);
            SetFloat(material, "_FlowPatternVerticalSpeed", 1.43f);
            SetFloat(material, "_FlowPatternHorizontalSpeed", 0.2f);
            SetFloat(material, "_FlowRippleSpeed", 0.2f);
            SetFloat(material, "_Glossiness", 0.822f);
            SetFloat(material, "_NormalMultiplier", 4.36f);
            SetFloat(material, "_WaveHeight", 0.025f);
            return material;
        }

        public static Material CreateBlackHoleCore()
        {
            Material material = Create(BLACK_HOLE_CORE_SHADER_NAME, "MP2_BlackHoleCore");
            if (material == null)
                return null;

            SetMainTexture(material, MP2.GetBundledSpriteTexture("effect_black_hole_core"));
            SetColor(material, "_Color", Color.white);
            SetFloat(material, "_InnerRadius", 0.53f);
            SetFloat(material, "_Range", 0.9f);
            SetFloat(material, "_ShineIntensity", 200f);
            return material;
        }

        public static Material CreateBlackHoleDisc()
        {
            Material material = Create(BLACK_HOLE_DISC_SHADER_NAME, "MP2_BlackHoleAccretionDisc");
            if (material == null)
                return null;

            Texture colors = MP2.GetBundledSpriteTexture("effect_accretion_disc_colors");
            SetTexture(material, "_ColorsTex", colors);
            SetTexture(material, "_NoiseTex", MP2.GetBundledSpriteTexture("effect_accretion_disc_noise"));
            SetMainTexture(material, colors);
            SetColor(material, "_Color", Color.white);
            SetFloat(material, "_InnerRadius", 0.6f);
            SetFloat(material, "_Range", 0.9f);
            SetFloat(material, "_ShineIntensity", 16f);
            return material;
        }

        /// <summary>
        /// A colorable metal from one of the game's custom-color materials, with the object's texture swapped for a
        /// solid color mask so the whole part takes the custom color. <paramref name="plain"/> also flattens the source's
        /// surface bumps. Null if the game material isn't loaded.
        /// </summary>
        public static Material CreateMetal(string sourceMaterialName, string materialName, bool plain)
        {
            Material source = FindGameMaterial(sourceMaterialName);
            if (source == null)
                return null;

            Material reflective = FindGameMaterial(REFLECTION_SOURCE_MATERIAL_NAME);
            Material material = new Material(source) { name = "MP2_" + materialName };

            // The custom-color shaders tint where their mask is pure red (color 1), green, blue or magenta.
            if (material.HasProperty("_MaskTex"))
            {
                SetTexture(material, "_MaskTex", GetColorMaskTexture());
                SetMainTexture(material, Texture2D.whiteTexture);
            }
            else
            {
                SetMainTexture(material, GetColorMaskTexture());
            }

            if (plain && material.HasProperty("_NormalMap"))
            {
                SetTexture(material, "_NormalMap", GetFlatNormalTexture());
                material.DisableKeyword("_NORMALMAP");
            }

            // Wear effects would cover the shine.
            material.DisableKeyword("PARKITECT_GRIME");
            material.DisableKeyword("PARKITECT_SMUDGES");
            SetFloat(material, "_Grime", 0f);
            SetFloat(material, "_Smudges", 0f);
            SetFloat(material, "_Illum", 0f);

            if (material.HasProperty("_ReflectionCubemap") && material.GetTexture("_ReflectionCubemap") == null && reflective != null
                && reflective.HasProperty("_ReflectionCubemap"))
                material.SetTexture("_ReflectionCubemap", reflective.GetTexture("_ReflectionCubemap"));

            bool reflects = material.HasProperty("_ReflectionCubemap") && material.GetTexture("_ReflectionCubemap") != null;
            if (reflects)
                material.EnableKeyword("PARKITECT_REFLECTIONS");
            else
                material.DisableKeyword("PARKITECT_REFLECTIONS");
            SetFloat(material, "_Reflection", reflects ? 1f : 0f);
            return material;
        }

        public static bool HasGameMaterial(string name) => FindGameMaterial(name) != null;

        /// <summary>The first of <paramref name="names"/> that's loaded, or null.</summary>
        public static string FirstGameMaterial(string[] names) => Array.Find(names, HasGameMaterial);

        private static Material FindGameMaterial(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            if (gameMaterials.TryGetValue(name, out Material cached) && cached != null)
                return cached;

            // Brush buttons ask every frame; a missing material is only searched for again after a while.
            if (missingMaterialCheckedAt.TryGetValue(name, out float checkedAt) && Time.unscaledTime - checkedAt < MISSING_MATERIAL_RECHECK_SECONDS)
                return null;

            Material[] objectMaterials = ScriptableSingleton<AssetManager>.Instance?.objectMaterials;
            Material found = objectMaterials != null ? Array.Find(objectMaterials, m => m != null && m.name == name) : null;
            // Materials only used by specific objects aren't in the asset manager's list, but are loaded with them.
            if (found == null)
                found = Array.Find(Resources.FindObjectsOfTypeAll<Material>(), m => m != null && m.name == name);

            if (found == null)
            {
                MP2.MPDebug($"Game material '{name}' not found.");
                missingMaterialCheckedAt[name] = Time.unscaledTime;
            }
            else
            {
                gameMaterials[name] = found;
                missingMaterialCheckedAt.Remove(name);
            }
            return found;
        }

        private static Texture2D GetColorMaskTexture()
        {
            if (colorMaskTexture == null)
            {
                colorMaskTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "MP2_ColorMask" };
                colorMaskTexture.SetPixel(0, 0, Color.red);
                colorMaskTexture.Apply();
            }
            return colorMaskTexture;
        }

        // A normal pointing straight out, in the layout the game's shaders unpack (x from red times alpha, y from green).
        private static Texture2D GetFlatNormalTexture()
        {
            if (flatNormalTexture == null)
            {
                flatNormalTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false, linear: true) { name = "MP2_FlatNormal" };
                flatNormalTexture.SetPixel(0, 0, new Color(1f, 0.5f, 1f, 0.5f));
                flatNormalTexture.Apply();
            }
            return flatNormalTexture;
        }

        private static Material Create(string shaderName, string materialName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                MP2.MPDebug($"Shader '{shaderName}' not found, trying '{FALLBACK_SHADER_NAME}'.", always_show: true);
                shader = Shader.Find(FALLBACK_SHADER_NAME);
            }

            return shader != null ? new Material(shader) { name = materialName } : null;
        }

        private static TerrainType FindTerrainType(string terrainTypeName)
        {
            AssetManager assetManager = ScriptableSingleton<AssetManager>.Instance;
            if (string.IsNullOrEmpty(terrainTypeName) || assetManager == null || assetManager.terrainTypes == null)
                return null;

            foreach (TerrainType terrainType in assetManager.terrainTypes)
            {
                if (terrainType != null && string.Equals(terrainType.name, terrainTypeName, StringComparison.OrdinalIgnoreCase))
                    return terrainType;
            }

            MP2.MPDebug($"Terrain type '{terrainTypeName}' not found.", always_show: true);
            return null;
        }

        private static void SetMainTexture(Material material, Texture texture)
        {
            if (texture == null)
                return;

            material.mainTexture = texture;
            SetTexture(material, "_MainTex", texture);
            SetTexture(material, "_BaseMap", texture);
        }

        private static void SetTexture(Material material, string property, Texture texture)
        {
            if (texture != null && material.HasProperty(property))
                material.SetTexture(property, texture);
        }

        private static void SetColor(Material material, string property, Color color)
        {
            if (material.HasProperty(property))
                material.SetColor(property, color);
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property))
                material.SetFloat(property, value);
        }
    }
}
