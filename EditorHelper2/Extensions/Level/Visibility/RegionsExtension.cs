using System.IO;
using System.Reflection;
using DanielWillett.UITools.API.Extensions;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Level.Visibility;

[EHExtension("Regions Extension", "JienSultan & Gamingtoday093")]
[UIExtension(typeof(EditorLevelVisibilityUI))]
public class RegionsExtension : UIExtension, IExtension
{
    /// <summary>
    /// Size of the area of Regions shown with a RegionBorder. Vanilla uses 7x7 for Region Labels but 1x1 looks better for RegionBorders
    /// </summary>
    private const int DebugSize = 1;

    private readonly Transform?[] _regionBorders;
    private readonly AssetBundle? _regionBorderBundle;
    private readonly GameObject? _regionBorderPrefab;
    
    public RegionsExtension()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string bundlePath = Path.Combine(Path.GetDirectoryName(assembly.Location) ?? string.Empty, "Assets" ,"RegionBorder.unity3d");

        _regionBorders = new Transform[DebugSize * DebugSize];

        _regionBorderBundle = AssetBundle.LoadFromFile(bundlePath);
        if (_regionBorderBundle == null)
        {
            UnturnedLog.info("[Regions Extension] Failed to load region border bundle");
            return;
        }

        _regionBorderPrefab = _regionBorderBundle.LoadAsset<GameObject>("RegionBorder");
        if (_regionBorderPrefab == null)
        {
            UnturnedLog.info("[Regions Extension] Failed to load RegionBorder prefab from bundle");
            return;
        }

        Initialize();
    }

    public void Initialize()
    {
        if (_regionBorderPrefab == null) return;

        const float REGION_HEIGHT = 1024f / 4f; // Doesn't seem to change anything, ask Sultan

        for (int i = 0; i < _regionBorders.Length; i++)
        {
            Transform regionBorder = Object.Instantiate(_regionBorderPrefab).transform;
            regionBorder.name = "EditorHelper:RegionBorder";
            regionBorder.localScale = new Vector3(Regions.REGION_SIZE, REGION_HEIGHT, Regions.REGION_SIZE);
            regionBorder.gameObject.SetActive(false);

            _regionBorders[i] = regionBorder;
        }
    }

    internal void CustomUpdateRegion(int cameraRegionX, int cameraRegionY)
    {
        if (_regionBorderPrefab == null || DebugSize < 1) return;

        // Loop through the grid around the camera's current region
        for (int i = -DebugSize / 2; i <= DebugSize / 2; i++)
        {
            for (int j = -DebugSize / 2; j <= DebugSize / 2; j++)
            {
                byte x = (byte)(cameraRegionX + i);
                byte y = (byte)(cameraRegionY + j);

                // Make sure we are within valid region bounds
                if (x < Regions.WORLD_SIZE && y < Regions.WORLD_SIZE)
                {
                    Vector3 regionPosition = new(
                        (x * Regions.REGION_SIZE) + (Regions.REGION_SIZE / 2) - (Regions.WORLD_SIZE * (Regions.REGION_SIZE / 2)),
                        0,
                        (y * Regions.REGION_SIZE) + (Regions.REGION_SIZE / 2) - (Regions.WORLD_SIZE * (Regions.REGION_SIZE / 2))
                    );

                    Transform? regionBorder = _regionBorders[((i + (DebugSize / 2)) * DebugSize) + j + (DebugSize / 2)];
                    if (regionBorder == null)
                    {
                        UnturnedLog.info($"RegionsExtension: {nameof(regionBorder)} is somehow null? This should never happen");
                        // This isn't a continue because this should be treated as an NullReferenceException
                        // It would also spam the Logs if the entire _regionBorders is null
                        return;
                    }

                    regionBorder.position = regionPosition;
                    regionBorder.gameObject.SetActive(true);
                }
            }
        }
    }

    protected override void Closed()
    {
        foreach (Transform? regionBorder in _regionBorders)
        {
            if (regionBorder == null) continue;
            regionBorder.gameObject.SetActive(false);
        }
    }

    public void Dispose()
    {
        foreach (Transform? regionBorder in _regionBorders)
        {
            if (regionBorder == null) continue;
            Object.Destroy(regionBorder.gameObject);
        }

        _regionBorderBundle?.Unload(true);
    }
}