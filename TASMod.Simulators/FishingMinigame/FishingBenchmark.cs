using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TASMod.Extensions;

namespace TASMod.Simulators.FishingMinigame;

public static class FishingBenchmark
{
    public sealed class FishingBenchmarkRequest
    {
        public int WorkerCount { get; set; } = Environment.ProcessorCount;
        public int IterationsPerWorker { get; set; } = 1;
        public int CloneCount { get; set; }
        public int StepCount { get; set; }
        public bool DisableRandomTracking { get; set; } = true;
    }

    public sealed class FishingBenchmarkResult
    {
        public int WorkerCount { get; init; }
        public int IterationsPerWorker { get; init; }
        public long CloneCount { get; init; }
        public long StepCount { get; init; }
        public long TotalOperations { get; init; }
        public double TotalMilliseconds { get; init; }
        public double CloneMilliseconds { get; init; }
        public double StepMilliseconds { get; init; }

        public double OpsPerSecond => TotalMilliseconds <= 0
            ? 0
            : TotalOperations / (TotalMilliseconds / 1000.0);
    }

    public sealed class FishingParallelizationResult
    {
        public FishingBenchmarkResult SerialResult { get; init; }
        public FishingBenchmarkResult ParallelResult { get; init; }
    }

    public static Task<FishingBenchmarkResult> BenchmarkTask { get; private set; }
    public static FishingBenchmarkResult LastBenchmarkResult { get; private set; }
    public static Task<FishingParallelizationResult> ParallelizationTask { get; private set; }
    public static FishingParallelizationResult LastParallelizationResult { get; private set; }
    public static string LastBenchmarkError { get; private set; }
    public static bool IsBenchmarkRunning => BenchmarkTask != null && !BenchmarkTask.IsCompleted;
    public static bool IsParallelizationTestRunning => ParallelizationTask != null && !ParallelizationTask.IsCompleted;
    public static bool IsAnyBenchmarkRunning => IsBenchmarkRunning || IsParallelizationTestRunning;


    public static bool StartBenchmark(FishingBenchmarkRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }
        if (IsBenchmarkRunning)
        {
            return false;
        }

        LastBenchmarkResult = null;
        LastBenchmarkError = null;

        BenchmarkTask = Task.Run(() => RunBenchmark(request));
        BenchmarkTask.ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                LastBenchmarkError = task.Exception?.GetBaseException().Message ?? "Benchmark failed.";
                return;
            }

            if (task.IsCanceled)
            {
                LastBenchmarkError = "Benchmark canceled.";
                return;
            }

            LastBenchmarkResult = task.Result;
        });

        return true;
    }

    public static bool StartParallelizationTest(FishingBenchmarkRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }
        if (IsBenchmarkRunning)
        {
            return false;
        }

        LastParallelizationResult = null;
        LastBenchmarkError = null;

        int numWorkersParallel = request.WorkerCount;
        // run serial
        request.WorkerCount = 1;
        BenchmarkTask = Task.Run(() => RunBenchmark(request));
        BenchmarkTask.ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                LastBenchmarkError = task.Exception?.GetBaseException().Message ?? "Parallelization test failed.";
                return;
            }

            if (task.IsCanceled)
            {
                LastBenchmarkError = "Parallelization test canceled.";
                return;
            }

            LastParallelizationResult = new FishingParallelizationResult
            {
                SerialResult = task.Result,
                ParallelResult = RunBenchmark(new FishingBenchmarkRequest
                {
                    WorkerCount = numWorkersParallel,
                    IterationsPerWorker = request.IterationsPerWorker,
                    CloneCount = request.CloneCount,
                    StepCount = request.StepCount,
                }),
            };
        }).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                LastBenchmarkError = task.Exception?.GetBaseException().Message ?? "Parallelization test failed.";
            }
        }, TaskContinuationOptions.OnlyOnFaulted);

        return true;
    }

    public static FishingBenchmarkResult RunBenchmark(FishingBenchmarkRequest request)
    {
        int workerCount = Math.Max(1, request.WorkerCount);
        int iterationsPerWorker = Math.Max(1, request.IterationsPerWorker);
        int cloneCount = Math.Max(0, request.CloneCount);
        int stepCount = Math.Max(0, request.StepCount);
        bool disableRandomTracking = request.DisableRandomTracking;

        long cloneTicks = 0;
        long stepTicks = 0;
        long totalClones = 0;
        long totalSteps = 0;

        Stopwatch totalStopwatch = Stopwatch.StartNew();
        Task[] tasks = new Task[workerCount];

        for (int worker = 0; worker < workerCount; worker++)
        {
            tasks[worker] = Task.Run(() =>
            {
                bool previousTracking = true;
                if (disableRandomTracking)
                {
                    previousTracking = RandomExtensions.SetTrackingEnabledForCurrentThread(false);
                }
                long localCloneTicks = 0;
                long localStepTicks = 0;
                long localClones = 0;
                long localSteps = 0;
                try
                {
                    {
                        SGame1 sim = new SGame1();

                        for (int iteration = 0; iteration < iterationsPerWorker; iteration++)
                        {
                            {
                                long start = Stopwatch.GetTimestamp();
                                for (int i = 0; i < cloneCount; i++)
                                {
                                    sim = sim.Clone();
                                }
                                localCloneTicks += Stopwatch.GetTimestamp() - start;
                                localClones += cloneCount;
                            }

                            {
                                long start = Stopwatch.GetTimestamp();
                                for (int i = 0; i < stepCount; i++)
                                {
                                    bool pressed = (i & 1) == 0;
                                    if (pressed)
                                    {
                                        sim.Press();
                                    }
                                    else
                                    {
                                        sim.Release();
                                    }
                                }
                                localStepTicks += Stopwatch.GetTimestamp() - start;
                                localSteps += stepCount;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModEntry.Console.Log($"Benchmark worker failed with exception: {ex}", StardewModdingAPI.LogLevel.Error);
                    ModEntry.Console.Log(ex.StackTrace, StardewModdingAPI.LogLevel.Error);
                }
                finally
                {
                    if (disableRandomTracking)
                    {
                        RandomExtensions.SetTrackingEnabledForCurrentThread(previousTracking);
                    }
                }

                Interlocked.Add(ref cloneTicks, localCloneTicks);
                Interlocked.Add(ref stepTicks, localStepTicks);
                Interlocked.Add(ref totalClones, localClones);
                Interlocked.Add(ref totalSteps, localSteps);
            });
        }

        Task.WaitAll(tasks);
        totalStopwatch.Stop();

        double frequency = Stopwatch.Frequency;
        return new FishingBenchmarkResult
        {
            WorkerCount = workerCount,
            IterationsPerWorker = iterationsPerWorker,
            CloneCount = totalClones,
            StepCount = totalSteps,
            TotalOperations = totalClones + totalSteps,
            TotalMilliseconds = totalStopwatch.Elapsed.TotalMilliseconds,
            CloneMilliseconds = (cloneTicks / frequency) * 1000.0,
            StepMilliseconds = (stepTicks / frequency) * 1000.0,
        };
    }
}
