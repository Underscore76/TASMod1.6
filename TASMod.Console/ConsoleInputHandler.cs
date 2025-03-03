using System.Collections.Generic;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;

namespace TASMod.Console
{
    public class ConsoleInputHandler
    {
        public HashSet<Keys> SpecialKeys = new HashSet<Keys>(
            new Keys[] { Keys.LeftShift, Keys.LeftControl, Keys.LeftAlt, Keys.LeftWindows }
        );
        public Dictionary<Keys, bool> specialKeys;
        public bool LeftShiftDown => IsKeyDown(Keys.LeftShift);
        public bool ControlKeyDown => IsKeyDown(Keys.LeftControl) || IsKeyDown(Keys.LeftWindows);
        public bool AltKeyDown => IsKeyDown(Keys.LeftAlt);

        public GameWindow Window;
        public TASConsole Console;

        public ConsoleInputHandler(GameWindow window, TASConsole console)
        {
            specialKeys = new Dictionary<Keys, bool>();
            Window = window;
            Window.TextInput += Event_TextInput;
            Window.KeyDown += Event_KeyDown;
            Window.KeyUp += Event_KeyUp;
            Console = console;
        }

        ~ConsoleInputHandler()
        {
            Window.TextInput -= Event_TextInput;
            Window.KeyDown -= Event_KeyDown;
            Window.KeyUp -= Event_KeyUp;
        }

        public void Event_TextInput(object sender, TextInputEventArgs e)
        {
            // Controller.Console.Debug(
            //     $"Event_TextInput: {e.Character}:{e.Key} {char.IsControl(e.Character)}"
            // );
            if (ImGui.GetIO().WantCaptureKeyboard)
            {
                return;
            }
            if (char.IsControl(e.Character))
            {
                Console.ReceiveCommandInput(e.Character);
            }
            else
            {
                Console.ReceiveTextInput(e.Character);
            }
        }

        public void Event_KeyDown(object sender, InputKeyEventArgs e)
        {
            // Controller.Console.Debug($"Event_KeyDown: {e.Key}");
            if (!specialKeys.ContainsKey(e.Key))
            {
                specialKeys.Add(e.Key, true);
            }
            else
            {
                specialKeys[e.Key] = true;
            }
            if (ImGui.GetIO().WantCaptureKeyboard)
            {
                return;
            }

            switch (e.Key)
            {
                case Keys.C:
                    if (ControlKeyDown)
                    {
                        Console.ReceiveCommandInput('\u0003');
                        break;
                    }
                    Console.ReceiveKey(e.Key);
                    break;
                case Keys.V:
                    if (ControlKeyDown)
                    {
                        Console.ReceiveCommandInput('\u0016');
                        break;
                    }
                    Console.ReceiveKey(e.Key);
                    break;
                default:
                    Console.ReceiveKey(e.Key);
                    break;
            }
        }

        public void Event_KeyUp(object sender, InputKeyEventArgs e)
        {
            // Controller.Console.Debug($"Event_KeyUp: {e.Key}");
            if (specialKeys.ContainsKey(e.Key))
            {
                specialKeys[e.Key] = false;
            }
        }

        public bool IsKeyDown(Keys key)
        {
            return specialKeys.ContainsKey(key) && specialKeys[key];
        }
    }
}
