using System;
using Newtonsoft.Json;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.common.Types;

public class SerializableEditorCopy
{
    public SerializableVector3 Position { get; set; }
    
    public SerializableQuaternion Rotation { get; set; }
    
    public SerializableVector3 Scale { get; set; }
    
    public Guid ObjectAssetGuid { get; set; }
    
    public Guid ItemAssetGuid { get; set; }

    public SerializableEditorCopy()
    {
    }
    
    public SerializableEditorCopy(Vector3 position, Quaternion rotation, Vector3 scale, Guid objectAssetGuid, Guid itemAssetGuid)
    {
        Position = new SerializableVector3(position);
        Rotation = new SerializableQuaternion(rotation);
        Scale = new SerializableVector3(scale);
        ObjectAssetGuid = objectAssetGuid;
        ItemAssetGuid = itemAssetGuid;
    }

    public EditorCopy ToEditorCopy()
    {
        ObjectAsset objectAsset = null!;
        ItemAsset itemAsset = null!;
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