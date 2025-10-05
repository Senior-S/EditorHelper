using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DanielWillett.ReflectionTools;
using DanielWillett.UITools;
using DanielWillett.UITools.API.Extensions;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using SDG.Provider;
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

    public static bool TryGetInstance<T>(out T? instance) where T : class, IExtension
    {
        UIExtensionInfo? info = UnturnedUIToolsNexus.UIExtensionManager.Extensions.FirstOrDefault(x => x.ImplementationType == typeof(T));
        instance = null;
        
        if (info == null)
        {
            return false;
        }
        
        EHExtensionAttribute? attribute = typeof(T).GetCustomAttribute<EHExtensionAttribute>();
        if (attribute == null || (_instanceStatus.ContainsKey(attribute) && !_instanceStatus[attribute])) return false; // Don't return if the extension is disabled
        
        instance = info.Instantiations.OfType<T>().LastOrDefault()!;
        return true;

    }
    
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