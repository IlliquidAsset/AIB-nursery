#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AIB.Editor
{
    public static class AIBOneClickSetup
    {
        [MenuItem("AIB/One-Click Setup (Do Everything)", priority = 0)]
        public static void RunFullSetup()
        {
            Debug.Log("[AIB] Starting one-click setup...");

            if (!EnsureCorrectScene())
            {
                Debug.LogError("[AIB] Setup aborted. Open AAI3EnvironmentManager scene first.");
                return;
            }

            CreateAllCharacterVisualAssets();
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/AIB/Animations/AbeAnimatorController.controller");
            GameObject abePrefab = CreateAbePrefab(controller);
            SetupSceneComponents(abePrefab);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log("[AIB] === ONE-CLICK SETUP COMPLETE ===");
            Debug.Log("[AIB] To test in Editor: AIB > Build > Set Observer Mode, then press Play");
            Debug.Log("[AIB] To build Experiment: AIB > Build > Build Experiment (Linux Headless)");
            Debug.Log("[AIB] To build Observer: AIB > Build > Build Observer (macOS)");
        }

        [MenuItem("AIB/Setup/Create Character Visual Assets", priority = 1)]
        public static void CreateAllCharacterVisualAssets()
        {
            AnimatorController abeController = CreateAnimatorController();
            VerifyOrCreateAbePrefab(abeController);

            AnimatorController babyAbbyController = CreateTwoStateAnimatorController(
                "BabyAbbyAnimatorController",
                "Assets/AIB/Models/BabyAbby",
                "Idle",
                "Calm_Walk"
            );
            CreateCharacterPrefab(
                babyAbbyController,
                "Assets/AIB/Models/BabyAbby",
                "Assets/AIB/Prefabs/BabyAbbyVisualMesh.prefab",
                "BabyAbbyVisualMesh"
            );

            AnimatorController motherController = CreateTwoStateAnimatorController(
                "MotherAnimatorController",
                "Assets/AIB/Models/Mother",
                "Idle",
                "Calm_Walk"
            );
            CreateCharacterPrefab(
                motherController,
                "Assets/AIB/Models/Mother",
                "Assets/AIB/Prefabs/MotherVisualMesh.prefab",
                "MotherVisualMesh"
            );

            AnimatorController motherAbbyController = CreateTwoStateAnimatorController(
                "MotherAbbyAnimatorController",
                "Assets/AIB/Models/MotherAbby",
                "Idle",
                "Calm_Walk"
            );
            CreateCharacterPrefab(
                motherAbbyController,
                "Assets/AIB/Models/MotherAbby",
                "Assets/AIB/Prefabs/MotherAbbyVisualMesh.prefab",
                "MotherAbbyVisualMesh"
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AIB] Character visual assets created/updated: Abe, BabyAbby, Mother, MotherAbby");
        }

        private static bool EnsureCorrectScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name.Contains("AAI3"))
            {
                return true;
            }
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene AAI3");
            if (sceneGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(sceneGuids[0]);
                EditorSceneManager.OpenScene(path);
                return true;
            }
            return false;
        }

        private static AnimatorController CreateAnimatorController()
        {
            Debug.Log("[AIB] Creating AnimatorController...");
            string dirPath = "Assets/AIB/Animations";
            if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

            string controllerPath = $"{dirPath}/AbeAnimatorController.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
            {
                AssetDatabase.DeleteAsset(controllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsBackward", AnimatorControllerParameterType.Bool);
            controller.AddParameter("HealthNormalized", AnimatorControllerParameterType.Float);
            controller.AddParameter("Phase", AnimatorControllerParameterType.Int);
            controller.AddParameter("Alertness", AnimatorControllerParameterType.Float);
            controller.AddParameter("Curiosity", AnimatorControllerParameterType.Float);

            var rootSM = controller.layers[0].stateMachine;

            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/AIB/Models/Abe" });
            Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__") && clip.name != "Take 001")
                    {
                        clips[clip.name] = clip;
                        Debug.Log($"[AIB]   Found clip: {clip.name}");
                    }
                }
            }

            AnimationClip Find(string partial) =>
                clips.FirstOrDefault(k => k.Key.ToLower().Contains(partial.ToLower())).Value;

            var walkClip = Find("Walking");
            var runClip = Find("Running");
            var crawlClip = Find("Crawl");
            var stumbleClip = Find("Stumble");
            var unsteadyClip = Find("Unsteady");
            var elderlyClip = Find("Elderly");
            var wakeUpClip = Find("Wake_Up");

            var idleState = rootSM.AddState("Idle");
            if (wakeUpClip != null) idleState.motion = wakeUpClip;

            BlendTree walkBlend;
            var walkState = controller.CreateBlendTreeInController("Walk", out walkBlend);
            walkBlend.blendType = BlendTreeType.Simple1D;
            walkBlend.blendParameter = "HealthNormalized";
            if (unsteadyClip != null) walkBlend.AddChild(unsteadyClip, 0.0f);
            if (elderlyClip != null) walkBlend.AddChild(elderlyClip, 0.5f);
            if (walkClip != null) walkBlend.AddChild(walkClip, 1.0f);

            var runState = rootSM.AddState("Run");
            if (runClip != null) runState.motion = runClip;

            var backState = rootSM.AddState("WalkBackward");
            if (walkClip != null) { backState.motion = walkClip; backState.speed = -1f; }

            var crawlState = rootSM.AddState("Crawl");
            if (crawlClip != null) crawlState.motion = crawlClip;

            var stumbleState = rootSM.AddState("Stumble");
            if (stumbleClip != null) stumbleState.motion = stumbleClip;

            rootSM.defaultState = idleState;

            AddTransition(idleState, walkState, "IsMoving", AnimatorConditionMode.If);
            AddTransition(walkState, idleState, "IsMoving", AnimatorConditionMode.IfNot);
            AddTransition(walkState, runState, "Speed", AnimatorConditionMode.Greater, 0.7f);
            AddTransition(runState, walkState, "Speed", AnimatorConditionMode.Less, 0.7f);
            AddTransition(walkState, backState, "IsBackward", AnimatorConditionMode.If);
            AddTransition(backState, walkState, "IsBackward", AnimatorConditionMode.IfNot);
            AddTransition(walkState, stumbleState, "HealthNormalized", AnimatorConditionMode.Less, 0.3f);
            AddTransition(stumbleState, walkState, "HealthNormalized", AnimatorConditionMode.Greater, 0.3f);

            AssetDatabase.SaveAssets();
            Debug.Log($"[AIB] AnimatorController created: {controllerPath} ({clips.Count} clips imported)");
            return controller;
        }

        private static void AddTransition(AnimatorState from, AnimatorState to, string param, AnimatorConditionMode mode, float threshold = 0f)
        {
            var t = from.AddTransition(to);
            t.AddCondition(mode, threshold, param);
            t.duration = 0.2f;
            t.hasExitTime = false;
        }

        private static AnimatorController CreateTwoStateAnimatorController(
            string controllerName,
            string modelFolder,
            string idleHint,
            string walkHint
        )
        {
            string dirPath = "Assets/AIB/Animations";
            if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

            string controllerPath = $"{dirPath}/{controllerName}.controller";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(controllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

            var rootSM = controller.layers[0].stateMachine;
            var idleState = rootSM.AddState("Idle");
            var walkState = rootSM.AddState("Calm_Walk");

            idleState.motion = FindAnimationClipInFolder(modelFolder, idleHint);
            walkState.motion = FindAnimationClipInFolder(modelFolder, walkHint);

            rootSM.defaultState = idleState;

            AddTransition(idleState, walkState, "IsMoving", AnimatorConditionMode.If);
            AddTransition(walkState, idleState, "IsMoving", AnimatorConditionMode.IfNot);

            AssetDatabase.SaveAssets();
            Debug.Log($"[AIB] Two-state AnimatorController created: {controllerPath}");
            return controller;
        }

        private static AnimationClip FindAnimationClipInFolder(string modelFolder, string partial)
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { modelFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__") && clip.name != "Take 001")
                    {
                        if (clip.name.ToLower().Contains(partial.ToLower()))
                        {
                            return clip;
                        }
                    }
                }
            }
            Debug.LogWarning($"[AIB] Could not find clip containing '{partial}' in {modelFolder}");
            return null;
        }

        private static GameObject VerifyOrCreateAbePrefab(AnimatorController controller)
        {
            const string abePrefabPath = "Assets/AIB/Prefabs/AbeVisualMesh.prefab";
            GameObject existingAbePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(abePrefabPath);
            if (existingAbePrefab != null && IsVisualPrefabStructureValid(existingAbePrefab))
            {
                Debug.Log("[AIB] AbeVisualMesh prefab structure verified.");
                return existingAbePrefab;
            }

            Debug.LogWarning("[AIB] AbeVisualMesh prefab missing/invalid. Rebuilding...");
            return CreateCharacterPrefab(
                controller,
                "Assets/AIB/Models/Abe",
                abePrefabPath,
                "AbeVisualMesh"
            );
        }

        private static bool IsVisualPrefabStructureValid(GameObject prefab)
        {
            if (prefab == null) return false;

            bool hasAnimator = prefab.GetComponentInChildren<Animator>() != null;
            bool hasSkinnedMeshRenderer = prefab.GetComponentInChildren<SkinnedMeshRenderer>() != null;
            bool hasLookAtIk = prefab.GetComponent<AbeLookAtIK>() != null;

            return hasAnimator && hasSkinnedMeshRenderer && hasLookAtIk;
        }

        private static GameObject CreateAbePrefab(AnimatorController controller)
        {
            return VerifyOrCreateAbePrefab(controller);
        }

        private static GameObject CreateCharacterPrefab(
            AnimatorController controller,
            string modelFolder,
            string prefabPath,
            string prefabName
        )
        {
            Debug.Log($"[AIB] Creating prefab: {prefabName}");

            if (!Directory.Exists("Assets/AIB/Prefabs")) Directory.CreateDirectory("Assets/AIB/Prefabs");

            string[] modelGuids = AssetDatabase.FindAssets("Meshy_AI_Character_output t:Model", new[] { modelFolder });
            if (modelGuids.Length == 0)
            {
                modelGuids = AssetDatabase.FindAssets("t:Model", new[] { modelFolder });
            }
            if (modelGuids.Length == 0)
            {
                Debug.LogError($"[AIB] Character model not found in {modelFolder}");
                return null;
            }

            string modelPath = AssetDatabase.GUIDToAssetPath(modelGuids[0]);
            SetModelRigType(modelPath);

            GameObject sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (sourceModel == null)
            {
                Debug.LogError($"[AIB] Failed to load model at {modelPath}");
                return null;
            }

            GameObject instance = Object.Instantiate(sourceModel);
            instance.name = prefabName;

            Animator animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.avatar = FindValidAvatar(modelPath);

            if (instance.GetComponent<AbeLookAtIK>() == null)
            {
                instance.AddComponent<AbeLookAtIK>();
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            Debug.Log($"[AIB] Prefab created: {prefabPath}");
            return prefab;
        }

        private static void SetModelRigType(string modelPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null) return;

            bool changed = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }

            Avatar avatar = FindValidAvatar(modelPath);
            if (avatar == null || !avatar.isHuman)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.SaveAndReimport();
                Debug.LogWarning($"[AIB] Humanoid rig mapping failed for {modelPath}. Using Generic rig.");
            }
        }

        private static Avatar FindValidAvatar(string modelPath)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is Avatar avatar && avatar.isValid)
                {
                    return avatar;
                }
            }
            return null;
        }

        private static void SetupSceneComponents(GameObject abePrefab)
        {
            Debug.Log("[AIB] Setting up scene components...");

            GameObject aibRoot = GameObject.Find("AIB_Root");
            if (aibRoot == null)
            {
                aibRoot = new GameObject("AIB_Root");
                Debug.Log("[AIB]   Created AIB_Root object");
            }

            TryAddComponent(aibRoot, "AIB.AbeStateBroadcaster");
            TryAddComponent(aibRoot, "AIB.StateHttpEndpoint");
            TryAddComponent(aibRoot, "AIB.AbeStateReceiver");
            TryAddComponent(aibRoot, "AIB.ConnectionStatusUI");
            TryAddComponent(aibRoot, "AIB.HUDManager");

            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                mainCam = Object.FindAnyObjectByType<Camera>();
            }

            if (mainCam != null)
            {
                TryAddComponent(mainCam.gameObject, "AIB.CameraController");
            }

            WireAbePrefabIntoAgentPrefab(abePrefab);

            Debug.Log("[AIB] Scene setup complete.");
        }

        private static void WireAbePrefabIntoAgentPrefab(GameObject abePrefab)
        {
            string agentPrefabPath = "Assets/Prefabs/AAI3Agent.prefab";
            GameObject agentPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(agentPrefabPath);

            if (agentPrefabAsset == null)
            {
                Debug.LogError($"[AIB] Agent prefab not found at {agentPrefabPath}");
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(agentPrefabPath);

            Transform agentChild = prefabRoot.transform.Find("Agent");
            if (agentChild == null)
            {
                agentChild = prefabRoot.transform;
                Debug.Log("[AIB]   'Agent' child not found, using prefab root");
            }

            var visualCtrl = agentChild.GetComponent<AbeVisualController>();
            if (visualCtrl == null)
            {
                visualCtrl = agentChild.gameObject.AddComponent<AbeVisualController>();
                Debug.Log("[AIB]   Added AbeVisualController to AAI3Agent prefab");
            }

            if (abePrefab != null)
            {
                var so = new SerializedObject(visualCtrl);
                var prop = so.FindProperty("agentMeshPrefab");
                if (prop != null)
                {
                    prop.objectReferenceValue = abePrefab;
                    so.ApplyModifiedProperties();
                    Debug.Log("[AIB]   Assigned Abe visual mesh to agent prefab");
                }
            }

            var meshRenderer = agentChild.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
                Debug.Log("[AIB]   Disabled old sphere MeshRenderer");
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, agentPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            Debug.Log("[AIB]   AAI3Agent prefab updated — Abe biped spawns automatically now");
        }

        private static void TryAddComponent(GameObject obj, string fullTypeName)
        {
            System.Type type = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(fullTypeName);
                if (type != null) break;
            }

            if (type == null)
            {
                Debug.Log($"[AIB]   Skipping {fullTypeName} (not in current build mode)");
                return;
            }

            if (obj.GetComponent(type) == null)
            {
                obj.AddComponent(type);
                Debug.Log($"[AIB]   Added {fullTypeName}");
            }
        }
    }
}
#endif
