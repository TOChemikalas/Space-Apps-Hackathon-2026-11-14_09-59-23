using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class InteriorWallArtwork : MonoBehaviour
{
    [Tooltip("Wall vertices in normalized artwork coordinates, measured from the top-left.")]
    [SerializeField] Vector2[] wallVertices;
    [SerializeField] int[] triangles;
    Sprite wallSprite;

    void Awake()
    {
        var floor = GetComponent<SpriteRenderer>();
        Sprite source = floor.sprite;
        Rect rect = source.rect;
        var vertices = new Vector2[wallVertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = new Vector2(wallVertices[i].x * rect.width, (1f - wallVertices[i].y) * rect.height);

        wallSprite = Sprite.Create(source.texture, rect, source.pivot / rect.size, source.pixelsPerUnit);
        wallSprite.OverrideGeometry(vertices, System.Array.ConvertAll(triangles, index => (ushort)index));
        var walls = new GameObject("Foreground walls").AddComponent<SpriteRenderer>();
        walls.transform.SetParent(transform, false);
        walls.sprite = wallSprite;
        walls.color = floor.color;
        walls.sharedMaterial = floor.sharedMaterial;
        walls.sortingLayerName = "Buildings";
    }

    void OnDestroy()
    {
        if (wallSprite) Destroy(wallSprite);
    }
}

