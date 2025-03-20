using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Models;

public class ReunRoadTransform : IReun
{
    // Not used for roads
    private Transform _model;

    private readonly int _vertexIndex = -1;
    private readonly int _tangentIndex = -1;

    private readonly Vector3 _fromPosition;
    private readonly Vector3 _toPosition;
    
    public int step { get; private set; }
    
    public Transform redo()
    {
        if (_tangentIndex > -1)
        {
            EditorRoads.road.moveTangent(_vertexIndex, _tangentIndex, _toPosition - EditorRoads.joint.vertex);
        }
        else if (_vertexIndex > -1)
        {
            EditorRoads.road.moveVertex(_vertexIndex, _toPosition);
        }
        
        return _model;
    }

    public void undo()
    {
        if (_tangentIndex > -1)
        {
            EditorRoads.road.moveTangent(_vertexIndex, _tangentIndex, _fromPosition - EditorRoads.joint.vertex);
        }
        else if (_vertexIndex > -1)
        {
            EditorRoads.road.moveVertex(_vertexIndex, _fromPosition);
        }
    }

    public ReunRoadTransform(int newStep, Vector3 newFromPosition, Vector3 newToPosition, int vertexIndex = -1, int tangentIndex = -1)
    {
        step = newStep;
        _fromPosition = newFromPosition;
        _toPosition = newToPosition;
        _vertexIndex = vertexIndex;
        _tangentIndex = tangentIndex;
    }
}