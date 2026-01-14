using System;
using System.Collections.Generic;
using System.Linq;
using DanielWillett.UITools.API.Extensions;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Types;
using EditorHelper2.UI.Builders;
using SDG.Framework.Devkit.Transactions;
using SDG.Framework.Foliage;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Terrain.Foliage;

[UIExtension(typeof(EditorTerrainDetailsUI))]
[EHExtension("Resource Replacer Extension", "Senior S")]
public class ResourceReplacerExtension : UIExtension, IExtension
{
    private const float MinRadius = 5f;
    private const float MaxRadius = 1500f;
    private const float DefaultRadius = 5f;
    private const int WarningThreshold = 500;

    private EditorTerrainDetailsUI? _currentUIInstance;
    private bool _menuActive;

    private readonly ISleekBox _menuPanel;
    private readonly ISleekButton _toggleButton;

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

    private readonly SleekButtonState _generationFilterDropdown;
    private readonly ISleekToggle _useNewTransformToggle;

    private readonly ISleekButton _replaceButton;
    private readonly ISleekButton _cleanOrphansButton;

    private readonly ISleekLabel _statusLabel;

    private ResourceAsset? _sourceAsset;
    private ResourceAsset? _targetAsset;
    private bool _selectingSource = true;
    private List<ResourceAsset> _allResourceAssets = [];
    private List<ResourceAsset> _filteredAssets = [];
    private float _currentRadius = DefaultRadius;

    public ResourceReplacerExtension(EditorTerrainDetailsUI instance)
    {
        _currentUIInstance = instance;

        UIBuilder builder = new(400f, 450f);
        builder.SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-200f)
            .SetOffsetVertical(600f);

        _menuPanel = builder.BuildBox();

        builder.ResetProperties()
            .SetAnchorHorizontal(0.5f)
            .SetOffsetHorizontal(-100f)
            .SetOffsetVertical(10f)
            .SetSizeHorizontal(200f)
            .SetSizeVertical(25f)
            .SetText("Resource Replacer");
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
        _pickNearestButton = builder.BuildButton("Pick the nearest resource to the camera as source");
        _pickNearestButton.OnClicked += OnPickNearestButtonClicked;
        _menuPanel.AddChild(_pickNearestButton);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(-58f)
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

        _generationFilterDropdown = new SleekButtonState(
            new GUIContent("All"),
            new GUIContent("Manual Only"),
            new GUIContent("Baked Only")
        );
        _generationFilterDropdown.PositionOffset_X = 10f;
        _generationFilterDropdown.PositionOffset_Y = 350f;
        _generationFilterDropdown.SizeOffset_X = 120f;
        _generationFilterDropdown.SizeOffset_Y = 25f;
        _generationFilterDropdown.AddLabel("Filter", ESleekSide.LEFT);
        _menuPanel.AddChild(_generationFilterDropdown);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(200f)
            .SetOffsetVertical(350f)
            .SetSizeHorizontal(40f)
            .SetSizeVertical(25f)
            .SetText("New Transform");
        _useNewTransformToggle = builder.BuildToggle("Use new asset's default rotation/scale");
        _useNewTransformToggle.Value = true;
        _menuPanel.AddChild(_useNewTransformToggle);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(10f)
            .SetOffsetVertical(385f)
            .SetSizeHorizontal(185f)
            .SetSizeVertical(30f)
            .SetText("Replace");
        _replaceButton = builder.BuildButton("Select both source and target assets");
        _replaceButton.IsClickable = false;
        _replaceButton.OnClicked += OnReplaceButtonClicked;
        _menuPanel.AddChild(_replaceButton);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(205f)
            .SetOffsetVertical(385f)
            .SetSizeHorizontal(185f)
            .SetSizeVertical(30f)
            .SetText("Clean Orphaned");
        _cleanOrphansButton = builder.BuildButton("Remove resources with missing assets");
        _cleanOrphansButton.OnClicked += OnCleanOrphansButtonClicked;
        _menuPanel.AddChild(_cleanOrphansButton);

        builder.ResetProperties()
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(10f)
            .SetOffsetVertical(420f)
            .SetSizeHorizontal(380f)
            .SetSizeVertical(20f)
            .SetText("");
        _statusLabel = builder.BuildLabel();
        _menuPanel.AddChild(_statusLabel);

