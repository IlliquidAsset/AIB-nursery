#if !EXPERIMENT_BUILD
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AIB
{
    public class AbeStateReceiver : MonoBehaviour
    {
        public string host = "localhost";
        public int port = 8765;

        private const string ConfigFileName = "aib_connection.txt";
        private const int DiscoveryPort = 8764;
        private const string DiscoveryMessage = "AIB_DISCOVER";
        private const string DiscoveryResponsePrefix = "AIB_HERE:";

        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;
        private Thread _receiveThread;
        private bool _isReconnecting = false;
        private bool _receivingEnabled = true;

        private void Start()
        {
            ResolveHost();
            Debug.Log($"[AbeStateReceiver] Target: {host}:{port}");
            if (_receivingEnabled)
            {
                Connect();
            }
        }

        private void ResolveHost()
        {
            // Priority: command-line arg > env var > config file > LAN discovery > default
            if (TryParseCommandLine()) return;
            if (TryEnvironmentVariable()) return;
            if (TryConfigFile()) return;
            if (TryLanDiscovery()) return;
            Debug.Log("[AbeStateReceiver] No host found — using default localhost:8765");
        }

        private bool TryParseCommandLine()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--aib-host" && i + 1 < args.Length)
                {
                    ApplyHostString(args[i + 1]);
                    Debug.Log($"[AbeStateReceiver] Host from command line: {host}:{port}");
                    return true;
                }
            }
            return false;
        }

        private bool TryEnvironmentVariable()
        {
            string envHost = System.Environment.GetEnvironmentVariable("AIB_HOST");
            if (!string.IsNullOrEmpty(envHost))
            {
                ApplyHostString(envHost);
                Debug.Log($"[AbeStateReceiver] Host from AIB_HOST env var: {host}:{port}");
                return true;
            }
            return false;
        }

        private bool TryConfigFile()
        {
            // Check next to the executable, then project root
            string[] searchPaths = {
                Path.Combine(Application.dataPath, "..", ConfigFileName),
                Path.Combine(Application.persistentDataPath, ConfigFileName),
                Path.Combine(System.Environment.CurrentDirectory, ConfigFileName)
            };

            foreach (string path in searchPaths)
            {
                if (File.Exists(path))
                {
                    string content = File.ReadAllText(path).Trim();
                    if (!string.IsNullOrEmpty(content) && !content.StartsWith("#"))
                    {
                        ApplyHostString(content);
                        Debug.Log($"[AbeStateReceiver] Host from config file ({path}): {host}:{port}");
                        return true;
                    }
                }
            }
            return false;
        }

        private bool TryLanDiscovery()
        {
            AbeStateBuffer.Instance.ConnectionStatus = "Scanning LAN...";
            Debug.Log("[AbeStateReceiver] Scanning LAN for experiment broadcaster...");

            try
            {
                using (var udpClient = new UdpClient())
                {
                    udpClient.EnableBroadcast = true;
                    udpClient.Client.ReceiveTimeout = 2000;

                    byte[] request = Encoding.UTF8.GetBytes(DiscoveryMessage);
                    udpClient.Send(request, request.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));

                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] response = udpClient.Receive(ref remoteEP);
                    string responseStr = Encoding.UTF8.GetString(response);

                    if (responseStr.StartsWith(DiscoveryResponsePrefix))
                    {
                        string wsPort = responseStr.Substring(DiscoveryResponsePrefix.Length);
                        host = remoteEP.Address.ToString();
                        if (int.TryParse(wsPort, out int discoveredPort))
                        {
                            port = discoveredPort;
                        }
                        Debug.Log($"[AbeStateReceiver] Found experiment on LAN: {host}:{port}");
                        return true;
                    }
                }
            }
            catch (SocketException)
            {
                Debug.Log("[AbeStateReceiver] No experiment found on LAN (timeout)");
            }
            catch (Exception e)
            {
                Debug.Log($"[AbeStateReceiver] LAN discovery error: {e.Message}");
            }

            return false;
        }

        private void ApplyHostString(string hostString)
        {
            string[] parts = hostString.Split(':');
            if (parts.Length == 2)
            {
                host = parts[0];
                if (int.TryParse(parts[1], out int parsedPort))
                {
                    port = parsedPort;
                }
            }
            else
            {
                host = hostString;
            }
        }

        private void Connect()
        {
            if (!_receivingEnabled) return;
            if (_webSocket != null && _webSocket.State == WebSocketState.Open) return;

            AbeStateBuffer.Instance.ConnectionStatus = _isReconnecting ? "Reconnecting..." : "Connecting...";
            AbeStateBuffer.Instance.IsConnected = false;

            _cancellationTokenSource = new CancellationTokenSource();
            _receiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true,
                Name = "WebSocketReceiveThread"
            };
            _receiveThread.Start();
        }

        public void SetReceivingEnabled(bool enabled)
        {
            _receivingEnabled = enabled;
            if (!enabled)
            {
                AbeStateBuffer.Instance.IsConnected = false;
                AbeStateBuffer.Instance.ConnectionStatus = "Replay selected";
                _cancellationTokenSource?.Cancel();
                try
                {
                    if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                    {
                        _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Switching to replay", CancellationToken.None).Wait(500);
                    }
                }
                catch { }
                return;
            }

            AbeStateBuffer.Instance.ConnectionStatus = "Connecting...";
            _isReconnecting = false;
            Connect();
        }

        private void Update()
        {
            AbeStateBuffer.Instance.SwapBuffers();
        }

        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            if (_webSocket != null)
            {
                try
                {
                    if (_webSocket.State == WebSocketState.Open)
                    {
                        // Use a synchronous wait for cleanup since we can't await in OnDestroy
                        _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).Wait(1000);
                    }
                }
                catch { }
                finally
                {
                    _webSocket.Dispose();
                }
            }
        }

        private async void ReceiveLoop()
        {
            while (_receivingEnabled && !_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    _webSocket = new ClientWebSocket();
                    Uri uri = new Uri($"ws://{host}:{port}");
                    
                    await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);
                    
                    AbeStateBuffer.Instance.ConnectionStatus = "Live";
                    AbeStateBuffer.Instance.IsConnected = true;
                    _isReconnecting = false;
                    
                    Debug.Log($"[AbeStateReceiver] Connected to {uri}");

                    byte[] buffer = new byte[65536];
                    while (_webSocket.State == WebSocketState.Open && !_cancellationTokenSource.IsCancellationRequested)
                    {
                        WebSocketReceiveResult result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                        
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            break;
                        }
                        else if (result.MessageType == WebSocketMessageType.Text)
                        {
                            string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            try
                            {
                                AbeStatePayload payload = AbeStatePayload.FromJson(json);
                                if (payload != null)
                                {
                                    AbeStateBuffer.Instance.Write(payload);
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogWarning($"[AbeStateReceiver] Failed to parse payload: {e.Message}");
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AbeStateReceiver] Connection error: {e.Message}");
                }
                finally
                {
                    AbeStateBuffer.Instance.IsConnected = false;
                    AbeStateBuffer.Instance.ConnectionStatus = "Reconnecting...";
                    _isReconnecting = true;
                    
                    if (_webSocket != null)
                    {
                        _webSocket.Dispose();
                        _webSocket = null;
                    }
                }

                if (_receivingEnabled && !_cancellationTokenSource.IsCancellationRequested)
                {
                    // Wait before reconnecting
                    try
                    {
                        await Task.Delay(3000, _cancellationTokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
        }
    }
}
#endif
