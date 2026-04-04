using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using StardewValley.Objects;

namespace TASMod
{
    public class PerformanceTiming
    {
        public class RingBuffer<T> : IEnumerable<T>
        {
            private List<T> values;
            private int ptr;
            private int capacity;
            public int Count => values.Count;

            public RingBuffer(int capacity)
            {
                values = new List<T>(capacity);
                ptr = 0;
                this.capacity = capacity;
            }

            public void Add(T val)
            {
                if (values.Count < capacity)
                {
                    values.Add(val);
                }
                else
                {
                    values[ptr] = val;
                }
                ptr = (ptr + 1) % capacity;
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return (IEnumerator<T>)this.values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.values.GetEnumerator();
            }

            public void Clear()
            {
                values.Clear();
                ptr = 0;
            }

            public T this[int i]
            {
                get
                {
                    return this.values[i];
                }
            }
        }

        public Stopwatch timer;
        public TimeSpan frameStart;
        public RingBuffer<TimeSpan> Frames;

        public TimeSpan updateStart;
        public RingBuffer<TimeSpan> Updates;

        public TimeSpan drawStart;
        public RingBuffer<TimeSpan> Draws;

        public TimeSpan serverStart;
        public RingBuffer<TimeSpan> Server;

        public Dictionary<int, TimeSpan> clientStart;
        public Dictionary<int, RingBuffer<TimeSpan>> Client;

        public Dictionary<int, TimeSpan> gameUpdatesStarts;
        public Dictionary<int, RingBuffer<TimeSpan>> GameUpdates;
        public Dictionary<int, TimeSpan> gameDrawStarts;
        public Dictionary<int, RingBuffer<TimeSpan>> GameDraws;


        public PerformanceTiming()
        {
            timer = Stopwatch.StartNew();
            frameStart = timer.Elapsed;
            Frames = new(10000);
            Updates = new(10000);
            Draws = new(10000);
            Server = new(10000);
            clientStart = new();
            Client = new();
            gameUpdatesStarts = new();
            GameUpdates = new();
            gameDrawStarts = new();
            GameDraws = new();
        }

        public void Reset()
        {
            Frames.Clear();
            Updates.Clear();
            Draws.Clear();
            Server.Clear();

            clientStart.Clear();
            Client.Clear();

            GameUpdates.Clear();
            gameUpdatesStarts.Clear();
            GameDraws.Clear();
            gameDrawStarts.Clear();
        }

        public void StartFrame()
        {
            frameStart = timer.Elapsed;
        }

        public void EndFrame()
        {
            Frames.Add(timer.Elapsed - frameStart);
        }

        public void UpdatePrefix()
        {
            updateStart = timer.Elapsed;
        }
        public void UpdatePostfix()
        {
            Updates.Add(timer.Elapsed - updateStart);
        }

        public void DrawPrefix()
        {
            drawStart = timer.Elapsed;
        }
        public void DrawPostfix()
        {
            Draws.Add(timer.Elapsed - drawStart);
        }

        public void ServerPrefix()
        {
            serverStart = timer.Elapsed;
        }
        public void ServerPostfix()
        {
            Server.Add(timer.Elapsed - serverStart);
        }

        public void ClientPrefix(int instanceIndex)
        {
            if (!Client.ContainsKey(instanceIndex))
            {
                Client[instanceIndex] = new(10000);
            }
            clientStart[instanceIndex] = timer.Elapsed;
        }
        public void ClientPostfix(int instanceIndex)
        {
            if (!Client.ContainsKey(instanceIndex))
            {
                Client[instanceIndex] = new(10000);
                clientStart[instanceIndex] = timer.Elapsed;
            }
            Client[instanceIndex].Add(timer.Elapsed - clientStart[instanceIndex]);
        }

        public void GameUpdatePrefix(int instanceIndex)
        {
            if (!GameUpdates.ContainsKey(instanceIndex))
            {
                GameUpdates[instanceIndex] = new(10000);
            }
            gameUpdatesStarts[instanceIndex] = timer.Elapsed;
        }
        public void GameUpdatePostfix(int instanceIndex)
        {
            if (!GameUpdates.ContainsKey(instanceIndex))
            {
                GameUpdates[instanceIndex] = new(10000);
                gameUpdatesStarts[instanceIndex] = timer.Elapsed;
            }
            GameUpdates[instanceIndex].Add(timer.Elapsed - gameUpdatesStarts[instanceIndex]);
        }

        public void GameDrawPrefix(int instanceIndex)
        {
            if (!GameDraws.ContainsKey(instanceIndex))
            {
                GameDraws[instanceIndex] = new(10000);
            }
            gameDrawStarts[instanceIndex] = timer.Elapsed;
        }
        public void GameDrawPostfix(int instanceIndex)
        {
            if (!GameDraws.ContainsKey(instanceIndex))
            {
                GameDraws[instanceIndex] = new(10000);
                gameDrawStarts[instanceIndex] = timer.Elapsed;
            }
            GameDraws[instanceIndex].Add(timer.Elapsed - gameDrawStarts[instanceIndex]);
        }

        public string Stats(RingBuffer<TimeSpan> buffer, string prefix)
        {
            TimeSpan avg = new();
            for (int i = 0; i < buffer.Count; i++)
            {
                avg += buffer[i];
            }
            if (buffer.Count > 0)
                avg = avg / buffer.Count;

            TimeSpan p95 = TimeSpan.Zero;
            TimeSpan p99 = TimeSpan.Zero;
            if (buffer.Count > 0)
            {
                TimeSpan[] samples = new TimeSpan[buffer.Count];
                for (int i = 0; i < buffer.Count; i++)
                    samples[i] = buffer[i];
                Array.Sort(samples);
                int idx = (int)Math.Ceiling(0.95 * samples.Length) - 1;
                if (idx < 0) idx = 0;
                p95 = samples[Math.Min(idx, samples.Length - 1)];
                idx = (int)Math.Ceiling(0.99 * samples.Length) - 1;
                if (idx < 0) idx = 0;
                p99 = samples[Math.Min(idx, samples.Length - 1)];
            }

            return $"{prefix} Avg({buffer.Count}): {avg.TotalMilliseconds}ms (p95: {p95.TotalMilliseconds}ms) (p99: {p99.TotalMilliseconds}ms)";
        }
        public string[] Dump()
        {
            List<string> results = new()
            {
                "Performance Timing Stats:",
                Stats(Frames, "Frame"),
                Stats(Updates, "Update"),
                Stats(Server, "Server")
            };
            foreach (var kvp in Client)
            {
                results.Add(Stats(kvp.Value, $"Client {kvp.Key}"));
            }
            foreach (var kvp in GameUpdates)
            {
                results.Add(Stats(kvp.Value, $"Game Update {kvp.Key}"));
            }
            foreach (var kvp in GameDraws)
            {
                results.Add(Stats(kvp.Value, $"Game Draw {kvp.Key}"));
            }

            results.Add(Stats(Draws, "Draw"));
            return results.ToArray();
        }
    }
}