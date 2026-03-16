using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Menus;
using TASMod.Inputs;
using TASMod.Simulators.FishingMinigame;

namespace TASMod.Automation
{
    public class FishingMctsAutoplay : IAutomatedLogic
    {
        public override string Name => "FishingMctsAutoplay";
        public override string Description => "Per-frame MCTS fishing autoplay for BobberBar";

        public static bool Running { get; private set; }
        public static int TotalFrames { get; private set; }
        public static int RemainingFrames { get; private set; }
        public static int AppliedFrames => Math.Max(0, TotalFrames - RemainingFrames);
        public static FishingInputAction LastAction { get; private set; } = FishingInputAction.Release;
        public static FishingMctsSolver.DecisionResult LastDecision { get; private set; }
        public static string LastError { get; private set; }
        public static bool IsPlanningInBackground => pendingDecisionTask != null && !pendingDecisionTask.IsCompleted;
        public static double LastSolveMilliseconds { get; private set; }
        public static int RunnerCount { get; private set; }

        private static readonly object Sync = new object();
        private static FishingMctsSolver[] solvers;
        private static CancellationTokenSource cancellation;
        private static Task<PlannerResult> pendingDecisionTask;

        private sealed class PlannerResult
        {
            public FishingMctsSolver.DecisionResult Decision;
            public FishingInputAction Action;
            public double ElapsedMilliseconds;
        }

        private sealed class WorkerResult
        {
            public FishingMctsSolver.DecisionResult Decision;
            public FishingInputAction Action;
            public double VoteWeight;
        }

        public FishingMctsAutoplay()
        {
            Active = false;
        }

        public override bool IsExecuting(int index)
        {
            lock (Sync)
            {
                return Running && pendingDecisionTask != null && !pendingDecisionTask.IsCompleted;
            }
        }

        public static void Start(FishingMctsOptions options, int frameCount, int runnerCount)
        {
            lock (Sync)
            {
                int clampedFrames = Math.Max(1, frameCount);
                FishingMctsOptions baseOptions = options ?? new FishingMctsOptions();
                int totalIterations = Math.Max(1, baseOptions.Iterations);
                int requestedRunners = Math.Max(1, runnerCount);
                int actualRunners = Math.Min(requestedRunners, totalIterations);

                solvers = new FishingMctsSolver[actualRunners];
                int baseIterations = totalIterations / actualRunners;
                int remainder = totalIterations % actualRunners;
                for (int i = 0; i < actualRunners; i++)
                {
                    FishingMctsOptions workerOptions = CloneOptions(baseOptions);
                    workerOptions.Iterations = baseIterations + (i < remainder ? 1 : 0);
                    solvers[i] = new FishingMctsSolver(workerOptions);
                }

                RunnerCount = actualRunners;
                TotalFrames = clampedFrames;
                RemainingFrames = clampedFrames;
                LastAction = Controller.LastFrameMouse().LeftMouseClicked ? FishingInputAction.Press : FishingInputAction.Release;
                LastDecision = null;
                LastError = null;
                LastSolveMilliseconds = 0;

                cancellation?.Cancel();
                cancellation?.Dispose();
                cancellation = new CancellationTokenSource();
                pendingDecisionTask = null;

                Running = true;

                FishingMctsAutoplay logic = AutomationManager.Get<FishingMctsAutoplay>();
                if (logic != null)
                {
                    logic.Active = true;
                }
            }
        }

        public static void Stop(string reason = null)
        {
            lock (Sync)
            {
                StopInternal(reason);
            }
        }

        private static void StopInternal(string reason)
        {
            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = null;
            pendingDecisionTask = null;

            Running = false;
            RemainingFrames = 0;
            RunnerCount = 0;
            solvers = null;
            if (!string.IsNullOrEmpty(reason))
            {
                LastError = reason;
            }

            FishingMctsAutoplay logic = AutomationManager.Get<FishingMctsAutoplay>();
            if (logic != null)
            {
                logic.Active = false;
            }
        }

