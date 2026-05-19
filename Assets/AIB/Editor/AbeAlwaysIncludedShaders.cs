// Add Sprites/Mask + GUI/Text Shader to GraphicsSettings.AlwaysIncludedShaders.
// Required for headless server builds where these shaders would otherwise
// be stripped, causing UnityEngine.Material constructor to throw on null
// shader and cascading to SIGSEGV.

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.Collections.Generic;

namespace AIB.Editor
{
    public static class AbeAlwaysIncludedShaders
    {
        [MenuItem("AIB/Add Always Included Shaders")]
        public static void Add()
        {
            string[] required = new[]
            {
                "Sprites/Mask",
                "GUI/Text Shader",
                "Sprites/Default",
                "UI/Default",
                "UI/DefaultETC1",
                "Hidden/InternalErrorShader",
            };

            // Use SerializedObject on the GraphicsSettings asset.
            var so = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            var prop = so.FindProperty("m_AlwaysIncludedShaders");
            if (prop == null)
            {
                Debug.LogError("[AIB] m_AlwaysIncludedShaders property not found");
                return;
            }

            // Collect existing shader references
            var existing = new HashSet<string>();
            for (int i = 0; i < prop.arraySize; i++)
            {
                var elt = prop.GetArrayElementAtIndex(i);
                var s = elt.objectReferenceValue as Shader;
                if (s != null) existing.Add(s.name);
            }
            Debug.Log($"[AIB] Existing always-included shaders: {existing.Count}");
            foreach (string n in existing) Debug.Log($"[AIB]   already: {n}");

            int added = 0;
            foreach (string name in required)
            {
                if (existing.Contains(name))
                {
                    Debug.Log($"[AIB]   skip (already included): {name}");
                    continue;
                }
                var shader = Shader.Find(name);
                if (shader == null)
                {
                    Debug.LogWarning($"[AIB]   shader not found via Shader.Find: {name}");
                    continue;
                }
                prop.arraySize++;
                prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = shader;
                Debug.Log($"[AIB]   added: {name}");
                added++;
            }
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log($"[AIB] Added {added} always-included shaders.");
        }
    }
}
#endif
