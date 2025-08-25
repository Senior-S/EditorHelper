using System.Collections.Generic;
using System.Linq;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers.Level.Objects;
using EditorHelper2.common.Types;
using EditorHelper2.Patches.Editor;
using EditorHelper2.UI.Builders;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Level.Objects;

[UIExtension(typeof(EditorLevelObjectsUI))]
[EHExtension("Extra Object Tools (Adjacent placement, Layer selection toggle, Object tag)", "Senior S")]
public class ExtrasExtension : UIExtension, IExtension
{
    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;
    
    private readonly SleekButtonIcon _adjacentPlaceButton;
    
    public int ObjectsLayerMask { get; private set; } = LayerMask.GetMask("Large", "Medium", "Small", "Barricade", "Structure");
    private readonly ISleekBox _layersContainer;
    private readonly Dictionary<ISleekToggle, string> _toggleToLayer = new();
    private readonly SleekButtonIcon _layersMaskButton;
    
    private readonly ISleekField _tagField;
    private readonly Dictionary<LevelObject, LevelObjectExtension> _dicLevelObjects;
    
    public ExtrasExtension()
    {
        UIBuilder builder = new(150f, 30f);
        
        builder.SetAnchorVertical(1f)
            .SetOffsetHorizontal(360f)
            .SetOffsetVertical(-70f)
            .SetText("Place adjacent");
        _adjacentPlaceButton = builder.BuildButton("Place the selected object adjacent to the world selected object");
        
        builder.SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(1)
            .SetOffsetHorizontal(-125f)
            .SetOffsetVertical(-210f)
            .SetSizeHorizontal(250f)
            .SetSizeVertical(160f)
            .SetText("");

        _layersContainer = builder.BuildBox();
        
        builder.SetAnchorVertical(0.5f)
            .SetSizeHorizontal(40f)
            .SetSizeVertical(40f)
            .SetOffsetHorizontal(-120f)
            .SetOffsetVertical(-60f);
        
        List<ISleekToggle> toggles = 
        [
            builder.SetText("Large").BuildToggle(),
            builder.SetText("Medium").BuildToggle()
        ];
        builder.SetOffsetHorizontal(5f)
            .SetOffsetVertical(-60f);
        toggles.AddRange([
            builder.SetText("Small").BuildToggle(),
            builder.SetText("Structure").BuildToggle()
        ]);
        builder.SetOffsetHorizontal(-60f)
            .SetOffsetVertical(30f);
        toggles.Add(builder.SetText("Barricade").BuildToggle());
        
        _toggleToLayer[toggles[0]] = "Large";
        _toggleToLayer[toggles[1]] = "Medium";
        _toggleToLayer[toggles[2]] = "Small";
        _toggleToLayer[toggles[3]] = "Structure";
        _toggleToLayer[toggles[4]] = "Barricade";
        
        foreach (ISleekToggle toggle in toggles)
        {
            _layersContainer.AddChild(toggle);
            toggle.OnValueChanged += OnLayerToggleChanged;
        }

        _layersContainer.IsVisible = false;
        
        builder.SetAnchorHorizontal(1f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-230f)
            .SetOffsetVertical(EditorLevelObjectsUI.assetsScrollBox.SizeOffset_Y + 200f)
            .SetSizeHorizontal(200)
            .SetSizeVertical(30)
            .SetText("Change layer mask");
        
        _layersMaskButton = builder.BuildButton("Change the layer mask that determines what can be selected");
        
        builder.SetOffsetVertical(EditorLevelObjectsUI.assetsScrollBox.SizeOffset_Y + 120f)
            .SetText("Object tag");
        
        _tagField = builder.BuildStringField();
        _tagField.IsVisible = false;
        _dicLevelObjects = [];
        
        Initialize();
    }
    
    public void Initialize()
    {
        if (_container == null) return;
        
        EditorLevelObjectsUI.assetsScrollBox.SizeOffset_Y -= 40f;
        
        _container.AddChild(_layersContainer);
        _container.AddChild(_layersMaskButton);
        _container.AddChild(_adjacentPlaceButton);
        _container.AddChild(_tagField);
        
        foreach (ISleekToggle toggle in _toggleToLayer.Keys)
        {
            toggle.OnValueChanged += OnLayerToggleChanged;
        }
        _layersMaskButton.onClickedButton += OnLayersMaskButtonClicked;
        _adjacentPlaceButton.onClickedButton += OnAdjacentPlaceClicked;
        _tagField.OnTextChanged += OnTagFieldTextChanged;
        EditorObjectsPatches.OnObjectTransformSelected += OnObjectTransformSelected;
    }
    
    #region Event Handlers
    private void OnLayerToggleChanged(ISleekToggle toggle, bool state)
    {
        ObjectsLayerMask = _toggleToLayer.Where(kvp => kvp.Key.Value).Aggregate(0, (current, kvp) => current | 1 << LayerMask.NameToLayer(kvp.Value));;
    }
    
    private void OnLayersMaskButtonClicked(ISleekElement button)
    {
        _layersContainer.IsVisible = !_layersContainer.IsVisible;
    }
    
