using System.Collections.Generic;
using EditorHelper2.common.Types.LSystem;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.common.Helpers.LSystem;

public sealed class RoadHelper
{
    private const int Offset = 24;

    private readonly Dictionary<Vector3Int, Transform> _roads = new();
    private readonly Dictionary<Vector3Int, HashSet<RoadDirection>> _implicitNeighbors = new();
    private readonly HashSet<Vector3Int> _pendingRoads = new();
    private readonly ObjectAsset _straight;
    private readonly ObjectAsset _tee;
    private readonly ObjectAsset _quad;
    private readonly ObjectAsset _corner;
    private readonly ObjectAsset _end;

    public List<Transform> PlacedRoads { get; } = new();

    private RoadHelper(ObjectAsset straight, ObjectAsset tee, ObjectAsset quad, ObjectAsset corner, ObjectAsset end)
    {
        _straight = straight;
        _tee = tee;
        _quad = quad;
        _corner = corner;
        _end = end;
    }

    public static RoadHelper? Create()
    {
        return Create(LSystemRoadSettings.Default);
    }

    public static RoadHelper? Create(LSystemRoadSettings settings)
    {
        ObjectAsset? straight = global::SDG.Unturned.Assets.find(settings.StraightRoadGuid) as ObjectAsset;
        ObjectAsset? tee = global::SDG.Unturned.Assets.find(settings.TeeRoadGuid) as ObjectAsset;
        ObjectAsset? quad = global::SDG.Unturned.Assets.find(settings.QuadRoadGuid) as ObjectAsset;
        ObjectAsset? corner = global::SDG.Unturned.Assets.find(settings.CornerRoadGuid) as ObjectAsset;
        ObjectAsset? end = global::SDG.Unturned.Assets.find(settings.EndRoadGuid) as ObjectAsset;

        return straight == null || tee == null || quad == null || corner == null || end == null
            ? null
            : new RoadHelper(straight, tee, quad, corner, end);
    }

    public void PlaceStreetPosition(ref Vector3 startPosition, Vector3 direction, int roadLength)
    {
        Vector3 flatDirection = new(direction.x, 0f, direction.z);
        if (flatDirection.sqrMagnitude <= 0.001f) return;

        Vector3 snappedDirection = SnapCardinal(flatDirection);
        startPosition = PlaceStreetSegment(startPosition, startPosition + snappedDirection * GetSegmentDistance(roadLength));
    }

    public void AddExistingRoad(Transform transform)
    {
        Vector3Int position = Vector3Int.RoundToInt(transform.position);
        if (!_roads.ContainsKey(position)) _roads.Add(position, transform);

        RoadDirection direction = ToRoadDirection(transform.up, transform.forward);
        _implicitNeighbors[position] = new HashSet<RoadDirection> { direction, GetOppositeDirection(direction) };
    }

    public Vector3 PlaceStreetSegment(Vector3 startPosition, Vector3 endPosition)
    {
        Vector3 delta = endPosition - startPosition;
        delta.y = 0f;
        if (delta.sqrMagnitude <= 0.001f) return startPosition;

        Vector3 direction = SnapCardinal(delta);
        float roadSpacing = GetRoadSpacing();
        int roadLength = Mathf.Max(1, Mathf.RoundToInt(delta.magnitude / roadSpacing));
        Vector3Int lastPosition = Vector3Int.RoundToInt(startPosition);

        for (int i = 0; i <= roadLength; i++)
        {
            Vector3 roadPosition = startPosition + direction * roadSpacing * i;
            Vector3Int position = Vector3Int.RoundToInt(roadPosition);
            lastPosition = position;

            if (_roads.ContainsKey(position)) continue;
            _pendingRoads.Add(position);
        }

        return lastPosition;
    }

    public float GetSegmentDistance(int roadLength)
    {
        return GetRoadSpacing() * GetRoadPieceCount(roadLength);
    }

    public void FixRoad()
    {
        foreach (Vector3Int position in _pendingRoads)
        {
            List<RoadDirection> neighborDirections = FindNeighbors(position);

            if (neighborDirections.Count == 1)
            {
                PlaceFinalRoad(position, _end, RotationFromDirection(neighborDirections[0]));
            }
            else if (neighborDirections.Count == 2)
            {
                if (neighborDirections.Contains(RoadDirection.Up) && neighborDirections.Contains(RoadDirection.Down)
                    || neighborDirections.Contains(RoadDirection.Right) && neighborDirections.Contains(RoadDirection.Left))
                {
                    Vector3 direction = neighborDirections.Contains(RoadDirection.Right) ? Vector3.right : Vector3.forward;
                    PlaceFinalRoad(position, _straight, Quaternion.LookRotation(direction) * Quaternion.Euler(-90f, 0f, 0f));
                    continue;
                }

                PlaceFinalRoad(position, _corner, GetCornerRotation(neighborDirections));
            }
            else if (neighborDirections.Count == 3)
            {
                Quaternion rotation = Quaternion.Euler(-90f, 0f, -90f);
                if (neighborDirections.Contains(RoadDirection.Right)
                    && neighborDirections.Contains(RoadDirection.Down)
                    && neighborDirections.Contains(RoadDirection.Left))
                {
                    rotation = Quaternion.Euler(-90f, 0f, 0f);
                }
                else if (neighborDirections.Contains(RoadDirection.Down)
                         && neighborDirections.Contains(RoadDirection.Left)
                         && neighborDirections.Contains(RoadDirection.Up))
                {
                    rotation = Quaternion.Euler(-90f, 0f, -270f);
                }
                else if (neighborDirections.Contains(RoadDirection.Left)
                         && neighborDirections.Contains(RoadDirection.Up)
                         && neighborDirections.Contains(RoadDirection.Right))
                {
                    rotation = Quaternion.Euler(-90f, 0f, -180f);
                }

                PlaceFinalRoad(position, _tee, rotation);
            }
            else if (neighborDirections.Count == 4)
            {
                PlaceFinalRoad(position, _quad, Quaternion.Euler(-90f, 0f, 0f));
            }
            else
            {
                PlaceFinalRoad(position, _straight, Quaternion.Euler(-90f, 0f, 0f));
            }
        }

        _pendingRoads.Clear();
    }

