using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BaseEnemy : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float maxHP = 50f;
    public float currentHP;
    public float moveSpeed = 3f;

    [Header("Detection")]
    public float playerDetectionRange = 12f;
    public float rememberPlayerLocationTime = 3f;
    public LayerMask wallLayer;

    [Header("Knockback")]
    public float knockbackRecoverySpeed = 8f;

    public bool TargetingPlayer { get; protected set; }
    protected Transform player;
    protected Vector3 lastKnownPlayerPos;

    public bool DEBUGCanSeePlayer;

    private float lostSightTimer;
    private bool isDead;
    private Vector3 knockbackVelocity;
    private bool isKnockedBack;

    protected NavMeshAgent agent;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.updateRotation = false;
        currentHP = maxHP;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    protected virtual void Update()
    {
        if (isDead) return;
        HandleKnockback();
        UpdateSightline();

#if UNITY_EDITOR
        DEBUGCanSeePlayer = TargetingPlayer;
#endif
    }

    // ?? SIGHTLINE ?????????????????????????????????????????????????????????????

    private void UpdateSightline()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= playerDetectionRange && HasLineOfSight())
        {
            TargetingPlayer = true;
            lastKnownPlayerPos = player.position;
            lostSightTimer = 0f;
        }
        else if (TargetingPlayer)
        {
            lostSightTimer += Time.deltaTime;
            if (lostSightTimer >= rememberPlayerLocationTime)
                TargetingPlayer = false;
        }
    }

    protected bool HasLineOfSight()
    {
        if (player == null) return false;

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 target = player.position + Vector3.up * 0.5f;
        Vector3 dir = target - origin;

        return !Physics.Raycast(origin, dir.normalized, dir.magnitude, wallLayer);
    }

    // ?? KNOCKBACK ?????????????????????????????????????????????????????????????

    public void ApplyKnockback(Vector3 direction, float speed)
    {
        knockbackVelocity = direction.normalized * speed;
        isKnockedBack = true;
        agent.isStopped = true;
        agent.ResetPath();
    }

    private void HandleKnockback()
    {
        if (!isKnockedBack) return;
        if (!agent.isOnNavMesh) return;

        agent.Move(knockbackVelocity * Time.deltaTime);

        knockbackVelocity = Vector3.MoveTowards(
            knockbackVelocity, Vector3.zero, knockbackRecoverySpeed * Time.deltaTime);

        if (knockbackVelocity.sqrMagnitude < 0.01f)
        {
            isKnockedBack = false;
            agent.isStopped = false;
        }
    }

    // ?? ROTATION ??????????????????????????????????????????????????????????????

    protected void FaceToward(Vector3 worldPos, float turnSpeed = 10f)
    {
        Vector3 dir = worldPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
    }

    // ?? IDAMAGEABLE ???????????????????????????????????????????????????????????

    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;
        currentHP -= damage;
        if (currentHP <= 0f) Die();
    }

    public virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        if (agent.isOnNavMesh) agent.isStopped = true;
        Destroy(gameObject);
    }

    // ?? GIZMOS ????????????????????????????????????????????????????????????????

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRange);
    }
}