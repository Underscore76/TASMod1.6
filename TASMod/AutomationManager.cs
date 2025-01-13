using System;
using System.Collections.Generic;
using System.Reflection;
using TASMod.Automation;
using TASMod.Inputs;

namespace TASMod
{
    public class AutomationManager
    {
        public static AutomationManager Instance { get; private set; }
        public bool Active { get; set; } = true;
        public static Dictionary<string, IAutomatedLogic> Automation;
        public static IEnumerable<string> Names => Automation.Keys;
        public static IEnumerable<IAutomatedLogic> Items => Automation.Values;
        public static IEnumerable<KeyValuePair<string, IAutomatedLogic>> Pairs => Automation;

        public static bool ContainsKey(string logicName) => Automation.ContainsKey(logicName);
        public static IAutomatedLogic Get(string logicName)
        {
            if (Automation.ContainsKey(logicName))
                return Automation[logicName];
            return null;
        }

        public static T Get<T>(string logicName) where T : IAutomatedLogic
        {
            if (Automation.ContainsKey(logicName))
                return Automation[logicName] as T;
            return null;
        }

        public static T Get<T>() where T : IAutomatedLogic
        {
            var logicName = typeof(T).Name;
            if (Automation.ContainsKey(logicName))
                return Automation[logicName] as T;
            foreach (var v in Automation)
            {
                if (v.Value is T)
                    return v.Value as T;
            }
            return null;
        }

        public AutomationManager()
        {
            Automation = new Dictionary<string, IAutomatedLogic>(StringComparer.OrdinalIgnoreCase);
            foreach (
                var v in Reflector.GetTypesInNamespace(
                    Assembly.GetExecutingAssembly(),
                    "TASMod.Automation"
                )
            )
            {
                if (v.IsAbstract || v.BaseType != typeof(IAutomatedLogic))
                    continue;
                IAutomatedLogic logic = (IAutomatedLogic)Activator.CreateInstance(v);
                Automation.Add(logic.Name, logic);
                ModEntry.Console.Log(
                    string.Format(
                        "AutomatedLogic \"{0}\" added to logic list ({1})",
                        logic.Name,
                        logic.Active
                    ),
                    StardewModdingAPI.LogLevel.Info
                );
            }
        }

        public bool HasUpdate()
        {
            if (!Active)
            {
                return false;
            }
            foreach (IAutomatedLogic logic in Automation.Values)
            {
                if (logic.Update(out _, out _, out _))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Update()
        {
            if (!Active)
            {
                return false;
            }
            foreach (IAutomatedLogic logic in Automation.Values)
            {
                if (logic.Update(out TASKeyboardState keys, out TASMouseState mouse, out _))
                {
                    if (keys != null)
                        TASInputState.SetKeyboard(keys);

                    if (mouse != null)
                        TASInputState.SetMouse(mouse);
                    return true;
                }
            }
            return false;
        }
    }
}
