using UnityEngine;
using UnityEngine.UI;

public class EnemyProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float damage;
    private float lifetime;
    private float timer;
    private bool initialized;

    private LayerMask hitMask;

    public void Initialize(Vector3 dir, float spd, float dmg, float life, LayerMask mask)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        lifetime = life;
        initialized = true;

        hitMask = mask;
    }

    private void Update()
    {
        if (!initialized) return;

        transform.position += direction * speed * Time.deltaTime;

        timer += Time.deltaTime;
        if (timer >= lifetime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!initialized) return;

        // Don't hit other enemies
        if (other.GetComponent<BaseEnemy>() != null) return;

        // Hit player or any damageable
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Check wall by layer instead of tag
        if (((1 << other.gameObject.layer) & hitMask) != 0)
        {
            Destroy(gameObject);
            return;
        }
    }
}