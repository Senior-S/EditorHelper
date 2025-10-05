using UnityEngine;

namespace EditorHelper2.common.Types;

public class RoadSelection
{
    public Transform Transform { get; private set; }

    public Vector3 FromPosition;

    public Matrix4x4 RelativeToPivot;

    public Vector3[] Tangents;

    public RoadSelection(Transform roadTransform, Vector3[] tangents)
    {
        Transform = roadTransform;
        FromPosition = roadTransform.position;
        this.Tangents = tangents;
    }
}