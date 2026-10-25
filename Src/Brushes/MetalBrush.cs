using UnityEngine;

namespace MaterialPainter2
{
    /// <summary>
    /// Shiny metal that takes the object's first custom color, built from one of the game's reflective custom-color
    /// materials.
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

        private readonly string sourceMaterialName;

        public MetalBrush(MaterialBrush id, string name, string icon, string sourceMaterialName, float metallic, float smoothness, float reflectivity, float reflectionBlur)
            : base(id, name, icon, () => BrushMaterials.CreateMetal(sourceMaterialName, id.ToString()), false,
                BrushParams.Metallic(metallic), BrushParams.Smoothness(smoothness), BrushParams.Reflectivity(reflectivity), BrushParams.ReflectionBlur(reflectionBlur))
        {
            this.sourceMaterialName = sourceMaterialName;
        }

        // Crossed out like a missing DLC brush; saved paints stay and show again once the material is there.
        public override string UnavailableReason => BrushMaterials.HasGameMaterial(sourceMaterialName)
            ? null
            : $"needs the game's '{sourceMaterialName}' material, which isn't loaded.";

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
