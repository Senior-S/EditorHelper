using UnityEngine;

namespace EditorHelper.LSystem;

public class AgentParameters
{
    public Vector3 position, direction;
    public int length;

    public AgentParameters(Vector3 position, Vector3 direction, int length)
    {
        this.position = position;
        this.direction = direction;
        this.length = length;
    }
}