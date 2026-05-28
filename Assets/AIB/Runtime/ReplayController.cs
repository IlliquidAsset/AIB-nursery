#if !EXPERIMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AIB;
using AIB.Runtime;
using UnityEngine;

#if UNITY_EDITOR && AIB_ENABLE_REPLAY_RECORDER
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
#endif

/// <summary>
/// CSV replay controller for observer/standalone workflows.
/// Priority -100 runs early to disable the arena system before Academy triggers an episode.
/// </summary>
[DefaultExecutionOrder(-100)]
public class ReplayController : MonoBehaviour
{
    [Header("Replay CSV")]
    [SerializeField] private string replayCsvPath = string.Empty;
    [SerializeField] private float interpolationT = 0.5f;
    [SerializeField] private float episodePauseSeconds = 1f;
    [SerializeField] private bool autoplayOnStart = true;
    [SerializeField] private float secondsPerStep = 0.08f;

    [Header("Replay Speeds")]
    [SerializeField] private float[] speedPresets = { 0.5f, 1f, 2f, 5f };
    [SerializeField] private int defaultSpeedIndex = 1;

    [Header("Scene References (optional)")]
    [SerializeField] private Transform agentTransform;
    [SerializeField] private GameObject motherGameObject;
    [SerializeField] private Camera observerCameraNE;

    private readonly List<ReplayRow> _rows = new List<ReplayRow>();
    private ReplayManifest _manifest;
    private VocalAudioPlayer _vocalAudio;

#if UNITY_EDITOR && AIB_ENABLE_REPLAY_RECORDER
    private RecorderController _recorderController;
#endif

    private bool _hasMotherPositionColumns;
    private bool _hasMotherActiveColumn;
    private bool _replayStarted;
    private int _currentIndex;
    private float _nextStepTime;
    private int _speedIndex;

    public bool IsLoaded => _rows.Count > 0;
    public bool IsPlaying { get; private set; }
    public int CurrentIndex => _currentIndex;
    public int RowCount => _rows.Count;
    public string CurrentReplayPath => replayCsvPath;
    public float Speed => _speedIndex >= 0 && _speedIndex < speedPresets.Length ? speedPresets[_speedIndex] : 1f;
    public int SpeedIndex => _speedIndex;
    public float NormalizedPosition => _rows.Count > 1 ? (float)_currentIndex / (_rows.Count - 1) : 0f;
    public int SpeedPresetCount => speedPresets.Length;
    public float GetSpeedPreset(int index) => index >= 0 && index < speedPresets.Length ? speedPresets[index] : 1f;

    private struct ReplayRow
    {
        public int Episode;
        public Vector3 AgentPosition;
        public Vector3 MotherPosition;
        public bool MotherActive;
        public AbeStatePayload Payload;
    }

    private void Awake()
    {
        ParseReplayCsvCliArg();

        if (!string.IsNullOrWhiteSpace(replayCsvPath))
        {
            DisableArenaSystem();
            EnsureReplayWindowSize();
        }
    }

    private void Start()
    {
        if (!ResolveReferences())
        {
            return;
        }

        _manifest = ReplayManifest.Load(replayCsvPath);

        if (!LoadCsvRows())
        {
            return;
        }

        if (_manifest != null)
        {
            ValidateManifestCompatibility();
        }

        PrepareVocalAudio();

        StartReplay();
    }

