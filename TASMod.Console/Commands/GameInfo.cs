using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Xna.Framework;
using StardewValley;
using TASMod.Helpers;

namespace TASMod.Console.Commands
{
    public class GetForage : IConsoleCommand
    {
        public override string Name => "forage";

        public override string Description => "list forage for locations";
        public override string[] Usage =>
            new string[]
            {
                string.Format("{0}: get forage details", Name),
                string.Format("for current loc: \"{0}\"", Name),
                string.Format("for specific loc: \"{0} locName\"", Name),
                string.Format("for all locs: \"{0} all\"", Name)
            };

        public override void Run(string[] tokens)
        {
            if (tokens.Length > 1)
            {
                Write(HelpText());
            }
            else if (tokens.Length == 1)
            {
                if (tokens[0].ToLower() == "all")
                {
                    foreach (var val in LocationHelpers.AllForage)
                    {
                        if (
                            !val.Key.Contains("Island")
                            || Utility.doesAnyFarmerHaveOrWillReceiveMail("Visited_Island")
                        )
                        {
                            WriteForLocation(val.Key, val.Value);
                        }
                    }
                }
                else
                {
                    GameLocation location = Game1.getLocationFromName(tokens[0]);
                    if (location == null)
                    {
                        Write("invalid location name: location {0} not found", tokens[0]);
                    }
                    else
                    {
                        WriteForLocation(location.Name, LocationHelpers.LocationForage(location));
                    }
                }
            }
            else
            {
                var CurrentLocation = InstanceCurrentLocation.Get(ActiveInstance.InstanceIndex);
                if (CurrentLocation.Active)
                {
                    WriteForLocation(CurrentLocation.Name, CurrentLocation.Forage);
                }
            }
        }

        private void WriteForLocation(
            string name,
            IEnumerable<KeyValuePair<Vector2, StardewValley.Object>> pairs
        )
        {
            if (pairs.Count() > 0)
            {
                Write("{0}:", name);
                int i = 0;
                foreach (var pair in pairs)
                {
                    Write("\t{0:000}\t{1}\t{2}\t{3}", i, pair.Key.X, pair.Key.Y, pair.Value.Name);
                    i++;
                }
            }
        }
    }

    public class FriendshipInfo : IConsoleCommand
    {
        public override string Name => "friendship";
        public override string Description => "print current friendship details";
        public override string[] Usage => new string[] {
            string.Format("{0}: get friendship details", Name),
            string.Format("for all npcs: \"{0}\"", Name),
            string.Format("for specific npc (case insensitive): \"{0} name\"", Name),
        };
        public override void Run(string[] tokens)
        {
        }
    }

    public class PlayerInfo : IConsoleCommand
    {
        public override string Name => "player";
        public override string Description => "print details on player including xp/friendship/pos/etc";

        public override void Run(string[] tokens)
        {
        }
    }

    public class TrashCanInfo : IConsoleCommand
    {
        public override string Name => "trashcans";
        public override string Description => "prints current trash can drops";

        public override void Run(string[] tokens)
        {
            var drops = LocationHelpers.GetTrashCans();
            if (drops.Count() > 0)
            {
                Write("Trash Can Drops:");
                int i = 0;
                foreach (var drop in drops)
                {
                    Write("\t{0:000}\t{1}\t{2}", i, drop.Item1, drop.Item2);
                    i++;
                }
            }
        }
    }
}
