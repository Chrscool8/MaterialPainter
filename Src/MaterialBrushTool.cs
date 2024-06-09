internal class MaterialBrushTool : AbstractMouseTool
{
    public override void onMouseToolEnable()
    {
        base.onMouseToolEnable();
        ScriptableSingleton<CursorManager>.Instance.setCursorType(CursorType.COLOR_DRAWING);
    }

    public override void onMouseToolDisable()
    {
        base.onMouseToolDisable();
        if (ScriptableSingleton<CursorManager>.Instance.cursorType == CursorType.COLOR_DRAWING)
        {
            ScriptableSingleton<CursorManager>.Instance.setCursorType(CursorType.DEFAULT);
        }
    }

    public override bool canEscapeMouseTool()
    {
        return true;
    }

    public override GameController.GameFeature getDisallowedFeatures()
    {
        return GameController.GameFeature.Picking;
    }
}