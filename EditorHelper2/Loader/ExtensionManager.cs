using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DanielWillett.ReflectionTools;
using EditorHelper2.API.Attributes;
using SDG.Unturned;

namespace EditorHelper2.Loader;

/// <summary>
/// Class with required methods to manage the module extensions
/// </summary>
public static class ExtensionManager
{
    /// <summary>
    /// Dictionary containing the enabled/disabled value of each extension
    /// </summary>
    private static readonly Dictionary<EHExtensionAttribute, bool> _instanceStatus = [];
    public static IReadOnlyDictionary<EHExtensionAttribute, bool> Instances => _instanceStatus;
    
    public static int LoadAllExtensions()
    {
        int loadedExtensions = 0;
        
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (Assembly asm in assemblies)
        {
            IEnumerable<Type> types = asm.GetTypes()
                .Where(t => t.TryGetAttributeSafe<EHExtensionAttribute>(out _)
                            && !t.IsAbstract);

            foreach (Type? type in types)
            {
                EHExtensionAttribute extensionAttribute = type.GetCustomAttribute<EHExtensionAttribute>();
                
                if (!extensionAttribute.AlwaysEnabled)
                {
                    _instanceStatus[extensionAttribute] = true;
                }
                
                CommandWindow.LogFormat("[EditorHelper2] Extension {0} loaded successfully.", extensionAttribute.Name);
                loadedExtensions++;
            }
        }

        return loadedExtensions;
    }

    public static void UpdateExtensionStatus(EHExtensionAttribute extensionAttribute, bool enabled)
    {
        if (!_instanceStatus.ContainsKey(extensionAttribute)) return;
        _instanceStatus[extensionAttribute] = enabled;
    }
}