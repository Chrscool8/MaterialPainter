using UnityEngine;

namespace MaterialPainter2
{
    public partial class MP2_Controller : MonoBehaviour
    {
        private static IMouseTool brush_tool = null;
        public static Window_MultiSelector wms = null;

        public MP2_Controller()
        {
            MP2.selected_brush = (int)MaterialBrush.None;
            MP2.selected_brush_custom = "";
            brush_tool = new MaterialBrushTool();
        }

        public void ActivatePipe()
        {
            MP2.MPDebug("enabled mouse tool");

            if (!GameController.Instance.isActiveMouseTool(brush_tool))
            {
                GameController.Instance.enableMouseTool(brush_tool);
                MP2.pipe_active = true;
            }
        }

        public void DeactivatePipe()
        {
            MP2.MPDebug("disabled mouse tool");

            if (GameController.Instance.isActiveMouseTool(brush_tool))
            {
                GameController.Instance.removeMouseTool(brush_tool);
                MP2.pipe_active = false;
            }
        }
    }
}