    private void PrepareVocalAudio()
    {
        _vocalAudio = FindFirstObjectByType<VocalAudioPlayer>();
        if (_vocalAudio == null)
        {
            GameObject vocalObj = new GameObject("VocalAudioPlayer");
            _vocalAudio = vocalObj.AddComponent<VocalAudioPlayer>();
        }

        if (_rows.Count == 0) return;

        float totalDuration = _rows.Count * 0.08f;
        _vocalAudio.PrepareClip(totalDuration);

        bool anyVocalData = false;
        foreach (ReplayRow row in _rows)
        {
            if (row.Payload == null) continue;

            anyVocalData |= row.Payload.hasVocalData;
            _vocalAudio.WriteSample(
                row.Payload.vocalPitch,
                row.Payload.vocalVolume,
                row.Payload.vocalFormant,
                row.Payload.vocalGate,
                1
            );
        }

        _vocalAudio.FinalizeClip();

        if (!anyVocalData)
        {
            Debug.Log("[ReplayController] No vocal data in replay. Audio will be silent.");
        }
        else
        {
            Debug.Log("[ReplayController] Vocal audio prepared from replay data.");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (IsPlaying) Pause(); else Play();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Pause();
            StepForward();
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Pause();
            StepBackward();
        }
        if (Input.GetKeyDown(KeyCode.Home))
        {
            ResetReplay();
        }

        if (!IsPlaying || _rows.Count == 0 || Time.unscaledTime < _nextStepTime)
        {
            return;
        }

        StepForward();
        _nextStepTime = Time.unscaledTime + Mathf.Max(0.01f, secondsPerStep / Speed);
    }

