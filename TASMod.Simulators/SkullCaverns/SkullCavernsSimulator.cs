using System.Collections.Generic;

namespace TASMod.Simulators.SkullCaverns
{
    public class SkullCavernsSimulator
    {
        public List<string> FrameData;
        public SkullCavernsState State;
        public List<SItem> ChestItems => State.ChestItems;
        public bool HasChestItems => State.HasChestItems;
        public int CurrentFrame => State.CurrentFrame;
        public bool HasFailed = false;

        public SkullCavernsSimulator()
        {
            State = new SkullCavernsState();
            FrameData = new List<string>();
        }

        public SkullCavernsSimulator(int unpausedRngCalls)
        {
            State = new SkullCavernsState(unpausedRngCalls);
            FrameData = new List<string>();
        }

        public SkullCavernsSimulator(SkullCavernsState state, List<string> frameData)
        {
            State = new SkullCavernsState(state);
            FrameData = new List<string>(frameData);
        }

        public SkullCavernsSimulator Clone()
        {
            return new SkullCavernsSimulator(State, FrameData);
        }

        public bool ContainsItem(string name)
        {
            return State.ContainsItem(name);
        }

        public bool CraftPage()
        {
            if (State.MenuState == MenuState.Crafting)
                return true;
            PushString("click_craft");
            State.PausedFrame(); // click crafting
            State.MenuState = MenuState.Crafting;
            return true;
        }

        public bool CreateObject()
        {
            CraftPage();
            PushString("mouse_object");
            State.CreateObject();
            State.PausedFrame();
            return true;
        }

        public bool CreateBigCraftable()
        {
            CraftPage();
            PushString("mouse_big");
            State.CreateBigCraftable();
            State.PausedFrame();
            return true;
        }

        public bool Pause()
        {
            PushString("pause");
            State.PausedFrame();
            return true;
        }

        public bool NOOP()
        {
            PushString("noop");
            State.PausedFrame();
            return true;
        }

        public bool Unpause()
        {
            PushString("unpause");
            return State.Unpause();
        }

        public SkullCavernsSimulator UnpauseCopy()
        {
            SkullCavernsSimulator skullCavernsSimulator = Clone();
            skullCavernsSimulator.Unpause();
            return skullCavernsSimulator;
        }

        public void PushString(string message)
        {
            FrameData.Add(string.Format("frame:{0:D8}\t{1}", State.CurrentFrame, message));
        }
    }
}
