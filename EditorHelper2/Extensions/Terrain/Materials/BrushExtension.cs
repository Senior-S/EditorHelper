using System;
using System.Reflection;
using DanielWillett.UITools.API.Extensions;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Keybinds;
using EditorHelper2.UI.Builders;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Framework.Landscapes;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Terrain.Materials;

[UIExtension(typeof(EditorTerrainMaterialsUI))]
[EHExtension("Splatmap Extension", "JienSultan")]
[HarmonyPatch(typeof(TerrainEditor))]
public class BrushExtension : UIExtension, IExtension
{
    private static BrushExtension? _instance;    
    private static Harmony? _harmony;
    
    private readonly ISleekToggle _useHeightLimitsToggle;
    private readonly ISleekFloat32Field _heightMinField;
    private readonly ISleekFloat32Field _heightMaxField;
    private readonly ISleekLabel _brushPositionLabel;
    private EditorTerrainMaterialsUI? _currentUIInstance;
    private TerrainEditor? _terrainEditor;
    
    public bool UseHeightLimits => _useHeightLimitsToggle?.Value ?? false;
    public float MinHeight => _heightMinField?.Value ?? 80f;
    public float MaxHeight => _heightMaxField?.Value ?? 110f;
    
    public BrushExtension(EditorTerrainMaterialsUI instance)
    {
        _currentUIInstance = instance;
        
        UIBuilder builder = new(200f, 30f);

        // Use Height Limits Toggle
        builder.SetAnchorHorizontal(1f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-560f)
            .SetOffsetVertical(-150f)
            .SetSizeHorizontal(30f)
            .SetSizeVertical(30f);
        _useHeightLimitsToggle = builder.BuildToggle();
        _useHeightLimitsToggle.AddLabel("Use Height Limits", ESleekSide.RIGHT);

        // Height Min Field
        builder.SetAnchorHorizontal(1f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-560f)
            .SetOffsetVertical(-70f)
            .SetSizeHorizontal(200f)
            .SetSizeVertical(30f);
        _heightMinField = builder.BuildFloatInput();
        _heightMinField.AddLabel("Min Height", ESleekSide.RIGHT);
        _heightMinField.Value = 80f; // Set default value

        // Height Max Field
        builder.SetAnchorHorizontal(1f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-560f)
            .SetOffsetVertical(-110f)
            .SetSizeHorizontal(200f)
            .SetSizeVertical(30f);
        _heightMaxField = builder.BuildFloatInput();
        _heightMaxField.AddLabel("Max Height", ESleekSide.RIGHT);
        _heightMaxField.Value = 110f; // Set default value
        
        
        builder.SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-100f)
            .SetOffsetVertical(-50f)
            .SetSizeHorizontal(200f)
            .SetSizeVertical(30f);
        _brushPositionLabel = builder.BuildLabel();
        _brushPositionLabel.Text = "Brush Position: N/A";

        Initialize();
    }

    public void Initialize()
    {
        if (_currentUIInstance == null) return;
        _instance = this;
        _terrainEditor = EditorInteract.instance?.terrainTool;
        _useHeightLimitsToggle.Value = false;
        
        _useHeightLimitsToggle.OnValueChanged += OnUseHeightLimitsChanged;
        _heightMinField.OnValueChanged += OnHeightMinChanged;
        _heightMaxField.OnValueChanged += OnHeightMaxChanged;
        
        _currentUIInstance.AddChild(_useHeightLimitsToggle);
        _currentUIInstance.AddChild(_heightMinField);
        _currentUIInstance.AddChild(_heightMaxField);
        _currentUIInstance.AddChild(_brushPositionLabel);
    }

    #region Event handlers
    
    private void OnUseHeightLimitsChanged(ISleekToggle toggle, bool value)
    {
        UnturnedLog.info($"Height limits toggled: {value}");
    }

    private void OnHeightMinChanged(ISleekFloat32Field field, float value)
    {
        UnturnedLog.info($"Min height changed: {value}");
    }

    private void OnHeightMaxChanged(ISleekFloat32Field field, float value)
    {
        UnturnedLog.info($"Max height changed: {value}");
    }

    #endregion Event handlers

    #region Extension Functions

    public Vector3? GetBrushWorldPosition()
    {
        try
        {
            if (_terrainEditor == null)
                _terrainEditor = EditorInteract.instance?.terrainTool;
                
            if (_terrainEditor == null) return null;
            
            FieldInfo? field = typeof(TerrainEditor).GetField("brushWorldPosition", 
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
                return (Vector3)field.GetValue(_terrainEditor);
        }
        catch (Exception ex)
        {
            UnturnedLog.error($"Error getting brush position: {ex.Message}");
        }
        
        return null;
    }
    
    // Harmony patch - this intercepts each individual paint pixel
    [HarmonyPatch(typeof(TerrainEditor), "handleSplatmapWritePaint")]
    [HarmonyPrefix]
    [UsedImplicitly]
    public static bool HandleSplatmapWritePaintPrefix(
        LandscapeCoord tileCoord, 
        SplatmapCoord splatmapCoord, 
        Vector3 worldPosition, 
        float[] currentWeights)
    {
        // Check if our extension is active and height limits are enabled
        if (_instance?.UseHeightLimits != true)
            return true; // Continue with original method
        
        // This is the actual position being painted
        float height = worldPosition.y;
        
        if (height < _instance.MinHeight || height > _instance.MaxHeight)
        {
            return false; // Skip painting at this position
        }
        
        return true; // Continue with original method
    }
    
    public void CustomUpdate()
    {
        if (TerrainEditor.toolMode == TerrainEditor.EDevkitLandscapeToolMode.SPLATMAP)
        {
            Vector3? brushPos = GetBrushWorldPosition();
            if (brushPos.HasValue)
            {
                _brushPositionLabel.Text = $"Brush: {brushPos.Value:F1}";
                
                if (UseHeightLimits)
                {
                    bool withinLimits = brushPos.Value.y >= MinHeight && brushPos.Value.y <= MaxHeight;
                    _brushPositionLabel.TextColor = withinLimits ? Color.green : Color.red;
                }
                else
                {
                    _brushPositionLabel.TextColor = Color.white;
                }
            }
        }

        if (KeybindManager.IsHeld(KeybindIds.BrushSetMinHeight))
        {
            _heightMinField.Value = GetBrushWorldPosition().Value.y;
        }
        
        if (KeybindManager.IsHeld(KeybindIds.BrushSetMaxHeight))
        {
            _heightMaxField.Value = GetBrushWorldPosition().Value.y;
        }
        
    }
    
    #endregion Extension Functions

    public void Dispose()
    {
        _instance = null;
        
        if (_currentUIInstance == null) return;
        _currentUIInstance.RemoveChild(_useHeightLimitsToggle);
        _currentUIInstance.RemoveChild(_heightMinField);
        _currentUIInstance.RemoveChild(_heightMaxField);
        _currentUIInstance.RemoveChild(_brushPositionLabel);
    }
}
