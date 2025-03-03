using System;
using System.Collections.Generic;
using System.Reflection;
using StardewValley;
using StardewValley.Menus;
using TASMod.System;

namespace TASMod.Inputs
{
    public class TextBoxInput
    {
        public static int LastWriteFrame;
        public static Dictionary<string, List<FieldInfo>> TextBoxes =
            new Dictionary<string, List<FieldInfo>>();

        public static bool SelectAndWrite<T>(T obj, string name, string text)
        {
            if (!SetSelected(obj, name, true))
                return false;
            Write(obj, name, text);
            if (!SetSelected(obj, name, false))
                return false;
            return true;
        }

        public static bool SetSelected<T>(T obj, string name, bool selected = true)
        {
            try
            {
                TextBox textBox = Reflector.GetValue<T, TextBox>(obj, name);
                textBox.Selected = selected;
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public static TextBox GetSelected()
        {
            if (Game1.activeClickableMenu == null)
                return null;
            TextBox box = (TextBox)Game1.keyboardDispatcher.Subscriber;

            return box != null && box.Selected ? box : null;
        }

        public static void Write(string text)
        {
            Write(GetSelected(), text);
        }

        public static void Write(TextBox textBox, string text)
        {
            if (textBox != null)
            {
                textBox.Text = "";
                foreach (char c in text)
                {
                    textBox.RecieveTextInput(c);
                }
            }
        }

        public static void Write<T>(T obj, string name, string text)
        {
            TextBox textBox = Reflector.GetValue<T, TextBox>(obj, name);
            Write(textBox, text);
        }

        public static string GetText<T>(T obj, string name)
        {
            TextBox textBox = Reflector.GetValue<T, TextBox>(obj, name);
            return textBox.Text;
        }

        public static void Reset()
        {
            LastWriteFrame = 0;
        }

        public static string Text
        {
            get
            {
                if (TASDateTime.CurrentFrame == 0)
                    return "";
                string text = Controller.State?.FrameStates?[(int)TASDateTime.CurrentFrame - 1].injectText ?? "";

                TextBox textBox = GetSelected();
                if (textBox != null)
                {
                    if (textBox.Text != text)
                    {
                        return textBox.Text == "" ? text : textBox.Text;
                    }
                }
                return text;
            }
            set
            {
                if (TASDateTime.CurrentFrame == 0 || Controller.State.FrameStates.Count == 0)
                    return;
                if (value != Controller.State.FrameStates[LastWriteFrame].injectText)
                {
                    LastWriteFrame = (int)TASDateTime.CurrentFrame - 1;
                    Controller.State.FrameStates[LastWriteFrame].injectText = value;
                    Write(value);
                }
            }
        }
    }
}
