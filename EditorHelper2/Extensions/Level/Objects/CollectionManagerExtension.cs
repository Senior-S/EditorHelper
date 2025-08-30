using System.Collections.Generic;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using DanielWillett.UITools.Util;
using EditorHelper.Writers;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.UI.Builders;
using SDG.Framework.Foliage;
using SDG.Unturned;

namespace EditorHelper2.Extensions.Level.Objects;

[UIExtension(typeof(EditorTerrainDetailsUI))]
[EHExtension("Live Collection Manager", "JienSultan")]
public sealed class CollectionManagerExtension: UIExtension, IExtension
{
    private readonly SleekList<FoliageInfoAsset> _assetScrollView;
    private readonly SleekButtonIcon _saveButton;
    private readonly SleekButtonIcon _collectionCreateButton;
    private readonly ISleekField _collectionNameField;
        
    // To keep track of the assets, because I can only give name to the toggle.
    private readonly Dictionary<ISleekBox, FoliageInfoAsset> _boxToAsset;
        
    // To not update 20 billion times every second
    private FoliageInfoCollectionAsset _lastCollectionAsset;
        
    // Save it in variable, because I don't know how else I can make it work
    private EditorTerrainDetailsUI _currentUIInstance;
    
    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;

    public CollectionManagerExtension()
    {
        _boxToAsset = new Dictionary<ISleekBox, FoliageInfoAsset>();
            
        // Main box container
        UIBuilder builder = new(200f, 30f);
            
        // Field
        builder.SetAnchorVertical(0f)
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(-0f)
            .SetOffsetVertical(0f);
        _collectionNameField = builder.BuildStringField();
        _collectionNameField.PlaceholderText = "Name";
        _collectionNameField.AddLabel("Name", ESleekSide.RIGHT);
            
        // Create
        builder.SetAnchorVertical(0f)
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(-0f)
            .SetOffsetVertical(40f);
        _collectionCreateButton = builder.BuildButton("Create Collection.");
        _collectionCreateButton.text = "Create New Collection";
        _collectionCreateButton.onClickedButton += CreateCollection;
            
        // Save button
        builder.SetAnchorVertical(0f)
            .SetAnchorHorizontal(1f)
            .SetOffsetHorizontal(0f)
            .SetOffsetVertical(-310f);
        _saveButton = builder.BuildButton("Write to file.");
        _saveButton.text = "Save";
        _saveButton.onClickedButton += WriteToFile;
            
        // Scrollbox
        builder.SetAnchorVertical(0f)
            .SetAnchorHorizontal(0f)
            .SetOffsetHorizontal(0f)
            .SetSizeHorizontal(280f)
            .SetSizeVertical(-440f);
        _assetScrollView = builder.BuildScrollBox<FoliageInfoAsset>(30, 1);
        _assetScrollView.onCreateElement = OnCreateElement;
    }
    
    public void Initialize()
    {
        if (_container == null) return;
        _container.AddChild(_assetScrollView);
            
        _container.AddChild(_collectionCreateButton);
        _container.AddChild(_collectionNameField);
        _container.AddChild(_saveButton);

        List<FoliageInfoAsset> foliageAssets = new();
        Assets.find(foliageAssets);

        _assetScrollView.SetData(foliageAssets);
        _assetScrollView.Update();
    }
    
