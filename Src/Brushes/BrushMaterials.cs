using System;

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
