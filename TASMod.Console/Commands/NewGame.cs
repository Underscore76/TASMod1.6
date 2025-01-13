using System;
using System.Windows.Input;
using StardewValley;
using TASMod.Recording;
using TASMod.System;

namespace TASMod.Console.Commands
{
    public class NewGame : IConsoleCommand
    {
        public override string Name => "newgame";
        public override string Description => "console menu for new save state creation";

        public LocalizedContentManager.LanguageCode DefaultLanguage = LocalizedContentManager
            .LanguageCode
            .en;
        public int DefaultSeed = 0;

        public enum Stage
        {
            Seed,
            Language,
            Prefix,
            Done
        };

        public Stage CurrentStage;
        public string Prefix;
        public LocalizedContentManager.LanguageCode Language;
        public int Seed;

        public override void Run(string[] tokens)
        {
            Seed = 0;
            Prefix = "";
            Subscribe();

            CurrentStage = Stage.Seed;
            Write("Enter Data for Field (empty -> default).");
            Write(MenuLine());
        }

        public string MenuLine()
        {
            switch (CurrentStage)
            {
                case Stage.Seed:
                    return string.Format("Enter Game Seed (default: {0}):", DefaultSeed);
                case Stage.Language:
                    return string.Format(
                        "Enter Language Code (options: [en,ja,ru,zh,pt,es,de,th,fr,ko,it,tr,hu]) (default: {0}):",
                        DefaultLanguage
                    );
                case Stage.Prefix:
                    return string.Format("Enter File Name (default: tmp_{0}):", Seed);
                case Stage.Done:
                    Unsubscribe();
                    Write("{0} | {1}", Language, Seed);
                    CreateState();
                    return string.Format("New input created: {0}", Controller.State.Prefix);
                default:
                    return "shouldnt be here...";
            }
        }

        private void CreateState()
        {
            Controller.State = new SaveState(Seed, Language);
            Controller.ResetGame = true;
            TASDateTime.setUniqueIDForThisGame((ulong)Controller.State.Seed);
            Controller.State.Prefix = Prefix;
            Controller.State.Save();
            Controller.Reset(fastAdvance: true);
        }

        public override void ReceiveInput(string input, bool writeEntry = true)
        {
            string value = input.Trim();
            switch (CurrentStage)
            {
                case Stage.Seed:
                    if (value == "")
                    {
                        Seed = DefaultSeed;
                        CurrentStage++;
                    }
                    else if (Int32.TryParse(value, out Seed))
                    {
                        CurrentStage++;
                    }
                    else
                    {
                        Write("Seed {0} cannot be cast to integer type, please try again", input);
                    }
                    break;
                case Stage.Language:
                    if (value == "")
                    {
                        Language = DefaultLanguage;
                        CurrentStage++;
                    }
                    else if (Enum.TryParse(value, out LocalizedContentManager.LanguageCode lang))
                    {
                        Language = lang;
                        CurrentStage++;
                    }
                    else
                    {
                        Write(
                            "Language {0} not valid, (options: [en,ja,ru,zh,pt,es,de,th,fr,ko,it,tr,hu]) (default: {1})",
                            value,
                            DefaultLanguage
                        );
                    }
                    break;
                case Stage.Prefix:
                    CurrentStage++;
                    if (value == "")
                        Prefix = string.Format("tmp_{0}", Seed);
                    else
                        Prefix = value;
                    break;
                default:
                    throw new Exception("shouldn't get here...");
            }
            Write(MenuLine());
        }
    }
}
