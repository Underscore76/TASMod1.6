using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using TASMod.Overlays;
using TASMod.System;
using static TASMod.Minigames.SMineCart;

namespace TASMod.Minigames
{
    public static class JunimoKartSimulator
    {
        public static int maxDepth = 2;
        public static ulong LastComputedFrame = 0;
        public static JunimoKartState BestSolution;
        public static JunimoKartState LastComputedState;

        public static JunimoKartState Simulate(SMineCart cart)
        {
            JunimoKartState state = new JunimoKartState(cart);
            // state can either click or release
            // the problem is this is 2^N possible states to run over N frames
            // so we need to limit the number of states we consider
            BestSolution = null;
            RecursiveSolver(state, 0, maxDepth);
            return BestSolution;
        }

        public static JunimoKartState RecursiveSolver(
            JunimoKartState state,
            int depth,
            int maxDepth
        )
        {
            if (state == null || state.Game.gameOver)
            {
                return null;
            }

            {
                // if past a fruit, fall out
                List<Fruit> fruits = state.GetFruits();
                foreach (Fruit fruit in fruits)
                {
                    if (fruit.GetBounds().Right < state.Game.player.position.X)
                    {
                        return null;
                    }
                }
            }

            // reached the end
            if (state.Game.reachedFinish)
            {
                return state;
            }

            // past max depth
            if (depth > maxDepth)
            {
                return state;
            }
            return null;
        }

        public class TrackState
        {
            public Vector2 position;
            public JunimoKartState state;
        }

        public static List<TrackState> GetTracks(JunimoKartState state)
        {
            List<TrackState> tracks = new List<TrackState>();
            // ModEntry.Console.Log("Getting grounded tracks", StardewModdingAPI.LogLevel.Info);
            List<TrackState> groundedTracks = GroundedTracks(state);
            // ModEntry.Console.Log("Getting jump tracks", StardewModdingAPI.LogLevel.Info);
            List<TrackState> jumpTracks = JumpTracks(state, 60);
            if (groundedTracks.Count == 0)
            {
                return jumpTracks;
            }
            if (jumpTracks.Count == 0)
            {
                return groundedTracks;
            }
            ModEntry.Console.Log(
                $"Merging tracks {groundedTracks.Count} {jumpTracks.Count}",
                StardewModdingAPI.LogLevel.Info
            );

            // get all jump tracks
            tracks.AddRange(jumpTracks);
            // // add jump tracks that are either not in the grounded tracks or have a higher score
            // foreach (var jumpTrack in jumpTracks)
            // {
            //     if (!groundedTracks.Select(o => o.position).Contains(jumpTrack.position))
            //     {
            //         tracks.Add(jumpTrack);
            //     }
            //     else
            //     {
            //         var groundedTrack = groundedTracks.First(o => o.position == jumpTrack.position);
            //         if (jumpTrack.state.Score > groundedTrack.state.Score)
            //         {
            //             tracks.Add(jumpTrack);
            //         }
            //     }
            // }

            // add grounded tracks that are in the jump tracks but weren't selected

            foreach (var groundedTrack in groundedTracks)
            {
                if (!tracks.Select(o => o.position).Contains(groundedTrack.position))
                {
                    tracks.Add(groundedTrack);
                }
                else
                {
                    var track = tracks.First(o => o.position == groundedTrack.position);
                    if (groundedTrack.state.Score > track.state.Score)
                    {
                        track.state = groundedTrack.state;
                    }
                }
                // if (
                //     jumpTracks.Select(o => o.position).Contains(groundedTrack.position)
                //     && !tracks.Select(o => o.position).Contains(groundedTrack.position)
                // )
                // {
                //     tracks.Add(groundedTrack);
                // }
            }
            tracks.Sort((a, b) => b.state.Score.CompareTo(a.state.Score));
            // ModEntry.Console.Log(
            //     $"Done merging tracks {tracks.Count}",
            //     StardewModdingAPI.LogLevel.Info
            // );
            return tracks;
        }

        public static List<TrackState> GroundedTracks(JunimoKartState state)
        {
            JunimoKartState groundedState = new JunimoKartState(state);
            List<TrackState> groundedTracks = new List<TrackState>();
            while (!groundedState.Game.gameOver && !groundedState.Game.reachedFinish)
            {
                groundedState.Release();
                if (!groundedState.Game.player.IsGrounded())
                    continue;

                Track track = groundedState.Game.player.GetTrack();
                if (track == null)
                {
                    track = groundedState.Game.player.GetTrack(new Vector2(0f, 2f));
                }
                if (track == null)
                {
                    continue;
                }

                if (!groundedTracks.Select(o => o.position).Contains(track.position))
                {
                    groundedTracks.Add(
                        new()
                        {
                            position = track.position,
                            state = new JunimoKartState(groundedState)
                        }
                    );
                }
            }
            return groundedTracks;
        }

