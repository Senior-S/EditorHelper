using DanielWillett.UITools;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers;
using EditorHelper2.Patches.Editor.UI;
using EditorHelper2.UI.Builders;
using EditorHelper2.UI.Elements;
using HarmonyLib;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace EditorHelper2.Extensions.Level.Objects;

[UIExtension(typeof(EditorLevelObjectsUI))]
[EHExtension("Object Icons Extension", "Gamingtoday093")]
public class IconsExtension : UIExtension, IExtension
{
    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;

    private readonly IconStore IconStore;

    private readonly ISleekBox _objectPreviewIconContainer;
    private readonly ISleekImage _objectPreviewIconImage;
    private int ObjectPreviewIconHandle;

    [ExistingMember("assets")]
    private readonly List<Asset>? _assets;

    private readonly SleekButtonIcon _objectIconGridButton;
    private readonly ISleekBox _objectIconGridContainer;
    private readonly SleekGrid<Asset> _objectIconGridScrollBox;
    private float PreferredObjectIconGridHeight = 760f;

    public IconsExtension()
    {
        IconStore = new IconStore(width: 300, height: 300);

        // Preview Icon

        UIBuilder builder = new(150f, 150f);

        builder.SetAnchorHorizontal(1f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-385f)
            .SetOffsetVertical(-150f)
            .SetText("Object Icon");

        _objectPreviewIconContainer = builder.BuildBox();
        _objectPreviewIconContainer.TextColor = new Color(0.35f, 0.35f, 0.35f, 0.35f);

        builder.SetOffsetHorizontal(-145f)
            .SetOffsetVertical(-145f)
            .SetSizeHorizontal(140f)
            .SetSizeVertical(140f);

        _objectPreviewIconImage = Glazier.Get().CreateImage();
        _objectPreviewIconImage.ShouldDestroyTexture = false;

        builder.FormatElement(ref _objectPreviewIconImage);

        _objectPreviewIconContainer.AddChild(_objectPreviewIconImage);

        // Grid View

        builder.ResetProperties()
            .SetAnchorHorizontal(1f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-385f)
            .SetOffsetVertical(-190f)
            .SetSizeHorizontal(150f)
            .SetSizeVertical(30f)
            .SetText("Grid View");

        Bundle bundle = Bundles.getBundle("/Bundles/Textures/Edit/Icons/EditorEnvironment/EditorEnvironment.unity3d");
        _objectIconGridButton = builder.BuildButton("View objects in a grid with icons", bundle.load<Texture2D>("Navigation"));
        bundle.unload();

        builder.SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(0f)
            .SetOffsetHorizontal(-500f)
            .SetOffsetVertical(0f)
            .SetSizeHorizontal(1000f)
            .SetSizeVertical(PreferredObjectIconGridHeight)
            .SetText(string.Empty);

        _objectIconGridContainer = builder.BuildAlphaBox();
        _objectIconGridContainer.IsVisible = false;

        builder.SetAnchorHorizontal(0f)
            .SetAnchorVertical(0f)
            .SetOffsetHorizontal(0f)
            .SetOffsetVertical(0f)
            .SetSizeHorizontal(1000f)
            .SetSizeVertical(0f)
            .SetScaleVertical(1f);

        _objectIconGridScrollBox = new SleekGrid<Asset>
        {
            itemSize = 125,
            itemPadding = 15,
            OnCreateElement = OnCreateGridObject
        };

        builder.FormatElement(ref _objectIconGridScrollBox);

        _objectIconGridContainer.AddChild(_objectIconGridScrollBox);

        Initialize();
    }

    public void Initialize()
    {
        if (_container == null) return;

        _container.AddChild(_objectPreviewIconContainer);
        _container.AddChild(_objectIconGridButton);
        _container.AddChild(_objectIconGridContainer);

        OnResolutionUpdateIconGridSize();

        _objectIconGridButton.onClickedButton += OnObjectIconGridButton;
        EditorLevelObjectsUIPatches.OnObjectAssetSelected += OnObjectAssetSelected;
        EditorLevelObjectsUIPatches.OnObjectBrowserUpdated += OnUpdateObjectBrowser;
        GraphicsSettings.graphicsSettingsApplied += OnResolutionUpdateIconGridSize;
    }

    public void CustomUpdate() => IconStore.CustomUpdate();

    #region Object Preview Icon
    private void OnObjectAssetSelected()
    {
        if (EditorObjects.selectedItemAsset != null)
            ObjectPreviewIconHandle = IconStore.RequestIcon(EditorObjects.selectedItemAsset, OnObjectIconReady);
        else if (EditorObjects.selectedObjectAsset != null)
            ObjectPreviewIconHandle = IconStore.RequestIcon(EditorObjects.selectedObjectAsset, OnObjectIconReady);
        else
            _objectPreviewIconContainer.Text = "Object Icon";
    }

    private void OnObjectIconReady(int handle, Texture2D texture)
    {
        if (handle != -1 && ObjectPreviewIconHandle != handle) return;

        _objectPreviewIconContainer.Text = string.Empty;
        _objectPreviewIconImage.UpdateTexture(texture);
    }
    #endregion

