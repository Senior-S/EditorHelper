using UnityEngine;

namespace EditorHelper.Models;

public class PositionModel
{
    public float X { get; set; }
    
    public float Y { get; set; }
    
    public float Z { get; set; }

    public PositionModel()
    {
    }

    public PositionModel(Vector3 position)
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