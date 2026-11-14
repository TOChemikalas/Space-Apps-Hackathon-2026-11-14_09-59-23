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
    Collider2D exteriorBarrier;
    SpriteRenderer exteriorRenderer;

    public void BindExterior(Collider2D barrier, SpriteRenderer renderer)
    {
        exteriorBarrier = barrier;
        exteriorRenderer = renderer;
    }

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

    public IEnumerator Animate(bool open, bool allowImmediatePassage = false, System.Func<bool> interrupt = null)
    {
        if (!IsConfigured || IsAnimating || IsOpen == open) yield break;
        IsAnimating = true;
        // Interior doors remain passable during their short animation.
        SetBlocked(!allowImmediatePassage);
        var frameDelay = new WaitForSecondsRealtime(Mathf.Max(.01f, animationSeconds) / 4);
        for (int i = 0; i < 4; i++)
        {
            ShowFrame(open ? i : 3 - i);
            yield return frameDelay;
            if (interrupt != null && interrupt())
            {
                ShowFrame(3);
                IsAnimating = false;
                yield break;
            }
        }
        IsOpen = open;
        IsAnimating = false;
        SetBlocked(!open);
    }

    void SetBlocked(bool blocked)
    {
        foreach (var barrier in barriers) if (barrier) barrier.enabled = blocked;
        if (exteriorBarrier) exteriorBarrier.enabled = blocked;
    }

    void ShowFrame(int index)
    {
        doorRenderer.sprite = openingFrames[index];
        if (exteriorRenderer) exteriorRenderer.sprite = openingFrames[index];
        foreach (var renderer in linkedRenderers) if (renderer) renderer.sprite = openingFrames[index];
    }
}
