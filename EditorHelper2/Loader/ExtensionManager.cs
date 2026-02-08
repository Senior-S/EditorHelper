using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using DanielWillett.ReflectionTools;
using DanielWillett.UITools;
using DanielWillett.UITools.API.Extensions;
using EditorHelper2.common;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using Newtonsoft.Json;
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
    
    private static readonly string DisabledExtensionsFile = Path.Combine(Globals.ExtensionsFolder, "disabled_extensions.json");
    private static readonly HashSet<string> _disabledExtensionNames = [];
    
    public static bool TryGetInstance<T>([NotNullWhen(true)] out T? instance) where T : class, IExtension
    {
        UIExtensionInfo? info = UnturnedUIToolsNexus.UIExtensionManager.Extensions.FirstOrDefault(x => x.ImplementationType == typeof(T));
        instance = null;
        
        if (info == null)
        {
            return false;
        }
        
        EHExtensionAttribute? attribute = typeof(T).GetCustomAttribute<EHExtensionAttribute>();
        if (attribute == null || (_instanceStatus.TryGetValue(attribute, out bool isEnabled) && !isEnabled)) return false; // Don't return if the extension is disabled

        instance = info.Instantiations.OfType<T>().LastOrDefault();
        return instance != null;
    }

    public static bool IsEnabled(string extensionName)
    {
        return _instanceStatus.Any(c => c.Key.Name.Equals(extensionName, StringComparison.OrdinalIgnoreCase)) 
               && _instanceStatus.First(c => c.Key.Name.Equals(extensionName, StringComparison.OrdinalIgnoreCase)).Value;
    }

    public static bool IsEnabled<TExtension>() where TExtension : class
    {
        EHExtensionAttribute? attribute = typeof(TExtension).GetCustomAttribute<EHExtensionAttribute>();
        if (attribute == null) return false;

        return !_instanceStatus.TryGetValue(attribute, out bool isEnabled) || isEnabled; // Always enabled extensions are not in _instanceStatus
    }
    
    public static int LoadAllExtensions()
    {
        LoadDisabledExtensions();
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
                    bool isDisabled = _disabledExtensionNames.Contains(extensionAttribute.Name);
                    _instanceStatus[extensionAttribute] = !isDisabled;
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

        if (extensionAttribute.Name.Equals("Discord extension", StringComparison.OrdinalIgnoreCase))
        {
            UpdateDiscordRichPresence(enabled);
        }
        
        if (enabled)
        {
            _disabledExtensionNames.Remove(extensionAttribute.Name);
        }
        else
        {
            _disabledExtensionNames.Add(extensionAttribute.Name);
        }
        SaveDisabledExtensions();
    }

    private static void UpdateDiscordRichPresence(bool value)
    {
        EditorHelper.GetRichPresence().UpdateAnonymous(value);
    }
    
    private static void LoadDisabledExtensions()
    {
        try
        {
            if (!File.Exists(DisabledExtensionsFile))
                return;

            string json = File.ReadAllText(DisabledExtensionsFile);
            List<string>? list = JsonConvert.DeserializeObject<List<string>>(json);
            _disabledExtensionNames.Clear();

            if (list == null) return;

            foreach (string? name in list)
                _disabledExtensionNames.Add(name);
        }
        catch (Exception ex)
        {
            CommandWindow.LogError($"[EditorHelper2] Failed to load disabled extensions: {ex}");
        }
    }

    private static void SaveDisabledExtensions()
    {
        try
        {
            Directory.CreateDirectory(Globals.ExtensionsFolder);

            string json = JsonConvert.SerializeObject(_disabledExtensionNames.ToList(), Formatting.Indented);
            File.WriteAllText(DisabledExtensionsFile, json);
        }
        catch (Exception ex)
        {
            CommandWindow.LogError($"[EditorHelper2] Failed to save disabled extensions: {ex}");
        }
    }
}