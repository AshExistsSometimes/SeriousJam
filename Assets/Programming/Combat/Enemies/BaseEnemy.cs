using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BaseEnemy : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public string EnemyName = "Enemy";
    public float maxHP = 50f;
    public float currentHP;
    public float moveSpeed = 3f;

    [Header("Detection")]
    public float playerDetectionRange = 12f;
    public float rememberPlayerLocationTime = 3f;
    public LayerMask wallLayer;

    [Header("Knockback")]
    public float knockbackRecoverySpeed = 8f;

    [Header("Audio")]
    public AudioSource audioSource;
    [Space]
    [SerializeField] private AudioClip HurtSFX;
    public Vector2 HurtPitchVariation = new Vector2(0.95f, 1.05f);
    [Space]
    public AudioClip AttackSFX;
    public Vector2 AttackPitchVariation = new Vector2(0.95f, 1.05f);

    private readonly List<StatusEffectInstance> activeStatusEffects = new();
    private float baseMoveSpeed;

    public bool TargetingPlayer { get; protected set; }
    protected Transform player;
    protected Vector3 lastKnownPlayerPos;

    public bool DEBUGCanSeePlayer;

    private float lostSightTimer;
    protected bool isDead;
    private Vector3 knockbackVelocity;
    private bool isKnockedBack;

    protected NavMeshAgent agent;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        baseMoveSpeed = moveSpeed;
        agent.speed = moveSpeed;
        agent.updateRotation = false;
        currentHP = maxHP;

        audioSource = FindFirstObjectByType<AudioSource>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    protected virtual void Update()
    {
        if (isDead) return;
        HandleKnockback();
        UpdateSightline();
        UpdateStatusEffects();

#if UNITY_EDITOR
        DEBUGCanSeePlayer = TargetingPlayer;
#endif
    }

    protected virtual void OnEnable()
    {
        LevelManager.OnLevelRegenerating += HandleLevelRegenerating;
    }

    protected virtual void OnDisable()
    {
        LevelManager.OnLevelRegenerating -= HandleLevelRegenerating;
    }

    private void HandleLevelRegenerating()
    {
        Destroy(gameObject);
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

        audioSource.pitch = Random.Range(HurtPitchVariation.x, HurtPitchVariation.y);
        audioSource.PlayOneShot(HurtSFX);

        if (currentHP <= 0f) Die();
    }

    public virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        if (agent.isOnNavMesh) agent.isStopped = true;
        Destroy(gameObject);
    }

    // STATUS EFFECTS
    private void UpdateStatusEffects()
    {
        for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
        {
            if (!activeStatusEffects[i].Update(Time.deltaTime))
                activeStatusEffects.RemoveAt(i);
        }
    }

    public void ApplySlow(float amount, float duration)
    {
        float originalSpeed = agent.speed;
        agent.speed = baseMoveSpeed * amount;

        activeStatusEffects.Add(new StatusEffectInstance(
            duration,
            duration,
            null,
            () => agent.speed = baseMoveSpeed
        ));
    }

    public void ApplyBurn(float damagePerTick, int ticks, float tickRate)
    {
        int remaining = ticks;

        activeStatusEffects.Add(new StatusEffectInstance(
            ticks * tickRate,
            tickRate,
            _ =>
            {
                if (remaining <= 0) return;
                TakeDamage(damagePerTick);
                remaining--;
            }
        ));
    }

    // ?? GIZMOS ????????????????????????????????????????????????????????????????

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRange);
    }
}


public class StatusEffectInstance
{
    public float duration;
    public System.Action<float> tickAction;
    public System.Action onEnd;
    public float tickRate;
    public float tickTimer;

    public StatusEffectInstance(float duration, float tickRate, System.Action<float> tickAction, System.Action onEnd = null)
    {
        this.duration = duration;
        this.tickRate = tickRate;
        this.tickAction = tickAction;
        this.onEnd = onEnd;
    }

    public bool Update(float dt)
    {
        duration -= dt;
        tickTimer += dt;

        if (tickTimer >= tickRate)
        {
            tickTimer = 0f;
            tickAction?.Invoke(dt);
        }

        if (duration <= 0f)
        {
            onEnd?.Invoke();
            return false;
        }

        return true;
    }
}