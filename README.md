# Timeskip

**Developed by Michal Leff-Cohen, Hila Buchbut, Shira Shalit, Eran Tzarum and Itamar Shpitzer**

📄 **[Download Project Book / Documentation](./TimeSkip_Book.pdf)**

[רכיבים רשמיים](https://github.com/gamedev-ariel/timeskip/wiki)

[קישור ל - itch](https://eran-david.itch.io/timeskip)

[קישור לטריילר](https://www.youtube.com/watch?v=pa8Azw4GSfM)

[תרשים זרימה מהבנות של ריפוי בעיסוק](https://github.com/gamedev-ariel/timeskip/blob/main/gameFlow.pdf)


## Timeskip: Game-based ADHD Prediction

Timeskip is a research game for PC that collects objective behavioral signals during short play sessions and uses them to predict the likelihood of ADHD in children (around ages 10–12). It is not a therapy or “focus booster.” The goal is to unobtrusively measure gameplay features, store them, and send them to a trained model that returns an ADHD prediction.

---

## Game flow (what the player does)
- Linear, single-player progression with a short, guided session:
  0. Start scene (instructions): on-screen guidance shows how to move.
     - Controls shown: press Space to jump; use Left/Right arrows to move.
     - Purpose: a quick warm-up to ensure the player understands the controls.
  1. Forest exploration: collect screws to fix the time machine while avoiding hazards.
  2. Dwarf working‑memory challenge (Memory Game): short recall trials (before the river).
  3. River crossing: navigate across the river while avoiding fish and staying in bounds; reach the other side to advance.
- Sessions are short. At the end of a session, the game summarizes the player’s behavior and triggers data upload/prediction.

---

## What we measure during play
The game logs fine‑grained events and aggregates them into a fixed, numeric feature vector the model expects (23 features). Examples:
- Task performance and safety
  - screws_spawned, screws_collected
  - forest_fish_collisions, forest_out_of_bounds, forest_approaches, forest_5_screws_collected
  - river_fish_collisions, river_out_of_bounds, river_win, river_approaches
- Timing and variability
  - target_rt_median, target_rt_cv (reaction‑time median and coefficient of variation for target actions)
  - dwarf_wm_rt_median, dwarf_wm_rt_cv (working‑memory response timing)
- Input dynamics
  - forest_keys, forest_key_gap_mean, forest_key_gap_cv
  - river_keys, river_key_gap_mean, river_key_gap_cv
- Working‑memory outcomes
  - dwarf_correct, dwarf_incorrect, dwarf_enters

These features are computed in a fixed order and sent exactly as a 23‑element vector to ensure compatibility with the trained classifier.

---

## Scenes and measurements (per minigame)

### Start scene — Controls tutorial
- Objective: teach the controls quickly (Space to jump; Left/Right arrows to move).
- Measurement: used for onboarding only; it does not contribute features to the 23‑value vector.
- Screenshot (to be added):
  ![Start scene](docs/images/start-scene.png)

### Forest — Exploration and collection
- Objective: collect screws to repair the time machine; avoid hazards.
- Controls: arrows to move; Space to jump.
- What we measure here (subset of the 23 features):
  - screws_spawned, screws_collected
  - forest_fish_collisions (hazard hits), forest_out_of_bounds, forest_approaches, forest_5_screws_collected
  - target_rt_median, target_rt_cv (reaction‑time to target actions like collecting screws)
  - forest_keys, forest_key_gap_mean, forest_key_gap_cv (input dynamics)
- Screenshot (to be added):
  ![Forest](docs/images/forest.png)

### Dwarf Memory Game — Working‑memory challenge
- Objective: remember a short sequence/pattern and respond correctly.
- Controls: standard input per on‑screen prompts.
- What we measure here (subset of the 23 features):
  - dwarf_enters (number of entries/rounds)
  - dwarf_correct, dwarf_incorrect (trial outcomes)
  - dwarf_wm_rt_median, dwarf_wm_rt_cv (response timing and variability)
- Screenshot (to be added):
  ![Memory game](docs/images/memory-game.png)

### River — Navigation under constraints
- Objective: cross the river while avoiding fish and staying within bounds; reach the far bank.
- Controls: arrows to steer; Space to jump if applicable.
- What we measure here (subset of the 23 features):
  - river_fish_collisions, river_out_of_bounds, river_win, river_approaches
  - river_keys, river_key_gap_mean, river_key_gap_cv (input dynamics)
- Screenshot (to be added):
  ![River](docs/images/river.png)

---

## Data pipeline (how prediction works)
1. Logging during play
   - The game records clicks, key presses, outcomes, minigame events, and scene transitions.
2. Session export and Supabase
   - On session end, a compact JSON is built with per‑scene summaries plus a human‑readable “summary line”.
   - A row is posted to Supabase (table `session_logs`) containing: `player_name`, `session_id`, and `session_json`.
   - If Supabase isn’t configured, the session is saved locally instead.
3. Feature vector → model API
   - The same session is converted to a 23‑value feature vector and sent to a configured HTTP endpoint (e.g., a Hugging Face Space `/predict`).
   - The endpoint returns an ADHD prediction (label/probability). The game can briefly show this on‑screen.

Configuration (in Unity Inspector):
- Supabase URL, anon key, and table name
- Model API URL (and optional bearer token when using a routed endpoint)
- Toggle to enable API calls and whether to show the on‑screen prediction overlay

---

## Purpose and scope
- Purpose: collect standardized gameplay features to predict ADHD likelihood using a trained model.
- Scope: this is a screening/research tool. It is not a medical diagnosis and not a focus/attention training app.

For a visual overview of the intended game flow, see the flowchart linked above.
