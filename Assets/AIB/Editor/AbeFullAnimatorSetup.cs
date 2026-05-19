// Self-contained Animator setup for Abe — combines procedural Idle +
// ProneGlobalWave generation with binding of recovered FBX clips. Runs in
// one batch so a single -executeMethod call from headless Unity completes
// the visual-pass animation prep.
//
// Drop this file into:
//   /Volumes/Video/AIB-nursery/Assets/AIB/Editor/AbeFullAnimatorSetup.cs
//
// Run from menu: AIB → Full Animator Setup
// Or headless:   Unity -batchmode -quit -projectPath ... -executeMethod AIB.Editor.AbeFullAnimatorSetup.SetupAll
//
// The script:
//   1. Generates Idle.anim (Y bob + head sway, loopable) and
//      ProneGlobalWave.anim (4-limb 2 Hz synchronized flail, loopable)
//      under Assets/AIB/Animations/.
//   2. Discovers FBX-derived AnimationClips in Assets/AIB/Models/Abe/.
//   3. Builds AbeAnimatorController.controller with states for:
//      Idle (procedural), ProneGlobalWave (procedural), Crawl, WakeUp,
//      UnsteadyWalk, StumbleWalk, Walk (BlendTree of Unsteady/Elderly/Walking),
//      WalkBackward, Run, ElderlyWalk, LookAround, CarryWalk.
//   4. Adds parameters: Speed, IsMoving, IsBackward, HealthNormalized,
//      Phase (int), Alertness, Curiosity, IsPreGABA (bool, default false).
//   5. Wires transitions for IsMoving/Speed/IsBackward/Phase=WOMB/IsPreGABA.

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;

namespace AIB.Editor
{
    public static class AbeFullAnimatorSetup
    {
        private const string AnimDir = "Assets/AIB/Animations";
        private const string ModelDir = "Assets/AIB/Models/Abe";
        private const string ControllerPath = "Assets/AIB/Animations/AbeAnimatorController.controller";

        // Bone paths inside the Abe rig (verified via AbeRigDump 2026-05-07).
        // The Animator sits on AbeVisualMesh; curve paths are relative to it.
        private static readonly string[] LimbBonePaths = new[]
        {
            "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm",
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm",
            "Armature/Hips/LeftUpLeg",
            "Armature/Hips/RightUpLeg",
        };
        private const string RootPath = "Armature/Hips";
        private const string NeckPath = "Armature/Hips/Spine02/Spine01/Spine/neck";

        [MenuItem("AIB/Full Animator Setup")]
        public static void SetupAll()
        {
            Debug.Log("[AIB] AbeFullAnimatorSetup begin.");
            EnsureDir(AnimDir);

            AnimationClip idleClip = CreateIdleClip();
            AnimationClip proneWaveClip = CreateProneGlobalWaveClip();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            BuildController(idleClip, proneWaveClip);

            Debug.Log("[AIB] AbeFullAnimatorSetup done.");
        }

        private static void EnsureDir(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static AnimationClip CreateIdleClip()
        {
            string path = Path.Combine(AnimDir, "Idle.anim");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = "Idle", legacy = false };
                AssetDatabase.CreateAsset(clip, path);
            }
            clip.frameRate = 30f;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            // Y bob: ±1 cm, period 3s.
            var yCurve = new AnimationCurve();
            for (int i = 0; i <= 30; i++)
            {
                float t = i / 30f * 3f;
                float y = Mathf.Sin(t * 2f * Mathf.PI / 3f) * 0.01f;
                yCurve.AddKey(t, y);
            }
            clip.SetCurve(RootPath, typeof(Transform), "localPosition.y", yCurve);

            // Head sway: ±3°, same period.
            var headSway = new AnimationCurve();
            for (int i = 0; i <= 30; i++)
            {
                float t = i / 30f * 3f;
                float r = Mathf.Sin(t * 2f * Mathf.PI / 3f) * 3f;
                headSway.AddKey(t, r);
            }
            clip.SetCurve(NeckPath, typeof(Transform), "localEulerAngles.y", headSway);

            EditorUtility.SetDirty(clip);
            Debug.Log($"[AIB] Procedural Idle clip ready at {path}");
            return clip;
        }

