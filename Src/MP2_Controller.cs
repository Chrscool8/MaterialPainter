﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Parkitect.Mods.AssetPacks;

using UnityEngine;
using UnityEngine.Video;

using VLB;


namespace MaterialPainter2
{
    public class ChangedMarker : MonoBehaviour
    {
        private Material[] materials = null;
        private MaterialPropertyBlock material_property_block = null;
        private VideoPlayer video_player = null;
        private bool _was_enabled = true;
        private int current_brush = -1;
        private string custom_string = "";

        public Material[] GetMaterials()
        { return materials; }

        public void SetMaterials(Material[] materials)
        { this.materials = materials; }

        public MaterialPropertyBlock GetMaterialPropertyBlock()
        { return material_property_block; }

        public void SetMaterialPropertyBlock(MaterialPropertyBlock material_property_block)
        { this.material_property_block = material_property_block; }

        public bool WasEnabled()
        { return _was_enabled; }

        public void SetEnabled(bool _was_enabled)
        { this._was_enabled = _was_enabled; }

        public void SetCurrentBrush(int brush, string custom_data = "")
        {
            current_brush = brush;
            custom_string = custom_data;
        }

        public int GetCurrentBrush()
        { return current_brush; }

        public string GetCurrentBrushString()
        { return custom_string; }

        public void SetVideoPlayer(VideoPlayer video_player)
        { this.video_player = video_player; }

        public VideoPlayer GetVideoPlayer()
        { return video_player; }
    }

    public class MP2_Controller : MonoBehaviour
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

        private void BackupMaterial(GameObject game_object)
        {
            //MP2.MPDebug("BACKUP!");

            ChunkedMesh chunked_meshes = game_object.GetComponent<ChunkedMesh>();
            if (chunked_meshes != null)
                chunked_meshes.enabled = false;

            Renderer renderer = game_object.GetComponent<Renderer>();
            VideoPlayer video_player = game_object.GetComponent<VideoPlayer>();

            if (renderer != null || video_player != null)
            {
                ChangedMarker changed_marker = game_object.AddComponent<ChangedMarker>();

                if (renderer != null)
                {
                    changed_marker.SetMaterials(renderer.materials);
                    MaterialPropertyBlock old_block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(old_block);
                    changed_marker.SetMaterialPropertyBlock(old_block);
                    changed_marker.SetEnabled(renderer.enabled);
                }
                else
                    MP2.MPDebug("No Renderer For: " + game_object.name);

                if (video_player != null)
                {
                    changed_marker.SetVideoPlayer(video_player);
                }
            }
        }

