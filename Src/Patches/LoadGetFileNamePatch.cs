using HarmonyLib;

using System;
using System.Reflection;

using static GameController;

namespace MaterialPainter2
{
    [HarmonyPatch]
    public class LoadGetFileNamePatch
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Sall Good")]
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(Loader), "loadSavegame", parameters: new Type[] { typeof(string), typeof(GameController.GameMode), typeof(ParkSettings), typeof(bool), typeof(bool), typeof(bool), typeof(OnParkLoadedHandler), typeof(OnParkDeserializedHandler), typeof(SerializationContext.Context) });

        [HarmonyPrefix]
        public static void Prefix(string filePath, GameMode gameMode, ParkSettings settings, bool rememberFilePath, bool newPark, bool showNewParkUI = true, GameController.OnParkLoadedHandler onParkLoadedHandler = null, GameController.OnParkDeserializedHandler onParkDeserializedHandler = null, SerializationContext.Context context = (SerializationContext.Context)0)
        {
            MP2.MPDebug(filePath);
            MP2.current_file_path = filePath;
        }
    }
}
