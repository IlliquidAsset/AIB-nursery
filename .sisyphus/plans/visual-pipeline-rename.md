# Visual Pipeline + Project Rename: AIB-nursery

## TL;DR

> **Quick Summary**: Rename project from `AIB-nursery` to `AIB-nursery`, then implement the PRD visual pipeline: fix camera height, swap agent model to Baby Abby, add visible Mother entity via ML-Agents side channel, and build CSV replay for offline video production.
>
> **Deliverables**:
> - Renamed project (GitHub repo, folder, all internal references)
> - Fixed first-person camera at eye height + fixed NE overview observer camera
> - Baby Abby as agent visual mesh with Idle/Calm_Walk animations
> - Mother entity visible in scene, position synced from Python via side channel
> - Extended CSV logging with mother position columns
> - Replay controller for offline video production from telemetry CSV
> - Fresh builds: Linux x86_64 (VM headless) + macOS ARM (local)
>
> **Estimated Effort**: Large
> **Parallel Execution**: YES — 5 waves
> **Critical Path**: Rename → Model Swap → Mother Entity → Replay → Final Build

---

## Context

### Original Request
Rename the project from `AIB-nursery` to `AIB-nursery` and implement the visual pipeline PRD (`PRD_visual_pipeline.md`): 4 phases covering camera fix, model swap, mother entity, and replay mode. The AIB agent (separate project at `/Users/kendrick/Documents/dev/AIB`) will handle all Python-side changes and update references to the new name on its end.

### Interview Summary
**Key Discussions**:
- Abe and Baby Abby are both playable agents (gender-specific). The nursery provides BOTH — AIB decides which to use.
- Mother Abby looks similar to Baby Abby (mother-daughter visual pair). Generic Mother is a separate character.
- ALL 4 models need prefabs: Abe (already has one), Baby Abby, Mother, Mother Abby.
- Model selection mechanism: via arena YAML config fields (`agentModel`, `motherModel`). Not yet implemented.
- PRD builds ON TOP of existing observer/experiment architecture. Not replacing it.
- All model FBX files already imported to Unity project — no copy step needed.
- Mother position via ML-Agents side channel (new `MotherPositionSideChannel`), not via existing HTTP/WebSocket.
- `_pending_scripts/MotherNPC.cs` used as starting point — adapt from autonomous BT/NavMesh to side-channel position receiver.
- No automated unit tests — agent-executed QA only. Exception: existing `CSVWriterTests.cs` must stay passing.
- Experiments run headless on VM → telemetry pulled to local Mac → observer mode replays with full visuals for video production.

**Research Findings**:
- Rename scope is clean: 3 critical files (git remote, codeql.yml, .slnx filename) + cosmetic updates in README, workflows, ProjectSettings, .sisyphus.
- ALL 4 model FBX sets already imported: `Assets/AIB/Models/Abe/` (GUID: 920f85f2), `BabyAbby/` (GUID: 7d9ac84b), `Mother/`, `MotherAbby/` — each with character FBX + 4 animation FBX + textures.
- Abe already has prefab (`AbeVisualMesh.prefab`) + AnimatorController. Other 3 models need prefabs.
- AIB `CameraController.cs` exists at `Assets/AIB/Runtime/` with `fpHeightOffset = 0.8f` but is NOT wired into any scene or prefab (GUID not referenced). `AIBOneClickSetup.cs` has code to wire it but was apparently never run for camera.
- `CSVWriter.cs` logs 23 columns per step (episode, step, health, reward, position XYZ, etc.) but NO mother position.
- Only one side channel: `ArenasParametersSideChannel` (GUID: 9c36c837-cad5-498a-b675-bc19c9370072). Registration in `AAI3EnvironmentManager.Awake() → InitialiseSideChannel()`.
- `MotherNPC.cs` in `_pending_scripts/` uses NavMeshAgent + CleverCrow BehaviorTree — both must be stripped.
- `AbeStateBuffer.Write()` doesn't copy `motherStrength` field — pre-existing bug to fix.
- `AAI3Agent.prefab` has BOTH `AnimalSkinManager` AND `AbeVisualController`. Old sphere mesh disabled, Abe visual active.
- Unity Recorder `5.1.5` available in `Packages/manifest.json`.
- `codeql.yml` references `AIB-nursery.slnx` and should stay aligned with the solution file.

### Metis Review
**Identified Gaps** (addressed):
- Mother/ vs MotherAbby/ folder ambiguity → RESOLVED: Both are real characters. Mother = generic mother. MotherAbby = looks like Baby Abby (mother-daughter pair). Both get prefabs.
- Mother on/off signal not defined → Using float[4] payload `[x, y, z, active]` in side channel.
- `AbeStateBuffer.Write()` missing `motherStrength` copy → Fix included in Task 3.
- `CSVWriterTests.cs` needs updating when `LogToCSV` signature changes → Included in Task 3.
- `#if EXPERIMENT_BUILD` guards needed for new scripts → Guardrail in every task.
- Phase 4 output specs → Defaults: 1920×1080, 30fps, MP4 (H.264), NE stationary camera.
- CSV format mismatch: PRD says `tick_log.csv` but existing CSVWriter produces `Observations_*.csv` → Use existing format.
- `AIBOneClickSetup.CreateAbePrefab()` pattern used as template for 3 new prefab creation methods → Included in Task 2.
- Observer build doesn't run ML-Agents — mother position must flow through `AbeStatePayload` for observer to see it → Included in Task 3.

---

## Work Objectives

### Core Objective
Rename the project and implement the visual pipeline so that produced videos show Baby Abby (not stock sphere), Mother entity (when present), camera at eye height, and support offline replay from experiment telemetry.

### Concrete Deliverables
- Renamed GitHub repo `IlliquidAsset/AIB-nursery` + local folder `/Volumes/Video 1/AIB-nursery`
- 4 character prefabs: `AbeVisualMesh` (verify existing), `BabyAbbyVisualMesh`, `MotherVisualMesh`, `MotherAbbyVisualMesh`
- Model selection via arena YAML config: `agentModel` (abe/baby_abby) + `motherModel` (mother/mother_abby)
- `Assets/AIB/Runtime/ModelSelector.cs` — switches agent/mother visual based on YAML config
- `Assets/AIB/Runtime/MotherPositionSideChannel.cs` + `MotherController.cs`
- Extended `CSVWriter.cs` with mother position columns
- Extended `AbeStatePayload.cs` + `AbeStateBuffer.cs` with mother fields
- `Assets/AIB/Runtime/ReplayController.cs` for CSV-to-video production
- Camera positioned at eye height + NE observer camera configured
- macOS ARM build + Linux x86_64 headless build

### Definition of Done
- [ ] `gh repo view --json name` returns `AIB-nursery`
- [ ] `grep -r "AIB-nursery" --include="*.yml" --include="*.md" --include="*.slnx" --include="*.asset" .` returns expected matches for the new name
- [ ] First-person camera Y offset is ~0.8 above agent position
- [ ] All 4 prefabs exist: AbeVisualMesh, BabyAbbyVisualMesh, MotherVisualMesh, MotherAbbyVisualMesh
- [ ] Model selection works via arena YAML config (`agentModel`, `motherModel` fields)
- [ ] Agent switchable between Abe and Baby Abby (no hard-wired default beyond backward compat)
- [ ] Mother character appears at correct position when side channel sends `active=1.0`
- [ ] Mother character disappears when side channel sends `active=0.0`
- [ ] `Observations_*.csv` includes `MotherX,MotherY,MotherZ,MotherActive` columns
- [ ] Observer state payload includes mother position fields and `AbeStateBuffer` copies them
- [ ] Replay from CSV produces MP4 video showing agent + mother movement
- [ ] macOS ARM build succeeds with zero errors
- [ ] Linux x86_64 headless build succeeds with zero errors

