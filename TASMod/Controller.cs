using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using TASMod.Automation;
using TASMod.Console;
using TASMod.Extensions;
using TASMod.Inputs;
using TASMod.Monogame.Framework;
using TASMod.Overlays;
using TASMod.Patches;
using TASMod.Recording;
using TASMod.System;

namespace TASMod
{
    public class Controller
    {
        public static TASConsole Console = null;
        public static AutomationManager Automation = null;
        public static OverlayManager Overlays = null;
        public static LaunchManager LaunchManager = null;
        public static RecordingManager Recording = null;

        public static TASMouseState LastFrameMouse() => Recording.LastFrameMouse();

        public static SaveState State
        {
            get { return Recording.State; }
            set { Recording.State = value; }
        }
        public static ulong FrameCount => (ulong)Recording.State.Count;

        public static TASSpriteBatch SpriteBatch;
        public static bool FastAdvance;
        public static bool AcceptRealInput = true;
        public static int FramesBetweenRender = 60;
        public static bool SkipSave = true;
        public static bool ResetGame;

        public static TASMouseState RealMouse { get; private set; } = new TASMouseState();
        public static TASKeyboardState RealKeyboard { get; private set; } = new TASKeyboardState();

        static Controller()
        {
            Console = new TASConsole();
            Automation = new AutomationManager();
            Overlays = new OverlayManager();
            LaunchManager = new LaunchManager();
            Recording = new RecordingManager();
            SpriteBatch = new TASSpriteBatch(Game1.graphics.GraphicsDevice);
        }

        public static void LateInit()
        {
            LoadEngineState();
            OverrideStaticDefaults();
            Reset();
        }

        public static bool Update()
        {
            // handle initial game launch
            if (LaunchManager.Update())
                return true;

            // update inputs and basic rendering
            RealInputState.Update();
            Console.Update();
            Overlays.Update();
            TASInputState.Active = false;

            // check if there is frame data to process
            if (Recording.Update())
            {
                return true;
            }

            // check if there is automation data to process
            if (Automation.Update())
            {
                Recording.PushFrame();
                return true;
            }

            if (HandleRealInput())
            {
                TASInputState.SetKeyboard(RealKeyboard);
                TASInputState.SetMouse(RealMouse);
                Recording.PushFrame();
                return true;
            }

            return false;
        }

        public static bool Draw()
        {
            bool tmp = TASSpriteBatch.Active;
            TASSpriteBatch.Active = true;
            Game1.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                null,
                null
            );
            foreach (var overlay in Overlays)
            {
                overlay.Value.Draw();
            }
            Game1.spriteBatch.End();
            TASSpriteBatch.Active = tmp;
            return true;
        }

        public static void DrawLate()
        {
            // draw over the final buffer
            bool tmp = TASSpriteBatch.Active;
            TASSpriteBatch.Active = true;
            if (Console != null)
            {
                Console.Draw();
            }

            TASSpriteBatch.Active = tmp;
        }

        private static bool HandleRealInput()
        {
            RealMouse = new TASMouseState(RealInputState.mouseState);
            RealKeyboard = new TASKeyboardState(RealInputState.keyboardState);

            if (Console.IsOpen)
                return false;

            bool capture = Overlays.HandleInput(RealMouse, RealKeyboard);
            if (capture)
                return false;

            if (!AcceptRealInput)
                return false;

            if (RealInputState.KeyTriggered(Keys.Q) || RealInputState.KeyTriggered(Keys.Down))
            {
                RealKeyboard.Remove(Keys.Q);
                RealKeyboard.Remove(Keys.Down);
                return true;
            }

            if (RealInputState.KeyTriggered(Keys.R))
            {
                RealKeyboard.Add(Keys.R);
                RealKeyboard.Add(Keys.RightShift);
                RealKeyboard.Add(Keys.Delete);
                return true;
            }

            if (RealInputState.IsKeyDown(Keys.Space))
            {
                RealKeyboard.Remove(Keys.Space);
                return true;
            }

            if (RealInputState.KeyTriggered(Keys.OemPipe))
            {
                Reset();
                return false;
            }

            return false;
        }

        public static void Reset(bool fastAdvance = false)
        {
            ModEntry.Console.Log("Calling reset", LogLevel.Error);
            FastAdvance = fastAdvance;
            ResetGame = true;
            Game1.audioEngine.Engine.Reset();
            GameRunner_Update.Reset();
            GameRunner_Draw.Reset();
            TASInputState.Reset();
            TASDateTime.Reset();
            RandomExtensions.Reset();
            // IsPaused = false;
        }

        public static void OverrideStaticDefaults()
        {
            // override the LocalMultiplayer.StaticDefaults for uniqueId for this Game
            FieldInfo defaultsField = typeof(LocalMultiplayer).GetField(
                "staticDefaults",
                BindingFlags.Static | BindingFlags.NonPublic
            );
            var defaults = (List<object>)defaultsField.GetValue(null);
            FieldInfo fieldsField = typeof(LocalMultiplayer).GetField(
                "staticFields",
                BindingFlags.Static | BindingFlags.NonPublic
            );
            var fields = (List<FieldInfo>)fieldsField.GetValue(null);
            for (int i = 0; i < fields.Count; i++)
            {
                //ModEntry.Console.Log($"{i.ToString("D4")}: {fields[i].Name}", LogLevel.Warn);
                if (fields[i].Name == "uniqueIDForThisGame")
                {
                    //ModEntry.Console.Log($"{defaults[i]}", LogLevel.Warn);
                    defaults[i] = TASDateTime.uniqueIdForThisGame;
                }
                if (fields[i].Name == "random")
                {
                    // TODO: Does this do anything? it's going to copy the reference to the same RNG object anyways
                    defaults[i] = new Random(0);
                    Game1.random = new Random(0);
                }
                if (fields[i].Name == "recentMultiplayerRandom")
                {
                    // TODO: Does this do anything? it's going to copy the reference to the same RNG object anyways
                    defaults[i] = new Random(0);
                    Game1.recentMultiplayerRandom = new Random(0);
                }
            }
            //ModEntry.Console.Log($"number of statics: {defaults.Count}");
        }

        public static void SaveEngineState(string engine_name = "default_engine_state")
        {
            EngineState state = new EngineState();
            string filePath = Path.Combine(
                Constants.BasePath,
                string.Format("{0}.json", engine_name)
            );
            using (StreamWriter file = File.CreateText(filePath))
            {
                JsonSerializer serializer = new JsonSerializer { Formatting = Formatting.Indented };
                serializer.Serialize(file, state);
            }
        }

        public static void LoadEngineState(string engine_name = "default_engine_state")
        {
            string filePath = Path.Combine(
                Constants.BasePath,
                string.Format("{0}.json", engine_name)
            );
            if (!File.Exists(filePath))
                return;

            EngineState state = null;
            using (StreamReader file = File.OpenText(filePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                // TODO: any safety rails for overwriting current State?
                state = (EngineState)serializer.Deserialize(file, typeof(EngineState));
            }
            state.UpdateGame();
        }
    }
}
