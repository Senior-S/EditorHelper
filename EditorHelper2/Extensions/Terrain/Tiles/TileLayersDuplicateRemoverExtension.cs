using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DanielWillett.UITools.API.Extensions;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.UI.Builders;
using SDG.Framework.Devkit;
using SDG.Framework.Landscapes;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Terrain.Tiles;

[UIExtension(typeof(EditorTerrainTilesUI))]
[EHExtension("Tile Layers Duplicate Remover Extension", "JienSultan")]
public class TileLayersDuplicateRemoverExtension : UIExtension, IExtension
{
    private readonly SleekButtonIcon _cleanSlotsButton;
    private readonly EditorTerrainTilesUI? _currentUIInstance;
    
    public TileLayersDuplicateRemoverExtension(EditorTerrainTilesUI instance)
    {
        _currentUIInstance = instance;
        UIBuilder builder = new(200f, 30f);
        
        Assembly assembly = typeof(EditorHelper).Assembly; 
        string iconsPath = Path.Combine(Path.GetDirectoryName(assembly.Location) ?? string.Empty, "Assets" ,"Icons.unity3d"); 
        Bundle icons = Bundles.getBundle(iconsPath, false);

        builder.SetAnchorHorizontal(1f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(-560f)
            .SetOffsetVertical(-440f)
            .SetSizeHorizontal(250f)
            .SetSizeVertical(30f);
        
        _cleanSlotsButton = builder.BuildButtonIcon("Remove duplicates to free up slots", icons.load<Texture2D>("Both"));
        _cleanSlotsButton.text = "Remove Duplicates";
        _cleanSlotsButton.onClickedButton += OnClickedRemoveDuplicates;
        
        icons.unload();
        
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

        // Collections to store materials, better than nested loops' O(n^2)
        // Shoutout to my teacher for teaching complexity and algorithms
        List<AssetReference<LandscapeMaterialAsset>> uniqueMaterials = new List<AssetReference<LandscapeMaterialAsset>>();
        HashSet<AssetReference<LandscapeMaterialAsset>> seenMaterials = new HashSet<AssetReference<LandscapeMaterialAsset>>();
        Dictionary<int, int> duplicateToKeptIndex = new Dictionary<int, int>(); // Maps duplicate layer index to kept layer index

        // Identify duplicates and build mapping
        for (int i = 0; i < selectedTile.materials.Count; i++)
        {
            // Current material
            AssetReference<LandscapeMaterialAsset> material = selectedTile.materials[i];
            if (!material.isNull)
            {
                // If already seen, it's a duplicate most likely
                if (seenMaterials.Contains(material))
                {
                    // Find the index of the first occurrence of this material
                    for (int j = 0; j < uniqueMaterials.Count; j++)
                    {
                        if (uniqueMaterials[j] == material)
                        {
                            duplicateToKeptIndex[i] = j;
                            break;
                        }
                    }
                }
                // Else just add it to the collections
                else
                {
                    seenMaterials.Add(material);
                    uniqueMaterials.Add(material);
                    duplicateToKeptIndex[i] = uniqueMaterials.Count - 1;
                }
            }
            else
            {
                // Handle null or invalid materials
                duplicateToKeptIndex[i] = -1;
            }
        }

        // Fill remaining slots with invalid references if needed
        while (uniqueMaterials.Count < Landscape.SPLATMAP_LAYERS)
        {
            uniqueMaterials.Add(AssetReference<LandscapeMaterialAsset>.invalid);
        }

        // Update splatmap to reassign weights from duplicate layers to kept layers
        float[,,] newSplatmap = new float[Landscape.SPLATMAP_RESOLUTION, Landscape.SPLATMAP_RESOLUTION, Landscape.SPLATMAP_LAYERS];
        for (int x = 0; x < Landscape.SPLATMAP_RESOLUTION; x++)
        {
            for (int y = 0; y < Landscape.SPLATMAP_RESOLUTION; y++)
            {
                float[] weights = new float[Landscape.SPLATMAP_LAYERS];
                // Sum weights for each kept material
                for (int oldLayer = 0; oldLayer < Landscape.SPLATMAP_LAYERS; oldLayer++)
                {
                    int newLayer = duplicateToKeptIndex[oldLayer];
                    if (newLayer >= 0)
                    {
                        weights[newLayer] += selectedTile.splatmap[x, y, oldLayer];
                    }
                }
                // Normalize weights to ensure they sum to 1
                float totalWeight = 0f;
                for (int i = 0; i < Landscape.SPLATMAP_LAYERS; i++)
                {
                    totalWeight += weights[i];
                }
                if (totalWeight > 0f)
                {
                    for (int i = 0; i < Landscape.SPLATMAP_LAYERS; i++)
                        newSplatmap[x, y, i] = weights[i] / totalWeight;
                }
                else
                {
                    // If no weights, set first layer to 1 (default behavior like resetSplatmap)
                    newSplatmap[x, y, 0] = 1f;
                }
            }
        }

        // Update the materials list
        for (int i = 0; i < Landscape.SPLATMAP_LAYERS; i++)
        {
            selectedTile.materials[i] = uniqueMaterials[i];
        }

        // Update splatmap
        selectedTile.splatmap = newSplatmap;
        if (!Dedicator.IsDedicatedServer)
        {
            selectedTile.data.SetAlphamaps(0, 0, newSplatmap);
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