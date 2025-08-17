using System;
using System.IO;
using System.Linq;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.API.Attributes;
using EditorHelper2.Loader;
using EditorHelper2.Patches.UI;
using EditorHelper2.UI.Builders;
using EditorHelper2.UI.Elements;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Menu;

[UIExtension(typeof(MenuWorkshopUI))]
[EHExtension("Extensions Menu", "Senior S", alwaysEnabled: true)]
public class ExtensionsMenu : UIExtension, IDisposable
{
    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _originalContainer;

    private bool _active;
    private readonly SleekButtonIcon _extensionsButton;
    private readonly SleekButtonIcon _backButton;
    private readonly SleekFullscreenBox _container;
    private readonly ISleekBox _headerBox;
    private ISleekScrollView _extensionsScrollView;
    
    public ExtensionsMenu()
    {
        _active = false;
        UIBuilder builder = new(200f, 50f);

        builder.SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(0.5f)
            .SetOffsetHorizontal(-100f)
            .SetOffsetVertical(-175f)
            .SetText("Extensions");

		string iconsPath = Path.Combine(Environment.CurrentDirectory, "EditorHelper2", "Assets", "Icons.unity3d");
        Bundle bundle = Bundles.getBundle(iconsPath, false);

        _extensionsButton = builder.BuildButton("Open the extensions menu", bundle.load<Texture2D>("EditorHelper"));

        builder.SetOffsetHorizontal(10f)
            .SetOffsetVertical(10f)
            .SetSizeHorizontal(-20f)
            .SetSizeVertical(-20f)
            .SetAnchorHorizontal(0)
            .SetAnchorVertical(0)
            .SetScaleHorizontal(1f)
            .SetScaleVertical(1f);
        _container = builder.BuildFullscreenBox();
        
        bundle.unload();
        bundle = Bundles.getBundle("/Bundles/Textures/Menu/Icons/MenuDashboard/MenuDashboard.unity3d");

        builder.SetSizeHorizontal(200f)
            .SetSizeVertical(50f)
            .SetAnchorHorizontal(0f)
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(5f)
            .SetOffsetVertical(-55f)
            .SetScaleHorizontal(0f)
            .SetScaleVertical(0f)
            .SetText("Back");

        _backButton = builder.BuildButton("Back", bundle.load<Texture2D>("Exit"), ESleekFontSize.Medium);

        builder.ResetProperties()
            .SetSizeVertical(50f)
            .SetScaleHorizontal(1f)
            .SetText("Editor Helper Extensions");

        _headerBox = builder.BuildBox();

        builder.ResetProperties()
            .SetOffsetVertical(60f)
            .SetSizeVertical(-120f)
            .SetScaleHorizontal(1f)
            .SetScaleVertical(1f);

        _extensionsScrollView = builder.BuildScrollView(scaleContentToWidth: true);
        PopulateExtensions();
        
        Initialize();
    }

    private void PopulateExtensions()
    {
        UIBuilder builder = new(0, 42f);
        for (int i = 0; i < ExtensionManager.Instances.Count; i++)
        {
            EHExtensionAttribute extensionAttribute = ExtensionManager.Instances.Keys.ElementAt(i);

            builder.SetOffsetVertical(i * 50)
                .SetScaleHorizontal(1f);

            SleekExtension sleekExtension = new(extensionAttribute);
            builder.FormatElement(ref sleekExtension);
            
            _extensionsScrollView.AddChild(sleekExtension);
        }
        _extensionsScrollView.ContentSizeOffset = new Vector2(0f, ExtensionManager.Instances.Count * 50 - 10);
        _extensionsScrollView.NormalizedStateCenter = new Vector2(0.5f, 1);
        
    }

    private void Initialize()
    {
        if (_originalContainer == null) return;
        
        _originalContainer.AddChild(_extensionsButton);
        _container.AddChild(_backButton);
        _container.AddChild(_headerBox);
        _container.AddChild(_extensionsScrollView);
        MenuUI.container.AddChild(_container);
        _container.AnimateOutOfView(0f, -1f);
        
        _extensionsButton.onClickedButton += OnExtensionsButtonClicked;
        _backButton.onClickedButton += OnBackButtonClicked;
        MenuUIPatches.OnEscapePressed += OnEscapePressed;
    }

    private void OnEscapePressed()
    {
        Close();
    }

    private void OnBackButtonClicked(ISleekElement button)
    {
        Close();
        MenuWorkshopUI.open();
    }

    private void OnExtensionsButtonClicked(ISleekElement button)
    {
        Open();
        MenuWorkshopUI.close();
    }

    private void Open()
    {
        if (!_active)
        {
            _active = true;
            _container.AnimateIntoView();
        }
    }

    private void Close()
    {
        if (_active)
        {
            _active = false;
            _container.AnimateOutOfView(0f, -1f);
        }
    }
    
    public void Dispose()
    {
        if (_originalContainer == null) return;
        
        _originalContainer.RemoveChild(_extensionsButton);
        _container.RemoveChild(_backButton);
        _container.RemoveChild(_headerBox);
        _container.RemoveChild(_extensionsScrollView);
        MenuUI.container.RemoveChild(_container);
        
        _extensionsButton.onClickedButton -= OnExtensionsButtonClicked;
        _backButton.onClickedButton -= OnBackButtonClicked;
        MenuUIPatches.OnEscapePressed -= OnEscapePressed;
    }
}