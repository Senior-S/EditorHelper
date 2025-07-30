using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Models;
public record struct RoadJointCustom
{
    public Road road;
    public int index;
    public Vector3 vertex;
}