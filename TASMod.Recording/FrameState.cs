using System;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using StardewValley;
using TASMod.Extensions;
using TASMod.Inputs;

namespace TASMod.Recording
{
    [JsonObject(MemberSerialization.OptIn)]
    public class FrameState
    {
        public struct RandomState
        {
            public int index;
            public int seed;

            public RandomState(Random random)
            {
                index = random.get_Index();
                seed = random.get_Seed();
            }

            public static bool operator ==(RandomState left, RandomState right)
            {
                return left.index == right.index && left.seed == right.seed;
            }

            public static bool operator !=(RandomState left, RandomState right)
            {
                return !(left == right);
            }

            public override bool Equals(object obj)
            {
                return (obj is RandomState) && this == (RandomState)obj;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public override string ToString()
            {
                return $"seed: {seed}, index:{index}";
            }
        }

        public static Keys[] ValidKeys =
        {
            // Inventory
            Keys.D1,
            Keys.D2,
            Keys.D3,
            Keys.D4,
            Keys.D5,
            Keys.D6,
            Keys.D7,
            Keys.D8,
            Keys.D9,
            Keys.D0,
            Keys.OemMinus,
            Keys.OemPlus,
            // Movement
            Keys.W,
            Keys.A,
            Keys.S,
            Keys.D,
            Keys.Up,
            Keys.Right,
            Keys.Down,
            Keys.Left,
            // Actions
            Keys.C,
            Keys.F,
            Keys.Y,
            Keys.X,
            Keys.N,
            Keys.Space,
            // Menus
            Keys.Escape,
            Keys.E,
            Keys.I,
            Keys.M,
            Keys.J,
            // Escape Keys
            Keys.RightShift,
            Keys.R,
            Keys.Delete,
            // Misc
            Keys.LeftShift,
            Keys.Tab,
            Keys.LeftControl,
            Keys.Enter,
        };

        [JsonProperty]
        public RandomState randomState;

        [JsonProperty]
        public TASKeyboardState keyboardState;

        [JsonProperty]
        public TASMouseState mouseState;
        public string comments;

        [JsonProperty]
        public string injectText;
        public string InjectText
        {
            get => injectText;
            set => injectText = value;
        }

        public TASGamePadState[] controllers;
        [JsonProperty]
        public string controllerState
        {
            get
            {
                string res = ""
                    + Convert.ToBase64String(controllers[0].ToBytes())
                    + Convert.ToBase64String(controllers[1].ToBytes())
                    + Convert.ToBase64String(controllers[2].ToBytes())
                    + Convert.ToBase64String(controllers[3].ToBytes());
                return res;
            }
            set
            {
                byte[] data = Convert.FromBase64String(value);
                controllers = new TASGamePadState[4];
                for (int i = 0; i < 4; i++)
                {
                    byte[] cdata = data.Skip(i * 12).Take(12).ToArray();
                    controllers[i] = TASGamePadState.FromBytes(cdata);
                }
            }
        }

        public FrameState()
        {
            randomState = new RandomState(Game1.random);
            keyboardState = new TASKeyboardState();
            mouseState = new TASMouseState();
            controllers = new TASGamePadState[4]
            {
                new TASGamePadState(),
                new TASGamePadState(),
                new TASGamePadState(),
                new TASGamePadState(),
            };
            comments = "";
            injectText = "";
        }

        public FrameState(FrameState o)
        {
            randomState = new RandomState()
            {
                index = o.randomState.index,
                seed = o.randomState.seed
            };
            keyboardState = new TASKeyboardState(o.keyboardState);
            keyboardState.IntersectWith(ValidKeys);
            mouseState = new TASMouseState(o.mouseState);
            controllers = new TASGamePadState[4]
            {
                new TASGamePadState(o.controllers[0]),
                new TASGamePadState(o.controllers[1]),
                new TASGamePadState(o.controllers[2]),
                new TASGamePadState(o.controllers[3]),
            };
            comments = o.comments;
            injectText = o.injectText;
        }

        public FrameState(
            KeyboardState kstate,
            MouseState mstate,
            GamePadState[] gstates = null,
            string comm = "",
            string inject = ""
        )
        {
            // clones input states to avoid reference issues
            randomState = new RandomState(Game1.random);
            keyboardState = new TASKeyboardState(kstate);
            keyboardState.IntersectWith(ValidKeys);
            mouseState = new TASMouseState(mstate);
            controllers = new TASGamePadState[4]
            {
                gstates != null && gstates.Length > 0
                    ? TASGamePadState.FromGamePadState(gstates[0])
                    : new TASGamePadState(),
                gstates != null && gstates.Length > 1
                    ? TASGamePadState.FromGamePadState(gstates[1])
                    : new TASGamePadState(),
                gstates != null && gstates.Length > 2
                    ? TASGamePadState.FromGamePadState(gstates[2])
                    : new TASGamePadState(),
                gstates != null && gstates.Length > 3
                    ? TASGamePadState.FromGamePadState(gstates[3])
                    : new TASGamePadState(),
            };
            comments = comm;
            injectText = inject;
        }

        public FrameState(
            TASKeyboardState kstate,
            TASMouseState mstate,
            TASGamePadState[] gstates = null,
            string comm = "",
            string inject = ""
        )
        {
            // clones input states to avoid reference issues
            randomState = new RandomState(Game1.random);
            keyboardState = new TASKeyboardState(kstate);
            keyboardState.IntersectWith(ValidKeys);
            mouseState = new TASMouseState(mstate);
            controllers = new TASGamePadState[4]
            {
                gstates != null && gstates.Length > 0
                    ? new TASGamePadState(gstates[0])
                    : new TASGamePadState(),
                gstates != null && gstates.Length > 1
                    ? new TASGamePadState(gstates[1])
                    : new TASGamePadState(),
                gstates != null && gstates.Length > 2
                    ? new TASGamePadState(gstates[2])
                    : new TASGamePadState(),
                gstates != null && gstates.Length > 3
                    ? new TASGamePadState(gstates[3])
                    : new TASGamePadState(),
            };
            comments = comm;
            injectText = inject;
        }

        public bool HasInjectText()
        {
            return !string.IsNullOrEmpty(injectText);
        }

        public void SetInjectText(string text)
        {
            injectText = text;
        }

        public void ClearInjectText()
        {
            injectText = "";
        }

        public void toStates(out TASKeyboardState kstate, out TASMouseState mstate, out TASGamePadState[] gstates)
        {
            // clones out states to avoid reference issues
            kstate = new TASKeyboardState(keyboardState);
            mstate = new TASMouseState(mouseState);
            gstates = new TASGamePadState[4]
            {
                new TASGamePadState(controllers[1]),
                new TASGamePadState(controllers[1]),
                new TASGamePadState(controllers[2]),
                new TASGamePadState(controllers[3]),
            };
        }

        public void toStates(out KeyboardState kstate, out MouseState mstate, out GamePadState[] gstates)
        {
            kstate = keyboardState.GetKeyboardState();
            mstate = mouseState.GetMouseState();
            gstates = new GamePadState[4]
            {
                controllers[0].ToGamePadState(),
                controllers[1].ToGamePadState(),
                controllers[2].ToGamePadState(),
                controllers[3].ToGamePadState(),
            };
        }

        public static bool operator ==(FrameState left, FrameState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FrameState left, FrameState right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is FrameState state)
            {
                return state.keyboardState.SetEquals(keyboardState)
                    && state.mouseState.Equals(mouseState)
                    && state.randomState.Equals(randomState)
                    && state.injectText == injectText;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
