using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float damage;
    private float lifetime;
    private float timer;
    private bool initialized;

    public void Initialize(Vector3 dir, float spd, float dmg, float life)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        lifetime = life;
        initialized = true;
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

        // Hit wall
        if (other.CompareTag("Wall"))
            Destroy(gameObject);
    }
}