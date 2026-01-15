using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.UI.Builders;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Level.Objects;

[UIExtension(typeof(EditorLevelObjectsUI))]
[EHExtension("Object Replacer Extension", "Senior S")]
public class ObjectReplacerExtension : UIExtension, IExtension
{
    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;

    private const float MinRadius = 5f;
    private const float MaxRadius = 1500f;
    private const float DefaultRadius = 5f;
    private const int WarningThreshold = 500;

    private bool _menuActive;

    private readonly ISleekBox _menuPanel;
    private readonly SleekButtonIcon _toggleButton;

    private readonly ISleekSlider _radiusSlider;
    private readonly ISleekLabel _radiusValueLabel;
    private readonly ISleekToggle _wholeMapToggle;

    private readonly ISleekButton _sourceButton;
    private readonly ISleekButton _pickNearestButton;
    private readonly ISleekButton _targetButton;
    private readonly ISleekLabel _selectionModeLabel;
    private readonly ISleekField _searchField;
    private readonly ISleekButton _searchButton;
    private readonly ISleekScrollView _assetScrollView;

    private readonly ISleekButton _replaceButton;

    private readonly ISleekLabel _statusLabel;

    private ObjectAsset? _sourceAsset;
    private ObjectAsset? _targetAsset;
    private bool _selectingSource = true;
    private List<ObjectAsset> _allObjectAssets = [];
    private List<ObjectAsset> _filteredAssets = [];
    private float _currentRadius = DefaultRadius;

    public ObjectReplacerExtension()
    {
        UIBuilder builder = new(400f, 410f);
        builder.SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-200f)
            .SetOffsetVertical(550f);

        _menuPanel = builder.BuildBox();

        builder.ResetProperties()
            .SetAnchorHorizontal(0.5f)
            .SetOffsetHorizontal(-100f)
            .SetOffsetVertical(10f)
            .SetSizeHorizontal(200f)
            .SetSizeVertical(25f)
            .SetText("Object Replacer");
        ISleekLabel titleLabel = builder.BuildLabel();
        _menuPanel.AddChild(titleLabel);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(10f)
            .SetOffsetVertical(45f)
            .SetSizeHorizontal(280f)
            .SetSizeVertical(20f);
        _radiusSlider = builder.BuildSlider();
        _radiusSlider.Value = (DefaultRadius - MinRadius) / (MaxRadius - MinRadius);
        _radiusSlider.AddLabel("Radius", ESleekSide.LEFT);
        _radiusSlider.OnValueChanged += OnRadiusSliderChanged;
        _menuPanel.AddChild(_radiusSlider);

