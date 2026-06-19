using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a bullet type including stats and modular effects.
/// Supports stacking multiple special behaviours per bullet.
/// </summary>
[CreateAssetMenu(menuName = "SpinToWin/Bullet")]
public class BulletSO : ScriptableObject
{
    [Header("Base Stats")]
    public string bulletName;
    public Sprite icon;
    public GameObject bulletPrefab;

    public float damage = 10f;
    public float speed = 20f;
    public float lifetime = 3f;

    [Header("Special Effects")]
    public List<BulletEffect> effects = new List<BulletEffect>();
}