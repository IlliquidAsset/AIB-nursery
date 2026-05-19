// Observer Mode patch set 1: minimal source-file edits for the
// fixes called out in .sisyphus/inbox/073:
//   1. ReplayController.cs — "ObserverCameraNE" → "observer_camera"
//   3. HUDManager.cs       — invert showHUD gate so Observer Mode shows the HUD
//   4. PlayerControls.cs   — guard the C-key camera cycle so it doesn't fight CameraController
//
// All edits use a literal-text replacement under a marker that prevents
// double-patching. Backups are written alongside the originals with a
// .pre-observerpatch.bak suffix.
//
// Run: AIB → Apply Observer Mode Patch Set 1
// Headless: -executeMethod AIB.Editor.AbeObserverModePatchSet1.ApplyAll

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace AIB.Editor
{
    public static class AbeObserverModePatchSet1
    {
        private const string ReplayPath = "Assets/AIB/Runtime/ReplayController.cs";
        private const string HUDPath = "Assets/AIB/Runtime/HUD/HUDManager.cs";
        private const string PlayerControlsPath = "Assets/Scripts/PlayerControls.cs";

        [MenuItem("AIB/Apply Observer Mode Patch Set 1")]
        public static void ApplyAll()
        {
            Debug.Log("[AIB] AbeObserverModePatchSet1 begin.");
            PatchReplay();
            PatchHUD();
            PatchPlayerControls();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AIB] AbeObserverModePatchSet1 done.");
        }

        private static void PatchReplay()
        {
            string p = ToFull(ReplayPath);
            if (!File.Exists(p)) { Debug.LogError($"[AIB] {p} missing"); return; }
            string src = File.ReadAllText(p);
            const string marker = "// AIB-observer-patch-replay";
            if (src.Contains(marker)) { Debug.Log("[AIB] ReplayController already patched."); return; }
            // Replace both occurrences:
            //   GameObject.Find("ObserverCameraNE")
            //   "ObserverCameraNE camera not found; recorder setup skipped."
            string before = src;
            src = src.Replace("\"ObserverCameraNE\"", "\"observer_camera\" " + marker);
            src = src.Replace("ObserverCameraNE camera not found", "observer_camera Camera not found");
            if (src == before) { Debug.LogWarning("[AIB] ReplayController: no replacements applied."); return; }
            BackupAndWrite(p, before, src);
            Debug.Log($"[AIB] Patched {ReplayPath}");
        }

        private static void PatchHUD()
        {
            string p = ToFull(HUDPath);
            if (!File.Exists(p)) { Debug.LogError($"[AIB] {p} missing"); return; }
            string src = File.ReadAllText(p);
            const string marker = "// AIB-observer-patch-hud";
            if (src.Contains(marker)) { Debug.Log("[AIB] HUDManager already patched."); return; }
            string old = "bool showHUD = !CameraController.BroadcastModeActive;";
            string nu  = "bool showHUD = CameraController.BroadcastModeActive; " + marker + " // inverted: HUD visible in Observer/Broadcast mode";
            if (!src.Contains(old))
            {
                Debug.LogWarning($"[AIB] HUDManager: anchor not found, manual patch needed.");
                return;
            }
            string before = src;
            src = src.Replace(old, nu);
            BackupAndWrite(p, before, src);
            Debug.Log($"[AIB] Patched {HUDPath}");
        }

        private static void PatchPlayerControls()
        {
            string p = ToFull(PlayerControlsPath);
            if (!File.Exists(p)) { Debug.LogError($"[AIB] {p} missing"); return; }
            string src = File.ReadAllText(p);
            const string marker = "// AIB-observer-patch-playercontrols";
            if (src.Contains(marker)) { Debug.Log("[AIB] PlayerControls already patched."); return; }
            string old = "if (canChangePerspective && Input.GetKeyDown(KeyCode.C))";
            string nu  = "if (canChangePerspective && !AIB.CameraController.BroadcastModeActive && Input.GetKeyDown(KeyCode.C)) " + marker;
            if (!src.Contains(old))
            {
                Debug.LogWarning($"[AIB] PlayerControls: anchor not found, manual patch needed.");
                return;
            }
            string before = src;
            src = src.Replace(old, nu);
            BackupAndWrite(p, before, src);
            Debug.Log($"[AIB] Patched {PlayerControlsPath}");
        }

        private static string ToFull(string assetPath)
        {
            string root = Path.GetDirectoryName(Application.dataPath); // strips trailing /Assets
            return Path.Combine(root, assetPath);
        }

        private static void BackupAndWrite(string fullPath, string before, string after)
        {
            string bak = fullPath + ".pre-observerpatch.bak";
            if (!File.Exists(bak)) File.WriteAllText(bak, before);
            File.WriteAllText(fullPath, after);
        }
    }
}
#endif
