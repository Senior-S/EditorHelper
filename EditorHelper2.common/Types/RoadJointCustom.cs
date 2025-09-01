using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.common.Types;

public record struct RoadJointCustom
{
    public Road Road;
    public int Index;
    public Vector3 Vertex;

    public Vector3[] TangentPositions;
}