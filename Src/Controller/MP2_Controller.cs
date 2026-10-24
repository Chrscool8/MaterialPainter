using UnityEngine;

namespace MaterialPainter2
{
    public class MP2_Controller : MonoBehaviour
    {
        private static MaterialBrushTool brush_tool = null;
        public static Window_MultiSelector wms = null;

        private void Awake()
        {
            MP2.selected_brush = (int)MaterialBrush.None;
            MP2.selected_brush_custom = "";
            brush_tool = new MaterialBrushTool();
        }

        private void Update()
        {
            if (GameController.Instance != null && GameController.Instance.isActiveMouseTool(brush_tool))
                brush_tool.tick();
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

        public void OnObjectClicked(GameObject game_object)
        {
            MP2.ResetCountdown();

            if (game_object == null)
                return;

            if (MP2._setting_target_supports && !game_object.name.ToLower().Contains("support"))
                return;

            Painter.Paint(game_object, (MaterialBrush)MP2.selected_brush, MP2.selected_brush_custom, userAction: true);
        }
    }
}
