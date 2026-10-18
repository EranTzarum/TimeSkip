exec(open('lib.py').read())
click(0.54, 0.465); sleep(3.5)
walk_to(20, 1, 60); walk_to(33, 1, 60)
for _ in range(3): key('Space'); sleep(0.3)
walk_to(52, 1, 40)
for _ in range(3): key('Space'); sleep(0.3)
for tx in (80, 104, 124): walk_to(tx, 1, 60)
key('Enter'); sleep(3); key('Space'); sleep(5)
def river_round(mode):
    n0=mark(); t0=time.time(); tr=[]; target=None; lj=0
    while time.time()-t0<70:
        p=P()
        if p.get('x') is None or since(n0,'Stopped FishSpawner'): break
        x,y,cx=p['x'],p['y'],p['cx']
        rocks=sorted([o for o in p['near'] if o['t']=='K' and cx-12<o['x']<cx+12], key=lambda o:o['x'])
        bank=[o for o in p['near'] if o['t']=='R']
        grounded = len(tr)>1 and abs(tr[-1][2]-y)<0.03
        if mode=='fail': tx=x-5
        elif bank and bank[0]['x']<cx+10: tx=bank[0]['x']+1
        else:
            if grounded:
                nxt=[o for o in rocks if 1.0<o['x']-x<5.0]
                if nxt and time.time()-lj>0.4:
                    target=nxt[0]['x']; jump(); lj=time.time()
                else:
                    cur=[o for o in rocks if abs(o['x']-x)<1.5]
                    target=cur[0]['x'] if cur else x
            tx = (target - 0.3) if target is not None else x   # rocks drift left with the river
        d=0 if abs(tx-x)<0.25 else (1 if tx>x else -1); hold(d,0.12)
        tr.append((round(time.time()-t0,1),round(x,1),round(y,2)))
        time.sleep(0.04)
    return tr, since(n0,'GameController: Player|Stopped Background').replace('\n','')[:100]
for rnd,(hx,jt) in enumerate([(1,0.9),(0.5,0.6)]):
    n0=mark(); t0=time.time(); tr=[]; lj=0
    while time.time()-t0<70:
        p=P()
        if p.get('x') is None or since(n0,'Stopped FishSpawner'): break
        x,y,cx=p['x'],p['y'],p['cx']
        ks=[(o['x'],o['y']) for o in p['near'] if o['t'] in 'KR']
        d = 1 if x < cx+2 else (0 if x < cx+5 else -1)
        hold(d*hx,0.15)
        if time.time()-lj>jt: jump(); lj=time.time()
        tr.append((round(time.time()-t0,1),round(x,1),round(y,1),len(ks)))
        time.sleep(0.04)
    log('round',rnd,'lasted',tr[-1] if tr else None, tr[::25])
    shot(f'river{rnd}'); click(0.36,0.58); sleep(9)
