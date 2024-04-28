using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TASMod.Console
{
    public abstract class IConsoleAware
    {
        public TASConsole Console => Controller.Console;
        public GameWindow Window => TASConsole.handler.Window;

        public abstract string Name { get; }
        public abstract string Description { get; }
        public virtual string[] Usage
        {
            get { return null; }
        }

        public virtual string[] HelpText()
        {
            List<string> lines = new List<string> { $"{Name}: {Description}" };
            if (Usage != null)
            {
                lines.AddRange(Usage);
            }
            return lines.ToArray();
        }

        public void Write(string line)
        {
            foreach (
                var field in line.Split(
                    new char[] { '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries
                )
            )
            {
                Console.PushResult(field);
            }
        }

        public void Write(string format, params object[] args)
        {
            Write(string.Format(format, args));
        }

        public void Write(string[] lines)
        {
            foreach (var line in lines)
            {
                Write(line);
            }
        }

        public void WriteAsEntry(string line)
        {
            if (line == "")
            {
                Console.PushEntry(line);
                return;
            }
            foreach (
                var field in line.Split(
                    new char[] { '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries
                )
            )
            {
                Console.PushEntry(field);
            }
        }

        public void WriteAsEntry(string format, params object[] args)
        {
            WriteAsEntry(string.Format(format, args));
        }

        public void WriteAsEntry(string[] lines)
        {
            foreach (var line in lines)
            {
                WriteAsEntry(line);
            }
        }

        public void Trace(string trace)
        {
            Console.Trace(trace);
        }

        public void Trace(string format, params object[] args)
        {
            Console.Trace(string.Format(format, args));
        }

        public void Warn(string warning)
        {
            Console.Warn(warning);
        }

        public void Warn(string format, params object[] args)
        {
            Console.Warn(string.Format(format, args));
        }

        public void Alert(string alert)
        {
            Console.Alert(alert);
        }

        public void Alert(string format, params object[] args)
        {
            Console.Alert(string.Format(format, args));
        }

        public void Error(string error)
        {
            Console.Error(error);
        }

        public void Error(string format, params object[] args)
        {
            Console.Error(string.Format(format, args));
        }

        public void RunAlias(string alias)
        {
            string command = GetAlias(alias);
            if (command != "")
                Console.RunCommand(command);
        }

        public IEnumerable<string> GetAllAliases()
        {
            return Console.Aliases.Keys;
        }

        public string GetAlias(string alias)
        {
            if (Console.Aliases.ContainsKey(alias))
                return Console.Aliases[alias];
            return "";
        }

        public void SetAlias(string alias, string command)
        {
            if (Console.Aliases.ContainsKey(alias))
                Console.Aliases[alias] = command;
            else
                Console.Aliases.Add(alias, command);
        }
    }
}
