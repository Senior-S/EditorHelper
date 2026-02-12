using System.Collections.Generic;
using DanielWillett.UITools.API;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.Util;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers;
using EditorHelper2.UI.Builders;
using SDG.Framework.Foliage;
using SDG.Unturned;

namespace EditorHelper2.Extensions.Terrain.Foliage;

[UIExtension(typeof(EditorTerrainDetailsUI))]
[EHExtension("Live Collection Editor", "JienSultan")]
public sealed class CollectionManagerExtension: UIExtension, IExtension
{
    private readonly SleekList<FoliageInfoAsset> _assetScrollView;
    private readonly SleekButtonIcon _saveButton;
    private readonly SleekButtonIcon _collectionCreateButton;
    private readonly ISleekField _collectionNameField;
        
    // To keep track of the assets, because I can only give name to the toggle.
    private readonly Dictionary<ISleekBox, FoliageInfoAsset> _boxToAsset;
        
    // To not update 20 billion times every second
    private FoliageInfoCollectionAsset? _lastCollectionAsset;
        
    // Save it in variable, because I don't know how else I can make it work
    private EditorTerrainDetailsUI? _currentUIInstance;

    public CollectionManagerExtension(EditorTerrainDetailsUI instance)
    {
        _currentUIInstance = instance;
        _boxToAsset = new Dictionary<ISleekBox, FoliageInfoAsset>();
            
        // Field
        UIBuilder builder = new(200f, 30f);
        builder.SetAnchorVertical(0f)
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(-0f)
            .SetOffsetVertical(0f);
        _collectionNameField = builder.BuildStringField();
        _collectionNameField.PlaceholderText = "Collection Name";
        _collectionNameField.AddLabel("Name", ESleekSide.RIGHT);
            
        // Create
        builder.SetAnchorVertical(0f)
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(-0f)
            .SetOffsetVertical(40f);
        _collectionCreateButton = builder.BuildButtonIcon("Creates a new collection in the currently edited map's folder");
        _collectionCreateButton.text = "Create New Collection";
        _collectionCreateButton.onClickedButton += CreateCollection;
            
        // Save button
        builder.SetAnchorHorizontal(0f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(0f)
            .SetOffsetVertical(-310);
        _saveButton = builder.BuildButtonIcon("Saves the changes to the collection's file");
        _saveButton.text = "Save Collection";
        _saveButton.onClickedButton += WriteToFile;

        // Scrollbox
        builder = builder.ResetProperties()
            .SetAnchorVertical(0f)      // Start from TOP
            .SetOffsetVertical(80f)     // Start at 80px
            //.SetSizeHorizontal(280f)    // Width of scroll area
            .SetSizeHorizontal(330f)    // Width of scroll area
            .SetSizeVertical(-400f)     // Move the bottom border up
            .SetScaleVertical(1f);      // Auto-resize vertically based on elements around it
        _assetScrollView = builder.BuildScrollBox<FoliageInfoAsset>(30, 1);
        _assetScrollView.onCreateElement = OnCreateElement;

        Initialize();
    }
    
    public void Initialize()
    {
        if (_currentUIInstance == null) return;
        _currentUIInstance.AddChild(_assetScrollView);
        _currentUIInstance.AddChild(_collectionCreateButton);
        _currentUIInstance.AddChild(_collectionNameField);
        _currentUIInstance.AddChild(_saveButton);

        List<FoliageInfoAsset> foliageAssets = new();
        SDG.Unturned.Assets.find(foliageAssets);

        _assetScrollView.SetData(foliageAssets);
        _assetScrollView.Update();
    }
    
    #region Extension functions
    public void CustomUpdate()
    {
        // Visibility
        bool visibility = _currentUIInstance != null && _currentUIInstance.tool.mode != FoliageEditor.EFoliageMode.BAKE && _currentUIInstance.searchTypeButton.state == 1;
            
        _collectionNameField.IsVisible = visibility;
        _collectionCreateButton.IsVisible = visibility;
        _saveButton.IsVisible = visibility;
        _assetScrollView.IsVisible = visibility;

        // If selection changed, update the list
        FoliageInfoCollectionAsset? current = _currentUIInstance?.tool.selectedCollectionAsset;
        if (_lastCollectionAsset != current)
        {
            _lastCollectionAsset = current;

            for (int i = 0; i < _assetScrollView.ElementCount; ++i)
            {
                ISleekElement element = _assetScrollView.GetElement(i);

                if (element is ISleekBox box && _boxToAsset.TryGetValue(box, out FoliageInfoAsset? asset))
                {
                    using SleekChildEnumerator sleekChildEnumerator = box.GetEnumerator();
                    foreach (var child in sleekChildEnumerator)
                    {
                        if (child is ISleekToggle toggle)
                        {
                            toggle.Value = IsInsideCollection(asset);
                        }
                        
                        if (child is ISleekFloat32Field weightField)
                        {
                            weightField.Value = GetAssetWeight(asset);
                            weightField.IsClickable = IsInsideCollection(asset);
                        }
                    }
                }
            }
        }
    }
    
    private bool IsInsideCollection(FoliageInfoAsset item)
    {
        // Bunch off null checks, because it crashes your editor by default, because nothing is selected xd
        if (_currentUIInstance?.tool == null)
            return false;

        FoliageInfoCollectionAsset? selectedCollectionAsset = _currentUIInstance.tool.selectedCollectionAsset;
        if (selectedCollectionAsset?.elements == null)
            return false;

        foreach (FoliageInfoCollectionAsset.FoliageInfoCollectionElement element in selectedCollectionAsset.elements)
        {
            if (element.asset.Find() == item)
            {
                return true;
            }
        }

        return false;
    }
    
    private float GetAssetWeight(FoliageInfoAsset item)
    {
        // Bunch off null checks, because it crashes your editor by default, because nothing is selected xd
        if (_currentUIInstance?.tool == null)
            return 0f;

        FoliageInfoCollectionAsset? selectedCollectionAsset = _currentUIInstance.tool.selectedCollectionAsset;
        if (selectedCollectionAsset?.elements == null)
            return 0f;

        UnturnedLog.info("Asset: " + item.name);
        foreach (FoliageInfoCollectionAsset.FoliageInfoCollectionElement element in selectedCollectionAsset.elements)
        {
            FoliageInfoAsset e = element.asset.Find();
            if (e == item)
            {
                UnturnedLog.info("Weight: " + element.weight);
                return element.weight;
            }
        }
        
        return 0f;
    }
    
    #endregion Extension functions
    
    public void Dispose()
    {
        if (_currentUIInstance == null) return;
        _currentUIInstance.RemoveChild(_assetScrollView);
        _currentUIInstance.RemoveChild(_collectionCreateButton);
        _currentUIInstance.RemoveChild(_collectionNameField);
        _currentUIInstance.RemoveChild(_saveButton);
    }
    
    #region Event Handlers
    private ISleekElement OnCreateElement(FoliageInfoAsset item)
    {
        UIBuilder builder = new(200f, 30f);
        builder
            .SetAnchorHorizontal(0f)
            .SetAnchorVertical(0f);
        ISleekBox box = builder.CreateSimpleAlphaBox();


        ISleekToggle toggle = Glazier.Get().CreateToggle();
        toggle.SizeOffset_X = 30f;
        toggle.SizeOffset_Y = 30f;
        toggle.AddLabel(item.name, ESleekSide.RIGHT);
        toggle.Value = IsInsideCollection(item);
        box.AddChild(toggle);

        builder
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(240f)
            .SetSizeHorizontal(60f)
            .SetSizeVertical(30f);
        ISleekFloat32Field weightField = builder.BuildFloatInput();
        weightField.Value = GetAssetWeight(item);
        weightField.TooltipText = "Weight of the asset";
        weightField.IsClickable = IsInsideCollection(item);
        box.AddChild(weightField);
        
        _boxToAsset[box] = item;
        
        // Event handling
        toggle.OnValueChanged += (_, value) =>
        {
            OnToggleElement(item, value, weightField);
        };
        // OnValueChanged, because many people don't bother pressing enter
        weightField.OnValueChanged += (_, value) =>
        {
            OnChangeWeight(item, value);
        };

        return box;
    }
    
    private void OnToggleElement(FoliageInfoAsset item, bool value, ISleekFloat32Field weightField)
    {
        // Use the stored UI instance, because I dont know how else I can use ref in other methods
        FoliageInfoCollectionAsset? selectedCollectionAsset = _currentUIInstance?.tool?.selectedCollectionAsset;
        if (selectedCollectionAsset?.elements == null)
            return;

        if (!value)
        {
            // Remove the asset from the collection
            selectedCollectionAsset.elements.RemoveAll(e => e.asset.Find() == item);
            weightField.IsClickable = false;
            weightField.Value = 0f;
        }
        else
        {
            // Add the asset to the collection if not already present
            if (!selectedCollectionAsset.elements.Exists(e => e.asset.Find() == item))
            {
                // Hardcoded to default to 1f
                weightField.Value = 1f;
                weightField.IsClickable = true;
                selectedCollectionAsset.elements.Add(new FoliageInfoCollectionAsset.FoliageInfoCollectionElement
                {
                    asset = new AssetReference<FoliageInfoAsset>(item.GUID),
                    weight = weightField.Value
                });
            }
        }
    }

    private void OnChangeWeight(FoliageInfoAsset item, float newWeight)
    {
        FoliageInfoCollectionAsset? selectedCollectionAsset = _currentUIInstance?.tool?.selectedCollectionAsset;
        if (selectedCollectionAsset?.elements == null)
            return;
        

        for (int i = 0; i < selectedCollectionAsset.elements.Count; i++)
        {
            if (selectedCollectionAsset.elements[i].asset.Find() == item)
            {
                var element = selectedCollectionAsset.elements[i];
                element.weight = newWeight;
                selectedCollectionAsset.elements[i] = element;
                break;
            }
        }
    }
    
    private void WriteToFile(ISleekElement button)
    {
        if (_currentUIInstance != null)
            AssetWriter.SaveFoliageInfoCollectionAsset(_currentUIInstance.tool.selectedCollectionAsset);
    }

    private void CreateCollection(ISleekElement button)
    {
        AssetWriter.CreateEmptyFoliageInfoCollectionAssetFile(_collectionNameField.Text);
        _collectionNameField.Text = "";

        new FoliageInfoCollectionAsset();
    }
    
    #endregion Event Handlers
}