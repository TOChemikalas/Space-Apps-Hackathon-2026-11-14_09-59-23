using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Put this on an empty IntroDirector GameObject in the CinematicIntro scene.
// Slide 0 is a title screen: it waits for a keyboard / mouse / touch interaction.
// Remaining slides play automatically with fades and music.
public class ProjectSolCinematicIntro : MonoBehaviour
{
    [Header("Scenes and slides (order matters)")]
    [SerializeField] private string gameplaySceneName = "SampleScene";
    [SerializeField] private Sprite[] slides = new Sprite[6];

    [Header("UI references")]
    [SerializeField] private Image slideImage;
    [SerializeField] private CanvasGroup blackOverlay;
    [SerializeField] private GameObject pressAnyKeyPrompt;

    [Header("Soundtrack")]
    [SerializeField] private AudioSource musicSource;

    [Header("Timing")]
    [SerializeField, Min(0.05f)] private float fadeDuration = 0.8f;
    [SerializeField, Min(0.1f)] private float slideHoldDuration = 4.0f;
    [SerializeField, Min(0.1f)] private float landingHoldDuration = 5.0f;
    [SerializeField, Min(0.05f)] private float finalFadeDuration = 1.5f;

    private bool isPlaying;
    private bool isLeaving;

    private void Start()
    {
        // Use unscaled time below, so the intro works even if gameplay was paused.
        Time.timeScale = 1f;

        if (slideImage == null || blackOverlay == null || slides == null || slides.Length == 0 || slides[0] == null)
        {
            Debug.LogError("ProjectSolCinematicIntro: Assign the Image, Black Overlay and all slides.", this);
            enabled = false;
            return;
        }

        blackOverlay.alpha = 0f;
        blackOverlay.blocksRaycasts = false;
        blackOverlay.interactable = false;
        slideImage.sprite = slides[0];
        slideImage.preserveAspect = true;

        if (pressAnyKeyPrompt != null) pressAnyKeyPrompt.SetActive(true);
        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            if (musicSource.clip != null) musicSource.clip.LoadAudioData();
        }
    }

    private void Update()
    {
        if (!isActiveAndEnabled || isPlaying || isLeaving) return;

        bool keyPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool screenTouched = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

        if (keyPressed || mouseClicked || screenTouched) BeginIntro();
    }

    // Optional: connect a UI Button's OnClick to this method.
    public void BeginIntro()
    {
        if (!isActiveAndEnabled || isPlaying || isLeaving) return;
        isPlaying = true;

        if (pressAnyKeyPrompt != null) pressAnyKeyPrompt.SetActive(false);

        StartCoroutine(PlayRemainingSlides());
    }

    private IEnumerator PlayRemainingSlides()
    {
        bool first = true;
        for (int i = 1; i < slides.Length; i++)
        {
            if (slides[i] == null)
            {
                Debug.LogWarning($"Intro slide {i + 1} is unassigned; skipping it.", this);
                continue;
            }

            if (!first) yield return FadeBlackTo(1f, fadeDuration);
            slideImage.sprite = slides[i];
            if (first)
            {
                if (musicSource != null && musicSource.clip != null) musicSource.Play();
                first = false;
            }
            else
            {
                yield return FadeBlackTo(0f, fadeDuration);
            }

            float hold = (i == slides.Length - 1) ? landingHoldDuration : slideHoldDuration;
            float elapsed = 0f;
            while (elapsed < hold)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        yield return FinishAndLoadGameplay();
    }

    // Optional: connect a Skip button to this method. Avoid placing it over slide captions.
    public void SkipToGameplay()
    {
        if (!isActiveAndEnabled || isLeaving) return;
        StopAllCoroutines();
        StartCoroutine(FinishAndLoadGameplay());
    }

    private IEnumerator FinishAndLoadGameplay()
    {
        if (!Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            Debug.LogError($"Gameplay scene '{gameplaySceneName}' is not in Build Profiles / Scene List.", this);
            yield break;
        }

        isLeaving = true;
        AsyncOperation load = SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Single);
        if (load == null) yield break;
        load.allowSceneActivation = false;

        float startAlpha = blackOverlay.alpha;
        float startVolume = musicSource != null ? musicSource.volume : 0f;
        float elapsed = 0f;

        while (elapsed < finalFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / finalFadeDuration);
            blackOverlay.alpha = Mathf.Lerp(startAlpha, 1f, t);
            if (musicSource != null) musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        blackOverlay.alpha = 1f;
        if (musicSource != null) musicSource.Stop();

        load.allowSceneActivation = true;
    }

    private IEnumerator FadeBlackTo(float target, float duration)
    {
        float from = blackOverlay.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            blackOverlay.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        blackOverlay.alpha = target;
    }
}

