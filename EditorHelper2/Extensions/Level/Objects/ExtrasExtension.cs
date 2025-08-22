using System.Collections.Generic;
using System.Linq;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers.Level.Objects;
using EditorHelper2.UI.Builders;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Level.Objects;

[UIExtension(typeof(EditorLevelObjectsUI))]
[EHExtension("Extra Object Tools (Adjacent placement, Layer selection toggle)", "Senior S")]
public class ExtrasExtension : UIExtension, IExtension
{
    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;
    
    private readonly SleekButtonIcon _adjacentPlaceButton;
    
    public int ObjectsLayerMask { get; private set; } = LayerMask.GetMask("Large", "Medium", "Small", "Barricade", "Structure");
    private readonly ISleekBox _layersContainer;
    private readonly Dictionary<ISleekToggle, string> _toggleToLayer = new();
    private readonly SleekButtonIcon _layersMaskButton;
    
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
        
        Initialize();
    }
    
    public void Initialize()
    {
        if (_container == null) return;
        
        EditorLevelObjectsUI.assetsScrollBox.SizeOffset_Y -= 40f;
        
        _container.AddChild(_layersContainer);
        _container.AddChild(_layersMaskButton);
        _container.AddChild(_adjacentPlaceButton);
        
        foreach (ISleekToggle toggle in _toggleToLayer.Keys)
        {
            toggle.OnValueChanged += OnLayerToggleChanged;
        }
        _layersMaskButton.onClickedButton += OnLayersMaskButtonClicked;
        _adjacentPlaceButton.onClickedButton += OnAdjacentPlaceClicked;
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
    #endregion

    public void Dispose()
    {
        if (_container == null) return;
        
        EditorLevelObjectsUI.assetsScrollBox.SizeOffset_Y += 40f;
        
        _container.RemoveChild(_layersContainer);
        _container.RemoveChild(_layersMaskButton);
        _container.RemoveChild(_adjacentPlaceButton);
        
        foreach (ISleekToggle toggle in _toggleToLayer.Keys)
        {
            toggle.OnValueChanged -= OnLayerToggleChanged;
        }
        _layersMaskButton.onClickedButton -= OnLayersMaskButtonClicked;
        _adjacentPlaceButton.onClickedButton -= OnAdjacentPlaceClicked;
    }
}