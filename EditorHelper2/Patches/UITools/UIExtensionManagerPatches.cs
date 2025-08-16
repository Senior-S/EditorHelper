using System;
using System.Collections.Generic;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.Core.Extensions;
using EditorHelper2.API.Attributes;
using EditorHelper2.Loader;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper2.Patches.UITools;

[HarmonyPatch(typeof(UIExtensionManager))]
public class UIExtensionManagerPatches
{
    [HarmonyPatch("InitializeExtension")]
    [HarmonyPrefix]
    [UsedImplicitly]
    static bool PrefixInitializeExtension(UIExtensionInfo info)
    {
        EHExtensionAttribute? attribute = (EHExtensionAttribute?)Attribute.GetCustomAttribute(info.ImplementationType, typeof(EHExtensionAttribute));
        
        return attribute == null || ExtensionManager.Instances.GetValueOrDefault(attribute, true);
    }

    [HarmonyPatch("CreateExtension")]
    [HarmonyPrefix]
    [UsedImplicitly]
    static bool PrefixCreateExtension(UIExtensionInfo info, object? uiInstance)
    {
        EHExtensionAttribute? attribute = (EHExtensionAttribute?)Attribute.GetCustomAttribute(info.ImplementationType, typeof(EHExtensionAttribute));

        return attribute == null || ExtensionManager.Instances.GetValueOrDefault(attribute, true);
    }
}