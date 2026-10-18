# TAKEOVER — jam playtest and flow fixes
Last updated: 2026-10-18 20:30 · branch `jam/s1-stabilize` @ `ca94726`

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

## Playtest results (2026-10-18 evening, WebGL, bots in tools/playtest)
Verified by playing:
- **Hub:** gate prompt; Enter enters; 3-jump skip; walls hold (house 50.4, river 122.2); return to the same spot.
- **Memory:** wrong answer drops a life and now shows "Not that one - try again!"; all 4 levels pass, then the kitchen.
- **Kitchen:** new hint on screen; walking down through the door returns to Start.
- **Ending:** spaceship shows a thank-you message, then the menu after about 6 s; STATISTICS shows the session.
- **Forest:** catching screws works (2 caught in one run); 90 s timeout ends the round.
- **River:** loses come easily; a pass was not reached by the bot (rocks have no tag or RockId).

Fixed this session (commits `32aa47b`..`ca94726`):
- gate walls 3 units thick: a frame stall tunneled through the old 0.4 wall;
- memory wrong-answer feedback (UI only);
- kitchen hint (`ScreenMessage` helper);
- ending: message on screen; waits for the upload at most 10 s, with the same 5 s timing before the save;
- CLOSE label on INFO and STATISTICS was invisible (two Graphics on one object);
- `DevInput`: vertical axis, Where finds untagged players and lists nearby objects.

## Where we stopped
Final WebGL and Windows builds were running. Check the exe launches, then the checklist rows 11-12.

## Next steps
1. Decide the open item below (forest and river top edge).
2. UI polish: gate prompt overlaps the player and is hard to read; forest player is tiny at the bottom; Fire Emblem tiles.
3. Clinician report, pitch, trailer, submission.

## Open decisions
- **Leaving through the top of the screen counts as a loss** (forest and river). In the forest, bouncing on a mushroom does it; in the river, Space has unlimited air jumps, so spamming it does it in ~7 s. Ignoring the top edge would be friendlier, but `out_of_bounds` counts are model features, so it changes the data. Needs Eran (and Itamar). Recommendation: leave as-is for the jam, mention it in the pitch as a known design point.
- **Standing still loses fast** (forest ~4 s, river ~2-3 s after the countdown): by design (auto-scroller), same data concern.
- Untracked Unity-generated files: ignore (unchanged recommendation).

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