### Must Have
- All 4 character prefabs available: Abe, Baby Abby, Mother, Mother Abby
- Model selection via arena YAML config (`agentModel`, `motherModel` fields)
- Agent switchable between Abe and Baby Abby at runtime via config
- Mother model switchable between Mother and Mother Abby via config
- Mother visible when `mother_on=True`, invisible when `mother_on=False`
- Camera at eye height (~0.8 above agent center) in first-person mode
- Fixed NE observer camera at (30, 10, 30) looking at arena center (20, 1.5, 20)
- CSV logging includes mother position per tick
- Replay produces video from experiment telemetry CSV
- Project fully renamed to AIB-nursery

### Must NOT Have (Guardrails)
- Do NOT hard-wire a specific model as "the" agent — ALL models must be selectable via config
- Do NOT add more than 2 animator states per character (Idle + Calm_Walk only)
- Do NOT restructure CSVWriter threading/architecture — add columns only
- Do NOT remove `com.fluid.behavior-tree` from `Packages/manifest.json` (harmless, removing causes reimport storm)
- Do NOT rename C# namespaces (`AIB` namespace stays as-is)
- Do NOT rename assembly definitions (`AIB.Runtime.asmdef`, `AIB.Editor.asmdef` stay)
- Do NOT add playback controls, timeline UI, or interactive features to replay (play once, record, done)
- Do NOT modify anything in the AIB repo (`/Users/kendrick/Documents/dev/AIB`)
- Do NOT create UI elements beyond what's needed for replay
- Do NOT add more than 4 CSV columns for mother data (MotherX, MotherY, MotherZ, MotherActive)

---

## Verification Strategy (MANDATORY)

> **UNIVERSAL RULE: ZERO HUMAN INTERVENTION**
>
> ALL tasks in this plan MUST be verifiable WITHOUT any human action.
> ALL verification is executed by the agent using tools (Bash, interactive_bash, grep). No exceptions.

### Test Decision
- **Infrastructure exists**: YES (Unity EditMode/PlayMode test dirs, `CSVWriterTests.cs` with 5 tests)
- **Automated tests**: NO new tests. Exception: `CSVWriterTests.cs` must be updated to stay passing when `LogToCSV` signature changes.
- **Framework**: Unity Test Runner (existing, via CLI batchmode)

### Agent-Executed QA Scenarios (MANDATORY — ALL tasks)

**Verification Tool by Deliverable Type:**

| Type | Tool | How Agent Verifies |
|------|------|-------------------|
| **Build success** | Bash (Unity CLI batchmode) | Run build command, check exit code 0 |
| **File existence** | Bash (ls) | Verify files exist at expected paths |
| **Content correctness** | Bash (grep) | Search for expected/forbidden strings |
| **Git/GitHub** | Bash (git/gh) | Verify repo name, remote URL |
| **Unity project** | Bash (Unity -batchmode -executeMethod) | Run setup/build scripts headlessly |
| **CSV format** | Bash (head) | Verify header row contains expected columns |

---

## Execution Strategy

### Parallel Execution Waves

```
Wave 1 (Start Immediately):
└── Task 0: Rename project to AIB-nursery [no dependencies]

Wave 2 (After Wave 1):
├── Task 1: Camera fix + NE observer camera [depends: 0]
└── Task 2: Baby Abby model swap + Mother prefab [depends: 0]

Wave 3 (After Wave 2):
└── Task 3: Mother entity — side channel + controller + CSV + state payload [depends: 2]

Wave 4 (After Wave 3):
└── Task 4: Replay controller [depends: 1, 3]

Wave 5 (After Wave 4):
└── Task 5: Final builds + integration verification [depends: all]

Critical Path: Task 0 → Task 2 → Task 3 → Task 4 → Task 5
Parallel Speedup: ~15% (Wave 2 parallelizes Tasks 1+2)
```

### Dependency Matrix

| Task | Depends On | Blocks | Can Parallelize With |
|------|------------|--------|---------------------|
| 0 (Rename) | None | 1, 2 | None (changes working dir) |
| 1 (Camera) | 0 | 4, 5 | 2 |
| 2 (Model Swap) | 0 | 3, 5 | 1 |
| 3 (Mother) | 2 | 4, 5 | None |
| 4 (Replay) | 1, 3 | 5 | None |
| 5 (Builds) | 1, 2, 3, 4 | None | None (final) |

### Agent Dispatch Summary

| Wave | Tasks | Recommended Agents |
|------|-------|-------------------|
| 1 | 0 | `task(category="quick", load_skills=["git-master"])` |
| 2 | 1, 2 | `task(category="unspecified-high")` — dispatch in parallel |
| 3 | 3 | `task(category="unspecified-high")` |
| 4 | 4 | `task(category="unspecified-high")` |
| 5 | 5 | `task(category="quick")` |

---

## TODOs

