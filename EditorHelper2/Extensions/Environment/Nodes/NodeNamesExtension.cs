using DanielWillett.UITools.API.Extensions;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.UI.Builders;
using SDG.Unturned;

namespace EditorHelper2.Extensions.Environment.Nodes;

[UIExtension(typeof(EditorEnvironmentNodesUI))]
[EHExtension("Node Names extension", "Senior S")]
public class NodeNamesExtension : UIExtension, IExtension
{
    public readonly ISleekToggle NodeNameToggle;

    public NodeNamesExtension()
    {
        UIBuilder builder = new(40f, 40f);

        builder.SetOffsetHorizontal(200f)
            .SetOffsetVertical(-75f)
            .SetAnchorHorizontal(0f)
            .SetAnchorVertical(0.5f)
            .SetText("Display node names in world");

        NodeNameToggle = builder.BuildToggle("Should name nodes display it's name as a text in the world?");
        
        Initialize();
    }

    public void Initialize()
    {
        object? container = this.Instance;
        if (container is not EditorEnvironmentNodesUI nodesUI) return;
        
        nodesUI.AddChild(NodeNameToggle);
    }

    public void Dispose()
    {
        object? container = this.Instance;
        if (container is not EditorEnvironmentNodesUI nodesUI) return;
        
        nodesUI.RemoveChild(NodeNameToggle);
    }
}