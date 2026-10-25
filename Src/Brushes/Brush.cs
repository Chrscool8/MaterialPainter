using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>Why the brush can't be used in the current park (e.g. missing DLC), or null if it can.</summary>
        public virtual string UnavailableReason => null;

        public bool IsAvailable => UnavailableReason == null;

        /// <summary>Applies the look to an already backed-up renderer. Returns false if the brush can't apply.</summary>
        public abstract bool Apply(Renderer renderer, string detail, PaintMarker marker);

        public virtual void OnColorsChanged(Renderer renderer, Color[] colors) { }

        /// <summary>Destroys any materials this brush created.</summary>
        public virtual void ReleaseCache() { }

        protected static void SetAllSlots(Renderer renderer, Material material)
        {
            // Every slot, including empty ones (some generated meshes start without a material).
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
                materials[i] = material;
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

    /// <summary>
    /// Paints every material slot with one material, created on first use and shared by all painted objects. Brushes
    /// with <see cref="Params"/> can be tuned: a paint's detail holds its slider values, and each distinct set of values
    /// gets its own copy of the material. An empty detail is the untuned look.
    /// </summary>
    public class SharedMaterialBrush : Brush
    {
        private readonly Func<Material> createMaterial;
        private readonly BrushParam[] parameters;
        private readonly Dictionary<string, Material> tunedMaterials = new Dictionary<string, Material>();
        private Material material;
        private BrushSettings preset;
        private BrushSettings current;

        public SharedMaterialBrush(MaterialBrush id, string name, string icon, Func<Material> createMaterial, bool wild = false, params BrushParam[] parameters)
            : base(id, name, icon, wild)
        {
            this.createMaterial = createMaterial;
            this.parameters = parameters ?? new BrushParam[0];
        }

        public IReadOnlyList<BrushParam> Params => parameters;

        public bool IsTunable => parameters.Length > 0;

        /// <summary>The untuned look's values. Null until the material can be made (e.g. a missing game material).</summary>
        public BrushSettings Preset
        {
            get
            {
                if (preset == null && GetMaterial() != null)
                    preset = new BrushSettings(parameters, parameters.Select(p => float.IsNaN(p.Default) ? ReadFloat(material, p) : p.Default));
                return preset;
            }
        }

        /// <summary>What the tuning sliders are set to: the settings the next paint with this brush gets.</summary>
        public BrushSettings Current
        {
            get => current ?? Preset;
            set => current = value;
        }

        /// <summary>The detail to paint with: empty while the sliders are untouched, so the paint keeps the default look.</summary>
        public string CurrentDetail
        {
            get
            {
                BrushSettings settings = Current;
                return !IsTunable || settings == null || settings.Equals(Preset) ? "" : settings.Encode(parameters);
            }
        }

        protected Material GetMaterial()
        {
            if (material == null)
            {
                material = createMaterial();
                if (material != null)
                {
                    material.enableInstancing = true;
                    // Explicit starting values; the rest are read from the material as it comes.
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (!float.IsNaN(parameters[i].Default) && material.HasProperty(parameters[i].PropertyId))
                            material.SetFloat(parameters[i].PropertyId, parameters[i].Default);
                    }
                }
            }
            return material;
        }

        protected Material GetMaterial(string detail)
        {
            Material untuned = GetMaterial();
            if (untuned == null || !IsTunable)
                return untuned;

            BrushSettings settings = BrushSettings.Decode(detail, parameters, Preset);
            if (settings.Equals(Preset))
                return untuned;

            string key = settings.Encode(parameters);
            if (tunedMaterials.TryGetValue(key, out Material tuned) && tuned != null)
                return tuned;

            tuned = new Material(untuned) { name = untuned.name + " [" + key + "]" };
            settings.ApplyTo(tuned, parameters);
            tunedMaterials[key] = tuned;
            return tuned;
        }

        private static float ReadFloat(Material material, BrushParam param)
        {
            return material.HasProperty(param.PropertyId) ? material.GetFloat(param.PropertyId) : param.Min;
        }

        public override bool Apply(Renderer renderer, string detail, PaintMarker marker)
        {
            Material shared = GetMaterial(detail);
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
            foreach (Material tuned in tunedMaterials.Values)
            {
                if (tuned != null)
                    UnityEngine.Object.Destroy(tuned);
            }
            tunedMaterials.Clear();

            if (material != null)
                UnityEngine.Object.Destroy(material);

            material = null;
            preset = null;
        }
    }

    public class WaterBrush : SharedMaterialBrush
    {
        private static readonly int colorID = Shader.PropertyToID("_Color");
        private static readonly int color2ID = Shader.PropertyToID("_Color2");
        private static readonly int shoreColorID = Shader.PropertyToID("_ShoreColor");

        private readonly WaterBody.BodyType bodyType;
        private readonly Color defaultColor;

        public WaterBrush(MaterialBrush id, string name, string icon, WaterBody.BodyType bodyType, Color defaultColor, params BrushParam[] parameters)
            : base(id, name, icon, () => BrushMaterials.CloneWaterBody(bodyType), false, parameters)
        {
            this.bodyType = bodyType;
            this.defaultColor = defaultColor;
        }

        private static Func<DLC_ID, bool> isDlcAvailable;

        // Lava is DLC content.
        public override string UnavailableReason
        {
            get
            {
                WaterBodyType type = BrushMaterials.FindWaterBodyType(bodyType);
                return type != null && IsDlcAvailable(type.dlc) ? null : Name + " needs its DLC enabled in this park.";
            }
        }

        // The game's own (internal) check: installed, and in multiplayer also enabled by the park. Resolved with plain
        // reflection, since other mod loaders may bring a different Harmony version. If it can't be found, assume yes.
        private static bool IsDlcAvailable(DLC_ID dlc)
        {
            if (isDlcAvailable == null)
            {
                isDlcAvailable = _ => true;
                try
                {
                    System.Reflection.MethodInfo method = typeof(GameController).Assembly.GetType("DLC")?.GetMethod("isOwnedAndEnabledInCurrentPark",
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (method != null)
                        isDlcAvailable = (Func<DLC_ID, bool>)Delegate.CreateDelegate(typeof(Func<DLC_ID, bool>), method);
                }
                catch (Exception ex)
                {
                    Safe.Report("DLC check lookup", ex);
                }
            }

            return isDlcAvailable(dlc);
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