    private void ParseReplayCsvCliArg()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--replayCSV":
                    replayCsvPath = (i < args.Length - 1) ? args[i + 1] : replayCsvPath;
                    break;
            }
        }
    }

    private void DisableArenaSystem()
    {
        MonoBehaviour[] allComponents = FindObjectsOfType<MonoBehaviour>();
        int disabled = 0;

        foreach (MonoBehaviour comp in allComponents)
        {
            if (comp == null) continue;

            string typeName = comp.GetType().Name;

            if (typeName == "TrainingArena" || typeName == "TrainingAgent" || typeName == "Academy")
            {
                comp.enabled = false;
                disabled++;
            }
        }

        Debug.Log($"[ReplayController] Disabled {disabled} arena components. Observer replay mode active.");
    }

    private void EnsureReplayWindowSize()
    {
        if (Screen.width < 640 || Screen.height < 480)
        {
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            Debug.Log("[ReplayController] Set replay window to 1280x720 windowed.");
        }
    }

    private void ValidateManifestCompatibility()
    {
        if (_manifest == null) return;

        if (!_manifest.IsValid(out string invalidReason))
        {
            Debug.LogWarning($"[ReplayController] Manifest invalid: {invalidReason}");
            return;
        }

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string expectedScene = _manifest.stage.scene_name;

        System.Text.StringBuilder notice = new System.Text.StringBuilder();
        notice.AppendLine($"[ReplayController] Replay recorded with: {_manifest.recorded_with?.binary_name ?? "unknown"} ({_manifest.recorded_with?.platform ?? "unknown"})");

        if (!string.Equals(currentScene, expectedScene, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"[ReplayController] Stage mismatch: replay expects '{expectedScene}' but Observer is in '{currentScene}'. Visual fidelity not guaranteed.");
            notice.AppendLine($"Stage mismatch: replay expects '{expectedScene}', Observer is in '{currentScene}'.");
        }
        else
        {
            notice.AppendLine($"Stage match: {expectedScene} (v{_manifest.stage.stage_version})");
        }

        if (_manifest.schemas != null)
        {
            notice.AppendLine($"Telemetry schema v{_manifest.schemas.telemetry_schema_version}, Agent state schema v{_manifest.schemas.agent_state_schema_version}");
        }

        Debug.Log(notice.ToString());
    }

    private bool ResolveReferences()
    {
        if (agentTransform == null)
        {
            GameObject agentObject = GameObject.Find("AAI3Agent");
            if (agentObject != null)
            {
                agentTransform = agentObject.transform;
            }
        }

        if (motherGameObject == null)
        {
            GameObject motherObject = GameObject.Find("Mother");
            if (motherObject != null)
            {
                motherGameObject = motherObject;
            }
        }

        if (observerCameraNE == null)
        {
            // AIB-observer-patch-replay: prefer "observer_camera" (matches AAI3Agent prefab + arena_bridge sensor name); fall back to legacy "ObserverCameraNE".
            GameObject observer = GameObject.Find("observer_camera") ?? GameObject.Find("ObserverCameraNE");
            if (observer != null)
            {
                observerCameraNE = observer.GetComponent<Camera>();
            }
        }

        if (agentTransform == null)
        {
            Debug.LogError("ReplayController: Unable to locate AAI3Agent transform.");
            return false;
        }

        return true;
    }

    private bool LoadCsvRows()
    {
        if (string.IsNullOrWhiteSpace(replayCsvPath))
        {
            Debug.LogWarning("ReplayController: --replayCSV not provided; replay will not start.");
            return false;
        }

        if (!File.Exists(replayCsvPath))
        {
            Debug.LogError($"ReplayController: CSV file not found: {replayCsvPath}");
            return false;
        }

        string[] allLines = File.ReadAllLines(replayCsvPath);
        if (allLines.Length < 2)
        {
            Debug.LogError("ReplayController: CSV is empty or missing data rows.");
            return false;
        }

        List<string> headerFields = ParseCsvLine(allLines[0]);
        Dictionary<string, int> header = BuildHeaderMap(headerFields);

        if (!TryGetAnyIndex(header, out int episodeIndex, "Episode", "arena_episode", "episode")
            || !TryGetAnyIndex(header, out int xPosIndex, "XPosition", "aib_x", "posX")
            || !TryGetAnyIndex(header, out int yPosIndex, "YPosition", "aib_y", "posY")
            || !TryGetAnyIndex(header, out int zPosIndex, "ZPosition", "aib_z", "posZ"))
        {
            Debug.LogError("ReplayController: Missing required replay columns (Episode/XPosition/YPosition/ZPosition or arena_episode/aib_x/aib_y/aib_z).");
            return false;
        }

        bool hasMotherX = TryGetIndex(header, "MotherX", out int motherXIndex);
        bool hasMotherY = TryGetIndex(header, "MotherY", out int motherYIndex);
        bool hasMotherZ = TryGetIndex(header, "MotherZ", out int motherZIndex);
        _hasMotherPositionColumns = hasMotherX && hasMotherY && hasMotherZ;
        _hasMotherActiveColumn = TryGetIndex(header, "MotherActive", out int motherActiveIndex);

        if (!_hasMotherPositionColumns)
        {
            Debug.LogWarning("ReplayController: Mother position columns not found; mother movement playback disabled.");
        }

        if (!_hasMotherActiveColumn)
        {
            Debug.LogWarning("ReplayController: MotherActive column not found; mother visibility playback disabled.");
        }

        _rows.Clear();
        for (int i = 1; i < allLines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(allLines[i]))
            {
                continue;
            }

            List<string> fields = ParseCsvLine(allLines[i]);

            Vector3 agentPosition = new Vector3(
                ParseFloat(GetField(fields, xPosIndex), 0f),
                ParseFloat(GetField(fields, yPosIndex), 0f),
                ParseFloat(GetField(fields, zPosIndex), 0f)
            );
            ReplayRow row = new ReplayRow
            {
                Episode = ParseInt(GetField(fields, episodeIndex), 0),
                AgentPosition = agentPosition,
                MotherPosition = Vector3.zero,
                MotherActive = true,
                Payload = BuildPayload(fields, header, i - 1, agentPosition, ParseInt(GetField(fields, episodeIndex), 0))
            };

            if (_hasMotherPositionColumns)
            {
                row.MotherPosition = new Vector3(
                    ParseFloat(GetField(fields, motherXIndex), 0f),
                    ParseFloat(GetField(fields, motherYIndex), 0f),
                    ParseFloat(GetField(fields, motherZIndex), 0f)
                );
            }

            if (_hasMotherActiveColumn)
            {
                row.MotherActive = ParseBool(GetField(fields, motherActiveIndex), true);
            }

            _rows.Add(row);
        }

        if (_rows.Count == 0)
        {
            Debug.LogError("ReplayController: No valid replay rows were loaded.");
            return false;
        }

        Debug.Log($"ReplayController: Replay CSV loaded: {replayCsvPath} rows={_rows.Count}");
        return true;
    }

    private void StartReplay()
    {
        if (_replayStarted)
        {
            return;
        }

        _replayStarted = true;
        _speedIndex = defaultSpeedIndex;
        ConfigureAndStartRecorder();
        _currentIndex = 0;
        ApplyDirect(_rows[0]);
        IsPlaying = autoplayOnStart;
        _nextStepTime = Time.unscaledTime + Mathf.Max(0.01f, secondsPerStep / Speed);
    }

    public bool LoadReplayCsv(string path)
    {
        replayCsvPath = path;
        _rows.Clear();
        _replayStarted = false;
        IsPlaying = false;
        _currentIndex = 0;
        if (!LoadCsvRows()) return false;
        StartReplay();
        return true;
    }

    public void Play()
    {
        if (_rows.Count == 0) return;
        IsPlaying = true;
        _nextStepTime = Time.unscaledTime;
        _vocalAudio?.Play();
    }

    public void Pause()
    {
        IsPlaying = false;
        _vocalAudio?.Stop();
    }

    public void ResetReplay()
    {
        if (_rows.Count == 0) return;
        IsPlaying = false;
        _currentIndex = 0;
        ApplyDirect(_rows[_currentIndex]);
        _vocalAudio?.Stop();
    }

    public void SetSpeed(int presetIndex)
    {
        _speedIndex = Mathf.Clamp(presetIndex, 0, speedPresets.Length - 1);
    }

    public void Seek(float normalizedPosition)
    {
        if (_rows.Count == 0) return;

        normalizedPosition = Mathf.Clamp01(normalizedPosition);
        _currentIndex = Mathf.Clamp(Mathf.RoundToInt(normalizedPosition * (_rows.Count - 1)), 0, _rows.Count - 1);
        ApplyDirect(_rows[_currentIndex]);
        _nextStepTime = Time.unscaledTime;
    }

    public void StepForward()
    {
        if (_rows.Count == 0) return;
        if (_currentIndex >= _rows.Count - 1)
        {
            IsPlaying = false;
            StopRecorder();
            return;
        }

        ReplayRow previous = _rows[_currentIndex];
        _currentIndex += 1;
        ReplayRow current = _rows[_currentIndex];
        if (current.Episode != previous.Episode)
        {
            ApplyDirect(current);
            _nextStepTime = Time.unscaledTime + Mathf.Max(episodePauseSeconds, secondsPerStep);
        }
        else
        {
            ApplyInterpolated(previous, current);
        }
    }

    public void StepBackward()
    {
        if (_rows.Count == 0) return;
        _currentIndex = Mathf.Max(0, _currentIndex - 1);
        ApplyDirect(_rows[_currentIndex]);
    }

    private void ApplyDirect(ReplayRow row)
    {
        agentTransform.position = row.AgentPosition;

        if (motherGameObject != null)
        {
            if (_hasMotherPositionColumns)
            {
                motherGameObject.transform.position = row.MotherPosition;
            }

            if (_hasMotherActiveColumn)
            {
                motherGameObject.SetActive(row.MotherActive);
            }
        }
        WriteReplayState(row.Payload);
    }

    private void ApplyInterpolated(ReplayRow from, ReplayRow to)
    {
        float t = Mathf.Clamp01(interpolationT);
        agentTransform.position = Vector3.Lerp(from.AgentPosition, to.AgentPosition, t);

        if (motherGameObject != null)
        {
            if (_hasMotherPositionColumns)
            {
                motherGameObject.transform.position = Vector3.Lerp(from.MotherPosition, to.MotherPosition, t);
            }

            if (_hasMotherActiveColumn)
            {
                motherGameObject.SetActive(to.MotherActive);
            }
        }
        WriteReplayState(to.Payload);
    }

    private void WriteReplayState(AbeStatePayload payload)
    {
        if (payload == null) return;
        AbeStateBuffer.Instance.ConnectionStatus = "Replay CSV";
        AbeStateBuffer.Instance.IsConnected = false;
        AbeStateBuffer.Instance.Write(payload);
        AbeStateBuffer.Instance.SwapBuffers();
    }

