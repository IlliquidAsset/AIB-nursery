#if EXPERIMENT_BUILD
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace AIB
{
    public class AbeStateBroadcaster : MonoBehaviour
    {
        public int port = 8765;

        private const int DiscoveryPort = 8764;
        private const string DiscoveryMessage = "AIB_DISCOVER";
        private const string DiscoveryResponsePrefix = "AIB_HERE:";

        private SimpleWebSocketServer _server;
        private UdpClient _discoveryListener;
        private Thread _discoveryThread;
        private bool _isRunning;

        private void Awake()
        {
            _isRunning = true;

            _server = new SimpleWebSocketServer();
            _server.OnClientConnected += OnClientConnected;
            _server.OnClientDisconnected += OnClientDisconnected;
            _server.Start(port);
            Debug.Log($"[AbeStateBroadcaster] Started WebSocket server on port {port}");

            StartDiscoveryResponder();
        }

        private void StartDiscoveryResponder()
        {
            try
            {
                _discoveryListener = new UdpClient(DiscoveryPort);
                _discoveryThread = new Thread(DiscoveryLoop)
                {
                    IsBackground = true,
                    Name = "DiscoveryResponder"
                };
                _discoveryThread.Start();
                Debug.Log($"[AbeStateBroadcaster] LAN discovery responder on UDP port {DiscoveryPort}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AbeStateBroadcaster] Could not start discovery responder: {e.Message}");
            }
        }

        private void DiscoveryLoop()
        {
            while (_isRunning)
            {
                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = _discoveryListener.Receive(ref remoteEP);
                    string message = Encoding.UTF8.GetString(data);

                    if (message == DiscoveryMessage)
                    {
                        byte[] response = Encoding.UTF8.GetBytes($"{DiscoveryResponsePrefix}{port}");
                        _discoveryListener.Send(response, response.Length, remoteEP);
                        Debug.Log($"[AbeStateBroadcaster] Discovery ping from {remoteEP.Address} — responded");
                    }
                }
                catch (SocketException)
                {
                    // Expected on shutdown
                }
                catch (Exception e)
                {
                    if (_isRunning) Debug.LogWarning($"[AbeStateBroadcaster] Discovery error: {e.Message}");
                }
            }
        }

        private void OnDestroy()
        {
            _isRunning = false;

            _discoveryListener?.Close();

            if (_server != null)
            {
                _server.OnClientConnected -= OnClientConnected;
                _server.OnClientDisconnected -= OnClientDisconnected;
                _server.Stop();
                Debug.Log("[AbeStateBroadcaster] Stopped WebSocket server");
            }
        }

        private void OnClientConnected(string clientId)
        {
            Debug.Log($"[AbeStateBroadcaster] Client connected: {clientId}. Total clients: {_server.ClientCount}");
        }

        private void OnClientDisconnected(string clientId)
        {
            Debug.Log($"[AbeStateBroadcaster] Client disconnected: {clientId}. Total clients: {_server.ClientCount}");
        }

        public void UpdateState(AbeStatePayload payload)
        {
            if (_server == null || _server.ClientCount == 0) return;

            string json = payload.ToJson();
            _server.Broadcast(json);
        }
    }
}
#endif