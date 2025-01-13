using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using TASMod.Extensions;
using TASMod.Inputs;
using TASMod.Overlays;
using TASMod.Recording;
using TASMod.System;

namespace TASMod
{
    public class RecordingManager
    {
        public bool Active { get; set; } = true;
        public SaveState State { get; set; } = new SaveState();

        public bool HasUpdate()
        {
            return Active && State.FrameStates.Count > 0 && HandleStoredInput();
        }

        public bool Update()
        {
            if (!Active)
            {
                return false;
            }

            HandleTextBoxEntry();
            if (HandleStoredInput())
            {
                FrameState state = PullFrame();
                if (
                    Game1.random.get_Index() != state.randomState.index
                    || Game1.random.get_Seed() != state.randomState.seed
                )
                {
                    ModEntry.Console.Log(
                        string.Format(
                            "{0}: Game1.random: [{1}]\tFrame: {2}",
                            TASDateTime.CurrentFrame,
                            Game1.random.ToString(),
                            state.randomState
                        ),
                        StardewModdingAPI.LogLevel.Error
                    );
                }
                return true;
            }
            return false;
        }

        public TASMouseState LastFrameMouse()
        {
            if (TASDateTime.CurrentFrame == 0 || State.FrameStates.Count == 0)
            {
                return new TASMouseState();
            }
            State
                .FrameStates[(int)TASDateTime.CurrentFrame - 1]
                .toStates(out _, out TASMouseState mouse);
            return mouse;
        }

        public void PushTextFrame(string text)
        {
            var mState = LastFrameMouse();
            mState.LeftMouseClicked = false;
            mState.RightMouseClicked = false;
            State.FrameStates.Add(
                new FrameState(new KeyboardState(), mState.GetMouseState(), inject: text)
            );

            TASInputState.Active = true;
        }

        private bool HandleStoredInput()
        {
            if (State.FrameStates.IndexInRange((int)TASDateTime.CurrentFrame))
            {
                return true;
            }
            return false;
        }

        private bool HandleTextBoxEntry()
        {
            TextBox textBox = TextBoxInput.GetSelected();
            if (textBox != null)
            {
                if (State.FrameStates[(int)TASDateTime.CurrentFrame - 1].HasInjectText())
                {
                    string text = State.FrameStates[(int)TASDateTime.CurrentFrame - 1].injectText;
                    TextBoxInput.Write(textBox, text);
                    return true;
                }
            }
            return false;
        }

        public FrameState PullFrame()
        {
            State
                .FrameStates[(int)TASDateTime.CurrentFrame]
                .toStates(out TASInputState.kState, out TASInputState.mState);
            TASInputState.Active = true;
            return State.FrameStates[(int)TASDateTime.CurrentFrame];
        }

        public void PushFrame()
        {
            State.FrameStates.Add(
                new FrameState(TASInputState.GetKeyboard(), TASInputState.GetMouse())
            );
            TASInputState.Active = true;
        }
    }
}
