// Patch AbeVisualController.cs to handle null shader in headless server
// build. Without this, AbeVisualController.Awake throws ArgumentNullException
// at `new Material(shader)` when Shader.Find returns null.
//
// Strategy: read the file, find every `new Material(<shader-expr>)` pattern,
// wrap with null check. Idempotent via marker.

#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

namespace AIB.Editor
{
    public static class AbeShaderNullPatch
    {
        [MenuItem("AIB/Patch Shader Null in AbeVisualController")]
        public static void Patch()
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath),
                "Assets/AIB/Runtime/AbeVisualController.cs");
            if (!File.Exists(path))
            {
                Debug.LogError($"[AIB] {path} not found");
                return;
            }
            string src = File.ReadAllText(path);
            const string marker = "// AIB-shader-null-patch-2026-05-08";
            if (src.Contains(marker))
            {
                Debug.Log("[AIB] AbeVisualController already patched.");
                return;
            }

            // Print the CreateDirectionIndicator method body so we know what to patch.
            int idx = src.IndexOf("CreateDirectionIndicator");
            if (idx < 0)
            {
                Debug.LogError("[AIB] CreateDirectionIndicator not found");
                return;
            }
            int methodStart = src.LastIndexOf("private void", idx);
            if (methodStart < 0) methodStart = src.LastIndexOf("void", idx);
            int methodEnd = idx;
            int braceDepth = 0;
            bool started = false;
            for (int i = methodStart; i < src.Length; i++)
            {
                if (src[i] == '{') { braceDepth++; started = true; }
                else if (src[i] == '}') { braceDepth--; if (started && braceDepth == 0) { methodEnd = i; break; } }
            }
            string snippet = src.Substring(methodStart, methodEnd - methodStart + 1);
            Debug.Log("[AIB] CreateDirectionIndicator BEFORE:\n" + snippet);

            // Simple guard: bail early on the entire AbeVisualController.Awake
            // when running headless (Application.isBatchMode || SystemInfo.graphicsDeviceID==0).
            // Insert at top of Awake().
            int awakeIdx = src.IndexOf("void Awake()");
            if (awakeIdx < 0) awakeIdx = src.IndexOf("void Awake ()");
            if (awakeIdx < 0)
            {
                Debug.LogError("[AIB] Awake() method not found");
                return;
            }
            int awakeBrace = src.IndexOf('{', awakeIdx);
            if (awakeBrace < 0) { Debug.LogError("[AIB] Awake brace not found"); return; }
            string guard =
                "\n            // " + marker + ": skip visual setup in headless server build.\n" +
                "            if (Application.isBatchMode || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)\n" +
                "            {\n" +
                "                Debug.Log(\"[AIB] AbeVisualController: skipping visual setup in headless mode\");\n" +
                "                return;\n" +
                "            }\n";
            string patched = src.Substring(0, awakeBrace + 1) + guard + src.Substring(awakeBrace + 1);

            // Backup
            File.WriteAllText(path + ".pre-shader-null.bak", src);
            File.WriteAllText(path, patched);
            Debug.Log($"[AIB] Patched {path} — Awake() now early-returns in headless mode.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif
