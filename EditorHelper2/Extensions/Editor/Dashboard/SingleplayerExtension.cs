using System.Collections;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers;
using EditorHelper2.Loader;
using EditorHelper2.UI.Builders;
using SDG.Framework.Utilities;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Editor.Dashboard;

[UIExtension(typeof(EditorDashboardUI))]
[EHExtension("Go to singleplayer extension", "Senior S", true)]
public class SingleplayerExtension : UIExtension, IExtension
{
    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;

    private readonly ISleekButton _singleplayerButton;
    
    public SingleplayerExtension()
    {
        UIBuilder builder = new(130f, 30f);

        builder.SetOffsetHorizontal(150)
            .SetOffsetVertical(-30f)
            .SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(1f)
            .SetText("Join Singleplayer");
        
        _singleplayerButton = builder.BuildButton("Join to the map spawning a player at your camera position");
        
        builder.SetOffsetVertical(-40f)
            .SetText("Back to editor");
        
        Initialize();
    }

    public void Initialize()
    {
        if (_container == null) return;
        
        SingleplayerSharedClass.LevelInfo = null;
        _singleplayerButton.OnClicked += OnSingleplayerClicked;
        
        _container.AddChild(_singleplayerButton);
    }
    
    #region Event handlers
    private void OnSingleplayerClicked(ISleekElement button)
    {
        if (ExtensionManager.TryGetInstance(out PromptsExtension? promptsExtension) && promptsExtension != null)
        {
            promptsExtension.DisplayQuestion("Save the level before joining singleplayer?", SDG.Unturned.Level.save,
                () =>
                {
                    SingleplayerSharedClass.LevelInfo = SDG.Unturned.Level.info;
                    SingleplayerSharedClass.CameraPosition = MainCamera.instance.transform.parent.position;
                    SingleplayerSharedClass.CameraRotation = MainCamera.instance.transform.parent.eulerAngles.y;
                    TimeUtility.singleton.StartCoroutine(SendToSingleplayer());
                });    
        }
    }
    #endregion Event handlers

    #region Extension Functions
    private IEnumerator SendToSingleplayer()
    {
        SDG.Unturned.Level.exit();
        yield return new WaitUntil(() => SDG.Unturned.Level.isExiting == false);
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        Provider.map = SingleplayerSharedClass.LevelInfo!.name;
        Provider.singleplayer(EGameMode.EASY, true);
        yield break;
    }
    #endregion Extension Functions

    public void Dispose()
    {
        if (_container == null) return;
        
        _singleplayerButton.OnClicked -= OnSingleplayerClicked;
        
        _container.RemoveChild(_singleplayerButton);
    }
}