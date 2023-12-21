using HarmonyLib;
using Parkitect.Mods.AssetPacks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MaterialPainter2
{
    public class ChangedMarker : MonoBehaviour
    {
        private Material[] materials = null;
        private MaterialPropertyBlock material_property_block = null;
        private bool _was_enabled = true;
        private int current_brush = -1;

        public Material[] get_materials()
        { return materials; }

        public void set_materials(Material[] materials)
        { this.materials = materials; }

        public MaterialPropertyBlock get_material_property_block()
        { return material_property_block; }

        public void set_material_property_block(MaterialPropertyBlock material_property_block)
        { this.material_property_block = material_property_block; }

        public bool was_enabled()
        { return _was_enabled; }

        public void set_enabled(bool _was_enabled)
        { this._was_enabled = _was_enabled; }

        public void set_current_brush(int brush)
        { current_brush = brush; }

        public int get_current_brush()
        { return current_brush; }
    }

    public class MP2Controller : MonoBehaviour
    {
        private IMouseTool brush_tool;
        public bool IncludeDescendants;
        public bool OnlyBuildables;
        public bool TargetSupports;

        public MP2Controller()
        {
            MP2.selected_brush = (int)MaterialBrush.None;
            brush_tool = new MP2BrushTool();
            IncludeDescendants = false;
            OnlyBuildables = true;
        }

        public void ActivatePipe()
        {
            MP2.MPDebug("enabled mouse tool");

            if (!GameController.Instance.isActiveMouseTool(brush_tool))
            {
                GameController.Instance.enableMouseTool(brush_tool);
            }
        }

        public void DeactivatePipe()
        {
            MP2.MPDebug("disabled mouse tool");

            if (GameController.Instance.isActiveMouseTool(brush_tool))
            {
                GameController.Instance.removeMouseTool(brush_tool);
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) && MP2.IsCoolDownReady())
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
                if (objectBelowMouse.hitObject != null)
                {
                    GameObject game_object = objectBelowMouse.hitObject.gameObject;
                    if (game_object != null)
                    {
                        MP2.MPDebug("1!" + game_object.ToString() + ": " + game_object.name);
                        //OnObjectClicked(game_object, true); // objectBelowMouse.hitObject as BuildableObject);
                    }
                }

                //////////////////////////////////////////
                MP2.MPDebug("2!");

                List<GameObject> things = new List<GameObject>();

                Plane[] cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
                BoundingVolume.Layers checkMask = BoundingVolume.Layers.Support;
                GameObject result = null;
                float distance = float.MaxValue;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                List<BoundingVolume> bvs = Traverse.Create(Collisions.Instance).Field("dynamicBoundingVolumes").GetValue() as List<BoundingVolume>;
                MP2.MPDebug($">> {bvs.Count}");

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
                }

                MP2.MPDebug($"{things.Count}");

                //////////////////////////////////////////
                MP2.MPDebug("3!");

                MouseCollider.HitInfo[] array2 = MouseCollisions.Instance.raycastAll(ray, float.MaxValue);

                var sortedList = array2.OrderBy(item => item.hitDistance).ToList();

                for (int i = 0; i < sortedList.Count; i++)
                {
                    MouseCollider.HitInfo raycastHit = sortedList[i];
                    MP2.MPDebug($"{raycastHit.hitObject.name} - {raycastHit.hitDistance}");
                    if (i == 0)
                        OnObjectClicked(raycastHit.hitObject);
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

            foreach (System.Reflection.MemberInfo m in componentType.GetMembers())
                Debug.Log(m.Name);
        }

        private void BackupMaterial(GameObject gameObject)
        {
            MP2.MPDebug("BACKUP!");

            ChunkedMesh chunkedMeshes = gameObject.GetComponent<ChunkedMesh>();
            if (chunkedMeshes != null)
                chunkedMeshes.enabled = false;

            Renderer renderer = gameObject.GetComponent<Renderer>();

            if (renderer != null)
            {
                ChangedMarker changed_marker = gameObject.AddComponent<ChangedMarker>();
                changed_marker.set_materials(renderer.materials);
                MaterialPropertyBlock old_block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(old_block);
                changed_marker.set_material_property_block(old_block);
                changed_marker.set_enabled(renderer.enabled);
            }
            else
                MP2.MPDebug("No Renderer For: " + gameObject.name);
        }

        private void RevertMaterial(GameObject gameObject)
        {
            MP2.MPDebug("REVERT!");

            Renderer renderer = gameObject.GetComponent<Renderer>();

            ChangedMarker changed_marker = gameObject.GetComponent<ChangedMarker>();
            renderer.materials = changed_marker.get_materials();
            renderer.SetPropertyBlock(changed_marker.get_material_property_block());
            renderer.enabled = (changed_marker.was_enabled());

            Destroy(changed_marker);

            ChunkedMesh chunkedMeshes = gameObject.GetComponent<ChunkedMesh>();
            if (chunkedMeshes != null)
                chunkedMeshes.enabled = true;
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

        public void OnObjectClicked(GameObject gameObject)
        {
            MP2.ResetCountdown();

            //MP2.MPDebug("Painting " + gameObject.name + " with Brush " + MP2.selected_brush);

            List<Transform> family = new List<Transform>();
            family.Add(gameObject.transform);

            if (IncludeDescendants)
                family.AddRange(GetDescendantsRecursive(gameObject.transform));

            if (TargetSupports)
                family = FindOnlySupports(family);

            HashSet<Transform> uniqueSet = new HashSet<Transform>(family);
            family = new List<Transform>(uniqueSet);
            family.Reverse();

            foreach (Transform child in family)
            {
                //MP2.MPDebug($"- Painting {child.name} ({child.gameObject.GetComponent<MonoBehaviour>().GetType().GetTypeInfo().ToString()}) with Brush " + MP2.selected_brush + "; " + child.GetInstanceID());
                //MP2.MPDebug("-- Buildable: " + isBuildable.ToString());

                //if (OnlyBuildables && !isBuildable)
                //    continue;

                if (MP2.selected_brush == (int)MaterialBrush.None)
                {
                    if (child.GetComponent<ChangedMarker>() != null)
                    {
                        RevertMaterial(child.gameObject);
                    }
                }
                else
                {
                    if (child.GetComponent<ChangedMarker>() == null)
                    {
                        BackupMaterial(child.gameObject);
                    }
                    ChangedMarker cm = child.GetComponent<ChangedMarker>();

                    if (cm == null)
                        continue;

                    cm.set_current_brush(MP2.selected_brush);

                    // Set Material

                    Renderer renderer = child.GetComponent<Renderer>();
                    if (renderer == null)
                        continue;

                    WaterBody waterBody = new WaterBody();

                    Color c = new Color(1, 1, 1, 1);
                    if (gameObject.GetComponent<CustomColors>() != null)
                        c = gameObject.GetComponent<CustomColors>().getColors()[0];

                    switch (MP2.selected_brush)
                    {
                        case (int)MaterialBrush.Water:
                            {
                                MP2.MPDebug("Water");
                                renderer.enabled = true;

                                waterBody.bodyType = WaterBody.BodyType.WATER;

                                Material[] shares = renderer.materials;

                                for (var i = 0; i < shares.Count(); i++)
                                {
                                    Material material_old = shares[i];
                                    if (material_old != null)
                                    {
                                        Material material_new = new Material(waterBody.getMaterial());
                                        //material_new.SetTexture("_MainTex", material_old.GetTexture("_MainTex"));
                                        material_new.enableInstancing = true;
                                        shares[i] = material_new;
                                    }
                                }

                                renderer.materials = shares;

                                c.a = 0.392156869f;

                                MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
                                renderer.GetPropertyBlock(materialPropertyBlock);
                                materialPropertyBlock.SetColor(Shader.PropertyToID("_Color"), c);
                                float H;
                                float S;
                                float V;
                                Color.RGBToHSV(c, out H, out S, out V);
                                c = Color.HSVToRGB(H, S, V / 2f);
                                c.a = 0.9019608f;
                                materialPropertyBlock.SetColor(Shader.PropertyToID("_Color2"), c);
                                materialPropertyBlock.SetColor(Shader.PropertyToID("_ShoreColor"), Color.HSVToRGB(H, S / 3f, V));
                                renderer.SetPropertyBlock(materialPropertyBlock);
                            }
                            break;

                        case (int)MaterialBrush.Lava:
                            {
                                MP2.MPDebug("Lava");
                                renderer.enabled = true;

                                if (renderer != null)
                                {
                                    waterBody.bodyType = WaterBody.BodyType.LAVA;

                                    Material[] shares = renderer.materials;

                                    for (var i = 0; i < shares.Count(); i++)
                                    {
                                        Material material_old = shares[i];
                                        if (material_old != null)
                                        {
                                            Material material_new = new Material(waterBody.getMaterial());
                                            material_new.enableInstancing = true;
                                            shares[i] = material_new;
                                        }
                                    }

                                    renderer.materials = shares;

                                    c.a = 0.392156869f;

                                    MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
                                    renderer.GetPropertyBlock(materialPropertyBlock);
                                    materialPropertyBlock.SetColor(Shader.PropertyToID("_Color"), c);
                                    float H;
                                    float S;
                                    float V;
                                    Color.RGBToHSV(c, out H, out S, out V);
                                    c = Color.HSVToRGB(H, S, V / 2f);
                                    c.a = 0.9019608f;
                                    materialPropertyBlock.SetColor(Shader.PropertyToID("_Color2"), c);
                                    materialPropertyBlock.SetColor(Shader.PropertyToID("_ShoreColor"), Color.HSVToRGB(H, S / 3f, V));
                                    renderer.SetPropertyBlock(materialPropertyBlock);
                                }
                            }
                            break;

                        case (int)MaterialBrush.Glass:
                            {
                                MaterialDecorator materialDecorator = new MaterialDecorator();
                                Material snag = new Material(ScriptableSingleton<AssetManager>.Instance.seeThroughMaterial);

                                if (snag == null)
                                {
                                    MP2.MPDebug("No CustomColorsTransparent in Material list?");
                                    return;
                                }

                                Material[] shares = renderer.materials;

                                for (var i = 0; i < shares.Count(); i++)
                                {
                                    Material material_old = shares[i];
                                    if (material_old != null)
                                    {
                                        Material material_new = snag;
                                        material_new.enableInstancing = true;
                                        shares[i] = material_new;
                                    }
                                }

                                renderer.materials = shares;
                            }
                            break;

                        case (int)MaterialBrush.Terrain:
                            {
                                MaterialDecorator materialDecorator = new MaterialDecorator();
                                Material snag = new Material(ScriptableSingleton<AssetManager>.Instance.terrainMaterial);
                                //Material snag = ScriptableSingleton<AssetManager>.Instance.decoVisibilityHighlightOverlayMaterial;

                                if (snag == null)
                                {
                                    MP2.MPDebug("No terrainMaterial in Material list?");
                                    return;
                                }

                                Material[] shares = renderer.materials;

                                for (var i = 0; i < shares.Count(); i++)
                                {
                                    Material material_old = shares[i];
                                    if (material_old != null)
                                    {
                                        Material material_new = snag;
                                        material_new.enableInstancing = true;
                                        shares[i] = material_new;
                                    }
                                }

                                renderer.materials = shares;
                            }
                            break;

                        case (int)MaterialBrush.Invisible:
                            {
                                MP2.MPDebug("Invisible");
                                renderer.enabled = false;
                            }
                            break;
                    }

                    ShockwaveController.Instance.addShockwave(child.position, .5f, true);
                    UnityEngine.Object.Instantiate<GameObject>(ScriptableSingleton<AssetManager>.Instance.employeeLevelUpParticlesGO).transform.position = child.position;
                    UnityEngine.Object.Instantiate<GameObject>(ScriptableSingleton<AssetManager>.Instance.plopParticlesGO).transform.position = child.position;
                }
            }
            return;
        }
    }
}