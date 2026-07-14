using System;
using System.Collections.Generic;
using NLua;
using NLua.Exceptions;
using TASMod.Inputs;
using TASMod.Scripting;

namespace TASMod.Recording
{
    public static class GamePadInputQueue
    {
        public static Queue<TASGamePadState>[] Queues = new Queue<TASGamePadState>[4]
        {
            new Queue<TASGamePadState>(),
            new Queue<TASGamePadState>(),
            new Queue<TASGamePadState>(),
            new Queue<TASGamePadState>()
        };

        public static LuaCoroutine[] GamePadCoroutines = new LuaCoroutine[4]
        {
            null,
            null,
            null,
            null
        };

        public static void Clear()
        {
            for (int i = 0; i < 4; i++)
            {
                Queues[i].Clear();
            }
            ClearCoroutines();
        }

        public static void ClearQueue(int index)
        {
            if (index < 0 || index >= 4)
            {
                return;
            }
            Queues[index].Clear();
        }

        public static void PushInput(
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
            if (GamePadCoroutines[index] == null)
            {
                return TASInputState.gState[index];
            }

            LuaCoroutine func = GamePadCoroutines[index];
            if (func.Resume() != LuaCoroutineStatus.Suspended)
            {
                func.Dispose();
                GamePadCoroutines[index] = null;
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


        public static void SetCoroutine(int index, string name)
        {
            if (index < 0 || index >= 4)
            {
                Controller.Console.PushResult($"Invalid controller index {index}");
                return;
            }
            if (!LuaFunctionRegistry.ContainsKey(name))
            {
                Controller.Console.PushResult($"No such function '{name}'");
                return;
            }
            if (GamePadCoroutines[index] != null)
            {
                GamePadCoroutines[index].Dispose();
            }
            if (LuaFunctionRegistry.TryGetFunction(name, out NamedLuaFunction func))
            {
                GamePadCoroutines[index] = func.CreateCoroutine(index);
            }
        }

        public static void SetManualFrameFunction(int index, LuaFunction func, string name, string description = "")
        {
            if (index < 0 || index >= 4)
            {
                Controller.Console.PushResult($"Invalid controller index {index}");
                return;
            }
            if (GamePadCoroutines[index] != null)
            {
                GamePadCoroutines[index].Dispose();
            }
            GamePadCoroutines[index] = new LuaCoroutine(LuaEngine.LuaState, func, index, name, description);
        }

        public static void ClearCoroutines()
        {
            for (int i = 0; i < 4; i++)
            {
                if (GamePadCoroutines[i] != null)
                {
                    GamePadCoroutines[i].Close();
                }
                GamePadCoroutines[i] = null;
            }
        }
        public static void ClearCoroutine(int index)
        {
            if (index < 0 || index >= 4)
            {
                return;
            }
            if (GamePadCoroutines[index] != null)
            {
                GamePadCoroutines[index].Close();
            }
            GamePadCoroutines[index] = null;
        }

        public static bool HasCoroutine(int index)
        {
            if (index < 0 || index >= 4)
            {
                return false;
            }
            return GamePadCoroutines[index] != null;
        }

        public static string GetCoroutineName(int i)
        {
            if (i < 0 || i >= 4)
            {
                return null;
            }
            return GamePadCoroutines[i]?.Name;
        }

        public static LuaCoroutine GetCoroutine(int i)
        {
            if (i < 0 || i >= 4)
            {
                return null;
            }
            return GamePadCoroutines[i];
        }
    }
}