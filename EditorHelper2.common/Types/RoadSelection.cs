using UnityEngine;

namespace EditorHelper2.common.Types;

public class RoadSelection
{
    public Transform Transform { get; }

    public Vector3 FromPosition;

    public Matrix4x4 RelativeToPivot;

    public Vector3[] FromTangents;

    public RoadSelection(Transform roadTransform, Vector3[] tangents)
    {
        Transform = roadTransform;
        FromPosition = roadTransform.position;
        FromTangents = tangents;
    }
}