using System;
using System.IO;
using UnityEngine;

namespace AIB.Runtime
{
    [Serializable]
    public class ReplayManifest
    {
        public int replay_schema_version = 1;

        public RecordedWith recorded_with = new RecordedWith();
        public StageInfo stage = new StageInfo();
        public LayoutInfo layout = new LayoutInfo();
        public SchemaInfo schemas = new SchemaInfo();

        [Serializable]
        public class RecordedWith
        {
            public string binary_name = "";
            public string platform = "";
            public string binary_build_id = "";
            public string git_commit = "";
        }

        [Serializable]
        public class StageInfo
        {
            public string stage_id = "";
            public int stage_version = 1;
            public string scene_name = "";
        }

        [Serializable]
        public class LayoutInfo
        {
            public string layout_id = "";
            public int layout_schema_version = 1;
            public int layout_seed = 0;
            public string layout_manifest_hash = "";
        }

        [Serializable]
        public class SchemaInfo
        {
            public int telemetry_schema_version = 1;
            public int agent_state_schema_version = 1;
        }

        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(stage?.scene_name))
            {
                reason = "Manifest missing stage.scene_name";
                return false;
            }

            if (string.IsNullOrWhiteSpace(stage?.stage_id))
            {
                reason = "Manifest missing stage.stage_id";
                return false;
            }

            reason = "";
            return true;
        }

        public static ReplayManifest Load(string replayCsvPath)
        {
            if (string.IsNullOrWhiteSpace(replayCsvPath))
                return null;

            string directory = Path.GetDirectoryName(replayCsvPath);
            if (string.IsNullOrWhiteSpace(directory))
                directory = ".";

            string manifestPath = Path.Combine(directory, "manifest.json");

            if (!File.Exists(manifestPath))
            {
                Debug.Log($"[ReplayManifest] No manifest found at {manifestPath}; replaying without stage identity.");
                return null;
            }

            try
            {
                string json = File.ReadAllText(manifestPath);
                ReplayManifest manifest = JsonUtility.FromJson<ReplayManifest>(json);

                if (manifest == null)
                {
                    Debug.LogWarning($"[ReplayManifest] Failed to parse {manifestPath}");
                    return null;
                }

                Debug.Log($"[ReplayManifest] Loaded: stage={manifest.stage?.stage_id}, scene={manifest.stage?.scene_name}");
                return manifest;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ReplayManifest] Error loading manifest: {ex.Message}");
                return null;
            }
        }
    }
}
