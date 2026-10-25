using UnityEngine;

namespace MaterialPainter2
{
    /// <summary>
    /// Shiny metal that takes the object's first custom color, built from one of the game's reflective custom-color
    /// materials (the first of its sources that's loaded).
    /// </summary>
    public class MetalBrush : SharedMaterialBrush
    {
        private static readonly int[] customColorIDs =
        {
            Shader.PropertyToID("_CustomColor1"),
            Shader.PropertyToID("_CustomColor2"),
            Shader.PropertyToID("_CustomColor3"),
            Shader.PropertyToID("_CustomColor4"),
        };

        private readonly string[] sourceMaterialNames;

        public MetalBrush(MaterialBrush id, string name, string icon, string[] sourceMaterialNames, bool plain, float metallic, float smoothness, float reflectivity, float reflectionBlur)
            : base(id, name, icon, () => CreateMaterial(sourceMaterialNames, id, plain), false,
                BrushParams.Metallic(metallic), BrushParams.Smoothness(smoothness), BrushParams.Reflectivity(reflectivity), BrushParams.ReflectionBlur(reflectionBlur))
        {
            this.sourceMaterialNames = sourceMaterialNames;
        }

        private static Material CreateMaterial(string[] sourceMaterialNames, MaterialBrush id, bool plain)
        {
            string source = BrushMaterials.FirstGameMaterial(sourceMaterialNames);
            return source != null ? BrushMaterials.CreateMetal(source, id.ToString(), plain) : null;
        }

        // Crossed out like a missing DLC brush; saved paints stay and show again once the material is there.
        public override string UnavailableReason => BrushMaterials.FirstGameMaterial(sourceMaterialNames) != null
            ? null
            : $"needs the game's '{string.Join("' or '", sourceMaterialNames)}' material, which isn't loaded.";

        // The game's custom-color shaders take per-instance colors from its chunk renderer when instanced, and ignore the
        // property block colors set below, which leaves the material's default red. Drawn normally, they read the block.
        protected override bool UseInstancing => false;

        // Every slot gets the same color, so the whole part is one color whichever mask channel the shader reads.
        protected override void ApplyColors(Renderer renderer, Color color)
        {
            SetBlockColors(renderer, block =>
            {
                foreach (int id in customColorIDs)
                    block.SetColor(id, color);
            });
        }
    }
}
