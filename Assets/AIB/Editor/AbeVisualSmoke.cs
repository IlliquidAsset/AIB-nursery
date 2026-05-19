// Editor-time visual smoke: instantiates the AAI3Agent prefab into a
// temporary scene, snaps a still through observer_camera, saves PNG.
// No play-mode, no Lightning, no battery — pure editor render.
//
// Run: AIB → Visual Smoke (Render observer_camera)
// Headless: -executeMethod AIB.Editor.AbeVisualSmoke.RenderSmoke
//
// Output: Assets/AIB/SmokeRenders/observer_camera_<timestamp>.png

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

namespace AIB.Editor
{
    public static class AbeVisualSmoke
    {
        private const string PrefabPath = "Assets/Prefabs/AAI3Agent.prefab";
        private const string OutDir = "Assets/AIB/SmokeRenders";
        private const string ObserverName = "observer_camera";
        private const int Width = 512;
        private const int Height = 512;

        [MenuItem("AIB/Visual Smoke — Pre-GABA Wave Sequence")]
        public static void RenderProneWaveSequence()
        {
            Debug.Log("[AIB] RenderProneWaveSequence begin.");
            EnsureOutDir();

            var clipPath = "Assets/AIB/Animations/ProneGlobalWave.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                Debug.LogError($"[AIB] No clip at {clipPath}");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            try
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

                GameObject abeMeshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AIB/Prefabs/AbeVisualMesh.prefab");
                Transform agentChild = inst.transform.Find("Agent");
                Transform parent = agentChild != null ? agentChild : inst.transform;
                GameObject mesh = (GameObject)PrefabUtility.InstantiatePrefab(abeMeshPrefab, parent);
                mesh.transform.localPosition = Vector3.zero;
                mesh.transform.localRotation = Quaternion.identity;

                foreach (Transform t in inst.GetComponentsInChildren<Transform>(true))
                    if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);

                Transform obsT = inst.transform.Find(ObserverName);
                Camera obsCam = obsT.GetComponent<Camera>();
                obsCam.enabled = true;

                Vector3 charCenter = Vector3.zero;
                foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    smr.updateWhenOffscreen = true;
                    charCenter = smr.bounds.center;
                    break;
                }
                obsT.position = charCenter + new Vector3(0f, 2f, -5f);
                obsT.LookAt(charCenter);

                GameObject lightGO = new GameObject("SmokeLight");
                Light light = lightGO.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

                Animator animator = mesh.GetComponentInChildren<Animator>();
                if (animator == null)
                {
                    Debug.LogError("[AIB] No Animator on AbeVisualMesh");
                    return;
                }

