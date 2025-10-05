/* Disabled until Sultan fix it */
/*using System.IO;
using System.Reflection;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers;
using EditorHelper2.Patches.UI;
using EditorHelper2.UI.Builders;
using EditorHelper2.UI.Elements;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Menu;

[UIExtension(typeof(MenuDashboardUI))]
[EHExtension("Custom Barns Extension", "JienSultan")]
public class BarnsExtension : UIExtension, IExtension
{
    private static SleekButtonIcon _barnsButton;
    private static MenuBarnsUI? _menuBarnsUI;
    private static BarnAssetManager? _barnAssetManager;

    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;
    
    public BarnsExtension()
    {
        Assembly assembly = typeof(EditorHelper).Assembly; 
        string iconsPath = Path.Combine(Path.GetDirectoryName(assembly.Location) ?? string.Empty, "Assets" ,"Icons.unity3d"); 
        Bundle bundle = Bundles.getBundle(iconsPath, false);
        
        UIBuilder builder = new(200f, 50f);
        builder
            .SetAnchorVertical(1f)
            .SetOffsetVertical(-110f)
            .SetText("Barns");
        _barnsButton = builder.BuildButtonIcon("Open the barns menu", bundle.load<Texture2D>("EditorHelper"));
        
        bundle.unload();
        Initialize();
    }

    public void Initialize()
    {
        if (_container == null) return;
        _container.AddChild(_barnsButton);
        _menuBarnsUI = new MenuBarnsUI();
        _barnAssetManager = new BarnAssetManager();
        
        _barnsButton.onClickedButton += OnBarnsButtonClicked;
        MenuUIPatches.OnEscapePressed += MenuUIPatchesOnEscapePressed;
    }

    #region Event handlers
    private static void OnBarnsButtonClicked(ISleekElement button)
    {
        MenuBarnsUI.Open();
        MenuDashboardUI.close();
        MenuTitleUI.close();
    }
    
    private void MenuUIPatchesOnEscapePressed()
    {
        if (Provider.provider.matchmakingService.isAttemptingServerQuery || !MenuBarnsUI.Active) return;
        
        MenuBarnsUI.Close();
        MenuDashboardUI.open();
        MenuTitleUI.open();
    }
    #endregion Event handlers
    
    public void Dispose()
    {
        if (_container == null) return;
        _container.RemoveChild(_barnsButton);
        _barnsButton.onClickedButton -= OnBarnsButtonClicked;
        MenuUIPatches.OnEscapePressed -= MenuUIPatchesOnEscapePressed;
    }
}*/