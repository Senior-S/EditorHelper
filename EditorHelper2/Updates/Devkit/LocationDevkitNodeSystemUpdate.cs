using EditorHelper2.Extensions.Environment.Nodes;
using EditorHelper2.Loader;
using SDG.Framework.Devkit;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Updates.Devkit;

public static class LocationDevkitNodeSystemUpdate
{
    public static bool Update(LocationDevkitNodeSystem __instance)
    {
        if (!ExtensionManager.TryGetInstance(out NodeNamesExtension? nodeNamesExtension) || nodeNamesExtension == null
            || !SpawnpointSystemV2.Get().IsVisible || !Level.isEditor)
        {
            return true;
        }
        
        foreach (LocationDevkitNode node in LocationDevkitNodeSystem.instance.allNodes)
        {
            Color color = (node.isSelected ? Color.yellow : Color.red);
            RuntimeGizmos.Get().Cube(node.transform.position, node.transform.rotation, 1.5f, color);
            if (nodeNamesExtension.NodeNameToggle.Value)
            {
                RuntimeGizmos.Get().Label(node.transform.position, node.locationName, (node.isSelected ? Color.green : Color.white));
            }
        }

        return false;
    }
}