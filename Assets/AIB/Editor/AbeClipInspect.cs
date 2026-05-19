// Inspect ProneGlobalWave.anim and Idle.anim curve bindings.

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace AIB.Editor
{
    public static class AbeClipInspect
    {
        [MenuItem("AIB/Inspect Procedural Clips")]
        public static void Inspect()
        {
            foreach (string p in new[] { "Assets/AIB/Animations/Idle.anim", "Assets/AIB/Animations/ProneGlobalWave.anim" })
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(p);
                if (clip == null) { Debug.LogWarning($"[AIB] No clip at {p}"); continue; }
                Debug.Log($"[AIB] === {p} length={clip.length:F3}s frameRate={clip.frameRate} legacy={clip.legacy} ===");
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
                Debug.Log($"[AIB]   {bindings.Length} curve bindings");
                foreach (var b in bindings)
                {
                    AnimationCurve c = AnimationUtility.GetEditorCurve(clip, b);
                    int n = c != null ? c.length : 0;
                    float min = float.PositiveInfinity, max = float.NegativeInfinity;
                    if (c != null) for (int i = 0; i < n; i++) { float v = c[i].value; if (v < min) min = v; if (v > max) max = v; }
                    Debug.Log($"[AIB]     path='{b.path}' type={b.type.Name} prop='{b.propertyName}' keys={n} range=[{min:F3}..{max:F3}]");
                }
            }
        }
    }
}
#endif
