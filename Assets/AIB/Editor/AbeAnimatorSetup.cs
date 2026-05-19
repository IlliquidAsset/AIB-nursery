#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;

namespace AIB.Editor
{
    public class AbeAnimatorSetup
    {
        [MenuItem("AIB/Setup Abe Animator")]
        public static void SetupAnimator()
        {
            string dirPath = "Assets/AIB/Animations";
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            string controllerPath = "Assets/AIB/Animations/AbeAnimatorController.controller";
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            // Add Parameters
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsBackward", AnimatorControllerParameterType.Bool);
            controller.AddParameter("HealthNormalized", AnimatorControllerParameterType.Float);
            controller.AddParameter("Phase", AnimatorControllerParameterType.Int);
            controller.AddParameter("Alertness", AnimatorControllerParameterType.Float);
            controller.AddParameter("Curiosity", AnimatorControllerParameterType.Float);

            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

            // Find Animation Clips
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/AIB/Models/Abe" });
            Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (Object asset in assets)
                {
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__") && !clip.name.StartsWith("Take 001"))
                    {
                        clips[clip.name] = clip;
                        Debug.Log($"Found clip: {clip.name} in {path}");
                    }
                }
            }

            AnimationClip GetClip(string partialName)
            {
                foreach (var kvp in clips)
                {
                    if (kvp.Key.ToLower().Contains(partialName.ToLower()))
                    {
                        return kvp.Value;
                    }
                }
                return null;
            }

            AnimationClip walkClip = GetClip("Walking");
            AnimationClip runClip = GetClip("Running");
            AnimationClip crawlClip = GetClip("Crawl");
            AnimationClip lookAroundClip = GetClip("Look_Around");
            AnimationClip stumbleClip = GetClip("Stumble");
            AnimationClip unsteadyClip = GetClip("Unsteady");
            AnimationClip elderlyClip = GetClip("Elderly");
            AnimationClip penguinClip = GetClip("penguin");
            AnimationClip tightropeClip = GetClip("Tightrope");
            AnimationClip carryClip = GetClip("Carry");
            AnimationClip wakeUpClip = GetClip("Wake_Up");

            // Create States
            AnimatorState idleState = rootStateMachine.AddState("Idle");
            AnimatorState runState = rootStateMachine.AddState("Run");
            if (runClip != null) runState.motion = runClip;

            AnimatorState walkBackwardState = rootStateMachine.AddState("WalkBackward");
            if (walkClip != null) 
            {
                walkBackwardState.motion = walkClip;
                walkBackwardState.speed = -1f;
            }

            AnimatorState crawlState = rootStateMachine.AddState("Crawl");
            if (crawlClip != null) crawlState.motion = crawlClip;

            AnimatorState stumbleState = rootStateMachine.AddState("StumbleWalk");
            if (stumbleClip != null) stumbleState.motion = stumbleClip;

            AnimatorState unsteadyState = rootStateMachine.AddState("UnsteadyWalk");
            if (unsteadyClip != null) unsteadyState.motion = unsteadyClip;

            AnimatorState elderlyState = rootStateMachine.AddState("ElderlyWalk");
            if (elderlyClip != null) elderlyState.motion = elderlyClip;

            AnimatorState lookAroundState = rootStateMachine.AddState("LookAround");
            if (lookAroundClip != null) lookAroundState.motion = lookAroundClip;

            AnimatorState carryWalkState = rootStateMachine.AddState("CarryWalk");
            if (carryClip != null) carryWalkState.motion = carryClip;

            AnimatorState wakeUpState = rootStateMachine.AddState("WakeUp");
            if (wakeUpClip != null) wakeUpState.motion = wakeUpClip;

            // Create Blend Tree for Walk
            BlendTree walkBlendTree;
            AnimatorState walkState = controller.CreateBlendTreeInController("Walk", out walkBlendTree);
            walkBlendTree.blendType = BlendTreeType.Simple1D;
            walkBlendTree.blendParameter = "HealthNormalized";

            if (unsteadyClip != null) walkBlendTree.AddChild(unsteadyClip, 0.0f);
            if (elderlyClip != null) walkBlendTree.AddChild(elderlyClip, 0.5f);
            if (walkClip != null) walkBlendTree.AddChild(walkClip, 1.0f);

            rootStateMachine.defaultState = idleState;

            // Transitions
            AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            
            AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");

            AnimatorStateTransition walkToRun = walkState.AddTransition(runState);
            walkToRun.AddCondition(AnimatorConditionMode.Greater, 0.7f, "Speed");

            AnimatorStateTransition runToWalk = runState.AddTransition(walkState);
            runToWalk.AddCondition(AnimatorConditionMode.Less, 0.7f, "Speed");

            AnimatorStateTransition walkToBackward = walkState.AddTransition(walkBackwardState);
            walkToBackward.AddCondition(AnimatorConditionMode.If, 0, "IsBackward");

            AnimatorStateTransition backwardToWalk = walkBackwardState.AddTransition(walkState);
            backwardToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsBackward");

            AnimatorStateTransition idleToCrawl = idleState.AddTransition(crawlState);
            idleToCrawl.AddCondition(AnimatorConditionMode.Equals, Animator.StringToHash("WOMB"), "Phase");

            AnimatorStateTransition walkToStumble = walkState.AddTransition(stumbleState);
            walkToStumble.AddCondition(AnimatorConditionMode.Less, 0.3f, "HealthNormalized");

            Debug.Log("Abe Animator Controller created successfully at " + controllerPath);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