        private void RevertMaterial(GameObject game_object)
        {
            //MP2.MPDebug("REVERT!");

            ChangedMarker changed_marker = game_object.GetComponent<ChangedMarker>();
            if (changed_marker == null)
            {
                MP2.MPDebug($"Can't revert {game_object}.");
                return;
            }

            Renderer renderer = game_object.GetComponent<Renderer>();

            if (renderer)
            {
                renderer.materials = changed_marker.GetMaterials();
                renderer.SetPropertyBlock(changed_marker.GetMaterialPropertyBlock());
                renderer.enabled = (changed_marker.WasEnabled());
            }

            VideoPlayer video_player = changed_marker.GetVideoPlayer();
            if (video_player != null)
            {
                //game_object.AddComponent<VideoPlayer>(video_player);
                DestroyImmediate(video_player);
            }

            DestroyImmediate(changed_marker);

            ChunkedMesh chunkedMeshes = game_object.GetComponent<ChunkedMesh>();
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

        public void SetMaterial(Transform tf, int brush_type = -1, string brush_type_custom = "")
        {
            int selected_brush = MP2.selected_brush;
            string selected_brush_custom = MP2.selected_brush_custom;

            if (brush_type != -1)
                selected_brush = brush_type;

            if (brush_type_custom != "")
                selected_brush_custom = brush_type_custom;

            MP2.MPDebug($"Painting {tf.gameObject.name} with Brush {selected_brush} ({brush_type}), {brush_type_custom}");

            ChangedMarker cm1 = tf.gameObject.GetComponent<ChangedMarker>();
            if (cm1 != null)
            {
                if (cm1.GetCurrentBrush() == selected_brush && cm1.GetCurrentBrushString() == selected_brush_custom)
                {
                    MP2.MPDebug("Same Brush");
                    return;
                }
            }

            if (tf.gameObject.GetComponent<ChangedMarker>() != null)
            {
                RevertMaterial(tf.gameObject);
            }

            if (selected_brush != (int)MaterialBrush.None)
            {
                if (tf.gameObject.GetComponent<ChangedMarker>() == null)
                {
                    BackupMaterial(tf.gameObject);
                }

                ChangedMarker cm = tf.gameObject.GetComponent<ChangedMarker>();

                if (cm == null)
                {
                    MP2.MPDebug("CM NULL??");
                    return;
                }

                cm.SetCurrentBrush(selected_brush);
                if (selected_brush == (int)MaterialBrush.Video)
                {
                    cm.SetCurrentBrush(selected_brush, selected_brush_custom);
                }

                // Set Material

                Renderer renderer = tf.GetComponent<Renderer>();
                if (renderer == null)
                {
                    MP2.MPDebug("RENDERER NULL??");
                    return;
                }

                switch (selected_brush)
                {
                    case (int)MaterialBrush.Water:
                        {
                            MP2.MPDebug("Water");
                            renderer.enabled = true;

                            WaterBody waterBody = new WaterBody();

                            Color c = WaterBody.defaultWaterColor;
                            if (tf.gameObject.GetComponent<CustomColors>() != null)
                                c = tf.gameObject.GetComponent<CustomColors>().getColors()[0];

                            waterBody.color = c;

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
                            Color.RGBToHSV(c, out float H, out float S, out float V);
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
                                WaterBody waterBody = new WaterBody();

                                Color c = new Color(1, 1, 1, 1);
                                if (tf.gameObject.GetComponent<CustomColors>() != null)
                                    c = tf.gameObject.GetComponent<CustomColors>().getColors()[0];

                                waterBody.color = c;
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

                            Color c = new Color(1, 1, 1, 1);
                            if (tf.gameObject.GetComponent<CustomColors>() != null)
                                c = tf.gameObject.GetComponent<CustomColors>().getColors()[0];
                            c.a = .5f;

                            MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
                            renderer.GetPropertyBlock(materialPropertyBlock);
                            materialPropertyBlock.SetColor(Shader.PropertyToID("_Color"), c);
                            renderer.SetPropertyBlock(materialPropertyBlock);
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

                            MaterialDecorator materialDecorator = new MaterialDecorator();
                            Material snag = new Material(ScriptableSingleton<AssetManager>.Instance.multiplayerBuildPreviewGhostMaterial);

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

                            renderer.enabled = false;
                        }
                        break;

                    case (int)MaterialBrush.InvisiblePreview:
                        {
                            MP2.MPDebug("Invisible Preview");
                            renderer.enabled = true;

                            MaterialDecorator materialDecorator = new MaterialDecorator();
                            Material snag = new Material(ScriptableSingleton<AssetManager>.Instance.multiplayerBuildPreviewGhostMaterial);

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

                    case (int)MaterialBrush.Video:
                        {
                            MP2.MPDebug($"Video, {selected_brush_custom}");
                            renderer.enabled = true;

                            var video_url = MP2._local_mods_directory + $"MaterialPainter2/Custom/Videos/{selected_brush_custom}.mp4";
                            if (!File.Exists(video_url))
                            {
                                MP2.MPDebug($"Couldn't find {video_url}.");

                                string fallback = MP2._local_mods_directory + $"MaterialPainter2/Custom/Videos/video-0.mp4"; ;
                                if (File.Exists(fallback))
                                {
                                    video_url = fallback;
                                }
                                else
                                {
                                    video_url = null;
                                }
                            }

                            MP2.MPDebug("Loading video: " + video_url);

                            if (video_url != null)
                            {
                                /*if (MP2.cached_videos.ContainsKey(url))
                                {
                                    video_player = MP2.cached_videos[url];
                                    tf.gameObject.AddComponent<VideoPlayer>(video_player);
                                }
                                else
                                {
                                    video_player = tf.gameObject.AddComponent<VideoPlayer>();
                                    MP2.cached_videos.Add(url, video_player);
                                }*/


                                VideoPlayer video_player = tf.gameObject.AddComponent<VideoPlayer>();
                                AudioSource audio_source = tf.gameObject.AddComponent<AudioSource>();

                                //video_player.audioOutputMode = VideoAudioOutputMode.None;

                                video_player.audioOutputMode = VideoAudioOutputMode.AudioSource;
                                video_player.SetTargetAudioSource(0, audio_source);

                                //audio_source.spatialize = true;
                                //audio_source.spatialBlend = 1.0f;

                                audio_source.volume = 0;

                                video_player.url = video_url;
                                video_player.isLooping = true;
                                video_player.Prepare();
                                video_player.prepareCompleted += OnVideoPrepared;

                                video_player.targetMaterialRenderer = renderer;
                                video_player.renderMode = VideoRenderMode.MaterialOverride;
                            }
                            else
                            {
                                MP2.MPDebug($"Couldn't load {video_url}.");
                            }
                        }
                        break;
                }

                ShockwaveController.Instance.addShockwave(tf.position, .5f, true);
                Instantiate(ScriptableSingleton<AssetManager>.Instance.employeeLevelUpParticlesGO).transform.position = tf.position;
                Instantiate(ScriptableSingleton<AssetManager>.Instance.plopParticlesGO).transform.position = tf.position;
                ScriptableSingleton<SoundAssetManager>.Instance.recolorObject.play2D();
            }
        }
        void OnVideoPrepared(VideoPlayer vp)
        {
            // Play the video when it is prepared
            vp.Play();
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