    private void PlaceFinalRoad(Vector3Int position, ObjectAsset asset, Quaternion rotation)
    {
        Transform transform = PlaceObject(asset, position, rotation);
        if (transform == null) return;

        _roads[position] = transform;
        PlacedRoads.Add(transform);
    }

    private float GetRoadSpacing()
    {
        return Offset;
    }

    private List<RoadDirection> FindNeighbors(Vector3Int position)
    {
        List<RoadDirection> neighborDirections = new();
        if (HasRoadAt(position + Vector3Int.right * Offset)) neighborDirections.Add(RoadDirection.Right);
        if (HasRoadAt(position - Vector3Int.right * Offset)) neighborDirections.Add(RoadDirection.Left);
        if (HasRoadAt(position + new Vector3Int(0, 0, 1) * Offset)) neighborDirections.Add(RoadDirection.Up);
        if (HasRoadAt(position - new Vector3Int(0, 0, 1) * Offset)) neighborDirections.Add(RoadDirection.Down);
        if (_implicitNeighbors.TryGetValue(position, out HashSet<RoadDirection>? implicitDirections))
        {
            foreach (RoadDirection direction in implicitDirections)
            {
                if (!neighborDirections.Contains(direction)) neighborDirections.Add(direction);
            }
        }

        return neighborDirections;
    }

    private bool HasRoadAt(Vector3Int position)
    {
        if (_roads.ContainsKey(position)) return true;
        if (_pendingRoads.Contains(position)) return true;

        // ponytail: O(n) fuzzy lookup is fine for editor-sized generated roads; spatial hash if this gets huge.
        foreach (Vector3Int roadPosition in _roads.Keys)
        {
            int x = roadPosition.x - position.x;
            int z = roadPosition.z - position.z;
            if (x * x + z * z <= 4) return true;
        }

        foreach (Vector3Int roadPosition in _pendingRoads)
        {
            int x = roadPosition.x - position.x;
            int z = roadPosition.z - position.z;
            if (x * x + z * z <= 4) return true;
        }

        return false;
    }

    private static Vector3 SnapCardinal(Vector3 direction)
    {
        Vector3 flatDirection = new(direction.x, 0f, direction.z);
        if (flatDirection.sqrMagnitude <= 0.001f) return Vector3.forward;

        return Mathf.Abs(flatDirection.x) > Mathf.Abs(flatDirection.z)
            ? flatDirection.x >= 0f ? Vector3.right : Vector3.left
            : flatDirection.z >= 0f ? Vector3.forward : Vector3.back;
    }

    private static RoadDirection ToRoadDirection(Vector3 direction, Vector3 fallback)
    {
        Vector3 flatDirection = new(direction.x, 0f, direction.z);
        if (flatDirection.sqrMagnitude <= 0.001f) flatDirection = new Vector3(fallback.x, 0f, fallback.z);

        return Mathf.Abs(flatDirection.x) > Mathf.Abs(flatDirection.z)
            ? flatDirection.x >= 0f ? RoadDirection.Right : RoadDirection.Left
            : flatDirection.z >= 0f ? RoadDirection.Up : RoadDirection.Down;
    }

    private static RoadDirection GetOppositeDirection(RoadDirection direction)
    {
        return direction switch
        {
            RoadDirection.Up => RoadDirection.Down,
            RoadDirection.Down => RoadDirection.Up,
            RoadDirection.Right => RoadDirection.Left,
            _ => RoadDirection.Right
        };
    }

    private static int GetRoadPieceCount(int roadLength)
    {
        return Mathf.Max(1, roadLength);
    }

    private static Quaternion GetCornerRotation(List<RoadDirection> directions)
    {
        float z = 0f;
        if (directions.Contains(RoadDirection.Up) && directions.Contains(RoadDirection.Right))
        {
            z = 90f;
        }
        else if (directions.Contains(RoadDirection.Down) && directions.Contains(RoadDirection.Right))
        {
            z = 180f;
        }
        else if (directions.Contains(RoadDirection.Down) && directions.Contains(RoadDirection.Left))
        {
            z = 270f;
        }

        return Quaternion.Euler(-90f, 0f, z);
    }

    private static Quaternion RotationFromDirection(RoadDirection direction)
    {
        Vector3 forward = direction switch
        {
            RoadDirection.Down => Vector3.back,
            RoadDirection.Left => Vector3.left,
            RoadDirection.Right => Vector3.right,
            _ => Vector3.forward
        };

        return Quaternion.LookRotation(forward) * Quaternion.Euler(-90f, 0f, 0f);
    }

    private static Transform PlaceObject(ObjectAsset objectAsset, Vector3Int position, Quaternion rotation)
    {
        EditorObjects.selectedObjectAsset = objectAsset;
        EditorObjects.selectedItemAsset = null;
        return LevelObjects.registerAddObject(position, rotation, Vector3.one, EditorObjects.selectedObjectAsset, EditorObjects.selectedItemAsset);
    }
}