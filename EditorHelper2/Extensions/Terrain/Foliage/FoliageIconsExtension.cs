using System.Collections.Generic;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers;
using EditorHelper2.UI.Builders;
using EditorHelper2.UI.Elements;
using SDG.Framework.Foliage;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Terrain.Foliage;

[UIExtension(typeof(EditorTerrainDetailsUI))]
[EHExtension("Foliage Icons Extension", "Senior S & Gamingtoday093")]
public class FoliageIconsExtension : UIExtension, IExtension
{
    private readonly EditorTerrainDetailsUI _currentUIInstance;
    private readonly IconStore _iconStore;

    [ExistingMember("selectedAssetBox")]
    private readonly ISleekBox? _selectedAssetBox;

    [ExistingMember("searchInfoAssets")]
    private readonly List<FoliageInfoAsset>? _searchInfoAssets;

    private readonly ISleekBox _previewIconContainer;
    private readonly ISleekImage _previewIconImage;
    private int _previewIconHandle;
    private FoliageInfoAsset? _lastSelectedAsset;

    private readonly SleekButtonIcon _iconGridButton;
    private readonly ISleekBox _iconGridContainer;
    private readonly SleekGrid<FoliageInfoAsset> _iconGridScrollBox;
    private float _preferredGridHeight = 760f;

    private int _lastGridCount = -1;
    private string _lastGridFirst = string.Empty;
    private string _lastGridLast = string.Empty;

    public FoliageIconsExtension(EditorTerrainDetailsUI instance)
    {
        _currentUIInstance = instance;
        _iconStore = new IconStore(width: 300, height: 300);

        UIBuilder builder = new(160f, 150f);

        builder.SetAnchorHorizontal(1f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-380f)
            .SetOffsetVertical(-160f)
            .SetText("Foliage Icon");

        _previewIconContainer = builder.BuildBox();
        _previewIconContainer.TextColor = new Color(0.35f, 0.35f, 0.35f, 0.35f);

        builder.SetOffsetHorizontal(-150f)
            .SetOffsetVertical(-145f)
            .SetSizeHorizontal(140f)
            .SetSizeVertical(140f);

        _previewIconImage = Glazier.Get().CreateImage();
        _previewIconImage.ShouldDestroyTexture = false;
        builder.FormatElement(ref _previewIconImage);
        _previewIconContainer.AddChild(_previewIconImage);

        builder.ResetProperties()
            .SetAnchorHorizontal(1f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-380f)
            .SetOffsetVertical(-200f)
            .SetSizeHorizontal(160f)
            .SetSizeVertical(30f)
            .SetText("Grid View");

        Bundle bundle = Bundles.getBundle("/Bundles/Textures/Edit/Icons/EditorEnvironment/EditorEnvironment.unity3d");
        _iconGridButton = builder.BuildButtonIcon("View foliage in a grid with icons", bundle.load<Texture2D>("Navigation"));
        bundle.unload();

        builder.SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(0f)
            .SetOffsetHorizontal(-500f)
            .SetOffsetVertical(0f)
            .SetSizeHorizontal(1000f)
            .SetSizeVertical(_preferredGridHeight)
            .SetText(string.Empty);

        _iconGridContainer = builder.BuildAlphaBox();
        _iconGridContainer.IsVisible = false;

        builder.SetAnchorHorizontal(0f)
            .SetAnchorVertical(0f)
            .SetOffsetHorizontal(0f)
            .SetOffsetVertical(0f)
            .SetSizeHorizontal(1000f)
            .SetSizeVertical(0f)
            .SetScaleVertical(1f);

        _iconGridScrollBox = new SleekGrid<FoliageInfoAsset>
        {
            itemSize = 125,
            itemPadding = 15,
            OnCreateElement = OnCreateGridAsset
        };

        builder.FormatElement(ref _iconGridScrollBox);
        _iconGridContainer.AddChild(_iconGridScrollBox);

        Initialize();
    }

    public void Initialize()
    {
        _currentUIInstance.AddChild(_previewIconContainer);
        _currentUIInstance.AddChild(_iconGridButton);
        _currentUIInstance.AddChild(_iconGridContainer);

        OnResolutionUpdateIconGridSize();

        _iconGridButton.onClickedButton += OnIconGridButtonClicked;
        GraphicsSettings.graphicsSettingsApplied += OnResolutionUpdateIconGridSize;
    }

