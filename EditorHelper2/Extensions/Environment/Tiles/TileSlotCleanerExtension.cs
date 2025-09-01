using System;
using System.Collections.Generic;
using DanielWillett.UITools.API.Extensions;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.UI.Builders;
using SDG.Framework.Devkit;
using SDG.Framework.Landscapes;
using SDG.Unturned;

namespace EditorHelper2.Extensions.Environment.Tiles;

[UIExtension(typeof(EditorTerrainTilesUI))]
[EHExtension("TileSlotCleanerExtension extension", "JienSultan")]
public class TileSlotCleanerExtension : UIExtension, IExtension
{
    private readonly SleekButtonIcon _cleanSlotsButton;
    private readonly EditorTerrainTilesUI? _currentUIInstance;
    
    public TileSlotCleanerExtension(EditorTerrainTilesUI instance)
    {
        _currentUIInstance = instance;
        UIBuilder builder = new(200f, 30f);

        builder.SetAnchorHorizontal(1f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-560f)
            .SetOffsetVertical(-440f)
            .SetSizeHorizontal(200)
            .SetSizeVertical(30);

        _cleanSlotsButton = builder.BuildButton("Free up duplicate slots.");
        _cleanSlotsButton.text = "Clean Duplicates";
        _cleanSlotsButton.onClickedButton += OnClickedRemoveDuplicates;
        
        Initialize();
    }

    public void Initialize()
    {
        if (_currentUIInstance == null) return;
        _currentUIInstance.AddChild(_cleanSlotsButton);
    }

    #region Event handlers
    
    private void OnClickedRemoveDuplicates(ISleekElement button)
    {
        if (_currentUIInstance == null) return;
        LandscapeTile selectedTile = TerrainEditor.selectedTile;
        if (selectedTile == null) return;

        // Create a list to store unique material references
        List<AssetReference<LandscapeMaterialAsset>> uniqueMaterials = new List<AssetReference<LandscapeMaterialAsset>>();
        HashSet<AssetReference<LandscapeMaterialAsset>> seenMaterials = new HashSet<AssetReference<LandscapeMaterialAsset>>();

        // Iterate through materials to identify duplicates
        for (int i = 0; i < selectedTile.materials.Count; i++)
        {
            var material = selectedTile.materials[i];
            if (material != null && !seenMaterials.Contains(material))
            {
                seenMaterials.Add(material);
                uniqueMaterials.Add(material);
            }
        }

        // Fill remaining slots with invalid references if needed
        while (uniqueMaterials.Count < Landscape.SPLATMAP_LAYERS)
        {
            uniqueMaterials.Add(AssetReference<LandscapeMaterialAsset>.invalid);
        }

        // Update the materials list
        for (int i = 0; i < Landscape.SPLATMAP_LAYERS; i++)
        {
            selectedTile.materials[i] = uniqueMaterials[i];
        }

        // Update terrain prototypes and UI
        selectedTile.updatePrototypes();
        for (int i = 0; i < _currentUIInstance.layers.Length; i++)
        {
            _currentUIInstance.layers[i].UpdateSelectedTile();
        }
        _currentUIInstance.SetSelectedLayerIndex(_currentUIInstance.selectedLayerIndex);

        // Mark level hierarchy as dirty to ensure changes are saved
        LevelHierarchy.MarkDirty();
    }

    #endregion Event handlers

    #region Extension Functions

    #endregion Extension Functions

    public void Dispose()
    {

    }
}