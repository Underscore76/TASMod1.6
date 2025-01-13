using System;
using System.Collections.Generic;
using StardewValley;
using static TASMod.Minigames.SMineCart;

namespace TASMod.Minigames
{
    public static class BestFirstSearch
    {
        public static int MaxDepth = 30;

        public static void SetDepth(int depth)
        {
            MaxDepth = Math.Max(depth, 30);
        }

        public static float Score(JunimoKartState state)
        {
            float score = state.Game.score;
            score += 10f * ((state.Game.player.position.X / state.Game.tileSize) % 1);

            // we passed a fruit!
            bool needToLandFinish = false;
            int collectedFruit = state.Game._collectedFruit.Count;
            GoalIndicator indicator = (GoalIndicator)
                Reflector.GetValue(state.Game, "_goalIndicator");
            for (int i = 0; i < state.Game._entities.Count; i++)
            {
                var entity = state.Game._entities[i];
                if (entity is Fruit && entity.position.X < state.Game.player.position.X)
                {
                    score -= 1000;
                }
                if (
                    indicator != null
                    && entity is Fruit
                    && entity.position.X > indicator.position.X
                )
                {
                    needToLandFinish = true;
                }
            }

            if (
                needToLandFinish
                && state.Game.reachedFinish
                && state.Game.player.position.Y > indicator.position.Y + 2 * state.Game.tileSize
            )
            {
                score -= 3000;
            }
            return score;
        }

        public static string StateHash(JunimoKartState state)
        {
            return $"{state.Game.player.position.X}:{state.Game.player.position.Y}:{Score(state)}";
        }

        public static List<JunimoKartState> GetNeighborsUnfiltered(JunimoKartState baseState)
        {
            HashSet<string> closedList = new HashSet<string>();
            return GetNeighbors(baseState, closedList);
        }

        public static void TestNeighbors(int num_trials)
        {
            JunimoKartState.Clones = 0;
            JunimoKartState.Simulates = 0;
            var state = new JunimoKartState(Game1.currentMinigame as SMineCart);
            for (int i = 0; i < num_trials; i++)
            {
                GetNeighborsUnfiltered(state);
            }
        }

        public static void TestSimulate(int num_trials, int num_frames)
        {
            JunimoKartState.Clones = 0;
            JunimoKartState.Simulates = 0;
            for (int i = 0; i < num_trials; i++)
            {
                var state = new JunimoKartState(Game1.currentMinigame as SMineCart);
                for (int j = 0; j < num_frames; j++)
                {
                    state.Game.Simulate(false);
                }
            }
        }

        public static void TestClone(int num_trials)
        {
            JunimoKartState.Clones = 0;
            JunimoKartState.Simulates = 0;
            float x = 0;
            for (int i = 0; i < num_trials; i++)
            {
                var state = new JunimoKartState(Game1.currentMinigame as SMineCart);
                x += state.ScreenLeftBound;
            }
        }

