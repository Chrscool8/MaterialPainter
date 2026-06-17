using HarmonyLib;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

namespace MaterialPainter2
{
    [HarmonyPatch]
    public class SaveInjectorPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(GameController), "storeSavegameData", parameters: new Type[]
        {
            typeof(List<Dictionary<string, object>>), typeof(string), typeof(bool), typeof(bool)
        });

        [HarmonyPrefix]
        public static bool storeSavegameData(List<Dictionary<string, object>> data, string filePath, bool compress, bool isTemporaryFile)
        {
            MP2.MPDebug("Injection Point");
            MP2.MPDebug($"{filePath}");

            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            List<GameObject> objectsWithChangedMarker = new List<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.GetComponent<ChangedMarker>() != null)
                    objectsWithChangedMarker.Add(obj);
            }

            Dictionary<string, string> myDictionary = new Dictionary<string, string>();
            myDictionary[MP2.SAVE_SCHEMA_KEY] = MP2.CURRENT_SAVE_SCHEMA_VERSION.ToString();

            foreach (GameObject obj in objectsWithChangedMarker)
            {
                MP2.MPDebug($"{obj.GetInstanceID()}");
                string key = obj.name + ":" + obj.transform.position.ToString();
                int value = obj.GetComponent<ChangedMarker>().GetCurrentBrush();
                if (value == (int)MaterialBrush.InvisiblePreview)
                {
                    value = (int)MaterialBrush.Invisible;
                }

                string detail = obj.GetComponent<ChangedMarker>().GetCurrentBrushString();
                if (detail == "")
                    myDictionary[key] = value.ToString();
                else
                    myDictionary[key] = value.ToString() + ":" + detail;


            }

            StringStringDictionary serializableDictionary = new StringStringDictionary();
            foreach (var kvp in myDictionary)
            {
                serializableDictionary.pairs.Add(new StringStringPair { key = kvp.Key, value = kvp.Value });
            }

            string json = JsonConvert.SerializeObject(serializableDictionary);

            Dictionary<string, object> dictionary = new Dictionary<string, object>
            {
                { MP2.SAVE_DATA_KEY, json }
            };

            data.Add(dictionary);

            return true;
        }
    }
}
