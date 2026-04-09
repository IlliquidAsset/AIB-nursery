# Build Mother Abby MVP

## Goal
Support the AIB Mother Abby MVP loop with the minimum Unity-side changes only.

## Do First
1. Keep changes laser-focused on enabling a testable mother-present arena, not a full persona stack.
2. Do not redesign the whole nursery.
3. Do not block on visual polish.

## Required Changes
- Maintain a buildable experiment binary for local Mac replay/screencap.
- Keep OTS camera support available for local replay clips.
- Keep MotherNPC.cs and related assets isolated until explicitly requested for integration into the MVP path.
- If asked to integrate MotherNPC, do it as a separate build branch, not mixed into the stable replay binary.

## Build Rules
- Local Mac: build and validate the macOS experiment binary first.
- VM: do NOT assume the new Unity binary is stable; wait for deployment instruction before attempting VM-targeted fixes.
- If a Unity-side change affects VM stability, stop and report rather than continuing to pile on changes.

## Smoke Test Expectations
- Local binary launches and reaches arena reset successfully.
- OTS camera component exists and can be used for replay/screencap path.
- No blocking compile errors in the Unity project.

## Report Back
Write short status notes to `AIB-nursery/.sisyphus/status.md` with:
- what changed
- what build was produced
- whether local replay works
- whether VM deployment is safe or risky
