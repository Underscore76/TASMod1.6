using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using TASMod.Extensions;
using TASMod.System;
using TASMod.Views;

namespace TASMod.Patches
{
    public class GameRunner_Draw : IPatch
    {
        public override string Name => "GameRunner.Draw";
        private static bool CanDraw;
        public static int Counter;

        public GameRunner_Draw()
        {
            CanDraw = false;
            Counter = 0;
        }

        public static void Reset()
        {
            CanDraw = false;
            Counter = 0;
        }

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(GameRunner), "Draw"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix(ref GameTime gameTime)
        {
            CanDraw = (Counter + 1) == GameRunner_Update.Counter;
            gameTime = TASDateTime.CurrentGameTime;
            return CanDraw;
        }

        public static void Postfix(ref GameTime gameTime)
        {
            if (CanDraw)
            {
                Counter++;
                TASDateTime.Update();
                // NOTE: Allows for each frame to get new rng values to match Interop.GetRandomBytes
                RandomExtensions.Update();
                Controller.Draw();
            }
            else
            {
                switch (Controller.ViewController.CurrentView)
                {
                    case TASView.Base:
                        RedrawFrame(gameTime);
                        Controller.Draw();
                        break;
                    default:
                        Controller.ViewController.Draw();
                        break;
                }
            }
            Controller.DrawLate();
            CanDraw = false;
        }

        public static void RedrawFrame(GameTime gameTime)
        {
            foreach (Game1 instance2 in GameRunner.instance.gameInstances)
            {
                GameRunner.LoadInstance(instance2);
                Viewport old_viewport = GameRunner.instance.GraphicsDevice.Viewport;
                Game1_renderScreenBuffer.Base(instance2.screen, instance2.uiScreen);
                GameRunner.instance.GraphicsDevice.Viewport = old_viewport;
            }

            if (LocalMultiplayer.IsLocalMultiplayer())
            {
                GameRunner.instance.GraphicsDevice.Clear(Color.White);
                foreach (Game1 gameInstance in GameRunner.instance.gameInstances)
                {
                    Game1.isRenderingScreenBuffer = true;
                    gameInstance.DrawSplitScreenWindow();
                    Game1.isRenderingScreenBuffer = false;
                }
            }

            // Run the base Game.Draw function so game doesn't hang
            InvokeBase(gameTime);
        }

        private static IntPtr FuncPtr = IntPtr.Zero;

        public static void InvokeBase(GameTime gameTime)
        {
            if (GameRunner.instance != null)
            {
                if (FuncPtr == IntPtr.Zero)
                {
                    var method = typeof(Game).GetMethod(
                        "Draw",
                        BindingFlags.NonPublic | BindingFlags.Instance
                    );
                    FuncPtr = method.MethodHandle.GetFunctionPointer();
                }
                // get the actual base function
                var func =
                    (Action<GameTime>)
                        Activator.CreateInstance(
                            typeof(Action<GameTime>),
                            GameRunner.instance,
                            FuncPtr
                        );
                func(gameTime);
            }
        }
    }

    public class GameRunner_Update : IPatch
    {
        public override string Name => "GameRunner.Update";
        private static bool CanUpdate;
        public static int Counter;

        private static List<string> LastRun = new List<string>();

        public GameRunner_Update()
        {
            CanUpdate = false;
            Counter = 0;
        }

        public static void Reset()
        {
            CanUpdate = false;
            Counter = 0;
        }

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(GameRunner), "Update"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix(GameRunner __instance, ref GameTime gameTime)
        {
            if (Controller.ResetGame)
            {
                ModEntry.Console.Log("Running Reset", LogLevel.Error);
                Controller.ResetGame = false;
                __instance.Reset();
                return false;
            }
            if (Controller.FastAdvance)
            {
                __instance.RunFast();
                return false;
            }
            if (GameRunner_Draw.Counter != Counter)
            {
                CanUpdate = false;
            }
            else
            {
                CanUpdate = Controller.Update();
                gameTime = TASDateTime.CurrentGameTime;
            }
            return CanUpdate;
        }

        public static void Postfix(ref GameTime gameTime)
        {
            if (CanUpdate)
            {
                Counter++;
            }
            else
            {
                Controller.ViewController.Update();
                InvokeBase(gameTime);
            }
            CanUpdate = false;
        }

        private static IntPtr FuncPtr = IntPtr.Zero;

        public static void InvokeBase(GameTime gameTime)
        {
            if (GameRunner.instance != null)
            {
                if (FuncPtr == IntPtr.Zero)
                {
                    var method = typeof(Game).GetMethod(
                        "Update",
                        BindingFlags.NonPublic | BindingFlags.Instance
                    );
                    FuncPtr = method.MethodHandle.GetFunctionPointer();
                }
                // get the actual base function
                var func =
                    (Action<GameTime>)
                        Activator.CreateInstance(
                            typeof(Action<GameTime>),
                            GameRunner.instance,
                            FuncPtr
                        );
                func(gameTime);
            }
        }
    }
}