        private static AnimationClip CreateProneGlobalWaveClip()
        {
            string path = Path.Combine(AnimDir, "ProneGlobalWave.anim");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = "ProneGlobalWave", legacy = false };
                AssetDatabase.CreateAsset(clip, path);
            }
            clip.frameRate = 30f;

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            const float frequencyHz = 2.0f;
            const float amplitudeDeg = 25f;
            const float durationSec = 1.0f;

            foreach (string bone in LimbBonePaths)
            {
                var curve = new AnimationCurve();
                int frames = Mathf.RoundToInt(durationSec * 30f);
                for (int i = 0; i <= frames; i++)
                {
                    float t = i / 30f;
                    float r = Mathf.Sin(t * 2f * Mathf.PI * frequencyHz) * amplitudeDeg;
                    curve.AddKey(t, r);
                }
                clip.SetCurve(bone, typeof(Transform), "localEulerAngles.x", curve);
            }

            // Prone Y-dip: stays at -0.4m so the body reads as on-ground.
            var dip = new AnimationCurve();
            dip.AddKey(0f, -0.4f);
            dip.AddKey(durationSec, -0.4f);
            clip.SetCurve(RootPath, typeof(Transform), "localPosition.y", dip);

            EditorUtility.SetDirty(clip);
            Debug.Log($"[AIB] Procedural ProneGlobalWave clip ready at {path}");
            return clip;
        }

        private static AnimationClip GetClip(Dictionary<string, AnimationClip> clips, string partial)
        {
            foreach (var kvp in clips)
            {
                if (kvp.Key.ToLower().Contains(partial.ToLower())) return kvp.Value;
            }
            return null;
        }

        private static void BuildController(AnimationClip idleClip, AnimationClip proneWaveClip)
        {
            // Discover FBX-derived clips in Models/Abe/.
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { ModelDir });
            var clips = new Dictionary<string, AnimationClip>();
            foreach (string guid in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(p);
                foreach (Object asset in assets)
                {
                    if (asset is AnimationClip c &&
                        !c.name.StartsWith("__preview__") &&
                        !c.name.StartsWith("Take 001"))
                    {
                        clips[c.name] = c;
                    }
                }
            }
            Debug.Log($"[AIB] Discovered {clips.Count} FBX-derived clips under {ModelDir}.");

            AnimationClip walkClip = GetClip(clips, "Walking");
            AnimationClip runClip = GetClip(clips, "Running");
            AnimationClip crawlClip = GetClip(clips, "Crawl");
            AnimationClip lookAroundClip = GetClip(clips, "Look_Around");
            AnimationClip stumbleClip = GetClip(clips, "Stumble");
            AnimationClip unsteadyClip = GetClip(clips, "Unsteady");
            AnimationClip elderlyClip = GetClip(clips, "Elderly");
            AnimationClip carryClip = GetClip(clips, "Carry");
            AnimationClip wakeUpClip = GetClip(clips, "Wake_Up");

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsBackward", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "HealthNormalized", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "Phase", AnimatorControllerParameterType.Int);
            EnsureParameter(controller, "Alertness", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "Curiosity", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "IsPreGABA", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            // Wipe any existing states so we can rebuild deterministically.
            foreach (var s in new List<ChildAnimatorState>(sm.states))
            {
                sm.RemoveState(s.state);
            }

            AnimatorState idleState = sm.AddState("Idle");
            idleState.motion = idleClip;

            AnimatorState proneWaveState = sm.AddState("ProneGlobalWave");
            proneWaveState.motion = proneWaveClip;

            AnimatorState runState = sm.AddState("Run");
            if (runClip != null) runState.motion = runClip;

            AnimatorState walkBackwardState = sm.AddState("WalkBackward");
            if (walkClip != null) { walkBackwardState.motion = walkClip; walkBackwardState.speed = -1f; }

            AnimatorState crawlState = sm.AddState("Crawl");
            if (crawlClip != null) crawlState.motion = crawlClip;

            AnimatorState stumbleState = sm.AddState("StumbleWalk");
            if (stumbleClip != null) stumbleState.motion = stumbleClip;

            AnimatorState unsteadyState = sm.AddState("UnsteadyWalk");
            if (unsteadyClip != null) unsteadyState.motion = unsteadyClip;

            AnimatorState elderlyState = sm.AddState("ElderlyWalk");
            if (elderlyClip != null) elderlyState.motion = elderlyClip;

            AnimatorState lookAroundState = sm.AddState("LookAround");
            if (lookAroundClip != null) lookAroundState.motion = lookAroundClip;

            AnimatorState carryWalkState = sm.AddState("CarryWalk");
            if (carryClip != null) carryWalkState.motion = carryClip;

            AnimatorState wakeUpState = sm.AddState("WakeUp");
            if (wakeUpClip != null) wakeUpState.motion = wakeUpClip;

            BlendTree walkBlendTree;
            AnimatorState walkState = controller.CreateBlendTreeInController("Walk", out walkBlendTree);
            walkBlendTree.blendType = BlendTreeType.Simple1D;
            walkBlendTree.blendParameter = "HealthNormalized";
            if (unsteadyClip != null) walkBlendTree.AddChild(unsteadyClip, 0.0f);
            if (elderlyClip != null) walkBlendTree.AddChild(elderlyClip, 0.5f);
            if (walkClip != null) walkBlendTree.AddChild(walkClip, 1.0f);

            sm.defaultState = idleState;

            AddTransition(idleState, walkState, AnimatorConditionMode.If, 0, "IsMoving");
            AddTransition(walkState, idleState, AnimatorConditionMode.IfNot, 0, "IsMoving");
            AddTransition(walkState, runState, AnimatorConditionMode.Greater, 0.7f, "Speed");
            AddTransition(runState, walkState, AnimatorConditionMode.Less, 0.7f, "Speed");
            AddTransition(walkState, walkBackwardState, AnimatorConditionMode.If, 0, "IsBackward");
            AddTransition(walkBackwardState, walkState, AnimatorConditionMode.IfNot, 0, "IsBackward");
            AddTransition(idleState, crawlState, AnimatorConditionMode.Equals, Animator.StringToHash("WOMB"), "Phase");
            AddTransition(walkState, stumbleState, AnimatorConditionMode.Less, 0.3f, "HealthNormalized");
            AddTransition(idleState, proneWaveState, AnimatorConditionMode.If, 0, "IsPreGABA");
            AddTransition(proneWaveState, idleState, AnimatorConditionMode.IfNot, 0, "IsPreGABA");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AIB] AnimatorController rebuilt with procedural + recovered clips.");
        }

        private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (var p in controller.parameters)
            {
                if (p.name == name) return;
            }
            controller.AddParameter(name, type);
        }

        private static void AddTransition(AnimatorState src, AnimatorState dst, AnimatorConditionMode mode, float threshold, string param)
        {
            AnimatorStateTransition t = src.AddTransition(dst);
            t.AddCondition(mode, threshold, param);
        }
    }
}
#endif
