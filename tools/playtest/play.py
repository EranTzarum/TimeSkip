# Headless playtest driver for the TimeSkip WebGL build.
# Usage: py -3 play.py <scenario.py> [shots_dir]
# The scenario is Python with pg, js(expr), key, click(fx,fy), shot(name), log, sleep in scope.
import sys, pathlib
from playwright.sync_api import sync_playwright

HELPERS = r"""
window.L=[];['log','warn','error'].forEach(k=>{const o=console[k];console[k]=(...a)=>{L.push(k[0]+':'+a.join(' '));o.apply(console,a)}});
"""
API = r"""
window.S=(m,a)=>unityInstance.SendMessage(...(a===undefined?['DevInput',m]:['DevInput',m,a]));
window.sl=ms=>new Promise(r=>setTimeout(r,ms));
window.pos=async()=>{S('Where');await sl(60);const m=[...L].reverse().find(x=>x.includes('[DevInput]'))||'';const r=m.match(/scene=(\S+) player=(?:\(([-\d.]+), ([-\d.]+)[^)]*\)|none) cam=(?:\(([-\d.]+), ([-\d.]+)[^)]*\)|none) visible=(\w+).*near=(\S*)/);return r?{s:r[1],x:r[2]===undefined?undefined:+r[2],y:+r[3],cx:+r[4],cy:+r[5],v:r[6],near:(r[7]||'').split(';').filter(Boolean).map(t=>{const q=t.split(':');return {t:q[0],x:+q[1],y:+q[2]}})}:{raw:m}};
window.walkTo=async(tx,dir=1,maxs=40)=>{let last=-1e9,stuck=0;const t0=Date.now();while(Date.now()-t0<maxs*1000){S('Hold',dir+',0.6');await sl(450);const p=await pos();if(p.x===undefined)return p;if((dir>0&&p.x>=tx)||(dir<0&&p.x<=tx)){S('Hold','0,0.1');return p}if(Math.abs(p.x-last)<0.3){stuck++;S('Hold',(-dir)+','+(0.3+0.3*(stuck%3)));await sl(350+300*(stuck%3));S('Hold',dir+',1.2');await sl(80);S('Jump');await sl(320);S('Jump');await sl(700);}else stuck=0;last=p.x;}return await pos()};
window.logs=(s)=>L.filter(x=>new RegExp(s).test(x)&&!/Blueberry is not|Water is not|sound which/.test(x));
"""

def main():
    scen = pathlib.Path(sys.argv[1]).read_text(encoding="utf-8")
    shots = pathlib.Path(sys.argv[2]) if len(sys.argv) > 2 else None
    with sync_playwright() as p:
        b = p.chromium.launch(headless=False, args=["--use-angle=d3d11", "--autoplay-policy=no-user-gesture-required", "--disable-backgrounding-occluded-windows", "--disable-renderer-backgrounding", "--disable-background-timer-throttling"])
        pg = b.new_page(viewport={"width": 960, "height": 600})
        pg.add_init_script(HELPERS)
        pg.goto("http://localhost:8765/")
        pg.wait_for_function("window.unityInstance", timeout=120000)
        pg.evaluate(API)
        pg.wait_for_timeout(2000)
        import time
        n = {"i": 0}
        def shot(name):
            if shots:
                shots.mkdir(parents=True, exist_ok=True)
                bb = pg.locator("#unity-canvas").bounding_box()
                pg.screenshot(path=str(shots / f"{n['i']:02d}_{name}.png"), clip=bb, animations="allow", timeout=15000)
                n["i"] += 1
        def key(k):
            pg.keyboard.press(k)
        def click(fx, fy):  # fractions of the canvas
            bb = pg.locator("#unity-canvas").bounding_box()
            pg.mouse.click(bb["x"] + fx * bb["width"], bb["y"] + fy * bb["height"])
        def js(expr):
            return pg.evaluate("async()=>{" + expr + "}")
        def log(*a):
            print(*a, flush=True)
        exec(scen, {"pg": pg, "js": js, "key": key, "click": click, "shot": shot, "log": log, "sleep": time.sleep, "json": __import__("json")})
        b.close()

main()
