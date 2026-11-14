using System.Collections;
using UnityEngine;

// Sprite-only animation; transforms, pivots, and surrounding artwork are never changed.
public sealed class AirlockDoor : MonoBehaviour
{
    [SerializeField] SpriteRenderer doorRenderer;
    [SerializeField] Sprite[] openingFrames = new Sprite[4];
    [SerializeField] Collider2D[] barriers;
    [SerializeField] SpriteRenderer[] linkedRenderers = System.Array.Empty<SpriteRenderer>();
    [SerializeField, Min(.01f)] float animationSeconds = .8f;
    public bool IsOpen { get; private set; }
    public bool IsAnimating { get; private set; }
    public bool IsConfigured => doorRenderer && openingFrames != null && openingFrames.Length == 4
        && System.Array.TrueForAll(openingFrames, frame => frame) && barriers != null && barriers.Length > 0
        && System.Array.TrueForAll(barriers, barrier => barrier);

    public void CloseImmediately()
    {
        IsOpen = false;
        IsAnimating = false;
        SetBlocked(true);
        if (IsConfigured) ShowFrame(0);
    }

    public IEnumerator Animate(bool open)
    {
        if (!IsConfigured || IsAnimating || IsOpen == open) yield break;
        IsAnimating = true;
        // Closing becomes solid immediately. Opening stays solid until the final frame completes.
        SetBlocked(true);
        var frameDelay = new WaitForSecondsRealtime(Mathf.Max(.01f, animationSeconds) / 4);
        for (int i = 0; i < 4; i++)
        {
            ShowFrame(open ? i : 3 - i);
            yield return frameDelay;
        }
        IsOpen = open;
        IsAnimating = false;
        SetBlocked(!open);
    }

    void SetBlocked(bool blocked)
    {
        foreach (var barrier in barriers) if (barrier) barrier.enabled = blocked;
    }

    void ShowFrame(int index)
    {
        doorRenderer.sprite = openingFrames[index];
        foreach (var renderer in linkedRenderers) if (renderer) renderer.sprite = openingFrames[index];
    }
}
