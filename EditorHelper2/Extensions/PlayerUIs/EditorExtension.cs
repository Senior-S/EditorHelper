using System.Collections;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers;
using EditorHelper2.UI.Builders;
using SDG.Framework.Utilities;
using SDG.Provider;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.PlayerUIs;

[UIExtension(typeof(PlayerUI))]
[EHExtension("Go to editor extension", "Senior S", true)]
public class EditorExtension : UIExtension, IExtension
{
    [ExistingMember("container")] 
    private readonly ISleekElement? _container;
    
    private readonly ISleekButton _editorButton;

    public EditorExtension()
    {
        UIBuilder builder = new(130f, 30f);

        builder.SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(150f)
            .SetOffsetVertical(-40f)
            .SetText("Join Editor");
        
        _editorButton = builder.BuildButton("Join the map editor ");

        Initialize();
    }

    public void Initialize()
    {
        if (_container == null || SingleplayerSharedClass.LevelInfo == null) return;
        
        _editorButton.OnClicked += OnEditorClicked;
        
        _container.AddChild(_editorButton);

        TimeUtility.singleton.StartCoroutine(TeleportPlayer(Player.LocalPlayer));
    }

    #region Event handlers
    private void OnEditorClicked(ISleekElement button)
    {
        TimeUtility.singleton.StartCoroutine(SendToEditor());
    }
    #endregion Event handlers

    #region Extension Functions
    private IEnumerator TeleportPlayer(Player player)
    { 
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        player.teleportToLocation(SingleplayerSharedClass.CameraPosition, 0f);
        
        yield break;
    }
    
    private IEnumerator SendToEditor()
    {
        Provider.RequestDisconnect("Going back to editor");
        yield return new WaitUntil(() => SDG.Unturned.Level.isExiting == false);
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        SDG.Unturned.Level.edit(SingleplayerSharedClass.LevelInfo);
        yield break;
    }
    #endregion Extension Functions

    public void Dispose()
    {
        if (_container == null) return;
        
        _editorButton.OnClicked -= OnEditorClicked;

        _container.RemoveChild(_editorButton);
    }
}