using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.UI.Builders;
using SDG.Unturned;

namespace EditorHelper2.Extensions.Editor.Pause;

[UIExtension(typeof(EditorPauseUI))]
[EHExtension("Documentation extension", "Senior S", true)]
public class DocumentationExtension : UIExtension, IExtension
{
    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;
    
    private readonly ISleekButton _docsButton;
    
    public DocumentationExtension()
    {
        UIBuilder builder = new(200f, 30f);

        builder.SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(0.5f)
            .SetOffsetHorizontal(-100f)
            .SetOffsetVertical(185f)
            .SetText("Documentation");

        _docsButton = builder.BuildButton("Open the documentation website");
        Initialize();
    }
    
    public void Initialize()
    {
        if (_container == null) return;
        
        _docsButton.OnClicked += OnDocumentationButtonClicked;
        _container.AddChild(_docsButton);
    }

    #region Event handlers
    private void OnDocumentationButtonClicked(ISleekElement button)
    {
        Provider.openURL("https://editorhelper.sshost.club/");
    }
    #endregion Event handlers

    public void Dispose()
    {
        if (_container == null) return;
        
        _docsButton.OnClicked -= OnDocumentationButtonClicked;
        _container.RemoveChild(_docsButton);
    }
}