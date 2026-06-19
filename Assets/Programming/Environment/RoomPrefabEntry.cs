using UnityEngine;

[System.Serializable]
public class RoomPrefabEntry
{
    public RoomType type;
    public GameObject prefab;

    [Range(0f, 1f)]
    public float rarity = 1f;
}
