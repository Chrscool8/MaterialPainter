using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using UnityEngine;

namespace MaterialPainter2
{
    /// <summary>One slider of the tuning panel: a float property of the brush's material.</summary>
    public sealed class BrushParam
    {
        /// <summary>Short name the value is saved under. Saved in paints, so never rename one.</summary>
        public string Key { get; }
        public string Label { get; }
        public string Property { get; }
        public float Min { get; }
        public float Max { get; }
        public float Step { get; }
        public string Tooltip { get; }

        /// <summary>Starting value; NaN uses whatever the brush's material comes with.</summary>
        public float Default { get; }

        public int PropertyId { get; }

        public BrushParam(string key, string label, string property, float min, float max, float step, string tooltip, float defaultValue = float.NaN)
        {
            Key = key;
            Label = label;
            Property = property;
            Min = min;
            Max = max;
            Step = step;
            Tooltip = tooltip;
            Default = defaultValue;
            PropertyId = Shader.PropertyToID(property);
        }

        public float Normalize(float value)
        {
            return Mathf.Clamp(Mathf.Round(value / Step) * Step, Min, Max);
        }

        public string Format(float value)
        {
            int decimals = Mathf.Max(0, Mathf.CeilToInt(-Mathf.Log10(Step) - 0.0001f));
            return value.ToString("F" + decimals, CultureInfo.InvariantCulture);
        }
    }

    /// <summary>The sliders brushes share. Leaving out the default starts from the material's own value.</summary>
    public static class BrushParams
    {
        public static BrushParam Metallic(float defaultValue = float.NaN) =>
            new BrushParam("m", "Metallic", "_Metallic", 0f, 1f, 0.01f, "How much it reflects like metal instead of plastic.", defaultValue);

        public static BrushParam Smoothness(float defaultValue = float.NaN) =>
            new BrushParam("s", "Smoothness", "_Glossiness", 0f, 1f, 0.01f, "How tight and sharp the highlights are.", defaultValue);

        public static BrushParam Reflectivity(float defaultValue = float.NaN) =>
            new BrushParam("r", "Reflection", "_Reflectivity", 0f, 1f, 0.01f, "How strongly the surroundings show in it.", defaultValue);

        public static BrushParam ReflectionBlur(float defaultValue = float.NaN) =>
            new BrushParam("b", "Blur", "_ReflectionBlur", 0f, 7f, 0.1f, "How blurry the reflection is.", defaultValue);

        public static BrushParam TextureScale() =>
            new BrushParam("t", "Texture Size", "_TextureScale", 0.1f, 8f, 0.05f, "Scale of the texture pattern.");

        public static BrushParam Shininess() =>
            new BrushParam("s", "Shininess", "_Shininess", 0.01f, 1f, 0.01f, "How sharp the highlights on the surface are.");

        public static BrushParam WaveHeight() =>
            new BrushParam("w", "Waves", "_WaveHeight", 0.005f, 0.3f, 0.005f, "How tall the surface waves are.");

        public static BrushParam Ripples() =>
            new BrushParam("n", "Ripples", "_NormalMultiplier", 0.1f, 10f, 0.1f, "Strength of the surface ripples.");

        public static BrushParam ShineIntensity(float max) =>
            new BrushParam("i", "Glow", "_ShineIntensity", 0f, max, max >= 100f ? 1f : 0.5f, "How bright the glow is.");
    }

    /// <summary>A brush's slider values, in the order of its <see cref="BrushParam"/>s. Saved with each paint as its detail.</summary>
    public sealed class BrushSettings : IEquatable<BrushSettings>
    {
        private readonly float[] values;

        public BrushSettings(IReadOnlyList<BrushParam> parameters, IEnumerable<float> values)
        {
            this.values = values.Select((value, i) => parameters[i].Normalize(value)).ToArray();
        }

        public float this[int index] => values[index];

        public BrushSettings With(IReadOnlyList<BrushParam> parameters, int index, float value)
        {
            float[] changed = (float[])values.Clone();
            changed[index] = value;
            return new BrushSettings(parameters, changed);
        }

        // "m=0.90;s=0.75;r=1.00;b=2.0"
        public string Encode(IReadOnlyList<BrushParam> parameters)
        {
            return string.Join(";", parameters.Select((p, i) => p.Key + "=" + p.Format(values[i])));
        }

        /// <summary>Reads a saved detail; anything missing or unreadable keeps the value from <paramref name="fallback"/>.</summary>
        public static BrushSettings Decode(string detail, IReadOnlyList<BrushParam> parameters, BrushSettings fallback)
        {
            if (string.IsNullOrEmpty(detail))
                return fallback;

            float[] decoded = fallback.values.ToArray();
            foreach (string pair in detail.Split(';'))
            {
                int separator = pair.IndexOf('=');
                if (separator < 0 || !float.TryParse(pair.Substring(separator + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                    continue;

                string key = pair.Substring(0, separator);
                for (int i = 0; i < parameters.Count; i++)
                {
                    if (parameters[i].Key == key)
                        decoded[i] = value;
                }
            }
            return new BrushSettings(parameters, decoded);
        }

        public void ApplyTo(Material material, IReadOnlyList<BrushParam> parameters)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                if (material.HasProperty(parameters[i].PropertyId))
                    material.SetFloat(parameters[i].PropertyId, values[i]);
            }
        }

        public bool Equals(BrushSettings other) => other != null && values.SequenceEqual(other.values);

        public override bool Equals(object obj) => Equals(obj as BrushSettings);

        public override int GetHashCode() => values.Aggregate(17, (hash, value) => hash * 31 + value.GetHashCode());
    }
}