                float[] sampleTimes = { 0.00f, 0.125f, 0.25f, 0.375f, 0.50f, 0.625f, 0.75f, 0.875f };
                // Diagnostic: directly rotate the limb bones to prove the rig
                // accepts pose changes. Bypasses AnimationMode entirely.
                Transform armRoot = animator.transform;
                string[] limbPaths = {
                    "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm",
                    "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm",
                    "Armature/Hips/LeftUpLeg",
                    "Armature/Hips/RightUpLeg",
                };
                foreach (string p in limbPaths)
                {
                    Transform bt = armRoot.Find(p);
                    Debug.Log($"[AIB]   bone-find '{p}' → {(bt != null ? "FOUND " + bt.name : "NULL")}");
                }
                AnimationMode.StartAnimationMode();
                try
                {
                    string stamp = System.DateTime.UtcNow.ToString("yyyyMMddTHHmmss");
                    int idx = 0;
                    foreach (float t in sampleTimes)
                    {
                        // BIG sanity rotation: 90° per frame to test whether
                        // the renderer is reading bone updates AT ALL.
                        float bigAngle = idx * 30f;  // 0, 30, 60, 90, 120, 150, 180, 210
                        foreach (string p in limbPaths)
                        {
                            Transform bt = armRoot.Find(p);
                            if (bt != null)
                            {
                                bt.localEulerAngles = new Vector3(bigAngle, 0f, 0f);
                            }
                        }
                        // Force the SkinnedMeshRenderer to rebake from the
                        // new bone pose before rendering.
                        foreach (var smr in animator.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        {
                            Mesh tmp = new Mesh();
                            smr.BakeMesh(tmp, useScale: true);
                            Object.DestroyImmediate(tmp);
                        }
                        AnimationMode.BeginSampling();
                        AnimationMode.SampleAnimationClip(animator.gameObject, clip, t);
                        AnimationMode.EndSampling();
                        // Force renderers to recompute bounds from sampled pose.
                        foreach (var smr in animator.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        {
                            smr.updateWhenOffscreen = true;
                        }

                        RenderTexture rt = RenderTexture.GetTemporary(Width, Height, 24);
                        obsCam.targetTexture = rt;
                        obsCam.Render();
                        obsCam.targetTexture = null;
                        RenderTexture prev = RenderTexture.active;
                        RenderTexture.active = rt;
                        Texture2D tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                        tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                        tex.Apply();
                        RenderTexture.active = prev;
                        RenderTexture.ReleaseTemporary(rt);

                        byte[] png = tex.EncodeToPNG();
                        Object.DestroyImmediate(tex);

                        string outPath = Path.Combine(OutDir, $"prone_wave_{stamp}_frame{idx:D2}_t{t:F3}.png");
                        File.WriteAllBytes(outPath, png);
                        Debug.Log($"[AIB]   frame {idx} t={t:F3} → {outPath} ({png.Length} bytes)");
                        idx++;
                    }
                }
                finally
                {
                    AnimationMode.StopAnimationMode();
                }
            }
            finally
            {
            }

            AssetDatabase.Refresh();
            Debug.Log("[AIB] RenderProneWaveSequence done.");
        }

        private static void EnsureOutDir()
        {
            if (!Directory.Exists(OutDir))
                Directory.CreateDirectory(OutDir);
        }

        [MenuItem("AIB/Visual Smoke (Render observer_camera)")]
        public static void RenderSmoke()
        {
            Debug.Log("[AIB] AbeVisualSmoke begin.");

            if (!Directory.Exists(OutDir))
            {
                Directory.CreateDirectory(OutDir);
            }

            // Open a fresh untitled scene so we don't perturb the active project scene.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            try
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab == null)
                {
                    Debug.LogError($"[AIB] Cannot load prefab at {PrefabPath}");
                    return;
                }

                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.transform.position = Vector3.zero;
                inst.transform.rotation = Quaternion.identity;

