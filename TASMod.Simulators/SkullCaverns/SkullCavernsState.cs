using System;
using System.Collections.Generic;
using StardewValley;
using TASMod.Extensions;
using TASMod.System;

namespace TASMod.Simulators.SkullCaverns
{
    public class SkullCavernsState
    {
        public SMineShaft Shaft = null;
        public List<SItem> ChestItems => Shaft == null ? null : Shaft.ChestItems;
        public bool HasChestItems => ChestItems != null && ChestItems.Count > 0;
        public Random SharedRandom;
        public Random Game1_random;
        public int PreviousMapNumber;
        public int CurrentMineLevel;
        public int blinkTimer;
        public bool doDrip;
        public int UnpauseRNGCalls;

        public int GenerateContentsIndex;
        public int TreasureRandomIndex;
        public int CurrentFrame;
        public MenuState MenuState;

        public SkullCavernsState()
        {
            SharedRandom = Extensions.RandomExtensions.SharedRandom.Copy();
            Game1_random = Game1.random.Copy();
            PreviousMapNumber = 0;
            CurrentMineLevel = 0;

            blinkTimer = Game1.player.blinkTimer;
            doDrip =
                Game1.isMusicContextActiveButNotPlaying()
                || Game1.getMusicTrackName().Contains("Ambient");

            UnpauseRNGCalls = 0;
            CurrentFrame = 0;
            MenuState = MenuState.None;
        }

        public SkullCavernsState(int unpausedRngCalls)
            : this()
        {
            UnpauseRNGCalls = unpausedRngCalls;
        }

        public SkullCavernsState(SkullCavernsState other)
        {
            SharedRandom = other.SharedRandom.Copy();
            Game1_random = other.Game1_random.Copy();
            PreviousMapNumber = other.PreviousMapNumber;
            CurrentMineLevel = other.CurrentMineLevel;

            blinkTimer = other.blinkTimer;
            doDrip = other.doDrip;

            UnpauseRNGCalls = other.UnpauseRNGCalls;
            CurrentFrame = other.CurrentFrame;
            MenuState = other.MenuState;
        }

        public bool ContainsItem(string name)
        {
            return ChestItems.Exists(i => i.Name == name);
        }

        public string UniqueID()
        {
            return $"{CurrentFrame}-{SharedRandom.get_Index():D4}-{Game1_random.get_Index():D4}";
        }

        public bool Unpause()
        {
            // advance the rng for the unpause frame
            for (int i = 0; i < UnpauseRNGCalls; i++)
            {
                Game1_random.NextDouble();
            }
            blinkTimer += 16;
            if (blinkTimer > 2200 && Game1_random.NextDouble() < 0.01)
            {
                blinkTimer = -150;
            }
            if (doDrip)
            {
                Game1_random.NextDouble();
            }
            SharedRandom.Next();
            CurrentFrame++;

            // click the ladder
            GenerateContentsIndex = Game1_random.get_Index();
            Shaft = new SMineShaft(CurrentMineLevel + 1, PreviousMapNumber, SharedRandom);
            Shaft.generateContents(Game1_random);
            if (!Shaft.isTreasureRoom)
            {
                return false;
            }
            for (int i = 0; i < 38; i++)
            {
                blinkTimer += 16;
                if (blinkTimer > 2200 && Game1_random.NextDouble() < 0.01)
                {
                    blinkTimer = -150;
                }
                if (doDrip)
                {
                    Game1_random.NextDouble();
                }
                SharedRandom.Next();
                CurrentFrame++;
            }
            TreasureRandomIndex = Game1_random.get_Index();
            Shaft.addLevelChests(Game1_random);
            return true;
        }

        public bool PausedFrame()
        {
            blinkTimer += 16;
            if (blinkTimer > 2200 && Game1_random.NextDouble() < 0.01)
            {
                blinkTimer = -150;
            }
            if (doDrip)
            {
                Game1_random.NextDouble();
            }
            SharedRandom.Next();
            CurrentFrame++;
            return true;
        }

        public bool CreateObject()
        {
            Game1_random.NextDouble(); // which obj to create
            Game1_random.NextDouble(); // object.flipped
            return true;
        }

        public bool CreateBigCraftable()
        {
            Game1_random.NextDouble(); // which obj to create
            return true;
        }
    }
}
