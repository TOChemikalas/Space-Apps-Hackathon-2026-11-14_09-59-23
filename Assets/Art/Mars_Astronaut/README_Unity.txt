MARS ASTRONAUT — UNITY 2D TOP-DOWN SPRITES

Contents:
- astronaut_walk_4dir_atlas_256.png : 4x4 animation atlas (transparent), 256px cell size
- astronaut_north_00..03.png etc : separate walk animation frames (transparent)
- astronaut_idle_north/east/south/west.png : idle facing sprites
- preview_walk_*.gif : looping sample animations, not for Unity import

UNITY IMPORT:
1) Drag the 4x4 atlas into Assets/Sprites/Astronaut.
2) Inspector: Texture Type = Sprite (2D and UI), Sprite Mode = Multiple, Mesh Type = Full Rect.
3) Sprite Editor > Slice > Grid By Cell Size > 256x256 > Slice > Apply.
4) Rows are North, East, South, West (top to bottom); frame numbers 0,1,2,3 left-to-right.
5) Use ~6 to 8 FPS per directional looping Walk clip. Idle uses frame 0 in each row.
6) For low-resolution pixelated display use Filter Mode = Point; otherwise Bilinear is fine.
7) Set PPU for desired world scale (e.g. 256 PPU = 1 Unity unit tall).
8) SpriteRenderer Sort Point = Pivot and Sprite Pivot = Bottom center (to sort by feet).
9) Animator parameters: MoveX (float), MoveY (float), IsMoving (bool); use directional clips or a 2D blend tree.

SOURCE LIMITATIONS:
Pinterest short link was inaccessible, so the character is styled to match the other off-white/orange Mars buildings rather than claiming exact correspondence.
Animation is an artistic four-frame loop per direction; visually check its cadence inside Unity.
