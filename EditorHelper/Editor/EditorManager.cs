using System.Collections;
using EditorHelper.Builders;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Editor;

public class EditorManager
{
    private LevelInfo _levelInfo;
    
    private readonly SleekButtonIcon _singleplayerButton;
    private readonly SleekButtonIcon _editorButton;
    
    private readonly ISleekBox _alertBox;
    private readonly SleekButtonIcon _acceptButton;

    private Vector3 _cameraPosition = Vector3.zero;

    public EditorManager()
    {
        ButtonBuilder builder = new(positionScaleX: 0.5f);
        
        builder.SetPositionOffsetX(150f)
            .SetPositionOffsetY(-30f)
            .SetText("Join Singleplayer");

        _singleplayerButton = builder.BuildButton("Join to the map spawning a player at your camera position");
        _singleplayerButton.onClickedButton += OnSingleplayerClicked;

        Provider.onEnemyConnected += OnEnemyConnected;
        
        builder
            .SetPositionOffsetY(-40f)
            .SetText("Back to editor");
        _editorButton = builder.BuildButton("Join to the map spawning a player at your camera position");
        _editorButton.onClickedButton += OnEditorClicked;

        builder.SetSizeOffsetX(250)
            .SetSizeOffsetY(80f)
            .SetPositionScaleX(0.5f)
            .SetPositionScaleY(0.15f);
        
        builder.SetPositionOffsetX(-125f);
        _alertBox = builder.BuildBox();

        builder.SetSizeOffsetX(100f)
            .SetSizeOffsetY(30f)
            .SetText("Ok")
            .SetPositionOffsetX(-50f)
            .SetOneTimeSpacing(30f);
        
        _acceptButton = builder.BuildButton(string.Empty);
        _acceptButton.onClickedButton += OnAcceptButtonClicked;
        _alertBox.IsVisible = false;
        _acceptButton.IsVisible = false;
    }

    public void Initialize()
    {
        EditorDashboardUI.container.AddChild(_singleplayerButton);
        
        EditorDashboardUI.container.AddChild(_alertBox);
        EditorDashboardUI.container.AddChild(_acceptButton);
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
    
    private void OnEnemyConnected(SteamPlayer player)
    {
        Level.instance.StartCoroutine(TeleportPlayer(player));
    }

    private void OnSingleplayerClicked(ISleekElement button)
    {
        _levelInfo = Level.info;
        _cameraPosition = MainCamera.instance.transform.parent.position;
        
        Level.save();
        Level.instance.StartCoroutine(SendToSingleplayer());
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
        Level.instance.StartCoroutine(SendToEditor());
    }
}