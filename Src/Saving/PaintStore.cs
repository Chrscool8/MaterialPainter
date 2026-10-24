using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace MaterialPainter2
{
    /// <summary>Keeps each object's <see cref="PaintData"/> in step with what's painted, and re-applies it after loading.</summary>
    public static class PaintStore
    {
        private const char PATH_SEPARATOR = '/';
        private const char DUPLICATE_MARK = '#';
        private const char DETAIL_SEPARATOR = ':';

        public static void Record(Renderer renderer, MaterialBrush id, string detail)
        {
            SerializedMonoBehaviour owner = renderer.GetComponentInParent<SerializedMonoBehaviour>();
            if (owner == null)
            {
                MP2.MPDebug($"{renderer.name} has no owning object; its paint won't be saved.");
                return;
            }

            if (!owner.tryGetCustomData(out PaintData data))
            {
                data = new PaintData();
                owner.addCustomData(data);
            }

            data.paints[GetPath(owner.transform, renderer.transform)] = Encode(id, detail);
        }

        public static void Forget(Renderer renderer)
        {
            SerializedMonoBehaviour owner = renderer.GetComponentInParent<SerializedMonoBehaviour>();
            if (owner == null || !owner.tryGetCustomData(out PaintData data))
                return;

            data.paints.Remove(GetPath(owner.transform, renderer.transform));
            if (data.paints.Count == 0)
                owner.removeCustomData<PaintData>();
        }

        /// <summary>
        /// Applies an object's saved paints. Paints whose renderer doesn't exist (yet) are kept, so a later
        /// restore or the next save still has them.
        /// </summary>
        public static void Restore(SerializedMonoBehaviour owner)
        {
            if (owner == null || !owner.tryGetCustomData(out PaintData data))
                return;

            foreach (KeyValuePair<string, string> paint in data.paints.ToList())
            {
                Transform target = Resolve(owner.transform, paint.Key);
                if (target != null && TryDecode(paint.Value, out MaterialBrush id, out string detail))
                    Painter.Paint(target.gameObject, id, detail, userAction: false);
            }
        }

        public static void RestoreAll()
        {
            if (GameController.Instance == null)
                return;

            foreach (SerializedMonoBehaviour owner in GameController.Instance.getSerializedObjects().ToList())
                Restore(owner);
        }

        public static string Encode(MaterialBrush id, string detail)
        {
            string code = ((int)id).ToString();
            return string.IsNullOrEmpty(detail) ? code : code + DETAIL_SEPARATOR + detail;
        }

        public static bool TryDecode(string encoded, out MaterialBrush id, out string detail)
        {
            int separator = encoded.IndexOf(DETAIL_SEPARATOR);
            string code = separator < 0 ? encoded : encoded.Substring(0, separator);
            detail = separator < 0 ? "" : encoded.Substring(separator + 1);

            bool valid = int.TryParse(code, out int value);
            id = (MaterialBrush)value;
            return valid;
        }

        // Path segments are child names; repeated names among siblings get "#n" (n = how many came before).
        public static string GetPath(Transform owner, Transform target)
        {
            List<string> segments = new List<string>();
            for (Transform current = target; current != owner && current != null; current = current.parent)
                segments.Add(GetSegment(current));

            segments.Reverse();
            return string.Join(PATH_SEPARATOR.ToString(), segments);
        }

        private static string GetSegment(Transform transform)
        {
            Transform parent = transform.parent;
            int duplicates = 0;
            for (int i = 0; parent != null && i < transform.GetSiblingIndex(); i++)
            {
                if (parent.GetChild(i).name == transform.name)
                    duplicates++;
            }

            return duplicates == 0 ? transform.name : transform.name + DUPLICATE_MARK + duplicates;
        }

        private static Transform Resolve(Transform owner, string path)
        {
            Transform current = owner;
            if (path.Length == 0)
                return current;

            foreach (string segment in path.Split(PATH_SEPARATOR))
            {
                current = FindChild(current, segment);
                if (current == null)
                    return null;
            }
            return current;
        }

        private static Transform FindChild(Transform parent, string segment)
        {
            string name = segment;
            int duplicate = 0;
            int mark = segment.LastIndexOf(DUPLICATE_MARK);
            if (mark >= 0 && int.TryParse(segment.Substring(mark + 1), out int parsed))
            {
                name = segment.Substring(0, mark);
                duplicate = parsed;
            }

            foreach (Transform child in parent)
            {
                if (child.name == name && duplicate-- == 0)
                    return child;
            }

            // The segment may be a child whose own name contains '#'.
            return mark >= 0 ? parent.Find(segment) : null;
        }
    }
}
