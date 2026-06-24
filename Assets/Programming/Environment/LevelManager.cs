using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using System.Collections;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    /*
     * LEVEL GENERATION REQUIREMENTS
     *
     * MAP STRUCTURE
     * - All rooms are identical physical size.
     * - Rooms placed on grid using roomScale spacing.
     * - 4-direction connectivity (N/S/E/W).
     *
     * START / BOSS
     * - One Start Room at origin.
     * - One Boss Room at end of main path.
     * - MapLength = CurrentLevel^2 + 3 (clamped).
     *
     * GENERATION RULES
     * - Main path is guaranteed and biased toward +Z.
     * - Branches may split and reconnect.
     * - All rooms remain reachable from Start.
     *
     * DEBUG
     * - Cyan: all connections
     * - Green: main path
     * - Red: boss
     * - White: normal rooms
     */

    public static System.Action OnLevelRegenerating;

    [Header("Level Progression")]
    [SerializeField] private Transform player;
    [SerializeField] private Image screenFadeImage;
    [SerializeField] private float screenFadeDuration = 0.5f;

    [Header("Loot Rooms")]
    [SerializeField] private int lootRoomCap = 5;

    private Transform currentLevelParent;

    [Header("NavMesh")]
    public NavMeshSurface navMeshSurface;

    [Header("Grid")]
    public float roomScale = 35f;

    [Header("Map Length")]
    public int MapLengthCap = 50;
    public int MapLength;

    [Header("Generation")]
    public int debugLevel = 1;
    [Range(0f, 1f)]
    public float zBiasStrength = 0.5f;

    [Range(0f, 1f)]
    public float branchChance = 0.4f;

    [Range(0f, 1f)]
    public float reconnectChance = 0.35f;

    public int maxBranchLength = 4;

    private Dictionary<Vector2Int, RoomNode> roomGraph =
        new Dictionary<Vector2Int, RoomNode>();

    private Dictionary<Vector2Int, GameObject> spawnedRooms =
    new Dictionary<Vector2Int, GameObject>();

    public List<RoomNode> mainPath =
        new List<RoomNode>();

    private RoomNode startNode;
    private RoomNode bossNode;

    public List<RoomPrefabEntry> RoomPrefabs;

    public GameObject DoorFillerPrefab;

    public List<RoomNode> MainPath => mainPath;

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private static readonly Dictionary<Vector2Int, Direction> DirMap = new()
{
    { Vector2Int.up, Direction.North },
    { Vector2Int.down, Direction.South },
    { Vector2Int.right, Direction.East },
    { Vector2Int.left, Direction.West }
};

    private void Start()
    {
        GenerateLayout();
    }

    public void ProgressToNextLevel()
    {
        StartCoroutine(ProgressToNextLevelRoutine());
    }

    private IEnumerator ProgressToNextLevelRoutine()
    {
        // Increase level
        GameManager.Instance.CurrentLevel++;
        

        // Enable fade image
        screenFadeImage.gameObject.SetActive(true);

        Color fadeColor = screenFadeImage.color;
        fadeColor.a = 0f;
        screenFadeImage.color = fadeColor;

        // Fade to black
        float timer = 0f;

        while (timer < screenFadeDuration)
        {
            timer += Time.deltaTime;

            fadeColor.a = Mathf.Lerp(
                0f,
                1f,
                timer / screenFadeDuration);

            screenFadeImage.color = fadeColor;

            yield return null;
        }

        fadeColor.a = 1f;
        screenFadeImage.color = fadeColor;

        // Notify everything that the level is about to be regenerated
        OnLevelRegenerating?.Invoke();

        // Destroy previous level
        if (currentLevelParent != null)
            Destroy(currentLevelParent.gameObject);

        roomGraph.Clear();
        spawnedRooms.Clear();
        mainPath.Clear();

        // Move player back to start
        if (player != null)
        {
            CharacterController cc =
                player.GetComponent<CharacterController>();

            if (cc != null)
                cc.enabled = false;

            player.position = new Vector3(0f, 1f, 0f);

            if (cc != null)
                cc.enabled = true;
        }

        // Generate new level
        LoadLevel(GameManager.Instance.CurrentLevel);

        yield return null;

        // Fade back in
        timer = 0f;

        while (timer < screenFadeDuration)
        {
            timer += Time.deltaTime;

            fadeColor.a = Mathf.Lerp(
                1f,
                0f,
                timer / screenFadeDuration);

            screenFadeImage.color = fadeColor;

            yield return null;
        }

        fadeColor.a = 0f;
        screenFadeImage.color = fadeColor;

        screenFadeImage.gameObject.SetActive(false);

        GameManager.Instance.NextLevelLogic();
    }

    [ContextMenu("Generate Layout")]
    public void GenerateLayout()
    {
        LoadLevel(debugLevel);
    }

    private void LoadLevel(int currentLevelNumber)
    {
        int calculationResult = Mathf.FloorToInt(currentLevelNumber * (currentLevelNumber / 2) + 3);

        MapLength = Mathf.Min(
            calculationResult,
            MapLengthCap);

        GenerateGraph();
        SpawnRooms();
        StartCoroutine(GenerateWallsNextFrame());

        FindFirstObjectByType<MinimapPathRenderer>()?.BuildPath();
    }

    private void GenerateGraph()
    {
        roomGraph.Clear();
        mainPath.Clear();

        GenerateMainPath();
        GenerateBranches();
        GenerateReconnects();
        GenerateLootRooms();
    }

    // ---------------- MAIN PATH ----------------

    private void GenerateMainPath()
    {
        Vector2Int currentPos = Vector2Int.zero;

        startNode = CreateNode(currentPos, RoomType.Start);
        mainPath.Add(startNode);

        RoomNode previous = startNode;

        for (int i = 0; i < MapLength; i++)
        {
            Vector2Int nextDir = PickBiasedDirection(currentPos);
            Vector2Int nextPos = currentPos + nextDir;

            if (roomGraph.ContainsKey(nextPos))
                continue;

            RoomNode node = CreateNode(nextPos, RoomType.Normal);

            Connect(previous, node);

            previous = node;
            currentPos = nextPos;

            mainPath.Add(node);
        }

        previous.RoomType = RoomType.Boss;
        bossNode = previous;
    }

    // ---------------- BIAS SYSTEM ----------------

    private Vector2Int PickBiasedDirection(Vector2Int currentPos)
    {
        List<Vector2Int> options = new List<Vector2Int>();

        foreach (var dir in Directions)
        {
            Vector2Int pos = currentPos + dir;

            if (!roomGraph.ContainsKey(pos))
                options.Add(dir);
        }

        if (options.Count == 0)
            return Vector2Int.up;

        float totalWeight = 0f;
        float[] weights = new float[options.Count];

        for (int i = 0; i < options.Count; i++)
        {
            Vector2Int dir = options[i];

            // base randomness
            float weight = 1f;

            // +Z preference (dir.y == 1 is up)
            if (dir == Vector2Int.up)
                weight += zBiasStrength;

            // slight penalty for downward movement (optional UX improvement)
            if (dir == Vector2Int.down)
                weight -= zBiasStrength * 0.5f;

            weight = Mathf.Max(0.1f, weight);

            weights[i] = weight;
            totalWeight += weight;
        }

        float roll = Random.value * totalWeight;

        float current = 0f;

        for (int i = 0; i < options.Count; i++)
        {
            current += weights[i];

            if (roll <= current)
                return options[i];
        }

        return options[0];
    }

    // ---------------- BRANCHES ----------------

    private void GenerateBranches()
    {
        List<RoomNode> nodes = new List<RoomNode>(roomGraph.Values);

        foreach (RoomNode source in nodes)
        {
            if (source == bossNode)
                continue;

            if (Random.value > branchChance)
                continue;

            RoomNode current = source;

            int length = Random.Range(1, maxBranchLength + 1);

            for (int i = 0; i < length; i++)
            {
                List<Vector2Int> options = new List<Vector2Int>();

                foreach (var dir in Directions)
                {
                    Vector2Int pos = current.GridPosition + dir;

                    if (!roomGraph.ContainsKey(pos))
                        options.Add(dir);
                }

                if (options.Count == 0)
                    break;

                Vector2Int chosen = options[Random.Range(0, options.Count)];
                Vector2Int newPos = current.GridPosition + chosen;

                RoomNode node = CreateNode(newPos, RoomType.Normal);

                Connect(current, node);

                current = node;
            }
        }
    }

    // ---------------- RECONNECTS ----------------

    private void GenerateReconnects()
    {
        List<RoomNode> nodes = new List<RoomNode>(roomGraph.Values);

        foreach (RoomNode a in nodes)
        {
            foreach (var dir in Directions)
            {
                if (Random.value > reconnectChance)
                    continue;

                Vector2Int targetPos = a.GridPosition + dir;

                if (!roomGraph.TryGetValue(targetPos, out RoomNode b))
                    continue;

                if (a.Connections.Contains(b))
                    continue;

                Connect(a, b);
            }
        }
    }

    // ---------------- GRAPH CORE ----------------

    private RoomNode CreateNode(Vector2Int pos, RoomType type)
    {
        RoomNode node = new RoomNode();
        node.GridPosition = pos;
        node.RoomType = type;

        roomGraph.Add(pos, node);

        return node;
    }

    private void Connect(RoomNode a, RoomNode b)
    {
        if (!a.Connections.Contains(b))
            a.Connections.Add(b);

        if (!b.Connections.Contains(a))
            b.Connections.Add(a);
    }

    // ---------------- ROOM GENERATION ----------------
    private void SpawnRooms()
    {
        spawnedRooms.Clear();

        currentLevelParent =
            new GameObject(
                $"Level - {GameManager.Instance.CurrentLevel}")
            .transform;

        foreach (var node in roomGraph.Values)
        {
            Vector3 worldPos = GridToWorld(node.GridPosition);

            GameObject prefab = GetPrefab(node.RoomType);

            GameObject roomObj =
            Instantiate(
                prefab,
                worldPos,
                Quaternion.identity,
                currentLevelParent);

            roomObj.name = node.RoomType + " Room " + node.GridPosition;

            spawnedRooms[node.GridPosition] = roomObj;
        }
    }

    private void GenerateLootRooms()
    {
        int lootCount =
            Mathf.Min(
                GameManager.Instance.CurrentLevel,
                lootRoomCap);

        List<RoomNode> validRooms =
            new List<RoomNode>();

        foreach (RoomNode room in roomGraph.Values)
        {
            if (room.RoomType == RoomType.Normal)
                validRooms.Add(room);
        }

        for (int i = 0; i < lootCount; i++)
        {
            if (validRooms.Count == 0)
                break;

            int index =
                Random.Range(0, validRooms.Count);

            validRooms[index].RoomType =
                RoomType.Loot;

            validRooms.RemoveAt(index);
        }
    }

    private void TryPlaceWall(Vector2Int pos, Vector2Int dir, RoomNode node)
    {
        Vector2Int targetPos = pos + dir;

        bool hasRoom = roomGraph.ContainsKey(targetPos);
        bool isConnected = node.Connections.Exists(c => c.GridPosition == targetPos);

        if (hasRoom && isConnected)
            return;

        if (!spawnedRooms.TryGetValue(pos, out GameObject anchorRoom))
        {
            Debug.LogError($"[WALL] Missing spawned room at {pos}");
            return;
        }

        Room room = anchorRoom.GetComponent<Room>();
        if (room == null)
        {
            Debug.LogError($"[WALL] Missing Room component on {anchorRoom.name}");
            return;
        }

        Transform anchor = GetDoorAnchor(room, dir);
        if (anchor == null)
        {
            Debug.LogError($"[WALL] Missing anchor {dir} on {anchorRoom.name}");
            return;
        }

        if (DoorFillerPrefab == null)
        {
            Debug.LogError("[WALL] DoorFillerPrefab is NULL");
            return;
        }

        Debug.Log($"[WALL] Placing wall at {pos} dir {dir}");

        Instantiate(
            DoorFillerPrefab,
            anchor.position,
            anchor.rotation,
            currentLevelParent);
    }

    private Transform GetDoorAnchor(Room room, Vector2Int dir)
    {
        if (dir == Vector2Int.up) return room.NorthDoor;
        if (dir == Vector2Int.down) return room.SouthDoor;
        if (dir == Vector2Int.left) return room.WestDoor;
        if (dir == Vector2Int.right) return room.EastDoor;

        return null;
    }

    private GameObject GetPrefab(RoomType type)
    {
        List<RoomPrefabEntry> valid = new List<RoomPrefabEntry>();

        foreach (var entry in RoomPrefabs)
        {
            if (entry.type == type)
                valid.Add(entry);
        }

        if (valid.Count == 0)
        {
            Debug.LogError("No prefab for type: " + type);
            return null;
        }

        float total = 0f;

        foreach (var v in valid)
            total += v.rarity;

        float roll = Random.value * total;

        float current = 0f;

        foreach (var v in valid)
        {
            current += v.rarity;

            if (roll <= current)
                return v.prefab;
        }

        return valid[0].prefab;
    }

    private void GenerateWalls()
    {
        foreach (var node in roomGraph.Values)
        {
            Vector2Int pos = node.GridPosition;

            TryPlaceWall(pos, Vector2Int.up, node);
            TryPlaceWall(pos, Vector2Int.down, node);
            TryPlaceWall(pos, Vector2Int.left, node);
            TryPlaceWall(pos, Vector2Int.right, node);
        }
    }

    private System.Collections.IEnumerator GenerateWallsNextFrame()
    {
        yield return null; // ensures all transforms are valid

        GenerateWalls();

        CameraManager camManager =
        FindFirstObjectByType<CameraManager>();

        if (camManager != null)
        {
            camManager.RefreshRooms();
        }

        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }
        else
        {
            Debug.LogWarning("[LevelManager] No NavMeshSurface assigned — skipping NavMesh bake.");
        }    
    }

    public Vector3 GridToWorldPosition(Vector2Int pos)
    {
        return GridToWorld(pos);
    }

    // ---------------- GIZMOS ----------------

    private void OnDrawGizmos()
    {
        if (roomGraph == null)
            return;

        DrawRooms();
        DrawConnections();
        DrawMainPath();
    }

    private void DrawMainPath()
    {
        if (mainPath == null || mainPath.Count < 2)
            return;

        Gizmos.color = Color.green;

        for (int i = 0; i < mainPath.Count - 1; i++)
        {
            Gizmos.DrawLine(
                GridToWorld(mainPath[i].GridPosition),
                GridToWorld(mainPath[i + 1].GridPosition));
        }
    }

    private void DrawRooms()
    {
        foreach (var node in roomGraph.Values)
        {
            switch (node.RoomType)
            {
                case RoomType.Start:
                    Gizmos.color = Color.green;
                    break;
                case RoomType.Boss:
                    Gizmos.color = Color.red;
                    break;
                default:
                    Gizmos.color = Color.white;
                    break;
            }

            DrawWireSquare(GridToWorld(node.GridPosition), roomScale);
        }
    }

    private void DrawConnections()
    {
        Gizmos.color = Color.cyan;

        HashSet<string> drawn = new HashSet<string>();

        foreach (var a in roomGraph.Values)
        {
            foreach (var b in a.Connections)
            {
                string key = a.GridPosition + "_" + b.GridPosition;
                string rev = b.GridPosition + "_" + a.GridPosition;

                if (drawn.Contains(key) || drawn.Contains(rev))
                    continue;

                drawn.Add(key);

                Gizmos.DrawLine(
                    GridToWorld(a.GridPosition),
                    GridToWorld(b.GridPosition));
            }
        }
    }

    public Vector3 GridToWorld(Vector2Int pos)
    {
        return new Vector3(pos.x * roomScale, 0f, pos.y * roomScale);
    }

    private void DrawWireSquare(Vector3 center, float size)
    {
        float h = size * 0.5f;

        Vector3 a = center + new Vector3(-h, 0, -h);
        Vector3 b = center + new Vector3(-h, 0, h);
        Vector3 c = center + new Vector3(h, 0, h);
        Vector3 d = center + new Vector3(h, 0, -h);

        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }
}

public enum Direction
{
    North,
    South,
    East,
    West
}