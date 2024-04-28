using System;
using System.Collections.Generic;
using System.Reflection;
using TASMod.Automation;
using TASMod.Inputs;

namespace TASMod
{
    public class AutomationManager
    {
        public bool Active { get; set; } = true;
        public Dictionary<string, IAutomatedLogic> Automation;
        public IEnumerable<string> Keys => Automation.Keys;
        public IEnumerable<IAutomatedLogic> Values => Automation.Values;

        public bool ContainsKey(string logicName) => Automation.ContainsKey(logicName);

        public Dictionary<string, IAutomatedLogic>.Enumerator GetEnumerator() =>
            Automation.GetEnumerator();

        public IAutomatedLogic this[string logicName]
        {
            get
            {
                if (Automation.ContainsKey(logicName))
                    return Automation[logicName];
                return null;
            }
        }

        public AutomationManager()
        {
            Automation = new Dictionary<string, IAutomatedLogic>();
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
