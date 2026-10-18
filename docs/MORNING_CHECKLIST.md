# Morning checklist (Mon 2026-10-19)

Builds are ready on branch `jam/s1-stabilize` (local only, nothing pushed). Upload files are in
`C:\Users\tzaru\Documents\TimeSkip\upload\`:
- `TimeSkip_WebGL_20261019.zip` (`index.html` at the zip root, for itch HTML5)
- `TimeSkip_Windows_20261019.zip` (a `TimeSkip` folder with `TimeSkip.exe`)

What the overnight session verified is in `docs/TAKEOVER_jam-playtest.md` ("Done").

## 1. Test locally (10 min)

1. In a terminal, from `C:\Users\tzaru\Documents\TimeSkip\code\timeskip`:
   ```bash
   python -m http.server 8765 -d Builds/WebGL
   ```
2. In Chrome open `http://localhost:8765`.
3. Play the whole path once:
   - **Hub:** forest backdrop, day at the start, night around the dwarf's house, day again near the river. The big "Forest >>>" box at the screen edge points to the next place.
   - **Forest:** catch a few screws. The end card shows "Play again / Back to the map / Main menu".
   - **Dwarf's house:** the memory game. A wrong answer flashes red. After 4 levels you're in the kitchen; walk down through the door.
   - **River:** Enter, or skip it with 3 jumps.
   - **Spaceship:** the "You fixed the time machine!" card, then the main menu within about 15 s.
   - **Main menu → STATISTICS:** your session shows; CLOSE works.
4. You should see no frozen screen, and nothing that blocks the path. If something breaks, send me a screenshot and the level name.

Windows: unzip `TimeSkip_Windows_20261019.zip` anywhere, open the `TimeSkip` folder, run `TimeSkip.exe`.

## 2. Upload to itch (5 min)

1. itch.io → Dashboard → Time Skip → **Edit game**.
2. **Uploads** → **Upload files** → `TimeSkip_WebGL_20261019.zip`.
3. On the new zip tick **This file will be played in the browser**; on the old HTML5 zip untick it (keep the old file, don't delete it).
4. **Embed options:** viewport 960 × 600, tick **Fullscreen button**.
5. Optional: upload `TimeSkip_Windows_20261019.zip` and tick **Windows**.
6. **Save**. The old zip stays on itch until you delete it, so you can switch back.

## 3. Test on itch with the ML model (10 min)

1. Open the game page on itch in Chrome, press **F12** → **Console**.
2. Play to the spaceship (the river can be skipped with 3 jumps).
3. In the console you should see, after the ending card:
   - `[ExperimentLogger] Sending 23 features to model API: https://ithamarspitz-adhd-noise-classifier.hf.space/predict`
   - `[ExperimentLogger] Model API response (200): {...}` — the model answered.
   - `[ExperimentLogger] Session successfully saved to Supabase.`
4. A first call can take up to a minute if the HF Space is asleep. The game goes back to the menu after 15 s anyway; the console line may appear a bit later.
5. Check the row: Supabase dashboard → project TimeSkip → **Table Editor** → `jam_sessions` → newest row has today's time.
6. Main menu → **STATISTICS** shows the session.

If the response is not 200 (for example 503): open the HF Space page in a tab to wake it, wait a minute, play again. The game itself works without the model.

## Still open (only you)
- Push/merge `jam/s1-stabilize`: nothing was pushed.
- Ask Itamar which pkl the Space serves, and whether `game_dataset.csv` came from real children.
- Ask the organizers whether building on a pre-existing project is allowed.
- Spaceship image is from pngtree (license unclear): keep for the jam, or ask me to swap it.
