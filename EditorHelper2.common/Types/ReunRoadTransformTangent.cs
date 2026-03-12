using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.common.Types;

public class ReunRoadTransformTangent(int newStep, Transform handle, Vector3 fromPosition, Vector3 toPosition) : IReun
{
    private readonly Transform _handle = handle;

    private readonly Vector3 _fromPosition = fromPosition;
    private readonly Vector3 _toPosition = toPosition;

    public int step { get; } = newStep;

    public Transform? redo()
    {
        if (_handle == null) return null;

        Road? road = LevelRoads.getRoad(_handle, out int vertexIndex, out int tangentIndex);
        if (road == null) return null;

        road.moveTangent(vertexIndex, tangentIndex, _toPosition);

        return null;
    }

    public void undo()
    {
        if (_handle == null) return;

        Road? road = LevelRoads.getRoad(_handle, out int vertexIndex, out int tangentIndex);
        if (road == null) return;

        road.moveTangent(vertexIndex, tangentIndex, _fromPosition);
    }
}