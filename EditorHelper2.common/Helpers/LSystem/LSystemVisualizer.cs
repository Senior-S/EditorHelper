using System.Collections.Generic;
using EditorHelper2.common.Types.LSystem;
using SDG.Unturned;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EditorHelper2.common.Helpers.LSystem;

public sealed class LSystemVisualizer
{
    private const float Angle = 90f;
    private const int MaxGeneratedRoadPieces = 450;

    private readonly LSystemGenerator? _lsystem;
    private readonly RoadHelper _roadHelper;
    private readonly LevelObject _startObject;
    private readonly LSystemRoadSettings? _settings;
    private int _length = 8;

    public LSystemVisualizer(LSystemGenerator lsystem, RoadHelper roadHelper, LevelObject startObject)
    {
        _lsystem = lsystem;
        _roadHelper = roadHelper;
        _startObject = startObject;
    }

    public LSystemVisualizer(RoadHelper roadHelper, LevelObject startObject, LSystemRoadSettings settings)
    {
        _roadHelper = roadHelper;
        _startObject = startObject;
        _settings = settings;
    }

    private int Length
    {
        get => _length > 0 ? _length : 1;
        set => _length = value;
    }

    public void Start()
    {
        _roadHelper.AddExistingRoad(_startObject.transform);

        if (_lsystem == null)
        {
            GrowRoads();
            return;
        }

        VisualizeSequence(_lsystem.GenerateSentence());
    }

    private void GrowRoads()
    {
        LSystemRoadSettings settings = _settings ?? LSystemRoadSettings.Default;
        int segmentLimit = Mathf.Max(0, settings.SegmentLimit);
        if (segmentLimit == 0) return;

        Vector3 startPosition = _startObject.transform.position;
        float roadSpacing = _roadHelper.GetSegmentDistance(1);
        float radius = settings.MaxRadiusFromStart > 0f ? settings.MaxRadiusFromStart : float.PositiveInfinity;
        Vector3 startDirection = FlattenDirection(_startObject.transform.up, _startObject.transform.forward);
        Queue<AgentParameters> agents = new();
        HashSet<Vector2Int> occupied = new() { Vector2Int.zero };
        int maxRoadPieces = Mathf.Min(MaxGeneratedRoadPieces, Mathf.Max(GetInitialLength(settings), segmentLimit * GetInitialLength(settings)));

        agents.Enqueue(new AgentParameters(startPosition, startDirection, GetInitialLength(settings)));
        agents.Enqueue(new AgentParameters(startPosition, -startDirection, GetInitialLength(settings)));

        int acceptedSegments = 0;
        while (agents.Count > 0 && acceptedSegments < segmentLimit && occupied.Count < maxRoadPieces)
        {
            AgentParameters agent = agents.Dequeue();
            Vector2Int startGrid = ToGrid(agent.Position, startPosition, roadSpacing);
            Vector3 direction = FlattenDirection(agent.Direction, startDirection);
            int maxLength = Mathf.Max(GetMinLength(settings), agent.Length);

            if (!TryPickSegment(startGrid, direction, maxLength, startPosition, roadSpacing, radius, occupied, settings,
                    out Vector2Int endGrid, out Vector3 segmentDirection))
            {
                continue;
            }

            int newRoadPieces = CountNewCells(startGrid, endGrid, occupied);

            if (newRoadPieces == 0 || occupied.Count + newRoadPieces > maxRoadPieces)
            {
                continue;
            }

            Vector3 segmentStart = ToWorld(startGrid, startPosition, roadSpacing);
            Vector3 segmentEnd = ToWorld(endGrid, startPosition, roadSpacing);
            _roadHelper.PlaceStreetSegment(segmentStart, segmentEnd);
            MarkOccupied(startGrid, endGrid, occupied);
            acceptedSegments++;

            float distanceRatio = radius < float.PositiveInfinity
                ? Mathf.Clamp01(HorizontalDistance(startPosition, segmentEnd) / radius)
                : 0f;
            int nextLength = Mathf.Max(GetMinLength(settings), maxLength - (Random.value < 0.45f ? 1 : 0));
            if (Random.value > distanceRatio * 0.65f)
            {
                agents.Enqueue(new AgentParameters(segmentEnd, Random.value < settings.TurnChance ? TurnRandom(segmentDirection) : segmentDirection, nextLength));
            }

            float branchChance = Mathf.Clamp01(settings.BranchChance * (1f - distanceRatio * 0.75f));
            if (Random.value < branchChance)
            {
                agents.Enqueue(new AgentParameters(segmentEnd, TurnRandom(segmentDirection), nextLength));
            }
        }

        _roadHelper.FixRoad();
    }

    private static bool TryPickSegment(
        Vector2Int startGrid,
        Vector3 direction,
        int maxLength,
        Vector3 origin,
        float roadSpacing,
        float radius,
        HashSet<Vector2Int> occupied,
        LSystemRoadSettings settings,
        out Vector2Int endGrid,
        out Vector3 pickedDirection)
    {
        Vector3[] candidates = BuildCandidates(direction, settings);
        int minLength = GetMinLength(settings);

        foreach (Vector3 candidate in candidates)
        {
            int length = Random.Range(minLength, maxLength + 1);
            Vector2Int candidateEnd = startGrid + ToGridDirection(candidate) * length;
            if (HorizontalDistance(origin, ToWorld(candidateEnd, origin, roadSpacing)) > radius) continue;
            if (RetracesTooMuch(startGrid, candidateEnd, occupied)) continue;

            endGrid = candidateEnd;
            pickedDirection = candidate;
            return true;
        }

        endGrid = startGrid;
        pickedDirection = direction;
        return false;
    }

