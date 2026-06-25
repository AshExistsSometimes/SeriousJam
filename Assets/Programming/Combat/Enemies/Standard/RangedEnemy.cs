using UnityEngine;

public class RangedEnemy : BaseEnemy
{
    [Header("Positioning")]
    public float idealRange = 6f;
    [Tooltip("How many units of tolerance around idealRange. 0 = exact, 1 = ±1 unit.")]
    public float rangeStrictness = 0.5f;

    [Header("Attack")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 8f;
    public float projectileDamage = 10f;
    public float projectileLifetime = 4f;
    public float attackSpeed = 2f;

    private float attackTimer;

    protected override void Update()
    {
        base.Update();

        if (currentHP <= 0f) return;
        if (!agent.isOnNavMesh) return;   // Wait until placed on NavMesh

        if (TargetingPlayer)
        {
            HandlePositioning();
            HandleAttack();
            FaceToward(player.position);
        }
        else if (lastKnownPlayerPos != Vector3.zero)
        {
            agent.SetDestination(lastKnownPlayerPos);
            FaceToward(lastKnownPlayerPos);
        }
        else
        {
            agent.ResetPath();
        }
    }

    // ?? POSITIONING ???????????????????????????????????????????????????????????

    private void HandlePositioning()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        float tooClose = idealRange - rangeStrictness;
        float tooFar = idealRange + rangeStrictness;

        if (dist < tooClose)
        {
            Vector3 awayDir = (transform.position - player.position).normalized;
            Vector3 backTarget = transform.position + awayDir * moveSpeed;
            agent.SetDestination(backTarget);
        }
        else if (dist > tooFar)
        {
            agent.SetDestination(player.position);
        }
        else
        {
            agent.ResetPath();
        }
    }

    // ?? ATTACK ????????????????????????????????????????????????????????????????

    private void HandleAttack()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;

        attackTimer = attackSpeed;
        FireProjectile();
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null || player == null) return;

        audioSource.pitch = Random.Range(AttackPitchVariation.x, AttackPitchVariation.y);
        audioSource.PlayOneShot(AttackSFX);

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dir = (player.position + Vector3.up * 0.5f - origin).normalized;


        GameObject proj = Instantiate(
            projectilePrefab,
            origin,
            Quaternion.LookRotation(dir));

        proj.GetComponent<EnemyProjectile>()
            ?.Initialize(dir, projectileSpeed, projectileDamage, projectileLifetime, wallLayer);
    }

    // ?? GIZMOS ????????????????????????????????????????????????????????????????

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, idealRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, idealRange + rangeStrictness);
    }
}