#if UNITY_EDITOR && AIB_ENABLE_REPLAY_RECORDER
    private string _previousObserverCameraTag;

    private void ConfigureAndStartRecorder()
    {
        if (observerCameraNE == null)
        {
            Debug.LogWarning("ReplayController: ObserverCameraNE camera not found; recorder setup skipped.");
            return;
        }

        string basePath = Application.isEditor
            ? Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
            : Path.GetDirectoryName(Application.dataPath);

        string replayDirectory = Path.Combine(basePath, "Builds", "Replays");
        Directory.CreateDirectory(replayDirectory);

        var movieRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieRecorder.name = "Replay MP4 Recorder";
        movieRecorder.Enabled = true;
        movieRecorder.OutputFile = Path.Combine(replayDirectory, $"Replay_{DateTime.Now:yyyyMMdd_HHmmss}");
        movieRecorder.CaptureAudio = false;

        _previousObserverCameraTag = observerCameraNE.tag;
        observerCameraNE.tag = "MainCamera";

        movieRecorder.ImageInputSettings = new CameraInputSettings
        {
            Source = ImageSource.MainCamera,
            CameraTag = "MainCamera",
            OutputWidth = 1920,
            OutputHeight = 1080
        };
        movieRecorder.EncoderSettings = new CoreEncoderSettings
        {
            Codec = CoreEncoderSettings.OutputCodec.H264,
            EncodingProfile = CoreEncoderSettings.H264EncodingProfile.Auto,
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High
        };

        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
        controllerSettings.FrameRate = 30.0f;
        controllerSettings.CapFrameRate = true;
        controllerSettings.AddRecorderSettings(movieRecorder);

        _recorderController = new RecorderController(controllerSettings);
        _recorderController.PrepareRecording();
        _recorderController.StartRecording();
    }

    private void StopRecorder()
    {
        if (_recorderController != null && _recorderController.IsRecording())
        {
            _recorderController.StopRecording();
        }

        if (observerCameraNE != null && !string.IsNullOrEmpty(_previousObserverCameraTag))
        {
            observerCameraNE.tag = _previousObserverCameraTag;
        }
    }
