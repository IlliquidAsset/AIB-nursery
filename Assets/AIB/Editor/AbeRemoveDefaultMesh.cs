// Disables legacy AnimalAI panda/sphere visual meshes on AAI3Agent.prefab
// while preserving colliders/physics. Idempotent — safe to re-run.
//
// Run: AIB → Remove Default Panda Mesh
// Headless: -executeMethod AIB.Editor.AbeRemoveDefaultMesh.RemoveDefault

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace AIB.Editor
{
    public static class AbeRemoveDefaultMesh
    {
        private static readonly string[] PrefabPaths = new[]
        {
            "Assets/Prefabs/AAI3Agent.prefab",
            "Assets/AIB/Prefabs/AbeVisualMesh.prefab",
        };

        [MenuItem("AIB/Remove Default Panda Mesh")]
        public static void RemoveDefault()
        {
            Debug.Log("[AIB] AbeRemoveDefaultMesh begin.");
            foreach (string p in PrefabPaths) ProcessPrefab(p);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AIB] AbeRemoveDefaultMesh done.");
        }

        private static void ProcessPrefab(string PrefabPath)
        {
            Debug.Log($"[AIB] === Processing {PrefabPath} ===");
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"[AIB] Cannot load prefab at {PrefabPath}");
                return;
            }

            try
            {
                int disabledMR = 0;
                int disabledSMR = 0;

                // Inventory pass — log every renderer we find.
                foreach (var mr in prefabRoot.GetComponentsInChildren<MeshRenderer>(true))
                {
                    string matName = mr.sharedMaterial != null ? mr.sharedMaterial.name : "(no mat)";
                    string meshName = "(no mesh)";
                    var mf = mr.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null) meshName = mf.sharedMesh.name;
                    Debug.Log($"[AIB]   MeshRenderer on {GetPath(mr.transform)} mesh={meshName} mat={matName} enabled={mr.enabled}");
                }
                foreach (var smr in prefabRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    string smName = smr.sharedMesh != null ? smr.sharedMesh.name : "(no mesh)";
                    Debug.Log($"[AIB]   SkinnedMeshRenderer on {GetPath(smr.transform)} mesh={smName} enabled={smr.enabled}");
                }

                // Disable any MeshRenderer whose mesh name suggests panda/sphere/legacy
                // AnimalAI agent body. Preserve colliders by only touching the
                // renderer component itself (not the GameObject's active state).
                string[] legacyMeshKeywords = new[] { "panda", "sphere", "baby_sphere", "panda_sphere" };
                foreach (var mr in prefabRoot.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var mf = mr.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) continue;
                    string meshLower = mf.sharedMesh.name.ToLower();
                    bool isLegacy = false;
                    foreach (string kw in legacyMeshKeywords)
                    {
                        if (meshLower.Contains(kw)) { isLegacy = true; break; }
                    }
                    if (isLegacy && mr.enabled)
                    {
                        mr.enabled = false;
                        disabledMR++;
                        Debug.Log($"[AIB]   DISABLED legacy MeshRenderer on {GetPath(mr.transform)} (mesh={mf.sharedMesh.name})");
                    }
                }

                // Belt-and-suspenders: also disable any MeshRenderer attached to
                // a GameObject named "Agent" itself or its direct child if no
                // SkinnedMeshRenderer is on the same object — these are the
                // legacy AnimalAI body MeshFilters that ride alongside Abe.
                foreach (var mr in prefabRoot.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!mr.enabled) continue;
                    if (mr.GetComponent<SkinnedMeshRenderer>() != null) continue;
                    string objLower = mr.gameObject.name.ToLower();
                    if (objLower == "agent" || objLower.Contains("body") || objLower.Contains("sphere"))
                    {
                        mr.enabled = false;
                        disabledMR++;
                        Debug.Log($"[AIB]   DISABLED MeshRenderer on {GetPath(mr.transform)} (heuristic match: gameobject name)");
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                Debug.Log($"[AIB] {PrefabPath}: disabled {disabledMR} MeshRenderers, {disabledSMR} SkinnedMeshRenderers.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static string GetPath(Transform t)
        {
            string path = t.name;
            Transform p = t.parent;
            while (p != null)
            {
                path = p.name + "/" + path;
                p = p.parent;
            }
            return path;
        }
    }
}
#endif
