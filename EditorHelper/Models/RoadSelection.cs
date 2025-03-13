using UnityEngine;

namespace EditorHelper.Models;

public class RoadSelection
{
    private Transform _transform;

    public Vector3 fromPosition;

    public Quaternion fromRotation;

    public Vector3 fromScale;

    public Matrix4x4 relativeToPivot;

    public Transform transform => _transform;

    public RoadSelection(Transform roadTransform)
    {
        _transform = roadTransform;
        fromPosition = roadTransform.position;
        fromRotation = roadTransform.rotation;
        fromScale = roadTransform.localScale;
    }
}