    private void OnAdjacentPlaceClicked(ISleekElement button)
    {
        if (EditorObjects.selection == null || EditorObjects.selection.Count != 1) return;
        Transform selectedObject = EditorObjects.selection.First().transform;
        
        LevelObject? levelObject = LevelObjects.FindLevelObject(selectedObject.gameObject);
        if (levelObject == null) return;

        Vector3 chosenDirection = ObjectsHelper.GetObjectFace(levelObject.transform);
        
        Bounds bounds = ObjectsHelper.GetObjectBounds(levelObject.transform);
        
        if (EditorObjects.selectedObjectAsset == null && EditorObjects.selectedItemAsset == null)
        {
            EditorObjects.selectedObjectAsset = levelObject.asset;
            EditorObjects.selectedItemAsset = null;
        }
        
        Vector3 point;
        if (EditorObjects.selectedObjectAsset == levelObject.asset)
        {
            float boundSize = Mathf.Abs(Vector3.Dot(chosenDirection, levelObject.transform.right)) > 0.5f
                ? bounds.size.x
                : bounds.size.y;
            Vector3 offset = chosenDirection * boundSize;
            point = levelObject.transform.position + offset;            
        }
        else
        {
            // https://stackoverflow.com/questions/58089093/place-an-object-on-the-right-side-of-another-object-in-unity
            float boundSize = Mathf.Abs(Vector3.Dot(chosenDirection, levelObject.transform.right)) > 0.5f
                ? bounds.extents.x
                : bounds.extents.y;
            
            Transform targetTransform = EditorObjects.selectedObjectAsset!.GetOrLoadModel().transform;
            Bounds targetBound = ObjectsHelper.GetObjectBounds(targetTransform);
            float targetBoundSize = Mathf.Abs(Vector3.Dot(chosenDirection, targetTransform.right)) > 0.5f
                ? targetBound.extents.x
                : targetBound.extents.y;
            Vector3 offset = chosenDirection * (boundSize + targetBoundSize);
            point = levelObject.transform.position + offset;
        }
        
        EditorObjects.handles.SetPreferredPivot(point, selectedObject.rotation);
        LevelObjects.step++;
        Transform transform = LevelObjects.registerAddObject(point, selectedObject.rotation, Vector3.one, EditorObjects.selectedObjectAsset, EditorObjects.selectedItemAsset);
        if (!transform) return;
        
        EditorObjects.clearSelection();
        EditorObjects.addSelection(transform);
    }
    
    private void OnTagFieldTextChanged(ISleekField field, string text)
    {
        if (EditorObjects.selection == null || EditorObjects.selection.Count != 1) return;
        Transform selectedObject = EditorObjects.selection.First().transform;
        
        LevelObject? levelObject = LevelObjects.FindLevelObject(selectedObject.gameObject);
        if (levelObject == null) return;
        
        if (_dicLevelObjects.TryGetValue(levelObject, out LevelObjectExtension extension))
        {
            extension?.UpdateTag(text);
        }
        else
        {
            _dicLevelObjects.Add(levelObject, new LevelObjectExtension(text));
        }
    }
    
    private void OnObjectTransformSelected(Transform obj)
    {
        if (EditorObjects.selection == null || EditorObjects.selection.Count != 1) return;
        Transform selectedObject = EditorObjects.selection.First().transform;
        
        LevelObject? levelObject = LevelObjects.FindLevelObject(selectedObject.gameObject);
        if (levelObject == null) return;
        
        string tag = string.Empty;
        if (_dicLevelObjects.TryGetValue(levelObject, out LevelObjectExtension extension))
        {
            tag = extension.Tag;
        }

        _tagField.Text = tag;
        
        if (!InputEx.GetKey(ControlsSettings.modify) && EditorObjects.selection.Count == 1 && tag != string.Empty && selectedObject != null)
        {
            List<KeyValuePair<LevelObject, LevelObjectExtension>> objects = _dicLevelObjects.Where(c => c.Value.Tag == tag).ToList();
            if (objects.Count > 1)
            {
                SelectSameTagObjects(objects.Select(c => c.Key.transform).ToList());
            }
        }
    }
    #endregion
    
    #region Extension functions
    public void ChangeButtonsVisibility(bool visible)
    {
        if (_tagField.IsVisible != visible)
        {
            _tagField.IsVisible = visible;
            if (visible)
            {
                EditorLevelObjectsUI.assetsScrollBox.SizeOffset_Y -= 40f;
            }
            else
            {
                EditorLevelObjectsUI.assetsScrollBox.SizeOffset_Y += 40f;    
            }
        }
    }
    
    private void SelectSameTagObjects(List<Transform> selectedObjects)
    {
        EditorObjects.selection.Clear();
        foreach (Transform select in selectedObjects)
        {
            HighlighterTool.highlight(select, Color.yellow);
            EditorObjects.selectDecals(select, true);
            EditorObjects.selection.Add(new EditorSelection(select, select.position, select.rotation, select.localScale));    
        }
        EditorObjects.calculateHandleOffsets();
    }
    #endregion

    public void Dispose()
    {
        if (_container == null) return;
        
        EditorLevelObjectsUI.assetsScrollBox.SizeOffset_Y += _tagField.IsVisible ? 80f : 40f;
        
        _container.RemoveChild(_layersContainer);
        _container.RemoveChild(_layersMaskButton);
        _container.RemoveChild(_adjacentPlaceButton);
        _container.RemoveChild(_tagField);
        
        foreach (ISleekToggle toggle in _toggleToLayer.Keys)
        {
            toggle.OnValueChanged -= OnLayerToggleChanged;
        }
        _layersMaskButton.onClickedButton -= OnLayersMaskButtonClicked;
        _adjacentPlaceButton.onClickedButton -= OnAdjacentPlaceClicked;
        _tagField.OnTextChanged -= OnTagFieldTextChanged;
        EditorObjectsPatches.OnObjectTransformSelected -= OnObjectTransformSelected;
    }
}