        public static List<JunimoKartState> GetNeighbors(
            JunimoKartState baseState,
            HashSet<string> closedList
        )
        {
            List<JunimoKartState> neighbors = new List<JunimoKartState>();
            HashSet<string> visited = new HashSet<string>();
            var states = new List<JunimoKartState>();
            for (int nclick = 0; nclick < 29; nclick++)
            {
                states.Add(new JunimoKartState(baseState));
            }
            int count = states.Count;
            for (int frames = 0; frames < MaxDepth; frames++)
            {
                if (count == 0)
                    break;
                for (int id = 0; id < 29; id++)
                {
                    if (states[id] == null)
                        continue;

                    // we bounced, cutoff the search
                    if ((float)Reflector.GetValue(states[id].Game.player, "forcedJumpTime") > 0)
                    {
                        // force the rollout of the bounce state
                        string hash = StateHash(states[id]);
                        JunimoKartState lastBounceFrame = new JunimoKartState(states[id]);
                        while (states[id].Game.player.IsJumping())
                        {
                            // rollout the original state
                            states[id].Game.Simulate(false);
                            // if still jumping, want to update the last bounce frame
                            if (states[id].Game.player.IsJumping())
                            {
                                lastBounceFrame.Game.Simulate(true);
                            }
                        }
                        if (!lastBounceFrame.Game.gameOver)
                        {
                            neighbors.Add(lastBounceFrame);
                            visited.Add(hash);
                        }
                        states[id] = null;
                        count--;
                        continue;
                    }

                    // hold jump button for "id" frames
                    states[id].Game.Simulate(frames < id);
                    if (closedList.Contains(StateHash(states[id])))
                    {
                        states[id] = null;
                        count--;
                        continue;
                    }
                    if (states[id].Game.gameOver)
                    {
                        states[id] = null;
                        count--;
                        continue;
                    }

                    if (visited.Contains(StateHash(states[id])))
                    {
                        states[id] = null;
                        count--;
                        continue;
                    }
                    // only want to add nodes that are grounded
                    if (states[id].Game.player.IsGrounded())
                    {
                        neighbors.Add(new JunimoKartState(states[id]));
                        visited.Add(StateHash(states[id]));
                    }
                }
            }
            // advance all the jumping/falling states to their conclusion
            foreach (var state in states)
            {
                if (state != null)
                {
                    while (
                        !state.Game.player.IsGrounded()
                        && !state.Game.gameOver
                        && !state.Game.reachedFinish
                    )
                    {
                        state.Game.Simulate(false);
                    }
                    if (!state.Game.gameOver && !visited.Contains(StateHash(state)))
                    {
                        neighbors.Add(new JunimoKartState(state));
                        visited.Add(StateHash(state));
                    }
                }
            }
            return neighbors;
        }

        public static float MaxScore(SMineCart cart)
        {
            var maxScore = cart.score;
            var goalIndicator = (GoalIndicator)Reflector.GetValue(cart, "_goalIndicator");
            if (goalIndicator != null && !cart.reachedFinish)
            {
                maxScore += 5000;
            }
            var lastTile = (int)Reflector.GetValue(cart, "_lastTilePosition");
            var remainingTiles = (int)Reflector.GetValue(cart, "distanceToTravel") - lastTile;
            maxScore += remainingTiles * 10;

            var spawnedFruit =
                (HashSet<CollectableFruits>)Reflector.GetValue(cart, "_spawnedFruit");
            var collectedFruit =
                (HashSet<CollectableFruits>)Reflector.GetValue(cart, "_collectedFruit");
            maxScore += (spawnedFruit.Count - collectedFruit.Count) * 1000;

            for (int i = 0; i < cart._entities.Count; i++)
            {
                var entity = cart._entities[i];
                if (entity is Coin && entity.position.X > cart.player.position.X)
                {
                    maxScore += 30;
                }
            }
            return maxScore;
        }

        public static JunimoKartState Search(SMineCart cart, int max_evals = -1)
        {
            var priorityQueue = new PriorityQueue<JunimoKartState, double>();
            var closedList = new HashSet<string>();
            var maxScore = MaxScore(cart);

            JunimoKartState start = new JunimoKartState(cart);
            if (max_evals == -1)
                return start;
            priorityQueue.Enqueue(start, 0);

            var bestScore = float.MinValue;
            JunimoKartState best = null;
            JunimoKartState current;
            int n_evals = 0;
            while (priorityQueue.Count > 0)
            {
                n_evals++;
                if (max_evals > 0 && n_evals > max_evals)
                    return best;

                current = priorityQueue.Dequeue();
                closedList.Add(StateHash(current));

                // reached the target state
                if (Score(current) >= maxScore)
                {
                    return current;
                }

                var neighbors = GetNeighbors(current, closedList);
                if (neighbors == null)
                    continue;
                foreach (var neighbor in neighbors)
                {
                    if (closedList.Contains(StateHash(neighbor)))
                        continue;

                    var score = Score(neighbor);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = neighbor;
                        if (bestScore >= maxScore)
                            return best;
                    }

                    priorityQueue.Enqueue(neighbor, 1 / (score + 1e-4));
                }
            }
            GC.Collect(0, GCCollectionMode.Forced, true, true);

            return best;
        }
    }
}
