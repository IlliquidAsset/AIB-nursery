#if !EXPERIMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AIB
{
    public sealed class ObserverControlsBootstrap : MonoBehaviour
    {
        private enum ObserverCameraMode { Top, OTS, FirstPerson, NE, Free }

        private string[] replayFiles = Array.Empty<string>();
        private int replayIndex;
        private bool overlayVisible = true;
        private ObserverCameraMode cameraMode = ObserverCameraMode.Top;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<ObserverControlsBootstrap>() != null) return;
            var obj = new GameObject("AIB Observer Controls Bootstrap");
            DontDestroyOnLoad(obj);
            obj.AddComponent<ObserverControlsBootstrap>();
        }

        private void OnGUI()
        {
            if (overlayVisible) DrawOverlay();
            DrawControls();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab)) cameraMode = (ObserverCameraMode)(((int)cameraMode + 1) % 5);
            if (Input.GetKeyDown(KeyCode.B)) overlayVisible = !overlayVisible;
        }

        private void LateUpdate()
        {
            ApplyCameraMode();
        }

        private void DrawOverlay()
        {
            AbeStatePayload s = AbeStateBuffer.Instance.CurrentState;
            ReplayController replay = FindFirstObjectByType<ReplayController>();
            string source = AbeStateBuffer.Instance.IsConnected ? "live-arena" : AbeStateBuffer.Instance.ConnectionStatus;
            string replayStatus = replay != null && replay.IsLoaded
                ? $" | replay: {(replay.IsPlaying ? "playing" : "paused")} {replay.CurrentIndex + 1}/{replay.RowCount}"
                : string.Empty;

            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            string text =
                $"AIB Observer | camera: {cameraMode} | source: {source}{replayStatus}\n" +
                $"tick: {s.tick} | episode: {s.episode} | deaths: {s.deaths} | phase: {s.phase}\n" +
                $"pos: ({s.posX:F1}, {s.posY:F1}, {s.posZ:F1}) | action F:{s.currentActionForward} R:{s.currentActionRotate}\n" +
                $"health: {s.health:F1} | lavaDist: {s.lavaDistance:F2} ({s.lavaDistanceDelta:+0.00;-0.00;0.00})\n" +
                $"reward: {s.rewardThisTick:+0.000;-0.000;0.000} | native: {s.naturalReward:+0.000;-0.000;0.000} | shaped: {s.shapedReward:+0.000;-0.000;0.000} | predErr: {s.predictionError:F3}\n" +
                $"DA:{s.dopamine:F2} CORT:{s.cortisol:F2} OXY:{s.oxytocin:F2} CUR:{s.curiosity:F2} STRESS:{s.stress:F2} PLAST:{s.plasticity:F2}";
            GUI.Box(new Rect(14f, 14f, 760f, 168f), text, style);
        }

        private void DrawControls()
        {
            const float panelWidth = 900f;
            const float panelHeight = 124f;
            Rect panel = new Rect((Screen.width - panelWidth) * 0.5f, Screen.height - panelHeight - 16f, panelWidth, panelHeight);
            GUI.Box(panel, string.Empty);

            ReplayController replay = FindFirstObjectByType<ReplayController>();
            AbeStateReceiver receiver = FindFirstObjectByType<AbeStateReceiver>();
            if (replayFiles.Length == 0) replayFiles = DiscoverReplayFiles();

            string source = replay != null && replay.IsLoaded ? "Replay" : "Live";
            string replayName = replayFiles.Length > 0 ? Path.GetFileName(replayFiles[Mathf.Clamp(replayIndex, 0, replayFiles.Length - 1)]) : "no CSV replays found";
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(panel.x + 16f, panel.y + 10f, panelWidth - 32f, 26f), $"Observer Source: {source} | Selected: {replayName}", labelStyle);

            float x = panel.x + 16f;
            float y = panel.y + 44f;
            if (GUI.Button(new Rect(x, y, 92f, 34f), "Live", buttonStyle))
            {
                receiver?.SetReceivingEnabled(true);
                replay?.Pause();
            }
            x += 102f;
            if (GUI.Button(new Rect(x, y, 92f, 34f), "Prev", buttonStyle)) CycleReplay(-1);
            x += 102f;
            if (GUI.Button(new Rect(x, y, 92f, 34f), "Next", buttonStyle)) CycleReplay(1);
            x += 102f;
            if (GUI.Button(new Rect(x, y, 92f, 34f), "Load", buttonStyle)) LoadSelectedReplay(replay, receiver);
            x += 102f;
            if (GUI.Button(new Rect(x, y, 92f, 34f), "Play", buttonStyle)) replay?.Play();
            x += 102f;
            if (GUI.Button(new Rect(x, y, 92f, 34f), "Pause", buttonStyle)) replay?.Pause();
            x += 102f;
            if (GUI.Button(new Rect(x, y, 92f, 34f), "Step", buttonStyle)) replay?.StepForward();
            x += 102f;
            if (GUI.Button(new Rect(x, y, 92f, 34f), "Reset", buttonStyle)) replay?.ResetReplay();

            GUI.Label(new Rect(panel.x + 16f, panel.y + 88f, panelWidth - 32f, 24f), $"Keys: Tab camera ({cameraMode}) | B overlay | Space play/pause | Left/Right step | Home reset", labelStyle);
        }

        private void ApplyCameraMode()
        {
            Camera cam = Camera.main ?? FindFirstObjectByType<Camera>();
            if (cam == null) return;
            Vector3 agentPos = FindAgentPosition();
            Quaternion agentRot = FindAgentRotation();

            cam.enabled = true;
            switch (cameraMode)
            {
                case ObserverCameraMode.Top:
                    cam.orthographic = true;
                    cam.orthographicSize = 14f;
                    cam.transform.SetPositionAndRotation(agentPos + new Vector3(0f, 18f, 0f), Quaternion.Euler(90f, 0f, 0f));
                    break;
                case ObserverCameraMode.OTS:
                    cam.orthographic = false;
                    cam.transform.SetPositionAndRotation(agentPos + agentRot * new Vector3(0.8f, 2.0f, -4.0f), Quaternion.LookRotation((agentPos + Vector3.up * 1.0f) - cam.transform.position));
                    break;
                case ObserverCameraMode.FirstPerson:
                    cam.orthographic = false;
                    cam.transform.SetPositionAndRotation(agentPos + Vector3.up * 1.1f + agentRot * Vector3.forward * 0.25f, agentRot);
                    break;
                case ObserverCameraMode.NE:
                    cam.orthographic = false;
                    cam.transform.SetPositionAndRotation(agentPos + new Vector3(8f, 8f, -8f), Quaternion.LookRotation(agentPos - cam.transform.position));
                    break;
                case ObserverCameraMode.Free:
                    cam.orthographic = false;
                    break;
            }
        }

        private Vector3 FindAgentPosition()
        {
            if (AbeStateBuffer.Instance.CurrentState != null) return AbeStateBuffer.Instance.CurrentState.Position;
            Transform agent = GameObject.Find("AAI3Agent")?.transform;
            if (agent != null) return agent.position;
            SkinnedMeshRenderer smr = FindFirstObjectByType<SkinnedMeshRenderer>();
            return smr != null ? smr.bounds.center : Vector3.zero;
        }

        private Quaternion FindAgentRotation()
        {
            if (AbeStateBuffer.Instance.CurrentState != null) return Quaternion.Euler(0f, AbeStateBuffer.Instance.CurrentState.rotationY, 0f);
            Transform agent = GameObject.Find("AAI3Agent")?.transform;
            return agent != null ? agent.rotation : Quaternion.identity;
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
                    Debug.LogWarning($"[ObserverControlsBootstrap] Replay scan failed for {root}: {e.Message}");
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
            if (replayFiles.Length == 0) replayFiles = DiscoverReplayFiles();
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
    }
}
#endif
