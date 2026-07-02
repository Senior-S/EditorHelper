using UnityEngine;

namespace EditorHelper2.common.Types.LSystem;

public readonly struct AgentParameters
{
    public readonly Vector3 Position;
    public readonly Vector3 Direction;
    public readonly int Length;

    public AgentParameters(Vector3 position, Vector3 direction, int length)
    {
        Position = position;
        Direction = direction;
        Length = length;
    }
}