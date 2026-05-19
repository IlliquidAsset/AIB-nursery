using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace AIB
{
    public class SimpleWebSocketServer
    {
        private TcpListener _listener;
        private Thread _acceptThread;
        private bool _isRunning;
        private readonly ConcurrentDictionary<string, TcpClient> _clients = new ConcurrentDictionary<string, TcpClient>();

        public event Action<string> OnClientConnected;
        public event Action<string> OnClientDisconnected;

        public int ClientCount => _clients.Count;

        public void Start(int port)
        {
            if (_isRunning) return;

            _isRunning = true;
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            _acceptThread = new Thread(AcceptLoop)
            {
                IsBackground = true,
                Name = "WebSocketAcceptThread"
            };
            _acceptThread.Start();
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _listener?.Stop();

            foreach (var client in _clients.Values)
            {
                try { client.Close(); } catch { }
            }
            _clients.Clear();
        }

        public void Broadcast(string message)
        {
            if (!_isRunning || _clients.Count == 0) return;

            byte[] frame = CreateTextFrame(message);

            foreach (var kvp in _clients)
            {
                try
                {
                    NetworkStream stream = kvp.Value.GetStream();
                    stream.Write(frame, 0, frame.Length);
                }
                catch
                {
                    DisconnectClient(kvp.Key);
                }
            }
        }

        private void AcceptLoop()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    Thread clientThread = new Thread(() => HandleClient(client))
                    {
                        IsBackground = true,
                        Name = "WebSocketClientThread"
                    };
                    clientThread.Start();
                }
                catch (SocketException)
                {
                    // Expected when listener is stopped
                }
                catch (Exception)
                {
                    // Log or handle other exceptions
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            string clientId = Guid.NewGuid().ToString();
            NetworkStream stream = client.GetStream();

            try
            {
                if (!PerformHandshake(stream))
                {
                    client.Close();
                    return;
                }

                _clients.TryAdd(clientId, client);
                OnClientConnected?.Invoke(clientId);

                byte[] buffer = new byte[8192];
                while (_isRunning && client.Connected)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Disconnected

                    ProcessFrame(stream, buffer, bytesRead);
                }
            }
            catch (Exception)
            {
                // Client disconnected or error
            }
            finally
            {
                DisconnectClient(clientId);
            }
        }

        private void DisconnectClient(string clientId)
        {
            if (_clients.TryRemove(clientId, out TcpClient client))
            {
                try { client.Close(); } catch { }
                OnClientDisconnected?.Invoke(clientId);
            }
        }

        private bool PerformHandshake(NetworkStream stream)
        {
            byte[] buffer = new byte[4096];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (Regex.IsMatch(request, "^GET", RegexOptions.IgnoreCase))
            {
                string swk = Regex.Match(request, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                byte[] swkaSha1 = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                byte[] response = Encoding.UTF8.GetBytes(
                    "HTTP/1.1 101 Switching Protocols\r\n" +
                    "Connection: Upgrade\r\n" +
                    "Upgrade: websocket\r\n" +
                    "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                stream.Write(response, 0, response.Length);
                return true;
            }
            return false;
        }

        private void ProcessFrame(NetworkStream stream, byte[] buffer, int bytesRead)
        {
            if (bytesRead < 2) return;

            bool fin = (buffer[0] & 0b10000000) != 0;
            int opcode = buffer[0] & 0b00001111;
            bool mask = (buffer[1] & 0b10000000) != 0;
            int payloadLength = buffer[1] & 0b01111111;

            int offset = 2;
            if (payloadLength == 126)
            {
                if (bytesRead < 4) return;
                payloadLength = (buffer[2] << 8) | buffer[3];
                offset = 4;
            }
            else if (payloadLength == 127)
            {
                // Not supporting > 64KB for simplicity in this basic implementation
                return;
            }

            byte[] masks = new byte[4];
            if (mask)
            {
                if (bytesRead < offset + 4) return;
                Array.Copy(buffer, offset, masks, 0, 4);
                offset += 4;
            }

            if (bytesRead < offset + payloadLength) return;

            byte[] payload = new byte[payloadLength];
            Array.Copy(buffer, offset, payload, 0, payloadLength);

            if (mask)
            {
                for (int i = 0; i < payloadLength; ++i)
                {
                    payload[i] = (byte)(payload[i] ^ masks[i % 4]);
                }
            }

            if (opcode == 0x8) // Close
            {
                throw new Exception("Client closed connection");
            }
            else if (opcode == 0x9) // Ping
            {
                byte[] pong = new byte[2 + payloadLength];
                pong[0] = 0b10001010; // FIN + Pong
                pong[1] = (byte)payloadLength;
                Array.Copy(payload, 0, pong, 2, payloadLength);
                stream.Write(pong, 0, pong.Length);
            }
        }

        private byte[] CreateTextFrame(string message)
        {
            byte[] payload = Encoding.UTF8.GetBytes(message);
            int payloadLength = payload.Length;

            byte[] frame;
            int offset;

            if (payloadLength <= 125)
            {
                frame = new byte[2 + payloadLength];
                frame[1] = (byte)payloadLength;
                offset = 2;
            }
            else if (payloadLength <= 65535)
            {
                frame = new byte[4 + payloadLength];
                frame[1] = 126;
                frame[2] = (byte)((payloadLength >> 8) & 255);
                frame[3] = (byte)(payloadLength & 255);
                offset = 4;
            }
            else
            {
                // Not supporting > 64KB
                throw new ArgumentException("Message too large");
            }

            frame[0] = 0b10000001; // FIN + Text frame
            Array.Copy(payload, 0, frame, offset, payloadLength);

            return frame;
        }
    }
}