using UnityEngine;

[CreateAssetMenu(menuName = "Mars/Crop Definition")]
public sealed class CropDefinition : ScriptableObject
{
    [SerializeField] string cropId;
    [SerializeField] string displayName;
    [SerializeField, Min(.001f)] double earthDays = 28;
    [SerializeField] Sprite[] growthSprites = new Sprite[6];
    [SerializeField, Min(1)] int harvestQuantity = 1;

    public string Id => cropId;
    public string DisplayName => displayName;
    public double EarthDays => earthDays;
    public double DurationSeconds => earthDays * 86400;
    public int HarvestQuantity => harvestQuantity;
    public Sprite SpriteAt(int stage) => growthSprites[Mathf.Clamp(stage, 0, 5)];
    public bool IsValid => !string.IsNullOrWhiteSpace(cropId) && DurationSeconds > 0
        && !double.IsInfinity(DurationSeconds) && harvestQuantity > 0
        && growthSprites != null && growthSprites.Length == 6 && System.Array.TrueForAll(growthSprites, sprite => sprite);
}
