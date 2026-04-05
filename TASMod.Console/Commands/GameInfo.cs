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
            string.Format("for specific npc (CASE SENSITIVE): \"{0} name\"", Name),
        };
        private void WriteFriendship(int index, string name)
        {
            var friendships = InstanceCurrentPlayer.Get(index).Friendships;
            if (friendships.TryGetValue(name, out var friendship))
            {
                Write(
                    "{0}: {1} {2} hearts, {3} points", index,
                    name,
                    friendship.Points / 250,
                    friendship.Points
                );
            }
        }
        private void WriteFriendships(int index)
        {
            var friendships = InstanceCurrentPlayer.Get(index).Friendships;
            foreach (var friendship in friendships.Pairs)
            {
                Write(
                    "{0}: {1} {2} hearts, {3} points", index,
                    friendship.Key,
                    friendship.Value.Points / 250,
                    friendship.Value.Points
                );
            }
        }
        public override void Run(string[] tokens)
        {
            if (tokens.Length > 1)
            {
                Write(HelpText());
            }
            else if (tokens.Length == 1)
            {
                Write("getting friendship for npc: {0}", tokens[0]);
                if (Game1.getCharacterFromName(tokens[0], false) == null)
                {
                    Write("invalid npc name: npc {0} not found", tokens[0]);
                    return;
                }
                for (int i = 0; i < GameRunner.instance.gameInstances.Count; i++)
                {
                    WriteFriendship(i, tokens[0]);
                }
            }
            else
            {
                Write("getting friendship for all npcs");
                for (int i = 0; i < GameRunner.instance.gameInstances.Count; i++)
                {
                    WriteFriendships(i);
                }
            }
        }
    }

    public class PlayerInfo : IConsoleCommand
    {
        public override string Name => "player";
        public override string Description => "print details on player including xp/friendship/pos/etc";

        private void WritePlayerInfo(int index)
        {
            var player = InstanceCurrentPlayer.Get(index).Player;
            Write(
                "{0}: {1} on {2} at tile ({3}, {4}) [raw:({5}, {6})]",
                index,
                player.Name,
                player.currentLocation.Name,
                player.Tile.X,
                player.Tile.Y,
                player.Position.X,
                player.Position.Y
            );
            Write("\tSkills:");
            for (int i = 0; i < 5; i++)
            {
                string skillName = Farmer.getSkillNameFromIndex(i);
                Write("\t\t{0}: level {1}, xp {2}", skillName, player.GetSkillLevel(i), player.experiencePoints[i]);
            }
            Write("\tFriendships:");
            foreach (var friendship in player.friendshipData.Pairs)
            {
                Write(
                    "\t\t{0}: {1} hearts, {2} points",
                    friendship.Key,
                    friendship.Value.Points / 250,
                    friendship.Value.Points
                );
            }
        }
        public override void Run(string[] tokens)
        {
            for (int i = 0; i < GameRunner.instance.gameInstances.Count; i++)
            {
                WritePlayerInfo(i);
            }
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
                foreach (var drop in drops)
                {
                    Write("\t{0}\t({1}, {2})\t{3}", drop.Item1, drop.Item2.X, drop.Item2.Y, drop.Item3);
                }
            }
        }
    }
}
