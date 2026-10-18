# TAKEOVER — jam playtest, UI polish and overnight finish
Last updated: 2026-10-19 00:25 · branch `jam/s1-stabilize` @ `8d61141` + docs commits (local only, no upstream)

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

- **Overnight (2026-10-19 00:25):**
  - `73ad1ea`: night tint on the opaque `BG_1`/`BG_2` layers, so day→night→day shows around the house.
  - `8d61141`: per Eran, the flat cyan sky band on top of the TobaccoLand trees (`Forest Parralax layer4`) is hidden in the hub except near the river (x ≥ 100). The River scene is untouched.
  - **Final WebGL build `8d61141`:** `full_flow.py` passed:
    - house wall 50.37;
    - memory, then the kitchen, then the door back to Start;
    - ending, then the menu in 7.2 s (HF model 200, probabilities returned);
    - STATISTICS shows the session.
    - Screenshots: `%TEMP%\reg\final3`.
  - **`hub_shots.py` passed:** forest day at the start, night at the house, day trees at the river, no cyan band. Screenshots: `%TEMP%\reg\hub3`.
  - **Windows build:** `Builds/Windows/TimeSkip.exe`. It ran 15 s and closed cleanly, with 0 exceptions in `Player.log`.
  - **Upload zips:** in `C:\Users\tzaru\Documents\TimeSkip\upload\`. Not uploaded.
    - `TimeSkip_WebGL_20261019.zip`: `index.html` at the root; decompression fallback is on, so no server headers are needed.
    - `TimeSkip_Windows_20261019.zip`.
  - `docs/MORNING_CHECKLIST.md` written.
  - Bot note: one earlier `full_flow` run read the scene before the kitchen had finished loading; the bot now waits for the kitchen player.

## Where we stopped
Everything on the overnight list is done. Next is Eran's morning check, following `docs/MORNING_CHECKLIST.md`.

## Next steps
1. Eran tests locally, uploads `TimeSkip_WebGL_20261019.zip` to itch, and tests on itch with the model (see the checklist).
2. If he reports a bug: fix it, rebuild WebGL, rerun `full_flow.py` and `hub_shots.py`, then re-zip into `upload/`.

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
