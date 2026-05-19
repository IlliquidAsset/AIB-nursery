#if !EXPERIMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

namespace AIB
{
    public enum CameraMode
    {
        OTS,
        FirstPerson,
        Free,
        Stationary1,
        Stationary2
    }

    public class CameraController : MonoBehaviour
    {
        public static bool BroadcastModeActive { get; private set; }
        public static CameraMode CurrentMode { get; private set; } = CameraMode.OTS;

        [Header("OTS Settings")]
        public Vector3 otsOffset = new Vector3(0.5f, 1.5f, -3f);
        public float otsLerpSpeed = 8f;
        public float otsLookVerticalOffset = 1f;

        [Header("First Person Settings")]
        public float fpHeightOffset = 0.8f;
        public float fpLerpSpeed = 15f;

        [Header("Free Cam Settings")]
        public float freeSpeed = 5f;
        public float freeSprintMultiplier = 2f;
        public float freeMouseSensitivity = 2f;

        [Header("Stationary Settings")]
        [SerializeField] private Vector3 stationary1Pos = new Vector3(10, 10, 10);
        [SerializeField] private Vector3 stationary2Pos = new Vector3(-10, 10, -10);

        public Vector3 Stationary1WorldPosition
        {
            get => stationary1Pos;
            set => stationary1Pos = value;
        }

        public Vector3 Stationary2WorldPosition
        {
            get => stationary2Pos;
            set => stationary2Pos = value;
        }

        private Camera mainCamera;
        private float freePitch = 0f;
        private float freeYaw = 0f;

        private GameObject watermarkObj;
        private TextMeshProUGUI watermarkText;
        private string[] replayFiles = Array.Empty<string>();
        private int replayIndex;

        private void Awake()
        {
            mainCamera = GetComponent<Camera>();
            if (mainCamera == null)
            {
                mainCamera = gameObject.AddComponent<Camera>();
            }
            
            CreateWatermark();
        }

        private void CreateWatermark()
        {
            GameObject canvasObj = new GameObject("BroadcastWatermarkCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            
            watermarkObj = new GameObject("WatermarkText");
            watermarkObj.transform.SetParent(canvasObj.transform, false);
            
            watermarkText = watermarkObj.AddComponent<TextMeshProUGUI>();
            watermarkText.fontSize = 22;
            // AIB-observer-patch-hud-contrast 2026-05-07: opaque white +
            // black outline so HUD reads on light or dark backgrounds.
            watermarkText.color = new Color(1f, 1f, 1f, 1f);
            watermarkText.outlineColor = Color.black;
            watermarkText.outlineWidth = 0.25f;
            watermarkText.alignment = TextAlignmentOptions.TopLeft;
            watermarkText.fontStyle = FontStyles.Bold;

            RectTransform rt = watermarkText.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(20, -20);
            rt.sizeDelta = new Vector2(800, 200);
            
            canvasObj.SetActive(false);
        }

        private void Update()
        {
            HandleInput();
            UpdateWatermark();
        }

        private void OnGUI()
        {
            DrawObserverControls();
        }

        private void LateUpdate()
        {
            UpdateCameraPosition();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                CurrentMode = (CameraMode)(((int)CurrentMode + 1) % 5);
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                ToggleBroadcastMode();
            }
        }

