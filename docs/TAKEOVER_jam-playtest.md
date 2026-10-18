# TAKEOVER — jam playtest and flow fixes
Last updated: 2026-10-18 21:30 · branch `jam/s1-stabilize` @ `e4b3db0`

## Goal
Sheba ARC Healthcare Game Jam (submit Mon 2026-10-19 16:00, demo 17:00). Make Time Skip demo-ready: fix bugs, edge cases and the game flow first; UI polish, clinician report and pitch come after. The full plan and timeline are in `C:\Users\tzaru\Documents\TimeSkip\JAM_PLAN.md`.

## Done (evidence)
- Windows build and WebGL build both compile: `[BuildScript] ... Succeeded, 0 errors`.
- S1 fixes (commits `baabee8`..`fe124ef`):
  - timeout panel instead of a soft-lock;
  - forest win no longer depends on a sound;
  - pseudonymous 6-char session code;
  - local session JSON always written;
  - no child-facing prediction;
  - Supabase writes to the `jam_sessions` table.
- Team rule (`105daee`): the model and the 23-feature logs are unchanged; jam changes are UI and stability only.
- Hub, menu and test hook (`4bd259d`):
  - 5 working menu buttons (INFO and STATISTICS are runtime overlays);
  - double jump;
  - gated hub entrances: the prompt shows once and the voice repeats every 8 s; an invisible wall blocks until Enter or a 3-jump skip, with an on-screen hint; a tall gate zone covers every height;
  - `DevInput` test hook.
- Verified by playing the WebGL build in the browser pane:
  - menu, INFO and STATISTICS;
  - walking START to the forest gate with the double jump;
  - the gate prompt and its wall;
  - entering the forest with Enter, and "go to main game" returning to the same spot with the gate cleared;
  - the house 3-jump skip.

## Where we stopped
`e4b3db0` is committed but **not verified**:
- the end-panel message as a separate centered label (the old MessageText parents two of the buttons);
- `DevInput.Jump`.

The WebGL rebuild was stopped before it finished. To rebuild, from the repo root:
```bash
"/c/Program Files/Unity/Hub/Editor/6000.0.28f1/Editor/Unity.exe" -batchmode -quit -projectPath "$(pwd -W)" -executeMethod BuildScript.BuildWebGL -logFile "$(cygpath -w "$TMP")\\build-webgl.log"
```
Then patch the build so the page can call the game:
```bash
sed -i 's/\.then((unityInstance) => {/.then((unityInstance) => { window.unityInstance = unityInstance;/' Builds/WebGL/index.html
```
Serve it with the browser pane `preview_start` name `webgl` (config in `C:\Users\tzaru\Documents\TimeSkip\.claude\launch.json`, port 8765).

## Next steps
1. Rebuild WebGL (~10 min, Gzip) and serve it.
2. Play like a real player, without a screenshot for every move.
   - Drive input from page JS: `unityInstance.SendMessage('DevInput','Hold','1,2')` (direction, seconds), `'Jump'`, and `'Where'` (logs the player position). Capture console logs in JS.
   - Use real key taps (`computer key`) for Space and Enter.
   - Screenshot only to confirm a result.
3. Edge cases:
   - **Forest:** collect all 5 screws, hit a berry, time out. It's an auto-scroller, so standing still drifts off-screen.
   - **River:** fail it and pass it. The entrance trigger is around x 112–122.
   - **Memory game:** pass all 4 levels. The correct option index is Q1: 2/3/4, Q2: 3, Q3: 0, Q4: 0. Also lose 3 lives.
   - **Kitchen:** after a memory win the game loads `kitchen`, which needs vertical movement (DevInput has none yet). Check whether a child gets lost there.
   - **Ending:** reach the spaceship at x=125 and confirm the session appears in STATISTICS.
4. Fix what breaks, commit small, then rebuild the Windows exe.

## Open decisions
- Kitchen after a memory win: keep the detour or return to Start? Returning changes the scene-visit logs, which goes against the team rule, so ask Eran first. Recommendation: return to Start only if a child gets lost.
- Untracked Unity-generated files (see below): commit or ignore? Recommendation: ignore. Itamar deliberately removed the AOT stubs.

## Owner to-dos
- Ask Itamar which pkl the Space should serve (`noise_model.pkl` vs `adhd_model.pkl`), and whether `game_dataset.csv` comes from real children.
- Ask the organizers on Discord whether building on a pre-existing project is allowed.
- Optional: watch the browser pane live while the playtest runs.

## Rules that applied
- Never push without Eran's OK; never push to main. Commit small, on the jam branch.
- No real patient data; the 50 old `session_logs` rows in Supabase are never read.
- The model and the 23 features are unchanged; any change to the logs needs Eran's explicit OK.
- Only one Unity instance at a time on this project.
- Never print secrets. The Supabase anon key is in `ExperimentLogger.cs`; the HF token goes in an env var.

## Not mine (left alone)
Unity-generated during builds, not staged:
- `Assets/TextMesh Pro/.../Oswald Bold SDF.asset`
- `ProjectSettings/ProjectSettings.asset` (WebGL compression settings written by BuildScript)
- `Assets/Resources/PerformanceTestRun*.json`
- `Assets/Unity.VisualScripting.Generated/`
