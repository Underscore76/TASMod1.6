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

        public LuaCoroutine CreateCoroutine(int playerIndex = 0)
        {
            return new LuaCoroutine(LuaEngine.LuaState, function, playerIndex, Name, Description);
        }
    }

    public static class LuaFunctionRegistry
    {
        public static List<string> FrameFunctionNames = new List<string>();
        public static Dictionary<string, NamedLuaFunction> NamedFunctions = new Dictionary<string, NamedLuaFunction>();

        public static bool ContainsKey(string name) => NamedFunctions.ContainsKey(name);
        public static bool TryGetFunction(string name, out NamedLuaFunction func) => NamedFunctions.TryGetValue(name, out func);
        public static void Clear()
        {
            FrameFunctionNames.Clear();
            NamedFunctions.Clear();
        }

        public static NamedLuaFunction GetFunctionByName(string name)
        {
            if (NamedFunctions.ContainsKey(name))
            {
                return NamedFunctions[name];
            }
            return null;
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
    }
}