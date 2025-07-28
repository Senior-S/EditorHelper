using System.Collections;
using EditorHelper.Builders;
using SDG.Framework.Utilities;
using SDG.Unturned;
using UnityEngine;
using Action = System.Action;

namespace EditorHelper.Editor;

public class EditorManager
{
    private LevelInfo _levelInfo;
    
    private readonly SleekButtonIcon _singleplayerButton;
    private readonly SleekButtonIcon _editorButton;
    
    private readonly SleekButtonIcon _docsButton;
    
    private readonly ISleekBox _alertBox;
    private readonly SleekButtonIcon _acceptButton;

    private readonly SleekButtonIcon _yesButton;
    private readonly SleekButtonIcon _noButton;

    private Vector3 _cameraPosition = Vector3.zero;

    /// <summary>
    /// Action executed if the answer is positive
    /// </summary>
    private Action _questionAction;
    /// <summary>
    /// Action executed after the user answer.
    /// </summary>
    private Action _questionPostAction;
    
    // Satellite stuff
    private readonly SleekButtonIcon _2xResolution;
    private readonly SleekButtonIcon _4xResolution;
    private readonly ISleekInt32Field _widthResolution;
    private readonly ISleekInt32Field _heightResolution;

    public int? Multiplier;
    public uint? CustomWidth;
    public uint? CustomHeight;
    public bool ShouldModifyResolution = false;

    public void ResetCustomResolution()
    {
        _widthResolution.Value = 0;
        _heightResolution.Value = 0;
        Multiplier = null;
        CustomWidth = null;
        CustomHeight = null;
        
        _widthResolution.BackgroundColor = SleekColor.BackgroundIfLight(Color.black);
        _heightResolution.BackgroundColor = SleekColor.BackgroundIfLight(Color.black);
        _2xResolution.backgroundColor = SleekColor.BackgroundIfLight(Color.black);
        _4xResolution.backgroundColor = SleekColor.BackgroundIfLight(Color.black);
        
        ShouldModifyResolution = false;
    }

    public EditorManager()
    {
        UIBuilder builder = new(positionScaleX: 0.5f);
        
        builder.SetPositionOffsetX(150f)
            .SetPositionOffsetY(-30f)
            .SetText("Join Singleplayer");

        _singleplayerButton = builder.BuildButton("Join to the map spawning a player at your camera position");
        _singleplayerButton.onClickedButton += OnSingleplayerClicked;

        Provider.onEnemyConnected += OnEnemyConnected;
        
        builder.SetPositionOffsetY(-40f)
            .SetText("Back to editor");
        _editorButton = builder.BuildButton("Join to the map spawning a player at your camera position");
        _editorButton.onClickedButton += OnEditorClicked;

        builder.SetSizeOffsetX(250)
            .SetSizeOffsetY(80f)
            .SetPositionScaleX(0.5f)
            .SetPositionScaleY(0.15f);
        
        builder.SetPositionOffsetX(-125f);
        _alertBox = builder.BuildBox();

        builder.SetPositionScaleX(0.5f)
            .SetPositionScaleY(1f)
            .SetSizeOffsetX(100f)
            .SetSizeOffsetY(30f)
            .SetText("Ok")
            .SetPositionOffsetX(-50f)
            .SetPositionOffsetY(5f)
            .SetOneTimeSpacing(30f);

        _acceptButton = builder.BuildButton(string.Empty);
        _acceptButton.onClickedButton += OnAcceptButtonClicked;
        
        builder.SetPositionScaleX(0.5f)
            .SetPositionScaleY(1f)
            .SetOneTimeSpacing(0)
            .SetPositionOffsetX(-110f)
            .SetText("Yes");
        
        _yesButton = builder.BuildButton(string.Empty);
        _yesButton.onClickedButton += (_) =>
        {
            _questionAction?.Invoke();
            
            _questionPostAction?.Invoke();
        };

        builder.SetOneTimeSpacing(0)
            .SetPositionOffsetX(10f)
            .SetText("No");
        
        _noButton = builder.BuildButton(string.Empty);
        _noButton.onClickedButton += (_) =>
        {
            _questionPostAction?.Invoke();
        };

        builder.SetPositionScaleX(0.5f)
            .SetPositionScaleY(0.5f)
            .SetOneTimeSpacing(0)
            .SetSizeOffsetX(200f)
            .SetSizeOffsetY(30f)
            .SetPositionOffsetX(-100f)
            .SetPositionOffsetY(185f)
            .SetText("Documentation");
        
        _docsButton = builder.BuildButton("Open the documentation website");
        _docsButton.onClickedButton += (_) => Provider.openURL("https://editorhelper.sshost.club/");

        builder.SetOneTimeSpacing(0)
            .SetPositionOffsetX(-145f)
            .SetPositionOffsetY(-55f)
            .SetSizeOffsetX(40f)
            .SetSizeOffsetY(30f)
            .SetText("2x");
        
        _2xResolution = builder.BuildButton("2x satellite resolution");
        _2xResolution.onClickedButton += (_) =>
        {
            if (Multiplier == 2)
            {
                Multiplier = null;
                _2xResolution.backgroundColor = SleekColor.BackgroundIfLight(Color.black);
                return;
            }
            Multiplier = 2;
            _2xResolution.backgroundColor = SleekColor.BackgroundIfLight(Color.white);
            ShouldModifyResolution = true;
        };

        builder.SetOneTimeSpacing(0)
            .SetPositionOffsetX(-187.5f)
            .SetText("4x");
        
        _4xResolution = builder.BuildButton("4x satellite resolution");
        _4xResolution.onClickedButton  += (_) =>
        {
            if (Multiplier == 4)
            {
                Multiplier = null;
                _4xResolution.backgroundColor = SleekColor.BackgroundIfLight(Color.black);
                return;
            }
            Multiplier = 4;
            _4xResolution.backgroundColor = SleekColor.BackgroundIfLight(Color.white);
            ShouldModifyResolution = true;
        };

        builder.SetOneTimeSpacing(0)
            .SetPositionOffsetX(-255f)
            .SetPositionOffsetY(-70f)
            .SetSizeOffsetX(65f)
            .SetText("Width");
        
        _widthResolution = builder.BuildInt32Field("Width resolution");
        _widthResolution.OnValueChanged += (field, value) =>
        {
            if (_widthResolution.Value < 1)
            {
                CustomWidth = null;
                _widthResolution.Value = 0;
                _widthResolution.BackgroundColor = SleekColor.BackgroundIfLight(Color.black);
                return;
            }
            
            CustomWidth = (uint)value;
            ShouldModifyResolution = true;
            _widthResolution.BackgroundColor = SleekColor.BackgroundIfLight(Color.white);
        };
        
        builder.SetOneTimeSpacing(0)
            .SetPositionOffsetY(-40f)
            .SetText("Height");
        
        _heightResolution = builder.BuildInt32Field("Height resolution");
        _heightResolution.OnValueChanged += (field, value) =>
        {
            if (_heightResolution.Value < 1)
            {
                CustomHeight = null;
                _heightResolution.Value = 0;
                _heightResolution.BackgroundColor = SleekColor.BackgroundIfLight(Color.black);
                return;
            }
            
            CustomHeight = (uint)value;
            ShouldModifyResolution = true;
            _heightResolution.BackgroundColor = SleekColor.BackgroundIfLight(Color.white);
        };
        
        _alertBox.IsVisible = false;
        _acceptButton.IsVisible = false;
        _yesButton.IsVisible = false;
        _noButton.IsVisible = false;
        _alertBox.AddChild(_yesButton);
        _alertBox.AddChild(_noButton);
        _alertBox.AddChild(_acceptButton);
    }

