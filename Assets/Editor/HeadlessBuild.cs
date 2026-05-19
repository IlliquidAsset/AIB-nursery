using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class HeadlessBuild
{
    [MenuItem("Build/Setup Baby + Build Linux")]
    public static void SetupBabyAndBuild()
    {
        SetupBabyModel();
        SetupOTSCamera();
        AIB.Editor.AIBOneClickSetup.RunFullSetup();
        AIB.Editor.AIBBuildConfig.SetExperimentMode();
        BuildLinuxHeadless();
    }

    [MenuItem("Build/Linux Headless")]
    public static void BuildLinuxHeadless()
    {
        AIB.Editor.AIBBuildConfig.SetExperimentMode();
        string[] scenes = GetEnabledScenes();
        string outputPath = GetArg("-outputPath", "Builds/AnimalAI.x86_64");

        // Force Mono backend — IL2CPP breaks ML-Agents gRPC communication
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);

        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.StandaloneLinux64,
            subtarget = (int)StandaloneBuildSubtarget.Player,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"Build failed: {report.summary.totalErrors} error(s)");
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log($"Build succeeded: {outputPath}");
            EditorApplication.Exit(0);
        }
    }

    static void SetupOTSCamera()
    {
        string prefabPath = "Assets/Prefabs/AAI3Arena.prefab";
        var prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);

        var existing = prefabContents.GetComponentInChildren<OTSRecordCamera>(true);
        if (existing != null)
        {
            Debug.Log("OTSRecordCamera already exists on arena prefab.");
            PrefabUtility.UnloadPrefabContents(prefabContents);
            return;
        }

        var camObj = new GameObject("OTSRecordCamera");
        camObj.transform.SetParent(prefabContents.transform);
        camObj.AddComponent<OTSRecordCamera>();

        PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabContents);
        Debug.Log("OTSRecordCamera added to arena prefab.");
    }

    static void SetupBabyModel()
    {
        string[] babyGuids = AssetDatabase.FindAssets("baby_sphere_model1 t:Model");
        if (babyGuids.Length == 0)
        {
            Debug.LogError("Baby model OBJ not found in project!");
            return;
        }

        string modelPath = AssetDatabase.GUIDToAssetPath(babyGuids[0]);
        ModelImporter babyImporter = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (babyImporter != null)
        {
            bool needsReimport = false;
            if (!babyImporter.isReadable)
            {
                babyImporter.isReadable = true;
                needsReimport = true;
            }
            if (babyImporter.useFileScale)
            {
                babyImporter.useFileScale = false;
                needsReimport = true;
            }
            if (needsReimport)
            {
                Debug.Log("Reimporting baby model with isReadable=true, useFileScale=false...");
                babyImporter.SaveAndReimport();
            }
        }

        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
        Mesh babyMesh = null;
        foreach (var asset in subAssets)
        {
            if (asset is Mesh m)
            {
                babyMesh = m;
                break;
            }
        }

        if (babyMesh == null)
        {
            Debug.LogError("No mesh found in baby model!");
            return;
        }

        Debug.Log($"Baby mesh: {babyMesh.name} ({babyMesh.vertexCount} verts, bounds={babyMesh.bounds})");

        string[] pandaGuids = AssetDatabase.FindAssets("panda_sphere_model1 t:Model");
        Mesh pandaMesh = null;
        if (pandaGuids.Length > 0)
        {
            string pandaPath = AssetDatabase.GUIDToAssetPath(pandaGuids[0]);
            ModelImporter pandaImporter = AssetImporter.GetAtPath(pandaPath) as ModelImporter;
            bool wasReadable = pandaImporter != null && pandaImporter.isReadable;
            if (pandaImporter != null && !pandaImporter.isReadable)
            {
                pandaImporter.isReadable = true;
                pandaImporter.SaveAndReimport();
            }
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(pandaPath))
            {
                if (asset is Mesh m)
                {
                    pandaMesh = m;
                    break;
                }
            }
            if (pandaImporter != null && !wasReadable)
            {
                pandaImporter.isReadable = false;
                pandaImporter.SaveAndReimport();
            }
        }

        if (pandaMesh != null)
        {
            Debug.Log($"Panda mesh: {pandaMesh.name} ({pandaMesh.vertexCount} verts, bounds={pandaMesh.bounds})");
        }
        else
        {
            Debug.LogWarning("Could not load panda reference mesh — will use default scale factor");
        }

        float scaleFactor = 1f;
        if (pandaMesh != null)
        {
            float pandaMaxExtent = Mathf.Max(pandaMesh.bounds.size.x, pandaMesh.bounds.size.y, pandaMesh.bounds.size.z);
            float babyMaxExtent = Mathf.Max(babyMesh.bounds.size.x, babyMesh.bounds.size.y, babyMesh.bounds.size.z);
            if (babyMaxExtent > 0.001f)
            {
                scaleFactor = pandaMaxExtent / babyMaxExtent;
            }
        }
        else
        {
            scaleFactor = 1.5f;
        }

        Debug.Log($"Scale factor: {scaleFactor:F3}");

        Mesh finalMesh = babyMesh;
        if (Mathf.Abs(scaleFactor - 1f) > 0.01f)
        {
            finalMesh = new Mesh();
            finalMesh.name = "baby_scaled";

            Vector3[] verts = babyMesh.vertices;
            Vector3 center = babyMesh.bounds.center;
            Vector3[] scaledVerts = new Vector3[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                scaledVerts[i] = center + (verts[i] - center) * scaleFactor;
            }
            finalMesh.vertices = scaledVerts;
            finalMesh.normals = babyMesh.normals;
            finalMesh.uv = babyMesh.uv;
            finalMesh.triangles = babyMesh.triangles;
            finalMesh.RecalculateBounds();
            finalMesh.RecalculateNormals();

            Debug.Log($"Scaled baby mesh bounds: {finalMesh.bounds}");

            string scaledMeshPath = "Assets/Meshes-Models/baby_scaled.asset";
            AssetDatabase.CreateAsset(finalMesh, scaledMeshPath);
            AssetDatabase.SaveAssets();
        }

        string matPath = "Assets/Materials/baby_skin.mat";
        Material babyMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        string refMatPath = "Assets/Materials/AnimalModelMaterials/panda_head_mat.mat";
        Material refMat = AssetDatabase.LoadAssetAtPath<Material>(refMatPath);

        if (babyMat == null || babyMat.shader.name == "Standard")
        {
            if (refMat != null)
            {
                babyMat = new Material(refMat);
                Debug.Log($"Cloned material from {refMatPath}, shader={refMat.shader.name}");
            }
            else
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                babyMat = new Material(shader);
                Debug.LogWarning($"No reference material found, using shader={shader.name}");
            }

            if (AssetDatabase.LoadAssetAtPath<Material>(matPath) != null)
                AssetDatabase.DeleteAsset(matPath);
            AssetDatabase.CreateAsset(babyMat, matPath);
        }

        babyMat.SetColor("_BaseColor", new Color(1.0f, 0.85f, 0.72f, 1.0f));
        babyMat.SetColor("_Color", new Color(1.0f, 0.85f, 0.72f, 1.0f));
        babyMat.SetFloat("_Smoothness", 0.3f);
        babyMat.SetFloat("_Metallic", 0f);
        if (babyMat.HasProperty("_Cull"))
            babyMat.SetFloat("_Cull", 0f);
        EditorUtility.SetDirty(babyMat);
        AssetDatabase.SaveAssets();

        Debug.Log($"Baby material: shader={babyMat.shader.name}, color={babyMat.color}");

        string prefabPath = "Assets/Prefabs/AAI3Agent.prefab";
        var prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
        var skinMgr = prefabContents.GetComponentInChildren<AnimalSkinManager>(true);

        if (skinMgr == null)
        {
            var allComponents = prefabContents.GetComponentsInChildren<MonoBehaviour>(true);
            Debug.Log($"Found {allComponents.Length} MonoBehaviours on prefab hierarchy:");
            foreach (var comp in allComponents)
            {
                Debug.Log($"  - {comp.GetType().Name} on '{comp.gameObject.name}'");
                if (comp is AnimalSkinManager asm)
                    skinMgr = asm;
            }
        }

        if (skinMgr == null)
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
            Debug.LogError("AnimalSkinManager not found on prefab!");
            return;
        }

        Debug.Log($"Found AnimalSkinManager on '{skinMgr.gameObject.name}'");
        Debug.Log($"  Current skin ID: {skinMgr.AnimalSkinID}");
        Debug.Log($"  Animal names: [{string.Join(", ", skinMgr.AnimalNames)}]");
        for (int i = 0; i < AnimalSkinManager.AnimalCount; i++)
        {
            if (skinMgr.AnimalMeshes[i] != null)
                Debug.Log($"  Mesh[{i}]: {skinMgr.AnimalMeshes[i].name} bounds={skinMgr.AnimalMeshes[i].bounds}");
            else
                Debug.Log($"  Mesh[{i}]: null");
        }

        skinMgr.AnimalNames[2] = "baby";
        skinMgr.AnimalMeshes[2] = finalMesh;
        skinMgr.AnimalMaterials[2] = new MultiDimArray<Material>();
        skinMgr.AnimalMaterials[2].array = new Material[] { babyMat };
        skinMgr.AnimalSkinID = 2;

        var camSensor = prefabContents.GetComponentInChildren<Unity.MLAgents.Sensors.CameraSensorComponent>(true);
        if (camSensor != null)
        {
            float meshHeight = finalMesh.bounds.size.y * scaleFactor;
            camSensor.transform.localPosition = new Vector3(0f, meshHeight * 0.85f, 0.1f);
            Debug.Log($"Camera repositioned to eye level: y={meshHeight * 0.85f:F2}");
        }

        PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabContents);

        Debug.Log($"Baby model setup complete! Mesh={finalMesh.name} ({finalMesh.vertexCount} verts), bounds={finalMesh.bounds}");
    }

    static void SetupGraduatedDamageZone()
    {
        string prefabPath = "Assets/Prefabs/AAI3Arena.prefab";
        var prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);

        var existing = prefabContents.GetComponentInChildren<GraduatedDamageZone>(true);
        if (existing != null)
        {
            Debug.Log("GraduatedDamageZone already exists on arena prefab, skipping.");
            PrefabUtility.UnloadPrefabContents(prefabContents);
            return;
        }

        var zoneObj = new GameObject("GraduatedDamageZone");
        zoneObj.transform.SetParent(prefabContents.transform);
        zoneObj.transform.localPosition = new Vector3(20f, 0.5f, 20f);

        var collider = zoneObj.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(40f, 3f, 40f);
        collider.center = Vector3.zero;

        var damageZone = zoneObj.AddComponent<GraduatedDamageZone>();
        damageZone.damageEnabled = true;
        damageZone.islandCenter = new Vector3(20f, 0f, 20f);
        damageZone.safeRadius = 5f;
        damageZone.outerRadius = 8f;
        // Damage rates: UpdateHealth(x) applies health += 100*x, so -0.003 = -0.3 HP/tick, -0.015 = -1.5 HP/tick
        damageZone.minDamagePerTick = -0.003f;
        damageZone.maxDamagePerTick = -0.015f;

        PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabContents);

        Debug.Log("GraduatedDamageZone added to arena prefab: safe=5, outer=8, damage=-0.003 to -0.015");
    }

    private static string[] GetEnabledScenes()
    {
        var scenes = new System.Collections.Generic.List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                scenes.Add(scene.path);
        }
        return scenes.ToArray();
    }

    private static string GetArg(string name, string defaultVal)
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
                return args[i + 1];
        }
        return defaultVal;
    }
}
