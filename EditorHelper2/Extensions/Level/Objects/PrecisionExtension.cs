using System;
using System.Linq;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.API.Attributes;
using EditorHelper2.Patches.Editor;
using EditorHelper2.UI.Builders;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Level.Objects;

[UIExtension(typeof(EditorLevelObjectsUI))]
[EHExtension("Precision Extension", "Senior S")]
public sealed class PrecisionExtension : UIExtension, IDisposable
{
    // An instance isn't required but recommended if other extensions may interact with yours
    public static PrecisionExtension? Instance;

    private readonly ISleekLabel _objectPositionLabel;
    private readonly ISleekFloat32Field _objectPositionX;
    private readonly ISleekFloat32Field _objectPositionY;
    private readonly ISleekFloat32Field _objectPositionZ;
    
    private readonly ISleekLabel _objectRotationLabel;
    private readonly ISleekFloat32Field _objectRotationX;
    private readonly ISleekFloat32Field _objectRotationY;
    private readonly ISleekFloat32Field _objectRotationZ;

    private readonly ISleekLabel _objectScaleLabel;
    private readonly ISleekFloat32Field _objectScaleX;
    private readonly ISleekFloat32Field _objectScaleY;
    private readonly ISleekFloat32Field _objectScaleZ;
    
    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;

    public PrecisionExtension()
    {
        Instance = this;
        UIBuilder builder = new(120f, 30f);
        
        builder.SetOffsetHorizontal(20f)
            .SetOffsetVertical(-390f)
            .SetText("X:");
        _objectPositionX = builder.BuildFloatInput();
        _objectPositionX.OnValueChanged += OnPositionValueUpdated;

        builder.SetSpacing(0f)
            .SetOffsetHorizontal(170f)
            .SetText("Y:");
        _objectPositionY = builder.BuildFloatInput();
        _objectPositionY.OnValueChanged += OnPositionValueUpdated;

        builder.SetOffsetHorizontal(320f)
            .SetText("Z:");
        _objectPositionZ = builder.BuildFloatInput();
        _objectPositionZ.OnValueChanged += OnPositionValueUpdated;
        
        builder.SetSpacing(25f)
            .SetOffsetHorizontal(5f)
            .SetText("Position");
        _objectPositionLabel = builder.BuildLabel(TextAnchor.MiddleLeft);
        
        builder.SetOffsetHorizontal(20f)
            .SetText("X:");
        _objectRotationX = builder.BuildFloatInput();
        _objectRotationX.OnValueChanged += OnRotationValueUpdated;

        builder.SetSpacing(0f)
            .SetOffsetHorizontal(170f)
            .SetText("Y:");
        _objectRotationY = builder.BuildFloatInput();
        _objectRotationY.OnValueChanged += OnRotationValueUpdated;
        
        builder.SetOffsetHorizontal(320f)
            .SetText("Z:");
        _objectRotationZ = builder.BuildFloatInput();
        _objectRotationZ.OnValueChanged += OnRotationValueUpdated;
        
        builder
            .SetSpacing(25f)
            .SetOffsetHorizontal(5f)
            .SetText("Rotation");
        _objectRotationLabel = builder.BuildLabel(TextAnchor.MiddleLeft);

        builder.SetText("X:")
            .SetOffsetHorizontal(20f);
        _objectScaleX = builder.BuildFloatInput();
        _objectScaleX.OnValueChanged += OnScaleValueUpdated;
        
        builder.SetSpacing(0f)
            .SetOffsetHorizontal(170f)
            .SetText("Y:");
        _objectScaleY = builder.BuildFloatInput();
        _objectScaleY.OnValueChanged += OnScaleValueUpdated;
        
        builder.SetOffsetHorizontal(320f)
            .SetText("Z:");
        _objectScaleZ = builder.BuildFloatInput();
        _objectScaleZ.OnValueChanged += OnScaleValueUpdated;
        
        builder.SetSpacing(25f)
            .SetOffsetHorizontal(5f)
            .SetText("Scale");
        _objectScaleLabel = builder.BuildLabel(TextAnchor.MiddleLeft);

        Initialize();
    }

    private void Initialize()
    {
        if (_container == null) return;
        _container.AddChild(_objectPositionLabel);
        _container.AddChild(_objectPositionX);
        _container.AddChild(_objectPositionY);
        _container.AddChild(_objectPositionZ);
        _container.AddChild(_objectRotationLabel);
        _container.AddChild(_objectRotationX);
        _container.AddChild(_objectRotationY);
        _container.AddChild(_objectRotationZ);
        _container.AddChild(_objectScaleLabel);
        _container.AddChild(_objectScaleX);
        _container.AddChild(_objectScaleY);
        _container.AddChild(_objectScaleZ);
        
        EditorObjectsPatches.OnCalculateHandleOffsets += HandleCalculatedOffsets;
    }

    private void HandleCalculatedOffsets(EditorObjects obj)
    {
        if (EditorObjects.selection == null || EditorObjects.selection.Count != 1) return;
        Transform selectedObject = EditorObjects.selection.First().transform;
        
        _objectPositionX.Value = selectedObject.position.x;
        _objectPositionY.Value = selectedObject.position.y;
        _objectPositionZ.Value = selectedObject.position.z;

        _objectRotationX.Value = selectedObject.rotation.eulerAngles.x;
        _objectRotationY.Value = selectedObject.rotation.eulerAngles.y;
        _objectRotationZ.Value = selectedObject.rotation.eulerAngles.z;

        _objectScaleX.Value = selectedObject.localScale.x;
        _objectScaleY.Value = selectedObject.localScale.y;
        _objectScaleZ.Value = selectedObject.localScale.z;
    }
    
