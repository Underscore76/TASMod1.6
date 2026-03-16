using ImGuiNET;
using Microsoft.Xna.Framework.Audio;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using TASMod.Automation;
using TASMod.Simulators.FishingMinigame;
using TASMod.System;
using Num = System.Numerics;

namespace TASMod.Overlays.Widgets;

public static class FishingSolverWindow
{
    private sealed class FishOption
    {
        public string Id;
        public string Name;
        public string Label;
    }

    private static List<FishOption> fishOptions;
    private static int selectedFishIndex;
    private static bool createTreasure;
    private static string fishOptionsError;

    private static int workerCount = Environment.ProcessorCount;
    private static int iterationsPerWorker = 1;
    private static int cloneCount = 100;
    private static int stepCount = 1000;
    private static bool disableRandomTracking = true;

    private static int mctsIterations = 1200;
    private static int mctsRunners = Math.Max(1, Math.Min(Environment.ProcessorCount, 8));
    private static int mctsPlanningDepth = 32;
    private static int mctsRolloutDepth = 32;
    private static int mctsAutoplayFrames = 600;
    private static float mctsExplorationConstant = 1.4142f;
    private static bool mctsReuseTree = true;
    private static bool useHeuristicRollout = true;
    private static float mctsWinBonus = 10f;
    private static float mctsLossPenalty = 10f;
    private static float mctsTreasureCaughtBonus = 1.5f;
    private static float mctsTreasureProgressWeight = 0.5f;
    private static float mctsBarAlignmentPenalty = 0.025f;
    private static float mctsPerfectLossPenalty = 2f;
    private static float mctsOutOfBarPenalty = 0.08f;
    private static float mctsFastOutsidePenalty = 0.04f;
    private static float mctsInsideCenterPenalty = 0.01f;
    private static float mctsHeuristicEdgeMargin = 10f;
    private static float mctsHeuristicCenterDeadzone = 6f;

