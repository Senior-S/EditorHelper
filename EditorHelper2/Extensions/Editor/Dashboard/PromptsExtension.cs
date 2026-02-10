using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.UI.Builders;
using SDG.Unturned;
using UnityEngine;
using Action = System.Action;

namespace EditorHelper2.Extensions.Editor.Dashboard;

[UIExtension(typeof(EditorDashboardUI))]
[EHExtension("Prompts extension", "Senior S", true)]
public class PromptsExtension : UIExtension, IExtension
{
    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;
    
    private readonly ISleekBox _alertBox;
    private readonly ISleekButton _acceptButton;

    private readonly ISleekButton _yesButton;
    private readonly ISleekButton _noButton;

    /// <summary>
    /// Action executed if the answer is positive
    /// </summary>
    private Action? _questionAction;
    /// <summary>
    /// Action executed after the user answer.
    /// </summary>
    private Action? _questionPostAction;
    
    public PromptsExtension()
    {
        UIBuilder builder = new(250f, 80f);

        builder.SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(0.15f)
            .SetOffsetHorizontal(-125f);

        _alertBox = builder.BuildBox();

        builder.SetAnchorVertical(1f)
            .SetSizeHorizontal(100f)
            .SetSizeVertical(30f)
            .SetOffsetHorizontal(-50f)
            .SetOffsetVertical(5f)
            .SetSpacing(30f)
            .SetText("Ok");

        _acceptButton = builder.BuildButton(string.Empty);

        builder.SetSpacing(0)
            .SetOffsetHorizontal(-110f)
            .SetText("Yes");
        
        _yesButton = builder.BuildButton(string.Empty);

        builder.SetSpacing(0)
            .SetOffsetHorizontal(10f)
            .SetText("No");
        
        _noButton = builder.BuildButton(string.Empty);
        
        _alertBox.IsVisible = false;
        _acceptButton.IsVisible = false;
        _yesButton.IsVisible = false;
        _noButton.IsVisible = false;
        _alertBox.AddChild(_yesButton);
        _alertBox.AddChild(_noButton);
        _alertBox.AddChild(_acceptButton);
        Initialize();
    }

    public void Initialize()
    {
        if (_container == null) return;
        
        _acceptButton.OnClicked += OnAcceptButtonClicked;
        _yesButton.OnClicked += OnYesButtonClicked;
        _noButton.OnClicked += OnNoButtonClicked;
        
        _container.AddChild(_alertBox);
    }
    
    #region Event handlers
    private void OnAcceptButtonClicked(ISleekElement button)
    {
        _alertBox.IsVisible = false;
        _acceptButton.IsVisible = false;
    }
    
    private void OnYesButtonClicked(ISleekElement button)
    {
        _questionAction?.Invoke();
        
        _questionPostAction?.Invoke();
    }
    
    private void OnNoButtonClicked(ISleekElement button)
    {
        _questionPostAction?.Invoke();
    }
    #endregion Event handlers

    #region Extension Functions
    public void DisplayAlert(string text)
    {
        _alertBox.Text = text;
        _alertBox.IsVisible = true;
        _acceptButton.IsVisible = true;
    }

    /// <summary>
    /// Display a question to the user via the Alert Box
    /// </summary>
    /// <param name="text">Question</param>
    /// <param name="yesAction">Action executed if the answers is positive</param>
    /// <param name="postAction">Action executed after the user have answered</param>
    public void DisplayQuestion(string text, Action yesAction, Action postAction)
    {
        _questionAction = yesAction;
        _questionPostAction = postAction;
        _alertBox.Text = text;
        _alertBox.IsVisible = true;
        _yesButton.IsVisible = true;
        _noButton.IsVisible = true;
    }
    #endregion Extension Functions

    public void Dispose()
    {
        if (_container == null) return;

        _acceptButton.OnClicked -= OnAcceptButtonClicked;
        _yesButton.OnClicked -= OnYesButtonClicked;
        _noButton.OnClicked -= OnNoButtonClicked;
        
        _container.RemoveChild(_alertBox);
    }
}