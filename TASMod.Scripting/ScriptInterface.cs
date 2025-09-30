using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Netcode;
using NLua;
using NLua.Exceptions;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using TASMod.Console;
using TASMod.Extensions;
using TASMod.Helpers;
using TASMod.Inputs;
using TASMod.Minigames;
using TASMod.Recording;
using TASMod.Simulators;
using TASMod.Simulators.SkullCaverns;
using TASMod.System;
using TASMod.Views;

namespace TASMod.Scripting
{
    public class ScriptInterface
    {
        public static ScriptInterface _instance = null;
        private static TASConsole Console => Controller.Console;
        public Dictionary<Keys, Tuple<string, LuaFunction>> KeyBinds;

        public bool SimulateRealAdvance = false;
        public double SleepRetry = 2;
        public Stopwatch _gameTimer;
        public long _previousTicks = 0;
        public TimeSpan _accumulatedElapsedTime;
        public TimeSpan _targetElapsedTime = TimeSpan.FromTicks(166667); // 60fps

        public ScriptInterface()
        {
            _instance = this;
            KeyBinds = new Dictionary<Keys, Tuple<string, LuaFunction>>();
            _gameTimer = Stopwatch.StartNew();
        }

        public void PrintKeyBinds()
        {
            foreach (var kvp in KeyBinds)
            {
                Console.PushResult($"{kvp.Key}: {kvp.Value}");
            }
        }

        public void AddKeyBind(Keys key, string funcName, LuaFunction function)
        {
            Console.PushResult($"Attempting to bind {key} to func `{funcName}`");
            KeyBinds.Add(key, new Tuple<string, LuaFunction>(funcName, function));
        }

        public void RemoveKeyBind(Keys key)
        {
            KeyBinds.Remove(key);
        }

        public void ClearKeyBinds()
        {
            KeyBinds.Clear();
        }

        public bool ReceiveKeys(IEnumerable<Keys> keys)
        {
            bool matched = false;
            foreach (var key in keys)
            {
                if (KeyBinds.ContainsKey(key))
                {
                    try
                    {
                        LuaFunction func = KeyBinds[key].Item2;
                        func.Call();
                        matched = true;
                    }
                    catch (LuaScriptException e)
                    {
                        string err = e.Message;
                        if (e.InnerException != null)
                            err += "\n\t" + e.InnerException.Message;
                        Console.PushResult("failed to run keybind");
                        Console.PushResult(err);
                    }
                }
            }
            return matched;
        }

        public int GetCurrentFrame()
        {
            return (int)TASDateTime.CurrentFrame;
        }

        public void Print(object s)
        {
            try
            {
                Console.PushResult(s.ToString());
                Console.historyTail = Console.historyLog.Count - 1;
            }
            catch (LuaScriptException e)
            {
                string err = e.Message;
                if (e.InnerException != null)
                    err += "\n\t" + e.InnerException.Message;
                Console.PushResult(err);
            }
        }

#pragma warning disable CA1822 // Mark members as static
        public bool HasStep
#pragma warning restore CA1822 // Mark members as static
        {
            get { return TASInputState.Active; }
        }

        public void WaitPrefix()
        {
        // uses the same logic as https://github.com/MonoGame/MonoGame/blob/develop/MonoGame.Framework/Game.cs#L58
        // idea is to force a sleep until the next tick should fire
        // not super accurate but from testing reaches pretty close to 60fps
        PrefixTicks:
            var currentTicks = _gameTimer.Elapsed.Ticks;
            _accumulatedElapsedTime += TimeSpan.FromTicks(currentTicks - _previousTicks);
            _previousTicks = currentTicks;

            if (SimulateRealAdvance && _accumulatedElapsedTime < _targetElapsedTime)
            {
                var sleepTime = (_targetElapsedTime - _accumulatedElapsedTime).TotalMilliseconds;
                if (sleepTime >= SleepRetry)
                    Thread.Sleep(1);
                goto PrefixTicks;
            }
        }

        public void WaitPostfix()
        {
            _accumulatedElapsedTime = TimeSpan.Zero;
        }

        public void AdvanceFrame(LuaTable input)
        {
            RealInputState.Update();
            WaitPostfix();
            ReadInputStates(
                input,
                out TASKeyboardState kstate,
                out TASMouseState mstate,
                out string injectText
            );
            Controller.State.FrameStates.Add(
                new FrameState(
                    kstate.GetKeyboardState(),
                    mstate.GetMouseState(),
                    inject: injectText
                )
            );
            StepLogic();
            WaitPostfix();
        }

