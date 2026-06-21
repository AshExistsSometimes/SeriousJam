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

    private bool isDead;

    private void Awake()
    {
        currentHP = maxHP;

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

        if (currentHP <= 0f)
            Die();
    }

    public void RestoreHP(float amount)
    {
        if (isDead) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
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
}