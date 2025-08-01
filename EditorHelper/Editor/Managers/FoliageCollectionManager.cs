using System;
using System.Collections.Generic;
using EditorHelper.Builders;
using EditorHelper.Writers;
using SDG.Framework.Foliage;
using SDG.Unturned;

namespace EditorHelper.Editor
{
    public class FoliageCollectionManager
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

        public FoliageCollectionManager()
        {
            _boxToAsset = new Dictionary<ISleekBox, FoliageInfoAsset>();
            
            // Main box container
            UIBuilder builder = new();
            
            // Field
            builder.SetPositionScaleX(0f)
                .SetPositionScaleY(0f)
                .SetPositionOffsetX(-0f)
                .SetPositionOffsetY(0f);
            _collectionNameField = builder.BuildStringField();
            _collectionNameField.PlaceholderText = "Name";
            _collectionNameField.AddLabel("Name", ESleekSide.RIGHT);
            
            // Create
            builder.SetPositionScaleX(0f)
                .SetPositionScaleY(0f)
                .SetPositionOffsetX(-0f)
                .SetPositionOffsetY(40f);
            _collectionCreateButton = builder.BuildButton("Create Collection.");
            _collectionCreateButton.text = "Create New Collection";
            _collectionCreateButton.onClickedButton += CreateCollection;
            
            // Save button
            builder.SetPositionScaleX(0f)
                .SetPositionScaleY(1f)
                .SetPositionOffsetX(0f)
                .SetPositionOffsetY(-310f);
            _saveButton = builder.BuildButton("Write to file.");
            _saveButton.text = "Save";
            _saveButton.onClickedButton += WriteToFile;
            
            // Scrollbox
            builder.SetPositionScaleX(0f)
                .SetPositionScaleY(0f)
                .SetPositionOffsetX(0f)
                //.SetSizeOffsetY(-240f)
                .SetSizeOffsetY(-440f)
                .SetSizeOffsetX(280f)
                .SetOneTimeSpacing(0f);
            _assetScrollView = builder.BuildScrollBox<FoliageInfoAsset>(30, 1);
            _assetScrollView.onCreateElement = OnCreateElement;
        }

        private ISleekElement OnCreateElement(FoliageInfoAsset item)
        {
            UIBuilder builder = new();
            builder
                .SetPositionScaleX(0f)
                .SetPositionScaleY(0f);
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

        public void CustomUpdate(EditorTerrainDetailsUI uiInstance)
        {
            // Store the current UI instance for use in toggle callbacks
            _currentUIInstance = uiInstance;

            // If selection changed, update the list
            var current = uiInstance.tool.selectedCollectionAsset;
            if (_lastCollectionAsset != current)
            {
                _lastCollectionAsset = current;

                for (int i = 0; i < _assetScrollView.ElementCount; ++i)
                {
                    ISleekElement element = _assetScrollView.GetElement(i);

                    if (element is ISleekBox box && _boxToAsset.TryGetValue(box, out var asset))
                    {
                        for (int j = 0; j < box.GetChildCount(); ++j)
                        {
                            var child = box.GetChildAtIndex(j);
                            if (child is ISleekToggle toggle)
                            {
                                toggle.Value = IsInsideCollection(uiInstance, asset);
                            }
                        }
                    }
                }
            }
        }

        private void OnToggleElement(FoliageInfoAsset item, bool value)
        {
            // Use the stored UI instance, because I dont know how else I can use ref in other methods
            var selectedCollectionAsset = _currentUIInstance?.tool?.selectedCollectionAsset;
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

            var selectedCollectionAsset = uiInstance.tool.selectedCollectionAsset;
            if (selectedCollectionAsset?.elements == null)
                return false;

            foreach (var element in selectedCollectionAsset.elements)
            {
                if (element.asset != null && element.asset.Find() == item)
                {
                    return true;
                }
            }

            return false;
        }

        public void Initialize(ref EditorTerrainDetailsUI uiInstance)
        {
            _currentUIInstance = uiInstance;
            uiInstance.AddChild(_assetScrollView);
            
            // Individual elements
            uiInstance.AddChild(_collectionCreateButton);
            uiInstance.AddChild(_collectionNameField);
            uiInstance.AddChild(_saveButton);

            List<FoliageInfoAsset> foliageAssets = new();
            Assets.find(foliageAssets);

            _assetScrollView.SetData(foliageAssets);
            _assetScrollView.Update();
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
}