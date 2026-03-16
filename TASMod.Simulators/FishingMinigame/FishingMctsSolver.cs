using System;
using System.Collections.Generic;
using System.Threading;
using TASMod.Extensions;

namespace TASMod.Simulators.FishingMinigame;

public enum FishingInputAction
{
    Release,
    Press,
}

public sealed class FishingMctsOptions
{
    public int Iterations = 1200;
    public int PlanningDepth = 32;
    public int MaxRolloutDepth = 32;
    public double ExplorationConstant = 1.41421356237;

    public double WinBonus = 10.0;
    public double LossPenalty = 10.0;
    public double TreasureCaughtBonus = 1.5;
    public double TreasureProgressWeight = 0.5;
    public double BarAlignmentPenalty = 0.025;
    public double PerfectLossPenalty = 2.0;
    public double OutOfBarPenalty = 0.08;
    public double FastOutsidePenalty = 0.04;
    public double InsideCenterPenalty = 0.01;

    public FishingInputAction FallbackAction = FishingInputAction.Release;
    public bool ReuseTree = true;

    // Optional: provide a deterministic or heuristic rollout policy.
    // If null, rollout is random.
    public Func<SGame1, FishingInputAction> RolloutActionPolicy;
}

public sealed class FishingMctsSolver
{
    public sealed class DecisionResult
    {
        public bool HasDecision;
        public FishingInputAction Action;
        public int Iterations;
        public int RootVisits;
        public int BestVisits;
        public double BestMeanValue;
        public double CurrentDistance;
        public double OneStepDistance;
        public double OneStepScore;
        public bool CurrentPerfect;
        public bool OneStepPerfect;
        public bool LostPerfectOneStep;
    }

    public sealed class FishingSearchState
    {
        public readonly SGame1 Game;
        public readonly FishingInputAction LastAction;

        public FishingSearchState(
            SGame1 game,
            FishingInputAction lastAction
        )
        {
            Game = game;
            LastAction = lastAction;
        }
    }

    private readonly FishingMctsOptions options;
    private readonly MCTS<FishingSearchState> mcts;
    private FishingInputAction? pendingAppliedAction;
    private bool rootStartedPerfect;

    public FishingMctsSolver(FishingMctsOptions options = null, Random random = null)
    {
        this.options = options ?? new FishingMctsOptions();

        mcts = new MCTS<FishingSearchState>(
            expandChildren: ExpandChildren,
            isTerminal: IsTerminal,
            evaluate: Evaluate,
            rolloutPolicy: SelectRolloutChild,
            stateEquals: SameState,
            random: random
        )
        {
            ExplorationConstant = this.options.ExplorationConstant,
            MaxTreeDepth = this.options.PlanningDepth,
            MaxRolloutDepth = this.options.MaxRolloutDepth,
            ReuseTree = this.options.ReuseTree,
        };
    }

    public void NotifyAppliedAction(FishingInputAction action)
    {
        pendingAppliedAction = action;
    }

    public FishingInputAction ChooseAction(int index, SGame1 current)
    {
        return Decide(index, current, CancellationToken.None).Action;
    }

    public DecisionResult Decide(int index, SGame1 current)
    {
        return Decide(index, current, CancellationToken.None);
    }