        builder.SetOffsetHorizontal(300f)
            .SetOffsetVertical(45f)
            .SetSizeHorizontal(50f)
            .SetSizeVertical(20f)
            .SetText(DefaultRadius.ToString("F0"));
        _radiusValueLabel = builder.BuildLabel();
        _radiusValueLabel.TextAlignment = TextAnchor.MiddleLeft;
        _menuPanel.AddChild(_radiusValueLabel);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(350f)
            .SetOffsetVertical(42f)
            .SetSizeHorizontal(40f)
            .SetSizeVertical(25f);
        _wholeMapToggle = builder.BuildToggle("Enable to replace on entire map");
        _wholeMapToggle.Value = false;
        _wholeMapToggle.OnValueChanged += OnWholeMapToggleChanged;
        _menuPanel.AddChild(_wholeMapToggle);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(10f)
            .SetOffsetVertical(75f)
            .SetSizeHorizontal(250f)
            .SetSizeVertical(25f)
            .SetText("Click to select source...");
        _sourceButton = builder.BuildButton("Click to set as source asset selection mode");
        _sourceButton.OnClicked += OnSourceButtonClicked;
        _menuPanel.AddChild(_sourceButton);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(270f)
            .SetOffsetVertical(75f)
            .SetSizeHorizontal(120f)
            .SetSizeVertical(25f)
            .SetText("Pick Nearest");
        _pickNearestButton = builder.BuildButton("Pick the nearest object to the camera as source");
        _pickNearestButton.OnClicked += OnPickNearestButtonClicked;
        _menuPanel.AddChild(_pickNearestButton);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(-56f)
            .SetOffsetVertical(75f)
            .SetSizeHorizontal(60f)
            .SetSizeVertical(25f)
            .SetText("Replace");
        ISleekLabel sourceLabel = builder.BuildLabel(TextAnchor.MiddleRight);
        _menuPanel.AddChild(sourceLabel);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(10f)
            .SetOffsetVertical(108f)
            .SetSizeHorizontal(380f)
            .SetSizeVertical(25f)
            .SetText("Click to select target...");
        _targetButton = builder.BuildButton("Click to set as target asset selection mode");
        _targetButton.OnClicked += OnTargetButtonClicked;
        _menuPanel.AddChild(_targetButton);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(-50f)
            .SetOffsetVertical(105f)
            .SetSizeHorizontal(55f)
            .SetSizeVertical(25f)
            .SetText("With");
        ISleekLabel targetLabel = builder.BuildLabel(TextAnchor.MiddleRight);
        _menuPanel.AddChild(targetLabel);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(10f)
            .SetOffsetVertical(140f)
            .SetSizeHorizontal(150f)
            .SetSizeVertical(20f)
            .SetText("Select Source");
        _selectionModeLabel = builder.BuildLabel(TextAnchor.MiddleLeft);
        _menuPanel.AddChild(_selectionModeLabel);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(10f)
            .SetOffsetVertical(165f)
            .SetSizeHorizontal(310f)
            .SetSizeVertical(25f)
            .SetText("Search assets...");
        _searchField = builder.BuildStringField();
        _menuPanel.AddChild(_searchField);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(325f)
            .SetOffsetVertical(165f)
            .SetSizeHorizontal(65f)
            .SetSizeVertical(25f)
            .SetText("Search");
        _searchButton = builder.BuildButton("Search for assets by name");
        _searchButton.OnClicked += OnSearchButtonClicked;
        _menuPanel.AddChild(_searchButton);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(10f)
            .SetOffsetVertical(195f)
            .SetSizeHorizontal(380f)
            .SetSizeVertical(150f);
        _assetScrollView = builder.BuildScrollView(true);
        _menuPanel.AddChild(_assetScrollView);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(10f)
            .SetOffsetVertical(350f)
            .SetSizeHorizontal(380f)
            .SetSizeVertical(30f)
            .SetText("Replace");
        _replaceButton = builder.BuildButton("Select both source and target assets");
        _replaceButton.IsClickable = false;
        _replaceButton.OnClicked += OnReplaceButtonClicked;
        _menuPanel.AddChild(_replaceButton);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(10f)
            .SetOffsetVertical(385f)
            .SetSizeHorizontal(380f)
            .SetSizeVertical(20f)
            .SetText("");
        _statusLabel = builder.BuildLabel();
        _menuPanel.AddChild(_statusLabel);

        Assembly assembly = typeof(EditorHelper).Assembly; 
        string iconsPath = Path.Combine(Path.GetDirectoryName(assembly.Location) ?? string.Empty, "Assets" ,"Icons.unity3d"); 
        Bundle icons = Bundles.getBundle(iconsPath, false);

        builder.ResetProperties()
            .SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(150)
            .SetOffsetVertical(-65f)
            .SetSizeHorizontal(130f)
            .SetSizeVertical(30f)
            .SetText("Object Replacer");
        _toggleButton = builder.BuildButtonIcon("Open object replacer menu");
        _toggleButton.onClickedButton += OnToggleButtonClicked;

        icons.unload();

        LoadObjectAssets();

