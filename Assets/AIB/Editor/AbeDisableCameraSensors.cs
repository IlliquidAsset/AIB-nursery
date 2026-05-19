// Disable ALL CameraSensorComponents on AAI3Agent.prefab so the headless
// build doesn't try to allocate GPU resources for them. Last-ditch attempt
// to get past Unity's Null GPU driver SIGSEGV on aib-arena.

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace AIB.Editor
{
    public static class AbeDisableCameraSensors
    {
        private const string PrefabPath = "Assets/Prefabs/AAI3Agent.prefab";

        [MenuItem("AIB/Disable Camera Sensors (headless)")]
        public static void Disable()
        {
            Debug.Log("[AIB] DisableCameraSensors begin.");
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"[AIB] Cannot load prefab at {PrefabPath}");
                return;
            }
            try
            {
                Type sensorType = Type.GetType("Unity.MLAgents.Sensors.CameraSensorComponent, Unity.ML-Agents")
                    ?? Type.GetType("Unity.MLAgents.Sensors.CameraSensorComponent");
                if (sensorType == null)
                {
                    Debug.LogError("[AIB] CameraSensorComponent type not found");
                    return;
                }
                int disabled = 0;
                foreach (Component c in prefabRoot.GetComponentsInChildren(sensorType, true))
                {
                    Behaviour b = c as Behaviour;
                    if (b != null && b.enabled)
                    {
                        b.enabled = false;
                        Debug.Log($"[AIB]   disabled CameraSensorComponent on {GetPath(c.transform)}");
                        disabled++;
                    }
                }
                int camDisabled = 0;
                foreach (Camera cam in prefabRoot.GetComponentsInChildren<Camera>(true))
                {
                    if (cam.enabled)
                    {
                        cam.enabled = false;
                        Debug.Log($"[AIB]   disabled Camera on {GetPath(cam.transform)}");
                        camDisabled++;
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                Debug.Log($"[AIB] Disabled {disabled} CameraSensors, {camDisabled} Cameras.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AIB] DisableCameraSensors done.");
        }

        private static string GetPath(Transform t)
        {
            string p = t.name;
            for (Transform x = t.parent; x != null; x = x.parent) p = x.name + "/" + p;
            return p;
        }
    }
}
#endif
