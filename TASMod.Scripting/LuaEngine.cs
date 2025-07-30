using System;
using System.IO;
using System.Text;
using NLua;
using NLua.Exceptions;

namespace TASMod.Scripting
{
    public class LuaEngine
    {
        public static Lua LuaState = null;
        public static bool HasBooted = false;

        public static void Init()
        {
            if (LuaState != null)
                LuaState.Close();
            LuaState = new Lua();
            LuaState.LoadCLRPackage();
        }

        public static void SetupImports()
        {
            // import namespaces for core MonoGame types
            ModEntry.Console.Log("\tLoading System", StardewModdingAPI.LogLevel.Trace);
            LuaState.DoString(
                @"
                import ('System')
                import ('System.Collections.Generic')
            "
            );
            ModEntry.Console.Log("\tLoading MonoGame", StardewModdingAPI.LogLevel.Trace);
            LuaState.DoString(
                @"
                import ('MonoGame.Framework', 'Microsoft.Xna.Framework')
                import ('MonoGame.Framework', 'Microsoft.Xna.Framework.Graphics')
                import ('MonoGame.Framework', 'Microsoft.Xna.Framework.Input')
                import ('MonoGame.Framework', 'Microsoft.Xna.Framework.Audio')
            "
            );
            ModEntry.Console.Log("\tLoading Stardew", StardewModdingAPI.LogLevel.Trace);
            LuaState.DoString("import ('StardewValley')");
            LuaState.DoString("import ('StardewValley.Locations')");
            LuaState.DoString("import ('StardewValley.Characters')");
            LuaState.DoString("import ('StardewValley.TerrainFeatures')");
            ModEntry.Console.Log("\tLoading TASMod", StardewModdingAPI.LogLevel.Trace);
            LuaState.DoString("import ('TASMod')");
            LuaState.DoString("import ('TASMod.Helpers')");
            LuaState.DoString("import ('TASMod.Extensions')");
            LuaState.DoString("import ('TASMod.Overlays')");
            LuaState.DoString("import ('TASMod.Inputs')");
            LuaState.DoString("import ('TASMod.Patches')");
            LuaState.DoString("import ('TASMod.System')");
            LuaState.DoString("import ('TASMod.Minigames')");
            LuaState.DoString("import ('TASMod.Simulators')");
            LuaState.DoString("import ('TASMod.Views')");
            LuaState.DoString("import ('TASMod.Scripting')");
            LuaState.DoString("import ('TASMod.Simulators.SkullCaverns')");
            LuaState.DoString("import ('TASMod.Simulators.Fishing')");
        }

        public static void LoadAllFiles()
        {
            Path.Join(ModEntry.Instance.Helper.DirectoryPath, "assets", "lua", "?.lua");
            string scriptsFiles = Path.Join(Constants.ScriptsPath, "?.lua");
            string packagedFiles = Path.Join(
                ModEntry.Instance.Helper.DirectoryPath,
                "assets",
                "lua",
                "?.lua"
            );
            string path = $"package.path = package.path .. ';{packagedFiles};{scriptsFiles}'";
            path = path.Replace("\\", "\\\\");
            var bytes = Encoding.ASCII.GetBytes(path.ToCharArray());
            LuaState.DoString(bytes);
        }

        public static void RegisterEnums()
        {
            LuaState["Keys"] = Activator.CreateInstance(typeof(Microsoft.Xna.Framework.Input.Keys));
        }

        public static void RegisterStaticClasses()
        {
            LuaState.RegisterFunction("RunCS", typeof(CSInterpreter).GetMethod("Eval"));
            LuaState.RegisterFunction(
                "GetValue",
                typeof(Reflector).GetMethod("GetDynamicCastField")
            );
        }

        public static string Reload()
        {
            try
            {
                ModEntry.Console.Log("Starting reload", StardewModdingAPI.LogLevel.Trace);
                Init();
                ModEntry.Console.Log("Starting import", StardewModdingAPI.LogLevel.Trace);
                SetupImports();
                ModEntry.Console.Log("Starting load", StardewModdingAPI.LogLevel.Trace);
                LoadAllFiles();
                ModEntry.Console.Log("Starting register", StardewModdingAPI.LogLevel.Trace);
                RegisterEnums();
                RegisterStaticClasses();

                ModEntry.Console.Log("Starting script interface", StardewModdingAPI.LogLevel.Trace);
                LuaState["interface"] = new ScriptInterface();

                ModEntry.Console.Log("Running prelaunch", StardewModdingAPI.LogLevel.Trace);
                LuaState.DoString(@"require('prelaunch')");
                LuaState.DoString(@"luanet.load_assembly('Stardew Valley')");
                // override System.Object since it gets confusing very fast
                LuaState.DoString(@"SObject=luanet.import_type('StardewValley.Object')");

                return "";
            }
            catch (LuaScriptException e)
            {
                string err = e.Message;
                if (e.InnerException != null)
                    err += "\n\t" + e.InnerException.Message;
                return err;
            }
        }

        public static string RunString(string command)
        {
            try
            {
                string message = "";
                switch (command)
                {
                    // NOTE: I was freezing on reload, might be a mac exclusive issue...
                    case "reload":
                        message = Reload();
                        break;
                    default:
                        object[] results = LuaState.DoString(Encoding.ASCII.GetBytes(command));
                        foreach (var r in results)
                        {
                            if (r == null)
                                message += "null\t";
                            else
                                message += string.Format("{0}\t", r.ToString());
                        }
                        break;
                }
                return message;
            }
            catch (LuaScriptException e)
            {
                ModEntry.Console.Log(e.Message, StardewModdingAPI.LogLevel.Warn);
                return FormatError(e.Message, e.InnerException?.InnerException ?? e.InnerException);
            }
            catch (TypeInitializationException e)
            {
                return FormatError(e.Message, e.InnerException);
            }
            catch (Exception e)
            {
                return FormatError(e.Message, e.InnerException);
            }
        }

        public static void Boot()
        {
            if (HasBooted)
                return;

            RunString("require('boot')");
            HasBooted = true;
        }

        public static string FormatError(string message, Exception innerException)
        {
            string err = message;
            if (innerException == null)
                return err;
            string[] items = innerException.Message.Split(" ");
            string curr = "";
            foreach (var item in items)
            {
                if (curr.Length > 65)
                {
                    err += "\n\t" + curr;
                    curr = "";
                }
                curr += item + " ";
            }
            err += "\n\t" + curr;
            return err;
        }
    }
}
