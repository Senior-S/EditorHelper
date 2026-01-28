using SDG.Unturned;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace EditorHelper2.common.Types;

[BurstCompile]
public struct CreateRaycastsJob : IJobParallelFor
{
    public NativeArray<RaycastCommand> Commands;

    public float ImageWidth;
    public float ImageHeight;

    public float CaptureWidth;
    public float CaptureHeight;

    public float X;

    public float4x4 LocalToWorldMatrix;

    public void Execute(int y)
    {
        Commands[4 * y + 0] = CreateCommand((float)X + 0.25f, (float)y + 0.25f);
        Commands[4 * y + 1] = CreateCommand((float)X + 0.25f, (float)y + 0.75f);
        Commands[4 * y + 2] = CreateCommand((float)X + 0.75f, (float)y + 0.25f);
        Commands[4 * y + 3] = CreateCommand((float)X + 0.75f, (float)y + 0.75f);
    }

    private readonly RaycastCommand CreateCommand(float x, float y)
    {
        float widthOffset = x / ImageWidth;
        float heightOffset = y / ImageHeight;
        float3 position = new((widthOffset - 0.5f) * CaptureWidth, (heightOffset - 0.5f) * CaptureHeight, 0f);
        float3 origin = math.transform(LocalToWorldMatrix, position);  

        return new RaycastCommand(origin, math.down(), new QueryParameters(RayMasks.CHART), Level.HEIGHT);
    }
}