        private void DrawObserverControls()
        {
            const float panelWidth = 620f;
            const float panelHeight = 92f;
            Rect panel = new Rect((Screen.width - panelWidth) * 0.5f, Screen.height - panelHeight - 16f, panelWidth, panelHeight);
            GUI.Box(panel, string.Empty);

            ReplayController replay = FindFirstObjectByType<ReplayController>();
            AbeStateReceiver receiver = FindFirstObjectByType<AbeStateReceiver>();
            if (replayFiles.Length == 0)
            {
                replayFiles = DiscoverReplayFiles();
            }

            string source = replay != null && replay.IsLoaded ? "Replay" : "Live";
            string replayName = replayFiles.Length > 0 ? Path.GetFileName(replayFiles[Mathf.Clamp(replayIndex, 0, replayFiles.Length - 1)]) : "no CSV replays found";
            GUI.Label(new Rect(panel.x + 12f, panel.y + 8f, panelWidth - 24f, 22f), $"Observer Source: {source} | {replayName}");

            float x = panel.x + 12f;
            float y = panel.y + 36f;
            if (GUI.Button(new Rect(x, y, 66f, 26f), "Live"))
            {
                receiver?.SetReceivingEnabled(true);
                replay?.Pause();
            }
            x += 72f;
            if (GUI.Button(new Rect(x, y, 66f, 26f), "Prev"))
            {
                CycleReplay(-1);
            }
            x += 72f;
            if (GUI.Button(new Rect(x, y, 66f, 26f), "Next"))
            {
                CycleReplay(1);
            }
            x += 72f;
            if (GUI.Button(new Rect(x, y, 66f, 26f), "Load"))
            {
                LoadSelectedReplay(replay, receiver);
            }
            x += 72f;
            if (GUI.Button(new Rect(x, y, 66f, 26f), "Play")) replay?.Play();
            x += 72f;
            if (GUI.Button(new Rect(x, y, 66f, 26f), "Pause")) replay?.Pause();
            x += 72f;
            if (GUI.Button(new Rect(x, y, 66f, 26f), "Step")) replay?.StepForward();
            x += 72f;
            if (GUI.Button(new Rect(x, y, 66f, 26f), "Reset")) replay?.ResetReplay();

            GUI.Label(new Rect(panel.x + 12f, panel.y + 66f, panelWidth - 24f, 20f), "Keys: Tab camera | B overlay | Space play/pause | ←/→ step | Home reset");
        }

        private string[] DiscoverReplayFiles()
        {
            var files = new List<string>();
            foreach (string root in ReplaySearchRoots())
            {
                try
                {
                    if (!Directory.Exists(root)) continue;
                    files.AddRange(Directory.GetFiles(root, "*.csv", SearchOption.AllDirectories));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CameraController] Replay scan failed for {root}: {e.Message}");
                }
            }
            files.Sort(StringComparer.OrdinalIgnoreCase);
            return files.ToArray();
        }

