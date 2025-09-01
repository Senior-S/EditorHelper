using System;
using DanielWillett.UITools.API.Extensions;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers;
using EditorHelper2.UI.Builders;
using SDG.Unturned;

namespace EditorHelper2.Extensions.Level.Objects;

[UIExtension(typeof(EditorTerrainDetailsUI))]
[EHExtension("Live Foliage Editor", "JienSultan")]
public class FoliageManagerExtension : UIExtension, IExtension
{
    private readonly SleekButtonIcon _densitySaveButton;
    private readonly ISleekFloat32Field _densityField;
    
    private EditorTerrainDetailsUI? _currentUIInstance;
    
    public FoliageManagerExtension(EditorTerrainDetailsUI instance)
    {
        _currentUIInstance = instance;
        UIBuilder builder = new(200f, 30f);

        builder.SetAnchorHorizontal(1f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-200f)
            .SetOffsetVertical(-30f)
            .SetSizeHorizontal(200)
            .SetSizeVertical(30);

        _densitySaveButton = builder.BuildButton("Write to file.");
        _densitySaveButton.onClickedButton += OnClickedButton;
        _densitySaveButton.text = "Save";

        builder.SetAnchorHorizontal(1f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-200f)
            .SetOffsetVertical(-70f)
            .SetSizeHorizontal(200)
            .SetSizeVertical(30);

        _densityField = builder.BuildFloatInput();
        _densityField.AddLabel("Density", ESleekSide.LEFT);
        _densityField.OnValueChanged += OnValueChanged;
        
        Initialize();
    }

    public void Initialize()
    {
        if (_currentUIInstance == null) return;
        _currentUIInstance.AddChild(_densitySaveButton);
        _currentUIInstance.AddChild(_densityField);
    }

    #region Event handlers
    
    private void OnClickedButton(ISleekElement button)
    {
        if (_currentUIInstance.tool.selectedInstanceAsset == null) return;

        _currentUIInstance.tool.selectedInstanceAsset.density = _densityField.Value;
        AssetWriter.SaveFoliageInfoAssetDensity(_currentUIInstance.tool.selectedInstanceAsset);
    }

    private void OnValueChanged(ISleekFloat32Field field, float value)
    {
        if (_currentUIInstance.tool.selectedInstanceAsset == null) return;

        _currentUIInstance.tool.selectedInstanceAsset.density = value;
    }

    #endregion Event handlers

    #region Extension Functions
    
    public void CustomUpdate()
    {
        bool visibility = _currentUIInstance.tool.mode != FoliageEditor.EFoliageMode.BAKE && _currentUIInstance.searchTypeButton.state == 0
            && _currentUIInstance.tool.selectedInstanceAsset != null;
        _densitySaveButton.IsVisible = visibility;
        _densityField.IsVisible = visibility;
        _currentUIInstance.assetScrollView.SizeOffset_Y = _densityField.IsVisible ? -200f : -120f;

        if (_currentUIInstance.tool.selectedInstanceAsset != null)
            _densityField.Value = _currentUIInstance.tool.selectedInstanceAsset.density;
        else _densityField.Value = 0;
    }

    #endregion Extension Functions

    public void Dispose()
    {
        if (_currentUIInstance == null) return;
        _currentUIInstance.RemoveChild(_densitySaveButton);
        _currentUIInstance.RemoveChild(_densityField);
    }
}