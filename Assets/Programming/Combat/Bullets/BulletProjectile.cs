using UnityEngine;

public class BulletProjectile : MonoBehaviour
{

    [SerializeField] private BulletSO data;
    [SerializeField] private Vector3 direction;
    [SerializeField] private float timer;
    [SerializeField] private bool initialized;
    public float myDamage = 0f;

    private LayerMask hitMask;

    public void Initialize(BulletSO bulletData, Vector3 dir, LayerMask mask)
    {
        data = bulletData;
        direction = dir.normalized;
        hitMask = mask;
        initialized = true;

        myDamage = data.damage;

        if (data == null)
        {
            Debug.LogError("BulletProjectile initialized with null BulletSO!");
            return;
        }

        foreach (var effect in data.effects)
            effect.OnSpawn(this);
    }

    private void Update()
    {
        if (!initialized || data == null) return;

        transform.position += direction * data.speed * Time.deltaTime;

        timer += Time.deltaTime;
        if (timer >= data.lifetime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!initialized || data == null) return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null && other.GetComponent<PlayerHealth>() == null)
        {
            foreach (var effect in data.effects)
                effect.OnHit(other.gameObject, transform.position, direction);

            damageable.TakeDamage(data.damage);
            Destroy(gameObject);
            return;
        }

        // Check wall by layer instead of tag
        if (((1 << other.gameObject.layer) & hitMask) != 0)
        {
            Destroy(gameObject);
        }
    }
}