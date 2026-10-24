using MiniJSON;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

using UnityEngine;

namespace MaterialPainter2
{
    /// <summary>
    /// Brings paints from older saves forward. Imported paints become <see cref="PaintData"/>, so the next save
    /// stores them the current way. Older formats keyed paints by "objectName:position":
    /// <list type="bullet">
    /// <item>V1: a "&lt;save&gt;.mat" file next to the save, values are brush ids.</item>
    /// <item>V2: a "MaterialPainter2" entry inside the save, values are brush ids.</item>
    /// <item>V3: the same entry with a "_schema" key, values are "brush[:detail]".</item>
    /// </list>
    /// </summary>
    public static class LegacySaveImporter
    {
        // Legacy paints found for the loaded park that haven't been converted yet.
        private static Dictionary<string, string> pending;

        /// <summary>True while the loaded park has legacy paints waiting to be converted.</summary>
        public static bool HasPending => pending != null;

        /// <summary>Forgets the previous park's legacy paints; call when a park starts loading.</summary>
        public static void Clear()
        {
            pending = null;
        }

        /// <summary>Reads the loaded park's legacy paints, if it has any and nothing has been converted yet.</summary>
        public static void Scan(string savePath)
        {
            pending = null;
            if (string.IsNullOrEmpty(savePath) || !File.Exists(savePath))
                return;

            // Once a park has current paint data it was saved by this version; any legacy data is stale.
            if (GameController.Instance.getSerializedObjects().Any(smb => smb.tryGetCustomData(out PaintData _)))
                return;

            try
            {
                Dictionary<string, string> paints = ReadSaveEntry(savePath) ?? ReadSideFile(savePath + ".mat");
                if (paints != null && paints.Count > 0)
                    pending = paints;
            }
            catch (Exception ex)
            {
                MP2.MPDebug("Reading legacy paints failed: " + ex, always_show: true);
            }
        }

        /// <summary>
        /// Applies the pending legacy paints. If none match (the park may not have finished building), they stay
        /// pending so the conversion can be retried.
        /// </summary>
        public static void Import()
        {
            if (pending == null)
                return;

            try
            {
                Dictionary<string, string> paints = new Dictionary<string, string>(pending);
                int applied = 0;
                foreach (GameObject gameObject in Utils.GetAllObjectsInScene())
                {
                    string key = gameObject.name + ":" + gameObject.transform.position.ToString();
                    if (paints.TryGetValue(key, out string encoded) && PaintStore.TryDecode(encoded, out MaterialBrush id, out string detail))
                    {
                        if (Painter.Paint(gameObject, id, detail, userAction: false))
                            applied++;
                        paints.Remove(key);
                    }
                }

                MP2.MPDebug($"Imported {applied} legacy paints, {paints.Count} unmatched.", always_show: true);
                if (applied > 0)
                    pending = null;
            }
            catch (Exception ex)
            {
                MP2.MPDebug("Legacy import failed: " + ex, always_show: true);
            }
        }

        /// <summary>The original Material Painter stored "spells" as out-of-range custom colors.</summary>
        public static void ImportSpells()
        {
            foreach (GameObject gameObject in Utils.GetAllObjectsInScene())
            {
                CustomColors customColors = gameObject.GetComponent<CustomColors>();
                Color[] colors = customColors != null ? customColors.getColors() : null;
                if (colors == null || colors.Length == 0)
                    continue;

                Color color = colors[0];
                MaterialBrush id;
                if (Utils.ColorCloseToEqual(color, new Color(11.151f, 17.15f, 12.18f), .2f))
                    id = MaterialBrush.Invisible;
                else if (Utils.ColorCloseToEqual(color, new Color(5.15f, 15.15f, 18.2f), .2f))
                    id = MaterialBrush.Water;
                else if (Utils.ColorCloseToEqual(color, new Color(11.15f, 2.57f, 1.2f), .2f))
                    id = MaterialBrush.Lava;
                else if (Utils.ColorCloseToEqual(color, new Color(55.27f, 55.28f, 55.29f), .2f))
                    id = MaterialBrush.Glass;
                else
                    continue;

                MP2.MPDebug($"Converting {id} spell on {gameObject.name}");
                customColors.setColor(new Color(1f, 1f, 1f, color.a), 0);
                foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>())
                    Painter.Paint(renderer.gameObject, id, "", userAction: false);
            }
        }

        private static Dictionary<string, string> ReadSaveEntry(string savePath)
        {
            foreach (string line in ReadSaveLines(savePath))
            {
                if (!line.Contains(MP2.SAVE_DATA_KEY))
                    continue;

                if (!(Json.Deserialize(line) is Dictionary<string, object> entry) || !(entry.TryGetValue(MP2.SAVE_DATA_KEY, out object value) && value is string json))
                    continue;

                if (json.Contains(MP2.SAVE_SCHEMA_KEY))
                {
                    Dictionary<string, string> paints = JsonConvert.DeserializeObject<StringStringDictionary>(json).pairs
                        .GroupBy(pair => pair.key).ToDictionary(group => group.Key, group => group.Last().value);
                    paints.Remove(MP2.SAVE_SCHEMA_KEY);
                    return paints;
                }

                return ToStringValues(JsonConvert.DeserializeObject<StringIntDictionary>(json));
            }

            return null;
        }

        private static Dictionary<string, string> ReadSideFile(string path)
        {
            return File.Exists(path) ? ToStringValues(JsonConvert.DeserializeObject<StringIntDictionary>(File.ReadAllText(path))) : null;
        }

        private static Dictionary<string, string> ToStringValues(StringIntDictionary dictionary)
        {
            return dictionary.pairs.GroupBy(pair => pair.key).ToDictionary(group => group.Key, group => group.Last().value.ToString());
        }

        private static IEnumerable<string> ReadSaveLines(string savePath)
        {
            using (FileStream file = File.OpenRead(savePath))
            using (Stream stream = System.IO.Path.GetExtension(savePath).Equals(".txt", StringComparison.OrdinalIgnoreCase) ? (Stream)file : new GZipStream(file, CompressionMode.Decompress))
            using (StreamReader reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    yield return line;
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
        public string value;
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