    public static void Benchmark()
    {
        ImGui.Text("Simulator Benchmark");
        ImGui.Separator();

        ImGui.InputInt("Workers", ref workerCount);
        ImGui.InputInt("Iterations / Worker", ref iterationsPerWorker);
        ImGui.InputInt("SGame1.Clone()", ref cloneCount);
        ImGui.InputInt("Press/Release Steps", ref stepCount);
        ImGui.Checkbox("Disable RNG Tracking", ref disableRandomTracking);

        workerCount = Math.Max(1, workerCount);
        iterationsPerWorker = Math.Max(1, iterationsPerWorker);
        cloneCount = Math.Max(0, cloneCount);
        stepCount = Math.Max(0, stepCount);

        if (!FishingBenchmark.IsAnyBenchmarkRunning)
        {
            if (ImGui.Button("Run Benchmark"))
            {
                FishingBenchmark.StartBenchmark(new FishingBenchmark.FishingBenchmarkRequest
                {
                    WorkerCount = workerCount,
                    IterationsPerWorker = iterationsPerWorker,
                    CloneCount = cloneCount,
                    StepCount = stepCount,
                    DisableRandomTracking = disableRandomTracking,
                });
            }
            if (ImGui.Button("Run Parallelization Test"))
            {
                FishingBenchmark.StartParallelizationTest(new FishingBenchmark.FishingBenchmarkRequest
                {
                    WorkerCount = workerCount,
                    IterationsPerWorker = iterationsPerWorker,
                    CloneCount = cloneCount,
                    StepCount = stepCount,
                    DisableRandomTracking = disableRandomTracking,
                });
            }
        }
        else
        {
            ImGui.BeginDisabled();
            ImGui.Button("Benchmark Running...");
            ImGui.EndDisabled();
        }

        if (!string.IsNullOrEmpty(FishingBenchmark.LastBenchmarkError))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Num.Vector4(1f, 0.35f, 0.35f, 1f));
            ImGui.TextWrapped(FishingBenchmark.LastBenchmarkError);
            ImGui.PopStyleColor();
        }

        FishingBenchmark.FishingBenchmarkResult result = FishingBenchmark.LastBenchmarkResult;
        if (result != null)
        {
            double cloneTotalOpsPerSecond = result.CloneCount / (result.CloneMilliseconds / 1000.0) * result.WorkerCount;
            double stepTotalOpsPerSecond = result.StepCount / (result.StepMilliseconds / 1000.0) * result.WorkerCount;
            ImGui.Separator();
            ImGui.Text($"Total: {result.TotalMilliseconds:F2} ms");
            ImGui.Text($"Ops: {result.TotalOperations} ({result.OpsPerSecond:F0} ops/s)");
            ImGui.Text($"Clone(): {result.CloneCount} in {result.CloneMilliseconds:F2} ms ({cloneTotalOpsPerSecond:F0} ops/s)");
            ImGui.Text($"Steps: {result.StepCount} in {result.StepMilliseconds:F2} ms ({stepTotalOpsPerSecond:F0} ops/s)");
        }

        FishingBenchmark.FishingParallelizationResult parResult = FishingBenchmark.LastParallelizationResult;
        if (parResult != null)
        {
            ImGui.Separator();
            ImGui.Text("Parallelization Test:");
            ImGui.Text($"Serial Total: {parResult.SerialResult.TotalMilliseconds:F2} ms");
            ImGui.Text($"Parallel Total: {parResult.ParallelResult.TotalMilliseconds:F2} ms");
            ImGui.Text($"Speedup: {parResult.SerialResult.TotalMilliseconds / (parResult.ParallelResult.TotalMilliseconds / parResult.ParallelResult.WorkerCount):F2}x");
            double serialCloneOpsPerSecond = parResult.SerialResult.CloneCount / (parResult.SerialResult.CloneMilliseconds / 1000.0);
            double parallelCloneOpsPerSecond = parResult.ParallelResult.WorkerCount * parResult.ParallelResult.CloneCount / (parResult.ParallelResult.CloneMilliseconds / 1000.0);
            ImGui.Text($"Serial Clone/s: {serialCloneOpsPerSecond:F0} ops/s");
            ImGui.Text($"Parallel Clone/s: {parallelCloneOpsPerSecond:F0} ops/s");
            ImGui.Text($"Clone Speedup: {parallelCloneOpsPerSecond / serialCloneOpsPerSecond:F2}x");
            double serialStepOpsPerSecond = parResult.SerialResult.StepCount / (parResult.SerialResult.StepMilliseconds / 1000.0);
            double parallelStepOpsPerSecond = parResult.ParallelResult.WorkerCount * parResult.ParallelResult.StepCount / (parResult.ParallelResult.StepMilliseconds / 1000.0);
            ImGui.Text($"Serial Step/s: {serialStepOpsPerSecond:F0} ops/s");
            ImGui.Text($"Parallel Step/s: {parallelStepOpsPerSecond:F0} ops/s");
            ImGui.Text($"Step Speedup: {parallelStepOpsPerSecond / serialStepOpsPerSecond:F2}x");
        }

        ImGui.Separator();
    }

    public static void CreateBobberBar()
    {
        EnsureFishOptionsLoaded();

        bool isBobberBarOpen = Game1.activeClickableMenu is BobberBar;
        ImGui.Text("Fish Generator");
        ImGui.Separator();

        if (isBobberBarOpen)
        {
            ImGui.Text("Status: BobberBar open");
        }
        else
        {
            ImGui.Text("Status: No BobberBar open");
        }

        if (!string.IsNullOrEmpty(fishOptionsError))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Num.Vector4(1f, 0.35f, 0.35f, 1f));
            ImGui.TextWrapped(fishOptionsError);
            ImGui.PopStyleColor();
        }

        if (fishOptions != null && fishOptions.Count > 0)
        {
            selectedFishIndex = Math.Clamp(selectedFishIndex, 0, fishOptions.Count - 1);
            if (ImGui.BeginCombo("Fish", fishOptions[selectedFishIndex].Label))
            {
                for (int i = 0; i < fishOptions.Count; i++)
                {
                    bool isSelected = i == selectedFishIndex;
                    if (ImGui.Selectable(fishOptions[i].Label, isSelected))
                    {
                        selectedFishIndex = i;
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.Checkbox("Treasure", ref createTreasure);

            if (ImGui.Button("Spawn BobberBar"))
            {
                try
                {
                    BobberBar.reelSound?.Stop(AudioStopOptions.Immediate);
                    BobberBar.unReelSound?.Stop(AudioStopOptions.Immediate);
                    BobberBar.reelSound = null;
                    BobberBar.unReelSound = null;

                    string fishId = fishOptions[selectedFishIndex].Id;
                    List<string> tackleIds =
                        (Game1.player.CurrentTool as StardewValley.Tools.FishingRod)
                        ?.GetTackleQualifiedItemIDs();

                    Game1.activeClickableMenu = new BobberBar(
                        fishId,
                        fishSize: 0.5f,
                        treasure: createTreasure,
                        bobbers: tackleIds,
                        setFlagOnCatch: null,
                        isBossFish: false
                    );
                    fishOptionsError = null;
                }
                catch (Exception ex)
                {
                    fishOptionsError = "Failed to spawn BobberBar: " + ex.Message;
                }
            }
        }
        ImGui.Separator();
    }

    private static void EnsureFishOptionsLoaded()
    {
        if (fishOptions != null)
        {
            return;
        }

        try
        {
            Dictionary<string, string> fishData = DataLoader.Fish(Game1.content);
            fishOptions = new List<FishOption>(fishData.Count);

            foreach (KeyValuePair<string, string> pair in fishData)
            {
                string[] fields = pair.Value.Split('/');
                string fishName = fields.Length > 0 ? fields[0] : pair.Key;
                fishOptions.Add(new FishOption
                {
                    Id = pair.Key,
                    Name = fishName,
                    Label = $"{fishName} ({pair.Key})"
                });
            }

            fishOptions.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            selectedFishIndex = 0;
            fishOptionsError = null;
        }
        catch (Exception ex)
        {
            fishOptions = new List<FishOption>();
            fishOptionsError = "Failed to load fish list: " + ex.Message;
        }
    }

    public static void Draw()
    {
        ImGui.SetNextWindowPos(new Num.Vector2(10, 420), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Num.Vector2(380, 320), ImGuiCond.FirstUseEver);

        if (!ImGui.Begin("Fishing Solver"))
        {
            ImGui.End();
            return;
        }

        CreateBobberBar();
        // Benchmark();
        ImGui.Text("MCTS Probe");

        ImGui.InputInt("MCTS Iterations", ref mctsIterations);
        ImGui.InputInt("MCTS Runners", ref mctsRunners);
        ImGui.InputInt("MCTS Planning Depth", ref mctsPlanningDepth);
        ImGui.InputInt("MCTS Rollout Depth", ref mctsRolloutDepth);
        ImGui.InputInt("MCTS Autoplay Frames", ref mctsAutoplayFrames);
        ImGui.InputFloat("MCTS Exploration C", ref mctsExplorationConstant);
        ImGui.Checkbox("Reuse Tree", ref mctsReuseTree);
        ImGui.Checkbox("Heuristic Rollout", ref useHeuristicRollout);
        ImGui.InputFloat("Heuristic Edge Margin", ref mctsHeuristicEdgeMargin);
        ImGui.InputFloat("Heuristic Deadzone", ref mctsHeuristicCenterDeadzone);
        ImGui.InputFloat("Win Bonus", ref mctsWinBonus);
        ImGui.InputFloat("Loss Penalty", ref mctsLossPenalty);
        ImGui.InputFloat("Treasure Caught Bonus", ref mctsTreasureCaughtBonus);
        ImGui.InputFloat("Treasure Progress Weight", ref mctsTreasureProgressWeight);
        ImGui.InputFloat("Bar Alignment Penalty", ref mctsBarAlignmentPenalty);
        ImGui.InputFloat("Perfect Loss Penalty", ref mctsPerfectLossPenalty);
        ImGui.InputFloat("Out Of Bar Penalty", ref mctsOutOfBarPenalty);
        ImGui.InputFloat("Fast Outside Penalty", ref mctsFastOutsidePenalty);
        ImGui.InputFloat("Inside Center Penalty", ref mctsInsideCenterPenalty);

        mctsIterations = Math.Max(1, mctsIterations);
        mctsRunners = Math.Max(1, mctsRunners);
        mctsPlanningDepth = Math.Max(1, mctsPlanningDepth);
        mctsRolloutDepth = Math.Max(1, mctsRolloutDepth);
        mctsAutoplayFrames = Math.Max(1, mctsAutoplayFrames);
        mctsExplorationConstant = Math.Max(0f, mctsExplorationConstant);
        mctsHeuristicEdgeMargin = Math.Max(0f, mctsHeuristicEdgeMargin);
        mctsHeuristicCenterDeadzone = Math.Max(0f, mctsHeuristicCenterDeadzone);
        mctsWinBonus = Math.Max(0f, mctsWinBonus);
        mctsLossPenalty = Math.Max(0f, mctsLossPenalty);
        mctsTreasureCaughtBonus = Math.Max(0f, mctsTreasureCaughtBonus);
        mctsTreasureProgressWeight = Math.Max(0f, mctsTreasureProgressWeight);
        mctsBarAlignmentPenalty = Math.Max(0f, mctsBarAlignmentPenalty);
        mctsPerfectLossPenalty = Math.Max(0f, mctsPerfectLossPenalty);
        mctsOutOfBarPenalty = Math.Max(0f, mctsOutOfBarPenalty);
        mctsFastOutsidePenalty = Math.Max(0f, mctsFastOutsidePenalty);
        mctsInsideCenterPenalty = Math.Max(0f, mctsInsideCenterPenalty);

        ImGui.Separator();
        ImGui.Text("MCTS Autoplay (Live)");

        if (!FishingMctsAutoplay.Running)
        {
            if (ImGui.Button("Start MCTS Autoplay"))
            {
                var options = new FishingMctsOptions
                {
                    Iterations = mctsIterations,
                    PlanningDepth = mctsPlanningDepth,
                    MaxRolloutDepth = mctsRolloutDepth,
                    ExplorationConstant = mctsExplorationConstant,
                    ReuseTree = mctsReuseTree,
                    WinBonus = mctsWinBonus,
                    LossPenalty = mctsLossPenalty,
                    TreasureCaughtBonus = mctsTreasureCaughtBonus,
                    TreasureProgressWeight = mctsTreasureProgressWeight,
                    BarAlignmentPenalty = mctsBarAlignmentPenalty,
                    PerfectLossPenalty = mctsPerfectLossPenalty,
                    OutOfBarPenalty = mctsOutOfBarPenalty,
                    FastOutsidePenalty = mctsFastOutsidePenalty,
                    InsideCenterPenalty = mctsInsideCenterPenalty,
                    RolloutActionPolicy = useHeuristicRollout
                        ? (Func<SGame1, FishingInputAction>)HeuristicRolloutAction
                        : null,
                };
                FishingMctsAutoplay.Start(options, mctsAutoplayFrames, mctsRunners);
            }
        }
        else
        {
            ImGui.Button("MCTS Autoplay Running...");
            ImGui.SameLine();
            if (ImGui.Button("Stop MCTS Autoplay"))
            {
                FishingMctsAutoplay.Stop("Stopped by user.");
            }
        }

        ImGui.Text($"Frames: {FishingMctsAutoplay.AppliedFrames}/{Math.Max(1, FishingMctsAutoplay.TotalFrames)}");
        ImGui.Text($"Remaining: {FishingMctsAutoplay.RemainingFrames}");
        ImGui.Text($"Last Action: {FishingMctsAutoplay.LastAction}");
        ImGui.Text($"Planner Running: {FishingMctsAutoplay.IsPlanningInBackground}");
        ImGui.Text($"Runners: {FishingMctsAutoplay.RunnerCount}");
        ImGui.Text($"Last Solve: {FishingMctsAutoplay.LastSolveMilliseconds:F2} ms");
        if (FishingMctsAutoplay.LastDecision != null)
        {
            ImGui.Text($"Autoplay Distance: {FishingMctsAutoplay.LastDecision.CurrentDistance:F4} -> {FishingMctsAutoplay.LastDecision.OneStepDistance:F4}");
            ImGui.Text($"Autoplay Mean Value: {FishingMctsAutoplay.LastDecision.BestMeanValue:F4}");
        }
        if (!string.IsNullOrEmpty(FishingMctsAutoplay.LastError))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Num.Vector4(1f, 0.35f, 0.35f, 1f));
            ImGui.TextWrapped(FishingMctsAutoplay.LastError);
            ImGui.PopStyleColor();
        }

        ImGui.End();
    }

    private static FishingInputAction HeuristicRolloutAction(SGame1 sim)
    {
        if (sim?.bobberBar == null)
        {
            return FishingInputAction.Release;
        }

        SBobberBar bar = sim.bobberBar;
        float fishTop = bar.bobberPosition - 16f;
        float fishBottom = bar.bobberPosition + 12f;
        float fishCenter = (fishTop + fishBottom) * 0.5f;
        float barTop = bar.bobberBarPos - 32f;
        float barBottom = barTop + bar.bobberBarHeight;
        float barCenter = (barTop + barBottom) * 0.5f;
        float edgeMargin = mctsHeuristicEdgeMargin;
        float centerDeadzone = mctsHeuristicCenterDeadzone;

        if (fishTop < barTop + edgeMargin)
        {
            return FishingInputAction.Press;
        }

        if (fishBottom > barBottom - edgeMargin)
        {
            return FishingInputAction.Release;
        }

        float predictedFishCenter = fishCenter + bar.bobberSpeed + bar.floaterSinkerAcceleration;
        float predictedBarCenter = barCenter + bar.bobberBarSpeed;
        float error = predictedFishCenter - predictedBarCenter;

        if (error < -centerDeadzone)
        {
            return FishingInputAction.Press;
        }

        if (error > centerDeadzone)
        {
            return FishingInputAction.Release;
        }

        return sim.previousFrameButtonPressed
            ? FishingInputAction.Press
            : FishingInputAction.Release;
    }
}
