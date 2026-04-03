using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TASMod.Console
{
    public static class ExternalLogger
    {
        private sealed class QueueItem
        {
            public string Text { get; init; }
            public string MessageType { get; init; }
        }

        private static readonly object SyncRoot = new();

        public static bool LogExternalMessages = true;
        public static int Pid;

        private static Channel<QueueItem> _channel;
        private static CancellationTokenSource _cts;
        private static Task _workerTask;
        private static bool _initialized;
        private static StreamWriter _writer;
        private static string _writerPath;

        public static void Init(int pid)
        {
            lock (SyncRoot)
            {
                if (_initialized)
                {
                    return;
                }

                Pid = pid;
                _channel = Channel.CreateUnbounded<QueueItem>(new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false,
                });
                _writerPath = null;
                _cts = new CancellationTokenSource();
                _workerTask = Task.Run(() => RunWorkerAsync(_channel.Reader, _cts.Token));
                _initialized = true;
            }
        }

        public static bool TryQueueMessage(string text, string message_type = null)
        {
            if (!LogExternalMessages)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var channel = _channel;
            if (channel is null)
            {
                return false;
            }

            return channel.Writer.TryWrite(new QueueItem
            {
                Text = text,
                MessageType = message_type,
            });
        }

        public static async Task ShutdownAsync()
        {
            Task workerTask;
            CancellationTokenSource cts;
            Channel<QueueItem> channel;
            StreamWriter writer;

            lock (SyncRoot)
            {
                if (!_initialized)
                {
                    return;
                }

                channel = _channel;
                workerTask = _workerTask;
                cts = _cts;
                writer = _writer;

                _channel = null;
                _workerTask = null;
                _cts = null;
                _writer = null;
                _writerPath = null;
                _initialized = false;
            }
            channel?.Writer.TryComplete();

            if (workerTask != null)
            {
                try
                {
                    await workerTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            if (writer != null)
            {
                writer.Dispose();
            }
        }

        private static async Task RunWorkerAsync(ChannelReader<QueueItem> reader, CancellationToken token)
        {
            try
            {
                while (await reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    while (reader.TryRead(out var item))
                    {
                        try
                        {
                            await PostMessageAsync(item.Text, item.MessageType, token).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"ExternalLogger worker error: {ex}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static async Task PostMessageAsync(string text, string messageType, CancellationToken token)
        {
            try
            {
                string line = JsonSerializer.Serialize(new
                {
                    pid = Pid,
                    type = "message",
                    text,
                    message_type = messageType,
                });
                await AppendLineAsync(line).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write message: {ex}");
            }
        }

        private static async Task AppendLineAsync(string line)
        {
            string prefix = GetCurrentPrefix();
            string path = Path.Combine(Constants.ExportsPath, $"{prefix}.{Pid}.out");
            if (!string.Equals(_writerPath, path, StringComparison.Ordinal))
            {
                _writer?.Dispose();
                _writer = CreateWriter(path);
                _writerPath = path;

                string header = JsonSerializer.Serialize(new
                {
                    pid = Pid,
                    type = "header",
                    state_prefix = prefix,
                    path,
                });
                await _writer.WriteLineAsync(header).ConfigureAwait(false);
                await _writer.FlushAsync().ConfigureAwait(false);
            }

            if (_writer is null)
            {
                return;
            }

            await _writer.WriteLineAsync(line).ConfigureAwait(false);
            await _writer.FlushAsync().ConfigureAwait(false);
        }

        private static StreamWriter CreateWriter(string path)
        {
            Directory.CreateDirectory(Constants.ExportsPath);
            var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            return new StreamWriter(stream);
        }

        private static string GetCurrentPrefix()
        {
            string prefix = Controller.State?.Prefix;
            if (string.IsNullOrWhiteSpace(prefix))
            {
                prefix = "tmp";
            }

            return SanitizeFileName(prefix);
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "tmp";
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            char[] chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalidChars, chars[i]) >= 0)
                {
                    chars[i] = '_';
                }
            }

            string sanitized = new string(chars).Trim();
            return string.IsNullOrWhiteSpace(sanitized) ? "tmp" : sanitized;
        }
    }
}