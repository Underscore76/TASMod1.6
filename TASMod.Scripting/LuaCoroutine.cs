using System;
using NLua;
using NLua.Exceptions;

namespace TASMod.Scripting
{
    public enum LuaCoroutineStatus
    {
        Running = 0,
        Suspended = 1,
        Dead = 2
    }

    public class LuaCoroutine : IDisposable
    {
        public string Name;
        public string Description;
        public int PlayerIndex;

        public static LuaTable Coroutine;
        public static LuaFunction ResumeFunction;
        public static LuaFunction CloseFunction;
        public static LuaFunction StatusFunction;

        public Lua State;
        public LuaThread Thread = null;
        public LuaFunction Function;

        public LuaCoroutine(Lua state, LuaFunction function, int index, string name = "", string description = "")
        {
            if (Coroutine == null)
            {
                Coroutine = state.GetTable("coroutine");
            }
            State = state;
            Function = function;
            state.NewThread(Function, out Thread);
            PlayerIndex = index;
            Name = name;
            Description = description;
        }

        ~LuaCoroutine()
        {
            Dispose();
        }

        public LuaCoroutineStatus Resume()
        {
            if (ResumeFunction == null)
            {
                ResumeFunction = Coroutine["resume"] as LuaFunction;
            }
            if ((object)Thread == null)
            {
                return LuaCoroutineStatus.Dead;
            }

            object[] callArgs = new object[] { Thread, PlayerIndex };
            try
            {
                var output = ResumeFunction.Call(callArgs);
                bool success = (bool)output[0];
                if (!success)
                {
                    if (output.Length > 1)
                    {
                        ModEntry.Console.Log(
                            $"Error in coroutine '{Name}': {Constants.StripUsername((string)output[1])}",
                            StardewModdingAPI.LogLevel.Error
                        );
                    }
                    return LuaCoroutineStatus.Dead;
                }
                return Status;
            }
            catch (LuaScriptException)
            {
                return LuaCoroutineStatus.Dead;
            }
        }

        public LuaCoroutineStatus Status
        {
            get
            {
                if (StatusFunction == null)
                {
                    StatusFunction = Coroutine["status"] as LuaFunction;
                }
                string stat;
                try
                {
                    stat = (string)StatusFunction.Call(Thread)[0];
                }
                catch (LuaScriptException)
                {
                    return LuaCoroutineStatus.Dead;
                }

                switch (stat)
                {
                    case "suspended":
                        return LuaCoroutineStatus.Suspended;
                    case "dead":
                        return LuaCoroutineStatus.Dead;
                    default:
                        return LuaCoroutineStatus.Running;
                }
            }
        }

        public void Close()
        {
            if (CloseFunction == null)
            {
                CloseFunction = Coroutine["close"] as LuaFunction;
            }

            if ((object)Thread == null)
                return;

            CloseFunction.Call(Thread);
            Thread.Reset();
            Thread = null;

        }

        public void Reset()
        {
            Close();
            State.NewThread(Function, out Thread);
        }

        public void Dispose()
        {
            Close();
        }
    }
}