- [ ] 0. Rename project from `AIB-nursery` to `AIB-nursery`

  **What to do**:
  1. Rename GitHub repo: `gh repo rename AIB-nursery`
  2. Rename local folder: from parent dir, `mv "AIB-nursery" "AIB-nursery"`
  3. Update git remote: `git remote set-url origin https://github.com/IlliquidAsset/AIB-nursery`
  4. Rename solution file: `mv AIB-nursery.slnx AIB-nursery.slnx`
  5. Update `.github/workflows/codeql.yml` line 42: ensure `dotnet build AIB-nursery.slnx`
  6. Update `.github/workflows/run_tests.yml` line 1: `name: Test AIB-nursery` and line 13: `name: Run Unity Tests on AIB-nursery`
  7. Update `README.md` lines 1, 3, 5: Replace "Animal-AI" branding with "AIB-nursery"
  8. Update `ProjectSettings/ProjectSettings.asset`:
     - Line 16: `productName: AIB-nursery`
     - Line 169: `Standalone: com.IlliquidAsset.AIB-nursery`
     - Line 704: `metroTileShortName: AIB-nursery`
  9. Update `.sisyphus/plans/build-mother-abby-mvp.md` line 28: path reference
  10. Verify: `grep -r "AIB-nursery" --include="*.yml" --include="*.md" --include="*.slnx" --include="*.asset" .` returns expected matches (excluding .git/)
  11. Commit all changes

  **Must NOT do**:
  - Do NOT rename C# namespaces, assembly definitions, or class names
  - Do NOT modify source code (.cs files) for naming — only config/metadata
  - Do NOT rename the `Assets/AIB/` directory (that's a different name)

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Straightforward file edits + git commands, no complex logic
  - **Skills**: [`git-master`]
    - `git-master`: Needed for repo rename, remote update, commit

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 1 (solo)
  - **Blocks**: Tasks 1, 2, 3, 4, 5
  - **Blocked By**: None

  **References**:

  **Pattern References**:
  - `.github/workflows/codeql.yml:42` — Build command that references solution file name
  - `.github/workflows/run_tests.yml:1,13` — Workflow display names containing repo name

  **File References**:
  - `AIB-nursery.slnx` — Solution file to rename (filename only, no internal string matches)
  - `ProjectSettings/ProjectSettings.asset:16,169,704` — Unity product metadata
  - `README.md:1,3,5` — Project branding text
  - `.sisyphus/plans/build-mother-abby-mvp.md:28` — Hardcoded path reference

  **WHY Each Reference Matters**:
  - `codeql.yml` line 42: CI build command will fail if solution file name doesn't match
  - `.slnx` filename: IDEs and build tools reference this by name
  - `ProjectSettings.asset`: Unity uses productName for built application name and bundle ID
  - Other references are cosmetic but should be consistent

  **Acceptance Criteria**:

  **Agent-Executed QA Scenarios:**

  ```
  Scenario: GitHub repo renamed successfully
    Tool: Bash (gh)
    Preconditions: gh CLI authenticated
    Steps:
      1. Run: gh repo view --json name -q '.name'
      2. Assert: output equals "AIB-nursery"
      3. Run: git remote -v
      4. Assert: origin URL contains "AIB-nursery"
    Expected Result: GitHub repo and local remote both reference AIB-nursery
    Evidence: Command output captured

  Scenario: No remaining references to old name
    Tool: Bash (grep)
    Preconditions: In project root directory
    Steps:
      1. Run: grep -r "animal-ai-unity" --include="*.yml" --include="*.md" --include="*.slnx" --include="*.asset" . | grep -v ".git/"
      2. Assert: zero matches returned
      3. Run: ls *.slnx
      4. Assert: only AIB-nursery.slnx exists
    Expected Result: Complete rename with no stale references
    Evidence: grep output (should be empty)

  Scenario: Solution file renamed correctly
    Tool: Bash (ls)
    Steps:
      1. Run: ls -la AIB-nursery.slnx
      2. Assert: file exists
      3. Run: ls -la AIB-nursery.slnx
      4. Assert: file does NOT exist (exit code 1)
    Expected Result: Old .slnx gone, new one present
    Evidence: ls output
  ```

  **Commit**: YES
  - Message: `chore: rename project from AIB-nursery to AIB-nursery`
  - Files: `AIB-nursery.slnx`, `.github/workflows/`, `README.md`, `ProjectSettings/ProjectSettings.asset`, `.sisyphus/`
  - Pre-commit: `grep -r "animal-ai-unity" --include="*.yml" --include="*.md" --include="*.slnx" --include="*.asset" . | grep -v ".git/" | wc -l` → should be 0

---

- [ ] 1. Camera fix + NE observer camera (PRD Phase 1)

  **What to do**:
  1. **First-person camera height fix**: The AIB `CameraController.cs` at `Assets/AIB/Runtime/CameraController.cs` already has `fpHeightOffset = 0.8f` which positions the camera at eye height. However, it is NOT wired into the scene/prefab. The original `PlayerControls.cs` camera system is still active.
     - Option A (preferred): Run `AIBOneClickSetup` which has code at ~line 264 to add CameraController to the main camera: `TryAddComponent(mainCam.gameObject, "AIB.CameraController")`. Verify this wires correctly.
     - Option B (fallback): If CameraController cannot be wired easily without breaking PlayerControls input handling (R=reset, Q=quit, P=screenshot), then directly modify the first-person camera's local Y position in `AAI3Agent.prefab`. Find the camera tagged `MainCamera` and set its local position Y to ~0.8.
     - Either way, verify: first-person camera looks at horizon, not upward past agent's body.
  2. **NE observer camera**: Configure a fixed observer camera for video recording:
     - If using `CameraController`: set `stationary1WorldPosition = new Vector3(30f, 10f, 30f)` and `stationary1LookTarget = new Vector3(20f, 1.5f, 20f)` (or equivalent serialized fields)
     - If `CameraController` is not wired: add a new camera child to the arena prefab at position (30, 10, 30) looking at (20, 1.5, 20). Name it `ObserverCameraNE`.
     - This camera shows full island, agent, mother zone, and lava moat from NE corner.
  3. **Verify**: Ensure existing observer cameras (`OTSRecordCamera` at offset (0,2.5,-4), `TopViewOrthoCamera` at (20,20,20)) still function.

  **Must NOT do**:
  - Do NOT remove or rewrite `PlayerControls.cs` — it handles non-camera input too
  - Do NOT add new camera modes beyond what CameraController already supports (5 modes)
  - Do NOT change OTSRecordCamera or TopViewOrthoCamera positions

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
    - Reason: Requires understanding Unity prefab/scene structure and camera wiring
  - **Skills**: `[]`

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2 (with Task 2)
  - **Blocks**: Tasks 4, 5
  - **Blocked By**: Task 0 (rename)

  **References**:

  **Pattern References**:
  - `Assets/AIB/Runtime/CameraController.cs` — Full camera system with 5 modes, `fpHeightOffset = 0.8f` at line ~15. Study how modes work and what fields control stationary camera positions.
  - `Assets/Scripts/PlayerControls.cs` — Active camera manager. Camera tags: `MainCamera` (first-person), `AgentCamMid` (third-person), `camBase` (bird's eye). Also handles R/Q/P input. Do NOT break this.
  - `Assets/AIB/Editor/AIBOneClickSetup.cs:264` — Has `TryAddComponent(mainCam.gameObject, "AIB.CameraController")` for wiring CameraController. Check if this already runs during setup.

  **Prefab References**:
  - `Assets/Prefabs/AAI3Agent.prefab` — Agent prefab containing camera children. `AgentCamMid` at local `(0,0,0)` under Agent at `(0, 1.078, 0.1)`. Find the `MainCamera`-tagged camera and check its Y position.
  - `Assets/Prefabs/AAI3Arena.prefab` — Arena prefab containing `OTSRecordCamera` with offset `(0, 2.5, -4)`.

  **Scene References**:
  - `Assets/Scenes/AAI3EnvironmentManager.unity` — Has `TopViewOrthoCamera` at `(20,20,20)` and `Main Camera` at `(20,50,20)`. The scene `Main Camera` is NOT the agent first-person camera.

  **API References**:
  - `Assets/AIB/Runtime/OTSRecordCamera.cs` — Existing observer camera with `CaptureFrame()` and `CaptureJPG()`. May be useful reference for NE camera.

  **WHY Each Reference Matters**:
  - `CameraController.cs`: Already solves the camera height problem (0.8f offset). The question is whether to wire it in vs. patching the prefab directly.
  - `PlayerControls.cs`: Must not be broken — it handles input beyond cameras.
  - `AIBOneClickSetup.cs`: Contains automation that may already wire the camera if re-run.
  - Prefab positions: Current camera transforms tell you what needs changing.

  **Acceptance Criteria**:

  **Agent-Executed QA Scenarios:**

  ```
  Scenario: First-person camera at eye height
    Tool: Bash (grep/Unity inspection)
    Preconditions: Project open or buildable
    Steps:
      1. Grep CameraController.cs for fpHeightOffset value
      2. Assert: fpHeightOffset >= 0.7 and <= 0.9 (eye height range)
      3. If using CameraController: Verify its script GUID appears in AAI3Agent.prefab or scene file
      4. If using direct prefab edit: Verify MainCamera-tagged camera local Y >= 0.7
    Expected Result: First-person camera positioned at eye height
    Evidence: grep output showing offset value + prefab/scene reference

  Scenario: NE observer camera configured
    Tool: Bash (grep)
    Steps:
      1. Search for Vector3(30, 10, 30) or equivalent in CameraController.cs or scene/prefab files
      2. Assert: Found in exactly one location
      3. Search for look target (20, 1.5, 20) or equivalent
      4. Assert: Found
    Expected Result: Fixed NE camera position configured
    Evidence: grep output showing camera position values

  Scenario: Existing cameras not broken
    Tool: Bash (grep)
    Steps:
      1. Grep AAI3Arena.prefab for OTSRecordCamera
      2. Assert: still present
      3. Grep scene file for TopViewOrthoCamera
      4. Assert: still present
    Expected Result: All pre-existing cameras intact
    Evidence: grep output
  ```

  **Commit**: YES (groups with Task 2 if in same wave)
  - Message: `feat(camera): fix first-person eye height + add NE observer camera`
  - Files: `Assets/AIB/Runtime/CameraController.cs` (if modified), `Assets/Prefabs/AAI3Agent.prefab` (if modified), scene files
  - Pre-commit: grep for camera position values

---

- [ ] 2. Create all character prefabs + model selection system (PRD Phase 2)

  **What to do**:

  **2a. Verify Abe prefab** (already exists):
  - Confirm `Assets/AIB/Prefabs/AbeVisualMesh.prefab` has: SkinnedMeshRenderer, Animator with `AbeAnimatorController`, valid material from Abe texture.
  - Confirm `Assets/AIB/Animations/AbeAnimatorController.controller` exists and has at minimum Idle + walk states.
  - If anything is broken or missing, fix it using the same process as the new prefabs below.

  **2b. Create Baby Abby prefab** (`Assets/AIB/Prefabs/BabyAbbyVisualMesh.prefab`):
  - Source FBX: `Assets/AIB/Models/BabyAbby/Meshy_AI_Character_output.fbx` (already imported, GUID: 7d9ac84b)
  - Set rig type to Humanoid in FBX import settings, validate avatar mapping
  - If humanoid auto-mapping fails: use Generic rig as fallback (add null guard for `animator.GetBoneTransform(HumanBodyBones.Head)` in `AbeVisualController.cs:32`)
  - Create prefab with: SkinnedMeshRenderer, Animator, material from `Meshy_AI_texture_0.png`
  - Follow structure of existing `Assets/AIB/Prefabs/AbeVisualMesh.prefab`
  - Create `Assets/AIB/Animations/BabyAbbyAnimatorController.controller`: exactly 2 states (Idle + Calm_Walk), Bool `IsMoving` parameter

  **2c. Create Mother prefab** (`Assets/AIB/Prefabs/MotherVisualMesh.prefab`):
  - Source FBX: `Assets/AIB/Models/Mother/Meshy_AI_Character_output.fbx`
  - Same process: Humanoid rig, material, SkinnedMeshRenderer
  - Create `Assets/AIB/Animations/MotherAnimatorController.controller`: 2 states (Idle + Calm_Walk)

  **2d. Create Mother Abby prefab** (`Assets/AIB/Prefabs/MotherAbbyVisualMesh.prefab`):
  - Source FBX: `Assets/AIB/Models/MotherAbby/Meshy_AI_Character_output.fbx`
  - Same process: Humanoid rig, material, SkinnedMeshRenderer
  - Create `Assets/AIB/Animations/MotherAbbyAnimatorController.controller`: 2 states (Idle + Calm_Walk)
  - Mother Abby should look similar to Baby Abby (mother-daughter pair). If textures/materials need adjustment to reinforce visual similarity, note it but don't block on it.

  **2e. Model selection system** — Create `Assets/AIB/Runtime/ModelSelector.cs`:
  - Reads `agentModel` and `motherModel` from arena YAML config (parsed by `ArenasParameters`)
  - Supported agent values: `"abe"` (default for backward compat), `"baby_abby"`
  - Supported mother values: `"mother"` (default), `"mother_abby"`
  - On arena config received: instantiates the correct visual mesh prefab for the agent
  - Replaces current `AbeVisualController.agentMeshPrefab` single-slot design with a lookup:
    ```
    Dictionary<string, GameObject> agentPrefabs = {
      "abe" → AbeVisualMesh.prefab,
      "baby_abby" → BabyAbbyVisualMesh.prefab
    };
    Dictionary<string, GameObject> motherPrefabs = {
      "mother" → MotherVisualMesh.prefab,
      "mother_abby" → MotherAbbyVisualMesh.prefab
    };
    ```
  - Serialized field for each prefab reference (drag-and-drop in editor or set via setup script)
  - When model changes: destroy old visual, instantiate new one, re-bind animator
  - Default (no config / backward compat): use `"abe"` for agent, `"mother"` for mother

  **2f. Extend arena YAML parsing**:
  - Modify `Assets/Scripts/ArenasParameters.cs` to parse `agentModel` and `motherModel` fields from YAML
  - These are top-level fields in the arena config, not per-arena fields
  - Store as string properties accessible by `ModelSelector`
  - If fields are absent in YAML: use defaults (`"abe"`, `"mother"`)

  **2g. Add null guard in AbeVisualController**:
  - `Assets/AIB/Runtime/AbeVisualController.cs:32` — `animator.GetBoneTransform(HumanBodyBones.Head)` returns null with Generic rig
  - Add `if (headBone != null)` guard to prevent NullReferenceException

  **Must NOT do**:
  - Do NOT hard-wire a specific model as "the" agent
  - Do NOT add more than 2 animator states (Idle + Calm_Walk) per AnimatorController
  - Do NOT delete `AbeVisualMesh.prefab` or `AbeAnimatorController.controller`
  - Do NOT wire Mother prefab into scene (that's Task 3)
  - Do NOT add visual polish or texture adjustments — just get the prefabs created and selectable

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
    - Reason: Unity prefab/animator creation for 3 new models, FBX import settings, YAML parsing extension, model selector component — significant scope
  - **Skills**: `[]`

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2 (with Task 1)
  - **Blocks**: Tasks 3, 5
  - **Blocked By**: Task 0 (rename)

  **References**:

  **Pattern References**:
  - `Assets/AIB/Prefabs/AbeVisualMesh.prefab` — FOLLOW THIS PATTERN exactly for all 3 new prefabs. Has Animator with controller GUID, SkinnedMeshRenderer, avatar. Replicate structure.
  - `Assets/AIB/Animations/AbeAnimatorController.controller` — Existing animator. Reference for state machine structure (simplify to 2 states only for each new controller).
  - `Assets/AIB/Editor/AIBOneClickSetup.cs:152-237` — `CreateAbePrefab()` method shows the full prefab creation flow: find FBX, create material, setup animator, create prefab. Generalize this into a reusable method or call it 3 times with different model paths.

  **Model References (all already imported)**:
  - `Assets/AIB/Models/Abe/` — Abe character (GUID: 920f85f2) + 11 animation FBX + texture
  - `Assets/AIB/Models/BabyAbby/` — Baby Abby character (GUID: 7d9ac84b) + 4 animation FBX (Idle, Calm_Walk, Comfort_Embrace, Crouch_Reach) + textures
  - `Assets/AIB/Models/Mother/` — Generic mother + 4 animation FBX + textures
  - `Assets/AIB/Models/MotherAbby/` — Mother Abby (looks like Baby Abby) + 4 animation FBX + textures

  **Code References**:
  - `Assets/AIB/Runtime/AbeVisualController.cs` — Has `agentMeshPrefab` serialized field (line ~12). Line 32: `animator.GetBoneTransform(HumanBodyBones.Head)` needs null guard. ModelSelector will extend or work alongside this.
  - `Assets/Scripts/ArenasParameters.cs` — YAML deserialization. Study how existing arena config fields are parsed to add `agentModel` and `motherModel` string fields.
  - `Assets/Scripts/ArenasParametersSideChannel.cs` — Delivers raw YAML bytes. No changes needed here — just the parser.
  - `Assets/Prefabs/AAI3Agent.prefab` — Contains `AbeVisualController` component. `ModelSelector` will be added here.

  **WHY Each Reference Matters**:
  - `AbeVisualMesh.prefab`: Exact template for all new prefabs. Don't guess — replicate.
  - `AIBOneClickSetup.CreateAbePrefab()`: Automates material creation, animator setup, avatar config. Generalize to handle any model path.
  - `ArenasParameters.cs`: This is where YAML gets deserialized. Model selection fields must live here to flow naturally from Python → side channel → Unity.
  - `AbeVisualController.cs:32`: Head bone null risk with Generic rig across all 4 models.

  **Acceptance Criteria**:

  **Agent-Executed QA Scenarios:**

  ```
  Scenario: All 4 prefabs exist with correct structure
    Tool: Bash (ls)
    Steps:
      1. ls Assets/AIB/Prefabs/AbeVisualMesh.prefab
      2. ls Assets/AIB/Prefabs/BabyAbbyVisualMesh.prefab
      3. ls Assets/AIB/Prefabs/MotherVisualMesh.prefab
      4. ls Assets/AIB/Prefabs/MotherAbbyVisualMesh.prefab
      5. Assert: all 4 files exist
      6. ls Assets/AIB/Animations/BabyAbbyAnimatorController.controller
      7. ls Assets/AIB/Animations/MotherAnimatorController.controller
      8. ls Assets/AIB/Animations/MotherAbbyAnimatorController.controller
      9. Assert: all 3 new controllers exist (Abe's already exists)
    Expected Result: Full model roster with prefabs and animators
    Evidence: ls output showing all files

  Scenario: ModelSelector component created with prefab references
    Tool: Bash (grep)
    Steps:
      1. ls Assets/AIB/Runtime/ModelSelector.cs
      2. Assert: file exists
      3. grep "agentModel\|motherModel" Assets/AIB/Runtime/ModelSelector.cs
      4. Assert: both config fields referenced
      5. grep "abe\|baby_abby\|mother\|mother_abby" Assets/AIB/Runtime/ModelSelector.cs
      6. Assert: all 4 model identifiers present
    Expected Result: ModelSelector supports all 4 models with config-driven selection
    Evidence: grep output

  Scenario: ArenasParameters extended with model selection fields
    Tool: Bash (grep)
    Steps:
      1. grep "agentModel\|motherModel" Assets/Scripts/ArenasParameters.cs
      2. Assert: both fields present in YAML parsing
      3. grep "abe\|baby_abby" Assets/Scripts/ArenasParameters.cs
      4. Assert: default values present
    Expected Result: YAML config can specify model selection
    Evidence: grep output

  Scenario: AbeVisualController has null guard for head bone
    Tool: Bash (grep)
    Steps:
      1. grep "GetBoneTransform" Assets/AIB/Runtime/AbeVisualController.cs
      2. Assert: line includes null check ("!= null" or "?.")
    Expected Result: No NullReferenceException with Generic rig fallback
    Evidence: grep output

  Scenario: No model hard-wired as default agent
    Tool: Bash (grep)
    Steps:
      1. grep -n "agentMeshPrefab" Assets/AIB/Runtime/ModelSelector.cs
      2. Assert: prefab selected by config lookup, not hardcoded assignment
      3. grep "\"abe\"" Assets/AIB/Runtime/ModelSelector.cs
      4. Assert: "abe" appears as default/fallback, not as only option
    Expected Result: Model selection is config-driven with backward-compatible default
    Evidence: grep output
  ```

  **Commit**: YES
  - Message: `feat(models): create all 4 character prefabs + YAML-driven model selection`
  - Files: `Assets/AIB/Prefabs/BabyAbbyVisualMesh.prefab`, `Assets/AIB/Prefabs/MotherVisualMesh.prefab`, `Assets/AIB/Prefabs/MotherAbbyVisualMesh.prefab`, `Assets/AIB/Animations/BabyAbby*.controller`, `Assets/AIB/Animations/Mother*.controller`, `Assets/AIB/Animations/MotherAbby*.controller`, `Assets/AIB/Runtime/ModelSelector.cs`, `Assets/Scripts/ArenasParameters.cs`, `Assets/AIB/Runtime/AbeVisualController.cs`, `Assets/AIB/Editor/AIBOneClickSetup.cs`
  - Pre-commit: All 4 prefabs exist, ModelSelector references all models

---

- [ ] 3. Mother entity — side channel + controller + CSV + state payload (PRD Phase 3)

  **What to do**:
  This is the most complex task. Four coordinated subsystems must be created/extended:

  **3a. Mother Position Side Channel** (`Assets/AIB/Runtime/MotherPositionSideChannel.cs`):
  - New class inheriting `Unity.MLAgents.SideChannels.SideChannel`
  - New unique GUID (generate one, e.g., `a]1b2c3d4-e5f6-7890-abcd-ef1234567890` — use a real UUID)
  - `OnMessageReceived(IncomingMessage msg)`:
    - Read 4 floats: `[x, y, z, active]`
    - `active >= 0.5` means mother is present, `< 0.5` means absent
    - Expose via public properties or event: `Vector3 MotherPosition`, `bool MotherActive`
  - Guard with `#if EXPERIMENT_BUILD` — this only runs in experiment builds (observer gets data via WebSocket)

  **3b. Mother Controller** (`Assets/AIB/Runtime/MotherController.cs`):
  - Start from `_pending_scripts/MotherNPC.cs` — move to `Assets/AIB/Runtime/MotherController.cs`
  - **STRIP**: All `CleverCrow.Fluid.BTs` references, `NavMeshAgent`, `BehaviorTree _tree`, `Start()` BT builder, `IsInfantStressed()`, `DistanceToLavaEdge()`
  - **STRIP**: All autonomous movement logic (patrol, approach, comfort sequences)
  - **KEEP as reference**: `MotherStrength` calculation pattern (distance-based), `_arenaCenter` concept
  - **ADD**: Reference to `MotherPositionSideChannel` for position updates
  - **ADD**: Smooth position interpolation (`Vector3.Lerp` between current and target position)
  - **ADD**: Animator control: set `IsMoving` parameter based on position delta (moving vs stationary)
  - **ADD**: Visibility toggle: `gameObject.SetActive(motherActive)` — show/hide based on side channel `active` flag
  - **ADD**: Get mother prefab from `ModelSelector` (which reads `motherModel` from YAML config — could be `MotherVisualMesh` or `MotherAbbyVisualMesh`)
  - **ADD**: Fallback rendering: if no mother prefab available, instantiate a green sphere (`Color(0, 0.8, 0, 1)`) as placeholder
  - Guard with `#if EXPERIMENT_BUILD` for side-channel path. For observer build, receive position from `AbeStateBuffer` instead.

  **3c. Side Channel Registration** (modify `Assets/Scripts/AAI3EnvironmentManager.cs`):
  - In `InitialiseSideChannel()`, after existing `ArenasParametersSideChannel` registration:
    - Create `MotherPositionSideChannel` instance
    - Register via `SideChannelManager.RegisterSideChannel(_motherChannel)`
    - Wire to `MotherController` (find or create Mother GameObject)
  - In `OnDestroy()`: unregister the new channel
  - Guard the new registration with `#if EXPERIMENT_BUILD`

  **3d. Extend CSV logging** (modify `Assets/Scripts/CSVWriter.cs`):
  - Add 4 new parameters to `LogToCSV()`: `float motherX, float motherY, float motherZ, bool motherActive`
  - Add to log entry format string: `,...,{motherX},{motherY},{motherZ},{motherActive}`
  - Update header (line 196): append `,MotherX,MotherY,MotherZ,MotherActive`
  - When mother is off: pass `0f, 0f, 0f, false`
  - **MUST**: Update `Assets/Tests/EditMode/CSVWriterTests.cs` — update expected header string and all `LogToCSV()` call sites to include the 4 new parameters. Verify tests still compile.

  **3e. Extend caller** (modify `Assets/Scripts/TrainingAgent.cs`):
  - In both `LogToCSV()` call sites (lines ~248 and ~289): add mother position arguments
  - Get mother data from `MotherController` reference or from `MotherPositionSideChannel`
  - If no mother data available: pass zeros and `false`

  **3f. Extend observer state payload** (modify `Assets/AIB/Runtime/AbeStatePayload.cs`):
  - Add fields: `public float motherPosX`, `motherPosY`, `motherPosZ`, `public bool motherActive`
  - (Pre-existing `motherStrength` field already exists but is unused)

  **3g. Fix AbeStateBuffer** (modify `Assets/AIB/Runtime/AbeStateBuffer.cs`):
  - In `Write()` method: add copy lines for ALL new mother fields AND the pre-existing `motherStrength` (bug fix)
  - This ensures observer builds receive mother position via WebSocket

  **Must NOT do**:
  - Do NOT keep any BehaviorTree or NavMeshAgent code in MotherController
  - Do NOT remove `com.fluid.behavior-tree` from Packages/manifest.json
  - Do NOT restructure CSVWriter threading — only add parameters and columns
  - Do NOT add more than 4 CSV columns (MotherX, MotherY, MotherZ, MotherActive)
  - Do NOT wire Mother into AIBOneClickSetup

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
    - Reason: Multi-file coordinated changes across side channels, controllers, CSV, state payload, and tests
  - **Skills**: `[]`

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 3 (solo)
  - **Blocks**: Tasks 4, 5
  - **Blocked By**: Task 2 (needs Mother prefab)

  **References**:

  **Pattern References**:
  - `Assets/Scripts/ArenasParametersSideChannel.cs` — EXACT PATTERN to follow for new side channel. Study: class declaration, GUID constructor, `OnMessageReceived`, event emission. Mirror this structure.
  - `Assets/Scripts/AAI3EnvironmentManager.cs:190-216` — `InitialiseSideChannel()` shows registration lifecycle: try/catch unregister, create, subscribe, register. Mirror this for mother channel.
  - `Assets/AIB/Runtime/AbeStatePayload.cs` — State payload schema. Has existing `motherStrength` field at line ~57. Add mother position fields following same naming convention.
  - `Assets/AIB/Runtime/AbeStateBuffer.cs` — Double-buffer `Write()` method. Currently OMITS `motherStrength` copy (bug). Fix this AND add new mother position field copies.

  **Starting Point**:
  - `_pending_scripts/MotherNPC.cs` — Full source at 128 lines. KEEP: `MotherStrength` calculation (lines 99-100), `_arenaCenter` (line 26), `comfortRange`/`maxMotherRange` fields. STRIP: everything else (BT, NavMesh, autonomous movement).

  **Test References**:
  - `Assets/Tests/EditMode/CSVWriterTests.cs` — 5 tests that call `LogToCSV()`. ALL must be updated to include 4 new parameters. Expected header string at test assertions must include new columns.

  **Code References**:
  - `Assets/Scripts/TrainingAgent.cs:248,289` — Two `LogToCSV()` call sites that must be updated with mother position arguments.
  - `Assets/Scripts/CSVWriter.cs:51-65` — `LogToCSV()` method signature (add 4 params). Line 73-98: format string (add 4 fields). Line 196: header string (add 4 column names).
  - `Assets/AIB/Runtime/AbeStateBroadcaster.cs` — Broadcasts state. Check that it correctly serializes new payload fields.
  - `Assets/AIB/Runtime/AbeStateReceiver.cs` — Observer-side receiver. Check that it deserializes new fields.

  **WHY Each Reference Matters**:
  - `ArenasParametersSideChannel.cs`: Don't invent a new pattern. Copy this exact structure with different GUID and float payload instead of bytes.
  - `AAI3EnvironmentManager.cs`: Registration lifecycle is specific (try/catch, order of operations). Follow it exactly.
  - `AbeStateBuffer.Write()`: This is where the pre-existing bug lives. If you don't fix the copy logic, mother position will be received by the experiment build but never forwarded to observers.
  - `CSVWriterTests.cs`: If you change `LogToCSV()` signature without updating tests, the project won't compile in test mode.
  - `TrainingAgent.cs` call sites: These are the ONLY places `LogToCSV()` is called at runtime. Miss one → mother data missing from some log entries.

  **Acceptance Criteria**:

  **Agent-Executed QA Scenarios:**

  ```
  Scenario: Side channel class created with correct pattern
    Tool: Bash (grep)
    Steps:
      1. grep "SideChannel" Assets/AIB/Runtime/MotherPositionSideChannel.cs
      2. Assert: class inherits SideChannel
      3. grep "OnMessageReceived" Assets/AIB/Runtime/MotherPositionSideChannel.cs
      4. Assert: method exists
      5. grep "EXPERIMENT_BUILD" Assets/AIB/Runtime/MotherPositionSideChannel.cs
      6. Assert: #if guard present
    Expected Result: Side channel follows existing pattern with compile guard
    Evidence: grep output

  Scenario: Side channel registered in environment manager
    Tool: Bash (grep)
    Steps:
      1. grep "MotherPositionSideChannel" Assets/Scripts/AAI3EnvironmentManager.cs
      2. Assert: found in InitialiseSideChannel region
      3. grep "RegisterSideChannel.*mother" Assets/Scripts/AAI3EnvironmentManager.cs -i
      4. Assert: registration call found
    Expected Result: Mother channel registered alongside arena channel
    Evidence: grep output

  Scenario: MotherController has no BT/NavMesh references
    Tool: Bash (grep)
    Steps:
      1. grep "CleverCrow\|BehaviorTree\|NavMeshAgent\|NavMesh" Assets/AIB/Runtime/MotherController.cs
      2. Assert: ZERO matches
      3. grep "SideChannel\|MotherPosition\|Lerp\|SetActive" Assets/AIB/Runtime/MotherController.cs
      4. Assert: all found (side channel receiver, interpolation, visibility toggle)
    Expected Result: Clean controller with side-channel input, no autonomous AI
    Evidence: grep output

  Scenario: CSVWriter extended with mother columns
    Tool: Bash (grep)
    Steps:
      1. grep "MotherX,MotherY,MotherZ,MotherActive" Assets/Scripts/CSVWriter.cs
      2. Assert: found in header string
      3. grep "motherX\|motherY\|motherZ\|motherActive" Assets/Scripts/CSVWriter.cs
      4. Assert: found in LogToCSV parameters and format string
    Expected Result: CSV includes 4 new mother columns
    Evidence: grep output

  Scenario: CSVWriterTests updated and compilable
    Tool: Bash (grep)
    Steps:
      1. grep "MotherX" Assets/Tests/EditMode/CSVWriterTests.cs
      2. Assert: found (updated expected header)
      3. Count LogToCSV call sites in test file
      4. Assert: all include mother parameters
    Expected Result: Tests updated to match new signature
    Evidence: grep output

  Scenario: AbeStateBuffer copies all mother fields
    Tool: Bash (grep)
    Steps:
      1. grep "motherStrength\|motherPosX\|motherPosY\|motherPosZ\|motherActive" Assets/AIB/Runtime/AbeStateBuffer.cs
      2. Assert: all 5 fields appear in Write() method
      3. Count occurrences in Write() method specifically
      4. Assert: count >= 5 (one copy per field)
    Expected Result: No silent field drops in state buffer
    Evidence: grep output

  Scenario: No pending MotherNPC in _pending_scripts
    Tool: Bash (ls)
    Steps:
      1. ls _pending_scripts/MotherNPC.cs
      2. Assert: file still exists (not deleted, just copied and adapted)
      3. ls Assets/AIB/Runtime/MotherController.cs
      4. Assert: new file exists
    Expected Result: Original preserved, new adapted version in place
    Evidence: ls output
  ```

  **Commit**: YES
  - Message: `feat(mother): add Mother entity with side channel position sync + CSV logging`
  - Files: `Assets/AIB/Runtime/MotherPositionSideChannel.cs`, `Assets/AIB/Runtime/MotherController.cs`, `Assets/Scripts/AAI3EnvironmentManager.cs`, `Assets/Scripts/CSVWriter.cs`, `Assets/Scripts/TrainingAgent.cs`, `Assets/AIB/Runtime/AbeStatePayload.cs`, `Assets/AIB/Runtime/AbeStateBuffer.cs`, `Assets/Tests/EditMode/CSVWriterTests.cs`
  - Pre-commit: grep for all expected class names and field names

---

- [ ] 4. Replay controller (PRD Phase 4)

  **What to do**:
  1. **Create** `Assets/AIB/Runtime/ReplayController.cs`:
     - Reads `Observations_*.csv` files (existing CSVWriter output format, now with mother columns)
     - Parses CSV header to find column indices for: `XPosition`, `YPosition`, `ZPosition`, `MotherX`, `MotherY`, `MotherZ`, `MotherActive`
     - Frame-by-frame playback: one CSV row per physics tick
     - Smooth interpolation between logged positions using `Vector3.Lerp`
     - Drives agent position (moves `AAI3Agent` transform directly)
     - Drives mother position (moves Mother GameObject, toggles visibility via `MotherActive` column)
     - Uses observer cameras from Task 1 (NE overview) for recording viewpoint
  2. **Unity Recorder integration**:
     - Configure Unity Recorder to capture from the NE observer camera
     - Output: MP4 (H.264), 1920×1080, 30fps
     - Output path: `Builds/Replays/` directory
     - Recording starts automatically when replay begins, stops when CSV rows exhausted
  3. **CSV file selection**:
     - Accept CSV path via command-line argument: `--replayCSV /path/to/Observations_*.csv`
     - Parse via `AAI3EnvironmentManager`-style CLI arg reading pattern
  4. **Episode handling**:
     - CSV contains `Episode` column. Each episode starts with a new episode number.
     - On episode boundary: reset positions, brief pause (1 second), continue
     - Do NOT split into separate videos per episode — one continuous recording
  5. **Guard with `#if !EXPERIMENT_BUILD`** — replay only runs in observer/standalone builds, never in experiment builds.

  **Must NOT do**:
  - Do NOT add playback controls (pause, rewind, speed adjustment)
  - Do NOT add timeline UI or scrubbing
  - Do NOT validate CSV integrity beyond basic header check
  - Do NOT support multiple simultaneous replay sources
  - Do NOT restructure existing camera system for replay

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
    - Reason: CSV parsing, Unity Recorder API, camera integration, CLI args
  - **Skills**: `[]`

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 4 (solo)
  - **Blocks**: Task 5
  - **Blocked By**: Tasks 1 (observer camera), 3 (mother CSV columns)

  **References**:

  **Pattern References**:
  - `Assets/Scripts/AAI3EnvironmentManager.cs:80-120` — CLI argument parsing pattern. Shows how to read `--argName value` from `System.Environment.GetCommandLineArgs()`. Follow this for `--replayCSV`.
  - `Assets/Scripts/CSVWriter.cs:196` — CSV header string. The replay parser must match these column names exactly (including the 4 new mother columns from Task 3).

  **API References**:
  - `Assets/AIB/Runtime/OTSRecordCamera.cs` — Existing camera with `CaptureFrame()`. May provide a pattern for video frame capture, but Unity Recorder handles this natively.

  **Documentation References**:
  - Unity Recorder 5.1.5: `com.unity.recorder` in `Packages/manifest.json`. Use `RecorderController` API for programmatic recording.
  - PRD Phase 4 spec: `PRD_visual_pipeline.md:70-76` — Requirements for replay mode

  **External References**:
  - Unity Recorder scripting API: Set up `MovieRecorderSettings` with H.264 codec, 1920×1080, 30fps. Use `RecorderController.PrepareRecording()` + `StartRecording()` + `StopRecording()`.

  **WHY Each Reference Matters**:
  - `AAI3EnvironmentManager.cs` CLI parsing: Don't invent a new argument parsing approach. The project already has a pattern.
  - `CSVWriter.cs` header: The replay parser MUST match the exact column order. If Task 3 changes columns, Task 4 must match.
  - Unity Recorder: Already in the project. Use its API directly — no need for custom frame capture.

  **Acceptance Criteria**:

  **Agent-Executed QA Scenarios:**

  ```
  Scenario: ReplayController created with correct guards
    Tool: Bash (grep)
    Steps:
      1. ls Assets/AIB/Runtime/ReplayController.cs
      2. Assert: file exists
      3. grep "EXPERIMENT_BUILD" Assets/AIB/Runtime/ReplayController.cs
      4. Assert: #if !EXPERIMENT_BUILD guard present
      5. grep "replayCSV\|--replayCSV" Assets/AIB/Runtime/ReplayController.cs
      6. Assert: CLI arg parsing present
    Expected Result: Replay controller exists with correct compile guard and CLI arg support
    Evidence: grep output

  Scenario: ReplayController parses CSV header columns
    Tool: Bash (grep)
    Steps:
      1. grep "XPosition\|YPosition\|ZPosition" Assets/AIB/Runtime/ReplayController.cs
      2. Assert: position column names referenced
      3. grep "MotherX\|MotherY\|MotherZ\|MotherActive" Assets/AIB/Runtime/ReplayController.cs
      4. Assert: mother column names referenced
    Expected Result: Parser looks for correct column names
    Evidence: grep output

  Scenario: Unity Recorder configured for MP4 output
    Tool: Bash (grep)
    Steps:
      1. grep "MovieRecorderSettings\|RecorderController\|H264\|1920\|1080" Assets/AIB/Runtime/ReplayController.cs
      2. Assert: recorder configuration references found
      3. grep "Replays" Assets/AIB/Runtime/ReplayController.cs
      4. Assert: output directory referenced
    Expected Result: Recorder configured for 1920x1080 MP4 at 30fps
    Evidence: grep output
  ```

  **Commit**: YES
  - Message: `feat(replay): add CSV replay controller with Unity Recorder video output`
  - Files: `Assets/AIB/Runtime/ReplayController.cs`
  - Pre-commit: Verify file exists and contains required patterns

---

- [ ] 5. Final builds + integration verification

  **What to do**:
  1. **macOS ARM build** (local M4):
     - Run via Unity CLI batchmode: `Unity -batchmode -nographics -projectPath . -executeMethod AIB.Editor.AIBBuildConfig.BuildExperimentMac -quit`
     - Verify: `Builds/AIB_Experiment.app` produced, size > 100KB
     - Verify: zero build errors in Unity log
  2. **Linux x86_64 headless build** (for VM):
     - Run via Unity CLI batchmode: `Unity -batchmode -nographics -projectPath . -executeMethod AIB.Editor.AIBBuildConfig.BuildExperiment -quit`
     - Output: `Builds/AIB_Experiment_Linux/` directory
     - Verify: executable produced, size > 100KB
     - Verify: zero build errors in Unity log
  3. **Integration smoke check**:
     - Launch macOS build briefly to verify it starts without crash
     - Check Unity log output for: no missing script references, no SkinnedMeshRenderer errors, no missing prefab warnings
  4. **Update status.md**: Write summary of what changed, what was built, what's ready for deployment

  **Must NOT do**:
  - Do NOT deploy to VM — local verification only
  - Do NOT run full experiment — just verify build + launch
  - Do NOT modify any source code in this task

  **Recommended Agent Profile**:
  - **Category**: `quick`
    - Reason: Build commands + verification checks, no code changes
  - **Skills**: `[]`

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 5 (solo, final)
  - **Blocks**: None (final task)
  - **Blocked By**: Tasks 0, 1, 2, 3, 4 (all must be complete)

  **References**:

  **Pattern References**:
  - `Assets/AIB/Editor/AIBBuildConfig.cs` — Build methods: `BuildExperiment()` (Linux), `BuildExperimentMac()` (macOS). These set `EXPERIMENT_BUILD` scripting symbol and configure paths.
  - `.sisyphus/status.md` — Existing status report format. Follow this structure for the update.

  **WHY Each Reference Matters**:
  - `AIBBuildConfig.cs`: Contains the exact Unity CLI methods to invoke. Don't guess build commands.
  - `status.md`: Maintains continuity of project status documentation.

  **Acceptance Criteria**:

  **Agent-Executed QA Scenarios:**

  ```
  Scenario: macOS build succeeds
    Tool: Bash (Unity CLI)
    Steps:
      1. Run Unity build command for macOS
      2. Assert: exit code 0
      3. ls Builds/AIB_Experiment.app
      4. Assert: .app bundle exists
      5. grep "error\|Error\|ERROR" Unity build log (case-insensitive)
      6. Assert: zero error lines (warnings OK)
    Expected Result: Clean macOS build produced
    Evidence: Build log + ls output

  Scenario: Linux headless build succeeds
    Tool: Bash (Unity CLI)
    Steps:
      1. Run Unity build command for Linux
      2. Assert: exit code 0
      3. ls Builds/AIB_Experiment_Linux/
      4. Assert: directory exists with executable
      5. grep "error\|Error\|ERROR" Unity build log
      6. Assert: zero error lines
    Expected Result: Clean Linux build produced
    Evidence: Build log + ls output

  Scenario: macOS build launches without crash
    Tool: interactive_bash (tmux)
    Steps:
      1. Launch: open Builds/AIB_Experiment.app (or run binary directly)
      2. Wait 10 seconds
      3. Check process is still running (not crashed)
      4. Kill process
      5. Check Unity Player.log for errors
    Expected Result: Application starts and runs briefly without crash
    Evidence: Process status + Player.log contents

  Scenario: Status report updated
    Tool: Bash (grep)
    Steps:
      1. grep "Baby Abby\|MotherController\|ReplayController\|AIB-nursery" .sisyphus/status.md
      2. Assert: all mentioned in updated status
    Expected Result: Status reflects current state of project
    Evidence: grep output
  ```

  **Commit**: YES
  - Message: `chore: verify builds and update status after visual pipeline implementation`
  - Files: `.sisyphus/status.md`, any build artifacts in `Builds/`
  - Pre-commit: Both builds exist

---

## Commit Strategy

| After Task | Message | Files | Verification |
|------------|---------|-------|--------------|
| 0 | `chore: rename project from AIB-nursery to AIB-nursery` | .slnx, workflows, README, ProjectSettings, .sisyphus | grep for old name = 0 |
| 1 | `feat(camera): fix first-person eye height + add NE observer camera` | CameraController, prefabs/scenes | Camera position values correct |
| 2 | `feat(models): create all 4 character prefabs + YAML-driven model selection` | Prefabs, Animations, ModelSelector, ArenasParameters | All 4 prefabs + selector exist |
| 3 | `feat(mother): add Mother entity with side channel position sync + CSV logging` | Side channel, controller, CSV, state payload, tests | All components present, tests updated |
| 4 | `feat(replay): add CSV replay controller with Unity Recorder video output` | ReplayController.cs | File exists with correct patterns |
| 5 | `chore: verify builds and update status after visual pipeline implementation` | status.md, Builds/ | Both builds succeed |

---

## Handoff to AIB Agent

After this plan is executed, the AIB agent (at `/Users/kendrick/Documents/dev/AIB`) needs to:
1. Update all references from `AIB-nursery` to `AIB-nursery` (repo URLs, path references, documentation)
2. Add `agentModel` and `motherModel` fields to arena YAML configs:
   ```yaml
   agentModel: baby_abby    # or "abe"
   motherModel: mother_abby  # or "mother"
   ```
3. Implement Python-side mother position sending via ML-Agents side channel in `aib/arena_bridge.py`:
   ```python
   if self.mother_agent is not None:
       mother_pos = self.mother_agent.position
       active = 1.0
   else:
       mother_pos = [0.0, 0.0]
       active = 0.0
   self._mother_channel.send_float_list([mother_pos[0], 0.0, mother_pos[1], active])
   ```
4. Register the `MotherPositionSideChannel` with the same GUID used in Unity
5. Update `aib_observer_bridge.py` to include mother position in HTTP POST state payload
6. Update binary paths for new build locations

---

## Success Criteria

### Verification Commands
```bash
# Rename complete
gh repo view --json name -q '.name'  # Expected: AIB-nursery
grep -r "animal-ai-unity" --include="*.yml" --include="*.md" --include="*.slnx" --include="*.asset" . | grep -v ".git/"  # Expected: 0 matches

# All 4 model prefabs exist
ls Assets/AIB/Prefabs/AbeVisualMesh.prefab         # Expected: exists
ls Assets/AIB/Prefabs/BabyAbbyVisualMesh.prefab    # Expected: exists
ls Assets/AIB/Prefabs/MotherVisualMesh.prefab      # Expected: exists
ls Assets/AIB/Prefabs/MotherAbbyVisualMesh.prefab  # Expected: exists

# Model selector exists
ls Assets/AIB/Runtime/ModelSelector.cs  # Expected: exists
grep "agentModel" Assets/Scripts/ArenasParameters.cs  # Expected: match

# Scripts exist
ls Assets/AIB/Runtime/MotherPositionSideChannel.cs  # Expected: exists
ls Assets/AIB/Runtime/MotherController.cs           # Expected: exists
ls Assets/AIB/Runtime/ReplayController.cs           # Expected: exists

# CSV extended
grep "MotherX,MotherY,MotherZ,MotherActive" Assets/Scripts/CSVWriter.cs  # Expected: match

# Builds succeed
ls Builds/AIB_Experiment.app       # Expected: exists (macOS)
ls Builds/AIB_Experiment_Linux/    # Expected: exists (Linux)
```

### Final Checklist
- [ ] All "Must Have" items present
- [ ] All "Must NOT Have" guardrails respected
- [ ] Both builds (macOS + Linux) succeed with zero errors
- [ ] No remaining references to `AIB-nursery` in tracked files
- [ ] All 4 character prefabs created and selectable via YAML config
- [ ] Abe is backward-compatible default when no agentModel specified
- [ ] Mother prefab exists and controller receives side channel data
- [ ] CSV includes mother columns
- [ ] Observer state payload includes mother position
- [ ] Replay controller exists with CLI arg support
- [ ] Status.md updated with current state
