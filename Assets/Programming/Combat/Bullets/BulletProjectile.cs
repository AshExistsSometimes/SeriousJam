using UnityEngine;

public class BulletProjectile : MonoBehaviour
{

    private BulletSO data;
    private Vector3 direction;
    private float timer;
    private bool initialized;
    public float myDamage = 0f;

    private LayerMask hitMask;

    private Rigidbody rb;

    public bool originBullet = true;// prevents multi shot bullets from exploding PCs

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(BulletSO bulletData, Vector3 dir, LayerMask mask, bool isOriginBullet)
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

        if (isOriginBullet)
        {
            foreach (var effect in data.effects)
                effect.OnSpawn(this);
        }
        
    }

    private void Update()
    {
        if (!initialized || data == null) return;

        Vector3 nextPos = transform.position + direction * data.speed * Time.deltaTime;

        if (rb != null)
        {
            rb.MovePosition(nextPos);
        }
        else
        {
            transform.position = nextPos;
        }

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


    public BulletSO GetBulletData()
    {
        return data;
    }

    public LayerMask GetHitMask()
    {
        return hitMask;
    }
}