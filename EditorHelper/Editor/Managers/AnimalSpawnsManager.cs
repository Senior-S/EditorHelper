using System.Linq;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Editor.Managers;

public class AnimalSpawnsManager
{
    private ushort _lastAnimal = 0;

    private MeshFilter _animalMeshFilter;
    private Material _defaultMaterial = null!;

    private Mesh _originalBoxMesh = null!;
    
    public AnimalSpawnsManager()
    {
        Level.onLevelLoaded += OnLevelLoaded;
    }

    private void OnLevelLoaded(int level)
    {
        if (Level.isEditor)
        {
            UpdateAnimalSpawns();
        }
    }

    public void CustomUpdate()
    {
        if (!EditorSpawns.isSpawning || EditorInteract.isFlying || !Glazier.Get().ShouldGameProcessInput 
            || EditorSpawns.spawnMode != ESpawnMode.ADD_ANIMAL) return;
        
        if (EditorSpawns.selectedAnimal == byte.MaxValue && _originalBoxMesh != null)
        {
            _lastAnimal = 0;
            _animalMeshFilter.mesh = _originalBoxMesh;
            EditorSpawns._animalSpawn.rotation = Quaternion.Euler(0f, 0f, 0f);
            return;
        }

        if (EditorSpawns.selectedAnimal >= LevelAnimals.tables.Count || LevelAnimals.tables[EditorSpawns.selectedAnimal].tiers == null 
                                                                      || LevelAnimals.tables[EditorSpawns.selectedAnimal].tiers.Count < 1)
        {
            return;
        }
        
        AnimalTier? tier = LevelAnimals.tables[EditorSpawns.selectedAnimal].tiers.FirstOrDefault();
        if (tier?.table == null || tier.table.Count < 1)
        {
            return;
        }

        AnimalSpawn? spawn = tier.table.FirstOrDefault();
        if (spawn == null || spawn.animal == 0 || _lastAnimal == spawn.animal)
        {
            return;
        }
        
        UpdateModel(spawn);
    }
    
    public void UpdateAnimalSpawns()
    {
        foreach (AnimalSpawnpoint spawn in LevelAnimals.spawns)
        {
            ushort animalID = LevelAnimals.getAnimal(spawn);
            Asset asset = Assets.find(EAssetType.ANIMAL, animalID);
            if (asset is not AnimalAsset animalAsset) continue;
            Transform? model0 = animalAsset.client.transform.GetChild(0).Find("Model_0");
            if (model0 == null) continue;
            SkinnedMeshRenderer animalSkinnedMeshRenderer = model0.gameObject.GetComponent<SkinnedMeshRenderer>();
            if (animalSkinnedMeshRenderer == null) continue;
            
            MeshFilter? meshFilter = spawn.node.GetComponent<MeshFilter>();
            if (!meshFilter) continue;
            meshFilter.mesh = animalSkinnedMeshRenderer.sharedMesh;
            
            spawn.node.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        }
    }
    
    private void UpdateModel(AnimalSpawn spawn)
    {
        _lastAnimal = spawn.animal;
        
        Asset? animal = Assets.find(EAssetType.ANIMAL, spawn.animal);

        if (animal is not AnimalAsset animalAsset) return;
        Transform? model0 = animalAsset.client.transform.GetChild(0).Find("Model_0");
        if (model0 == null) return;
        SkinnedMeshRenderer animalSkinnedMeshRenderer = model0.gameObject.GetComponent<SkinnedMeshRenderer>();
        if (animalSkinnedMeshRenderer == null) return;

        if (_animalMeshFilter == null)
        {
            _animalMeshFilter = EditorSpawns._animalSpawn.GetComponent<MeshFilter>();
            _originalBoxMesh = _animalMeshFilter.sharedMesh;
        }

        _animalMeshFilter.mesh = animalSkinnedMeshRenderer.sharedMesh;
        EditorSpawns._animalSpawn.rotation = Quaternion.Euler(-90f, 0f, 0f);
    }
}