        public override bool ActiveUpdate(
            int index,
            out TASKeyboardState kstate,
            out TASMouseState mstate,
            out TASGamePadState gstate
        )
        {
            kstate = null;
            gstate = null;
            mstate = new TASMouseState(Controller.LastFrameMouse(), false, false);

            lock (Sync)
            {
                if (!Running || solvers == null || solvers.Length == 0 || RemainingFrames <= 0)
                {
                    StopInternal(null);
                    return false;
                }
            }

            if (Game1.activeClickableMenu is not BobberBar)
            {
                Stop("Fishing MCTS autoplay stopped: active menu is not BobberBar.");
                return false;
            }

            try
            {
                FishingInputAction actionToApply = FishingInputAction.Release;
                bool hasNewDecision = false;
                lock (Sync)
                {
                    if (pendingDecisionTask != null && pendingDecisionTask.IsCompleted)
                    {
                        if (pendingDecisionTask.IsFaulted)
                        {
                            Exception root = pendingDecisionTask.Exception?.GetBaseException();
                            StopInternal("Fishing MCTS autoplay error: " + (root?.Message ?? "unknown"));
                            return false;
                        }

                        if (!pendingDecisionTask.IsCanceled)
                        {
                            PlannerResult result = pendingDecisionTask.Result;
                            LastDecision = result.Decision;
                            LastAction = result.Action;
                            LastSolveMilliseconds = result.ElapsedMilliseconds;
                            actionToApply = LastAction;
                            hasNewDecision = true;
                        }

                        pendingDecisionTask = null;
                    }

                    if (!hasNewDecision)
                    {
                        if (Running && solvers != null && solvers.Length > 0 && pendingDecisionTask == null && cancellation != null)
                        {
                            SGame1 snapshot = new SGame1();
                            FishingMctsSolver[] localSolvers = solvers;
                            CancellationToken token = cancellation.Token;
                            int localIndex = index;

                            pendingDecisionTask = Task.Run(() => PlanParallel(localIndex, snapshot, localSolvers, token), token);
                        }

                        return false;
                    }

                    RemainingFrames--;
                    if (RemainingFrames <= 0)
                    {
                        StopInternal(null);
                    }
                }

                bool press = actionToApply == FishingInputAction.Press;
                mstate = new TASMouseState(Controller.LastFrameMouse(), press, false);

                lock (Sync)
                {
                    if (solvers != null)
                    {
                        for (int i = 0; i < solvers.Length; i++)
                        {
                            solvers[i]?.NotifyAppliedAction(actionToApply);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Stop("Fishing MCTS autoplay error: " + ex.Message);
                return false;
            }
        }

        private static PlannerResult PlanParallel(int index, SGame1 snapshot, FishingMctsSolver[] localSolvers, CancellationToken token)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Task<WorkerResult>[] tasks = new Task<WorkerResult>[localSolvers.Length];

            for (int i = 0; i < localSolvers.Length; i++)
            {
                FishingMctsSolver workerSolver = localSolvers[i];
                tasks[i] = Task.Run(() =>
                {
                    Extensions.RandomExtensions.SetTrackingEnabledForCurrentThread(false);
                    SGame1 workerSnapshot = snapshot.Clone();
                    FishingMctsSolver.DecisionResult decision = workerSolver.Decide(index, workerSnapshot, token);
                    FishingInputAction action = decision?.Action ?? FishingInputAction.Release;
                    double voteWeight = Math.Max(1.0, decision?.BestVisits ?? 0);
                    return new WorkerResult
                    {
                        Decision = decision,
                        Action = action,
                        VoteWeight = voteWeight,
                    };
                }, token);
            }

            Task.WaitAll(tasks, token);

            double pressVotes = 0;
            double releaseVotes = 0;
            WorkerResult bestWorker = null;

            for (int i = 0; i < tasks.Length; i++)
            {
                WorkerResult worker = tasks[i].Result;
                if (worker.Action == FishingInputAction.Press)
                {
                    pressVotes += worker.VoteWeight;
                }
                else
                {
                    releaseVotes += worker.VoteWeight;
                }

                if (bestWorker == null || worker.VoteWeight > bestWorker.VoteWeight)
                {
                    bestWorker = worker;
                }
            }

            sw.Stop();
            return new PlannerResult
            {
                Decision = bestWorker?.Decision,
                Action = pressVotes >= releaseVotes ? FishingInputAction.Press : FishingInputAction.Release,
                ElapsedMilliseconds = sw.Elapsed.TotalMilliseconds,
            };
        }

        private static FishingMctsOptions CloneOptions(FishingMctsOptions source)
        {
            return new FishingMctsOptions
            {
                Iterations = source.Iterations,
                PlanningDepth = source.PlanningDepth,
                MaxRolloutDepth = source.MaxRolloutDepth,
                ExplorationConstant = source.ExplorationConstant,
                WinBonus = source.WinBonus,
                LossPenalty = source.LossPenalty,
                TreasureCaughtBonus = source.TreasureCaughtBonus,
                TreasureProgressWeight = source.TreasureProgressWeight,
                BarAlignmentPenalty = source.BarAlignmentPenalty,
                PerfectLossPenalty = source.PerfectLossPenalty,
                OutOfBarPenalty = source.OutOfBarPenalty,
                FastOutsidePenalty = source.FastOutsidePenalty,
                InsideCenterPenalty = source.InsideCenterPenalty,
                FallbackAction = source.FallbackAction,
                ReuseTree = source.ReuseTree,
                RolloutActionPolicy = source.RolloutActionPolicy,
            };
        }
    }
}
