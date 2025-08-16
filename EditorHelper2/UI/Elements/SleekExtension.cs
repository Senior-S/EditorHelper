using EditorHelper2.API.Attributes;
using EditorHelper2.Loader;
using EditorHelper2.UI.Builders;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.UI.Elements;

public class SleekExtension : SleekWrapper
{
    private readonly EHExtensionAttribute _extensionAttribute;
    
    public SleekExtension(EHExtensionAttribute extensionAttribute, int i)
    {
        _extensionAttribute = extensionAttribute;
        UIBuilder builder = new(0, 0);
        
        builder.ResetProperties()
            .SetScaleHorizontal(1f)
            .SetScaleVertical(1f);
        
        ISleekBox box = builder.BuildBox();
        AddChild(box);
        builder.ResetProperties()
            .SetOffsetVertical(-21f)
            .SetAnchorVertical(0.5f)
            .SetSizeHorizontal(42f)
            .SetSizeVertical(42f)
            .SetText(extensionAttribute.Name + $"[{i}]");

        ISleekToggle toggle = builder.BuildToggle($"Toggle the status of {extensionAttribute.Name}");
        toggle.Value = ExtensionManager.Instances[extensionAttribute];
        toggle.OnValueChanged += OnExtensionToggleChanged;
        AddChild(toggle);
        
        builder.ResetProperties()
            .SetOffsetHorizontal(-5f)
            .SetSizeVertical(42f)
            .SetScaleHorizontal(1f)
            .SetText($"by {extensionAttribute.Author}");

        ISleekLabel label = builder.BuildLabel(TextAnchor.MiddleRight);
        AddChild(label);
    }

    private void OnExtensionToggleChanged(ISleekToggle toggle, bool state)
    {
        ExtensionManager.UpdateExtensionStatus(_extensionAttribute, state);
    }
}