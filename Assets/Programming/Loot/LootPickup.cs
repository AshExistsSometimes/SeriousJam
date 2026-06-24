using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger that feeds a LootWheelUI with a custom loot pool.
/// Each pickup can define its own standard + jackpot rewards.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LootPickup : MonoBehaviour
{
    [Header("Loot Pools")]
    [SerializeField] private List<BulletSO> regularLootPool = new();
    [SerializeField] private List<BulletSO> jackpotLootPool = new();

    [Header("Wheel Reference")]
    [SerializeField] private LootWheelUI lootWheelUI;

    [Header("Auto Find")]
    [SerializeField] private bool findWheelAutomatically = true;

    private bool activated;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        lootWheelUI = LootWheelUI.Instance;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activated)
            return;

        if (!IsPlayer(other))
            return;

        if (lootWheelUI == null)
        {
            Debug.LogError("[LootPickup] Missing LootWheelUI reference.");
            return;
        }

        activated = true;

        lootWheelUI.OpenWheel(regularLootPool, jackpotLootPool, this);
    }

    /// <summary>
    /// Called after reward is granted to remove pickup.
    /// </summary>
    public void Consume()
    {
        Destroy(gameObject);
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player")
            || other.GetComponent<PlayerMovement>() != null;
    }
}