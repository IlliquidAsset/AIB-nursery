// Dump the full transform hierarchy under AbeVisualMesh's animator,
// so we can find the actual bone paths for procedural clip curves.

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace AIB.Editor
{
    public static class AbeRigDump
    {
        [MenuItem("AIB/Dump Abe Rig Hierarchy")]
        public static void Dump()
        {
            string path = "Assets/AIB/Prefabs/AbeVisualMesh.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogError($"[AIB] No prefab at {path}"); return; }

            Debug.Log($"[AIB] === Hierarchy under {path} ===");
            DumpTransform(prefab.transform, "");
            Debug.Log("[AIB] === End hierarchy ===");
        }

        private static void DumpTransform(Transform t, string indent)
        {
            string compInfo = "";
            if (t.GetComponent<SkinnedMeshRenderer>() != null) compInfo += " [SMR]";
            if (t.GetComponent<Animator>() != null) compInfo += " [Animator]";
            Debug.Log($"[AIB] {indent}{t.name}{compInfo}");
            foreach (Transform c in t)
                DumpTransform(c, indent + "  ");
        }
    }
}
#endif
