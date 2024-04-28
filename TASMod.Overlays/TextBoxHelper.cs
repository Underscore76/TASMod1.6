using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using TASMod.Console;
using TASMod.Inputs;
using TASMod.System;

namespace TASMod.Overlays
{
    public class TextBoxHelper : IOverlay
    {
        public bool WasListening;
        public bool Listening;
        public bool HasInserted;
        public override string Name => "TextBoxHelper";
        public override string Description => "live text overlay for injecting text";
        public string TempText = "";
        public TextBox LastBox = null;

        public TextBoxHelper()
        {
            Window.TextInput += Window_TextInput;
            Reset();
        }

        public override void ActiveUpdate()
        {
            WasListening = Listening;

            TextBox box = TextBoxInput.GetSelected();
            Listening = box != null && box == LastBox && !Console.IsOpen;

            if (Listening && !WasListening)
            {
                Warn("Listening for text input");
                HasInserted = false;
                TempText = box.Text;
            }
            else if (!Listening && WasListening)
            {
                Warn("Stopped listening for text input");
                TempText = "";
                HasInserted = false;
            }

            LastBox = box;
        }

        private void Window_TextInput(object sender, TextInputEventArgs e)
        {
            if (Listening && !HasInserted)
            {
                if (char.IsControl(e.Character))
                {
                    switch (e.Character)
                    {
                        case '\u0008':
                            if (TempText.Length > 0)
                                TempText = TempText.Substring(0, TempText.Length - 1);
                            return;
                        default:
                            return;
                    }
                }
                TempText += e.Character;
            }
        }

        public override void Reset()
        {
            TempText = "";
            Listening = false;
            WasListening = false;
            LastBox = null;
            HasInserted = false;
        }

        public override bool ActiveHandleInput(
            TASMouseState realMouse,
            TASKeyboardState realKeyboard
        )
        {
            if (!Listening || HasInserted)
                return false;
            if (RealInputState.KeyTriggered(Keys.Enter))
            {
                Controller.State.FrameStates[(int)TASDateTime.CurrentFrame - 1].injectText =
                    TempText;
                HasInserted = true;
            }
            else if (RealInputState.KeyTriggered(Keys.V) && TASConsole.handler.ControlKeyDown)
            {
                string pasteResult = "";
                DesktopClipboard.GetText(ref pasteResult);
                TempText += pasteResult;
            }

            return true;
        }

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            if (!Listening || HasInserted || TASDateTime.CurrentFrame != Controller.FrameCount)
                return;

            TextBox box = TextBoxInput.GetSelected();
            if (box == null)
                return;
            float scale = 2;
            Vector2 dim = Font.MeasureString(TempText) * scale;
            Vector2 pos = new Vector2(box.X, box.Y - dim.Y);

            DrawText(
                spriteBatch,
                TempText,
                pos,
                Console.textEntryColor,
                Console.backgroundEntryColor,
                scale
            );
        }
    }
}
