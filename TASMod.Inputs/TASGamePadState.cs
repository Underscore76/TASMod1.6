using System;
using System.Reflection;
using Microsoft.Xna.Framework.Input;

namespace TASMod.Inputs
{
    public class TASGamePadState
    {
        public float AnalogX;
        public float AnalogY;
        public bool DPadUp;
        public bool DPadDown;
        public bool DPadLeft;
        public bool DPadRight;
        public bool ButtonA;
        public bool ButtonB;
        public bool ButtonX;
        public bool ButtonY;
        public bool ButtonL;
        public bool ButtonR;
        public bool ButtonZL;
        public bool ButtonZR;
        public bool ButtonStart;
        public bool ButtonSelect;

        public void SetDPad(bool up, bool down, bool left, bool right)
        {
            DPadUp = up;
            DPadDown = down;
            DPadLeft = left;
            DPadRight = right;
        }

        private static ButtonState BoolToPressed(bool value)
        {
            return value ? ButtonState.Pressed : ButtonState.Released;
        }

        public TASGamePadState() { }
        public TASGamePadState(TASGamePadState other)
        {
            AnalogX = other.AnalogX;
            AnalogY = other.AnalogY;
            DPadUp = other.DPadUp;
            DPadDown = other.DPadDown;
            DPadLeft = other.DPadLeft;
            DPadRight = other.DPadRight;
            ButtonA = other.ButtonA;
            ButtonB = other.ButtonB;
            ButtonX = other.ButtonX;
            ButtonY = other.ButtonY;
            ButtonL = other.ButtonL;
            ButtonR = other.ButtonR;
            ButtonZL = other.ButtonZL;
            ButtonZR = other.ButtonZR;
            ButtonStart = other.ButtonStart;
            ButtonSelect = other.ButtonSelect;
        }

        public static TASGamePadState FromGamePadState(GamePadState state)
        {
            var cs = new TASGamePadState();
            cs.DPadUp = state.DPad.Up == ButtonState.Pressed;
            cs.DPadDown = state.DPad.Down == ButtonState.Pressed;
            cs.DPadLeft = state.DPad.Left == ButtonState.Pressed;
            cs.DPadRight = state.DPad.Right == ButtonState.Pressed;
            cs.ButtonA = state.IsButtonDown(Buttons.A);
            cs.ButtonB = state.IsButtonDown(Buttons.B);
            cs.ButtonX = state.IsButtonDown(Buttons.X);
            cs.ButtonY = state.IsButtonDown(Buttons.Y);
            cs.ButtonL = state.IsButtonDown(Buttons.LeftShoulder);
            cs.ButtonR = state.IsButtonDown(Buttons.RightShoulder);
            cs.ButtonZL = state.IsButtonDown(Buttons.LeftTrigger);
            cs.ButtonZR = state.IsButtonDown(Buttons.RightTrigger);
            cs.ButtonStart = state.IsButtonDown(Buttons.Start);
            cs.ButtonSelect = state.IsButtonDown(Buttons.Back);
            cs.AnalogX = state.ThumbSticks.Left.X;
            cs.AnalogY = state.ThumbSticks.Left.Y;
            return cs;
        }

        public GamePadState ToGamePadState()
        {
            Buttons buttons = new Buttons();
            if (ButtonA) buttons |= Buttons.A;
            if (ButtonB) buttons |= Buttons.B;
            if (ButtonX) buttons |= Buttons.X;
            if (ButtonY) buttons |= Buttons.Y;
            if (ButtonL) buttons |= Buttons.LeftShoulder;
            if (ButtonR) buttons |= Buttons.RightShoulder;
            if (ButtonZL) buttons |= Buttons.LeftTrigger;
            if (ButtonZR) buttons |= Buttons.RightTrigger;
            if (ButtonStart) buttons |= Buttons.Start;
            if (ButtonSelect) buttons |= Buttons.Back;

            var thumbSticks = new GamePadThumbSticks(
                Microsoft.Xna.Framework.Vector2.Zero,
                new Microsoft.Xna.Framework.Vector2(AnalogX, AnalogY)
            );

            return new GamePadState(
                thumbSticks,
                new GamePadTriggers(),
                new GamePadButtons(buttons),
                new GamePadDPad(BoolToPressed(DPadUp), BoolToPressed(DPadDown), BoolToPressed(DPadLeft), BoolToPressed(DPadRight))
            );
        }

        public byte[] ToBytes()
        {
            byte[] data = new byte[12];
            BitConverter.GetBytes(AnalogX).CopyTo(data, 0);
            BitConverter.GetBytes(AnalogY).CopyTo(data, 4);
            int flags = 0;
            flags |= DPadUp ? 1 : 0;
            flags |= DPadDown ? 2 : 0;
            flags |= DPadLeft ? 4 : 0;
            flags |= DPadRight ? 8 : 0;
            flags |= ButtonA ? 16 : 0;
            flags |= ButtonB ? 32 : 0;
            flags |= ButtonX ? 64 : 0;
            flags |= ButtonY ? 128 : 0;
            flags |= ButtonL ? 256 : 0;
            flags |= ButtonR ? 512 : 0;
            flags |= ButtonZL ? 1024 : 0;
            flags |= ButtonZR ? 2048 : 0;
            flags |= ButtonStart ? 4096 : 0;
            flags |= ButtonSelect ? 8192 : 0;
            BitConverter.GetBytes(flags).CopyTo(data, 8);
            return data;
        }

        public static TASGamePadState FromBytes(byte[] data)
        {
            if (data.Length != 12) throw new ArgumentException("Data length must be at least 12 bytes");
            var cs = new TASGamePadState();
            cs.AnalogX = BitConverter.ToSingle(data, 0);
            cs.AnalogY = BitConverter.ToSingle(data, 4);
            int flags = BitConverter.ToInt32(data, 8);
            cs.DPadUp = (flags & 1) != 0;
            cs.DPadDown = (flags & 2) != 0;
            cs.DPadLeft = (flags & 4) != 0;
            cs.DPadRight = (flags & 8) != 0;
            cs.ButtonA = (flags & 16) != 0;
            cs.ButtonB = (flags & 32) != 0;
            cs.ButtonX = (flags & 64) != 0;
            cs.ButtonY = (flags & 128) != 0;
            cs.ButtonL = (flags & 256) != 0;
            cs.ButtonR = (flags & 512) != 0;
            cs.ButtonZL = (flags & 1024) != 0;
            cs.ButtonZR = (flags & 2048) != 0;
            cs.ButtonStart = (flags & 4096) != 0;
            cs.ButtonSelect = (flags & 8192) != 0;
            return cs;
        }
    }
}
