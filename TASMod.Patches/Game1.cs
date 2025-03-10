using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using TASMod.Extensions;
using TASMod.System;

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
                original: AccessTools.PropertyGetter(typeof(Game1), "IsActiveNoOverlay"),
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
            Trace($"Game1.SetWindowSize {TASDateTime.CurrentFrame} {Game1.graphics.IsFullScreen}");
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
            Trace($"Prefix:viewport {Game1.viewport}");
            Trace(
                $"Prefix:ClientBounds {__instance.Window.ClientBounds} DisplayBounds: {__instance.Window.GetDisplayBounds(0)}"
            );
            Trace(
                $"Prefix:graphics {Game1.graphics.PreferredBackBufferWidth}x{Game1.graphics.PreferredBackBufferHeight} ({Game1.graphics.GraphicsProfile})"
            );
            Trace($"Prefix:screen {Game1.game1.screen.Bounds}");
            return true;
        }

        public static void Postfix(ref Game1 __instance)
        {
            Trace($"Postfix:viewport {Game1.viewport}");
            Trace(
                $"Postfix:ClientBounds {__instance.Window.ClientBounds} DisplayBounds: {__instance.Window.GetDisplayBounds(0)}"
            );
            Trace(
                $"Postfix:graphics {Game1.graphics.PreferredBackBufferWidth}x{Game1.graphics.PreferredBackBufferHeight} ({Game1.graphics.GraphicsProfile})"
            );
            Trace($"Postfix:screen {Game1.game1.screen.Bounds}");
        }
    }

    public class Game1_CheckGamepadMode : IPatch
    {
        public override string Name => "Game1.CheckGamepadMode";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), "CheckGamepadMode"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix()
        {
            bool _oldGamepadConnectedState = (bool)
                Reflector.GetValue(Game1.game1, "_oldGamepadConnectedState");
            GamePadState gamePadState = Game1.input.GetGamePadState();
            bool flag2 =
                Game1.isAnyGamePadButtonBeingPressed()
                || Game1.isDPadPressed()
                || Game1.isGamePadThumbstickInMotion()
                || gamePadState.Triggers.Left != 0f
                || gamePadState.Triggers.Right != 0f;
            // Warn($"PRE: Game1.CheckGamepadMode");
            // Warn($"   gamepadControls            {Game1.options.gamepadControls}");
            // Warn($"   gamepadMode                {Game1.options.gamepadMode}");
            // Warn($"   _oldGamepadConnectedState  {_oldGamepadConnectedState}");
            // Warn($"   flag2                      {flag2}");
            // Warn(
            //     $"   connect diff               {_oldGamepadConnectedState} {gamePadState.IsConnected} {gamePadState.IsConnected != _oldGamepadConnectedState}"
            // );
            return true;
        }

        public static void Postfix()
        {
            bool _oldGamepadConnectedState = (bool)
                Reflector.GetValue(Game1.game1, "_oldGamepadConnectedState");
            GamePadState gamePadState = Game1.input.GetGamePadState();
            bool flag2 =
                Game1.isAnyGamePadButtonBeingPressed()
                || Game1.isDPadPressed()
                || Game1.isGamePadThumbstickInMotion()
                || gamePadState.Triggers.Left != 0f
                || gamePadState.Triggers.Right != 0f;
            // Warn($"POST: Game1.CheckGamepadMode");
            // Warn($"   gamepadControls            {Game1.options.gamepadControls}");
            // Warn($"   gamepadMode                {Game1.options.gamepadMode}");
            // Warn($"   _oldGamepadConnectedState  {_oldGamepadConnectedState}");
            // Warn($"   flag2                      {flag2}");
            // Warn(
            //     $"   connect diff               {_oldGamepadConnectedState} {gamePadState.IsConnected} {gamePadState.IsConnected != _oldGamepadConnectedState}"
            // );
        }
    }
}
