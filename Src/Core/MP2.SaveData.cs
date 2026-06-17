using MiniJSON;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

using UnityEngine;

namespace MaterialPainter2
{
    public partial class MP2
    {
        private List<string> LoadGZippedTextFile(string filePath)
        {
            List<string> lines = new List<string>();

            using (FileStream fs = File.OpenRead(filePath))
            using (GZipStream gzStream = new GZipStream(fs, CompressionMode.Decompress))
            using (StreamReader reader = new StreamReader(gzStream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            return lines;
        }

        [Serializable]
        public class UserData
        {
            public string name;
            public string value;
        }

        public void ReassignMaterialsAfterLoadingSave()
        {

            MP2.MPDebug("Assigning Materials From Save.");

            try
            {
                List<GameObject> allObjects = Utils.GetAllObjectsInScene();
                MPDebug($"Number of GOs: {allObjects.Count}");
                MPDebug($"Numbers of Serials: {GameController.Instance.getSerializedObjects().Count}");

                if (current_file_path == null || current_file_path == "")
                {
                    MPDebug("Bad current_file_path");
                    return;
                }

                bool set_any_new = false;

                {
                    string file_path = current_file_path;
                    MPDebug(file_path);

                    bool compressed = !System.IO.Path.GetExtension(current_file_path).Equals(".txt");

                    if (!compressed)
                    {
                        MPDebug("Uncompressed is not yet supported.");
                        return;
                    }

                    List<string> file_lines = LoadGZippedTextFile(file_path);
                    foreach (string line in file_lines)
                    {
                        Dictionary<string, object> dictionary = (Dictionary<string, object>)Json.Deserialize(line);

                        if (dictionary != null && dictionary.ContainsKey(SAVE_DATA_KEY))
                        {
                            set_any_new = true;

                            string inner_json = (string)dictionary[SAVE_DATA_KEY];

                            if (inner_json.Contains(SAVE_SCHEMA_KEY))
                            {
                                Dictionary<string, string> myDictionary = new Dictionary<string, string>();
                                StringStringDictionary serializedDictionary = JsonConvert.DeserializeObject<StringStringDictionary>(inner_json);

                                foreach (var pair in serializedDictionary.pairs)
                                {
                                    myDictionary[pair.key] = pair.value;
                                }

                                MPDebug($"File Schema V{myDictionary[SAVE_SCHEMA_KEY]}");

                                if (int.TryParse(myDictionary[SAVE_SCHEMA_KEY], out int schema) && schema == CURRENT_SAVE_SCHEMA_VERSION)
                                {
                                    foreach (var obj in allObjects)
                                    {
                                        string key = obj.name + ":" + obj.transform.position.ToString();
                                        if (myDictionary.ContainsKey(key))
                                        {
                                            string brush_data = myDictionary[key];
                                            MPDebug($"{key}: {brush_data}");


                                            if (brush_data.Contains(":")) // has subbrush
                                            {
                                                string[] parts = brush_data.Split(':');
                                                if (int.TryParse(parts[0], out int brush_type) && parts[1] != "")
                                                {
                                                    controller.SetMaterial(obj.transform, brush_type, parts[1]);
                                                }
                                                else
                                                {
                                                    MPDebug($"Brush detail empty.");
                                                }
                                            }
                                            else //basic brush
                                            {
                                                if (int.TryParse(brush_data, out int value))
                                                {
                                                    controller.SetMaterial(obj.transform, value);
                                                }
                                                else
                                                {
                                                    MPDebug($"Invalid Brush: {value}");
                                                }
                                            }

                                            myDictionary.Remove(key);
                                        }
                                    }

                                    myDictionary.Remove(SAVE_SCHEMA_KEY);

                                    if (myDictionary.Count == 0)
                                        MP2.MPDebug("Every key in the dictionary was assigned!");
                                    else
                                    {
                                        MP2.MPDebug("The following items were not reassigned materials:");
                                        foreach (var (index, key, value) in myDictionary.Enumerate())
                                        {
                                            MP2.MPDebug($"{index} {key} {value}");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                MPDebug("File Schema V2");

                                Dictionary<string, int> myDictionary = new Dictionary<string, int>();
                                StringIntDictionary serializedDictionary = JsonConvert.DeserializeObject<StringIntDictionary>(inner_json);

                                foreach (var pair in serializedDictionary.pairs)
                                {
                                    myDictionary[pair.key] = pair.value;
                                }

                                foreach (var obj in allObjects)
                                {
                                    string key = obj.name + ":" + obj.transform.position.ToString();
                                    if (myDictionary.ContainsKey(key))
                                    {
                                        int previous_brush = myDictionary[key];
                                        controller.SetMaterial(obj.transform, previous_brush);
                                    }
                                }
                            }

                            selected_brush = 0;
                            selected_brush_custom = "";
                        }
                    }
                }

                if (!set_any_new && File.Exists(current_file_path + ".mat"))
                {
                    MPDebug("File Schema V1");

                    string file_path = current_file_path + ".mat";

                    MPDebug(file_path);
                    Dictionary<string, int> myDictionary = new Dictionary<string, int>();
                    if (file_path != "" && File.Exists(file_path))
                    {
                        string json = File.ReadAllText(file_path);
                        StringIntDictionary serializedDictionary = JsonConvert.DeserializeObject<StringIntDictionary>(json);
                        foreach (var pair in serializedDictionary.pairs)
                        {
                            myDictionary[pair.key] = pair.value;
                        }
                    }
                    foreach (var obj in allObjects)
                    {
                        string key = obj.name + ":" + obj.transform.position.ToString();
                        if (myDictionary.ContainsKey(key))
                        {
                            int previous_brush = myDictionary[key];
                            controller.SetMaterial(obj.transform, (int)MaterialBrush.None);
                            controller.SetMaterial(obj.transform, previous_brush);
                        }
                    }
                    selected_brush = 0;
                    selected_brush_custom = "";
                }
            }
            catch (Exception ex)
            {
                MP2.MPDebug("Loading Messed up!");
                MP2.MPDebug("An error occurred: " + ex.Message);
                MP2.MPDebug("Stack Trace: " + ex.StackTrace);
            }
        }

        public void ConvertLegacySpellsToMP()
        {
            MPDebug("Converting Legacy Spells to MP");

            List<GameObject> allObjects = Utils.GetAllObjectsInScene();
            MPDebug($"Number of GOs: {allObjects.Count}");

            MPDebug($"Numbers of Serials: {GameController.Instance.getSerializedObjects().Count}");

            foreach (GameObject _obj in allObjects)
            {
                GameObject obj = _obj;

                if (obj != null)
                {
                    CustomColors cc = obj.GetComponent<CustomColors>();

                    if (cc != null)
                    {
                        if (cc.getColors().Length > 0)
                        {
                            int brush = -1;
                            Color c = cc.getColors()[0];
                            if (Utils.ColorCloseToEqual(c, new Color(11.151f, 17.15f, 12.18f), .2f))
                            {
                                MPDebug("INVISIBLE SPELL LOCATED");
                                brush = (int)MaterialBrush.Invisible;
                            }
                            else if (Utils.ColorCloseToEqual(c, new Color(5.15f, 15.15f, 18.2f), .2f))
                            {
                                MPDebug("WATER SPELL LOCATED");
                                brush = (int)MaterialBrush.Water;
                            }
                            else if (Utils.ColorCloseToEqual(c, new Color(11.15f, 2.57f, 1.2f), .2f))
                            {
                                MPDebug("LAVA SPELL LOCATED");
                                brush = (int)MaterialBrush.Lava;
                            }
                            else if (Utils.ColorCloseToEqual(c, new Color(55.27f, 55.28f, 55.29f), .2f))
                            {
                                MPDebug("GLASS SPELL LOCATED");
                                brush = (int)MaterialBrush.Glass;
                            }
                            else if (c[0] > 1.0 || c[1] > 1.0 || c[2] > 1.0)
                            {
                                MPDebug($">1 - {obj.name} {c[0]} {c[1]} {c[2]} ");
                            }

                            if (brush != -1)
                            {
                                cc.setColor(new Color(1.0f, 1.0f, 1.0f, c.a), 0);

                                foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>())
                                {
                                    controller.SetMaterial(renderer.gameObject.transform, brush);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    [Serializable]
    public struct StringIntPair
    {
        public string key;
        public int value;
    }

    [Serializable]
    public struct StringStringPair
    {
        public string key;
        public String value;
    }

    [Serializable]
    public class StringIntDictionary
    {
        public List<StringIntPair> pairs = new List<StringIntPair>();
    }

    [Serializable]
    public class StringStringDictionary
    {
        public List<StringStringPair> pairs = new List<StringStringPair>();
    }
}
