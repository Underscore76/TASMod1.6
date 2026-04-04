using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using TASMod.Extensions;
using TASMod.Monogame.Framework.Audio;

namespace TASMod.Patches
{
    public class Game1_ShouldDrawOnBuffer : IPatch
    {
        public override string Name => "Game1.ShouldDrawOnBuffer";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), "ShouldDrawOnBuffer"),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static void Postfix(ref bool __result)
        {
            // ensure the game always attempts to draw on top of the buffer
            __result = true;
        }
    }

    public class Game1_renderScreenBuffer : IPatch
    {
        public override string Name => "Game1.renderScreenBuffer";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), "renderScreenBuffer"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix()
        {
            return false;
        }

        public static void Postfix(Game1 __instance, RenderTarget2D target_screen)
        {
            if (Game1.game1.takingMapScreenshot)
            {
                Game1.game1.GraphicsDevice.SetRenderTarget(null);
            }
            else
            {
                Base(target_screen, __instance.uiScreen);
            }
        }

        private static Color color => Color.CornflowerBlue;

        public static void Base(RenderTarget2D screen, RenderTarget2D uiScreen)
        {
            bool inBeginEndPair = Game1.spriteBatch.inBeginEndPair();
            if (!inBeginEndPair)
            {
                Game1.spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.Opaque,
                    SamplerState.LinearClamp,
                    DepthStencilState.Default,
                    RasterizerState.CullNone
                );
            }
            Game1.game1.GraphicsDevice.SetRenderTarget(null);
            Game1.game1.GraphicsDevice.Clear(color);
            if (screen != null)
            {
                Game1.spriteBatch.Draw(
                    screen,
                    new Vector2(0f, 0f),
                    screen.Bounds,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    Game1.options.zoomLevel,
                    SpriteEffects.None,
                    1f
                );
            }
            if (uiScreen != null)
            {
                Game1.spriteBatch.End();
                Game1.spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.LinearClamp,
                    DepthStencilState.Default,
                    RasterizerState.CullNone
                );
                Game1.spriteBatch.Draw(
                    uiScreen,
                    new Vector2(0f, 0f),
                    uiScreen.Bounds,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    Game1.options.uiScale,
                    SpriteEffects.None,
                    1f
                );
            }
            if (!inBeginEndPair)
            {
                Game1.spriteBatch.End();
            }
        }
    }

    public class Game1_OnActivated : IPatch
    {
        public override string Name => "Game1.OnActivated";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), "OnActivated"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix()
        {
            return false;
        }

        public static void Postfix(ref Game1 __instance, object sender, EventArgs args)
        {
            var field = ModEntry.Reflection.GetField<int>(typeof(Game1), "_activatedTick");
            field.SetValue(Game1.ticks + 1);
        }
    }

    public class Game1_IsActiveNoOverlay : IPatch
    {
        public override string Name => "Game1.IsActiveNoOverlay";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Property(typeof(Game1), "IsActiveNoOverlay").GetMethod,
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static void Postfix(ref bool __result)
        {
            __result = true;
        }
    }

    public class Game1_SetWindowSize : IPatch
    {
        public override string Name => "Game1.SetWindowSize";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), "SetWindowSize"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix(Game1 __instance, ref int w, ref int h)
        {
            bool windowed = ModEntry.Config.Windowed;
            Game1.graphics.IsFullScreen = !windowed;
            w = ModEntry.Config.ScreenWidth;
            h = ModEntry.Config.ScreenHeight;
            Game1.options.fullscreen = !windowed;
            Game1.options.preferredResolutionX = w;
            Game1.options.preferredResolutionY = h;
            __instance.Window.BeginScreenDeviceChange(!windowed);
            __instance.Window.EndScreenDeviceChange(__instance.Window.ScreenDeviceName, w, h);
            Game1.graphics.PreferredBackBufferWidth = w;
            Game1.graphics.PreferredBackBufferHeight = h;
            Game1.graphics.SynchronizeWithVerticalRetrace = true;
            return true;
        }

        public static void Postfix(ref Game1 __instance)
        {
        }
    }

    public class Game1_Draw : IPatch
    {
        public override string Name => "Game1.Draw";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(
                    typeof(Game1), "Draw"
                ),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }
        public static bool Prefix(ref Game1 __instance)
        {
            Controller.Timing.GameDrawPrefix(__instance.instanceIndex);
            return true;
        }

        public static void Postfix(ref Game1 __instance)
        {
            Controller.Timing.GameDrawPostfix(__instance.instanceIndex);
        }
    }

    // see SMAPI_Score.cs: effectively need to patch out the IsActive check
    public class Game1__update : IPatch
    {
        public override string Name => "Game1._update";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(
                    typeof(Game1), "_update"
                ),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix)),
                transpiler: new HarmonyMethod(this.GetType(), nameof(this.Transpiler))
            );
        }
        public static bool Prefix(ref Game1 __instance)
        {
            Controller.Timing.GameUpdatePrefix(__instance.instanceIndex);
            return true;
        }

        public static void Postfix(ref Game1 __instance)
        {
            Controller.Timing.GameUpdatePostfix(__instance.instanceIndex);
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr)
        {
            /*
 ldarg.0 NULL => this
 call System.Boolean StardewValley.InstanceGame::get_IsActive() => this.IsActive
 brfalse Label104 => if (!this.IsActive) jump to Label104
            */
            List<CodeInstruction> instructions = new List<CodeInstruction>(instr);
            List<Label> labels = null;
            for (int i = 0; i < instructions.Count - 3; i++)
            {
                if (
                    instructions[i].opcode == OpCodes.Ldarg_0
                    && (
                        instructions[i + 1].opcode == OpCodes.Call
                        && instructions[i + 1].operand is MethodInfo mi
                        && mi.Name == "get_IsActive"
                        && mi.DeclaringType.FullName == "StardewValley.InstanceGame"
                    )

                )
                {
                    labels = instructions[i].labels;
                    instructions.RemoveRange(i, 3);
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

    public class Game1_InitializeSound : IPatch
    {
        public static bool Override = true;
        public override string Name => "Game1.InitializeSounds";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), "InitializeSounds"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix()
        {
            return !Override;
        }
        public static void Postfix()
        {
            if (!Override) return;
            Game1.audioEngine = TASAudioEngine.Get();
            Game1.waveBank = TASWaveBank.Get(Game1.audioEngine.Engine, "Wave Bank.xwb");
            Game1.waveBank1_4 = TASWaveBank.Get(Game1.audioEngine.Engine, "Wave Bank(1.4).xwb");
            Game1.soundBank = TASSoundBank.Get(Game1.audioEngine.Engine, "Sound Bank.xsb");
            if (Game1.audioEngine is TASAudioEngine engine && Game1.soundBank is TASSoundBank soundBank && engine.SoundBank == null)
            {
                engine.SoundBank = soundBank;
            }
            Game1.audioEngine.Update();
            Game1.musicCategory = Game1.audioEngine.GetCategory("Music");
            Game1.soundCategory = Game1.audioEngine.GetCategory("Sound");
            Game1.ambientCategory = Game1.audioEngine.GetCategory("Ambient");
            Game1.footstepCategory = Game1.audioEngine.GetCategory("Footsteps");
            Game1.wind = Game1.soundBank.GetCue("wind");
            Game1.chargeUpSound = Game1.soundBank.GetCue("toolCharge");
            AmbientLocationSounds.InitShared();
        }
    }
}