#else
    private void ConfigureAndStartRecorder() { }
    private void StopRecorder() { }
#endif

    private static Dictionary<string, int> BuildHeaderMap(List<string> headerFields)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < headerFields.Count; i++)
        {
            string key = headerFields[i].Trim();
            if (!map.ContainsKey(key))
            {
                map.Add(key, i);
            }
        }

        return map;
    }

    private static bool TryGetIndex(Dictionary<string, int> map, string key, out int index)
    {
        return map.TryGetValue(key, out index);
    }

    private static bool TryGetAnyIndex(Dictionary<string, int> map, out int index, params string[] keys)
    {
        foreach (string key in keys)
        {
            if (map.TryGetValue(key, out index))
            {
                return true;
            }
        }

        index = -1;
        return false;
    }

    private static string GetOptionalField(List<string> fields, Dictionary<string, int> header, params string[] keys)
    {
        return TryGetAnyIndex(header, out int index, keys) ? GetField(fields, index) : string.Empty;
    }

    private static AbeStatePayload BuildPayload(
        List<string> fields,
        Dictionary<string, int> header,
        int rowIndex,
        Vector3 agentPosition,
        int episode
    )
    {
        var payload = AbeStatePayload.Default();
        payload.Position = agentPosition;
        payload.rotationY = ParseFloat(GetOptionalField(fields, header, "rotationY", "RotationY", "yaw"), 0f);
        payload.currentActionForward = ParseInt(GetOptionalField(fields, header, "currentActionForward", "action_forward", "movement", "action"), 0);
        payload.currentActionRotate = ParseInt(GetOptionalField(fields, header, "currentActionRotate", "action_rotate", "turn"), 0);
        payload.health = ParseFloat(GetOptionalField(fields, header, "health", "Health"), 100f);
        payload.deaths = ParseInt(GetOptionalField(fields, header, "deaths", "Deaths"), 0);
        payload.episode = episode;
        payload.lavaDistance = ParseFloat(GetOptionalField(fields, header, "lavaDistance", "lava_distance", "LavaDistance"), 0f);
        payload.lavaDistanceDelta = ParseFloat(GetOptionalField(fields, header, "lavaDistanceDelta", "lava_distance_delta", "LavaDistanceDelta"), 0f);
        payload.dopamine = ParseFloat(GetOptionalField(fields, header, "dopamine"), 0f);
        payload.cortisol = ParseFloat(GetOptionalField(fields, header, "cortisol"), 0f);
        payload.oxytocin = ParseFloat(GetOptionalField(fields, header, "oxytocin"), 0f);
        payload.serotonin = ParseFloat(GetOptionalField(fields, header, "serotonin"), 0f);
        payload.norepinephrine = ParseFloat(GetOptionalField(fields, header, "norepinephrine"), 0f);
        payload.endorphins = ParseFloat(GetOptionalField(fields, header, "endorphins"), 0f);
        payload.curiosity = ParseFloat(GetOptionalField(fields, header, "curiosity"), 0f);
        payload.stress = ParseFloat(GetOptionalField(fields, header, "stress"), 0f);
        payload.plasticity = ParseFloat(GetOptionalField(fields, header, "plasticity"), 0f);
        payload.alertness = ParseFloat(GetOptionalField(fields, header, "alertness"), 0f);
        payload.focus = ParseFloat(GetOptionalField(fields, header, "focus"), 0f);
        payload.inhibition = ParseFloat(GetOptionalField(fields, header, "inhibition"), 0f);
        payload.bonding = ParseFloat(GetOptionalField(fields, header, "bonding"), 0f);
        payload.predictionError = ParseFloat(GetOptionalField(fields, header, "predictionError", "prediction_error"), 0f);
        payload.rewardThisTick = ParseFloat(GetOptionalField(fields, header, "rewardThisTick", "reward_to_network", "reward"), 0f);
        payload.naturalReward = ParseFloat(GetOptionalField(fields, header, "naturalReward", "native_reward"), 0f);
        payload.shapedReward = ParseFloat(GetOptionalField(fields, header, "shapedReward", "shaped_reward"), 0f);
        payload.motherStrength = ParseFloat(GetOptionalField(fields, header, "motherStrength", "mother_strength"), 0f);
        payload.tick = ParseInt(GetOptionalField(fields, header, "tick", "Tick"), rowIndex);
        string phase = GetOptionalField(fields, header, "phase", "Phase");
        payload.phase = string.IsNullOrWhiteSpace(phase) ? "REPLAY" : phase;

        payload.vocalPitch = ParseFloat(GetOptionalField(fields, header, "vocal_pitch"), 0f);
        payload.vocalVolume = ParseFloat(GetOptionalField(fields, header, "vocal_volume"), 0f);
        payload.vocalFormant = ParseFloat(GetOptionalField(fields, header, "vocal_formant"), 2.5f);
        payload.vocalGate = ParseBool(GetOptionalField(fields, header, "vocal_gate"), false);
        payload.vocalDistressCry = ParseBool(GetOptionalField(fields, header, "vocal_distress_cry"), false);
        payload.hasVocalData = TryGetIndex(header, "vocal_pitch", out _);

        return payload;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        if (line == null)
        {
            fields.Add(string.Empty);
            return fields;
        }

        bool inQuotes = false;
        int start = 0;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(UnwrapCsvField(line.Substring(start, i - start)));
                start = i + 1;
            }
        }

        fields.Add(UnwrapCsvField(line.Substring(start)));
        return fields;
    }

    private static string UnwrapCsvField(string field)
    {
        string value = field.Trim();
        if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
        {
            value = value.Substring(1, value.Length - 2);
        }

        return value.Replace("\"\"", "\"");
    }

    private static string GetField(List<string> fields, int index)
    {
        if (index < 0 || index >= fields.Count)
        {
            return string.Empty;
        }

        return fields[index];
    }

    private static int ParseInt(string raw, int fallback)
    {
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : fallback;
    }

    private static float ParseFloat(string raw, float fallback)
    {
        return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
            ? value
            : fallback;
    }

    private static bool ParseBool(string raw, bool fallback)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        if (bool.TryParse(raw, out bool parsedBool))
        {
            return parsedBool;
        }

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedInt))
        {
            return parsedInt != 0;
        }

        return fallback;
    }
}
#endif