    public void ChangeButtonsVisibility(bool visible)
    {
        _objectPositionLabel.IsVisible = visible;
        _objectPositionX.IsVisible = visible;
        _objectPositionY.IsVisible = visible;
        _objectPositionZ.IsVisible = visible;

        _objectRotationLabel.IsVisible = visible;
        _objectRotationX.IsVisible = visible;
        _objectRotationY.IsVisible = visible;
        _objectRotationZ.IsVisible = visible;

        _objectScaleLabel.IsVisible = visible;
        _objectScaleX.IsVisible = visible;
        _objectScaleY.IsVisible = visible;
        _objectScaleZ.IsVisible = visible;
    }

    private void OnPositionValueUpdated(ISleekFloat32Field field, float value)
    {
        if (EditorObjects.selection == null || EditorObjects.selection.Count != 1) return;
        Transform selectedObject = EditorObjects.selection.First().transform;
        LevelObject? levelObject = LevelObjects.FindLevelObject(selectedObject.gameObject);
        if (levelObject == null) return;

        Vector3 position = selectedObject.position;

        // Probably there's a better way of checking this
        if (field == _objectPositionX)
        {
            if (_objectPositionX.Value > SDG.Unturned.Level.size || _objectPositionX.Value < SDG.Unturned.Level.size * -1)
            {
                _objectPositionX.Value = position.x;
                return;
            }
            
            position.x = _objectPositionX.Value;
        }
        else if (field == _objectPositionY)
        {
            if (_objectPositionY.Value > SDG.Unturned.Level.size || _objectPositionY.Value < SDG.Unturned.Level.size * -1)
            {
                _objectPositionY.Value = position.y;
                return;
            }
            
            position.y = _objectPositionY.Value;
        }
        else if (field == _objectPositionZ)
        {
            if (_objectPositionZ.Value > SDG.Unturned.Level.size || _objectPositionZ.Value < SDG.Unturned.Level.size * -1)
            {
                _objectPositionZ.Value = position.z;
                return;
            }
            
            position.z = _objectPositionZ.Value;
        }
        
        selectedObject.position = position;
        EditorObjects.calculateHandleOffsets();
    }
    
    private void OnRotationValueUpdated(ISleekFloat32Field field, float value)
    {
        if (EditorObjects.selection == null || EditorObjects.selection.Count != 1) return;
        Transform selectedObject = EditorObjects.selection.First().transform;
        LevelObject? levelObject = LevelObjects.FindLevelObject(selectedObject.gameObject);
        if (levelObject == null) return;

        Vector3 rotation = selectedObject.rotation.eulerAngles;

        // Probably there's a better way of checking this
        if (field == _objectRotationX)
        {
            rotation.x = _objectRotationX.Value;
        }
        else if (field == _objectRotationY)
        {
            rotation.y = _objectRotationY.Value;
        }
        else if (field == _objectRotationZ)
        {
            rotation.z = _objectRotationZ.Value;
        }
        
        selectedObject.rotation = Quaternion.Euler(rotation);
        EditorObjects.calculateHandleOffsets();
    }
    
    private void OnScaleValueUpdated(ISleekFloat32Field field, float value)
    {
        if (EditorObjects.selection == null || EditorObjects.selection.Count != 1) return;
        Transform selectedObject = EditorObjects.selection.First().transform;
        LevelObject? levelObject = LevelObjects.FindLevelObject(selectedObject.gameObject);
        if (levelObject == null) return;

        Vector3 scale = selectedObject.localScale;

        // Probably there's a better way of checking this
        if (field == _objectScaleX)
        {
            scale.x = _objectScaleX.Value;
        }
        else if (field == _objectScaleY)
        {
            scale.y = _objectScaleY.Value;
        }
        else if (field == _objectScaleZ)
        {
            scale.z = _objectScaleZ.Value;
        }
        
        selectedObject.localScale = scale;
        EditorObjects.calculateHandleOffsets();
    }
    
    public void Dispose()
    {
        EditorObjectsPatches.OnCalculateHandleOffsets -= HandleCalculatedOffsets;
        _objectPositionX.OnValueChanged -= OnPositionValueUpdated;
        _objectPositionY.OnValueChanged -= OnPositionValueUpdated;
        _objectPositionZ.OnValueChanged -= OnPositionValueUpdated;
        _objectRotationX.OnValueChanged -= OnRotationValueUpdated;
        _objectRotationY.OnValueChanged -= OnRotationValueUpdated;
        _objectRotationZ.OnValueChanged -= OnRotationValueUpdated;
        _objectScaleX.OnValueChanged -= OnScaleValueUpdated;
        _objectScaleY.OnValueChanged -= OnScaleValueUpdated;
        _objectScaleZ.OnValueChanged -= OnScaleValueUpdated;

        Instance = null;
        if (_container == null) return;
        
        _container.RemoveChild(_objectPositionLabel);
        _container.RemoveChild(_objectPositionX);
        _container.RemoveChild(_objectPositionY);
        _container.RemoveChild(_objectPositionZ);
        _container.RemoveChild(_objectRotationLabel);
        _container.RemoveChild(_objectRotationX);
        _container.RemoveChild(_objectRotationY);
        _container.RemoveChild(_objectRotationZ);
        _container.RemoveChild(_objectScaleLabel);
        _container.RemoveChild(_objectScaleX);
        _container.RemoveChild(_objectScaleY);
        _container.RemoveChild(_objectScaleZ);
    }
}