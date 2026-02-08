using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.common.Helpers;

public static class SingleplayerSharedClass
{
    public static Vector3 CameraPosition = Vector3.zero;
    public static float CameraRotation = 0f;

    public static LevelInfo? LevelInfo = null;
}