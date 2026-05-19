// Adds a third-person observer_camera GameObject + Camera + CameraSensor
// to the AAI3Agent prefab. Idempotent: detects an existing observer_camera
// child and skips if present.
//
// Run from menu: AIB → Add Observer Camera
// Or headless:   Unity -batchmode -quit -projectPath ... -executeMethod AIB.Editor.AbeObserverCameraSetup.AddObserverCamera

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace AIB.Editor
{
    public static class AbeObserverCameraSetup
    {
        private const string PrefabPath = "Assets/Prefabs/AAI3Agent.prefab";
        private const string ObserverName = "observer_camera";

        [MenuItem("AIB/Add Observer Camera")]
        public static void AddObserverCamera()
        {
            Debug.Log("[AIB] AddObserverCamera begin.");

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"[AIB] Cannot load prefab at {PrefabPath}");
                return;
            }

            try
            {
                Transform existing = prefabRoot.transform.Find(ObserverName);
                if (existing != null)
                {
                    Debug.Log($"[AIB] {ObserverName} already exists on AAI3Agent prefab. No change.");
                    return;
                }

                GameObject obs = new GameObject(ObserverName);
                obs.transform.SetParent(prefabRoot.transform, worldPositionStays: false);

                // Third-person view: a few meters back and above the agent.
                obs.transform.localPosition = new Vector3(0f, 2.5f, -4.0f);
                obs.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);

                Camera cam = obs.AddComponent<Camera>();
                cam.fieldOfView = 60f;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 100f;
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.depth = -1f;  // render behind agent's main first-person cameras

                // Try to add a UnityCameraSensorComponent if ML-Agents is in this
                // project. If the type isn't found, log a warning and continue;
                // the Camera alone is still usable via RenderTexture for clips.
                System.Type sensorType = System.Type.GetType("Unity.MLAgents.Sensors.CameraSensorComponent, Unity.ML-Agents")
                    ?? System.Type.GetType("Unity.MLAgents.Sensors.CameraSensorComponent");
                if (sensorType != null)
                {
                    Component sensor = obs.AddComponent(sensorType);
                    SetField(sensor, "m_Camera", cam);
                    SetField(sensor, "m_SensorName", ObserverName);
                    SetField(sensor, "m_Width", 256);
                    SetField(sensor, "m_Height", 256);
                    SetField(sensor, "m_Grayscale", false);
                    Debug.Log($"[AIB] Added {sensorType.FullName} component named {ObserverName}.");
                }
                else
                {
                    Debug.LogWarning("[AIB] ML-Agents CameraSensorComponent type not found by reflection. Camera added without sensor; consumer can attach a RenderTexture to capture.");
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                Debug.Log($"[AIB] Saved {PrefabPath} with {ObserverName} child.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AIB] AddObserverCamera done.");
        }

        private static void SetField(Component c, string name, object value)
        {
            FieldInfo f = c.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null)
            {
                f.SetValue(c, value);
                return;
            }
            PropertyInfo p = c.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && p.CanWrite)
            {
                p.SetValue(c, value);
                return;
            }
            Debug.LogWarning($"[AIB] Could not set {name} on {c.GetType().FullName}");
        }
    }
}
#endif