    #region Icon Grid
    private void OnObjectIconGridButton(ISleekElement button)
    {
        _objectIconGridContainer.IsVisible = !_objectIconGridContainer.IsVisible;
        if (_objectIconGridContainer.IsVisible)
        {
            if (_assets != null) _objectIconGridScrollBox.SetData(_assets);
            _objectIconGridContainer.SizeOffset_Y = Mathf.Min(PreferredObjectIconGridHeight, _objectIconGridScrollBox.ContentHeight);

            SchematicsExtension? schematicsExtension = UnturnedUIToolsNexus.UIExtensionManager.GetInstance<SchematicsExtension>();
            schematicsExtension?.HideSchematicsContainer();
        }
    }

    public void HideIconGridContainer() => _objectIconGridContainer.IsVisible = false;

    private void OnResolutionUpdateIconGridSize()
    {
        if (GraphicsSettings.resolution.Width < 1500)
            UpdateIconGridSize(725f, 500f);
        else if (GraphicsSettings.resolution.Width < 1700)
            UpdateIconGridSize(860f, 600f);
        else
            UpdateIconGridSize(1000f, 760f);
    }

    private void UpdateIconGridSize(float newWidth, float newHeight)
    {
        _objectIconGridContainer.PositionOffset_X = -(newWidth / 2f);
        _objectIconGridContainer.SizeOffset_X = newWidth;
        _objectIconGridScrollBox.SizeOffset_X = newWidth;

        PreferredObjectIconGridHeight = newHeight;

        if (_objectIconGridContainer.IsVisible)
            _objectIconGridScrollBox.ForceRebuildElements();

        _objectIconGridContainer.SizeOffset_Y = Mathf.Min(PreferredObjectIconGridHeight, _objectIconGridScrollBox.ContentHeight);
    }

    private ISleekElement OnCreateGridObject(Asset item)
    {
        UIBuilder builder = new(0, 0);

        ISleekButton button = builder.CreateSimpleButton();
        button.OnClicked += OnGridObjectClicked;

        builder.SetAnchorHorizontal(0f)
            .SetAnchorVertical(0f)
            .SetOffsetHorizontal(12f)
            .SetOffsetVertical(7f);

        ISleekImage objectIcon = Glazier.Get().CreateImage();
        objectIcon.ShouldDestroyTexture = false;
        objectIcon.SizeScale_X = 0.8f;
        objectIcon.SizeScale_Y = 0.8f;
        
        builder.FormatElement(ref objectIcon);
        
        button.AddChild(objectIcon);

        builder.SetOffsetHorizontal(0f)
            .SetOffsetVertical(-12f)
            .SetText(item.FriendlyName);

        ISleekLabel objectLabel = builder.BuildLabel(TextAnchor.LowerCenter);
        objectLabel.SizeScale_X = 1f;
        objectLabel.SizeScale_Y = 1f;
        objectLabel.TextContrastContext = ETextContrastContext.ColorfulBackdrop;

        builder.FormatElement(ref objectLabel);

        button.AddChild(objectLabel);

        int iconHandle = -2;
        if (item is ItemAsset itemAsset)
            iconHandle = IconStore.RequestIcon(itemAsset, (handle, texture) =>
            {
                if (handle != -1 && handle != iconHandle) return;
                objectIcon.UpdateTexture(texture);
            });
        else if (item is ObjectAsset objectAsset)
            iconHandle = IconStore.RequestIcon(objectAsset, (handle, texture) =>
            {
                if (handle != -1 && handle != iconHandle) return;
                objectIcon.UpdateTexture(texture);
            });

        return button;
    }

    public void OnUpdateObjectBrowser(List<Asset> objectAssets)
    {
        if (!_objectIconGridContainer.IsVisible) return;

        _objectIconGridScrollBox.SetData(objectAssets);
        _objectIconGridContainer.SizeOffset_Y = Mathf.Min(PreferredObjectIconGridHeight, _objectIconGridScrollBox.ContentHeight);
    }

    private void OnGridObjectClicked(ISleekElement button)
    {
        Asset? objectAsset = _objectIconGridScrollBox.GetItemFromVisibleElement(button);
        if (objectAsset == null) return;

        EditorLevelObjectsUIPatches.SetSelectedObjectAsset(objectAsset);
    }
    #endregion

    public void Dispose()
    {
        if (_container == null) return;

        _container.RemoveChild(_objectPreviewIconContainer);
        _container.RemoveChild(_objectIconGridButton);
        _container.RemoveChild(_objectIconGridContainer);

        _objectIconGridButton.onClickedButton -= OnObjectIconGridButton;
        EditorLevelObjectsUIPatches.OnObjectAssetSelected -= OnObjectAssetSelected;
        EditorLevelObjectsUIPatches.OnObjectBrowserUpdated -= OnUpdateObjectBrowser;
        GraphicsSettings.graphicsSettingsApplied -= OnResolutionUpdateIconGridSize;
    }
}
