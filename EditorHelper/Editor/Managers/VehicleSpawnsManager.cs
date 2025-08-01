using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using SDG.Unturned;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorHelper.Editor;

public class VehicleSpawnsManager
{
    private ushort _lastVehicle = 0;

    private Transform _arrowTransform;
    private MeshFilter _vehicleMeshFilter;
    private Material _defaultMaterial = null!;
    private Transform? _wheelsTransform = null;
    private List<Renderer> _wheelRenderers = [];

    private Mesh _originalBoxMesh = null!;
    
    public VehicleSpawnsManager()
    {
        Level.onLevelLoaded += OnLevelLoaded;
    }

    private void OnLevelLoaded(int level)
    {
        if (Level.isEditor)
        {
            UpdateVehicleSpawns();
        }
    }

    public void CustomUpdateUI()
    {
        if (!EditorSpawns.isSpawning || EditorInteract.isFlying || !Glazier.Get().ShouldGameProcessInput 
            || EditorSpawns.spawnMode != ESpawnMode.ADD_VEHICLE) return;
        
        if (EditorSpawns.selectedVehicle == byte.MaxValue && _originalBoxMesh != null)
        {
            _lastVehicle = 0;
            _vehicleMeshFilter.mesh = _originalBoxMesh;
            Object.Destroy(_wheelsTransform);
            _wheelsTransform = null;
            
            EditorSpawns._vehicleSpawn.rotation = Quaternion.Euler(0f, EditorSpawns.rotation, 0f);
            _arrowTransform.rotation = Quaternion.Euler(0f, EditorSpawns.rotation, 0f);
            return;
        }

        if (EditorSpawns.selectedVehicle >= LevelVehicles.tables.Count || LevelVehicles.tables[EditorSpawns.selectedVehicle].tiers == null 
            || LevelVehicles.tables[EditorSpawns.selectedVehicle].tiers.Count < 1)
        {
            return;
        }
        
        EditorSpawns._vehicleSpawn.rotation = Quaternion.Euler(-90f, 180f + EditorSpawns.rotation, 0f);
        if (_arrowTransform == null)
        {
            _arrowTransform = EditorSpawns._vehicleSpawn.Find("Arrow");
        }
        _arrowTransform.rotation = Quaternion.Euler(360f, EditorSpawns.rotation, 0f);
        
        VehicleTier? tier = LevelVehicles.tables[EditorSpawns.selectedVehicle].tiers.FirstOrDefault();
        if (tier?.table == null || tier.table.Count < 1)
        {
            return;
        }

        VehicleSpawn? spawn = tier.table.FirstOrDefault();
        if (spawn == null || spawn.vehicle == 0 || _lastVehicle == spawn.vehicle)
        {
            return;
        }
        
        UpdateModel(spawn);
    }
    
    public void UpdateRotation()
    {
        if (EditorSpawns.selectedVehicle == byte.MaxValue && _originalBoxMesh != null)
        {
            EditorSpawns._vehicleSpawn.rotation = Quaternion.Euler(0f, EditorSpawns.rotation, 0f);
            _arrowTransform.rotation = Quaternion.Euler(0f, EditorSpawns.rotation, 0f);
        }
        else
        {
            EditorSpawns._vehicleSpawn.rotation = Quaternion.Euler(-90f, 180f + EditorSpawns.rotation, 0f);
            if (_arrowTransform != null)
            {
                _arrowTransform.rotation = Quaternion.Euler(360f, EditorSpawns.rotation, 0f);
            }
        }
    }

    public void UpdateColor()
    {
        if (_wheelRenderers.Count < 1) return;
        
        Color color = EditorSpawns.selectedVehicle != byte.MaxValue ? LevelVehicles.tables[EditorSpawns.selectedVehicle].color : Color.white;
        _wheelRenderers.ForEach(r =>
        {
            r.material.color = color;
        });
    }
    
    public void UpdateVehicleSpawns()
    {
        foreach (VehicleSpawnpoint spawn in LevelVehicles.spawns)
        {
            Asset vehicle = LevelVehicles.GetRandomAssetForSpawnpoint(spawn);
            Color color = LevelVehicles.tables[spawn.type].color;
            MeshFilter? vehicleMeshFilter = GetVehicleModel(vehicle, out Transform? wheels, color);
            if (spawn.node.childCount > 1)
            {
                Transform wheelsTransform = spawn.node.GetChild(1);
                Object.Destroy(wheelsTransform.gameObject);
            }
            if (vehicleMeshFilter == null) continue;
            if (wheels != null)
            {
                wheels.SetParent(spawn.node);
                wheels.localPosition = new Vector3(0f, 0f, 0f);
                wheels.gameObject.SetTagIfUntaggedRecursively(spawn.node.tag);
                wheels.gameObject.SetLayerRecursively(spawn.node.gameObject.layer);
                wheels.position = spawn.node.position;
                wheels.localRotation = Quaternion.Euler(0f, 0f, 0f);
                wheels.rotation = spawn.node.rotation * Quaternion.Euler(-90f, 0f, 0f);
            }
        
            MeshFilter? meshFilter = spawn.node.GetComponent<MeshFilter>();
            if (!meshFilter) continue;
            meshFilter.mesh = vehicleMeshFilter.mesh;
            
            spawn.node.rotation = Quaternion.Euler(-90f, 180f + spawn.angle, 0f);
            Transform? arrow = spawn.node.Find("Arrow");
            if (arrow == null) continue;
            arrow.rotation = Quaternion.Euler(360f, spawn.angle, 0f);
        }
    }

