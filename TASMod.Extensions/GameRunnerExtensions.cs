using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using TASMod.Monogame.Framework;
using TASMod.Networking;
using TASMod.System;
using TASMod.Views;

namespace TASMod.Extensions
{
    internal static class GameRunnerExtensions
    {
        public static void Reset(this GameRunner runner)
        {
            Controller.ViewController.Reset();
            var input = Game1.input;


            ModEntry.Console.Log("Reseting the GameRunner", LogLevel.Trace);
            if (Game1.content != null)
                Game1.content.Unload();

            int numInstances = runner.gameInstances.Count;
            ModEntry.Console.Log("Deleting all existing instances", LogLevel.Trace);
            for (int i = numInstances - 1; i >= 0; i--)
            {
                GameRunner.LoadInstance(runner.gameInstances[i]);
                if (Game1.server != null) { Game1.server = null; }
                if (Game1.client != null) { Game1.client = null; }
                runner.gameInstances[i].exitEvent(null, null);
                runner.gameInstances.RemoveAt(i);
                Game1.game1 = null;
            }
            NetworkState.Shutdown();
            // Force random to be in a vanilla state
            Controller.OverrideStaticDefaults();

            ModEntry.Console.Log(
                $"Current DateTime: {DateTime.UtcNow} ({TASDateTime.uniqueIdForThisGame}) ({(DateTime.UtcNow - new DateTime(2012, 6, 22)).TotalSeconds})",
                LogLevel.Alert
            );
            ModEntry.Console.Log(
                $"Before instance create random: {Game1.random}\tuniqId: {Game1.uniqueIDForThisGame}",
                LogLevel.Alert
            );
            ModEntry.Console.Log("Creating a New Instance", LogLevel.Trace);
            runner.AddGameInstance(Microsoft.Xna.Framework.PlayerIndex.One);
            ModEntry.Console.Log(
                $"After instance create random: {Game1.random}\tuniqId: {Game1.uniqueIDForThisGame}",
                LogLevel.Alert
            );
            ModEntry.Console.Log("Setting Instance Defaults", LogLevel.Trace);
            var method = ModEntry.Reflection.GetMethod(typeof(GameRunner), "SetInstanceDefaults");
            method.Invoke(new object[] { runner.gameInstances[0] });
            ModEntry.Console.Log(
                $"After setting instance defaults random: {Game1.random}\tuniqId: {Game1.uniqueIDForThisGame}",
                LogLevel.Alert
            );

            ModEntry.Console.Log($"Set Game1.game1 to {runner.gameInstances[0]}", LogLevel.Trace);
            Game1.game1 = runner.gameInstances[0];
            Game1.game1.instancePlayerOneIndex = PlayerIndex.One;
            Game1.game1.instanceIndex = 0;

            // TODO: force an initialization of the rng. Our Frame 0 RNG isn't the same for some reason
            // I assume it's cause we are getting a garbo'd random that is unassigned
            // it's getting the actual game seed time
            Game1.random = new Random(Controller.State == null ? 0 : Controller.State.Frame0RandomSeed);
            for (int i = 0; i < Controller.State?.Frame0RandomIndex; i++)
                Game1.random.Next();
            ModEntry.Console.Log("Instance_Initialize", LogLevel.Trace);
            runner.gameInstances[0].Instance_Initialize();

            // enforcing input/multiplayer get carried over
            // Game1.input = input;
            Game1.input = (InputState)Activator.CreateInstance(Reflector.GetTypeInAnyAssembly("StardewModdingAPI.Framework.Input.SInputState"));
            ModEntry
                .Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer")
                .SetValue(new Multiplayer());

            ModEntry.Console.Log("Instance_LoadContent", LogLevel.Trace);
            runner.gameInstances[0].Instance_LoadContent();

            // reset game seed
            TASDateTime.setUniqueIDForThisGame((ulong)Controller.State.Seed);
            Game1.uniqueIDForThisGame = TASDateTime.uniqueIdForThisGame;

            // stash state
            ModEntry.Console.Log("SaveInstance", LogLevel.Trace);
            GameRunner.SaveInstance(runner.gameInstances[0], force: true);

            ModEntry.Console.Log("ResetStaticTextures", LogLevel.Trace);
            ResetStaticTextures();

            Controller.State.RestoreGameDetails();
        }

        public static void ResetStaticTextures()
        {
            StardewValley.TerrainFeatures.HoeDirt.lightTexture = null;
            StardewValley.TerrainFeatures.HoeDirt.darkTexture = null;
            StardewValley.TerrainFeatures.HoeDirt.snowTexture = null;
        }

        public static void InvokeUpdate(this GameRunner runner, GameTime gameTime)
        {
            Reflector.InvokeMethod(runner, "Update", new object[] { gameTime });
        }

        public static void InvokeBeginDraw(this GameRunner runner)
        {
            Reflector.InvokeMethod(runner, "BeginDraw");
        }

        public static void InvokeDraw(this GameRunner runner, GameTime gameTime)
        {
            Reflector.InvokeMethod(runner, "Draw", new object[] { gameTime });
        }

