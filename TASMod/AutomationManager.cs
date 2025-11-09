using System;
using System.Collections.Generic;
using System.Reflection;
using TASMod.Automation;
using TASMod.Inputs;
using TASMod.Networking;
using TASMod.Patches;
using TASMod.Recording;
using TASMod.System;

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
        public static string AppliedLogic = null;
        public static int AppliedFrame = -1;

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
            // ActiveInstance.Stash(0);
            bool flag = false;
            AppliedLogic = null;
            foreach (IAutomatedLogic logic in Automation.Values)
            {
                if (logic.Update(0, out _, out _, out _))
                {
                    flag = true;
                    AppliedLogic = logic.Name;
                    break;
                }
            }
            if (flag)
            {
                for (int i = 1; i <= NetworkState.NumConnections; i++)
                    flag &= GamePadInputQueue.HasInput(i) || GamePadInputQueue.HasPlayerCoroutine(i);
            }
            // ActiveInstance.Pop();
            return flag;
        }

        public bool Update()
        {
            if (!Active)
            {
                return false;
            }
            if ((int)TASDateTime.CurrentFrame == AppliedFrame)
            {
                // already applied this frame
                return AppliedLogic != null;
            }

            bool flag = false;
            AppliedLogic = null;
            AppliedFrame = -1;

            foreach (IAutomatedLogic logic in Automation.Values)
            {
                if (logic.Update(0, out TASKeyboardState keys, out TASMouseState mouse, out _))
                {
                    if (keys != null)
                        TASInputState.SetKeyboard(keys);

                    if (mouse != null)
                        TASInputState.SetMouse(mouse);
                    flag = true;
                    AppliedLogic = logic.Name;
                    AppliedFrame = (int)TASDateTime.CurrentFrame;
                    break;
                }
            }
            return flag;
        }
    }
}
