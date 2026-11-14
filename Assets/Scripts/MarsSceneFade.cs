
using System.Collections;
using UnityEngine;

public class MarsSceneFade : MonoBehaviour
{
    [SerializeField] private CanvasGroup overlay;
    [SerializeField] private float fadeSeconds = 1.6f;
    [SerializeField] private bool pauseGameDuringFade = true;

    [SerializeField] private MarsClock clock;
    private bool clockWasEnabled;
    private static bool hasFadedOnce;
    private bool paused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        hasFadedOnce = false;
    }

    private void Awake()
    {
        if (overlay == null)
        {
            Debug.LogError("Assign the overlay CanvasGroup.");
            enabled = false;
            return;
        }

        if (hasFadedOnce)
        {
            overlay.alpha = 0f;
            gameObject.SetActive(false);
            return;
        }

        hasFadedOnce = true;
        overlay.alpha = 1f;

        if (pauseGameDuringFade)
        {
            if (clock)
            {
                clockWasEnabled = clock.enabled;
                clock.enabled = false;
            }
            Time.timeScale = 0f;
            paused = true;
        }
    }

    private void Start()
    {
        if (isActiveAndEnabled)
            StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;

        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            overlay.alpha = 1f - Mathf.Clamp01(elapsed / fadeSeconds);
            yield return null;
        }

        overlay.alpha = 0f;
        RestoreTime();
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        RestoreTime();
    }

    private void RestoreTime()
    {
        if (!paused) return;

        Time.timeScale = 1f;
        if (clock) clock.enabled = clockWasEnabled;
        paused = false;
    }
}
