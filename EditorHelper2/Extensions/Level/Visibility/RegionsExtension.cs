using System.IO;
using System.Reflection;
using DanielWillett.UITools.API.Extensions;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Level.Visibility;

[EHExtension("Regions Extension", "JienSultan")]
[UIExtension(typeof(EditorLevelVisibilityUI))]
public class RegionsExtension : UIExtension, IExtension
{
    private const int DebugSize = 1;
    private Camera? _mainCamera;
    private readonly Transform? _regionBordersParent;
    private readonly AssetBundle? _regionBorderBundle;
    private readonly GameObject? _regionBorderPrefab;
    
    public RegionsExtension()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string bundlePath = Path.Combine(Path.GetDirectoryName(assembly.Location) ?? string.Empty, "Assets" ,"RegionBorder.unity3d"); 

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

        _regionBordersParent = new GameObject("RegionBorders").transform; // Create a parent for clips
        
        Initialize();
    }

    public void Initialize()
    {
        _mainCamera = Camera.main;
    }

    #region Extension Functions
    internal void CustomUpdate()
    {
        if (_mainCamera == null) return;

        float regionSize = Regions.REGION_SIZE;
        Vector3 cameraPosition = _mainCamera.transform.position;

        // Get the current region of the camera
        if (!Regions.tryGetCoordinate(cameraPosition, out byte cameraRegionX, out byte cameraRegionY))
            return;

        // Remove previous clips to prevent duplicates
        foreach (Transform child in _regionBordersParent!)
            Object.Destroy(child.gameObject);

        // Loop through the 7x7 grid around the camera's current region
        for (int i = -DebugSize / 2; i <= DebugSize / 2; i++)
        {
            for (int j = -DebugSize / 2; j <= DebugSize / 2; j++)
            {
                byte x = (byte)(cameraRegionX + i);
                byte y = (byte)(cameraRegionY + j);

                // Make sure we are within valid region bounds
                if (x < Regions.WORLD_SIZE && y < Regions.WORLD_SIZE)
                {
                    Vector3 regionPosition = new Vector3(
                        (x * regionSize) + (regionSize / 2) - (Regions.WORLD_SIZE * (regionSize / 2)),
                        0,
                        (y * regionSize) + (regionSize / 2) - (Regions.WORLD_SIZE * (regionSize / 2))
                    );

                    // Create and configure the world border
                    RegionBorders(regionPosition, regionSize);
                }
            }
        }
    }
    
    private void RegionBorders(Vector3 position, float size)
    {
        float height = 1024;

        if (_regionBorderPrefab != null)
        {
            Transform wall = (Object.Instantiate(_regionBorderPrefab)).transform;
            wall.position = position;
            wall.localScale = new Vector3(size, height / 4f, size);
            wall.name = "RegionBorder";
            wall.parent = _regionBordersParent;
        }
    }

    #endregion Extension Functions

    public void Dispose()
    {
        for (int i = 0; i < _regionBordersParent!.childCount; i++)
        {
            Transform child = _regionBordersParent.GetChild(i);
            Object.Destroy(child.gameObject);
        }
        Object.Destroy(_regionBordersParent);

        _regionBorderBundle!.Unload(true);
    }
}