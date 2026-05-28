using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIB.Runtime
{
    public class NurseryBootstrap : MonoBehaviour
{
    private const string StageArg = "--nursery-stage";

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        string stageId = ParseStageArg();

        if (string.IsNullOrWhiteSpace(stageId))
        {
            Debug.LogError("[NurseryBootstrap] No --nursery-stage argument provided. Usage: --nursery-stage <stage_id>");
            Application.Quit(1);
            return;
        }

        NurseryStageRegistry registry = LoadRegistry();

        if (registry == null)
        {
            Debug.LogError("[NurseryBootstrap] NurseryStageRegistry asset not found. Place one in a Resources folder or assign in inspector.");
            Application.Quit(1);
            return;
        }

        if (!registry.TryGetStage(stageId, out StageEntry entry))
        {
            Debug.LogError($"[NurseryBootstrap] Unknown stage: '{stageId}'. Registered stages: {string.Join(", ", StageIds(registry))}");
            Application.Quit(1);
            return;
        }

        Debug.Log($"[NurseryBootstrap] Loading stage: {stageId} -> scene: {entry.sceneName} (behavior: {entry.behaviorName})");
        SceneManager.LoadScene(entry.sceneName);
    }

    private static string ParseStageArg()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], StageArg, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }

    private static NurseryStageRegistry LoadRegistry()
    {
        NurseryStageRegistry registry = Resources.Load<NurseryStageRegistry>("NurseryStageRegistry");

        if (registry == null)
        {
            Debug.LogWarning("[NurseryBootstrap] Registry not found via Resources. Searching all loaded assets...");
            NurseryStageRegistry[] all = Resources.FindObjectsOfTypeAll<NurseryStageRegistry>();
            if (all != null && all.Length > 0)
                registry = all[0];
        }

        return registry;
    }

    private static string[] StageIds(NurseryStageRegistry registry)
    {
        if (registry?.Stages == null || registry.Stages.Count == 0)
            return new[] { "(none)" };

        string[] ids = new string[registry.Stages.Count];
        for (int i = 0; i < registry.Stages.Count; i++)
            ids[i] = registry.Stages[i].stageId;

        return ids;
    }
    }
}
