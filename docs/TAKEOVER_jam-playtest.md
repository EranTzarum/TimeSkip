# TAKEOVER — jam playtest, UI polish and overnight finish
Last updated: 2026-10-18 23:05 · branch `jam/s1-stabilize` @ `e4199a7` (local only, no upstream)

## Goal
Sheba ARC Healthcare Game Jam: submit Mon 2026-10-19 16:00, demo 17:00.
By the time Eran wakes up, the game must be:
- built (WebGL and Windows);
- regression-tested;
- polished;
- working exactly as before in every level.

Eran then tests it himself, uploads to itch, and tests on itch with the ML model. Full plan: `C:\Users\tzaru\Documents\TimeSkip\JAM_PLAN.md`.

## Done (evidence)
- **S1 fixes, hub gates, double jump, INFO and STATISTICS:** commits up to `c188d5f`.
- **This session (28 commits, `32aa47b`..`e4199a7`):**
  - **Gate walls:** 3 units thick; a frame stall had tunneled through 0.4.
  - **Memory:** wrong-answer feedback.
  - **Kitchen:** hint.
  - **Ending:** a card, then the menu within 10 s; the timing before the save is unchanged.
  - **CLOSE label:** fixed on INFO and STATISTICS.
  - **Shared UI kit (`Assets/Scripts/ScreenMessage.cs`):** navy panel and gold accent, used for:
    - the instruction banner, with key names;
    - hub progress (top-right);
    - forest and river start cards;
    - the win/try-again card ("Play again / Back to the map / Main menu");
    - the ending card.
  - **YOU marker:** over the forest and river player.
  - **Sharp pixels:** `filterMode 0` on pixel-art texture metas (514 files; only that line changed).
  - **Copyright:**
    - no Fire Emblem sprites left in the build scenes (hub grass strip emptied, memory panel set to a flat color);
    - FE PNGs remain in the repo but are unreferenced by built scenes.
  - **`Assets/Scripts/Game1/HubVisuals.cs` (Start scene, visual only):**
    - day→night→day forest backdrop from `Assets/Resources/Sky/sky_day.jpg` and `sky_night.jpg` (Canva AI, forest interior), fading by player x: night 36→46, full until 96, day by 108;
    - hides the opaque `BG_3` sky layer when the backdrop loads;
    - hides the old static red arrows;
    - edge guide arrow to the next uncleared gate, then the spaceship;
    - drawing-only feet offset on the floor (y < -3; no collider change).
  - **Playtest bots in `tools/playtest`:**
    - `play.py` drives a visible Chromium window at 60 fps;
    - scenarios `full_flow.py`, `hub_shots.py`, `ui_shots.py`, `forest.py`, `river.py`;
    - see `tools/playtest/README.md`.
- **Last full regression (build before `e4199a7`):** `full_flow.py` passed:
  - gates (3-jump skip, walls at 50.37 and 122.2);
  - memory 4 levels, then the kitchen, then the door back to Start;
  - spaceship ending, then the menu in 7.2 s (HF model answered 200);
  - STATISTICS shows the session with a working CLOSE.
- **Hub screenshots:** edge arrow "Forest >>>" visible, red arrows gone, sharp pixels. The forest backdrop was NOT visible yet (`BG_3` covered it); `e4199a7` fixes that.

## Where we stopped
Background task `bqilv4sw1` was building WebGL with `e4199a7` and then running `hub_shots.py`. Output:
```
C:\Users\tzaru\AppData\Local\Temp\claude\C--Users-tzaru-Documents-TimeSkip\95827d21-870a-4be3-93e7-e4974d7a5f70\tasks\bqilv4sw1.output
```
Screenshots go to `%TEMP%\reg\hub`. If Unity is no longer running and that output ends with `Succeeded`, check the screenshots. Otherwise rebuild:
```bash
"/c/Program Files/Unity/Hub/Editor/6000.0.28f1/Editor/Unity.exe" -batchmode -quit -projectPath "$(pwd -W)" -executeMethod BuildScript.BuildWebGL -logFile "$(cygpath -w "$TMP")\\build-webgl.log"
sed -i 's/\.then((unityInstance) => {/.then((unityInstance) => { window.unityInstance = unityInstance;/' Builds/WebGL/index.html
```
Serve: `python -m http.server 8765 -d Builds/WebGL`, or reuse the browser-pane preview "webgl" (`C:\Users\tzaru\Documents\TimeSkip\.claude\launch.json`). Bots: `cd tools/playtest && py -3 play.py full_flow.py <shots_dir>`.