    private static Vector3[] BuildCandidates(Vector3 direction, LSystemRoadSettings settings)
    {
        Vector3 left = Rotate(direction, -Angle);
        Vector3 right = Rotate(direction, Angle);
        bool turnFirst = Random.value < Mathf.Clamp01(settings.TurnChance);

        if (Random.value < 0.5f)
        {
            (left, right) = (right, left);
        }

        return turnFirst
            ? new[] { left, direction, right }
            : new[] { direction, left, right };
    }

    private void VisualizeSequence(string sequence)
    {
        Stack<AgentParameters> savePoints = new();
        Vector3 currentPosition = _startObject.transform.position;
        Vector3 direction = _startObject.transform.up;

        foreach (char c in sequence)
        {
            switch ((EncodingLetter)c)
            {
                case EncodingLetter.Save:
                    savePoints.Push(new AgentParameters(currentPosition, direction, Length));
                    break;
                case EncodingLetter.Load:
                    if (savePoints.Count < 1) break;
                    AgentParameters agentParameters = savePoints.Pop();
                    currentPosition = agentParameters.Position;
                    direction = agentParameters.Direction;
                    Length = agentParameters.Length;
                    break;
                case EncodingLetter.Draw:
                    _roadHelper.PlaceStreetPosition(ref currentPosition, direction, Length);
                    Length -= 2;
                    break;
                case EncodingLetter.TurnRight:
                    direction = Quaternion.AngleAxis(Angle, Vector3.up) * direction;
                    break;
                case EncodingLetter.TurnLeft:
                    direction = Quaternion.AngleAxis(-Angle, Vector3.up) * direction;
                    break;
            }
        }

        _roadHelper.FixRoad();
    }

    private static int GetInitialLength(LSystemRoadSettings settings)
    {
        return Mathf.Max(GetMinLength(settings), settings.InitialLength);
    }

    private static int GetMinLength(LSystemRoadSettings settings)
    {
        return Mathf.Max(1, settings.MinLength);
    }

    private static Vector3 FlattenDirection(Vector3 direction, Vector3 fallback)
    {
        Vector3 flatDirection = new(direction.x, 0f, direction.z);
        if (flatDirection.sqrMagnitude > 0.001f) return SnapCardinal(flatDirection);

        Vector3 flatFallback = new(fallback.x, 0f, fallback.z);
        return flatFallback.sqrMagnitude > 0.001f ? SnapCardinal(flatFallback) : Vector3.forward;
    }

    private static Vector3 SnapCardinal(Vector3 direction)
    {
        return Mathf.Abs(direction.x) > Mathf.Abs(direction.z)
            ? direction.x >= 0f ? Vector3.right : Vector3.left
            : direction.z >= 0f ? Vector3.forward : Vector3.back;
    }

    private static Vector3 Rotate(Vector3 direction, float angle)
    {
        return SnapCardinal(Quaternion.AngleAxis(angle, Vector3.up) * direction);
    }

    private static Vector3 TurnRandom(Vector3 direction)
    {
        return Rotate(direction, Random.value < 0.5f ? -Angle : Angle);
    }

    private static Vector2Int ToGrid(Vector3 position, Vector3 origin, float roadSpacing)
    {
        Vector3 offset = position - origin;
        return new Vector2Int(Mathf.RoundToInt(offset.x / roadSpacing), Mathf.RoundToInt(offset.z / roadSpacing));
    }

    private static Vector3 ToWorld(Vector2Int grid, Vector3 origin, float roadSpacing)
    {
        return origin + new Vector3(grid.x * roadSpacing, 0f, grid.y * roadSpacing);
    }

    private static Vector2Int ToGridDirection(Vector3 direction)
    {
        Vector3 snapped = SnapCardinal(direction);
        return Mathf.Abs(snapped.x) > Mathf.Abs(snapped.z)
            ? new Vector2Int(snapped.x > 0f ? 1 : -1, 0)
            : new Vector2Int(0, snapped.z > 0f ? 1 : -1);
    }

    private static bool RetracesTooMuch(Vector2Int start, Vector2Int end, HashSet<Vector2Int> occupied)
    {
        Vector2Int step = new(Mathf.Clamp(end.x - start.x, -1, 1), Mathf.Clamp(end.y - start.y, -1, 1));
        int length = Mathf.Abs(end.x - start.x) + Mathf.Abs(end.y - start.y);
        int occupiedCount = 0;

        for (int i = 1; i <= length; i++)
        {
            if (occupied.Contains(start + step * i)) occupiedCount++;
        }

        return occupiedCount > Mathf.Max(1, length / 3);
    }

    private static int CountNewCells(Vector2Int start, Vector2Int end, HashSet<Vector2Int> occupied)
    {
        Vector2Int step = new(Mathf.Clamp(end.x - start.x, -1, 1), Mathf.Clamp(end.y - start.y, -1, 1));
        int length = Mathf.Abs(end.x - start.x) + Mathf.Abs(end.y - start.y);
        int count = 0;

        for (int i = 1; i <= length; i++)
        {
            if (!occupied.Contains(start + step * i)) count++;
        }

        return count;
    }

    private static void MarkOccupied(Vector2Int start, Vector2Int end, HashSet<Vector2Int> occupied)
    {
        Vector2Int step = new(Mathf.Clamp(end.x - start.x, -1, 1), Mathf.Clamp(end.y - start.y, -1, 1));
        int length = Mathf.Abs(end.x - start.x) + Mathf.Abs(end.y - start.y);

        for (int i = 0; i <= length; i++)
        {
            occupied.Add(start + step * i);
        }
    }

    private static float HorizontalDistance(Vector3 first, Vector3 second)
    {
        float x = first.x - second.x;
        float z = first.z - second.z;
        return Mathf.Sqrt(x * x + z * z);
    }
}