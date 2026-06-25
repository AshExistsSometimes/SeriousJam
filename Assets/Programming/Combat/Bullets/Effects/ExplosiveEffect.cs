using UnityEngine;

[CreateAssetMenu(menuName = "SpinToWin/BulletEffects/Explosive")]
public class ExplosiveEffect : BulletEffect
{
    public float radius = 3f;
    public float damage = 25f;
    public LayerMask enemyLayer;

    public override void OnHit(GameObject hit, Vector3 point, Vector3 direction)
    {
        Collider[] hits = Physics.OverlapSphere(point, radius, enemyLayer);

        foreach (var col in hits)
        {
            BaseEnemy enemy = col.GetComponent<BaseEnemy>();
            if (enemy == null) continue;

            enemy.TakeDamage(damage);

            Vector3 knockDir = (enemy.transform.position - point).normalized;
            enemy.ApplyKnockback(knockDir, 10f);
        }
    }
}