## Next steps (overnight, in order)
1. Check the hub screenshots: the forest backdrop shows behind the platforms (day at the start, night around the house, day near the river), and the platforms stay readable. If the backdrop is too busy behind the platforms, darken or desaturate it in code (sprite color, e.g. 0.85 gray), not by editing art.
2. Run `full_flow.py` and `hub_shots.py` on the final WebGL build. Every step must pass as in the last regression. Fix only regressions, then rebuild once.
3. Build Windows: `BuildScript.BuildWindows` gives `Builds/Windows/TimeSkip.exe`. Launch it for 15 s and check the log has 0 exceptions.
4. Prepare the itch upload (do NOT upload): zip `Builds/WebGL` contents (`index.html` at the zip root) as `C:\Users\tzaru\Documents\TimeSkip\upload\TimeSkip_WebGL_<date>.zip`, and zip the Windows build as `TimeSkip_Windows_<date>.zip`.
5. Write `docs/MORNING_CHECKLIST.md` for Eran:
   - local test steps;
   - itch upload steps (Edit game → Uploads → replace the HTML5 zip, "This file will be played in the browser", viewport 960×600);
   - on-itch test: play to the spaceship, check STATISTICS, and check the session in Supabase `jam_sessions` plus the HF model response in the browser console.
6. Update this doc and `docs/STATE.md`, commit docs. No push.

## Open decisions
- **Spaceship sprite** is a pngtree image (`Assets/Prefabs/intro/pngtree-alien-spaceships-...png`); its license is unclear. Options: keep it for the jam, or replace it with a Canva AI spaceship. Recommendation: replace if there's time after everything passes; ask Eran first.
- **Top-edge out-of-bounds loss** (forest and river): Eran decided to keep it (data stays comparable).
- **Feet offset** threshold `FloorY = -3.0`, drop 0.35, in `HubVisuals.cs`; tune only if the screenshots show sinking.

## Owner to-dos (Eran, morning)
- Test the build locally, upload to itch, and test on itch with the ML model (see `docs/MORNING_CHECKLIST.md` once written).
- Ask Itamar which pkl the Space should serve, and whether `game_dataset.csv` comes from real children.
- Ask the organizers whether building on a pre-existing project is allowed.
- Decide on push/merge of `jam/s1-stabilize` (local only).

## Rules that applied
- Never push, and never touch main, without Eran's OK. Commit small, on `jam/s1-stabilize`.
- The model and the 23-feature logs stay unchanged (no gameplay timing or physics changes without OK). UI and visual changes only.
- No real patient data. One Unity instance at a time. Never print secrets (the Supabase anon key is in `ExperimentLogger.cs`).
- Prefer one build per batch of changes; Eran dislikes many rebuilds.
- Canva: the Canva connection can export images by swapping the fill in design `DAHYYiyvzqY` and exporting PNG. Direct thumbnail URLs are signed and low-res.

## Not mine (left alone)
Unity-generated during builds, not staged:
- `Oswald Bold SDF.asset`
- `ProjectSettings/ProjectSettings.asset`
- `Assets/Resources/PerformanceTestRun*.json`
- `Assets/Unity.VisualScripting.Generated/`

`Assets/Resources/Sky.meta` and the sky `.jpg.meta` files are build-generated metas for this session's images; commit them with the next code commit.
