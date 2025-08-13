using System;
using System.Collections.Generic;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.Core.Extensions;
using EditorHelper2.API.Attributes;
using EditorHelper2.Loader;
using HarmonyLib;
using JetBrains.Annotations;

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

        if (attribute == null)
        {
            return true;
        }

        return ExtensionManager.Instances.GetValueOrDefault(attribute.Name, true);
    }
}