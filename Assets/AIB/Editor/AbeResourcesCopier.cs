// Copy AbeVisualMesh.prefab into Assets/AIB/Resources/ so it's loadable
// at runtime via Resources.Load<GameObject>("AbeVisualMesh").
//
// Idempotent — checks dest mtime and only copies on stale.

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AIB.Editor
{
    public static class AbeResourcesCopier
    {
        private const string SrcPath = "Assets/AIB/Prefabs/AbeVisualMesh.prefab";
        private const string DstDir = "Assets/AIB/Resources";
        private const string DstPath = "Assets/AIB/Resources/AbeVisualMesh.prefab";

        [MenuItem("AIB/Copy Abe Visual Mesh to Resources")]
        public static void Copy()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            string src = Path.Combine(root, SrcPath);
            string dstDir = Path.Combine(root, DstDir);
            string dst = Path.Combine(root, DstPath);

            if (!File.Exists(src))
            {
                Debug.LogError($"[AIB] Source missing: {src}");
                return;
            }
            if (!Directory.Exists(dstDir)) Directory.CreateDirectory(dstDir);

            bool needsCopy = !File.Exists(dst) ||
                File.GetLastWriteTime(src) > File.GetLastWriteTime(dst);
            if (!needsCopy)
            {
                Debug.Log("[AIB] Resources copy already up to date.");
                return;
            }

            File.Copy(src, dst, overwrite: true);
            // Also copy the .meta file so Unity treats it as the same prefab.
            string srcMeta = src + ".meta";
            string dstMeta = dst + ".meta";
            if (File.Exists(srcMeta)) File.Copy(srcMeta, dstMeta, overwrite: true);

            AssetDatabase.Refresh();
            Debug.Log($"[AIB] Copied AbeVisualMesh.prefab to {DstPath}");
        }
    }
}
#endif
