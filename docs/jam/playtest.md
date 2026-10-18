# S1 playtest checklist

Build: `Unity.exe -batchmode -quit -projectPath . -executeMethod BuildScript.BuildWindows -logFile build.log` → `Builds/Windows/TimeSkip.exe`.
Session files: `%USERPROFILE%\AppData\LocalLow\<company>\<product>\session_logs\*.json`

| # | Do | Expect |
|---|----|--------|
| 1 | Main menu | 3 buttons (START, JUMP INTO, QUIT); no INFO/STATISTICS |
| 2 | Start: hold/spam Space | Single jump only, no flying |
| 3 | Forest: collect 5 screws | "Well Done!" + buttons, even if no victory sound |
| 4 | Forest: wait out the timer | "Time's up! Good job!" + buttons (was a soft-lock) |
| 5 | Forest: hit a berry | "Try Again!" once; buttons work once each |
| 6 | River: wait out the timer | "Time's up!" panel |
| 7 | River: fall in water / hit fish | "Try Again!"; Restart, Main, Start all work |
| 8 | Memory: play | Options in authored order (unchanged by design); question text is a sentence |
| 9 | Memory: press Space | No "Use right arrow" text |
| 10 | Memory: lose 3 lives → "Dwarf" | Loads dwarf house |
| 11 | Spaceship | Quits after ~5 s; a new JSON exists in session_logs |
| 12 | Quit from main menu mid-session | A JSON exists for that session too |
| 13 | Anywhere | No "ADHD" text on screen; JSON `playerName` is a 6-char code |
