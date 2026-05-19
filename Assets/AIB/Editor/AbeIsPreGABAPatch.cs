// Reads AbeAnimationMapper.cs from the editor process (which has TCC
// scope for /Volumes/Video) and inserts an animator.SetBool("IsPreGABA", ...)
// call so the procedural ProneGlobalWave actually plays during early ticks.
//
// Idempotent: detects existing IsPreGABA marker and skips.
//
// Run: AIB → Patch IsPreGABA Wire-up
// Headless: -executeMethod AIB.Editor.AbeIsPreGABAPatch.PatchIsPreGABA

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace AIB.Editor
{
    public static class AbeIsPreGABAPatch
    {
        private const string MapperPath = "Assets/AIB/Runtime/AbeAnimationMapper.cs";
        private const string Marker = "// AIB-IsPreGABA-AUTOPATCH";
        private const int PreGABATickThreshold = 1000;

        [MenuItem("AIB/Patch IsPreGABA Wire-up")]
        public static void PatchIsPreGABA()
        {
            Debug.Log("[AIB] AbeIsPreGABAPatch begin.");
            string fullPath = Path.Combine(Application.dataPath, "AIB/Runtime/AbeAnimationMapper.cs");
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[AIB] Mapper file not found at {fullPath}");
                return;
            }

            string src = File.ReadAllText(fullPath);
            Debug.Log($"[AIB] Read {src.Length} chars from AbeAnimationMapper.cs");

            if (src.Contains(Marker))
            {
                Debug.Log("[AIB] Already patched (marker present); skipping.");
                return;
            }

            // Find the SetInteger(PhaseParam, phaseHash) line — the existing
            // mapper site that already runs per state update. Insert our
            // SetBool call right after it.
            string anchor = "animator.SetInteger(PhaseParam, phaseHash);";
            int idx = src.IndexOf(anchor);
            if (idx < 0)
            {
                Debug.LogError($"[AIB] Anchor '{anchor}' not found in mapper. Manual patch needed.");
                Debug.Log("[AIB] First 800 chars of mapper for inspection:");
                Debug.Log(src.Substring(0, System.Math.Min(800, src.Length)));
                return;
            }
            int endOfLine = src.IndexOf('\n', idx);
            if (endOfLine < 0) endOfLine = src.Length;

            string insert = $"\n            // AIB-IsPreGABA-AUTOPATCH 2026-05-07: pre-GABA global wave for tick<{PreGABATickThreshold}.\n" +
                            $"            animator.SetBool(\"IsPreGABA\", state.tick < {PreGABATickThreshold});";
            string patched = src.Substring(0, endOfLine) + insert + src.Substring(endOfLine);

            string backup = fullPath + ".pre-isPreGABA.bak";
            if (!File.Exists(backup)) File.WriteAllText(backup, src);
            File.WriteAllText(fullPath, patched);
            Debug.Log($"[AIB] Patched AbeAnimationMapper.cs (backup at {backup}).");
            Debug.Log($"[AIB] Inserted SetBool after byte offset {endOfLine} (length grew by {insert.Length}).");

            AssetDatabase.Refresh();
            Debug.Log("[AIB] AbeIsPreGABAPatch done.");
        }
    }
}
#endif
