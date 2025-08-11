using System;
using DanielWillett.UITools.API.Extensions;
using EditorHelper2.Loader;
using SDG.Unturned;

namespace EditorHelper2.API.Abstract;

public abstract class LevelObjectsExtension : UIExtension
{
    /// <summary>
    /// Is the extension enabled?
    /// </summary>
    public bool IsEnabled => !ExtensionManager.Instances.TryGetValue(GetType().Name, out bool value) || value;
    
    /// <summary>
    /// Event invoked after <see cref="EditorObjects.calculateHandleOffsets"/> is called.
    /// </summary>
    public static event Action<EditorObjects> OnCalculateHandleOffsets;
}