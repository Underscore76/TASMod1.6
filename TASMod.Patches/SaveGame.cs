using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewValley;

namespace TASMod.Patches
{
    public class SaveGame_TryReadSaveFile : IPatch
    {
        public static string BaseKey = "SaveGame.TryReadSaveFile";
        public override string Name => BaseKey;

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(
                    typeof(SaveGame),
                    BaseKey.Split(".")[1]
                ),
                transpiler: new HarmonyMethod(this.GetType(), nameof(this.Transpiler))
            );
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr)
        {
            // NOTE: the GetSavesFolder call is getting fully inlined, we need to transpile the whole method and swap the op to call our method
            // ideally this could've just been a prefix/postfix on Program.GetSavesFolder
            foreach (var i in instr)
            {
                if (
                    i.opcode == OpCodes.Call
                    && i.operand is MethodInfo m
                    && m.Name == "GetSavesFolder"
                )
                {
                    yield return new CodeInstruction(
                        OpCodes.Call,
                        AccessTools.Method(typeof(SaveGame_TryReadSaveFile), nameof(Override))
                    );
                    continue;
                }
                yield return i;
            }
        }

        public static string Override()
        {
            // ModEntry.Console.Log($"Getting SavesPath for {BaseKey}: {Constants.SavesPath}");
            return Constants.SavesPath;
        }
    }
}
