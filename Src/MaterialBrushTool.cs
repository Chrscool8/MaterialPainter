using System;
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
        private static Action<GameObject> onPicked;

        /// <summary>Eyedropper mode: the next left click picks the part's paint instead of painting it.</summary>
        public static bool Picking => onPicked != null;

        /// <summary>Starts eyedropper mode. Right click, or the brush tool closing, cancels it.</summary>
        public static void StartPicking(Action<GameObject> picked)
        {
            MP2_Controller.wms?.close();
            onPicked = picked;
            SetCursor(CursorType.COLOR_DRAWING, CursorType.COLOR_PICKING);
        }

        public static void StopPicking()
        {
            if (onPicked == null)
                return;

            onPicked = null;
            SetCursor(CursorType.COLOR_PICKING, CursorType.COLOR_DRAWING);
        }

        // Only swaps the cursor if it's still the one this tool set.
        private static void SetCursor(CursorType from, CursorType to)
        {
            CursorManager cursors = ScriptableSingleton<CursorManager>.Instance;
            if (cursors.cursorType == from)
                cursors.setCursorType(to);
        }

        public override void onMouseToolEnable()
        {
            base.onMouseToolEnable();
            ScriptableSingleton<CursorManager>.Instance.setCursorType(CursorType.COLOR_DRAWING);
        }

        public override void onMouseToolDisable()
        {
            base.onMouseToolDisable();
            MP2_Controller.wms?.close();
            StopPicking();
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
            if (Picking)
            {
                TickPicking();
                return;
            }

            bool dragSelect = MP2._setting_drag_select;

            // Clicks on the window and the open selector are the game UI's; they never paint through.
            bool leftClick = MP2_Controller.wms == null
                && (dragSelect ? Input.GetMouseButton(0) : Input.GetMouseButtonUp(0));
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

        private static void TickPicking()
        {
            if (Input.GetMouseButtonUp(1))
            {
                StopPicking();
                return;
            }

            if (!Input.GetMouseButtonUp(0) || UIUtility.isMouseOverUIElement())
                return;

            MouseCollider.HitInfo hit = MouseCollisions.Instance.raycast(Camera.main.ScreenPointToRay(Input.mousePosition), float.MaxValue);
            Action<GameObject> picked = onPicked;
            StopPicking();
            // Clicking nothing (the sky) just ends picking.
            if (hit.hitSomething)
                picked(hit.hitObject);
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
