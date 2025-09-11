using System.IO;
using System.Reflection;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.UI.Builders;
using HarmonyLib;
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
        _barnsButton = builder.BuildButton("Open the barns menu", bundle.load<Texture2D>("EditorHelper"));
        _barnsButton.fontSize = ESleekFontSize.Medium;
        _barnsButton.iconColor = ESleekTint.FOREGROUND;
        _barnsButton.onClickedButton += OnBarnsButtonClicked;
        
        bundle.unload();
        Initialize();
    }

    public void Initialize()
    {
        if (_container == null) return;
        _container.AddChild(_barnsButton);
        _menuBarnsUI = new MenuBarnsUI();
        _barnAssetManager = new BarnAssetManager();
    }

    #region Event handlers
    
    private static void OnBarnsButtonClicked(ISleekElement button)
    {
        MenuBarnsUI.Open();
        MenuDashboardUI.close();
        MenuTitleUI.close();
    }

    #endregion Event handlers

    #region Extension Functions

    #endregion Extension Functions

    public void Dispose()
    {
        if (_container == null) return;
        _container.RemoveChild(_barnsButton);
        _barnsButton.onClickedButton -= OnBarnsButtonClicked;
    }
}

[HarmonyPatch(typeof(MenuUI), "escapeMenu")]
public class MenuUIEscapeMenuPatch
{ 
    [HarmonyPostfix]
    public static void PostfixEscapeMenu()
    {
        if (!Provider.provider.matchmakingService.isAttemptingServerQuery)
            if (MenuBarnsUI.Active)
            {
                MenuBarnsUI.Close();
                MenuDashboardUI.open();
                MenuTitleUI.open();
            }
    }
}