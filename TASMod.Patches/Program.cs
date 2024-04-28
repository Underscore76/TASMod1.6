// Force the game to use the new save path associated with your StardewTAS folder.
// Games will save/load into this folder instead of using your actual save files.
// The hope is that this will allow you to use your actual save files while TASing and avoid corruption.
using System;
using HarmonyLib;
using StardewValley;

namespace TASMod.Patches
{
    public class Program_GetLocalAppDataFolder : IPatch
    {
        public static string BaseKey = "Program.GetLocalAppDataFolder";
        public override string Name => BaseKey;

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method("StardewValley.Program:GetLocalAppDataFolder"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.PostfixBase))
            );
            harmony.Patch(
                original: AccessTools.Method(
                    "StardewValley.Program:GetLocalAppDataFolder",
                    new Type[] { typeof(string), typeof(bool) }
                ),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix()
        {
            return false;
        }

        public static void PostfixBase(ref string __result)
        {
            // Warn($"Program.GetLocalAppDataFolder BASE");
            __result = Constants.BasePath;
        }

        public static void Postfix(ref string __result, string subfolder)
        {
            // Warn($"Program.GetLocalAppDataFolder {subfolder}");
            switch (subfolder)
            {
                case "Screenshots":
                    __result = Constants.ScreenshotPath;
                    return;
                case "Saves":
                    __result = Constants.SavesPath;
                    return;
                case "ErrorLogs":
                    __result = Constants.ErrorLogsPath;
                    return;
                case "DisconnectLogs":
                    __result = Constants.DisconnectLogsPath;
                    return;
                case "Exports":
                    __result = Constants.ExportsPath;
                    return;
                default:
                    throw new NotImplementedException(
                        $"Subfolder {subfolder} not implemented in {BaseKey}"
                    );
            }
        }
    }

    public class Program_GetAppDataFolder : IPatch
    {
        public static string BaseKey = "Program.GetAppDataFolder";
        public override string Name => BaseKey;

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Program), BaseKey.Split(".")[1]),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.PostfixBase))
            );

            harmony.Patch(
                original: AccessTools.Method(
                    typeof(Program),
                    BaseKey.Split(".")[1],
                    new Type[] { typeof(string), typeof(bool) }
                ),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix()
        {
            return false;
        }

        public static void PostfixBase(ref string __result)
        {
            // Warn($"Program.GetAppDataFolder BASE");
            __result = Constants.BasePath;
        }

        public static void Postfix(ref string __result, string subfolder)
        {
            // Trace($"Program.GetAppDataFolder {subfolder}");
            switch (subfolder)
            {
                case "Screenshots":
                    __result = Constants.ScreenshotPath;
                    return;
                case "Saves":
                    __result = Constants.SavesPath;
                    return;
                case "ErrorLogs":
                    __result = Constants.ErrorLogsPath;
                    return;
                case "DisconnectLogs":
                    __result = Constants.DisconnectLogsPath;
                    return;
                case "Exports":
                    __result = Constants.ExportsPath;
                    return;
                case null:
                    __result = Constants.BasePath;
                    return;
                default:
                    throw new NotImplementedException(
                        $"Subfolder {subfolder} not implemented in {BaseKey}"
                    );
            }
        }
    }

    public class Program_GetSavesFolder : IPatch
    {
        public static string BaseKey = "Program.GetSavesFolder";
        public override string Name => BaseKey;

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Program), BaseKey.Split(".")[1]),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix()
        {
            return false;
        }

        public static void Postfix(ref string __result)
        {
            // Warn($"Program.GetSavesFolder");
            __result = Constants.SavesPath;
        }
    }
}
