using UnityEngine;

namespace EditorHelper2.common.Types;
public struct MapResolution(uint width, uint height)
{
    /// <summary>
    /// Value of 0 is <see cref="null"/> as it's not a valid dimension
    /// </summary>
    public uint Width = width, Height = height;

    public readonly uint GetAspectWidth(MapResolution aspectResolution) => (uint)Mathf.CeilToInt(Height * ((float)aspectResolution.Width / (float)aspectResolution.Height));
    public readonly uint GetAspectHeight(MapResolution aspectResolution) => (uint)Mathf.CeilToInt(Width * ((float)aspectResolution.Height / (float)aspectResolution.Width));
}
