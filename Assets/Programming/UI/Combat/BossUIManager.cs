using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton used by bosses to display health information.
/// </summary>
public class BossUIManager : MonoBehaviour
{
    public static BossUIManager Instance;

    [Header("References")]
    public GameObject BossHealthBar;
    public Slider BossHealthSlider;
    public TMP_Text BossName;

    private void Awake()
    {
        Instance = this;

        if (BossHealthBar != null)
        {
            BossHealthBar.SetActive(false);
        }
    }

    public void ShowBoss(string bossName, float maxHP)
    {
        BossHealthBar.SetActive(true);

        BossName.text = ("~ " + bossName + " ~");

        BossHealthSlider.maxValue = maxHP;
        BossHealthSlider.value = maxHP;
    }

    public void UpdateHealth(float currentHP)
    {
        BossHealthSlider.value = currentHP;
    }

    public void HideBoss()
    {
        BossHealthBar.SetActive(false);
    }
}