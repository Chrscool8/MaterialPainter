using System.Collections.Generic;

namespace MaterialPainter2
{
    /// <summary>
    /// Paints of one game object, stored as the game's per-object custom data so they travel with the
    /// object through saves, blueprints and multiplayer sync. Without the mod installed the game skips it.
    /// </summary>
    public class PaintData : SerializedRawObject
    {
        private const string PAINTS_KEY = "paints";

        /// <summary>Renderer path relative to the owning object -> encoded paint (see <see cref="PaintStore"/>).</summary>
        public readonly Dictionary<string, string> paints = new Dictionary<string, string>();

        public override void serialize(SerializationContext context, Dictionary<string, object> values)
        {
            Dictionary<string, object> serialized = new Dictionary<string, object>();
            foreach (KeyValuePair<string, string> paint in paints)
                serialized[paint.Key] = paint.Value;

            values[PAINTS_KEY] = serialized;
        }

        public override void deserialize(SerializationContext context, Dictionary<string, object> values)
        {
            paints.Clear();
            if (!values.TryGetValue(PAINTS_KEY, out object value) || !(value is Dictionary<string, object> serialized))
                return;

            foreach (KeyValuePair<string, object> paint in serialized)
            {
                if (paint.Value is string encoded)
                    paints[paint.Key] = encoded;
            }
        }
    }
}
