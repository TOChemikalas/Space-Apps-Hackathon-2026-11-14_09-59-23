MARS COLONY - TOP DOWN 2D GAME SPRITES

CONTENTS
- 6 unique assets: solar station, rover, life support, greenhouse, habitat, shuttle.
- 2 lighting states for each: day, night (12 PNGs).
- 8 compass-facing rover sprites for each lighting state (16 directional PNGs).
- 2 transparent packed atlases, one for assets and one for rover directions.

All images are PNG with alpha transparency. Original designs are derived from your six supplied reference images and reillustrated as true orthographic overhead art.

UNITY IMPORT
1. Extract into Assets/Art/MarsColony/.
2. For individual PNGs, set Texture Type to Sprite (2D and UI), Sprite Mode Single, Filter Mode Bilinear and Compression High Quality or None.
3. For an atlas: Sprite Mode Multiple, open Sprite Editor -> Slice -> Grid by Cell Size 448 x 448, pivot Center.
4. Atlas order: buildings = day row first then night row; columns: solar_station, rover, life_support, greenhouse, habitat, shuttle.
5. Rover atlas order: day row then night row; columns N, NE, E, SE, S, SW, W, NW.
6. Switch sprite for direction and day/night; do not also rotate direction sprites in Unity.
7. For fixed buildings, keep their pivot as Center, or set a custom pivot at entrance foot if placement requires it.

Notes: Generated art may not match supplied references pixel-for-pixel. For night state, emissive lighting is baked into each sprite; no Unity lighting setup is required to see windows/beacons glow. A separate global 2D darkness overlay can reinforce nighttime.
