using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace MaterialPainter2
{
    /// <summary>
    /// Sends user paints through the game's mod command channel. Commands run in order on every player (the sender
    /// included; offline they run immediately), so everyone applies the same paint. Players without the mod ignore
    /// them. Parts are addressed by the owning object's shared objectID plus the part's path within it.
    /// </summary>
    public static class PaintSync
    {
        private const string COMMAND_ID = "MaterialPainter2.Paint";
        private const byte PAYLOAD_VERSION = 1;

        // Tokens of commands this player sent; only the painter gets the paint effects.
        private static readonly HashSet<int> ownTokens = new HashSet<int>();
        private static readonly System.Random random = new System.Random();
        private static bool registered;

        // Drag painting asks every frame; skip repeats while the previous command is still on its way.
        private static GameObject lastTarget;
        private static string lastPaint;

        public static void Register()
        {
            if (registered || CommandController.Instance == null)
                return;

            CommandController.Instance.registerCustomCommand(COMMAND_ID, OnCommand);
            registered = true;
        }

        public static void Unregister()
        {
            if (!registered)
                return;

            if (CommandController.Instance != null)
                CommandController.Instance.unregisterCustomCommand(COMMAND_ID, OnCommand);

            registered = false;
            ownTokens.Clear();
        }

        /// <summary>Paints (or clears, with None) a part for every player.</summary>
        public static void Paint(GameObject target, MaterialBrush id, string detail)
        {
            detail = detail ?? "";
            if (IsAlreadyPainted(target, id, detail))
                return;

            string paint = PaintStore.Encode(id, detail);
            if (target == lastTarget && paint == lastPaint)
                return;

            SerializedMonoBehaviour owner = target.GetComponentInParent<SerializedMonoBehaviour>();
            if (!registered || owner == null || owner.objectID == 0)
            {
                // Not addressable across players (e.g. a preview); paint locally only.
                Painter.Paint(target, id, detail, userAction: true);
                return;
            }

            int token = random.Next();
            ownTokens.Add(token);
            lastTarget = target;
            lastPaint = paint;
            CommandController.Instance.addCustomCommand(COMMAND_ID, Write(token, owner.objectID, PaintStore.GetPath(owner.transform, target.transform), id, detail));
        }

        private static void OnCommand(string identifier, byte[] data)
        {
            try
            {
                if (!TryRead(data, out int token, out uint objectID, out string path, out MaterialBrush id, out string detail))
                    return;

                bool own = ownTokens.Remove(token);
                if (own)
                    lastTarget = null;

                SerializedMonoBehaviour owner = GameController.Instance.getSerializedObject<SerializedMonoBehaviour>(objectID);
                Transform target = owner != null ? PaintStore.Resolve(owner.transform, path) : null;
                if (target == null)
                {
                    MP2.MPDebug($"Paint command target not found: object {objectID}, part '{path}'.");
                    return;
                }

                Painter.Paint(target.gameObject, id, detail, userAction: own);
            }
            catch (Exception ex)
            {
                MP2.MPDebug("Paint command failed: " + ex, always_show: true);
            }
        }

        private static bool IsAlreadyPainted(GameObject target, MaterialBrush id, string detail)
        {
            PaintMarker marker = target.GetComponent<PaintMarker>();
            return id == MaterialBrush.None ? marker == null : marker != null && marker.Matches(id, detail);
        }

        private static byte[] Write(int token, uint objectID, string path, MaterialBrush id, string detail)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(PAYLOAD_VERSION);
                writer.Write(token);
                writer.Write(objectID);
                writer.Write(path);
                writer.Write((int)id);
                writer.Write(detail);
                writer.Flush();
                return stream.ToArray();
            }
        }

        private static bool TryRead(byte[] data, out int token, out uint objectID, out string path, out MaterialBrush id, out string detail)
        {
            using (BinaryReader reader = new BinaryReader(new MemoryStream(data)))
            {
                token = 0;
                objectID = 0;
                path = detail = "";
                id = MaterialBrush.None;

                if (reader.ReadByte() != PAYLOAD_VERSION)
                    return false;

                token = reader.ReadInt32();
                objectID = reader.ReadUInt32();
                path = reader.ReadString();
                id = (MaterialBrush)reader.ReadInt32();
                detail = reader.ReadString();
                return true;
            }
        }
    }
}
