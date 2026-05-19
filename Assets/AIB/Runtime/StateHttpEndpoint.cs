#if EXPERIMENT_BUILD
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

namespace AIB
{
    public class StateHttpEndpoint : MonoBehaviour
    {
        public int port = 8766;
        private HttpListener _listener;
        private Thread _listenerThread;
        private bool _isRunning;
        private ConcurrentQueue<AbeStatePayload> _payloadQueue = new ConcurrentQueue<AbeStatePayload>();
        private AbeStateBroadcaster _broadcaster;

        private void Start()
        {
            _broadcaster = FindFirstObjectByType<AbeStateBroadcaster>();
            if (_broadcaster == null)
            {
                Debug.LogWarning("[StateHttpEndpoint] AbeStateBroadcaster not found in scene. HTTP payloads will not be broadcasted.");
            }

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://+:{port}/state/");
            
            try
            {
                _listener.Start();
                _isRunning = true;
                _listenerThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "HttpEndpointThread"
                };
                _listenerThread.Start();
                Debug.Log($"[StateHttpEndpoint] Started HTTP server on port {port}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[StateHttpEndpoint] Failed to start HTTP server: {e.Message}");
            }
        }

        private void Update()
        {
            while (_payloadQueue.TryDequeue(out AbeStatePayload payload))
            {
                if (_broadcaster != null)
                {
                    _broadcaster.UpdateState(payload);
                }
            }
        }

        private void OnDestroy()
        {
            _isRunning = false;
            if (_listener != null && _listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
                Debug.Log("[StateHttpEndpoint] Stopped HTTP server");
            }
        }

        private void ListenLoop()
        {
            while (_isRunning)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    ProcessRequest(context);
                }
                catch (HttpListenerException)
                {
                    // Expected when listener is stopped
                }
                catch (Exception e)
                {
                    if (_isRunning)
                    {
                        Debug.LogError($"[StateHttpEndpoint] Error processing request: {e.Message}");
                    }
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                if (request.HttpMethod == "POST")
                {
                    using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        string json = reader.ReadToEnd();
                        AbeStatePayload payload = AbeStatePayload.FromJson(json);
                        
                        if (payload != null)
                        {
                            _payloadQueue.Enqueue(payload);
                            response.StatusCode = (int)HttpStatusCode.OK;
                        }
                        else
                        {
                            response.StatusCode = (int)HttpStatusCode.BadRequest;
                        }
                    }
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                }
            }
            catch (Exception)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
            finally
            {
                response.Close();
            }
        }
    }
}
#endif