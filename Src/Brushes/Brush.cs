using System;

using UnityEngine;

namespace MaterialPainter2
{
    public abstract class Brush
    {
        public MaterialBrush Id { get; }
        public string Name { get; }
        public string Icon { get; }

        /// <summary>Experimental brushes only shown with the "Go Wild" setting.</summary>
        public bool Wild { get; }

        protected Brush(MaterialBrush id, string name, string icon, bool wild = false)
        {
            Id = id;
            Name = name;
            Icon = icon;
            Wild = wild;
        }

        /// <summary>Applies the look to an already backed-up renderer. Returns false if the brush can't apply.</summary>
        public abstract bool Apply(Renderer renderer, string detail, PaintMarker marker);

        public virtual void OnColorsChanged(Renderer renderer, Color[] colors) { }

        /// <summary>Destroys any materials this brush created.</summary>
        public virtual void ReleaseCache() { }

        protected static void SetAllSlots(Renderer renderer, Material material)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null)
                    materials[i] = material;
            }
            renderer.sharedMaterials = materials;
        }

        protected static Color GetObjectColor(Renderer renderer, Color fallback)
        {
            CustomColors customColors = renderer.GetComponentInParent<CustomColors>();
            Color[] colors = customColors != null ? customColors.getColors() : null;
            return colors != null && colors.Length > 0 ? colors[0] : fallback;
        }

        protected static void SetBlockColors(Renderer renderer, Action<MaterialPropertyBlock> setColors)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            setColors(block);
            renderer.SetPropertyBlock(block);
        }
    }

    /// <summary>Paints every material slot with one material, created on first use and shared by all painted objects.</summary>
    public class SharedMaterialBrush : Brush
    {
        private readonly Func<Material> createMaterial;
        private Material material;

        public SharedMaterialBrush(MaterialBrush id, string name, string icon, Func<Material> createMaterial, bool wild = false)
            : base(id, name, icon, wild)
        {
            this.createMaterial = createMaterial;
        }

        protected Material GetMaterial()
        {
            if (material == null)
            {
                material = createMaterial();
                if (material != null)
                    material.enableInstancing = true;
            }
            return material;
        }

        public override bool Apply(Renderer renderer, string detail, PaintMarker marker)
        {
            Material shared = GetMaterial();
            if (shared == null)
            {
                MP2.MPDebug($"No material available for brush {Name}.");
                return false;
            }

            SetAllSlots(renderer, shared);
            renderer.SetPropertyBlock(new MaterialPropertyBlock());
            renderer.enabled = true;
            ApplyColors(renderer, GetObjectColor(renderer, DefaultColor));
            return true;
        }

        public override void OnColorsChanged(Renderer renderer, Color[] colors)
        {
            if (colors != null && colors.Length > 0)
                ApplyColors(renderer, colors[0]);
        }

        protected virtual Color DefaultColor => Color.white;

        protected virtual void ApplyColors(Renderer renderer, Color color) { }

        public override void ReleaseCache()
        {
            if (material != null)
                UnityEngine.Object.Destroy(material);

            material = null;
        }
    }

    public class WaterBrush : SharedMaterialBrush
    {
        private static readonly int colorID = Shader.PropertyToID("_Color");
        private static readonly int color2ID = Shader.PropertyToID("_Color2");
        private static readonly int shoreColorID = Shader.PropertyToID("_ShoreColor");

        private readonly Color defaultColor;

        public WaterBrush(MaterialBrush id, string name, string icon, WaterBody.BodyType bodyType, Color defaultColor)
            : base(id, name, icon, () => BrushMaterials.CloneWaterBody(bodyType))
        {
            this.defaultColor = defaultColor;
        }

        protected override Color DefaultColor => defaultColor;

        // Mirrors how the game tints its own water bodies.
        protected override void ApplyColors(Renderer renderer, Color color)
        {
            SetBlockColors(renderer, block =>
            {
                color.a = 0.392156869f;
                block.SetColor(colorID, color);
                Color.RGBToHSV(color, out float h, out float s, out float v);
                Color deep = Color.HSVToRGB(h, s, v / 2f);
                deep.a = 0.9019608f;
                block.SetColor(color2ID, deep);
                block.SetColor(shoreColorID, Color.HSVToRGB(h, s / 3f, v));
            });
        }
    }

    public class GlassBrush : SharedMaterialBrush
    {
        private static readonly int colorID = Shader.PropertyToID("_Color");

        public GlassBrush(MaterialBrush id, string name, string icon)
            : base(id, name, icon, () => BrushMaterials.Clone(ScriptableSingleton<AssetManager>.Instance.seeThroughMaterial))
        {
        }

        protected override void ApplyColors(Renderer renderer, Color color)
        {
            color.a = 0.5f;
            SetBlockColors(renderer, block => block.SetColor(colorID, color));
        }
    }

    /// <summary>Hidden in play; shown as a ghost while the painter window is open.</summary>
    public class InvisibleBrush : SharedMaterialBrush
    {
        public InvisibleBrush(MaterialBrush id, string name, string icon)
            : base(id, name, icon, () => BrushMaterials.Clone(ScriptableSingleton<AssetManager>.Instance.multiplayerBuildPreviewGhostMaterial))
        {
        }

        public override bool Apply(Renderer renderer, string detail, PaintMarker marker)
        {
            if (!base.Apply(renderer, detail, marker))
                return false;

            renderer.enabled = Painter.PreviewInvisible;
            return true;
        }
    }

    public class ImageBrush : Brush
    {
        public ImageBrush(MaterialBrush id, string name, string icon) : base(id, name, icon) { }

        public override bool Apply(Renderer renderer, string detail, PaintMarker marker)
        {
            Texture2D texture = MP2.GetCustomImageTexture(detail);
            if (texture == null)
            {
                MP2.MPDebug($"Couldn't load image {detail}.");
                return false;
            }

            renderer.enabled = true;
            MediaPaint.Apply(renderer, texture, marker);
            return true;
        }
    }

    public class VideoBrush : Brush
    {
        public VideoBrush(MaterialBrush id, string name, string icon) : base(id, name, icon) { }

        public override bool Apply(Renderer renderer, string detail, PaintMarker marker)
        {
            string videoPath = MP2.GetCustomVideoFilePath(detail);
            if (!System.IO.File.Exists(videoPath))
            {
                MP2.MPDebug($"Couldn't find {videoPath}, using fallback.");
                videoPath = MP2.GetFallbackVideoPath();
                if (!System.IO.File.Exists(videoPath))
                    return false;
            }

            renderer.enabled = true;
            MediaPaint.ApplyVideo(renderer, videoPath, detail, marker);
            return true;
        }
    }
}
