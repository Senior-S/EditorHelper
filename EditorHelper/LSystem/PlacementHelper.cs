using System.Collections.Generic;
using UnityEngine;

namespace EditorHelper.LSystem;

public static class PlacementHelper
{
    public static List<Direction> FindNeighbor(Vector3Int position, ICollection<Vector3Int> collection, int offset)
    {
        List<Direction> neighborDirections = new();
        if (collection.Contains(position + Vector3Int.right * offset))
        {
            neighborDirections.Add(Direction.Right);
        }
        if (collection.Contains(position - Vector3Int.right * offset))
        {
            neighborDirections.Add(Direction.Left);
        }
        if (collection.Contains(position + new Vector3Int(0, 0, 1) * offset))
        {
            neighborDirections.Add(Direction.Up);
        }
        if (collection.Contains(position - new Vector3Int(0, 0, 1) * offset))
        {
            neighborDirections.Add(Direction.Down);
        }

        return neighborDirections;
    }
}