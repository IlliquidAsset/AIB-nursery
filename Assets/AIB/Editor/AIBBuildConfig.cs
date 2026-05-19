#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.Collections.Generic;

namespace AIB.Editor
{
    public static class AIBBuildConfig
    {
        private const string ExperimentSymbol = "EXPERIMENT_BUILD";

        [MenuItem("AIB/Build/Set Experiment Mode")]
        public static void SetExperimentMode()
        {
            AddScriptingSymbol(ExperimentSymbol, BuildTargetGroup.Standalone);
            Debug.Log("[AIB] Scripting symbols set for EXPERIMENT build. EXPERIMENT_BUILD is now defined.");
        }

        [MenuItem("AIB/Build/Set Observer Mode")]
        public static void SetObserverMode()
        {
            RemoveScriptingSymbol(ExperimentSymbol, BuildTargetGroup.Standalone);
            Debug.Log("[AIB] Scripting symbols set for OBSERVER build. EXPERIMENT_BUILD is removed.");
        }

        [MenuItem("AIB/Build/Build Experiment (Linux Headless)")]
        public static void BuildExperiment()
        {
            SetExperimentMode();

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);

            string outputPath = GetArg("-outputPath", "Builds/AIB_Experiment.x86_64");

            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = outputPath,
                target = BuildTarget.StandaloneLinux64,
                subtarget = (int)StandaloneBuildSubtarget.Server,
                options = BuildOptions.EnableHeadlessMode
            };

            BuildReport report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[AIB] Experiment build failed: {report.summary.totalErrors} error(s)");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log($"[AIB] Experiment build succeeded: {outputPath}");
            }
        }

        [MenuItem("AIB/Build/Build Experiment (macOS)")]
        public static void BuildExperimentMac()
        {
            SetExperimentMode();

            string outputPath = GetArg("-outputPath", "Builds/AIB_Experiment.app");
            var namedBuildTarget = NamedBuildTarget.Standalone;
            int previousArchitecture = PlayerSettings.GetArchitecture(namedBuildTarget);

            try
            {
                PlayerSettings.SetArchitecture(namedBuildTarget, (int)OSArchitecture.ARM64);

                BuildPlayerOptions opts = new BuildPlayerOptions
                {
                    scenes = GetEnabledScenes(),
                    locationPathName = outputPath,
                    target = BuildTarget.StandaloneOSX,
                    subtarget = (int)StandaloneBuildSubtarget.Player,
                    options = BuildOptions.None
                };

                BuildReport report = BuildPipeline.BuildPlayer(opts);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError($"[AIB] macOS Experiment build failed: {report.summary.totalErrors} error(s)");
                    EditorApplication.Exit(1);
                }
                else
                {
                    Debug.Log($"[AIB] macOS Experiment build succeeded: {outputPath}");
                }
            }
            finally
            {
                PlayerSettings.SetArchitecture(namedBuildTarget, previousArchitecture);
            }
        }

        [MenuItem("AIB/Build/Build Observer (macOS)")]
        public static void BuildObserver()
        {
            SetObserverMode();

            string outputPath = GetArg("-outputPath", "Builds/AIB_Observer.app");
            var namedBuildTarget = NamedBuildTarget.Standalone;
            int previousArchitecture = PlayerSettings.GetArchitecture(namedBuildTarget);

            try
            {
                PlayerSettings.SetArchitecture(namedBuildTarget, (int)OSArchitecture.ARM64);

                BuildPlayerOptions opts = new BuildPlayerOptions
                {
                    scenes = GetEnabledScenes(),
                    locationPathName = outputPath,
                    target = BuildTarget.StandaloneOSX,
                    subtarget = (int)StandaloneBuildSubtarget.Player,
                    options = BuildOptions.CleanBuildCache | BuildOptions.DetailedBuildReport
                };

                BuildReport report = BuildPipeline.BuildPlayer(opts);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError($"[AIB] Observer build failed: {report.summary.totalErrors} error(s)");
                    EditorApplication.Exit(1);
                }
                else
                {
                    Debug.Log($"[AIB] Observer build succeeded: {outputPath}");
                }
            }
            finally
            {
                PlayerSettings.SetArchitecture(namedBuildTarget, previousArchitecture);
            }
        }

        [MenuItem("AIB/Build/Build Experiment Continuous (macOS)")]
        public static void BuildExperimentContinuousMac()
        {
            SetExperimentMode();

            string outputPath = GetArg("-outputPath", "Builds/AIB_Experiment_continuous.app");
            var namedBuildTarget = NamedBuildTarget.Standalone;
            int previousArchitecture = PlayerSettings.GetArchitecture(namedBuildTarget);

            try
            {
                PlayerSettings.SetArchitecture(namedBuildTarget, (int)OSArchitecture.ARM64);

                BuildPlayerOptions opts = new BuildPlayerOptions
                {
                    scenes = GetEnabledScenes(),
                    locationPathName = outputPath,
                    target = BuildTarget.StandaloneOSX,
                    subtarget = (int)StandaloneBuildSubtarget.Player,
                    options = BuildOptions.None
                };

                BuildReport report = BuildPipeline.BuildPlayer(opts);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError($"[AIB] Continuous Experiment build failed: {report.summary.totalErrors} error(s)");
                    EditorApplication.Exit(1);
                }
                else
                {
                    Debug.Log($"[AIB] Continuous Experiment build succeeded: {outputPath}");
                }
            }
            finally
            {
                PlayerSettings.SetArchitecture(namedBuildTarget, previousArchitecture);
            }
        }

        [MenuItem("AIB/Build/Check Current Mode")]
        public static void CheckCurrentMode()
        {
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            bool isExperiment = symbols.Contains(ExperimentSymbol);
            Debug.Log($"[AIB] Current mode: {(isExperiment ? "EXPERIMENT" : "OBSERVER")}");
            Debug.Log($"[AIB] Scripting symbols: {symbols}");
        }

        private static void AddScriptingSymbol(string symbol, BuildTargetGroup group)
        {
            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            if (!symbols.Contains(symbol))
            {
                symbols = string.IsNullOrEmpty(symbols) ? symbol : symbols + ";" + symbol;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, symbols);
            }
        }

        private static void RemoveScriptingSymbol(string symbol, BuildTargetGroup group)
        {
            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            var list = new List<string>(symbols.Split(';'));
            list.RemoveAll(s => s.Trim() == symbol);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", list));
        }

        private static string[] GetEnabledScenes()
        {
            var scenes = new List<string>();
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
}
#endif
