using System;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Models;

public class SerializableEditorCopy
{
    public PositionModel Position { get; set; }
    
    public RotationModel Rotation { get; set; }
    
    public PositionModel Scale { get; set; }
    
    public Guid ObjectAssetGuid { get; set; }
    
    public Guid ItemAssetGuid { get; set; }

    public SerializableEditorCopy()
    {
    }
    
    public SerializableEditorCopy(Vector3 position, Quaternion rotation, Vector3 scale, Guid objectAssetGuid, Guid itemAssetGuid)
    {
        Position = new PositionModel(position);
        Rotation = new RotationModel(rotation);
        Scale = new PositionModel(scale);
        ObjectAssetGuid = objectAssetGuid;
        ItemAssetGuid = itemAssetGuid;
    }

    public EditorCopy ToEditorCopy()
    {
        ObjectAsset objectAsset = null;
        ItemAsset itemAsset = null;
        if (ObjectAssetGuid != Guid.Empty)
        {
            objectAsset = Assets.find<ObjectAsset>(ObjectAssetGuid);
        }
        if (ItemAssetGuid != Guid.Empty)
        {
            itemAsset = Assets.find<ItemAsset>(ItemAssetGuid);
        }
        
        
        return new EditorCopy(Position.ToVector3(), Rotation.ToQuaternion(), Scale.ToVector3(), objectAsset, itemAsset);
    }
}