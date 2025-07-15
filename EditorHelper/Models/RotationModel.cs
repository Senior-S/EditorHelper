using UnityEngine;

namespace EditorHelper.Models;

public class RotationModel
{
    public float X { get; set; }
    
    public float Y { get; set; }
    
    public float Z { get; set; }
    
    public float W { get; set; }

    public RotationModel()
    {
    }

    public RotationModel(Quaternion rotation)
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