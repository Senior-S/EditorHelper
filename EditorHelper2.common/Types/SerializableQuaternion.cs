using UnityEngine;

namespace EditorHelper2.common.Types;

public class SerializableQuaternion
{
    public float X { get; set; }
    
    public float Y { get; set; }
    
    public float Z { get; set; }
    
    public float W { get; set; }

    public SerializableQuaternion()
    {
    }

    public SerializableQuaternion(Quaternion rotation)
    {
        X = rotation.x;
        Y = rotation.y;
        Z = rotation.z;
        W = rotation.w;
    }

    public Quaternion ToQuaternion()
    {
        return new Quaternion(X, Y, Z, W);
    }
}