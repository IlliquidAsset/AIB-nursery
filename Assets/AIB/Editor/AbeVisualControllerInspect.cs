// Inspect AbeVisualController.cs CreateDirectionIndicator method via
// editor file read (Unity has TCC scope where Bash doesn't).

#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

namespace AIB.Editor
{
    public static class AbeVisualControllerInspect
    {
        [MenuItem("AIB/Inspect AbeVisualController")]
        public static void Inspect()
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath),
                "Assets/AIB/Runtime/AbeVisualController.cs");
            if (!File.Exists(path))
            {
                Debug.LogError($"[AIB] {path} not found");
                return;
            }
            string src = File.ReadAllText(path);
            int idx = src.IndexOf("CreateDirectionIndicator");
            if (idx < 0)
            {
                Debug.LogError("[AIB] CreateDirectionIndicator not found in source");
                return;
            }
            // Print 60 lines around the method
            int start = src.LastIndexOf('\n', idx);
            int end = src.IndexOf('\n', idx + 60 * 80);
            if (end < 0) end = src.Length;
            Debug.Log("[AIB] Source extract:\n" + src.Substring(start, end - start));
        }
    }
}
#endif