    public void CustomUpdate()
    {
        _iconStore.CustomUpdate();

        bool isVisible = _currentUIInstance.tool.mode != FoliageEditor.EFoliageMode.BAKE &&
                         _currentUIInstance.searchTypeButton.state == 0;

        _previewIconContainer.IsVisible = isVisible;
        _iconGridButton.IsVisible = isVisible;

        if (!isVisible)
        {
            _iconGridContainer.IsVisible = false;
            return;
        }

        if (_iconGridContainer.IsVisible)
        {
            SyncGridData();
        }

        FoliageInfoAsset? selectedAsset = _currentUIInstance.tool.selectedInstanceAsset;
        if (selectedAsset != _lastSelectedAsset)
        {
            _lastSelectedAsset = selectedAsset;
            OnFoliageAssetSelected(selectedAsset);
        }
    }

    private void OnFoliageAssetSelected(FoliageInfoAsset? selectedAsset)
    {
        if (selectedAsset == null)
        {
            _previewIconContainer.Text = "Foliage Icon";
            return;
        }

        if (_selectedAssetBox != null)
        {
            _selectedAssetBox.Text = selectedAsset.name;
        }

        if (selectedAsset is FoliageObjectInfoAsset objectInfoAsset)
        {
            ObjectAsset? objectAsset = objectInfoAsset.obj.Find();
            if (objectAsset != null)
            {
                _previewIconHandle = _iconStore.RequestIcon(objectAsset, OnFoliageIconReady);
                return;
            }
        }
        else if (selectedAsset is FoliageResourceInfoAsset resourceInfoAsset)
        {
            ResourceAsset? resourceAsset = resourceInfoAsset.resource.Find();
            if (resourceAsset != null)
            {
                _previewIconHandle = _iconStore.RequestIcon(resourceAsset, OnFoliageIconReady);
                return;
            }
        }
        else if (selectedAsset is FoliageInstancedMeshInfoAsset instancedMeshInfoAsset)
        {
            _previewIconHandle = _iconStore.RequestIcon(instancedMeshInfoAsset, OnFoliageIconReady);
            return;
        }

        _previewIconContainer.Text = "No Preview";
        _previewIconImage.UpdateTexture(null);
    }

    private void OnFoliageIconReady(int handle, Texture2D texture)
    {
        if (handle != -1 && _previewIconHandle != handle)
        {
            return;
        }

        _previewIconContainer.Text = string.Empty;
        _previewIconImage.UpdateTexture(texture);
    }

    private void OnIconGridButtonClicked(ISleekElement button)
    {
        _iconGridContainer.IsVisible = !_iconGridContainer.IsVisible;
        if (_iconGridContainer.IsVisible)
        {
            ShowIconGrid();
        }
    }

    /// <summary>
    /// Switches the foliage editor to Exact asset mode and opens the icon grid.
    /// </summary>
    public void ShowExactIconGrid()
    {
        _currentUIInstance.tool.mode = FoliageEditor.EFoliageMode.EXACT;
        _currentUIInstance.searchTypeButton.state = 0;
        ShowIconGrid();
    }

    private void ShowIconGrid()
    {
        _iconGridContainer.IsVisible = true;
        SyncGridData(force: true);
        _iconGridContainer.SizeOffset_Y = Mathf.Min(_preferredGridHeight, _iconGridScrollBox.ContentHeight);
    }

    private void SyncGridData(bool force = false)
    {
        List<FoliageInfoAsset> assets = BuildCurrentAssetList();

        int count = assets.Count;
        string first = count > 0 ? assets[0].GUID.ToString() : string.Empty;
        string last = count > 0 ? assets[count - 1].GUID.ToString() : string.Empty;

        if (!force && count == _lastGridCount && first == _lastGridFirst && last == _lastGridLast)
        {
            return;
        }

        _lastGridCount = count;
        _lastGridFirst = first;
        _lastGridLast = last;

        _iconGridScrollBox.SetData(assets);
        _iconGridContainer.SizeOffset_Y = Mathf.Min(_preferredGridHeight, _iconGridScrollBox.ContentHeight);
    }

    private List<FoliageInfoAsset> BuildCurrentAssetList()
    {
        List<FoliageInfoAsset> foliageAssets;

        if (_searchInfoAssets != null && _searchInfoAssets.Count > 0)
        {
            foliageAssets = new List<FoliageInfoAsset>(_searchInfoAssets);
        }
        else
        {
            foliageAssets = [];
            SDG.Unturned.Assets.find(foliageAssets);
        }

        foliageAssets.Sort((left, right) => string.Compare(left.name, right.name, System.StringComparison.OrdinalIgnoreCase));
        return foliageAssets;
    }

