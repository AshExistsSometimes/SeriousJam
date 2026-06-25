using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Boss enemy with intro, health bar and fade-out death.
/// </summary>
public class BaseBoss : BaseEnemy
{
    [Header("Spawn Intro")]
    public float SpawnHeight = 15f;
    public float FallSpeed = 20f;
    public float IntroDelay = 1f;

    [Header("Death")]
    public float FadeDuration = 2f;

    private Renderer[] cachedRenderers;
    private Material[] cachedMaterials;

    protected Vector3 roomCenter;
    private bool roomCenterAssigned;
    private bool spawning;
    private bool dying;

    protected bool IsSpawning => spawning;
    protected bool IsDying => dying;

    public void InitializeBoss(Vector3 centre)
    {
        roomCenter = centre;
        roomCenterAssigned = true;

        if (GameManager.Instance.CurrentLevel > 1)
        {
            maxHP = 
                (maxHP + (maxHP * (GameManager.Instance.CurrentLevel * 0.15f) ));

            currentHP = maxHP;
        }
    }

    protected override void Awake()
    {
        base.Awake();

        CameraManager.Instance.SetCameraMode(CameraMode.Room);

        cachedRenderers = GetComponentsInChildren<Renderer>();

        int materialCount = 0;

        foreach (Renderer r in cachedRenderers)
        {
            materialCount += r.materials.Length;
        }

        cachedMaterials = new Material[materialCount];

        int index = 0;

        foreach (Renderer r in cachedRenderers)
        {
            foreach (Material mat in r.materials)
            {
                cachedMaterials[index] = mat;
                index++;
            }
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        SetAlpha(1f);

        StartCoroutine(SpawnRoutine());
    }

    protected override void Update()
    {
        if (spawning || dying)
            return;

        base.Update();
    }

    private IEnumerator SpawnRoutine()
    {
        spawning = true;

        yield return null;

        if (!roomCenterAssigned)
        {
            roomCenter = transform.position;
        }

        Vector3 spawnPos = roomCenter + Vector3.up * SpawnHeight;

        transform.position = spawnPos;

        if (agent.isOnNavMesh)
        {
            agent.enabled = false;
        }

        Vector3 landingPoint = roomCenter;

        RaycastHit hit;

        if (Physics.Raycast(
            roomCenter + Vector3.up * 50f,
            Vector3.down,
            out hit,
            200f))
        {
            landingPoint = hit.point;
        }

        Vector3 velocity = Vector3.down * FallSpeed;

        while (Vector3.Distance(transform.position, landingPoint) > 0.1f)
        {
            transform.position += velocity * Time.deltaTime;

            if (transform.position.y <= landingPoint.y)
            {
                transform.position = landingPoint;
                break;
            }

            yield return null;
        }

        agent.enabled = true;

        yield return new WaitForSeconds(IntroDelay);

        BossUIManager.Instance.ShowBoss(
            EnemyName,
            maxHP);

        spawning = false;
    }

    public override void TakeDamage(float damage)
    {
        if (dying)
            return;

        base.TakeDamage(damage);

        if (BossUIManager.Instance != null)
        {
            BossUIManager.Instance.UpdateHealth(currentHP);
        }
    }

    public override void Die()
    {
        if (dying)
            return;

        dying = true;

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        float timer = 0f;

        while (timer < FadeDuration)
        {
            timer += Time.deltaTime;

            float alpha = Mathf.Lerp(
                1f,
                0f,
                timer / FadeDuration);

            SetAlpha(alpha);

            yield return null;
        }

        SetAlpha(0f);

        GameManager.Instance.BossDefeated();

        if (BossUIManager.Instance != null)
        {
            BossUIManager.Instance.HideBoss();
        }

        Destroy(gameObject);
    }

    private void SetAlpha(float alpha)
    {
        foreach (Material mat in cachedMaterials)
        {
            if (!mat.HasProperty("_Color"))
                continue;

            Color c = mat.color;
            c.a = alpha;
            mat.color = c;
        }
    }
}