    private void UpdateModel(VehicleSpawn spawn)
    {
        _lastVehicle = spawn.vehicle;
        
        // Nelson be like:
        // Let's update vehicles to work only with guids and make IDs not a requirement for vehicles
        // Also Nelson: Lets continue using IDs for spawns so mapmakers take shit and can't spawn vehicles without an id
        Asset? vehicle = Assets.find(EAssetType.VEHICLE, spawn.vehicle);

        MeshFilter? vehicleMeshFilter = GetVehicleModel(vehicle, out Transform? wheels);
        if (vehicleMeshFilter == null) return;
        if (_vehicleMeshFilter == null)
        {
            _vehicleMeshFilter = EditorSpawns._vehicleSpawn.GetComponent<MeshFilter>();
            _originalBoxMesh = _vehicleMeshFilter.sharedMesh;
        }

        if (wheels != null)
        {
            _wheelsTransform = wheels;
            _wheelsTransform.SetParent(EditorSpawns._vehicleSpawn);
            wheels.gameObject.SetTagIfUntaggedRecursively(EditorSpawns._vehicleSpawn.tag);
            wheels.gameObject.SetLayerRecursively(EditorSpawns._vehicleSpawn.gameObject.layer);
            wheels.localRotation = Quaternion.Euler(0f, 0f, 0f) * Quaternion.Euler(-90f, 0f, 0f);
            wheels.localPosition = Vector3.zero;
        }
        
        _vehicleMeshFilter.mesh = vehicleMeshFilter.mesh;
    }

    private MeshFilter? GetVehicleModel(Asset asset, out Transform? wheels, Color? color = null)
    {
        wheels = null;
        if (asset == null) return null;
        GameObject? vehicleModel = null;
        if (asset is VehicleRedirectorAsset redirectorAsset)
        {
            VehicleAsset? vehicleAsset = redirectorAsset.TargetVehicle.Find();
            if (vehicleAsset != null)
            {
                vehicleModel = vehicleAsset.GetOrLoadModel();
            }
        }
        else if (asset is VehicleAsset vehicleAsset)
        {
            vehicleModel = vehicleAsset.GetOrLoadModel();
        }

        if (vehicleModel == null) return null;
        if (color == null && _wheelsTransform != null)
        {
            _wheelRenderers.Clear();
            Object.Destroy(_wheelsTransform.gameObject);
        }
        
        Transform? model0 = vehicleModel.transform.Find("Model_0");
        if (model0 == null) return null;
        
        wheels = vehicleModel.transform.Find("Wheels");
        bool hasWheels = wheels != null;
        if (hasWheels)
        {
            wheels = Object.Instantiate(wheels);
            if (wheels == null) return null;
            wheels.name = "Wheels";
            for (int i = 0; i < wheels.childCount; i++)
            {
                Transform? wheel = wheels.GetChild(i);
                if (wheel == null) continue;
                LODGroup lodGroup = wheel.GetComponent<LODGroup>();
                if (lodGroup != null)
                {
                    Object.Destroy(lodGroup);
                }
                
                for (int j = 0; j < wheel.childCount; j++)
                {
                    Transform? wModel = wheel.GetChild(j);
                    if (wModel == null) continue;
                    
                    if (_defaultMaterial == null)
                    {
                        Renderer renderer = EditorSpawns._vehicleSpawn.GetComponent<Renderer>();
                        _defaultMaterial = renderer.material;
                    }
                    
                    Renderer wheelRenderer = wModel.GetComponent<Renderer>();
                    if (color == null)
                    {
                        _wheelRenderers.Add(wheelRenderer);
                        wheelRenderer.material = _defaultMaterial;
                    }
                    else
                    {
                        Material unlitColor = new(Shader.Find("Unlit/Color"));
                        wheelRenderer.material = unlitColor;
                        wheelRenderer.material.color = (Color)color;
                    }
                }
            }
        }

        MeshFilter vehicleMeshFilter = model0.GetComponent<MeshFilter>();

        return vehicleMeshFilter;
    }
}