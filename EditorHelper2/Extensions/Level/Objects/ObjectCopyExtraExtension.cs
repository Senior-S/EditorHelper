using DanielWillett.UITools.API.Extensions;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers.Level.Objects;
using EditorHelper2.common.Types;
using SDG.Unturned;
using System.Collections.Generic;
using UnityEngine;

namespace EditorHelper2.Extensions.Level.Objects;

[UIExtension(typeof(EditorLevelObjectsUI))]
[EHExtension("Object Copy Extra Data Extension", "Gamingtoday093", true)]
public class ObjectCopyExtraExtension : UIExtension, IExtension
{
    private readonly Dictionary<EditorCopy, ObjectCopyExtraData> _copyExtraData;

    public ObjectCopyExtraExtension()
    {
        _copyExtraData = new();

        Initialize();
    }

    public void Initialize() { }

    public void RegisterCopy(Transform transform, EditorCopy copyData)
    {
        LevelObject? levelObject = ObjectsHelper.GetObject(transform);
        if (levelObject == null) return;

        _copyExtraData[copyData] = new ObjectCopyExtraData(levelObject);
    }

    public void ClearCopy() => _copyExtraData.Clear();

    public void ApplyCopy(Transform transform, EditorCopy copyData)
    {
        LevelObject? levelObject = ObjectsHelper.GetObject(transform);
        if (levelObject == null) return;

        if (!_copyExtraData.TryGetValue(copyData, out ObjectCopyExtraData copyExtraData)) return;

        copyExtraData.Apply(levelObject);
    }

    public void Dispose()
    {
        ClearCopy();
    }
}
