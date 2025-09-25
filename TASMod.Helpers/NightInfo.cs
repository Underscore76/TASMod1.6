using System;
using StardewValley;

namespace TASMod.Helpers
{
    public static class NightInfo
    {
        public struct Tomorrow
        {
            public string dishOfTheDay;
            public int numDishOfTheDay;
            public double dailyLuck;
            public string friend;
            public int numRequired;
        }

        public static (string itemId, int count) UpdateDishOfTheDay(Random random)
        {
            string itemId;
            do
            {
                itemId = random.Next(194, 240).ToString();
            }
            while (Utility.IsForbiddenDishOfTheDay(itemId));
            int count = random.Next(1, 4 + ((random.NextDouble() < 0.08) ? 10 : 0));
            random.NextDouble();
            return (itemId, count);
        }

        public static int GetDayOfMonthFromDay(int day)
        {
            return (day - 1) % 28 + 1;
        }

        public static Tomorrow GetTomorrow(int numExtraSteps)
        {
            int day = (int)Game1.stats.DaysPlayed;
            int seed = Utility.CreateRandomSeed(Game1.uniqueIDForThisGame / 100, day * 10 + 1, Game1.stats.StepsTaken+numExtraSteps);
            Random r = Utility.CreateRandom(seed);
            for (int k = 0; k < GetDayOfMonthFromDay(day); k++)
            {
                r.Next();
            }
            (var dish, var count) = UpdateDishOfTheDay(r);
            string friend = "";
            if(Utility.TryGetRandom(Game1.player.friendshipData, out var whichFriend, out var friendship, r))
            {
                friend = whichFriend;
            }
            int required = r.Next(10)+1;
            r.Next(); //rarecrow
            
            double dailyLuck = Math.Min(0.10000000149011612, (double)r.Next(-100, 101) / 1000.0);
            return new(){
                dishOfTheDay = dish,
                numDishOfTheDay = count,
                friend = friend,
                numRequired = required,
                dailyLuck = dailyLuck,
            };
        }
    }
}