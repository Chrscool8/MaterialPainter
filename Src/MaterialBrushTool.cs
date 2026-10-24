using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace MaterialPainter2
{
    /// <summary>
    /// The painting mouse tool. Like the game's own tools it reads input in <see cref="tick"/>, which its owner
    /// (<see cref="MP2_Controller"/>) calls every frame while the tool is active.
    /// </summary>
    public class MaterialBrushTool : AbstractMouseTool, IMouseToolTicked
    {
        public override void onMouseToolEnable()
        {
            base.onMouseToolEnable();
            ScriptableSingleton<CursorManager>.Instance.setCursorType(CursorType.COLOR_DRAWING);
        }

        public override void onMouseToolDisable()
        {
            base.onMouseToolDisable();
            MP2_Controller.wms?.close();
            if (ScriptableSingleton<CursorManager>.Instance.cursorType == CursorType.COLOR_DRAWING)
                ScriptableSingleton<CursorManager>.Instance.setCursorType(CursorType.DEFAULT);
        }

        public override bool canEscapeMouseTool()
        {
            return true;
        }

        public override GameController.GameFeature getDisallowedFeatures()
        {
            return GameController.GameFeature.Picking;
        }

        public void tick()
        {
            bool dragSelect = MP2._setting_drag_select;

            // Clicks on the open selector's buttons are handled by the selector, not painted through.
            bool leftClick = MP2_Controller.wms == null
                && (dragSelect ? Input.GetMouseButton(0) : Input.GetMouseButtonUp(0) && MP2.IsCoolDownReady());
            bool rightClick = Input.GetMouseButtonUp(1);

            if (leftClick == rightClick || UIUtility.isMouseOverUIElement())
                return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (leftClick)
            {
                MouseCollider.HitInfo hit = MouseCollisions.Instance.raycast(ray, float.MaxValue);
                if (hit.hitSomething)
                    MP2.controller.OnObjectClicked(hit.hitObject);
            }
            else
            {
                OpenSelector(ray);
            }
        }

        /// <summary>Lists every part under the mouse, nearest first, so buried or hidden parts can be picked.</summary>
        private static void OpenSelector(Ray ray)
        {
            MP2_Controller.wms?.close();

            List<GameObject> hits = MouseCollisions.Instance.raycastAll(ray, float.MaxValue)
                .OrderBy(hit => hit.hitDistance)
                .Select(hit => hit.hitObject)
                .Where(hitObject => hitObject != null)
                .Distinct()
                .ToList();

            if (hits.Count > 0)
                MP2_Controller.wms = new Window_MultiSelector(hits, Input.mousePosition);
        }
    }
}
