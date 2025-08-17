using System.Collections.Generic;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.API.Attributes;
using EditorHelper2.API.Interfaces;
using EditorHelper2.UI.Builders;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Level.Objects;

[UIExtension(typeof(EditorLevelObjectsUI))]
[EHExtension("Extra Object Tools (Adjacent placement, Filter by mod, Layer selection toggle)", "Senior S")]
public class ExtrasExtension : UIExtension, IExtension
{
    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;
    
    private readonly SleekButtonIcon _adjacentPlaceButton;
    
    private readonly SleekButtonIcon _filterByModButton;
    private readonly ISleekField _filterByModField;
    private string _filterByModText = string.Empty;
    
    public int ObjectsLayerMask { get; private set; } = LayerMask.GetMask("Large", "Medium", "Small", "Barricade", "Structure");
    private readonly ISleekBox _layersContainer;
    private readonly Dictionary<ISleekToggle, string> _toggleToLayer = new();
    private readonly SleekButtonIcon _layersMaskButton;
    
    public ExtrasExtension()
    {
        UIBuilder builder = new(150f, 30f);
        
        builder.SetText("Filter objects")
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(205f)
            .SetOffsetVertical(-110f);
        _filterByModButton = builder.BuildButton("Highlight all objects that derive from this mod.");

        builder.SetText("Mod ID");
        _filterByModField = builder.BuildStringField();
        
        Initialize();
    }
    
    public void Initialize()
    {
        if (_container == null) return;
        
        _container.AddChild(_filterByModButton);
        _container.AddChild(_filterByModField);
        
        _filterByModButton.onClickedButton += OnFilterByModClicked;
        _filterByModField.OnTextSubmitted += OnFilterByModFieldSubmitted;
    }

    #region Event Handlers
    private void OnFilterByModClicked(ISleekElement button)
    {
        throw new System.NotImplementedException();
    }
    
    private void OnFilterByModFieldSubmitted(ISleekField field)
    {
        throw new System.NotImplementedException();
    }
    #endregion

    public void Dispose()
    {
        if (_container == null) return;
        
        _container.RemoveChild(_filterByModButton);
        _container.RemoveChild(_filterByModField);
        
        _filterByModButton.onClickedButton -= OnFilterByModClicked;
        _filterByModField.OnTextSubmitted -= OnFilterByModFieldSubmitted;
    }
}