    public DecisionResult Decide(int index, SGame1 current, CancellationToken cancellationToken)
    {
        DecisionResult fallback = BuildFallback(current);
        if (current == null || current.bobberBar == null)
        {
            return fallback;
        }

        rootStartedPerfect = current.bobberBar.perfect;

        if (options.ReuseTree && pendingAppliedAction.HasValue)
        {
            FishingInputAction applied = pendingAppliedAction.Value;
            mcts.TryPromoteRoot(s => s.LastAction == applied);
            pendingAppliedAction = null;
        }

        FishingSearchState root = new FishingSearchState(
            game: current.Clone(),
            lastAction: options.FallbackAction
        );

        MCTS<FishingSearchState>.SearchResult result = mcts.Search(index, root, options.Iterations, cancellationToken);
        if (!result.HasBestState || result.BestState == null)
        {
            return fallback;
        }

        FishingInputAction action = result.BestState.LastAction;
        SGame1 oneStep = current.Clone();
        if (action == FishingInputAction.Press)
        {
            oneStep.Press();
        }
        else
        {
            oneStep.Release();
        }

        return new DecisionResult
        {
            HasDecision = true,
            Action = action,
            Iterations = result.Iterations,
            RootVisits = result.RootVisits,
            BestVisits = result.BestVisits,
            BestMeanValue = result.BestMeanValue,
            CurrentDistance = current.bobberBar.distanceFromCatching,
            OneStepDistance = oneStep.bobberBar != null ? oneStep.bobberBar.distanceFromCatching : 0,
            OneStepScore = Evaluate(index, new FishingSearchState(oneStep, action)),
            CurrentPerfect = current.bobberBar?.perfect ?? false,
            OneStepPerfect = oneStep.bobberBar?.perfect ?? false,
            LostPerfectOneStep = (current.bobberBar?.perfect ?? false) && !(oneStep.bobberBar?.perfect ?? false),
        };
    }

    private DecisionResult BuildFallback(SGame1 current)
    {
        double currentDistance = current?.bobberBar?.distanceFromCatching ?? 0;
        return new DecisionResult
        {
            HasDecision = false,
            Action = options.FallbackAction,
            Iterations = options.Iterations,
            RootVisits = 0,
            BestVisits = 0,
            BestMeanValue = 0,
            CurrentDistance = currentDistance,
            OneStepDistance = currentDistance,
            OneStepScore = 0,
            CurrentPerfect = current?.bobberBar?.perfect ?? false,
            OneStepPerfect = current?.bobberBar?.perfect ?? false,
            LostPerfectOneStep = false,
        };
    }

    private IEnumerable<FishingSearchState> ExpandChildren(int index, FishingSearchState state)
    {
        if (state == null || state.Game == null || state.Game.bobberBar == null)
        {
            yield break;
        }

        yield return Advance(state, FishingInputAction.Press);
        yield return Advance(state, FishingInputAction.Release);
    }

    private FishingSearchState Advance(FishingSearchState state, FishingInputAction action)
    {
        SGame1 next = state.Game.Clone();
        if (action == FishingInputAction.Press)
        {
            next.Press();
        }
        else
        {
            next.Release();
        }

        return new FishingSearchState(
            game: next,
            lastAction: action
        );
    }

    private bool IsTerminal(int index, FishingSearchState state)
    {
        if (state == null || state.Game == null || state.Game.bobberBar == null)
        {
            return true;
        }

        float d = state.Game.bobberBar.distanceFromCatching;
        return d <= 0f || d >= 1f;
    }

    private static bool SameState(FishingSearchState a, FishingSearchState b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a == null || b == null)
        {
            return false;
        }

