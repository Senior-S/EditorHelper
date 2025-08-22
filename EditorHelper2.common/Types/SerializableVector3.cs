using Newtonsoft.Json;
using UnityEngine;

namespace EditorHelper2.common.Types;

public class SerializableVector3
{
    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public SerializableVector3()
    {
    }

    public SerializableVector3(Vector3 position)
    {
        X = position.x;
        Y = position.y;
        Z = position.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(X, Y, Z);
    }
}