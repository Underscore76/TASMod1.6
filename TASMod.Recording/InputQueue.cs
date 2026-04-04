using System;
using System.Collections.Generic;
using NLua;
using NLua.Exceptions;
using TASMod.Inputs;
using TASMod.Scripting;

namespace TASMod.Recording
{
    public class NamedLuaFunction
    {
        public string Name;
        public string Description;
        public LuaFunction function;

        public NamedLuaFunction(string name, LuaFunction func, string description = "")
        {
            Name = name;
            function = func;
            Description = description;
        }

        public LuaCoroutine CreateCoroutine(int playerIndex)
        {
            return new LuaCoroutine(LuaEngine.LuaState, function, playerIndex, Name, Description);
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

        public static LuaCoroutine[] PlayerCoroutines = new LuaCoroutine[4]
        {
            null,
            null,
            null,
            null
        };

        public static List<string> FrameFunctionNames = new List<string>();
        public static Dictionary<string, NamedLuaFunction> NamedFunctions = new Dictionary<string, NamedLuaFunction>();

        public static NamedLuaFunction GetFunctionByName(string name)
        {
            if (NamedFunctions.ContainsKey(name))
            {
                return NamedFunctions[name];
            }
            return null;
        }

        public static void Clear()
        {
            for (int i = 0; i < 4; i++)
            {
                Queues[i].Clear();
            }
            ClearPlayerCoroutines();
            NamedFunctions.Clear();
            FrameFunctionNames.Clear();
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
            if (PlayerCoroutines[index] == null)
            {
                return TASInputState.gState[index];
            }

            LuaCoroutine func = PlayerCoroutines[index];
            if (func.Resume() != LuaCoroutineStatus.Suspended)
            {
                func.Dispose();
                PlayerCoroutines[index] = null;
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
            if (!FrameFunctionNames.Contains(name))
            {
                FrameFunctionNames.Add(name);
            }
            NamedFunctions[name] = new NamedLuaFunction(name, func, description);
        }

        public static void SetPlayerCoroutine(int index, string name)
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
            if (PlayerCoroutines[index] != null)
            {
                PlayerCoroutines[index].Dispose();
            }
            PlayerCoroutines[index] = NamedFunctions[name].CreateCoroutine(index);
        }

        public static void SetManualFrameFunction(int index, LuaFunction func, string name, string description = "")
        {
            if (index < 0 || index >= 4)
            {
                Controller.Console.PushResult($"Invalid controller index {index}");
                return;
            }
            if (PlayerCoroutines[index] != null)
            {
                PlayerCoroutines[index].Dispose();
            }
            PlayerCoroutines[index] = new LuaCoroutine(LuaEngine.LuaState, func, index, name, description);
        }

        public static void ClearPlayerCoroutines()
        {
            for (int i = 0; i < 4; i++)
            {
                if (PlayerCoroutines[i] != null)
                {
                    PlayerCoroutines[i].Close();
                }
                PlayerCoroutines[i] = null;
            }
        }
        public static void ClearPlayerCoroutine(int index)
        {
            if (index < 0 || index >= 4)
            {
                return;
            }
            if (PlayerCoroutines[index] != null)
            {
                PlayerCoroutines[index].Close();
            }
            PlayerCoroutines[index] = null;
        }

        public static bool HasPlayerCoroutine(int index)
        {
            if (index < 0 || index >= 4)
            {
                return false;
            }
            return PlayerCoroutines[index] != null;
        }

        public static string GetPlayerCoroutineName(int i)
        {
            if (i < 0 || i >= 4)
            {
                return null;
            }
            return PlayerCoroutines[i]?.Name;
        }

        public static LuaCoroutine GetPlayerCoroutine(int i)
        {
            if (i < 0 || i >= 4)
            {
                return null;
            }
            return PlayerCoroutines[i];
        }
    }
}