        public static void InvokeEndDraw(this GameRunner runner)
        {
            Reflector.InvokeMethod(runner, "EndDraw");
        }

        public static void Step(this GameRunner runner)
        {
            GameTime gameTime = TASDateTime.CurrentGameTime;
            //ModEntry.Console.Log($"invoking Update... {gameTime.TotalGameTime}", LogLevel.Error);
            runner.InvokeUpdate(gameTime);
            //ModEntry.Console.Log($"\tinvoking BeginDraw... {gameTime.TotalGameTime}", LogLevel.Error);
            runner.InvokeBeginDraw();
            //ModEntry.Console.Log($"\tinvoking Draw... {gameTime.TotalGameTime}", LogLevel.Error);
            runner.InvokeDraw(gameTime);
            //ModEntry.Console.Log($"\tinvoking EndDraw... {gameTime.TotalGameTime}", LogLevel.Error);
            runner.InvokeEndDraw();
            //ModEntry.Console.Log($"finished step. {gameTime.TotalGameTime}", LogLevel.Error);
            runner.EventLoop();
        }

        public static void RunFast(this GameRunner runner)
        {
            //ModEntry.Console.Log($"Resetting Fast... {TASDateTime.CurrentFrame} to {Controller.State.Count}", LogLevel.Error);
            Controller.FastAdvance = false;
            int counter = 0;
            TASSpriteBatch.Active = false;
            int maxFrame = Controller.State.Count - Controller.FinalFrames;
            if (Controller.PlaybackFrame != -1)
                maxFrame = Math.Min(maxFrame, Controller.PlaybackFrame);
            while ((int)TASDateTime.CurrentFrame < maxFrame)
            {
                try
                {
                    //ModEntry.Console.Log($"Reset {TASDateTime.CurrentFrame}", LogLevel.Error);
                    //runner.Step();
                    GameTime gameTime = TASDateTime.CurrentGameTime;
                    runner.InvokeUpdate(gameTime);
                    runner.InvokeDraw(gameTime);
                    runner.EventLoop();
                    if (counter++ >= Controller.FramesBetweenRender)
                        break;
                }
                catch
                {
                    Game1.game1.Exit();
                    Environment.Exit(1);
                }
            }
            //ModEntry.Console.Log($"CurrentFrame: {TASDateTime.CurrentFrame}", LogLevel.Warn);
            TASSpriteBatch.Active = true;
            if (counter >= Controller.FramesBetweenRender)
                Controller.FastAdvance = true;
        }

        // these are used to ensure that blocking resets still allow SDL to update the window
        // helps with active/inactive checks on backgrounding the window
        public static object Platform = null;
        public static MethodInfo RunEventLoop = null;

        public static void EventLoop(this GameRunner runner)
        {
            if (Platform == null)
            {
                var fieldInfo = Reflector.GetField(runner, "Platform");
                if (fieldInfo != null)
                {
                    Platform = Reflector.GetValue(runner, "Platform");
                    RunEventLoop = Platform
                        .GetType()
                        .GetMethod("SdlRunLoop", BindingFlags.NonPublic | BindingFlags.Instance);
                }
                else
                {
                    throw new InvalidOperationException("Platform field not found");
                }
            }
            RunEventLoop.Invoke(Platform, new object[] { });
        }

        public static bool IsExiting(this GameRunner runner)
        {
            return true;
        }

        public static void BlockingReset(this GameRunner runner)
        {
            Controller.Reset(fastAdvance: false);
            Controller.ResetGame = false;
            runner.Reset();
            Controller.AcceptRealInput = true;
            Controller.Console.historyTail = Controller.Console.historyLog.Count - 1;
            while ((int)TASDateTime.CurrentFrame < Controller.State.Count)
            {
                runner.EventLoop();
                Controller.Console.Update();
                runner.Step();
            }
        }

        public static void BlockingFastReset(this GameRunner runner)
        {
            Controller.Reset(fastAdvance: false);
            Controller.ResetGame = false;
            runner.Reset();
            Controller.AcceptRealInput = true;
            Controller.Console.historyTail = Controller.Console.historyLog.Count - 1;
            Controller.BlockOverlays = false;
            while ((int)TASDateTime.CurrentFrame < Controller.State.Count)
            {
                runner.EventLoop();
                Controller.Console.Update();
                // intermittently draw or ensure the last few frames draw completely
                if (
                    (int)TASDateTime.CurrentFrame % Controller.FramesBetweenRender == 0
                    || (int)TASDateTime.CurrentFrame + 3 > Controller.State.Count
                )
                {
                    runner.Step();
                }
                else
                {
                    TASSpriteBatch.Active = false;
                    GameTime gameTime = TASDateTime.CurrentGameTime;
                    runner.InvokeUpdate(gameTime);
                    runner.InvokeDraw(gameTime);
                    TASSpriteBatch.Active = true;
                }
            }
            Controller.BlockOverlays = true;
        }
    }
}
