using UnityEngine;

namespace EditorHelper.Models;

public class CameraPosition
{
    public Vector3 Position;

    public Quaternion Rotation;

    public CameraPosition(Vector3 position, Quaternion rotation)
    {
        Position = position;
        Rotation = rotation;
    }
}