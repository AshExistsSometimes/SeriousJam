using UnityEngine;
using UnityEngine.AI;

public class BossStandard : BaseBoss
{
    public enum Phase { FollowPlayer, TurretAttack, ShockwaveAttack }

    public Phase DebugPhase;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    public float projectileDamage = 10f;
    public float projectileLifetime = 5f;

    [Header("Follow Player")]
    public float followDuration = 6f;
    public float followAttackRate = 1f;
    public float idealFollowRange = 6f;
    public float followTolerance = 1.5f;

    [Header("Turret Attack")]
    public float turretDuration = 5f;
    public float turretFireRate = 0.25f;
    public float turretSpeedMultiplier = 4f;

    [Header("Shockwave")]
    public int shockwaveLoops = 2;
    public float shockwaveFireRate = 1.5f;

    // ?? STATE ?????????????????????????????????????????????????????????????????

    private Phase phase;
    private float stateTimer;
    private float attackTimer;
    private float defaultSpeed;

    // Turret
    private Vector3 turretTarget;
    private bool turretReached;
    private float defaultTurretFireRate;

    // Shockwave
    private int shockwaveShotsLeft;
    private float shockwaveOffset;
    private Vector3 shockwaveCenter;

    // ?? LIFECYCLE ?????????????????????????????????????????????????????????????

    protected override void Awake()
    {
        base.Awake();
        defaultSpeed = moveSpeed;

        defaultTurretFireRate = turretFireRate;
    }

    private void Start()
    {
        EnterFollow();
    }

    protected override void Update()
    {
        base.Update();

        if (IsSpawning || IsDying || !agent.isOnNavMesh) return;

        DebugPhase = phase;

        switch (phase)
        {
            case Phase.FollowPlayer: UpdateFollow(); break;
            case Phase.TurretAttack: UpdateTurret(); break;
            case Phase.ShockwaveAttack: UpdateShockwave(); break;
        }
    }

    // ?? FOLLOW ????????????????????????????????????????????????????????????????

    private void EnterFollow()
    {
        phase = Phase.FollowPlayer;
        stateTimer = followDuration;
        attackTimer = 0f;
        SetSpeed(defaultSpeed);
        agent.stoppingDistance = 0f;
    }