        return SameGameState(a.Game, b.Game);
    }

    private static bool SameGameState(SGame1 a, SGame1 b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a == null || b == null)
        {
            return false;
        }

        SBobberBar barA = a.bobberBar;
        SBobberBar barB = b.bobberBar;
        if (barA == null || barB == null)
        {
            return barA == barB;
        }

        if (a.previousFrameButtonPressed != b.previousFrameButtonPressed
            || a.currentFrameButtonPressed != b.currentFrameButtonPressed)
        {
            return false;
        }

        if (a.random != null && b.random != null)
        {
            if (a.random.get_Index() != b.random.get_Index() || a.random.get_Seed() != b.random.get_Seed())
            {
                return false;
            }
        }
        else if (a.random != b.random)
        {
            return false;
        }

        return Approx(barA.distanceFromCatching, barB.distanceFromCatching)
            && Approx(barA.bobberPosition, barB.bobberPosition)
            && Approx(barA.bobberTargetPosition, barB.bobberTargetPosition)
            && Approx(barA.bobberBarPos, barB.bobberBarPos)
            && Approx(barA.bobberBarSpeed, barB.bobberBarSpeed)
            && Approx(barA.bobberSpeed, barB.bobberSpeed)
            && Approx(barA.bobberAcceleration, barB.bobberAcceleration)
            && Approx(barA.floaterSinkerAcceleration, barB.floaterSinkerAcceleration)
            && barA.bobberInBar == barB.bobberInBar
            && barA.bobberBarHeight == barB.bobberBarHeight
            && barA.motionType == barB.motionType
            && Approx(barA.difficulty, barB.difficulty)
            && barA.treasure == barB.treasure
            && barA.treasureCaught == barB.treasureCaught
            && Approx(barA.treasurePosition, barB.treasurePosition)
            && Approx(barA.treasureCatchLevel, barB.treasureCatchLevel)
            && Approx(barA.treasureScale, barB.treasureScale)
            && Approx(barA.treasureAppearTimer, barB.treasureAppearTimer)
            && barA.perfect == barB.perfect;
    }

    private static bool Approx(float a, float b)
    {
        return Math.Abs(a - b) <= 0.0001f;
    }

    private double Evaluate(int index, FishingSearchState state)
    {
        if (state == null || state.Game == null || state.Game.bobberBar == null)
        {
            return -options.LossPenalty;
        }

        SBobberBar bar = state.Game.bobberBar;
        double distance = bar.distanceFromCatching;
        double score = distance;

        if (distance >= 1f)
        {
            score += options.WinBonus;
        }
        else if (distance <= 0f)
        {
            score -= options.LossPenalty;
        }

        if (bar.treasureCaught)
        {
            score += options.TreasureCaughtBonus;
        }
        score += bar.treasureCatchLevel * options.TreasureProgressWeight;

        if (!bar.bobberInBar)
        {
            score -= options.OutOfBarPenalty;

            double fishSpeed = Math.Abs(bar.bobberSpeed + bar.floaterSinkerAcceleration);
            double normalizedFishSpeed = fishSpeed / 8.0;
            score -= normalizedFishSpeed * options.FastOutsidePenalty;
        }

        double barTop = bar.bobberBarPos - 32f;
        double barBottom = barTop + bar.bobberBarHeight;
        double fishTop = bar.bobberPosition - 16f;
        double fishBottom = bar.bobberPosition + 12f;
        double fishCenter = (fishTop + fishBottom) * 0.5;
        double barCenter = (barTop + barBottom) * 0.5;
        double halfHeight = Math.Max(1.0, bar.bobberBarHeight * 0.5);
        double outsideDistance = 0.0;
        if (fishBottom < barTop)
        {
            outsideDistance = barTop - fishBottom;
        }
        else if (fishTop > barBottom)
        {
            outsideDistance = fishTop - barBottom;
        }

        double normalizedAlignmentError = outsideDistance / halfHeight;
        score -= normalizedAlignmentError * options.BarAlignmentPenalty;

        if (bar.bobberInBar)
        {
            double normalizedCenterError = Math.Abs(fishCenter - barCenter) / halfHeight;
            score -= normalizedCenterError * options.InsideCenterPenalty;
        }

        if (rootStartedPerfect && !bar.perfect)
        {
            score -= options.PerfectLossPenalty;
        }

        return score;
    }

    private FishingSearchState SelectRolloutChild(
        int index,
        FishingSearchState state,
        IReadOnlyList<FishingSearchState> children,
        Random random
    )
    {
        if (children == null || children.Count == 0)
        {
            return state;
        }

        if (options.RolloutActionPolicy != null)
        {
            FishingInputAction desiredAction = options.RolloutActionPolicy(state.Game);
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i].LastAction == desiredAction)
                {
                    return children[i];
                }
            }
        }

        return children[random.Next(children.Count)];
    }
}
