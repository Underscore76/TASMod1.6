using System.Reflection;
using Microsoft.Xna.Framework.Input;

namespace TASMod.Inputs
{
    public class TASGamePadState
    {
        public GamePadButtons mButtons;
        public GamePadDPad mDPad;
        public GamePadThumbSticks mThumbSticks;
        public GamePadTriggers mTriggers;

        public TASGamePadState()
        {
            mButtons = new GamePadButtons();
            mDPad = new GamePadDPad();
            mThumbSticks = new GamePadThumbSticks();
            mTriggers = new GamePadTriggers();
        }

        public GamePadState GetGamePadState()
        {
            GamePadState state = new GamePadState(mThumbSticks, mTriggers, mButtons, mDPad);
            if (state.GetHashCode() == default(GamePadState).GetHashCode())
            {
                return default;
            }
            return state;
        }

        public void ToggleButton(Buttons button)
        {
            mButtons = new GamePadButtons((Buttons)mButtons.GetHashCode() ^ button);
        }

        public void AddButton(Buttons button)
        {
            mButtons = new GamePadButtons((Buttons)mButtons.GetHashCode() | button);
        }

        public void RemoveButton(Buttons button)
        {
            mButtons = new GamePadButtons((Buttons)mButtons.GetHashCode() & ~button);
        }
    }
}
