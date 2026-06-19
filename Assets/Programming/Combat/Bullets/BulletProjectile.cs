using UnityEngine;

/// <summary>
/// Handles bullet movement and collision.
/// Applies BulletSO effects on hit.
/// </summary>
public class BulletProjectile : MonoBehaviour
{
    private BulletSO data;
    private Vector3 direction;
    private float timer;
    private bool initialized;

    [SerializeField] private LayerMask hitMask;

    public void Initialize(BulletSO bulletData, Vector3 dir)
    {
        data = bulletData;
        direction = dir.normalized;
        initialized = true;

        if (data == null)
        {
            Debug.LogError("BulletProjectile initialized with NULL BulletSO!");
            return;
        }

        foreach (var effect in data.effects)
        {
            effect.OnSpawn(this);
        }
    }

    private void Update()
    {
        if (!initialized || data == null)
            return;

        transform.position += direction * data.speed * Time.deltaTime;

        timer += Time.deltaTime;

        if (timer >= data.lifetime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!initialized || data == null)
            return;

        // ignore non-hit objects
        if (!other.CompareTag("Enemy") && !other.CompareTag("Wall"))
            return;

        foreach (var effect in data.effects)
        {
            effect.OnHit(other.gameObject, transform.position, direction);
        }

        DestroySelf();
    }


    private void DestroySelf()
    {
        Destroy(gameObject);
    }
}