        public void StepLogic()
        {
            if (RealInputState.IsKeyDown(Keys.OemMinus) && RealInputState.IsKeyDown(Keys.OemPlus))
            {
                throw new Exception("dropping out of step logic");
            }
            Controller.AcceptRealInput = false;
            GameRunner.instance.Step();
            TASInputState.Active = Controller.HasUpdate();
            Controller.AcceptRealInput = true;
        }

        public static void ReadInputStates(
            LuaTable input,
            out TASKeyboardState kstate,
            out TASMouseState mstate,
            out string injectText
        )
        {
            LuaTable keyboard = null;
            LuaTable mouse = null;
            injectText = "";
            if (input != null)
            {
                keyboard = (LuaTable)input["keyboard"];
                mouse = (LuaTable)input["mouse"];
                injectText = (string)input["text"];
            }

            kstate = new TASKeyboardState();
            if (keyboard != null)
            {
                kstate = new TASKeyboardState();
                foreach (var obj in keyboard.Values)
                {
                    _ = kstate.Add((Keys)obj);
                }
            }

            if (mouse != null)
            {
                mstate = new TASMouseState
                {
                    MouseX = Convert.ToInt32(mouse["X"]),
                    MouseY = Convert.ToInt32(mouse["Y"]),
                    LeftMouseClicked = Convert.ToBoolean(mouse["left"]),
                    RightMouseClicked = Convert.ToBoolean(mouse["right"])
                };
            }
            else
            {
                mstate = new TASMouseState(Controller.LastFrameMouse(), false, false);
            }
        }

        public void ResetGame(int frame)
        {
            Controller.State.Reset(frame);
            Controller.Reset(fastAdvance: false);
            StepLogic();
        }

        public void FastResetGame(int frame)
        {
            Controller.State.Reset(frame);
            Controller.Reset(fastAdvance: true);
        }

        public void BlockResetGame(int frame)
        {
            Controller.State.Reset(frame);
            GameRunner.instance.BlockingReset();
        }

        public void BlockFastResetGame(int frame)
        {
            Controller.State.Reset(frame);
            GameRunner.instance.BlockingFastReset();
        }

        public void BlockLoad(string prefix)
        {
            SaveState state = SaveState.Load(prefix);
            if (state == null)
            {
                Controller.Console.Warn($"\tInput file \"{prefix}\" not found");
                return;
            }
            Controller.State = state;
            GameRunner.instance.BlockingReset();
        }
        public void BlockFastLoad(string prefix)
        {
            SaveState state = SaveState.Load(prefix);
            if (state == null)
            {
                Controller.Console.Warn($"\tInput file \"{prefix}\" not found");
                return;
            }
            Controller.State = state;
            GameRunner.instance.BlockingFastReset();
        }

        public Random GetGame1Random()
        {
            return Game1.random.Copy();
        }

        public Random CopyRandom(Random random)
        {
            return random.Copy();
        }

        public void ScreenshotLocation(GameLocation location, string file_prefix)
        {
            Color old_ambientLoght = Game1.ambientLight;
            Game1.ambientLight = Color.White;
            GameLocation curr = Game1.currentLocation;
            Game1.currentLocation = location;
            Game1.game1.takeMapScreenshot(0.25f, file_prefix, null);
            Game1.currentLocation = curr;
            Game1.ambientLight = old_ambientLoght;
        }

        public void SetTargetFPS(int fps)
        {
            // 60 fps => 166667
            double rate = 60.0 / fps;
            int ticks = (int)(166667 * rate);
            _targetElapsedTime = TimeSpan.FromTicks(ticks);
        }

        public void StartMinecart()
        {
            Game1.currentMinigame = new SMineCart(
                whichTheme: SMineCart.brownArea,
                mode: SMineCart.infiniteMode,
                GetGame1Random(),
                TASDateTime.CurrentFrame
            );
        }

        public SMineCart GetMinecart()
        {
            SMineCart cart = ((SMineCart)Game1.currentMinigame).Clone();
            cart.StartFrame = TASDateTime.CurrentFrame;
            cart.CurrentFrame = TASDateTime.CurrentFrame;
            return cart;
        }

