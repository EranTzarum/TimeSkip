# Screenshot tour for UI review: one shot per approved UI item.
exec(open('lib.py').read())
click(0.54, 0.465); sleep(3); shot('1_hub_banner')
walk_to(20, 1, 60); walk_to(33, 1, 60); shot('1_gate_prompt')
key('Enter'); sleep(3); shot('3_forest_start_card'); key('Space'); sleep(7); shot('5_you_marker')
t0 = time.time()
while time.time() - t0 < 100 and not since(0, 'Stopped FishSpawner'): sleep(1)
sleep(1.5); shot('4_end_card')
click(0.5, 0.60); sleep(4); shot('2_hub_progress')       # Back to the map
walk_to(52, 1, 40); key('Enter'); sleep(9); shot('memory_background')
for fx in (0.48, 0.60, 0.21, 0.21):
    sleep(1.2); click(fx, 0.63); sleep(7.5)
shot('kitchen_hint')
for _ in range(10):
    hold(0, 0.5, -1); sleep(0.55)
    if P().get('s') == 'Start': break
for tx in (80, 104, 121): walk_to(tx, 1, 60)
for _ in range(3): key('Space'); sleep(0.3)
sleep(1); walk_to(140, 1, 30); sleep(2); shot('6_ending_card')
t0 = time.time()
while time.time() - t0 < 25 and P().get('s') != 'main': sleep(0.5)
click(0.54, 0.63); sleep(2); shot('stats_close_label')
