using System;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using StitchingImage.Stitch_Tools.Utils;

namespace StitchingImage.Connection
{
    public enum ConnectionStateTCP
    {
        Disconnected,
        Connecting,
        Connected,
        Failed
    }

    public sealed class TcpConnectionManager : IDisposable
    {
        private readonly object _lock = new object();
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        private TcpClient _client;
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private Task _listenTask;

        public event Action<ConnectionStateTCP, string> StateChanged;
        public event Action<string> ProtocolSent;
        public event Action<string> ProtocolReceived;
        public event Action<string> ProtocolData;
        public event Action<string> ProtocolStatus;
        public event Action<StitchRequest> StitchRequested;
        public event Action<string> StitchStopRequested;

        public Func<bool> IsStitching { get; set; }

        public ConnectionStateTCP State { get; private set; } = ConnectionStateTCP.Disconnected;

        public void Start(string host, int port, int maxAttempts, TimeSpan delay)
        {
            Stop();

            _cts = new CancellationTokenSource();
            // Tony 20260202 Switch to TCP host listener (ISP role).
            _listenTask = Task.Run(() => ListenLoop(host, port, maxAttempts, delay, _cts.Token));
        }

        public void Stop()
        {
            lock (_lock)
            {
                _cts?.Cancel();
                _cts = null;
            }

            // Tony 20260202 Stop listener before waiting to unblock AcceptTcpClient.
            CloseListener();

            try
            {
                _listenTask?.Wait(500);
            }
            catch
            {
                // ignore
            }
            finally
            {
                _listenTask = null;
            }

            CloseClient();
            UpdateState(ConnectionStateTCP.Disconnected, "Disconnected");
        }

