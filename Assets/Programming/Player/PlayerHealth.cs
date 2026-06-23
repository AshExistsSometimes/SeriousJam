using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHP = 100f;
    public float currentHP { get; private set; }

    [Header("Death UI")]
    [Tooltip("A full-screen UI Image that fades in on death")]
    public Image deathFadeImage;
    public float fadeDuration = 1.5f;

    [Header("References")]
    public PlayerCombat playerCombat;
    public PlayerMovement playerMovement;
    public Slider PlayerHealthSlider;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [Space]
    [SerializeField] private AudioClip HealSFX;
    public Vector2 HealPitchVariation = new Vector2(0.95f, 1.05f);
    [Space]
    [SerializeField] private AudioClip DamageSFX;
    public Vector2 DamagePitchVariation = new Vector2(0.95f, 1.05f);

    private bool isDead;

    private void Awake()
    {
        currentHP = maxHP;
        UpdateHealthSlider();

        if (deathFadeImage != null)
        {
            Color c = deathFadeImage.color;
            c.a = 0f;
            deathFadeImage.color = c;
            deathFadeImage.gameObject.SetActive(false);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHP = Mathf.Max(0f, currentHP - damage);

        audioSource.pitch = Random.Range(DamagePitchVariation.x, DamagePitchVariation.y);
        audioSource.PlayOneShot(DamageSFX);

        UpdateHealthSlider();

        if (currentHP <= 0f)
            Die();
    }

    public void RestoreHP(float amount)
    {
        if (isDead) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);

        audioSource.pitch = Random.Range(HealPitchVariation.x, HealPitchVariation.y);
        audioSource.PlayOneShot(HealSFX);

        UpdateHealthSlider();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (playerCombat != null) playerCombat.enabled = false;
        if (playerMovement != null) playerMovement.enabled = false;

        StartCoroutine(FadeDeathScreen());
    }

    private IEnumerator FadeDeathScreen()
    {
        if (deathFadeImage == null) yield break;

        deathFadeImage.gameObject.SetActive(true);
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            Color c = deathFadeImage.color;
            c.a = Mathf.Clamp01(elapsed / fadeDuration);
            deathFadeImage.color = c;
            yield return null;
        }
    }

    public void UpdateHealthSlider()
    {
        PlayerHealthSlider.maxValue = maxHP;
        PlayerHealthSlider.value = currentHP;
    }
}