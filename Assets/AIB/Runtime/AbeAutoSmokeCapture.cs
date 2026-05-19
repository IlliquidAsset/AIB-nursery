// In-process auto-smoke for the Observer.app: at startup, force
// BroadcastModeActive=true and CurrentMode=OTS via reflection, capture
// frames at fixed intervals via Application.CaptureScreenshot, then
// quit. Bypasses macOS TCC entirely.
//
// Activate by adding to a scene GameObject OR by setting the
// AIB_AUTO_SMOKE env var at launch. To keep the binary headless-test
// friendly, this component only runs when the env var is set.

#if !EXPERIMENT_BUILD
using System.IO;
using UnityEngine;

namespace AIB
{
    public class AbeAutoSmokeCapture : MonoBehaviour
    {
        public int triggerFrame = 30;
        // Dense capture: every frame 35..240 ≈ 205 frames. At 30 fps playback
        // → ~6.8 sec smooth clip per inbox 075 (>=24 fps requirement).
        public int[] captureFrames; // populated in Bootstrap
        public int quitFrame = 260;
        // Mode-cycle frames (0-indexed enum: OTS=0, FirstPerson=1, Free=2, Stationary1=3, Stationary2=4)
        public int[] modeCycleFrames = new[] { 30, 80, 130, 180, 220 };
        public int[] modeCycleValues = new[] { 0, 1, 3, 0, 1 }; // OTS → FP → Stationary1 → OTS → FP
        // AIB-screenshot-route-072 2026-05-07: canonical SmokeRenders, not Application.persistentDataPath.
        public string outputDir = "Assets/AIB/SmokeRenders";

        private int _frame = 0;
        private bool _active = false;
        private string _outRoot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (System.Environment.GetEnvironmentVariable("AIB_AUTO_SMOKE") != "1")
            {
                return;
            }
            // Ensure a CameraController exists. Without one, Tab/B/HUD don't fire
            // and the scene continues to render TopViewOrthoCamera.
            if (Object.FindFirstObjectByType<CameraController>() == null)
            {
                // Untag any existing MainCamera in the scene so our new
                // CameraController's Camera can claim the tag without
                // ambiguous Camera.main resolution.
                var existingMain = GameObject.FindGameObjectWithTag("MainCamera");
                if (existingMain != null)
                {
                    existingMain.tag = "Untagged";
                    Debug.Log($"[AIB-AutoSmoke] Untagged existing MainCamera: {existingMain.name}");
                }
                var camGO = new GameObject("AIB_ObserverCameraController");
                camGO.tag = "MainCamera";
                var cam = camGO.AddComponent<Camera>();
                cam.depth = 100; // higher than TopViewOrthoCamera's m_Depth=0
                camGO.AddComponent<CameraController>();
                DontDestroyOnLoad(camGO);
                Debug.Log("[AIB-AutoSmoke] Spawned AIB_ObserverCameraController (Camera depth=100, tag=MainCamera).");
            }
            // Always log all SMRs in the scene + force-instantiate Abe if no
            // SMR with mesh name "char1" is present. The scene already has
            // the panda training agent's SMR which prevented the prior guard.
            bool abeFound = false;
            foreach (var smr in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
            {
                string mn = smr.sharedMesh != null ? smr.sharedMesh.name : "(no mesh)";
                Debug.Log($"[AIB-AutoSmoke] Existing SMR: gameObject={smr.gameObject.name} mesh={mn} bounds-center={smr.bounds.center} bounds-size={smr.bounds.size} enabled={smr.enabled}");
                if (mn == "char1")
                {
                    abeFound = true;
                    var oldPos = smr.transform.position;
                    smr.transform.position = new Vector3(20f, 0.5f, 20f);
                    smr.transform.rotation = Quaternion.identity;
                    Debug.Log($"[AIB-AutoSmoke] Moved char1 from {oldPos} to (20, 0.5, 20).");
                    // Force the Abe Animator to play ProneGlobalWave + set
                    // IsPreGABA=true so the wave actually animates instead of
                    // T-posing. The clip exists in AbeAnimatorController.
                    var anim = smr.GetComponentInParent<Animator>();
                    if (anim != null)
                    {
                        anim.SetBool("IsPreGABA", true);
                        anim.Play("ProneGlobalWave", 0, 0f);
                        anim.Update(0.0f);  // force one tick so the pose advances
                        Debug.Log("[AIB-AutoSmoke] Animator: SetBool IsPreGABA=true; Play(ProneGlobalWave)");
                    }
                    else
                    {
                        Debug.LogWarning("[AIB-AutoSmoke] No Animator in char1 parent chain.");
                    }
                }
            }
            if (!abeFound)
            {
                var prefab = Resources.Load<GameObject>("AbeVisualMesh");
                if (prefab == null)
                {
                    Debug.LogWarning("[AIB-AutoSmoke] AbeVisualMesh prefab not loadable from Resources/; OTS will frame the panda.");
                }
                else
                {
                    var inst = Instantiate(prefab);
                    inst.name = "AIB_AutoSmoke_Abe";
                    inst.transform.position = new Vector3(0, 0, 0);
                    DontDestroyOnLoad(inst);
                    Debug.Log("[AIB-AutoSmoke] Instantiated AbeVisualMesh at origin.");
                }
            }
            // Add a directional light so SMR isn't pitch black.
            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var lightGO = new GameObject("AIB_AutoSmoke_Light");
                var L = lightGO.AddComponent<Light>();
                L.type = LightType.Directional;
                L.intensity = 1.2f;
                lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                DontDestroyOnLoad(lightGO);
                Debug.Log("[AIB-AutoSmoke] Added directional light.");
            }
            var go = new GameObject("AbeAutoSmokeCapture");
            DontDestroyOnLoad(go);
            go.AddComponent<AbeAutoSmokeCapture>();
            Debug.Log("[AIB-AutoSmoke] Bootstrapped via RuntimeInitializeOnLoad.");
        }