        Initialize();
    }

    public void Initialize()
    {
        if (_container == null) return;

        _container.AddChild(_menuPanel);
        _container.AddChild(_toggleButton);

        RefreshAssetList();
    }

    private void LoadObjectAssets()
    {
        _allObjectAssets.Clear();
        SDG.Unturned.Assets.find(_allObjectAssets);
        _allObjectAssets = _allObjectAssets.OrderBy(a => a.objectName).ToList();
        _filteredAssets = new List<ObjectAsset>(_allObjectAssets);
    }

    private void RefreshAssetList()
    {
        _assetScrollView.RemoveAllChildren();
        float offsetY = 0f;

        UIBuilder itemBuilder = new(0f, 25f);

        foreach (ObjectAsset asset in _filteredAssets)
        {
            itemBuilder.ResetProperties()
                .SetAnchorHorizontal(0f)
                .SetOffsetVertical(offsetY)
                .SetScaleHorizontal(1f)
                .SetSizeVertical(25f)
                .SetText(asset.FriendlyName);

            ISleekButton assetButton = itemBuilder.BuildButton();
            assetButton.OnClicked += _ => OnAssetSelected(asset);
            _assetScrollView.AddChild(assetButton);
            offsetY += 25f;
        }

        _assetScrollView.ContentSizeOffset = new Vector2(0f, offsetY);
    }

    private void UpdateReplaceButtonState()
    {
        bool canReplace = _sourceAsset != null && _targetAsset != null;
        _replaceButton.IsClickable = canReplace;
        _replaceButton.TooltipText = canReplace
            ? $"Replace {_sourceAsset!.FriendlyName} with {_targetAsset!.FriendlyName}"
            : "Select both source and target assets";
    }

    private void UpdateSelectionHighlight()
    {
        if (_selectingSource)
        {
            _sourceButton.BackgroundColor = new SleekColor(ESleekTint.BACKGROUND, 0.8f);
            _targetButton.BackgroundColor = new SleekColor(ESleekTint.BACKGROUND, 0.4f);
        }
        else
        {
            _sourceButton.BackgroundColor = new SleekColor(ESleekTint.BACKGROUND, 0.4f);
            _targetButton.BackgroundColor = new SleekColor(ESleekTint.BACKGROUND, 0.8f);
        }
    }

    #region Event Handlers

    private void OnToggleButtonClicked(ISleekElement button)
    {
        _menuActive = !_menuActive;
        float targetY = _menuActive ? -485f : 600f;
        _menuPanel.AnimatePositionOffset(-200f, targetY, ESleekLerp.EXPONENTIAL, 15f);
    }

    private void OnRadiusSliderChanged(ISleekSlider slider, float value)
    {
        _currentRadius = MinRadius + value * (MaxRadius - MinRadius);
        _radiusValueLabel.Text = _currentRadius.ToString("F0");
    }

    private void OnWholeMapToggleChanged(ISleekToggle toggle, bool value)
    {
        _radiusSlider.IsInteractable = !value;
    }

    private void OnSourceButtonClicked(ISleekElement button)
    {
        _selectingSource = true;
        _selectionModeLabel.Text = "Select Source";
        UpdateSelectionHighlight();
    }

    private void OnTargetButtonClicked(ISleekElement button)
    {
        _selectingSource = false;
        _selectionModeLabel.Text = "Select Target";
        UpdateSelectionHighlight();
    }

    private void OnSearchButtonClicked(ISleekElement button)
    {
        string searchText = _searchField.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(searchText))
        {
            _filteredAssets = new List<ObjectAsset>(_allObjectAssets);
        }
        else
        {
            _filteredAssets = _allObjectAssets
                .Where(a => a.FriendlyName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        RefreshAssetList();
    }

    private void OnAssetSelected(ObjectAsset asset)
    {
        if (_selectingSource)
        {
            _sourceAsset = asset;
            _sourceButton.Text = asset.FriendlyName;
        }
        else
        {
            _targetAsset = asset;
            _targetButton.Text = asset.FriendlyName;
        }

        UpdateReplaceButtonState();
    }

    private void OnPickNearestButtonClicked(ISleekElement button)
    {
        Vector3 cameraPosition = MainCamera.instance?.transform.position ?? Vector3.zero;
        float closestDistanceSquared = float.MaxValue;
        ObjectAsset? closestAsset = null;

        for (byte x = 0; x < Regions.WORLD_SIZE; x++)
        {
            for (byte y = 0; y < Regions.WORLD_SIZE; y++)
            {
                foreach (LevelObject levelObject in LevelObjects.objects[x, y])
                {
                    if (levelObject.asset == null) continue;

                    float distSq = (levelObject.transform.position - cameraPosition).sqrMagnitude;
                    if (distSq < closestDistanceSquared)
                    {
                        closestDistanceSquared = distSq;
                        closestAsset = levelObject.asset;
                    }
                }
            }
        }

        if (closestAsset != null)
        {
            _selectingSource = true;
            OnAssetSelected(closestAsset);
            _selectionModeLabel.Text = "Select Source";
            UpdateSelectionHighlight();
            _statusLabel.Text = $"Selected nearest: {closestAsset.FriendlyName}";
            OnTargetButtonClicked(null);
        }
        else
        {
            _statusLabel.Text = "No objects found nearby";
        }
    }

    private void OnReplaceButtonClicked(ISleekElement button)
    {
        if (_sourceAsset == null || _targetAsset == null) return;

        List<LevelObject> matchingObjects = GetMatchingObjects();

        if (matchingObjects.Count >= WarningThreshold)
        {
            UnturnedLog.info($"[ObjectReplacer] Warning: Replacing {matchingObjects.Count} objects");
        }

        ExecuteReplacement(matchingObjects);
    }

    #endregion Event Handlers

    #region Core Logic

    private List<LevelObject> GetMatchingObjects()
    {
        List<LevelObject> allObjects = [];

        for (byte x = 0; x < Regions.WORLD_SIZE; x++)
        {
            for (byte y = 0; y < Regions.WORLD_SIZE; y++)
            {
                allObjects.AddRange(LevelObjects.objects[x, y]);
            }
        }

        Vector3 cameraPosition = MainCamera.instance?.transform.position ?? Vector3.zero;
        bool wholeMap = _wholeMapToggle.Value;
        float radiusSquared = _currentRadius * _currentRadius;

        return allObjects.Where(obj =>
        {
            if (obj.asset == null || obj.asset.GUID != _sourceAsset!.GUID)
                return false;

            if (!wholeMap)
            {
                float distanceSquared = (obj.transform.position - cameraPosition).sqrMagnitude;
                if (distanceSquared > radiusSquared)
                    return false;
            }

            return true;
        }).ToList();
    }

    private void ExecuteReplacement(List<LevelObject> objectsToReplace)
    {
        if (_targetAsset == null || _sourceAsset == null) return;

        int replacedCount = 0;

        foreach (LevelObject levelObject in objectsToReplace)
        {
            Vector3 position = levelObject.transform.position;
            Quaternion rotation = levelObject.transform.rotation;
            Vector3 scale = levelObject.transform.localScale;
            AssetReference<MaterialPaletteAsset> customMaterial = levelObject.customMaterialOverride;
            int materialIndex = levelObject.materialIndexOverride;
            
            LevelObjects.registerRemoveObject(levelObject.transform);
            Transform newTransform = LevelObjects.registerAddObject(position, rotation, scale, _targetAsset, null);
            
            if (newTransform != null && Regions.tryGetCoordinate(newTransform.position, out byte newX, out byte newY))
            {
                for (int i = 0; i < LevelObjects.objects[newX, newY].Count; i++)
                {
                    if (LevelObjects.objects[newX, newY][i].transform == newTransform)
                    {
                        LevelObject obj = LevelObjects.objects[newX, newY][i];
                        obj.customMaterialOverride = customMaterial;
                        obj.materialIndexOverride = materialIndex;
                        break;
                    }
                }
            }

            replacedCount++;
        }

        _statusLabel.Text = $"Replaced {replacedCount} objects";
    }

    #endregion Core Logic

    #region Extension Functions

    public void CustomUpdate()
    {
    }

    #endregion Extension Functions

    public void Dispose()
    {
        if (_container == null) return;

        _container.RemoveChild(_menuPanel);
        _container.RemoveChild(_toggleButton);

        _toggleButton.onClickedButton -= OnToggleButtonClicked;
        _radiusSlider.OnValueChanged -= OnRadiusSliderChanged;
        _wholeMapToggle.OnValueChanged -= OnWholeMapToggleChanged;
        _sourceButton.OnClicked -= OnSourceButtonClicked;
        _targetButton.OnClicked -= OnTargetButtonClicked;
        _searchButton.OnClicked -= OnSearchButtonClicked;
        _replaceButton.OnClicked -= OnReplaceButtonClicked;
        _pickNearestButton.OnClicked -= OnPickNearestButtonClicked;
    }
}
