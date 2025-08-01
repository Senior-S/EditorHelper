using UnityEngine;

namespace EditorHelper.Models;

public class RoadSelection
{
    private Transform _transform;

    public Vector3 fromPosition;
    
    public Matrix4x4 relativeToPivot;

    public Vector3[] tangents;

    public Transform transform => _transform;

    public RoadSelection(Transform roadTransform, Vector3[] tangents)
    {
        _transform = roadTransform;
        fromPosition = roadTransform.position;
        this.tangents = tangents;
    }
}