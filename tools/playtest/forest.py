exec(open('lib.py').read())
click(0.54, 0.465); sleep(3.5)
walk_to(20, 1, 60); walk_to(33, 1, 60); key('Enter'); sleep(3); key('Space'); sleep(4.8)
n0=mark(); t0=time.time(); last=None
while time.time()-t0<95:
    p=P()
    if p.get('x') is None: log('dead', round(time.time()-t0,1), 'last', last); break
    x,y,cx=p['x'],p['y'],p['cx']; last=(round(x,1),round(y,1))
    sc=[o for o in p['near'] if o['t']=='S' and o['y']<9 and cx-11<o['x']<cx+11]
    tx = min(sc,key=lambda o:o['y'])['x']-0.6 if sc else cx-1   # lowest falling screw, lead for drift
    tx=max(cx-8,min(cx+8,tx))
    d=0 if abs(tx-x)<0.3 else (1 if tx>x else -1); hold(d,0.15)
    if time.time()-t0<3 or int((time.time()-t0)*10)%50==0: log(round(time.time()-t0,1),x,y,cx,tx,d)
    time.sleep(0.05)
log('end', since(n0,'GameController|Time').replace('\n','')[:200]); shot('forest_win')