        public void SetMinecart(SMineCart cart)
        {
            SMineCart current = cart.Clone();
            TASDateTime.CurrentFrame = cart.CurrentFrame;
            current.CurrentFrame = cart.CurrentFrame;
            Game1.currentMinigame = current;
        }

        // public void ReloadDay()
        // {
        //     if (Controller.State.LastSave != null)
        //     {
        //         Controller.State.LastSave.Load();
        //     }
        // }

        public List<ClickableItems.ClickObject> GetClickableObjects()
        {
            return ClickableItems.GetClickObjects();
        }

        public SkullCavernsSimulator GetSkullCavernsSimulator(int unpausedRngCalls, int index)
        {
            return SkullCavernsSolver.Solve(unpausedRngCalls, index);
        }

        public Item TrySpawnChestFloorItem(int menuFrames, int unpausedRandomOffset, bool tryCursor)
        {
            // stash state
            ICue old_cue = Game1.currentSong;
            Random old_random = Game1.random.Copy();
            Random sharedRandom = RandomExtensions.SharedRandom.Copy();

            int LowestMineLevel = MineShaft.lowestLevelReached;
            HashSet<int> mushroomLevelsGeneratedToday = new(MineShaft.mushroomLevelsGeneratedToday);

            // run menu frames
            int blinkTimer = Game1.player.blinkTimer;
            bool doDrip =
                Game1.isMusicContextActiveButNotPlaying()
                || Game1.getMusicTrackName().Contains("Ambient");
            for (int i = 0; i < menuFrames; i++)
            {
                if (i == 2 && tryCursor)
                {
                    Game1.random.NextDouble();
                }
                blinkTimer += 16;
                if (blinkTimer > 2200 && Game1.random.NextDouble() < 0.01)
                {
                    blinkTimer = -150;
                }
                if (doDrip)
                {
                    Game1.random.NextDouble();
                }
                RandomExtensions.Update();
            }

            // run single unpaused frame
            for (int i = 0; i < unpausedRandomOffset; i++)
            {
                Game1.random.NextDouble();
            }
            blinkTimer += 16;
            if (blinkTimer > 2200 && Game1.random.NextDouble() < 0.01)
            {
                blinkTimer = -150;
            }
            if (doDrip)
            {
                Game1.random.NextDouble();
            }
            RandomExtensions.Update();

            // build the mineshaft
            MineShaft mineShaft = new MineShaft(CurrentLocation.MineLevel + 1);
            Controller.Console.Warn(
                $"\test: b:generateContents: {Game1.random.get_Index():D4} {mineShaft.mineRandom.get_Index():D4}"
            );
            Reflector.InvokeMethod(mineShaft, "generateContents");
            Controller.Console.Warn(
                $"\test: a:generateContents: {Game1.random.get_Index():D4} {mineShaft.mineRandom.get_Index():D4}"
            );

            // advance up to the add chest call
            for (int i = 0; i < 38; i++)
            {
                blinkTimer += 16;
                if (blinkTimer > 2200 && Game1.random.NextDouble() < 0.01)
                {
                    blinkTimer = -150;
                }
                if (doDrip)
                {
                    Game1.random.NextDouble();
                }
                RandomExtensions.Update();
            }
            bool flag = false;
            if (Reflector.GetValue<MineShaft, NetBool>(mineShaft, "netIsTreasureRoom").Value)
            {
                Controller.Console.Warn(
                    $"\test: b:addLevelChests: {Game1.random.get_Index():D4} {mineShaft.mineRandom.get_Index():D4}"
                );
                flag = true;
                Reflector.InvokeMethod(mineShaft, "addLevelChests");
                Controller.Console.Warn(
                    $"\test: a:addLevelChests: {Game1.random.get_Index():D4} {mineShaft.mineRandom.get_Index():D4}"
                );
            }

            // reset the state
            Game1.currentSong = old_cue;
            Game1.random = old_random;
            RandomExtensions.SharedRandom = sharedRandom;
            MineShaft.lowestLevelReached = LowestMineLevel;
            MineShaft.mushroomLevelsGeneratedToday = mushroomLevelsGeneratedToday;
            if (flag)
            {
                return (mineShaft.Objects[new Vector2(9, 9)] as Chest).Items[0];
            }
            return null;
        }