        builder.ResetProperties()
            .SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-75f)
            .SetOffsetVertical(-35f)
            .SetSizeHorizontal(150f)
            .SetSizeVertical(30f)
            .SetText("Resource Replacer");
        _toggleButton = builder.BuildButton("Open the resource replacer menu");
        _toggleButton.OnClicked += OnToggleButtonClicked;

        LoadResourceAssets();

        Initialize();
    }

    public void Initialize()
    {
        if (_currentUIInstance == null) return;

        _currentUIInstance.AddChild(_menuPanel);
        _currentUIInstance.AddChild(_toggleButton);

        RefreshAssetList();
    }

    private void LoadResourceAssets()
    {
        _allResourceAssets.Clear();
        SDG.Unturned.Assets.find(_allResourceAssets);
        _allResourceAssets = _allResourceAssets.OrderBy(a => a.FriendlyName).ToList();
        _filteredAssets = new List<ResourceAsset>(_allResourceAssets);
    }

    private void RefreshAssetList()
    {
        _assetScrollView.RemoveAllChildren();
        float offsetY = 0f;

        UIBuilder itemBuilder = new(0f, 25f);

        foreach (ResourceAsset asset in _filteredAssets)
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
            _filteredAssets = new List<ResourceAsset>(_allResourceAssets);
        }
        else
        {
            _filteredAssets = _allResourceAssets
                .Where(a => a.FriendlyName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        RefreshAssetList();
    }

    private void OnAssetSelected(ResourceAsset asset)
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
        ResourceAsset? closestAsset = null;

        // Check Trees
        List<ResourceSpawnpoint> allTrees = [];
        LevelGround.GatherAllTrees(allTrees);
        foreach (ResourceSpawnpoint tree in allTrees)
        {
            if (tree.asset == null) continue;

            float distSq = (tree.point - cameraPosition).sqrMagnitude;
            if (distSq < closestDistanceSquared)
            {
                closestDistanceSquared = distSq;
                closestAsset = tree.asset;
            }
        }

        // Check Foliage
        List<FoliageResourceInfoAsset> resourceInfoAssets = [];
        SDG.Unturned.Assets.find(resourceInfoAssets);

        foreach (KeyValuePair<FoliageCoord, FoliageTile> tilePair in FoliageSystem.tiles)
        {
            FoliageTile tile = tilePair.Value;
            foreach (KeyValuePair<AssetReference<FoliageInstancedMeshInfoAsset>, FoliageInstanceList> pair in tile.instances)
            {
                FoliageResourceInfoAsset? info = resourceInfoAssets.FirstOrDefault(x => x.GUID == pair.Key.GUID);
                ResourceAsset? resourceAsset = info?.resource.Find();
                if (resourceAsset == null) continue;

                foreach (List<Matrix4x4> matrixList in pair.Value.matrices)
                {
                    foreach (Matrix4x4 matrix in matrixList)
                    {
                        float distSq = (matrix.GetPosition() - cameraPosition).sqrMagnitude;
                        if (distSq < closestDistanceSquared)
                        {
                            closestDistanceSquared = distSq;
                            closestAsset = resourceAsset;
                        }
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
            _statusLabel.Text = "No resources found nearby";
        }
    }

    private void OnReplaceButtonClicked(ISleekElement button)
    {
        if (_sourceAsset == null || _targetAsset == null) return;

        List<ResourceSpawnpoint> matchingResources = GetMatchingResources();

        if (matchingResources.Count >= WarningThreshold)
        {
            UnturnedLog.info($"[ResourceReplacer] Warning: Replacing {matchingResources.Count} resources");
        }

        ExecuteReplacement(matchingResources);
    }

    private void OnCleanOrphansButtonClicked(ISleekElement button)
    {
        int cleanedCount = CleanOrphanedResources();
        _statusLabel.Text = $"Cleaned {cleanedCount} orphaned resources";
    }

    #endregion Event Handlers

    #region Core Logic

    private List<ResourceSpawnpoint> GetMatchingResources()
    {
        List<ResourceSpawnpoint> allTrees = [];
        LevelGround.GatherAllTrees(allTrees);

        Vector3 cameraPosition = MainCamera.instance?.transform.position ?? Vector3.zero;
        bool wholeMap = _wholeMapToggle.Value;
        float radiusSquared = _currentRadius * _currentRadius;

        int filterState = _generationFilterDropdown.state;

        return allTrees.Where(tree =>
        {
            if (tree.asset == null || tree.asset.GUID != _sourceAsset!.GUID)
                return false;

            if (tree.isDead)
                return false;

            if (!wholeMap)
            {
                float distanceSquared = (tree.point - cameraPosition).sqrMagnitude;
                if (distanceSquared > radiusSquared)
                    return false;
            }

            switch (filterState)
            {
                case 1: // Manual only
                    if (tree.isGenerated) return false;
                    break;
                case 2: // Baked only
                    if (!tree.isGenerated) return false;
                    break;
                // case 0: All - no filter
            }

            return true;
        }).ToList();
    }

    private void ExecuteReplacement(List<ResourceSpawnpoint> resourcesToReplace)
    {
        if (_targetAsset == null || _sourceAsset == null) return;

        List<ReplacedResourceData> resourceTransactionData = [];
        List<ReplacedFoliageData> foliageTransactionData = [];

        int replacedCount = 0;
        bool useNewTransform = _useNewTransformToggle.Value;

        foreach (ResourceSpawnpoint resource in resourcesToReplace)
        {
            Vector3 position = resource.point;
            Quaternion originalRotation = resource.angle;
            Vector3 originalScale = resource.scale;
            bool isGenerated = resource.isGenerated;

            Quaternion newRotation = originalRotation;
            Vector3 newScale = originalScale;

            if (useNewTransform)
            {
                _targetAsset.GetLegacyRotationAndScale(position, out newRotation, out newScale);
            }

            resourceTransactionData.Add(new ReplacedResourceData
            {
                Position = position,
                Rotation = originalRotation,
                Scale = originalScale,
                OriginalAssetGuid = _sourceAsset.GUID,
                NewAssetGuid = _targetAsset.GUID,
                IsGenerated = isGenerated
            });

            resource.destroy();
            RemoveResourceFromStorage(resource);
            LevelGround.addSpawn(position, newRotation, newScale, _targetAsset.GUID, isGenerated);

            replacedCount++;
        }

        int foliageReplaced = ReplaceFoliageInstances(foliageTransactionData);
        replacedCount += foliageReplaced;

        if (resourceTransactionData.Count > 0 || foliageTransactionData.Count > 0)
        {
            DevkitTransactionManager.beginTransaction("Resource Replacement");
            ResourceReplacementTransaction transaction = new(resourceTransactionData, foliageTransactionData);
            DevkitTransactionManager.recordTransaction(transaction);
            DevkitTransactionManager.endTransaction();
        }

        _statusLabel.Text = $"Replaced {replacedCount} resources";
    }

    private void RemoveResourceFromStorage(ResourceSpawnpoint resource)
    {
        Vector2Int coord = Regions.GetCoordinateVector2Int(resource.point);

        List<ResourceSpawnpoint>? regionTrees = LevelGround.GetTreesOrNullInRegion(coord);
        regionTrees?.Remove(resource);
    }

    private int ReplaceFoliageInstances(List<ReplacedFoliageData> transactionData)
    {
        if (_sourceAsset == null || _targetAsset == null) return 0;

        int replacedCount = 0;
        bool useNewTransform = _useNewTransformToggle.Value;
        Vector3 cameraPosition = MainCamera.instance?.transform.position ?? Vector3.zero;
        bool wholeMap = _wholeMapToggle.Value;
        float radiusSquared = _currentRadius * _currentRadius;

        List<FoliageResourceInfoAsset> resourceInfoAssets = [];
        SDG.Unturned.Assets.find(resourceInfoAssets);

        FoliageResourceInfoAsset? sourceFoliageAsset = resourceInfoAssets
            .FirstOrDefault(f => f.resource.Find()?.GUID == _sourceAsset.GUID);

        FoliageResourceInfoAsset? targetFoliageAsset = resourceInfoAssets
            .FirstOrDefault(f => f.resource.Find()?.GUID == _targetAsset.GUID);

        if (sourceFoliageAsset == null || targetFoliageAsset == null)
            return 0;

        AssetReference<FoliageInstancedMeshInfoAsset> sourceRef = new(sourceFoliageAsset.GUID);
        AssetReference<FoliageInstancedMeshInfoAsset> targetRef = new(targetFoliageAsset.GUID);

        List<(FoliageTile tile, FoliageCoord coord, int matricesIndex, int matrixIndex, Matrix4x4 matrix, bool clearWhenBaked)> instancesToReplace = [];

        foreach (KeyValuePair<FoliageCoord, FoliageTile> tilePair in FoliageSystem.tiles)
        {
            FoliageTile tile = tilePair.Value;
            FoliageCoord coord = tilePair.Key;

            if (!tile.instances.TryGetValue(sourceRef, out FoliageInstanceList? instanceList))
                continue;

            for (int i = 0; i < instanceList.matrices.Count; i++)
            {
                List<Matrix4x4> matrixList = instanceList.matrices[i];
                List<bool> clearWhenBakedList = instanceList.clearWhenBaked[i];

                for (int j = 0; j < matrixList.Count; j++)
                {
                    Matrix4x4 matrix = matrixList[j];
                    Vector3 position = matrix.GetPosition();

                    if (!wholeMap)
                    {
                        float distanceSquared = (position - cameraPosition).sqrMagnitude;
                        if (distanceSquared > radiusSquared)
                            continue;
                    }

                    instancesToReplace.Add((tile, coord, i, j, matrix, clearWhenBakedList[j]));
                }
            }
        }

        instancesToReplace.Reverse();

        foreach (var (tile, coord, matricesIndex, matrixIndex, matrix, clearWhenBaked) in instancesToReplace)
        {
            if (!tile.instances.TryGetValue(sourceRef, out FoliageInstanceList? oldList))
                continue;

            tile.removeInstance(oldList, matricesIndex, matrixIndex);

            Matrix4x4 newMatrix = matrix;
            if (useNewTransform)
            {
                Vector3 position = matrix.GetPosition();
                _targetAsset.GetLegacyRotationAndScale(position, out Quaternion newRotation, out Vector3 newScale);
                newMatrix = Matrix4x4.TRS(position, newRotation, newScale);
            }

            transactionData.Add(new ReplacedFoliageData
            {
                TileCoord = coord,
                OriginalMatrix = matrix,
                NewMatrix = newMatrix,
                OriginalAssetGuid = sourceFoliageAsset.GUID,
                NewAssetGuid = targetFoliageAsset.GUID,
                ClearWhenBaked = clearWhenBaked
            });

            FoliageInstanceGroup newInstance = new(targetRef, newMatrix, clearWhenBaked);
            tile.addInstance(newInstance);

            replacedCount++;
        }

        return replacedCount;
    }

    private int CleanOrphanedResources()
    {
        List<ResourceSpawnpoint> allTrees = [];
        LevelGround.GatherAllTrees(allTrees);

        Vector3 cameraPosition = MainCamera.instance?.transform.position ?? Vector3.zero;
        bool wholeMap = _wholeMapToggle.Value;
        float radiusSquared = _currentRadius * _currentRadius;

        List<ResourceSpawnpoint> orphans = allTrees.Where(tree =>
        {
            if (tree.asset != null)
                return false;

            if (!wholeMap)
            {
                float distanceSquared = (tree.point - cameraPosition).sqrMagnitude;
                if (distanceSquared > radiusSquared)
                    return false;
            }

            return true;
        }).ToList();

        foreach (ResourceSpawnpoint orphan in orphans)
        {
            orphan.destroy();
            RemoveResourceFromStorage(orphan);
        }

        return orphans.Count;
    }

    #endregion Core Logic

    #region Extension Functions

    public void CustomUpdate()
    {
        if (!_menuActive || !Input.GetKey(KeyCode.LeftControl)) return;
        if (Input.GetKeyDown(KeyCode.X))
        {
            DevkitTransactionManager.redo();
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            DevkitTransactionManager.undo();
        }
    }

    #endregion Extension Functions

    public void Dispose()
    {
        if (_currentUIInstance == null) return;

        _currentUIInstance.RemoveChild(_menuPanel);
        _currentUIInstance.RemoveChild(_toggleButton);

        _toggleButton.OnClicked -= OnToggleButtonClicked;
        _radiusSlider.OnValueChanged -= OnRadiusSliderChanged;
        _wholeMapToggle.OnValueChanged -= OnWholeMapToggleChanged;
        _sourceButton.OnClicked -= OnSourceButtonClicked;
        _targetButton.OnClicked -= OnTargetButtonClicked;
        _searchButton.OnClicked -= OnSearchButtonClicked;
        _replaceButton.OnClicked -= OnReplaceButtonClicked;
        _cleanOrphansButton.OnClicked -= OnCleanOrphansButtonClicked;
        _pickNearestButton.OnClicked -= OnPickNearestButtonClicked;
    }
}
