using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Handles main menu interactions, gunshot effects,
/// scene transitions and application quitting.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string GameplaySceneName = "Level";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip gunshotSFX;
    [SerializeField] private AudioClip StartSound;
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    [Header("UI")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private RectTransform bulletHoleImage;

    [Header("Transition")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float clickDelay = 0.15f;

    private bool hasClicked;

    /// <summary>
    /// Returns whether the menu is currently locked.
    /// </summary>
    public bool IsLocked => hasClicked;

    private void Start()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(false);
        }

        if (bulletHoleImage != null)
        {
            bulletHoleImage.gameObject.SetActive(false);
        }


        if (audioSource == null || StartSound == null)
            return;
        audioSource.PlayOneShot(StartSound);
    }

    /// <summary>
    /// Called by Play button.
    /// </summary>
    public void PlayGame()
    {
        if (hasClicked)
            return;

        StartCoroutine(PlayRoutine());
    }

    /// <summary>
    /// Called by Quit button.
    /// </summary>
    public void QuitToDesktop()
    {
        if (hasClicked)
            return;

        StartCoroutine(QuitRoutine());
    }

    /// <summary>
    /// Handles play transition.
    /// </summary>
    private IEnumerator PlayRoutine()
    {
        hasClicked = true;

        SpawnBulletHole();
        PlayGunshot();

        yield return new WaitForSeconds(clickDelay);

        yield return FadeToBlack();

        SceneManager.LoadScene(GameplaySceneName);
    }

    /// <summary>
    /// Handles quit transition.
    /// </summary>
    private IEnumerator QuitRoutine()
    {
        hasClicked = true;

        SpawnBulletHole();
        PlayGunshot();

        yield return new WaitForSeconds(clickDelay);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Plays gunshot with random pitch.
    /// </summary>
    private void PlayGunshot()
    {
        if (audioSource == null || gunshotSFX == null)
            return;

        audioSource.pitch = Random.Range(minPitch, maxPitch);
        audioSource.PlayOneShot(gunshotSFX);
    }

    /// <summary>
    /// Moves bullet hole image to cursor position.
    /// </summary>
    private void SpawnBulletHole()
    {
        if (bulletHoleImage == null)
            return;

        bulletHoleImage.gameObject.SetActive(true);
        bulletHoleImage.position = Input.mousePosition;
    }

    /// <summary>
    /// Fades screen to black.
    /// </summary>
    private IEnumerator FadeToBlack()
    {
        if (fadeImage == null)
            yield break;

        fadeImage.gameObject.SetActive(true);

        Color color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            color.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            fadeImage.color = color;

            yield return null;
        }

        color.a = 1f;
        fadeImage.color = color;
    }
}