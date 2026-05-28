#if !EXPERIMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AIB
{
    public class ObserverControlsPanel : MonoBehaviour
    {
    private readonly List<string> replayPaths = new List<string>();
    private TextMeshProUGUI statusText;
    private ReplayController replayController;
    private AbeStateReceiver stateReceiver;
    private int selectedReplayIndex;
    private Slider scrubSlider;

        private void Awake()
        {
            CreateUI();
        }

        private void Start()
        {
            RefreshReplayList();
        }

        private void Update()
        {
            FindComponents();
            if (Input.GetKeyDown(KeyCode.R))
            {
                RefreshReplayList();
            }

            if (scrubSlider != null && replayController != null && replayController.IsLoaded)
            {
                scrubSlider.SetValueWithoutNotify(replayController.NormalizedPosition);
            }

            var buffer = AbeStateBuffer.Instance;
            string source = buffer.IsConnected ? "LIVE ARENA" : buffer.ConnectionStatus;
            string selected = replayPaths.Count > 0
                ? ShortReplayName(replayPaths[Mathf.Clamp(selectedReplayIndex, 0, replayPaths.Count - 1)])
                : "no replay files found";
            string replay = "Replay: not loaded";
            if (replayController != null && replayController.IsLoaded)
            {
                string state = replayController.IsPlaying ? "playing" : "paused";
                replay = $"Replay: {state} {replayController.CurrentIndex + 1}/{replayController.RowCount}";
            }

            statusText.text =
                $"Source: {source}    Selected: {selected}\n" +
                $"{replay}\n" +
                "Keys: Space play/pause · ←/→ step · Home reset · Tab camera · B HUD · R rescan";
        }

        public void RefreshReplayList()
        {
            replayPaths.Clear();
            foreach (string root in ReplaySearchRoots())
            {
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) continue;
                TryCollect(root, "tick_log.csv", true);
                TryCollect(root, "*.csv", false);
            }

            replayPaths.Sort((a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));
            if (replayPaths.Count > 100)
            {
                replayPaths.RemoveRange(100, replayPaths.Count - 100);
            }
            selectedReplayIndex = Mathf.Clamp(selectedReplayIndex, 0, Mathf.Max(0, replayPaths.Count - 1));
        }

        private void SelectLive()
        {
            FindComponents();
            replayController?.Pause();
            stateReceiver?.SetReceivingEnabled(true);
        }

        private void SelectPreviousReplay()
        {
            if (replayPaths.Count == 0) return;
            selectedReplayIndex = (selectedReplayIndex - 1 + replayPaths.Count) % replayPaths.Count;
        }

        private void SelectNextReplay()
        {
            if (replayPaths.Count == 0) return;
            selectedReplayIndex = (selectedReplayIndex + 1) % replayPaths.Count;
        }

        private void LoadSelectedReplay()
        {
            if (replayPaths.Count == 0) return;
            FindComponents();
            if (replayController == null) return;
            stateReceiver?.SetReceivingEnabled(false);
            replayController.LoadReplayCsv(replayPaths[Mathf.Clamp(selectedReplayIndex, 0, replayPaths.Count - 1)]);
        }

        private void CreateUI()
        {
            statusText = CreateText("ObserverStatus", new Vector2(12, -8), new Vector2(596, 52), 15, TextAlignmentOptions.TopLeft);

            CreateButton("Live", new Vector2(12, -62), () => SelectLive());
            CreateButton("Prev", new Vector2(88, -62), () => SelectPreviousReplay());
            CreateButton("Next", new Vector2(164, -62), () => SelectNextReplay());
            CreateButton("Load", new Vector2(240, -62), () => LoadSelectedReplay());

            CreateButton("Play", new Vector2(12, -96), () => FindReplay()?.Play());
            CreateButton("Pause", new Vector2(88, -96), () => FindReplay()?.Pause());
            CreateButton("Step", new Vector2(164, -96), () => FindReplay()?.StepForward());
            CreateButton("Reset", new Vector2(240, -96), () => FindReplay()?.ResetReplay());

            scrubSlider = CreateSlider("ReplayScrub", new Vector2(12, -130), new Vector2(300, 20), (float v) =>
            {
                FindReplay()?.Seek(v);
            });

            CreateSpeedButton("0.5x", new Vector2(324, -96), 0);
            CreateSpeedButton("1x", new Vector2(380, -96), 1);
            CreateSpeedButton("2x", new Vector2(436, -96), 2);
            CreateSpeedButton("5x", new Vector2(492, -96), 3);
        }

        private void FindComponents()
        {
            if (replayController == null)
            {
                replayController = FindFirstObjectByType<ReplayController>();
            }
            if (stateReceiver == null)
            {
                stateReceiver = FindFirstObjectByType<AbeStateReceiver>();
            }
        }

        private ReplayController FindReplay()
        {
            FindComponents();
            return replayController;
        }

        private IEnumerable<string> ReplaySearchRoots()
        {
            string envRoot = Environment.GetEnvironmentVariable("AIB_REPLAY_DIR");
            string argsRoot = ParseReplayDirArg();
            string projectLogs = "/Users/kendrick/Documents/dev/AIB/logs";
            string unityRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            if (!string.IsNullOrWhiteSpace(argsRoot)) yield return argsRoot;
            if (!string.IsNullOrWhiteSpace(envRoot)) yield return envRoot;
            yield return Path.Combine(unityRoot, "ObservationLogs");
            yield return Path.Combine(unityRoot, "Builds", "Replays");
            yield return projectLogs;
        }

        private static string ParseReplayDirArg()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--replayDir") return args[i + 1];
            }
            return string.Empty;
        }

        private void TryCollect(string root, string pattern, bool recursive)
        {
            try
            {
                var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                foreach (string path in Directory.GetFiles(root, pattern, option))
                {
                    if (!replayPaths.Contains(path)) replayPaths.Add(path);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ObserverControlsPanel] Replay scan failed for {root}: {e.Message}");
            }
        }

        private static string ShortReplayName(string path)
        {
            DirectoryInfo parent = Directory.GetParent(path);
            string name = parent != null ? parent.Name : Path.GetFileName(path);
            return $"{name}/{Path.GetFileName(path)}";
        }

        private TextMeshProUGUI CreateText(string name, Vector2 pos, Vector2 size, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.outlineColor = Color.black;
            text.outlineWidth = 0.2f;
            RectTransform rt = text.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return text;
        }

        private void CreateButton(string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            GameObject obj = new GameObject(label + "Button");
            obj.transform.SetParent(transform, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(70, 28);

            Image image = obj.AddComponent<Image>();
            image.color = new Color32(35, 40, 48, 230);
            Button button = obj.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            TextMeshProUGUI text = CreateText(label + "Label", Vector2.zero, new Vector2(70, 28), 13, TextAlignmentOptions.Center);
            text.transform.SetParent(obj.transform, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            text.text = label;
        }

        private Slider CreateSlider(string name, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction<float> onValueChanged)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Slider slider = obj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.onValueChanged.AddListener(onValueChanged);

            return slider;
        }

        private void CreateSpeedButton(string label, Vector2 pos, int speedIndex)
        {
            CreateButton(label, pos, () =>
            {
                ReplayController r = FindReplay();
                if (r != null) r.SetSpeed(speedIndex);
            });
        }
    }
}
#endif
