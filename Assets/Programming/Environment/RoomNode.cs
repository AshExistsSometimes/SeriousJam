using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomNode
{
    public Vector2Int GridPosition;

    public RoomType RoomType;

    public List<RoomNode> Connections = new List<RoomNode>();
}
