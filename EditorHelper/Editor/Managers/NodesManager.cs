using EditorHelper.Builders;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Editor.Managers;

public class NodesManager
{
    private readonly ISleekToggle _nodeNameToggle;
    
    public NodesManager()
    {
        UIBuilder builder = new(40f, 40f);
        
        builder.SetPositionOffsetX(200f)
            .SetPositionOffsetY(-75f)
            .SetText("Display node names in world");
        
        _nodeNameToggle = builder.BuildToggle("Should name nodes display it's name as a text in the world?");
    }

    public void Initialize(ref EditorEnvironmentNodesUI uiInstance)
    {
        uiInstance.AddChild(_nodeNameToggle);
        LevelVisibility.nodesVisible = false;
    }

    public void CustomUpdate()
    {
        foreach (LocationDevkitNode node in LocationDevkitNodeSystem.instance.allNodes)
        {
            Color color = (node.isSelected ? Color.yellow : Color.red);
            RuntimeGizmos.Get().Cube(node.transform.position, node.transform.rotation, 1.5f, color);
            if (_nodeNameToggle.Value)
            {
                RuntimeGizmos.Get().Label(node.transform.position, node.locationName, (node.isSelected ? Color.green : Color.white));
            }
        }
    }
}