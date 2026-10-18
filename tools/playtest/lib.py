# Shared scenario helpers (exec'd before each scenario)
import time, json
def P():
    return json.loads(js("return JSON.stringify(await pos())"))
def hold(x, sec, y=None):
    js(f"S('Hold','{x},{y},{sec}')" if y is not None else f"S('Hold','{x},{sec}')")
def jump():
    js("S('Jump')")
def walk_to(tx, d=1, maxs=60):
    return json.loads(js(f"return JSON.stringify(await walkTo({tx},{d},{maxs}))"))
def mark():
    return js("return L.length")
def since(n, pat):
    return js(f"return L.slice({n}).filter(x=>new RegExp({json.dumps(pat)}).test(x)).join(' || ')")
def runner(kind, maxs=100):
    """Bot for forest/river: stay on screen, go for screws/rocks, hop over fish. Returns end log."""
    n0 = mark(); t0 = time.time(); last_jump = 0
    while time.time() - t0 < maxs:
        p = P()
        if p.get('x') is None:
            break
        if since(n0, 'Stopped FishSpawner'):
            break
        x, y, cx = p['x'], p['y'], p['cx']
        near = p.get('near', [])
        fish = [o for o in near if o['t'] == 'F' and -0.5 < o['x'] - x < 2.2 and abs(o['y'] - y) < 1.5]
        want = None
        if kind == 'forest':
            # player-like: run to the mushroom under the nearest screw and bounce up; hop berries
            sc = [o for o in near if o['t'] == 'S' and cx - 10 < o['x'] < cx + 10]
            mush = [o for o in near if o['t'] == 'M' and cx - 9 < o['x'] < cx + 9]
            if sc:
                target = min(sc, key=lambda o: abs(o['x'] - x))
                want = min(mush, key=lambda o: abs(o['x'] - target['x'])) if (mush and target['y'] > y + 3) else target
                if abs(target['x'] - x) < 1.2 and target['y'] > y + 0.5:
                    want = target
        else:
            bank = [o for o in near if o['t'] == 'R']
            rocks = [o for o in near if o['t'] == 'M' and o['x'] > x - 1.5 and o['x'] < cx + 8]
            if bank and bank[0]['x'] < cx + 9:
                want = bank[0]
            elif rocks:
                want = min(rocks, key=lambda o: abs(o['x'] - (x + 1)))
        tx = want['x'] if want else cx - 2
        tx = max(cx - 7, min(cx + 5, tx))      # never chase off-screen
        d = 0 if abs(tx - x) < 0.4 else (1 if tx > x else -1)
        hold(d, 0.25)
        now = time.time()
        if kind == 'forest' and now - last_jump > 0.45 and (fish or (want and want['t'] == 'S' and want['y'] > y + 0.5 and abs(want['x'] - x) < 1.5)):
            jump(); last_jump = now
        time.sleep(0.08)
    return since(n0, 'GameController|DevInput.*none')
