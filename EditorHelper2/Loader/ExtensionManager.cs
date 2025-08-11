using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EditorHelper2.API.Abstract;
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
    private static readonly Dictionary<string, bool> _instanceStatus = [];
    public static IReadOnlyDictionary<string, bool> Instances => _instanceStatus;
    
    public static int LoadAllExtensions()
    {
        int loadedExtensions = 0;
        
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (Assembly asm in assemblies)
        {
            IEnumerable<Type> types = asm.GetTypes()
                .Where(t => typeof(LevelObjectsExtension).IsAssignableFrom(t) 
                            && !t.IsAbstract);

            foreach (Type? type in types)
            {
                AlwaysEnabledAttribute alwaysEnabled = type.GetCustomAttribute<AlwaysEnabledAttribute>();
                if (alwaysEnabled == null)
                { 
                    _instanceStatus[type.Name] = false;
                }
                
                CommandWindow.LogFormat("[EditorHelper2] Extension {0} loaded successfully.", type.Name);
                loadedExtensions++;
            }
        }

        return loadedExtensions;
    }
}