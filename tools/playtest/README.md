# Playtest bots (WebGL build)

Drives the WebGL build in a visible Chromium window (60 fps; headless runs at ~12 fps and is too slow).

1. Build WebGL, then expose the instance: `sed -i 's/\.then((unityInstance) => {/.then((unityInstance) => { window.unityInstance = unityInstance;/' Builds/WebGL/index.html`
2. Serve it: `python -m http.server 8765 -d Builds/WebGL`
3. From this folder: `py -3 play.py full_flow.py shots` (needs `pip install playwright`).

Scenarios get `js`, `key`, `click(fx, fy)` (canvas fractions), `shot(name)`, `log`, `sleep`, and the helpers in `lib.py`.
Movement goes through the in-game `DevInput` hook (`Hold`, `Jump`, `Where`), because synthetic key taps are too short to count as held.

- `full_flow.py`: menu, gates (Enter and 3-jump skip), memory game (one wrong answer, then all 4 levels), kitchen exit, spaceship ending, STATISTICS.
- `forest.py`: catches falling screws until win, loss or timeout.
- `river.py`: hold-right-and-jump player; two styles.