                // AbeVisualController instantiates AbeVisualMesh at runtime in
                // Awake/Start, but editor-time rendering skips lifecycle. Force
                // an instance now so the mesh is in the scene before Render().
                const string AbeMeshPath = "Assets/AIB/Prefabs/AbeVisualMesh.prefab";
                GameObject abeMeshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AbeMeshPath);
                if (abeMeshPrefab != null)
                {
                    Transform agentChild = inst.transform.Find("Agent");
                    Transform parent = agentChild != null ? agentChild : inst.transform;
                    GameObject mesh = (GameObject)PrefabUtility.InstantiatePrefab(abeMeshPrefab, parent);
                    mesh.transform.localPosition = Vector3.zero;
                    mesh.transform.localRotation = Quaternion.identity;
                    int meshSmrCount = mesh.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
                    int meshMrCount = mesh.GetComponentsInChildren<MeshRenderer>(true).Length;
                    Debug.Log($"[AIB] Instantiated AbeVisualMesh under {(agentChild ? "Agent" : "AAI3Agent")}: {meshSmrCount} SMRs, {meshMrCount} MRs in subtree.");
                }
                else
                {
                    Debug.LogWarning($"[AIB] {AbeMeshPath} not found; smoke render will not show Abe mesh.");
                }

                Transform obsT = inst.transform.Find(ObserverName);
                if (obsT == null)
                {
                    Debug.LogError($"[AIB] {ObserverName} child not found on instantiated prefab.");
                    return;
                }
                Camera obsCam = obsT.GetComponent<Camera>();
                if (obsCam == null)
                {
                    Debug.LogError($"[AIB] No Camera component on {ObserverName} child.");
                    return;
                }
                obsCam.enabled = true;

                // Find the actual rendered character via SkinnedMeshRenderer
                // bounds — the AAI3Agent prefab nests Abe at a non-zero
                // offset that doesn't move when we set the prefab root to
                // origin. Compute the camera position relative to Abe's
                // real bounds-center.
                Vector3 charCenter = Vector3.zero;
                bool foundChar = false;
                foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    charCenter = smr.bounds.center;
                    foundChar = true;
                    break;
                }
                if (!foundChar) charCenter = new Vector3(0f, 1f, 0f);
                Vector3 camPos = charCenter + new Vector3(0f, 2f, -5f);
                obsT.position = camPos;
                obsT.LookAt(charCenter);
                Debug.Log($"[AIB]   Camera repositioned to {camPos}, looking at {charCenter}");

                // Force all skinned/mesh renderers in the scene visible —
                // editor-time render skips Awake/Start, which may leave
                // the rig culled / inactive.
                int smrCount = 0;
                // Walk the inst's full subtree including inactive GameObjects.
                foreach (Transform t in inst.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                }
                foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    smr.enabled = true;
                    smr.updateWhenOffscreen = true;
                    smr.gameObject.SetActive(true);
                    Debug.Log($"[AIB]   SMR on {smr.gameObject.name} world-pos={smr.transform.position} bounds-center={smr.bounds.center} bounds-size={smr.bounds.size} mats={smr.sharedMaterials.Length}");
                    smrCount++;
                }
                int mrCount = 0;
                foreach (var mr in inst.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (mr.enabled)
                    {
                        var mf = mr.GetComponent<MeshFilter>();
                        string mn = mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "(no mesh)";
                        Debug.Log($"[AIB]   ENABLED MR: {mr.gameObject.name} mesh={mn}");
                    }
                    mrCount++;
                }
                // Also list ALL renderers in the scene, not just under AAI3Agent.
                int totalSMR = 0, totalMR = 0;
                foreach (var smr in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (smr.enabled) Debug.Log($"[AIB]   SCENE-SMR enabled: {smr.gameObject.name} root={smr.transform.root.name}");
                    totalSMR++;
                }
                foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (mr.enabled)
                    {
                        var mf = mr.GetComponent<MeshFilter>();
                        string mn = mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "(no mesh)";
                        Debug.Log($"[AIB]   SCENE-MR enabled: {mr.gameObject.name} mesh={mn} root={mr.transform.root.name}");
                    }
                    totalMR++;
                }
                Debug.Log($"[AIB]   Total renderers under AAI3Agent: SMR={smrCount}, MR={mrCount}; whole scene SMR={totalSMR}, MR={totalMR}");

                // Add a directional light so the agent isn't pitch black.
                GameObject lightGO = new GameObject("SmokeLight");
                Light light = lightGO.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

                // Force-render the scene through observer_camera.
                RenderTexture rt = RenderTexture.GetTemporary(Width, Height, 24);
                obsCam.targetTexture = rt;
                obsCam.Render();
                obsCam.targetTexture = null;

                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);

                byte[] png = tex.EncodeToPNG();
                Object.DestroyImmediate(tex);

                string stamp = System.DateTime.UtcNow.ToString("yyyyMMddTHHmmss");
                string outPath = Path.Combine(OutDir, $"observer_camera_{stamp}.png");
                File.WriteAllBytes(outPath, png);
                AssetDatabase.Refresh();
                Debug.Log($"[AIB] Wrote smoke render to {outPath} ({png.Length} bytes).");

                // Also write a stable-name copy for easy retrieval.
                string latest = Path.Combine(OutDir, "observer_camera_latest.png");
                File.WriteAllBytes(latest, png);
                AssetDatabase.Refresh();
                Debug.Log($"[AIB] Latest copy at {latest}.");
            }
            finally
            {
                // Don't bother saving the temp scene; just finish.
            }

            Debug.Log("[AIB] AbeVisualSmoke done.");
        }
    }
}
#endif