        private void ListenLoop(string host, int port, int maxAttempts, TimeSpan delay, CancellationToken token)
        {
            var bindAddress = ResolveBindAddress(host);
            var bindLabel = bindAddress == System.Net.IPAddress.Any ? "0.0.0.0" : bindAddress.ToString();
            // Tony 20260202 Start TCP host listener with retry support.
            UpdateState(ConnectionStateTCP.Connecting, $"Starting TCP host on {bindLabel}:{port}...");
            Logger.Info($"TCP listen loop started. Bind={bindLabel}:{port}, attempts={maxAttempts}, delay={delay.TotalSeconds}s");

            for (var attempt = 1; attempt <= maxAttempts && !token.IsCancellationRequested; attempt++)
            {
                try
                {
                    UpdateState(ConnectionStateTCP.Connecting, $"Attempt {attempt}/{maxAttempts} -> {bindLabel}:{port}");
                    Logger.Info($"TCP listen attempt {attempt}/{maxAttempts} on {bindLabel}:{port}");
                    var listener = new TcpListener(bindAddress, port);
                    listener.Start();
                    lock (_lock)
                    {
                        _listener = listener;
                    }

                    UpdateState(ConnectionStateTCP.Connected, $"Listening on {bindLabel}:{port}");
                    Logger.Info($"TCP listener started on {bindLabel}:{port}");
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Warning($"TCP listen failed {attempt}/{maxAttempts}: {ex.Message}");
                }

                if (attempt < maxAttempts && !token.IsCancellationRequested)
                {
                    try
                    {
                        Task.Delay(delay, token).Wait(token);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }

            if (_listener == null)
            {
                UpdateState(ConnectionStateTCP.Failed, $"Failed to start host on {bindLabel}:{port}");
                Logger.Warning($"TCP listener failed after {maxAttempts} attempts.");
                return;
            }

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = _listener.AcceptTcpClient();
                    if (token.IsCancellationRequested)
                    {
                        client.Close();
                        break;
                    }

                    lock (_lock)
                    {
                        _client?.Close();
                        _client = client;
                    }

                    var remoteEndPoint = client.Client?.RemoteEndPoint?.ToString() ?? "unknown";
                    // Tony 20260202 Update state when AH01 connects to ISP host.
                    UpdateState(ConnectionStateTCP.Connected, $"Client connected: {remoteEndPoint}");
                    Logger.Info($"TCP client connected from {remoteEndPoint}");

#if DEBUG
                    // Tony 20260202 DEBUG: log when AH01 connects.
                    Logger.Info("DEBUG: AH01 connected, receive loop started.");
#endif
                    _ = ReceiveLoop(client, token);
                }
                catch (SocketException ex)
                {
                    if (token.IsCancellationRequested)
                        break;
                    Logger.Warning($"TCP accept failed: {ex.Message}");
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        private void CloseClient()
        {
            lock (_lock)
            {
                if (_client == null)
                    return;

                try
                {
                    _client.Close();
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    _client = null;
                }
            }
        }

        private void CloseListener()
        {
            // Tony 20260202 Ensure TCP listener shuts down cleanly.
            lock (_lock)
            {
                if (_listener == null)
                    return;

                try
                {
                    _listener.Stop();
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    _listener = null;
                }
            }
        }

        private static System.Net.IPAddress ResolveBindAddress(string host)
        {
            // Tony 20260202 Default to binding on any interface when host is empty.
            if (string.IsNullOrWhiteSpace(host) || host == "0.0.0.0")
            {
                return System.Net.IPAddress.Any;
            }

            if (System.Net.IPAddress.TryParse(host, out var address))
            {
                return address;
            }

            return System.Net.IPAddress.Any;
        }

        // Tony 20260202 Receive and handle TCP protocol messages from AH01.
        private async Task ReceiveLoop(TcpClient client, CancellationToken token)
        {
            var buffer = new byte[4096];
            var sb = new StringBuilder();
            var disconnected = false;
            try
            {
                using (var stream = client.GetStream())
                {
                    while (!token.IsCancellationRequested && client.Connected)
                    {
                        if (!stream.DataAvailable)
                        {
                            await Task.Delay(100, token);
                            continue;
                        }

                        var read = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                        if (read <= 0)
                        {
                            disconnected = true;
                            break;
                        }

                        sb.Append(Encoding.UTF8.GetString(buffer, 0, read));
                        await DrainReceiveBuffer(sb, token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                Logger.Warning($"TCP receive loop error: {ex.Message}");
            }
            finally
            {
                if (disconnected && !token.IsCancellationRequested)
                {
                    CloseClient();
                    UpdateState(ConnectionStateTCP.Disconnected, "Client disconnected.");
                }
            }
        }

        private async Task DrainReceiveBuffer(StringBuilder buffer, CancellationToken token)
        {
            var payload = buffer.ToString();
            var newlineIndex = payload.IndexOf('\n');
            while (newlineIndex >= 0)
            {
                var line = payload.Substring(0, newlineIndex).Trim();
                if (!string.IsNullOrWhiteSpace(line))
                    await HandlePayload(line, token);
                payload = payload.Substring(newlineIndex + 1);
                newlineIndex = payload.IndexOf('\n');
            }

            if (!string.IsNullOrWhiteSpace(payload) && await TryHandleJsonAsync(payload))
            {
                payload = string.Empty;
            }

            buffer.Clear();
            buffer.Append(payload);
        }

        private async Task<bool> TryHandleJsonAsync(string payload)
        {
            try
            {
                _serializer.DeserializeObject(payload);
                await HandlePayload(payload, CancellationToken.None);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task HandlePayload(string payload, CancellationToken token)
        {
            NotifyProtocolReceived(payload);
#if DEBUG
            // Tony 20260202 DEBUG: show protocol receive payload in log.
            Logger.Info($"DEBUG protocol RX: {payload}");
#endif

            Dictionary<string, object> command;
            try
            {
                command = _serializer.Deserialize<Dictionary<string, object>>(payload);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Protocol parse failed: {ex.Message}");
                return;
            }

            if (command == null)
                return;

            var messageId = GetInt(command, "message_id");
            var cmd = GetString(command, "cmd");
            var respond = GetString(command, "respond");
            var task = GetString(command, "task");
            var action = GetString(command, "action");

            var statusLabel = !string.IsNullOrWhiteSpace(cmd) ? cmd : respond;
            if (!string.IsNullOrWhiteSpace(statusLabel))
                NotifyProtocolStatus($"RX {statusLabel}");

            if (command.TryGetValue("data", out var dataObj))
            {
                var dataText = _serializer.Serialize(dataObj);
                NotifyProtocolData(dataText);
            }

            if (!string.IsNullOrWhiteSpace(cmd))
            {
                await HandleCommand(cmd, task, action, messageId, command, token);
            }
        }

        private async Task HandleCommand(string cmd, string task, string action, int messageId, Dictionary<string, object> command, CancellationToken token)
        {
            var normalized = cmd.ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(task) || string.IsNullOrWhiteSpace(action))
                ParseCommand(cmd, out task, out action);

            // Tony 20260202 Return BUSY during stitching for heartbeat/link requests.
            if (normalized.Contains("LINK") || normalized.Contains("HEARTBEAT")
                || string.Equals(task, "LINK", StringComparison.OrdinalIgnoreCase)
                || string.Equals(task, "HEARTBEAT", StringComparison.OrdinalIgnoreCase))
            {
                var ackStatus = IsStitching?.Invoke() == true ? "BUSY" : "OK";
                await SendAckAsync("LINK", messageId, ackStatus, string.Empty, token);
                return;
            }

            if (normalized.Contains("STITCH") || string.Equals(task, "STITCH", StringComparison.OrdinalIgnoreCase))
            {
                // Tony 20260202 Allow AH01.STITCH.END to stop stitching immediately.
                if (string.Equals(action, "END", StringComparison.OrdinalIgnoreCase))
                {
                    StitchStopRequested?.Invoke("AH01.STITCH.END received.");
                    await SendStitchResultsAsync(messageId, false, ErrorCodePLP.DataStitchCancelled);
                    return;
                }

                // Tony 20260202 Return BUSY during stitching for start requests.
                if (IsStitching?.Invoke() == true)
                {
                    await SendAckAsync("STITCH", messageId, "BUSY", string.Empty, token);
                    return;
                }

                var request = BuildStitchRequest(command, messageId, cmd, action);
                if (request == null || string.IsNullOrWhiteSpace(request.Data?.SharedFolder))
                {
                    await SendAckAsync("STITCH", messageId, "ERROR", ErrorCodeConnection.SharedFolderNotFound, token);
                    return;
                }

                await SendAckAsync("STITCH", messageId, "OK", string.Empty, token);
                StitchRequested?.Invoke(request);
            }
        }

        public async Task SendStitchResultsAsync(int messageId, bool success, string errorCode)
        {
            var payload = new Dictionary<string, object>
            {
                { "respond", "ISP.STITCH.RESULTS" },
                { "message_id", messageId },
                { "source", "ISP" },
                { "destination", "AH01" },
                { "task", "STITCH" },
                { "action", "RESULTS" },
                { "timestamp", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") },
                { "data", new Dictionary<string, object>
                    {
                        { "status", success ? 0 : 1 },
                        { "errors", success ? string.Empty : (errorCode ?? ErrorCodePLP.DataStitchFailed) }
                    }
                }
            };

            await SendPayloadAsync(_serializer.Serialize(payload), CancellationToken.None);
        }

        public async Task SendRawAsync(string payload, CancellationToken token)
        {
            await SendPayloadAsync(payload, token);
        }

        private async Task SendAckAsync(string task, int messageId, string ack, string error, CancellationToken token)
        {
            var payload = new Dictionary<string, object>
            {
                { "respond", $"ISP.{task}.ACK" },
                { "message_id", messageId },
                { "source", "ISP" },
                { "destination", "AH01" },
                { "task", task },
                { "action", "ACK" },
                { "timestamp", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") },
                { "data", new Dictionary<string, object>
                    {
                        { "ACK", ack },
                        { "errors", error ?? string.Empty }
                    }
                }
            };

            await SendPayloadAsync(_serializer.Serialize(payload), token);
        }

        private async Task SendPayloadAsync(string payload, CancellationToken token)
        {
            var bytes = Encoding.UTF8.GetBytes(payload + "\n");
            TcpClient client;
            lock (_lock)
            {
                client = _client;
            }

            if (client == null || !client.Connected)
            {
                Logger.Warning("TCP send skipped: no active client.");
                return;
            }

            try
            {
                var stream = client.GetStream();
                await stream.WriteAsync(bytes, 0, bytes.Length, token);

                NotifyProtocolSent(payload);
#if DEBUG
                // Tony 20260202 DEBUG: show protocol send payload in log.
                Logger.Info($"DEBUG protocol TX: {payload}");
#endif
            }
            catch (Exception ex)
            {
                Logger.Warning($"TCP send failed: {ex.Message}");
            }
        }

        private StitchRequest BuildStitchRequest(Dictionary<string, object> command, int messageId, string cmd, string action)
        {
            if (!command.TryGetValue("data", out var dataObj))
                return null;

            var data = dataObj as Dictionary<string, object>;
            if (data == null)
                return null;

            return new StitchRequest
            {
                MessageId = messageId,
                Command = cmd,
                Action = action,
                Data = new StitchPayload
                {
                    SharedFolder = GetString(data, "shared_folder"),
                    Rows = GetInt(data, "rows"),
                    Columns = GetInt(data, "columns"),
                    GroupId = GetInt(data, "group_id"),
                    StartPoint = GetInt(data, "start_point"),
                    Direction = GetInt(data, "direction"),
                    Overlap = GetDouble(data, "overlap"),
                    Resolution = GetDouble(data, "resolution")
                }
            };
        }

        private static void ParseCommand(string cmd, out string task, out string action)
        {
            task = string.Empty;
            action = string.Empty;
            if (string.IsNullOrWhiteSpace(cmd))
                return;

            var parts = cmd.Split('.');
            if (parts.Length >= 3)
            {
                task = parts[1];
                action = parts[2];
            }
        }

        private static string GetString(Dictionary<string, object> data, string key)
        {
            if (data == null || !data.TryGetValue(key, out var value) || value == null)
                return string.Empty;

            return value.ToString();
        }

        private static int GetInt(Dictionary<string, object> data, string key)
        {
            if (data == null || !data.TryGetValue(key, out var value) || value == null)
                return 0;

            if (value is int intValue)
                return intValue;

            if (int.TryParse(value.ToString(), out var parsed))
                return parsed;

            return 0;
        }

        private static double GetDouble(Dictionary<string, object> data, string key)
        {
            if (data == null || !data.TryGetValue(key, out var value) || value == null)
                return 0;

            if (value is double dblValue)
                return dblValue;

            if (double.TryParse(value.ToString(), out var parsed))
                return parsed;

            return 0;
        }

        private void NotifyProtocolSent(string payload)
        {
            var entry = $"[{DateTime.Now:HH:mm:ss}] {payload}";
            ProtocolSent?.Invoke(entry);
            NotifyProtocolStatus($"TX {ExtractCommandLabel(payload)}");
        }

        private void NotifyProtocolReceived(string payload)
        {
            var entry = $"[{DateTime.Now:HH:mm:ss}] {payload}";
            ProtocolReceived?.Invoke(entry);
        }

        private void NotifyProtocolData(string dataText)
        {
            var entry = $"[{DateTime.Now:HH:mm:ss}] {dataText}";
            ProtocolData?.Invoke(entry);
        }

        private void NotifyProtocolStatus(string status)
        {
            var entry = $"[{DateTime.Now:HH:mm:ss}] {status}";
            ProtocolStatus?.Invoke(entry);
        }

        private string ExtractCommandLabel(string payload)
        {
            // Tony 20260202 Extract protocol command/respond label for status UI.
            try
            {
                var data = _serializer.Deserialize<Dictionary<string, object>>(payload);
                var respond = GetString(data, "respond");
                if (!string.IsNullOrWhiteSpace(respond))
                    return respond;
                var cmd = GetString(data, "cmd");
                if (!string.IsNullOrWhiteSpace(cmd))
                    return cmd;
            }
            catch
            {
                // ignore parse errors
            }

            return "UNKNOWN";
        }

        private void UpdateState(ConnectionStateTCP state, string message)
        {
            State = state;
            StateChanged?.Invoke(state, message);
        }

        public void Dispose()
        {
            Stop();
        }
    }

    public sealed class StitchRequest
    {
        public int MessageId { get; set; }
        public string Command { get; set; }
        public string Action { get; set; }
        public StitchPayload Data { get; set; }
    }

    public sealed class StitchPayload
    {
        public string SharedFolder { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
        public int GroupId { get; set; }
        public int StartPoint { get; set; }
        public int Direction { get; set; }
        public double Overlap { get; set; }
        public double Resolution { get; set; }
    }
}