    public void CustomUpdate()
    {
        bool visibility = _currentUIInstance.tool.mode != FoliageEditor.EFoliageMode.BAKE && _currentUIInstance.searchTypeButton.state == 1;
            
        _collectionNameField.IsVisible = visibility;
        _collectionCreateButton.IsVisible = visibility;
        _saveButton.IsVisible = visibility;
        _assetScrollView.IsVisible = visibility;

        // If selection changed, update the list
        FoliageInfoCollectionAsset? current = _currentUIInstance.tool.selectedCollectionAsset;
        if (_lastCollectionAsset != current)
        {
            _lastCollectionAsset = current;

            for (int i = 0; i < _assetScrollView.ElementCount; ++i)
            {
                ISleekElement element = _assetScrollView.GetElement(i);

                if (element is ISleekBox box && _boxToAsset.TryGetValue(box, out FoliageInfoAsset? asset))
                {
                    using var sleekChildEnumerator = box.GetEnumerator();
                    foreach (var child in sleekChildEnumerator)
                    {
                        if (child is ISleekToggle toggle)
                        {
                            toggle.Value = IsInsideCollection(_currentUIInstance, asset);
                        }
                    }

                    /*for (int j = 0; j < box.GetChildCount(); ++j)
                    {
                        ISleekElement? child = box.GetChildAtIndex(j);
                        if (child is ISleekToggle toggle)
                        {
                            toggle.Value = IsInsideCollection(_currentUIInstance, asset);
                        }
                    }*/
                }
            }
        }
    }
    
    public void Dispose()
    {
        if (_container == null) return;
        _container.RemoveChild(_assetScrollView);
        _container.RemoveChild(_collectionCreateButton);
        _container.RemoveChild(_collectionNameField);
        _container.RemoveChild(_saveButton);
    }
    
    private ISleekElement OnCreateElement(FoliageInfoAsset item)
    {
        UIBuilder builder = new(200f, 200f);
        builder
            .SetAnchorHorizontal(0f)
            .SetAnchorVertical(0f);
        ISleekBox box = builder.CreateSimpleBox();


        ISleekToggle toggle = Glazier.Get().CreateToggle();
        toggle.SizeOffset_X = 30f;
        toggle.SizeOffset_Y = 30f;
        toggle.AddLabel(item.name, ESleekSide.RIGHT);

        toggle.Value = IsInsideCollection(_currentUIInstance, item);
        toggle.OnValueChanged += (t, value) =>
        {
            OnToggleElement(item, value);
        };

        box.AddChild(toggle);
        _boxToAsset[box] = item;

        return box;
    }
    
    private void OnToggleElement(FoliageInfoAsset item, bool value)
    {
        // Use the stored UI instance, because I dont know how else I can use ref in other methods
        FoliageInfoCollectionAsset? selectedCollectionAsset = _currentUIInstance?.tool?.selectedCollectionAsset;
        if (selectedCollectionAsset?.elements == null)
            return;

        if (!value)
        {
            // Remove the asset from the collection
            selectedCollectionAsset.elements.RemoveAll(e => e.asset != null && e.asset.Find() == item);
        }
        else
        {
            // Add the asset to the collection if not already present
            if (!selectedCollectionAsset.elements.Exists(e => e.asset != null && e.asset.Find() == item))
            {
                selectedCollectionAsset.elements.Add(new FoliageInfoCollectionAsset.FoliageInfoCollectionElement
                {
                    asset = new AssetReference<FoliageInfoAsset>(item.GUID),
                    weight = 1f // Default weight, maybe I will add a textBox
                });
            }
        }
    }

    private bool IsInsideCollection(EditorTerrainDetailsUI uiInstance, FoliageInfoAsset item)
    {
        // Bunch off null checks, because it crashes your editor by default, because nothing is selected xd
        if (uiInstance?.tool == null)
            return false;

        FoliageInfoCollectionAsset? selectedCollectionAsset = uiInstance.tool.selectedCollectionAsset;
        if (selectedCollectionAsset?.elements == null)
            return false;

        foreach (FoliageInfoCollectionAsset.FoliageInfoCollectionElement element in selectedCollectionAsset.elements)
        {
            if (element.asset != null && element.asset.Find() == item)
            {
                return true;
            }
        }

        return false;
    }
    
    private void WriteToFile(ISleekElement button)
    {
        AssetWriter.SaveFoliageInfoCollectionAsset(_currentUIInstance.tool.selectedCollectionAsset);
    }

    private void CreateCollection(ISleekElement button)
    {
        AssetWriter.CreateEmptyFoliageInfoCollectionAssetFile(_collectionNameField.Text);
        _collectionNameField.Text = "";

        new FoliageInfoCollectionAsset();
    }
}