using System;
using System.Collections.Generic;
using NLua;
using NLua.Exceptions;
using TASMod.Inputs;
using TASMod.Scripting;

namespace TASMod.Recording
{
    public class KeyboardMouseInputQueue
    {
        public static Queue<(TASKeyboardState, TASMouseState)> Queue = new Queue<(TASKeyboardState, TASMouseState)>();
        public static Queue<LuaCoroutine> Coroutine = new Queue<LuaCoroutine>();

        public static void Clear()
        {
            Queue.Clear();
            while (Coroutine.Count > 0)
            {
                Coroutine.Dequeue().Close();
            }
            Coroutine = new Queue<LuaCoroutine>();
        }

        public static void PushInput(TASKeyboardState keyboardState, TASMouseState mouseState)
        {
            Queue.Enqueue((keyboardState, mouseState));
        }

        private static (TASKeyboardState, TASMouseState) GetPreviousInput()
        {
            return (TASInputState.GetTASKeyboard(), TASInputState.GetTASMouse());
        }

        public static bool HasInput()
        {
            return Queue.Count > 0;
        }
        public static bool HasCoroutine()
        {
            return Coroutine.Count > 0;
        }
        public static string GetCoroutineName()
        {
            return Coroutine.Count > 0 ? Coroutine.Peek().Name : null;
        }
        public static LuaCoroutine GetCoroutine()
        {
            return Coroutine.Count > 0 ? Coroutine.Peek() : null;
        }

        public static bool GetNextInput(out TASKeyboardState keyboardState, out TASMouseState mouseState)
        {
            keyboardState = null;
            mouseState = null;
            // have a queued input, just take it
            if (Queue.Count > 0)
            {
                var input = Queue.Dequeue();
                keyboardState = input.Item1;
                mouseState = input.Item2;
                return true;

            }
            // no frame function, just return current state
            if (Coroutine == null)
            {
                return false;
            }

            // try and resume current coroutine
            if (Coroutine.Count > 0 && Coroutine.Peek().Resume() != LuaCoroutineStatus.Suspended)
            {
                Coroutine.Dequeue().Dispose();
            }
            // did we get an input from the coroutine?
            if (Queue.Count > 0)
            {
                var input = Queue.Dequeue();
                keyboardState = input.Item1;
                mouseState = input.Item2;
                return true;
            }

            return false;
        }

        public static void PushNamedFunction(string name)
        {
            if (!LuaFunctionRegistry.ContainsKey(name))
            {
                Controller.Console.PushResult($"No such function '{name}'");
                return;
            }
            if (LuaFunctionRegistry.TryGetFunction(name, out NamedLuaFunction func))
            {
                PushFunction(func.function, func.Name, func.Description);
            }
        }

        public static void PushFunction(LuaFunction func, string name, string description = "")
        {
            Coroutine.Enqueue(new LuaCoroutine(LuaEngine.LuaState, func, 0, name, description));
        }
    }
}