        private IEnumerable<string> ReplaySearchRoots()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--replayDir") yield return args[i + 1];
            }
            string envReplayDir = Environment.GetEnvironmentVariable("AIB_REPLAY_DIR");
            if (!string.IsNullOrWhiteSpace(envReplayDir)) yield return envReplayDir;
            yield return Path.Combine(Application.dataPath, "..", "..", "..", "ObservationLogs");
            yield return Path.Combine(Application.dataPath, "..", "..", "..", "Builds", "Replays");
            yield return "/Users/kendrick/Documents/dev/AIB/logs";
        }

        private void CycleReplay(int delta)
        {
            if (replayFiles.Length == 0)
            {
                replayFiles = DiscoverReplayFiles();
            }
            if (replayFiles.Length == 0) return;
            replayIndex = (replayIndex + delta + replayFiles.Length) % replayFiles.Length;
        }

        private void LoadSelectedReplay(ReplayController replay, AbeStateReceiver receiver)
        {
            if (replay == null || replayFiles.Length == 0) return;
            receiver?.SetReceivingEnabled(false);
            replay.LoadReplayCsv(replayFiles[Mathf.Clamp(replayIndex, 0, replayFiles.Length - 1)]);
            replay.Pause();
        }

        // Public entry so AbeAutoSmokeCapture and remote drivers can flip
        // Broadcast Mode without simulating a keystroke (TCC blocks that).
        public void ToggleBroadcastMode()
        {
            BroadcastModeActive = !BroadcastModeActive;
            if (watermarkObj != null)
            {
                watermarkObj.transform.parent.gameObject.SetActive(BroadcastModeActive);
            }
            // AIB-observer-patch-cameras 2026-05-07: when entering Broadcast Mode,
            // disable competing high-priority cameras so the CameraController-driven
            // camera becomes the rendered view (otherwise TopViewOrthoCamera and
            // similar continue to render the orthographic top-down).
            ToggleCompetingCameras(!BroadcastModeActive);
        }

        private static readonly string[] _competingCameraNames = { "TopViewOrthoCamera", "ObserverCameraNE" };

        private void ToggleCompetingCameras(bool enabled)
        {
            foreach (var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                foreach (string name in _competingCameraNames)
                {
                    if (cam.gameObject.name == name)
                    {
                        cam.enabled = enabled;
                        Debug.Log($"[AIB] CameraController: {name}.enabled = {enabled}");
                    }
                }
            }
        }

        private void UpdateWatermark()
        {
            // AIB-observer-patch-overlay 2026-05-07: extended overlay.
            // When no broadcaster is connected, show a minimal "no-feed" HUD
            // so Observer Mode is still visually self-explanatory.
            if (!BroadcastModeActive) return;
            if (AbeStateBuffer.Instance != null)
            {
                var s = AbeStateBuffer.Instance.CurrentState;
                string source = AbeStateBuffer.Instance.IsConnected ? "live-arena" : AbeStateBuffer.Instance.ConnectionStatus;
                string action = $"F:{s.currentActionForward} R:{s.currentActionRotate}";
                ReplayController replay = FindFirstObjectByType<ReplayController>();
                string replayStatus = replay != null && replay.IsLoaded
                    ? $" | replay: {(replay.IsPlaying ? "playing" : "paused")} {replay.CurrentIndex + 1}/{replay.RowCount}"
                    : string.Empty;
                watermarkText.text =
                    $"AIB Observer | mode: {CurrentMode} | source: {source}{replayStatus}\n" +
                    $"tick: {s.tick} | episode: {s.episode} | deaths: {s.deaths} | phase: {s.phase}\n" +
                    $"pos: ({s.posX:F1}, {s.posY:F1}, {s.posZ:F1}) | action: {action}\n" +
                    $"health: {s.health:F1} | lavaDist: {s.lavaDistance:F2} ({s.lavaDistanceDelta:+0.00;-0.00;0.00})\n" +
                    $"reward: {s.rewardThisTick:+0.000;-0.000;0.000} | native: {s.naturalReward:+0.000;-0.000;0.000} | shaped: {s.shapedReward:+0.000;-0.000;0.000} | predErr: {s.predictionError:F3}\n" +
                    $"DA:{s.dopamine:F2} CORT:{s.cortisol:F2} OXY:{s.oxytocin:F2}  CUR:{s.curiosity:F2} STRESS:{s.stress:F2} PLAST:{s.plasticity:F2}";
            }
            else
            {
                watermarkText.text =
                    $"AIB Observer | mode: {CurrentMode} | source: no-feed (standalone)\n" +
                    $"Tab: cycle camera mode\n" +
                    $"B: toggle Broadcast Mode\n" +
                    $"WASD + Right-mouse: Free Cam\n" +
                    $"Connect Python ML-Agents endpoint for live tick stream.";
            }
        }

        private void UpdateCameraPosition()
        {
            // AIB-observer-patch-fallback 2026-05-07: when no broadcaster is
            // connected (Observer.app standalone, no Python ML-Agents), fall
            // back to whatever SkinnedMeshRenderer is in the scene (typically
            // AbeVisualMesh's char1) so OTS/FirstPerson still frame the agent.
            Vector3 agentPos;
            Quaternion agentRot;
            if (AbeStateBuffer.Instance != null && AbeStateBuffer.Instance.CurrentState != null)
            {
                var state = AbeStateBuffer.Instance.CurrentState;
                agentPos = state.Position;
                agentRot = Quaternion.Euler(0, state.rotationY, 0);
            }
            else
            {
                // AIB-observer-patch-fallback-prefer-abe 2026-05-07: prefer the
                // Abe SMR (mesh name "char1") over any other SMR in the scene
                // (e.g. the panda training agent), so OTS frames Abe.
                Transform stableRoot = GameObject.Find("AAI3Agent")?.transform;
                SkinnedMeshRenderer chosen = null;
                foreach (var smr in UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
                {
                    if (smr.sharedMesh != null && smr.sharedMesh.name == "char1")
                    {
                        chosen = smr;
                        stableRoot = smr.transform.root != null ? smr.transform.root : smr.transform;
                        break;
                    }
                    if (chosen == null) chosen = smr;
                }
                if (stableRoot != null)
                {
                    agentPos = stableRoot.position;
                    agentRot = stableRoot.rotation;
                }
                else if (chosen != null)
                {
                    agentPos = chosen.bounds.center;
                    agentRot = chosen.transform.rotation;
                }
                else
                {
                    agentPos = Vector3.zero;
                    agentRot = Quaternion.identity;
                }
            }

            switch (CurrentMode)
            {
                case CameraMode.OTS:
                    Vector3 targetOtsPos = agentPos + agentRot * otsOffset;
                    transform.position = Vector3.Lerp(transform.position, targetOtsPos, Time.deltaTime * otsLerpSpeed);
                    
                    Vector3 lookTarget = agentPos + Vector3.up * otsLookVerticalOffset;
                    Quaternion targetOtsRot = Quaternion.LookRotation(lookTarget - transform.position);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetOtsRot, Time.deltaTime * otsLerpSpeed);
                    break;

                case CameraMode.FirstPerson:
                    Vector3 targetFpPos = agentPos + Vector3.up * fpHeightOffset;
                    transform.position = Vector3.Lerp(transform.position, targetFpPos, Time.deltaTime * fpLerpSpeed);
                    transform.rotation = Quaternion.Slerp(transform.rotation, agentRot, Time.deltaTime * fpLerpSpeed);
                    break;

                case CameraMode.Free:
                    HandleFreeCamera();
                    break;

                case CameraMode.Stationary1:
                    HandleStationaryCamera(stationary1Pos, agentPos);
                    break;

                case CameraMode.Stationary2:
                    HandleStationaryCamera(stationary2Pos, agentPos);
                    break;
            }
        }

        private void HandleFreeCamera()
        {
            if (Input.GetMouseButton(1)) // Right click to look
            {
                freeYaw += Input.GetAxis("Mouse X") * freeMouseSensitivity;
                freePitch -= Input.GetAxis("Mouse Y") * freeMouseSensitivity;
                freePitch = Mathf.Clamp(freePitch, -90f, 90f);
                transform.eulerAngles = new Vector3(freePitch, freeYaw, 0f);
            }

            float speed = freeSpeed * (Input.GetKey(KeyCode.LeftShift) ? freeSprintMultiplier : 1f);
            
            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) move += transform.forward;
            if (Input.GetKey(KeyCode.S)) move -= transform.forward;
            if (Input.GetKey(KeyCode.A)) move -= transform.right;
            if (Input.GetKey(KeyCode.D)) move += transform.right;
            if (Input.GetKey(KeyCode.E)) move += transform.up;
            if (Input.GetKey(KeyCode.Q)) move -= transform.up;

            transform.position += move.normalized * speed * Time.deltaTime;
        }

        private void HandleStationaryCamera(Vector3 pos, Vector3 agentPos)
        {
            transform.position = pos;

            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(1))
            {
                freeYaw += Input.GetAxis("Mouse X") * freeMouseSensitivity;
                freePitch -= Input.GetAxis("Mouse Y") * freeMouseSensitivity;
                freePitch = Mathf.Clamp(freePitch, -90f, 90f);
                transform.eulerAngles = new Vector3(freePitch, freeYaw, 0f);
            }
            else
            {
                Vector3 dir = agentPos - transform.position;
                if (dir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(dir);
                    freePitch = transform.eulerAngles.x;
                    freeYaw = transform.eulerAngles.y;
                }
            }
        }
    }
}
#endif
