using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewValley;

namespace TASMod.Patches
{
    /*
want to remove cases where Microsoft.Xna.Framework.Game.get_IsActive cause a branch
* if would jump on false, remove the jump check
* if would jump on true? that'd be harder...
* IL for both cases at least seems like it's brfalse in this method

also need to ensure any labels assigned to the start of the IL code get passed to the next one
    */
    public class SInputState_TrueUpdate : IPatch
    {
        public override string Name => "SCore.OnPlayerInstanceUpdating";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(
                    "StardewModdingAPI.Framework.SCore:OnPlayerInstanceUpdating"
                ),
                transpiler: new HarmonyMethod(this.GetType(), nameof(this.Transpiler))
            );
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr)
        {
            /*
ldarg.0 NULL [labels] => push this onto the stack
ldfld StardewModdingAPI.Framework.SGameRunner StardewModdingAPI.Framework.SCore::Game => get the field offset from this for Game
callvirt System.Boolean Microsoft.Xna.Framework.Game::get_IsActive() => call the virtual function on this field (pops prior two things off the stack) and push result on stack
brfalse.s Label11 => jump if top of stack
            */
            List<CodeInstruction> instructions = new List<CodeInstruction>(instr);
            List<Label> labels = null;
            for (int i = 0; i < instructions.Count - 3; i++)
            {
                if (
                    instructions[i].opcode == OpCodes.Ldarg_0
                    && (
                        instructions[i + 1].opcode == OpCodes.Ldfld
                        && instructions[i + 1].operand is FieldInfo fi
                        && fi.Name == "Game"
                        && fi.DeclaringType.FullName == "StardewModdingAPI.Framework.SCore"
                    )
                    && (
                        instructions[i + 2].opcode == OpCodes.Callvirt
                        && instructions[i + 2].operand is MethodInfo mi
                        && mi.Name == "get_IsActive"
                        && mi.DeclaringType.FullName == "Microsoft.Xna.Framework.Game")
                )
                {
                    labels = instructions[i].labels;
                    instructions.RemoveRange(i, 4);
                }
                if (labels != null)
                {
                    instructions[i].labels.AddRange(labels);
                    labels = null;
                }
            }
            foreach (var i in instructions)
            {
                yield return i;
            }
        }
    }
}