        public MineShaft SpawnNextMineShaftWithOffset(int offset)
        {
            // stash state
            ICue old_cue = Game1.currentSong;
            Random old_random = Game1.random.Copy();
            Random sharedRandom = RandomExtensions.SharedRandom.Copy();

            int LowestMineLevel = MineShaft.lowestLevelReached;
            HashSet<int> mushroomLevelsGeneratedToday = new(MineShaft.mushroomLevelsGeneratedToday);

            // run offset frames
            int blinkTimer = Game1.player.blinkTimer;
            bool doDrip =
                Game1.isMusicContextActiveButNotPlaying()
                || Game1.getMusicTrackName().Contains("Ambient");
            for (int i = 0; i < offset; i++)
            {
                blinkTimer += 16;
                if (blinkTimer > 2200 && Game1.random.NextDouble() < 0.01)
                {
                    blinkTimer = -150;
                }
                if (doDrip)
                {
                    Game1.random.NextDouble();
                }
            }

            // build the mineshaft
            MineShaft mineShaft = new MineShaft(CurrentLocation.MineLevel + 1);
            Reflector.InvokeMethod(mineShaft, "generateContents");

            // advance up to the add chest call
            for (int i = 0; i < 38; i++)
            {
                blinkTimer += 16;
                if (blinkTimer > 2200 && Game1.random.NextDouble() < 0.01)
                {
                    blinkTimer = -150;
                }
                if (doDrip)
                {
                    Game1.random.NextDouble();
                }
            }
            Reflector.InvokeMethod(mineShaft, "addLevelChests");

            // reset the state
            Game1.currentSong = old_cue;
            Game1.random = old_random;
            RandomExtensions.SharedRandom = sharedRandom;
            MineShaft.lowestLevelReached = LowestMineLevel;
            MineShaft.mushroomLevelsGeneratedToday = mushroomLevelsGeneratedToday;
            return mineShaft;
        }

        public MineShaft SpawnMineShaft(int level)
        {
            // stash state
            ICue old_cue = Game1.currentSong;
            Random old_random = Game1.random.Copy();
            Random sharedRandom = RandomExtensions.SharedRandom.Copy();

            int LowestMineLevel = MineShaft.lowestLevelReached;
            HashSet<int> mushroomLevelsGeneratedToday = new(MineShaft.mushroomLevelsGeneratedToday);

            // build the new floor
            MineShaft mineShaft = new MineShaft(level);
            Reflector.InvokeMethod(mineShaft, "generateContents");
            int blinkTimer = Game1.player.blinkTimer;
            bool doDrip =
                Game1.isMusicContextActiveButNotPlaying()
                || Game1.getMusicTrackName().Contains("Ambient");
            for (int i = 0; i < 38; i++)
            {
                blinkTimer += 16;
                if (blinkTimer > 2200 && Game1.random.NextDouble() < 0.01)
                {
                    blinkTimer = -150;
                }
                if (doDrip)
                {
                    Game1.random.NextDouble();
                }
            }
            Reflector.InvokeMethod(mineShaft, "addLevelChests");

            // reset the state
            Game1.currentSong = old_cue;
            Game1.random = old_random;
            RandomExtensions.SharedRandom = sharedRandom;
            MineShaft.lowestLevelReached = LowestMineLevel;
            MineShaft.mushroomLevelsGeneratedToday = mushroomLevelsGeneratedToday;
            return mineShaft;
        }

        public void SetView(TASView view)
        {
            Controller.ViewController.SetView(view);
        }

        public void NextView()
        {
            Controller.ViewController.NextView();
        }

        public void ResetView()
        {
            Controller.ViewController.Reset();
        }

        public void ViewLocation(GameLocation location)
        {
            Controller.ViewController.ViewLocation(location);
        }

        public List<Vector2> GetClay()
        {
            ClayMap clayMap = new ClayMap();
            return clayMap.GetClayTiles();
        }

        public void Kill()
        {
            Process.GetCurrentProcess().Kill();
        }

        public object InspectStaticVars(int index, string key)
        {
            try
            {
                var obj = GameRunner.instance.gameInstances[index].staticVarHolder;
                return Reflector.GetValue(obj, key);
            }
            catch (Exception)
            {
                Console.PushResult($"failed to get static var {index}:{key}");
                return null;
            }
        }

        public void LoadGameByIndex(int index)
        {
            GameRunner.LoadInstance(GameRunner.instance.gameInstances[index], true);
        }
    }
}
