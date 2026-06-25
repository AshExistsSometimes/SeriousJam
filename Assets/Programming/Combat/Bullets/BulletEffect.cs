using UnityEngine;

/// <summary>
/// Base class for all bullet effects.
/// Effects can modify projectiles or apply logic on hit.
/// </summary>
public abstract class BulletEffect : ScriptableObject
{
    /// <summary>
    /// Called when bullet is spawned. (must be during instantiation)
    /// </summary>
    public virtual void OnSpawn(BulletProjectile projectile) { }

    /// <summary>
    /// Called every frame if needed (optional use).
    /// </summary>
    public virtual void OnUpdate(BulletProjectile projectile) { }

    /// <summary>
    /// Called when bullet hits something.
    /// </summary>
    public abstract void OnHit(GameObject hit, Vector3 point, Vector3 direction);
}

