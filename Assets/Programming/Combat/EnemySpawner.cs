using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Activation")]
    [Range(0f, 1f)]
    [Tooltip("Chance this spawner is active. 1 = always, 0 = never.")]
    public float activeChance = 0.75f;

    [Header("Spawning")]
    public List<GameObject> potentialEnemies = new();
    public float spawnDelay = 0.5f;

    [Tooltip("How many random NavMesh positions to sample when finding a spawn point.")]
    public float spawnSampleRadius = 1f;

    [Header("Enemy Count Weighting")]
    [Range(0f, 1f)]
    public float levelWeighting = 0.4f;
    public int MinimumEnemiesToSpawn = 2;


    [Header("Boss Spawner")]
    public bool BossSpawner;

    private bool hasTriggered;
    private bool isActive;

    private void Awake()
    {
        float roll = Random.value;
        if (roll > activeChance)
        {
            Debug.Log($"[EnemySpawner] {gameObject.name} deactivated (rolled {roll:F2}, needed <= {activeChance:F2})");
            Destroy(gameObject);
            return;
        }

        Debug.Log($"[EnemySpawner] {gameObject.name} is ACTIVE (rolled {roll:F2})");
        isActive = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[EnemySpawner] {gameObject.name} trigger entered by {other.gameObject.name} (tag: {other.tag})");

        if (!isActive)
        {
            Debug.Log($"[EnemySpawner] {gameObject.name} is not active, ignoring.");
            return;
        }

        if (hasTriggered)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        hasTriggered = true;
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(spawnDelay);

        if (potentialEnemies.Count == 0)
        {
            yield break;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning($"[EnemySpawner] {gameObject.name} could not find GameManager.Instance — defaulting to level 1.");
        }

        int level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1;
        int count = RollEnemyCount(level);


        int successCount = 0;
        Bounds bounds = GetComponent<Collider>().bounds;
        Vector3 roomCenter = bounds.center;

        for (int i = 0; i < count; i++)
        {
            if (!TryGetNavMeshPosition(out Vector3 spawnPos))
            {
                continue;
            }

            GameObject prefab = potentialEnemies[Random.Range(0, potentialEnemies.Count)];

            Vector3 finalSpawnPos = BossSpawner ? roomCenter : spawnPos;

            GameObject spawned = Instantiate(prefab, finalSpawnPos, Quaternion.identity);

            BaseBoss boss = spawned.GetComponent<BaseBoss>();

            if (boss != null)
            {
                boss.InitializeBoss(roomCenter);
            }

            successCount++;
        }

        Debug.Log($"[EnemySpawner] {gameObject.name} done. {successCount}/{count} enemies spawned successfully.");
    }

    // ?? ENEMY COUNT ???????????????????????????????????????????????????????????

    private int RollEnemyCount(int level)
    {
        int min = MinimumEnemiesToSpawn;
        int max = Mathf.FloorToInt(level * 1.25f);

        List<int> pool = new();
        for (int i = min; i <= max; i++)
            pool.Add(i);

        int extraCopies = Mathf.RoundToInt(levelWeighting * 10f);
        for (int i = 0; i < extraCopies; i++)
            pool.Add(level);


        if (!BossSpawner)
        {
            int result = pool[Random.Range(0, pool.Count)];
            Debug.Log($"[EnemySpawner] RollEnemyCount: level={level}, range=[{min}-{max}], rolled={result}");
            return result;
        }
        else
        {
            int result = 1;

            return result;
        }    
        
    }

    // ?? NAVMESH SAMPLING ??????????????????????????????????????????????????????

    private bool TryGetNavMeshPosition(out Vector3 result)
    {
        Collider col = GetComponent<Collider>();
        Bounds bounds = col != null ? col.bounds : new Bounds(transform.position, Vector3.one * 5f);

        // Try with increasing radius on each failed pass
        float[] radii = { 2f, 5f, 10f };

        foreach (float radius in radii)
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                Vector3 candidate = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    bounds.center.y,        // Y doesn't matter much — SamplePosition searches vertically
                    Random.Range(bounds.min.z, bounds.max.z));

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, NavMesh.AllAreas))
                {
                    Debug.Log($"[EnemySpawner] NavMesh hit at {hit.position} with radius {radius}");
                    result = hit.position;
                    return true;
                }
            }

            Debug.Log($"[EnemySpawner] No hit with radius {radius}, trying larger...");
        }

        // Last resort: try sampling directly at the spawner's own position with a huge radius
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit fallback, 50f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"[EnemySpawner] Used last-resort sample at {fallback.position}");
            result = fallback.position;
            return true;
        }

        Debug.LogError($"[EnemySpawner] Completely failed to find NavMesh near {transform.position}. " +
                       $"Bounds: {bounds}. NavMesh may not be baked or this spawner is far from walkable area.");
        result = Vector3.zero;
        return false;
    }

    // ?? GIZMOS ????????????????????????????????????????????????????????????????

    private void OnDrawGizmos()
    {
        if (!BossSpawner)
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
        }
        else
        {
            Gizmos.color = Color.red;
        }
            Collider col = GetComponent<Collider>();
        if (col != null)
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        else
            Gizmos.DrawWireSphere(transform.position, 3f);
    }
}