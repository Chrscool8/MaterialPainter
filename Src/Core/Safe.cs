using System;
using System.Collections.Generic;

namespace MaterialPainter2
{
    /// <summary>
    /// Runs mod code that's called from inside the game (patches, events, per-frame updates, enable/disable) so an
    /// error in the mod is logged instead of breaking the game's own work, like loading a park.
    /// </summary>
    public static class Safe
    {
        // Log each distinct failure in full once; repeats (e.g. every frame) only go to the debug log.
        private static readonly HashSet<string> reported = new HashSet<string>();

        public static void Run(string context, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Report(context, ex);
            }
        }

        public static void Report(string context, Exception ex)
        {
            bool firstTime = reported.Add(context + "|" + ex.GetType().FullName + "|" + ex.Message);
            MP2.MPDebug($"Error in {context}: {(firstTime ? ex.ToString() : ex.Message)}", always_show: firstTime);
        }
    }
}
