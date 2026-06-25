using UnityEngine;

/// <summary>
/// Activated after boss defeat.
/// Enables exit trigger and spawns reward.
/// </summary>
public class LevelExit : MonoBehaviour
{
    [Header("Exit")]
    public GameObject ExitTrigger;

    [Header("Reward")]
    public GameObject BossRewardPrefab;
    public Transform BossRewardSpot;

    private bool activated;

    private void OnEnable()
    {
        GameManager.OnBossDefeated += HandleBossDefeated;
    }

    private void OnDisable()
    {
        GameManager.OnBossDefeated -= HandleBossDefeated;
    }

    private void Start()
    {
        if (ExitTrigger != null)
        {
            ExitTrigger.SetActive(false);
        }
    }

    private void HandleBossDefeated()
    {
        if (activated)
            return;

        activated = true;

        if (ExitTrigger != null)
        {
            ExitTrigger.SetActive(true);
        }

        if (BossRewardPrefab != null && BossRewardSpot != null)
        {
            Instantiate(
                BossRewardPrefab,
                BossRewardSpot.position,
                BossRewardSpot.rotation);
        }
    }
}