    public void Initialize()
    {
        EditorDashboardUI.container.AddChild(_singleplayerButton);
        
        EditorDashboardUI.container.AddChild(_alertBox);
        EditorPauseUI.container.AddChild(_docsButton);
        EditorPauseUI.container.AddChild(_2xResolution);
        EditorPauseUI.container.AddChild(_4xResolution);
        EditorPauseUI.container.AddChild(_widthResolution);
        EditorPauseUI.container.AddChild(_heightResolution);
    }
    
    private void OnAcceptButtonClicked(ISleekElement button)
    {
        _alertBox.IsVisible = false;
        _acceptButton.IsVisible = false;
    }

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
    
    private void OnEnemyConnected(SteamPlayer player)
    {
        TimeUtility.singleton.StartCoroutine(TeleportPlayer(player));
    }

    private void OnSingleplayerClicked(ISleekElement button)
    {
        DisplayQuestion("Save the level before joining singleplayer?", Level.save,
            () =>
            {
                _levelInfo = Level.info;
                _cameraPosition = MainCamera.instance.transform.parent.position;
                TimeUtility.singleton.StartCoroutine(SendToSingleplayer());
            });
    }

    private IEnumerator TeleportPlayer(SteamPlayer player)
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        player.player.teleportToLocation(_cameraPosition, 0f);
        PlayerUI.container.AddChild(_editorButton);

        yield break;
    }

    private IEnumerator SendToSingleplayer()
    {
        Level.exit();
        EditorHelper.Instance.ObjectsManager = null;
        EditorHelper.Instance.VisibilityManager = null;
        yield return new WaitUntil(() => Level.isExiting == false);
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        Provider.map = _levelInfo.name;
        Provider.singleplayer(EGameMode.EASY, true);
        yield break;
    }

    private IEnumerator SendToEditor()
    {
        Provider.RequestDisconnect("Going back to editor");
        yield return new WaitUntil(() => Level.isExiting == false);
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        Level.edit(_levelInfo);
        yield break;
    }
    
    private void OnEditorClicked(ISleekElement button)
    {
        TimeUtility.singleton.StartCoroutine(SendToEditor());
    }
}