    private ISleekElement OnCreateGridAsset(FoliageInfoAsset item)
    {
        UIBuilder builder = new(0f, 0f);

        ISleekButton button = builder.CreateSimpleButton();
        button.OnClicked += OnGridAssetClicked;

        builder.SetAnchorHorizontal(0f)
            .SetAnchorVertical(0f)
            .SetOffsetHorizontal(12f)
            .SetOffsetVertical(7f);

        ISleekImage iconImage = Glazier.Get().CreateImage();
        iconImage.ShouldDestroyTexture = false;
        iconImage.SizeScale_X = 0.8f;
        iconImage.SizeScale_Y = 0.8f;

        builder.FormatElement(ref iconImage);
        button.AddChild(iconImage);

        builder.SetOffsetHorizontal(0f)
            .SetOffsetVertical(-12f)
            .SetText(item.FriendlyName);

        ISleekLabel label = builder.BuildLabel(TextAnchor.LowerCenter);
        label.SizeScale_X = 1f;
        label.SizeScale_Y = 1f;
        label.TextContrastContext = ETextContrastContext.ColorfulBackdrop;

        builder.FormatElement(ref label);
        button.AddChild(label);

        int iconHandle = -2;
        if (item is FoliageObjectInfoAsset objectInfoAsset)
        {
            ObjectAsset? objectAsset = objectInfoAsset.obj.Find();
            if (objectAsset != null)
            {
                iconHandle = _iconStore.RequestIcon(objectAsset, (handle, texture) =>
                {
                    if (handle != -1 && handle != iconHandle) return;
                    iconImage.UpdateTexture(texture);
                });
            }
        }
        else if (item is FoliageResourceInfoAsset resourceInfoAsset)
        {
            ResourceAsset? resourceAsset = resourceInfoAsset.resource.Find();
            if (resourceAsset != null)
            {
                iconHandle = _iconStore.RequestIcon(resourceAsset, (handle, texture) =>
                {
                    if (handle != -1 && handle != iconHandle) return;
                    iconImage.UpdateTexture(texture);
                });
            }
        }
        else if (item is FoliageInstancedMeshInfoAsset instancedMeshInfoAsset)
        {
            iconHandle = _iconStore.RequestIcon(instancedMeshInfoAsset, (handle, texture) =>
            {
                if (handle != -1 && handle != iconHandle) return;
                iconImage.UpdateTexture(texture);
            });
        }

        return button;
    }

    private void OnGridAssetClicked(ISleekElement button)
    {
        FoliageInfoAsset? selectedAsset = _iconGridScrollBox.GetItemFromVisibleElement(button);
        if (selectedAsset == null)
        {
            return;
        }

        _currentUIInstance.tool.selectedInstanceAsset = selectedAsset;
        _currentUIInstance.tool.selectedCollectionAsset = null;

        _lastSelectedAsset = selectedAsset;
        OnFoliageAssetSelected(selectedAsset);
    }

    private void OnResolutionUpdateIconGridSize()
    {
        if (GraphicsSettings.resolution.Width < 1500)
        {
            UpdateIconGridSize(725f, 500f);
        }
        else if (GraphicsSettings.resolution.Width < 1700)
        {
            UpdateIconGridSize(860f, 600f);
        }
        else
        {
            UpdateIconGridSize(1000f, 760f);
        }
    }

    private void UpdateIconGridSize(float newWidth, float newHeight)
    {
        _iconGridContainer.PositionOffset_X = -(newWidth / 2f);
        _iconGridContainer.SizeOffset_X = newWidth;
        _iconGridScrollBox.SizeOffset_X = newWidth;

        _preferredGridHeight = newHeight;

        if (_iconGridContainer.IsVisible)
        {
            _iconGridScrollBox.ForceRebuildElements();
        }

        _iconGridContainer.SizeOffset_Y = Mathf.Min(_preferredGridHeight, _iconGridScrollBox.ContentHeight);
    }

    public void Dispose()
    {
        _currentUIInstance.RemoveChild(_previewIconContainer);
        _currentUIInstance.RemoveChild(_iconGridButton);
        _currentUIInstance.RemoveChild(_iconGridContainer);

        _iconGridButton.onClickedButton -= OnIconGridButtonClicked;
        GraphicsSettings.graphicsSettingsApplied -= OnResolutionUpdateIconGridSize;
    }
}