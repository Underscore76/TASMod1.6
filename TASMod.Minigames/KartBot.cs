using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static StardewValley.Minigames.MineCart;

namespace TASMod.Minigames
{
    public static class KartBot
    {
        public static int MaxDistance = 0;
        public static AStar<JunimoKartState> PathFinder;

        static KartBot()
        {
            PathFinder = new AStar<JunimoKartState>(
                GetNeighbors,
                DistanceStep,
                DistanceHeuristic,
                EqualityFunction
            );
        }

        public static List<JunimoKartState> FindPath(JunimoKartState start, int max_evals)
        {
            var end = new JunimoKartState(start);
            end.Game.buttonPresses = null;
            var path = PathFinder.Search(start, end, out _, max_evals);
            return path;
        }

        public static IEnumerable<JunimoKartState> GetNeighbors(JunimoKartState state)
        {
            // get all jump arcs from the current state
            var current = state.ClickClone();
            List<JunimoKartState> neighbors = new List<JunimoKartState>();
            while (
                current.Game.player.IsJumping()
                || (float)Reflector.GetValue(current.Game.player, "forcedJumpTime") > 0
            )
            {
                // rollout the release until grounded/dead/done
                var release = current.ReleaseClone();
                while (
                    !release.Game.player.IsGrounded()
                    && !release.Game.gameOver
                    && !release.Game.reachedFinish
                )
                {
                    release.Game.Simulate(false);
                }
                if (!release.Game.gameOver)
                {
                    neighbors.Add(release);
                }

                current.Game.Simulate(true);
            }

            // push a release frame
            if (state.Game.player.IsGrounded())
            {
                var release = state.ReleaseClone();
                if (!release.Game.gameOver)
                {
                    neighbors.Add(release);
                }
            }
            return neighbors;
        }

        public static double DistanceStep(JunimoKartState a, JunimoKartState b)
        {
            // each step is a single button click
            return Math.Abs(b.Game.player.position.X - a.Game.player.position.X);
        }

        public static double DistanceHeuristic(JunimoKartState a, JunimoKartState b)
        {
            // heuristic is the distance to the end of the level
            if (MaxDistance != 0)
                return MaxDistance * a.Game.tileSize - a.Game.player.position.X;
            return a.Game.distanceToTravel * a.Game.tileSize - a.Game.player.position.X;
        }

        public static bool EqualityFunction(JunimoKartState a, JunimoKartState b)
        {
            if (b.Game.buttonPresses == null)
            {
                return DistanceHeuristic(a, a) < 0;
            }
            if (a.Game.buttonPresses == null)
            {
                return DistanceHeuristic(b, b) < 0;
            }
            return a.Game.buttonPresses.SequenceEqual(b.Game.buttonPresses);
        }
    }
}
