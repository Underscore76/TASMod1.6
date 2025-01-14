using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StardewValley;
using StardewValley.Locations;
using TASMod.Extensions;
using TASMod.Simulators.SkullCaverns;
using TASMod.System;

namespace TASMod.Simulators
{
    public class SkullCavernsSolver
    {
        [ThreadStatic]
        public static HashSet<string> FailureStates;
        public static SkullCavernsSimulator _BestSolution = null;
        public static int MaxLookAhead = 150;

        public static SkullCavernsSimulator Solve(int unpausedRngCalls, int index)
        {
            FailureStates = new HashSet<string>();
            _BestSolution = null;
            int startFrame = (int)TASDateTime.CurrentFrame;
            SkullCavernsSimulator sim = new SkullCavernsSimulator(unpausedRngCalls);
            sim.State.CurrentFrame = startFrame;
            sim.State.PreviousMapNumber = (Game1.currentLocation as MineShaft).loadedMapNumber;
            sim.State.CurrentMineLevel = (Game1.currentLocation as MineShaft).mineLevel;
            if (Game1.activeClickableMenu == null)
            {
                sim.Pause();
            }
            RecursiveHelper(sim, index, startFrame);
            return _BestSolution;
        }

        public static SkullCavernsSimulator SolveMany(
            int unpausedRngCalls,
            IEnumerable<int> indexes
        )
        {
            List<int> sortedIndexes = new List<int>(indexes);
            sortedIndexes.Sort();
            int completed = 0;
            int startFrame = (int)TASDateTime.CurrentFrame;
            var tasks = new List<Task>();
            _BestSolution = null;
            foreach (var index in sortedIndexes)
            {
                tasks.Add(
                    Task.Run(() =>
                    {
                        FailureStates = new HashSet<string>();
                        SkullCavernsSimulator sim = new SkullCavernsSimulator(unpausedRngCalls);
                        sim.State.CurrentFrame = startFrame;
                        sim.State.PreviousMapNumber = (
                            Game1.currentLocation as MineShaft
                        ).loadedMapNumber;
                        sim.State.CurrentMineLevel = (Game1.currentLocation as MineShaft).mineLevel;
                        if (Game1.activeClickableMenu == null)
                        {
                            sim.Pause();
                        }
                        RecursiveHelper(sim, index, startFrame);
                        Interlocked.Increment(ref completed);
                    })
                );
            }
            Task task = Task.WhenAll(tasks);
            try
            {
                task.Wait();
            }
            catch { }
            if (task.Status == TaskStatus.RanToCompletion)
            {
                return _BestSolution;
            }
            else if (task.Status == TaskStatus.Faulted) { }
            return null;
        }

        public static List<int> GetItemIndexes(string item, int scanLength)
        {
            List<int> result = new List<int>();
            Random r = Game1.random.Copy();
            for (int i = 0; i < scanLength; i++)
            {
                int index = r.get_Index();
                if (SMineShaft.getTreasureRoomItem(r.Copy()).Name == item)
                {
                    result.Add(index);
                }
                r.Next();
            }
            return result;
        }

        public static void SetMaxLookAhead(int lookAhead)
        {
            MaxLookAhead = lookAhead;
        }

        public static void SetBestSolution(SkullCavernsSimulator sim)
        {
            SkullCavernsSimulator init;
            SkullCavernsSimulator best = sim;
            do
            {
                init = _BestSolution;
                if (best == null || (init != null && best.CurrentFrame > init.CurrentFrame))
                    best = init;
            } while (init != Interlocked.CompareExchange(ref _BestSolution, best, init));
            ModEntry.Console.Log("New best solution found at frame " + best.CurrentFrame);
        }

        public static SkullCavernsSimulator RecursiveHelper(
            SkullCavernsSimulator sim,
            int index,
            int StartFrame
        )
        {
            // ModEntry.Console.Log(
            //     $"Depth: {depth} Frame: {sim.CurrentFrame} UniqueID: {sim.State.UniqueID()}"
            // );
            // error state fallout
            if (FailureStates.Contains(sim.State.UniqueID()))
            {
                return sim;
            }
            if (_BestSolution != null && _BestSolution.CurrentFrame < sim.CurrentFrame)
            {
                FailureStates.Add(sim.State.UniqueID());
                return sim;
            }
            if (
                sim.CurrentFrame > StartFrame + MaxLookAhead
                || sim.State.Game1_random.get_Index() > index
            )
            {
                FailureStates.Add(sim.State.UniqueID());
                return sim;
            }
            // run an unpause rollout
            {
                SkullCavernsSimulator unpause = sim.UnpauseCopy();
                if (
                    unpause.HasChestItems
                    && unpause.State.TreasureRandomIndex == index
                    && unpause.State.Shaft.Characters.Count == 0
                )
                {
                    if (_BestSolution == null || _BestSolution.CurrentFrame > unpause.CurrentFrame)
                    {
                        SetBestSolution(unpause);
                    }
                    return unpause;
                }
                else
                {
                    FailureStates.Add(sim.State.UniqueID());
                }
            }

            // craft an object
            {
                SkullCavernsSimulator craftObject = sim.Clone();
                craftObject.CreateObject();
                var craftObjectRollout = RecursiveHelper(craftObject, index, StartFrame);
                if (
                    !craftObjectRollout.HasChestItems
                    || craftObjectRollout.State.TreasureRandomIndex != index
                )
                {
                    FailureStates.Add(craftObjectRollout.State.UniqueID());
                }
            }

            // craft a big craftable
            {
                SkullCavernsSimulator craftBig = sim.Clone();
                craftBig.CreateBigCraftable();
                var craftBigRollout = RecursiveHelper(craftBig, index, StartFrame);
                if (
                    !craftBigRollout.HasChestItems
                    || craftBigRollout.State.TreasureRandomIndex != index
                )
                {
                    FailureStates.Add(craftBigRollout.State.UniqueID());
                }
            }

            // run a noop frame
            {
                SkullCavernsSimulator noop = sim.Clone();
                noop.NOOP();
                var noopRollout = RecursiveHelper(noop, index, StartFrame);
                if (!noopRollout.HasChestItems || noopRollout.State.TreasureRandomIndex != index)
                {
                    FailureStates.Add(noopRollout.State.UniqueID());
                }
            }

            return sim;
        }
    }
}