    private void UpdateFollow()
    {
        stateTimer -= Time.deltaTime;

        if (TargetingPlayer)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            float tooClose = idealFollowRange - followTolerance;
            float tooFar = idealFollowRange + followTolerance;

            if (dist < tooClose)
                agent.SetDestination(transform.position + (transform.position - player.position).normalized * defaultSpeed);
            else if (dist > tooFar)
                agent.SetDestination(player.position);
            else
                agent.ResetPath();

            FaceToward(player.position);

            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                attackTimer = followAttackRate;
                FireAtPlayer();
            }
        }

        if (stateTimer <= 0f)
        {
            if (Random.value > 0.5f) EnterTurret();
            else EnterShockwave();
        }
    }

    // ?? TURRET ????????????????????????????????????????????????????????????????

    private void EnterTurret()
    {
        if (GameManager.Instance.CurrentLevel != 1)
        {
            turretFireRate = (defaultTurretFireRate - ((GameManager.Instance.CurrentLevel * 2) / 10));

            if (turretFireRate < 0.1f)
            {
                turretFireRate = 0.1f;
            }
        }

        phase = Phase.TurretAttack;
        stateTimer = turretDuration;
        attackTimer = 0f;
        turretReached = false;
        turretTarget = GetTurretPoint();

        SetSpeed(defaultSpeed * turretSpeedMultiplier);

        // Low stopping distance so it actually reaches the point
        agent.stoppingDistance = 0.5f;
        agent.SetDestination(turretTarget);
    }

    private void UpdateTurret()
    {
        if (!turretReached)
        {
            float dist = Vector3.Distance(transform.position, turretTarget);
            if (dist > 1f)
            {
                FaceToward(turretTarget);
                return;
            }

            // Arrived
            turretReached = true;
            agent.ResetPath();
            SetSpeed(defaultSpeed);
            agent.stoppingDistance = 0f;
        }

        // Stationary turret phase
        if (player != null) FaceToward(player.position);

        stateTimer -= Time.deltaTime;
        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            attackTimer = turretFireRate;
            FireAtPlayer();
        }

        if (stateTimer <= 0f)
            EnterShockwave();
    }

    private Vector3 GetTurretPoint()
    {
        Vector3 c = roomCenter;
        c.y = transform.position.y;

        Vector3[] options =
        {
            c,
            c + new Vector3(-14f, 0f,  14f),
            c + new Vector3( 14f, 0f,  14f),
            c + new Vector3( 14f, 0f, -14f),
            c + new Vector3(-14f, 0f, -14f)
        };

        return options[Random.Range(0, options.Length)];
    }

    // ?? SHOCKWAVE ?????????????????????????????????????????????????????????????

    private void EnterShockwave()
    {
        phase = Phase.ShockwaveAttack;
        shockwaveShotsLeft = shockwaveLoops * GameManager.Instance.CurrentLevel;
        attackTimer = 0f;
        shockwaveOffset = 0f;

        SetSpeed(defaultSpeed);

        // Sample center with generous radius, fall back to current position
        if (NavMesh.SamplePosition(roomCenter, out NavMeshHit hit, 20f, NavMesh.AllAreas))
            shockwaveCenter = hit.position;
        else
            shockwaveCenter = transform.position;

        // Critical: low stopping distance so the agent actually arrives
        agent.stoppingDistance = 0.5f;
        agent.SetDestination(shockwaveCenter);
    }

    private void UpdateShockwave()
    {
        float dist = Vector3.Distance(transform.position, shockwaveCenter);

        if (dist > 3.5f)
        {
            agent.SetDestination(shockwaveCenter);
            Debug.Log($"[Boss] Moving to shockwave center, dist={dist:F2}");
            return;
        }

        agent.ResetPath();

        attackTimer -= Time.deltaTime;
        Debug.Log($"[Boss] At center, attackTimer={attackTimer:F2}, shotsLeft={shockwaveShotsLeft}");

        if (attackTimer > 0f) return;

        attackTimer = shockwaveFireRate;
        shockwaveOffset += 30f;

        Debug.Log($"[Boss] Firing shockwave, offset={shockwaveOffset:F1}, projectilePrefab={(projectilePrefab == null ? "NULL" : projectilePrefab.name)}");

        for (int i = 0; i < 8; i++)
        {
            float angle = shockwaveOffset + (360f / 8f) * i;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Debug.Log($"[Boss] Firing projectile {i} at angle={angle:F1} dir={dir}");
            FireProjectile(dir, false);

            if (audioSource != null && AttackSFX != null)
            {
                audioSource.pitch = Random.Range(AttackPitchVariation.x, AttackPitchVariation.y);
                audioSource.PlayOneShot(AttackSFX);
            }
        }

        shockwaveShotsLeft--;
        Debug.Log($"[Boss] Shots remaining: {shockwaveShotsLeft}");

        if (shockwaveShotsLeft <= 0)
            EnterFollow();
    }

    // ?? PROJECTILES ???????????????????????????????????????????????????????????

    private void FireAtPlayer()
    {
        if (player == null) return;

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dir = (player.position + Vector3.up * 0.5f - origin).normalized;
        FireProjectile(dir, true);
    }

    private void FireProjectile(Vector3 direction, bool playSound) 
    { 
        if (projectilePrefab == null) 
            return; 
        
        if (audioSource != null && AttackSFX != null && playSound) 
        { 
            audioSource.pitch = Random.Range(AttackPitchVariation.x, AttackPitchVariation.y);
            audioSource.PlayOneShot(AttackSFX);
        } 
        
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        
        GameObject proj = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
        
        proj.GetComponent<EnemyProjectile>()?.Initialize(direction, projectileSpeed, projectileDamage, projectileLifetime, wallLayer); }

    // ?? HELPERS ???????????????????????????????????????????????????????????????

    private void SetSpeed(float speed)
    {
        moveSpeed = speed;
        agent.speed = speed;
    }
}