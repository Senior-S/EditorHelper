using System.IO;
using System.Reflection;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.UI.Builders;
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
    private const int DEBUG_SIZE = 1;

    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;
    [ExistingMember("regionLabels")]
    private readonly ISleekLabel[]? _regionLabels;

    private readonly ISleekLabel _toggleDescriptiveInfo;
    private ISleekLabel? _targetRegionLabel;
    private string? _targetRegionLabelText;
    private bool _wantsDescriptiveInfo;

    private readonly Transform?[] _regionBorders;
    private readonly AssetBundle? _regionBorderBundle;
    private readonly GameObject? _regionBorderPrefab;
    
    public RegionsExtension()
    {
        UIBuilder builder = new UIBuilder(0f, 30f)
            .SetAnchorVertical(1f)
            .SetScaleHorizontal(1f)
            .SetOffsetVertical(-40f)
            .SetText($"Hold [{MenuConfigurationControlsUI.getKeyCodeText(ControlsSettings.snap)}] to show descriptive information");

        _toggleDescriptiveInfo = builder.BuildLabel();
        _toggleDescriptiveInfo.TextContrastContext = ETextContrastContext.ColorfulBackdrop;

        Assembly assembly = Assembly.GetExecutingAssembly();
        string bundlePath = Path.Combine(Path.GetDirectoryName(assembly.Location) ?? string.Empty, "Assets" ,"RegionBorder.unity3d");

        _regionBorders = new Transform[DEBUG_SIZE * DEBUG_SIZE];

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
        _container?.AddChild(_toggleDescriptiveInfo);

        if (_regionLabels != null)
            _targetRegionLabel = _regionLabels[_regionLabels.Length / 2];

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

    internal void CustomUpdate()
    {
        if (_targetRegionLabel == null) return;

        _wantsDescriptiveInfo = InputEx.GetKey(ControlsSettings.snap);

        if (_wantsDescriptiveInfo && _targetRegionLabelText == null)
        {
            _targetRegionLabelText = _targetRegionLabel.Text;

            string[] lines = _targetRegionLabelText.Split('\n');
            lines[1] = "Objects: " + lines[1] + " of all";
            lines[2] = "Triangles: " + lines[2];

            _targetRegionLabel.Text = string.Join('\n', lines);
        }
        else if (!_wantsDescriptiveInfo && _targetRegionLabelText != null)
        {
            _targetRegionLabel.Text = _targetRegionLabelText;
            _targetRegionLabelText = null;
        }
    }

    internal void CustomUpdateRegion(int cameraRegionX, int cameraRegionY)
    {
        if (_targetRegionLabel != null && _wantsDescriptiveInfo)
        {
            _targetRegionLabelText = _targetRegionLabel.Text;

            string[] lines = _targetRegionLabelText.Split('\n');
            lines[1] = "Objects: " + lines[1] + " of all";
            lines[2] = "Triangles: " + lines[2];

            _targetRegionLabel.Text = string.Join('\n', lines);
        }

        if (_regionBorderPrefab == null || DEBUG_SIZE < 1) return;

        // Loop through the grid around the camera's current region
        for (int i = -DEBUG_SIZE / 2; i <= DEBUG_SIZE / 2; i++)
        {
            for (int j = -DEBUG_SIZE / 2; j <= DEBUG_SIZE / 2; j++)
            {
                byte x = (byte)(cameraRegionX + i);
                byte y = (byte)(cameraRegionY + j);

                if (!Regions.checkSafe(x, y)) continue;

                Vector3 regionPosition = new(
                    (x * Regions.REGION_SIZE) + (Regions.REGION_SIZE / 2) - (Regions.WORLD_SIZE * (Regions.REGION_SIZE / 2)),
                    0,
                    (y * Regions.REGION_SIZE) + (Regions.REGION_SIZE / 2) - (Regions.WORLD_SIZE * (Regions.REGION_SIZE / 2))
                );

                Transform? regionBorder = _regionBorders[((i + (DEBUG_SIZE / 2)) * DEBUG_SIZE) + j + (DEBUG_SIZE / 2)];
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
        _targetRegionLabel = null;

        foreach (Transform? regionBorder in _regionBorders)
        {
            if (regionBorder == null) continue;
            Object.Destroy(regionBorder.gameObject);
        }

        _regionBorderBundle?.Unload(true);
    }
}