using UnityEngine;
using UnityEngine.UI;
using static GreenhouseCanvas;

public sealed class MarsWatch : MonoBehaviour
{
    [SerializeField] private MarsClock clock;
    [SerializeField] private Canvas canvas;

    private Image card;
    private Text readout;
    private int lastSol, lastMinute = -1;

    private void Start()
    {
        if (!clock || !canvas) return;
        card = Image(canvas.transform, "Mars watch", new Rect(12, 12, 174, 46), Background);
        Image(card.transform, "Accent", new Rect(0, 0, 3, 46), Orange);
        readout = Label(card.transform, "Local time", new Rect(10, 0, 156, 46), "", 18, TextAnchor.MiddleLeft, FontStyle.Bold);
    }

    private void LateUpdate()
    {
        if (!readout) return;
        int minute = Mathf.Clamp((int)(clock.Hour * 60d + 1e-8), 0, 1439);
        int sol = clock.Sol;
        if (lastMinute == minute && lastSol == sol) return;
        lastMinute = minute;
        lastSol = sol;
        readout.text = $"{minute / 60:00}:{minute % 60:00}  ·  Sol {sol}";
    }

    private void OnDestroy()
    {
        if (card) Destroy(card.gameObject);
    }
}