        public static List<TrackState> JumpTracks(JunimoKartState state, int numSteps)
        {
            // ModEntry.Console.Log(
            //     $"Starting jump tracks {state.Game.player}",
            //     StardewModdingAPI.LogLevel.Info
            // );
            List<TrackState> jumps = new List<TrackState>();
            if (!state.Game.player.IsGrounded() || state.Game.player.GetTrack() == null)
            {
                return jumps;
            }

            JunimoKartState clickState = new JunimoKartState(state);
            Vector2 currentTrack = state.Game.player.GetTrack().position;
            // ModEntry.Console.Log(
            //     $"Starting jump tracks from {currentTrack}",
            //     StardewModdingAPI.LogLevel.Info
            // );
            for (int clicks = 1; clicks < 28; clicks++)
            {
                // ModEntry.Console.Log($"\tClicking {clicks} times", StardewModdingAPI.LogLevel.Info);
                clickState.Click();
                if (clickState.Game.gameOver || clickState.Game.reachedFinish)
                    break;
                JunimoKartState currentState = new JunimoKartState(clickState);
                var velocity = currentState.Game.player.velocity;
                float gravity = currentState.Game.player.gravity;
                if (currentState.Game.player.IsJumping())
                {
                    velocity.Y = -30f;
                    gravity = 0;
                }
                // ModEntry.Console.Log(
                //     $"\t\tStarting velocity {velocity} gravity {gravity}",
                //     StardewModdingAPI.LogLevel.Info
                // );
                var speedMultiplier = (float)
                    Reflector.GetValue(currentState.Game.player, "_speedMultiplier");
                var maxFallSpeed = currentState.Game.player.GetMaxFallSpeed();
                float time = 1f / 60f;
                var pos = currentState.Game.player.position;
                bool haveLanded = false;
                for (int step = 0; step < numSteps && !haveLanded; step++)
                {
                    gravity += time * 3000f;
                    velocity.Y += time * gravity;

                    pos.X += time * velocity.X * speedMultiplier;
                    pos.Y += time * velocity.Y;

                    if (velocity.Y > maxFallSpeed)
                    {
                        velocity.Y = maxFallSpeed;
                    }
                    // ModEntry.Console.Log(
                    //     $"\t\tStep {step} pos {pos} velocity {velocity} gravity {gravity}",
                    //     StardewModdingAPI.LogLevel.Info
                    // );

                    var tracks = currentState.Game.GetTracksForXPosition(pos.X);
                    if (tracks == null)
                        continue;
                    // ModEntry.Console.Log(
                    //     $"\tFound {tracks.Count} tracks for position {pos.X}",
                    //     StardewModdingAPI.LogLevel.Info
                    // );
                    foreach (var track in tracks)
                    {
                        if (track.CanLandHere(pos))
                        {
                            // check that this track hasn't been hit yet/not the one we're on
                            if (currentTrack != track.position)
                            {
                                bool found = false;
                                for (int i = 0; i < jumps.Count; i++)
                                {
                                    if (jumps[i].position == track.position)
                                    {
                                        found = true;
                                        if (currentState.Score > jumps[i].state.Score)
                                        {
                                            jumps[i].state = currentState.RolloutUntilGrounded(1);
                                        }
                                        break;
                                    }
                                }
                                if (!found)
                                {
                                    jumps.Add(
                                        new()
                                        {
                                            position = track.position,
                                            state = currentState.RolloutUntilGrounded(1)
                                        }
                                    );
                                }
                            }
                            haveLanded = true;
                            break;
                        }
                    }
                }
            }
            return jumps;
        }

        public static bool GetInput()
        {
            SMineCart mineCart = Game1.currentMinigame as SMineCart;
            if (mineCart == null)
            {
                return false;
            }
            if (LastComputedFrame != TASDateTime.CurrentFrame)
            {
                LastComputedState = Simulate(mineCart);
                LastComputedFrame = TASDateTime.CurrentFrame;
            }
            if (LastComputedState == null)
            {
                return false;
            }
            if (LastComputedState.Game.buttonPresses.Count == 0)
            {
                return false;
            }
            return LastComputedState.Game.buttonPresses[0];
        }
    }
}
