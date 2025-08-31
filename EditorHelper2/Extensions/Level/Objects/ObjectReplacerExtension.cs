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
[EHExtension("Object replacer extension", "Senior S")]
public class ObjectReplacerExtension : UIExtension, IExtension
{
    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;

    private bool _active = false;
    private readonly ISleekBox _replacerMenu;
    private readonly SleekButtonIcon _replacerMenuButton;
    private readonly ISleekField _fromReplaceField;
    private readonly ISleekField _toReplaceField;
    private readonly SleekButtonIcon _replaceButton;
    
    public ObjectReplacerExtension()
    {
        UIBuilder builder = new(200f, 250f);
        
        Assembly assembly = typeof(EditorHelper).Assembly; 
        string iconsPath = Path.Combine(Path.GetDirectoryName(assembly.Location) ?? string.Empty, "Assets" ,"Icons.unity3d"); 
        Bundle icons = Bundles.getBundle(iconsPath, false);

        builder.SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-100f)
            .SetOffsetVertical(1000f);

        _replacerMenu = builder.BuildBox();

        builder.ResetProperties()
            .SetAnchorHorizontal(0.5f)
            .SetOffsetVertical(10f)
            .SetText("Object replacer")
            .SetOffsetHorizontal(-80f)
            .SetSizeHorizontal(160f)
            .SetSizeVertical(20f);

        ISleekLabel titleLabel = builder.BuildLabel();
        _replacerMenu.AddChild(titleLabel);

        builder.SetSpacing(40f)
            .SetText("Replace");
        
        ISleekLabel fromReplaceLabel = builder.BuildLabel();
        _replacerMenu.AddChild(fromReplaceLabel);
        
        builder.SetSpacing(25f)
            .SetSizeVertical(30f);
        _fromReplaceField = builder.BuildStringField();
        _fromReplaceField.IsClickable = false;
        Color color = GlazierConst.DefaultFieldBackgroundColor;
        color.a *= 4f;
        _fromReplaceField.BackgroundColor = color;
        _replacerMenu.AddChild(_fromReplaceField);
        
        builder.SetSpacing(55f)
            .SetSizeVertical(20f)
            .SetText("With");
        
        ISleekLabel toReplaceLabel = builder.BuildLabel();
        _replacerMenu.AddChild(toReplaceLabel);
        
        builder.SetSpacing(25f)
            .SetSizeVertical(30f);
        _toReplaceField = builder.BuildStringField();
        _toReplaceField.BackgroundColor = color;
        _toReplaceField.IsClickable = false;
        _replacerMenu.AddChild(_toReplaceField);

        builder.SetAnchorVertical(1f)
            .SetOffsetHorizontal(-90f)
            .SetOffsetVertical(-40f)
            .SetSizeHorizontal(180f)
            .SetSizeVertical(35f)
            .SetText("Replace");

        _replaceButton = builder.BuildButton("Replace the objects");
        _replacerMenu.AddChild(_replaceButton);
        
        builder.SetAnchorHorizontal(1f)
            .SetAnchorVertical(1f)
            .SetSizeHorizontal(30f)
            .SetSizeVertical(30f)
            .SetOffsetHorizontal(-30f)
            .SetOffsetVertical(-30f);
        
        _replacerMenuButton = builder.BuildButton("Open the replacer menu", icons.load<Texture2D>("Both"));
        
        icons.unload();
        Initialize();
    }
    
    public void Initialize()
    {
        if (_container == null) return;
        
        _container.AddChild(_replacerMenu);
        _container.AddChild(_replacerMenuButton);

        _replacerMenuButton.onClickedButton += OnReplacerMenuButtonClicked;
        _replaceButton.onClickedButton += OnReplaceButtonClicked;
    }
    
    #region Event handlers
    private void OnReplacerMenuButtonClicked(ISleekElement button)
    {
        _active = !_active;
        
        _replacerMenu.AnimatePositionOffset(-100f, _active ? -315f : 800f, ESleekLerp.EXPONENTIAL, 15f);
    }
    
    private void OnReplaceButtonClicked(ISleekElement button)
    {
        if (EditorObjects.selection == null || EditorObjects.selection.Count != 1) return;
        Transform selectedObject = EditorObjects.selection.First().transform;
        LevelObject? levelObject = LevelObjects.FindLevelObject(selectedObject.gameObject);
        if (levelObject == null) return;
        ObjectAsset? selectedAsset = EditorObjects.selectedObjectAsset;
        if (selectedAsset == null) return;
        
        IEnumerable<LevelObject> levelObjects = LevelObjects.objects.Cast<List<LevelObject>>().SelectMany(list => list);
        levelObjects = levelObjects.Where(c => c.GUID == levelObject.GUID).ToList();
        
        EditorObjects.clearSelection();
        foreach (LevelObject obj in levelObjects)
        {
            _ = LevelObjects.registerAddObject(obj.transform.position, obj.transform.rotation, Vector3.one, EditorObjects.selectedObjectAsset, EditorObjects.selectedItemAsset);
            LevelObjects.registerRemoveObject(obj.transform);
        }
    }
    #endregion Event handlers
    
    #region Extension Functions
    public void ChangeButtonsVisibility(bool visible)
    {
        _replacerMenuButton.IsVisible = visible;

        if (!_active) return;
        
        if (visible)
        {
            if (EditorObjects.selection == null || EditorObjects.selection.Count != 1)
            {
                _replaceButton.isClickable = false;
                return;
            }
            Transform selectedObject = EditorObjects.selection.First().transform;
            LevelObject? levelObject = LevelObjects.FindLevelObject(selectedObject.gameObject);
            if (levelObject == null)
            {
                _replaceButton.isClickable = false;
                return;
            }
            _fromReplaceField.Text = levelObject.asset.FriendlyName;
                
            ObjectAsset? selectedAsset = EditorObjects.selectedObjectAsset;
            if (selectedAsset == null)
            {
                _replaceButton.isClickable = false;
                return;
            }
            _toReplaceField.Text = selectedAsset.FriendlyName;
                
            _replaceButton.isClickable = true;
        }
        else
        {
            _active = false;
            _replacerMenu.AnimatePositionOffset(-100f, 800f, ESleekLerp.EXPONENTIAL, 15f);
        }
    }
    #endregion Extension Functions
    
    public void Dispose()
    {
        if (_container == null) return;
        
        _container.RemoveChild(_replacerMenu);
        _container.RemoveChild(_replacerMenuButton);
        
        _replacerMenuButton.onClickedButton -= OnReplacerMenuButtonClicked;
        _replaceButton.onClickedButton -= OnReplaceButtonClicked;
    }
}