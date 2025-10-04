using System;
using System.Collections.Generic;
using NLua;
using NLua.Exceptions;
using TASMod.Inputs;
using TASMod.Scripting;

namespace TASMod.Recording
{
    public class FrameFunction
    {
        public string name;
        public LuaFunction function;
        public string description;

        public FrameFunction(string n, LuaFunction func, string desc = "")
        {
            name = n;
            function = func;
            this.description = desc;
        }

        public bool Call(int index)
        {
            try
            {
                var res = function.Call(index);
                if (res.Length > 0 && res[0] is bool b)
                {
                    return b;
                }
                return false;
            }
            catch (LuaScriptException e)
            {
                ModEntry.Console.Log(e.Message, StardewModdingAPI.LogLevel.Warn);
                Controller.Console.PushResult(LuaEngine.FormatError(e.Message, e.InnerException?.InnerException ?? e.InnerException));
                // default to current state on error
                return false;
            }
        }
    }

    public static class GamePadInputQueue
    {

        public static Queue<TASGamePadState>[] Queues = new Queue<TASGamePadState>[4]
        {
            new Queue<TASGamePadState>(),
            new Queue<TASGamePadState>(),
            new Queue<TASGamePadState>(),
            new Queue<TASGamePadState>()
        };

        public static FrameFunction[] FrameFunctions = new FrameFunction[4]
        {
            null,
            null,
            null,
            null
        };

        public static Dictionary<string, FrameFunction> NamedFunctions = new Dictionary<string, FrameFunction>();

        public static void Clear()
        {
            for (int i = 0; i < 4; i++)
            {
                Queues[i].Clear();
            }
            ClearFrameFunctions();
            NamedFunctions.Clear();
        }

        public static void ClearQueue(int index)
        {
            if (index < 0 || index >= 4)
            {
                return;
            }
            Queues[index].Clear();
        }

        public static void PushGamePadInput(
            int index,
            TASGamePadState state
        )
        {
            if (index < 0 || index >= 4)
            {
                return;
            }
            Queues[index].Enqueue(state);
        }

        public static bool HasAnyInput()
        {
            for (int i = 0; i < 4; i++)
            {
                if (Queues[i].Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasInput(int index)
        {
            if (index < 0 || index >= 4)
            {
                return false;
            }
            return Queues[index].Count > 0;
        }

        public static TASGamePadState GetNextInput(
            int index
        )
        {
            // have a queued input, just take it
            if (index >= 0 && index < 4 && Queues[index].Count != 0)
            {
                return Queues[index].Dequeue();
            }
            // no frame function, just return current state
            if (FrameFunctions[index] == null)
            {
                return TASInputState.gState[index];
            }

            FrameFunction func = FrameFunctions[index];
            if (!func.Call(index))
            {
                FrameFunctions[index] = null;
            }
            if (Queues[index].Count > 0)
            {
                return Queues[index].Dequeue();
            }
            return TASInputState.gState[index];
        }

        public static TASGamePadState[] GetNextInputs()
        {
            TASGamePadState[] states = new TASGamePadState[4];
            for (int i = 0; i < 4; i++)
            {
                states[i] = GetNextInput(i);
            }
            return states;
        }

        public static void RegisterFunction(string name, LuaFunction func, string description = "")
        {
            if (NamedFunctions.ContainsKey(name))
            {
                Controller.Console.PushResult($"Overriding existing function '{name}'");
            }
            NamedFunctions[name] = new FrameFunction(name, func, description);
        }

        public static void SetFrameFunction(int index, string name)
        {
            if (index < 0 || index >= 4)
            {
                Controller.Console.PushResult($"Invalid controller index {index}");
                return;
            }
            if (!NamedFunctions.ContainsKey(name))
            {
                Controller.Console.PushResult($"No such function '{name}'");
                return;
            }
            FrameFunctions[index] = new FrameFunction(name, NamedFunctions[name].function, NamedFunctions[name].description);
        }

        public static void ClearFrameFunctions()
        {
            for (int i = 0; i < 4; i++)
            {
                FrameFunctions[i] = null;
            }
        }
        public static void ClearFrameFunction(int index)
        {
            if (index < 0 || index >= 4)
            {
                return;
            }
            FrameFunctions[index] = null;
        }

        public static bool HasFrameFunction(int index)
        {
            if (index < 0 || index >= 4)
            {
                return false;
            }
            return FrameFunctions[index] != null;
        }

        public static string GetFrameFunctionName(int i)
        {
            if (i < 0 || i >= 4)
            {
                return null;
            }
            return FrameFunctions[i]?.name;
        }

        public static FrameFunction GetFrameFunction(int i)
        {
            if (i < 0 || i >= 4)
            {
                return null;
            }
            return FrameFunctions[i];
        }
    }
}