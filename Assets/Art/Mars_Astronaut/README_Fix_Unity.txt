IMPROVED ASTRONAUT WALK CYCLE -- REPLACEMENT SPRITES

WHAT CHANGED
The walk sequence is neutral -> stride A -> neutral -> stride B.
The north and south stride-B frames are mirrored from stride-A, instead of repeating the same stepping leg.
East and west use the existing alternate stride-B poses.
All canvases remain 256x256 px with matching baselines, no positional movement baked into the animation.

HOW TO INSTALL
1. Stop Play Mode in Unity.
2. Extract Mars_Astronaut folder over Assets/Art/Mars_Astronaut and overwrite the PNG files.
3. Do not delete the .meta files. Unity should reimport the PNG content without losing references.
4. Existing Unity animation clips should update if they reference these individual PNG assets.
5. Walk clips: four frames at 6-8 Samples/sec, Loop Time enabled.
6. Idle clips: use a single astronaut_idle_DIRECTION.png sprite keyframe.
7. Animator: Idle and Walk states; IsMoving boolean conditions; no Has Exit Time; Transition Duration 0.
8. If character world position moves or teleports, inspect each .anim clip's property list and delete any Transform Position tracks. Walk clips should animate only Visual > Sprite Renderer > Sprite.
9. Check that only one script controls Astronaut movement, and the Animator is on its Visual child.

Notes: This correction uses mirroring for the second north/south step. Tiny asymmetric suit details may mirror on that single frame.
