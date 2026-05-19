// Observer Mode patch 2: re-route ScreenshotCamera off the scene's
// TopViewOrthoCamera (which captures the orthographic top-down view that
// the user rejected) and onto the AAI3Agent prefab's observer_camera child.
//
// Idempotent — detects existing ScreenshotCamera on observer_camera and
// no-ops on rerun.
//
// Run: AIB → Reroute Screenshot Camera
// Headless: -executeMethod AIB.Editor.AbeScreenshotRouteFix.Reroute

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace AIB.Editor
{
    public static class AbeScreenshotRouteFix
    {
        private const string ScenePath = "Assets/Scenes/AAI3EnvironmentManager.unity";
        private const string AgentPrefabPath = "Assets/Prefabs/AAI3Agent.prefab";
        private const string ObserverName = "observer_camera";
        private const string TopViewName = "TopViewOrthoCamera";

        [MenuItem("AIB/Reroute Screenshot Camera")]
        public static void Reroute()
        {
            Debug.Log("[AIB] AbeScreenshotRouteFix begin.");

            // 1. Patch the scene: remove ScreenshotCamera from TopViewOrthoCamera.
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[AIB] Scene asset missing: {ScenePath}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            int removed = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == TopViewName)
                    {
                        var sc = t.GetComponent<ScreenshotCamera>();
                        if (sc != null)
                        {
                            Debug.Log($"[AIB] Removing ScreenshotCamera from scene {TopViewName}");
                            Object.DestroyImmediate(sc, true);
                            removed++;
                        }
                    }
                }
            }
            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[AIB] Saved scene with {removed} ScreenshotCamera removed.");
            }
            else
            {
                Debug.Log($"[AIB] No ScreenshotCamera on {TopViewName}; scene unchanged.");
            }

            // 2. Patch the prefab: ensure observer_camera has a ScreenshotCamera.
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(AgentPrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"[AIB] Cannot load prefab at {AgentPrefabPath}");
                return;
            }
            try
            {
                Transform obsT = prefabRoot.transform.Find(ObserverName);
                if (obsT == null)
                {
                    Debug.LogError($"[AIB] {ObserverName} child not found on AAI3Agent prefab");
                    return;
                }
                var existing = obsT.GetComponent<ScreenshotCamera>();
                if (existing != null)
                {
                    Debug.Log($"[AIB] ScreenshotCamera already on {ObserverName}; no add.");
                }
                else
                {
                    var sc = obsT.gameObject.AddComponent<ScreenshotCamera>();
                    sc.fileName = "observer_capture";
                    // AIB-screenshot-route-072 2026-05-07: canonical AIB SmokeRenders.
                    sc.filePath = "Assets/AIB/SmokeRenders";
                    Debug.Log($"[AIB] Added ScreenshotCamera to AAI3Agent/{ObserverName}");
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, AgentPrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AIB] AbeScreenshotRouteFix done.");
        }
    }
}
#endif