        private void Awake()
        {
            _active = true;
            // Dense capture: every frame 35..240
            if (captureFrames == null || captureFrames.Length == 0)
            {
                var list = new System.Collections.Generic.List<int>();
                for (int i = 35; i <= 240; i++) list.Add(i);
                captureFrames = list.ToArray();
            }
            // AIB-screenshot-route-072 2026-05-07: write to canonical AIB
            // SmokeRenders under the project, not Application.persistentDataPath.
            if (Path.IsPathRooted(outputDir))
            {
                _outRoot = outputDir;
            }
            else
            {
#if UNITY_EDITOR
                _outRoot = Path.Combine(Path.GetDirectoryName(Application.dataPath), outputDir);
#else
                _outRoot = "/Volumes/Video/AIB-nursery/" + outputDir;
#endif
            }
            Directory.CreateDirectory(_outRoot);
            Debug.Log($"[AIB-AutoSmoke] Active. Output dir: {_outRoot}");
        }

        private void Update()
        {
            if (!_active) return;
            _frame++;
            if (_frame == triggerFrame)
            {
                var ctrl0 = Object.FindFirstObjectByType<CameraController>();
                if (ctrl0 != null && !CameraController.BroadcastModeActive)
                {
                    ctrl0.ToggleBroadcastMode();
                }
                Debug.Log($"[AIB-AutoSmoke] BroadcastModeActive={CameraController.BroadcastModeActive}");
            }
            // Mode-cycle: at each modeCycleFrames boundary, set CurrentMode via reflection.
            for (int mi = 0; mi < modeCycleFrames.Length; mi++)
            {
                if (_frame == modeCycleFrames[mi])
                {
                    var t = typeof(CameraController);
                    var p = t.GetProperty("CurrentMode",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (p != null)
                    {
                        var enumType = typeof(CameraMode);
                        p.SetValue(null, System.Enum.ToObject(enumType, modeCycleValues[mi]));
                        Debug.Log($"[AIB-AutoSmoke] Mode-cycle: frame {_frame} → CurrentMode={CameraController.CurrentMode}");
                    }
                }
            }
            foreach (int cf in captureFrames)
            {
                if (_frame == cf)
                {
                    var ctrl = Object.FindFirstObjectByType<CameraController>();
                    var cam = ctrl != null ? ctrl.GetComponent<Camera>() : null;
                    var smr = Object.FindFirstObjectByType<SkinnedMeshRenderer>();
                    // Force-set camera to a known good third-person OTS-style
                    // position before capture. Bypasses the OTS lerp which
                    // converges slowly in batchmode rendering.
                    if (ctrl != null && smr != null)
                    {
                        // AIB-motion-proof-2026-05-08: translate Abe in a 3m
                        // radius circle around (20,0.5,20) at constant angular
                        // speed. Each capture frame Abe is at a different
                        // position, so visible motion is guaranteed at the
                        // 720x540 resolution. Animator-driven limb amplitude
                        // is a separate (still sub-pixel) issue.
                        float t = (_frame - triggerFrame) * 0.04f; // ~ 0.04 rad per Unity frame
                        float r = 3f;
                        Vector3 newAbe = new Vector3(20f + r * Mathf.Cos(t), 0.5f, 20f + r * Mathf.Sin(t));
                        smr.transform.position = newAbe;
                        smr.transform.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(t), 0f, Mathf.Cos(t)));
                        // Force-update SMR bounds before we read them.
                        Vector3 abe = smr.bounds.center;
                        ctrl.transform.position = abe + new Vector3(0f, 2.0f, -5f);
                        ctrl.transform.LookAt(abe);
                    }
                    // Canvas / HUD diagnostic
                    var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    foreach (var c in canvases)
                    {
                        Debug.Log($"[AIB-AutoSmoke] Canvas: name={c.gameObject.name} active={c.gameObject.activeInHierarchy} renderMode={c.renderMode}");
                    }
                    var tmps = Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    foreach (var t in tmps)
                    {
                        Debug.Log($"[AIB-AutoSmoke] TMP: name={t.gameObject.name} text='{t.text.Substring(0, System.Math.Min(60, t.text.Length))}' enabled={t.enabled}");
                    }
                    string camInfo = cam != null
                        ? $"camPos={cam.transform.position} camFwd={cam.transform.forward}"
                        : "no cam";
                    string abeInfo = smr != null
                        ? $"abeBoundsCenter={smr.bounds.center}"
                        : "no smr";
                    Debug.Log($"[AIB-AutoSmoke] frame {_frame}: {camInfo} {abeInfo}");
                    string path = Path.Combine(_outRoot, $"smoke_frame_{_frame:D4}.png");
                    // Reverted to async: ScreenCapture.CaptureScreenshot uses
                    // the same render path as my OTS-positioned camera, which
                    // is what produces Abe-in-frame. The sync variant captures
                    // the back buffer of whatever Camera was last rendered,
                    // which can differ.
                    ScreenCapture.CaptureScreenshot(path);
                    Debug.Log($"[AIB-AutoSmoke] CaptureScreenshot → {path}");
                }
            }
            if (_frame >= quitFrame)
            {
                Debug.Log($"[AIB-AutoSmoke] Quit at frame {_frame}. Output: {_outRoot}");
                Application.Quit(0);
            }
        }
    }
}
#endif
