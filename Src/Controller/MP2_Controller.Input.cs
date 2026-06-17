using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;

using VLB;

namespace MaterialPainter2
{
    public partial class MP2_Controller
    {
        /*private void FixedUpdate()
        {
            MP2.MPDebug("Fixed Update");
        }*/

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Don't worry 'bout it.")]
        private void Update()
        {
            if (MP2.pipe_active)
            {
                //brush_tool.tick();
            }

            //MP2.MPDebug("Update");

            bool left_click = ((!MP2._setting_drag_select && Input.GetMouseButtonUp(0) && MP2.IsCoolDownReady()) || (MP2._setting_drag_select && Input.GetMouseButton(0)) && MP2_Controller.wms == null);
            bool right_click = (!MP2._setting_drag_select && Input.GetMouseButtonUp(1));

            if (left_click ^ right_click)
            {
                if (!GameController.Instance.isActiveMouseTool(brush_tool))
                {
                    return;
                }

                if (UIUtility.isMouseOverUIElement())
                {
                    return;
                }

                Utility.ObjectBelowMouseInfo objectBelowMouse = Utility.getObjectBelowMouse<SerializedMonoBehaviour>();
                /*if (objectBelowMouse.hitObject != null)
                {
                    GameObject game_object = objectBelowMouse.hitObject.gameObject;
                    if (game_object != null)
                    {
                        MP2.MPDebug("1!" + game_object.ToString() + ": " + game_object.name);
                        //OnObjectClicked(game_object, true); // objectBelowMouse.hitObject as BuildableObject);
                    }
                }*/

                //////////////////////////////////////////
                //MP2.MPDebug("2!");

                List<GameObject> things = new List<GameObject>();

                Plane[] cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
                //BoundingVolume.Layers checkMask = BoundingVolume.Layers.Support;
                //GameObject result = null;
                //float distance = float.MaxValue;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                /*
                 List<BoundingVolume> bvs = Traverse.Create(Collisions.Instance).Field("dynamicBoundingVolumes").GetValue() as List<BoundingVolume>;
                 //MP2.MPDebug($">> {bvs.Count}");

                 List<BoundingVolume>.Enumerator enumerator = bvs.GetEnumerator();
                 while (enumerator.MoveNext())
                 {
                     BoundingVolume current = enumerator.Current;

                     if (current.hasMask(checkMask))
                     {
                         if (!current.isStatic)
                         {
                             current.setPosition(current.gameObject.transform.position, current.gameObject.transform.rotation, current.gameObject.transform.lossyScale);
                         }

                         if (current.isVisible(cameraFrustumPlanes) && current.collides(ray, out var distance2) && distance2 < distance)
                         {
                             distance = distance2;
                             result = current.gameObject;
                             things.Add(enumerator.Current.gameObject);
                             MP2.MPDebug($"{current.gameObject.name}");
                         }
                     }
                 }*/

                //MP2.MPDebug($"{things.Count}");

                //////////////////////////////////////////
                //MP2.MPDebug("3!");

                MouseCollider.HitInfo[] array2 = MouseCollisions.Instance.raycastAll(ray, float.MaxValue);

                var sortedList = array2.OrderBy(item => item.hitDistance).ToList();

                for (int i = 0; i < sortedList.Count; i++)
                {
                    MouseCollider.HitInfo raycastHit = sortedList[i];
                    MP2.MPDebug($"{i} {raycastHit.hitObject.name} - {raycastHit.hitDistance}");
                }

                List<GameObject> list = new List<GameObject>();

                for (int i = 0; i < sortedList.Count; i++)
                {
                    MouseCollider.HitInfo raycastHit = sortedList[i];
                    //MP2.MPDebug($"{raycastHit.hitObject.name} - {raycastHit.hitDistance}");
                    if (i == 0 && left_click)
                    {
                        OnObjectClicked(raycastHit.hitObject);
                    }
                    else if (right_click)
                    {
                        list.Add(raycastHit.hitObject);
                    }
                }

                if (right_click)
                {
                    wms?.close();

                    if (list.Count > 0)
                        wms = new Window_MultiSelector(list, Input.mousePosition.xy());
                }
            }
        }

        public void PrintVariables(Component component)
        {
            // Get the type of the current component
            Type componentType = component.GetType();
            MP2.MPDebug(componentType.Name);

            // Get all fields (variables) of the component type
            FieldInfo[] fields = componentType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            MP2.MPDebug(fields.Length.ToString());

            // Print the values of each field
            foreach (FieldInfo field in fields)
            {
                object value = field.GetValue(this);
                Debug.Log($"Variable: {field.Name}, Value: {value}");
            }

            foreach (MemberInfo m in componentType.GetMembers())
                Debug.Log(m.Name);
        }

        public static List<Transform> GetDescendantsRecursive(Transform parent)
        {
            List<Transform> descendants = new List<Transform>();

            foreach (Transform child in parent)
            {
                descendants.Add(child);
                descendants.AddRange(GetDescendantsRecursive(child));
            }

            return descendants;
        }

        private List<Transform> FindOnlySupports(List<Transform> incoming)
        {
            List<Transform> outgoing = new List<Transform>();

            while (incoming.Count > 0)
            {
                Transform current = incoming[0];
                if (current.gameObject.name.ToLower().Contains("support"))
                {
                    MP2.MPDebug($"Keep {current.gameObject.name}");
                    outgoing.Add(current);
                }
                else
                    MP2.MPDebug($"Toss {current.gameObject.name}");

                incoming.RemoveAt(0);
            }
            return outgoing;
        }

        public void OnObjectClicked(GameObject game_object)
        {
            MP2.ResetCountdown();

            if (game_object != null)
            {
                if (!MP2._setting_target_supports || (MP2._setting_target_supports && game_object.name.ToLower().Contains("support")))
                {
                    SetMaterial(game_object.transform);
                }
            }
        }
    }
}
