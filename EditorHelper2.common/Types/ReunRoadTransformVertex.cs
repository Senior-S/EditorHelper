using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.common.Types;

public class ReunRoadTransformVertex(int newStep, Transform vertex, Vector3 fromPosition, Vector3 toPosition, Vector3[]? fromTangents, Vector3[]? toTangents) : IReun
{
    private readonly Transform _vertex = vertex;

    private readonly Vector3 _fromPosition = fromPosition;
    private readonly Vector3 _toPosition = toPosition;

    private readonly Vector3[]? _fromTangents = fromTangents;
    private readonly Vector3[]? _toTangents = toTangents;

    public int step { get; } = newStep;

    public Transform? redo()
    {
        if (_vertex == null) return null;

        Road? road = LevelRoads.getRoad(_vertex, out int vertexIndex, out int tangentIndex);
        if (road == null) return null;

        road.moveVertex(vertexIndex, _toPosition);

        if (_toTangents != null)
        {
            for (int i = 0; i < _toTangents.Length; i++)
                road.moveTangent(vertexIndex, i, _toTangents[i]);
        }

        return null;
    }

    public void undo()
    {
        if (_vertex == null) return;

        Road? road = LevelRoads.getRoad(_vertex, out int vertexIndex, out int tangentIndex);
        if (road == null) return;

        road.moveVertex(vertexIndex, _fromPosition);

        if (_fromTangents != null)
        {
            for (int i = 0; i < _fromTangents.Length; i++)
                road.moveTangent(vertexIndex, i, _fromTangents[i]);
        }
    }
}
