using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using DanielWillett.UITools.Util;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Keybinds;
using EditorHelper2.UI.Builders;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Editor.Pause;

[UIExtension(typeof(EditorPauseUI))]
[EHExtension("EditorHelper2 Keybinds", "Senior S", alwaysEnabled: true)]
public class KeybindsMenuExtension : UIExtension, IExtension
{
    private static KeybindsMenuExtension? _instance;

    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _container;

    [ExistingMember("container", OwningType = typeof(EditorDashboardUI))]
    private readonly SleekFullscreenBox? _dashboardContainer;

    [ExistingMember("container", OwningType = typeof(EditorTerrainUI))]
    private readonly SleekFullscreenBox? _terrainContainer;

    [ExistingMember("container", OwningType = typeof(EditorEnvironmentUI))]
    private readonly SleekFullscreenBox? _environmentContainer;

    [ExistingMember("container", OwningType = typeof(EditorSpawnsUI))]
    private readonly SleekFullscreenBox? _spawnsContainer;

    [ExistingMember("container", OwningType = typeof(EditorLevelUI))]
    private readonly SleekFullscreenBox? _levelContainer;

    [ExistingMember("container", OwningType = typeof(EditorLevelVisibilityUI))]
    private readonly SleekFullscreenBox? _levelVisibilityContainer;

    [ExistingMember("container", OwningType = typeof(EditorLevelPlayersUI))]
    private readonly SleekFullscreenBox? _levelPlayersContainer;

    [ExistingMember("container", OwningType = typeof(EditorEnvironmentLightingUI))]
    private readonly SleekFullscreenBox? _environmentLightingContainer;

    [ExistingMember("container", OwningType = typeof(EditorEnvironmentRoadsUI))]
    private readonly SleekFullscreenBox? _environmentRoadsContainer;

    [ExistingMember("container", OwningType = typeof(EditorEnvironmentNavigationUI))]
    private readonly SleekFullscreenBox? _environmentNavigationContainer;

    [ExistingMember("container", OwningType = typeof(EditorSpawnsAnimalsUI))]
    private readonly SleekFullscreenBox? _spawnsAnimalsContainer;

    [ExistingMember("container", OwningType = typeof(EditorSpawnsItemsUI))]
    private readonly SleekFullscreenBox? _spawnsItemsContainer;

    [ExistingMember("container", OwningType = typeof(EditorSpawnsZombiesUI))]
    private readonly SleekFullscreenBox? _spawnsZombiesContainer;

    [ExistingMember("container", OwningType = typeof(EditorSpawnsVehiclesUI))]
    private readonly SleekFullscreenBox? _spawnsVehiclesContainer;

    private readonly ISleekButton _openButton;
    private readonly SleekFullscreenBox _root;
    private readonly ISleekBox _screenDimmer;
    private readonly SleekFullscreenBox _panel;
    private readonly ISleekBox _backgroundDimmer;
    private readonly ISleekBox _headerBox;
    private readonly ISleekLabel _hintLabel;
    private readonly ISleekScrollView _scrollView;
    private readonly ISleekButton _resetButton;
    private readonly ISleekButton _closeButton;

    private readonly Dictionary<string, ISleekButton> _bindingButtons = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ISleekElement, bool> _hiddenElements = new();
    private bool _active;

    public KeybindsMenuExtension()
    {
        _instance = this;
        _active = false;
        UIBuilder builder = new(200f, 50f);

        builder.SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(0.5f)
            .SetOffsetHorizontal(110f)
            .SetOffsetVertical(185f)
            .SetText("EditorHelper2 Keybinds");
        _openButton = builder.BuildButton("Open EditorHelper2 keybinds");

        builder.ResetProperties()
            .SetScaleHorizontal(1f)
            .SetScaleVertical(1f);
        _root = builder.BuildFullscreenBox();

        builder.ResetProperties()
            .SetScaleHorizontal(1f)
            .SetScaleVertical(1f);
        _screenDimmer = builder.BuildColoredBox(new SleekColor(ESleekTint.BACKGROUND, 1f));

        builder.ResetProperties()
            .SetOffsetHorizontal(10f)
            .SetOffsetVertical(10f)
            .SetSizeHorizontal(-20f)
            .SetSizeVertical(-20f)
            .SetScaleHorizontal(1f)
            .SetScaleVertical(1f);
        _panel = builder.BuildFullscreenBox();

        builder.ResetProperties()
            .SetScaleHorizontal(1f)
            .SetScaleVertical(1f);
        _backgroundDimmer = builder.BuildColoredBox(new SleekColor(ESleekTint.BACKGROUND, 1f));

        builder.ResetProperties()
            .SetSizeVertical(40f)
            .SetScaleHorizontal(1f)
            .SetText("EditorHelper2 Keybinds");
        _headerBox = builder.BuildBox();

        builder.ResetProperties()
            .SetOffsetVertical(45f)
            .SetSizeVertical(20f)
            .SetScaleHorizontal(1f)
            .SetText("Click a binding, then press a key. Esc to cancel, Backspace to clear.");
        _hintLabel = builder.BuildLabel(TextAnchor.MiddleCenter, ESleekFontSize.Small);

        builder.ResetProperties()
            .SetOffsetHorizontal(20f)
            .SetOffsetVertical(70f)
            .SetSizeHorizontal(-40f)
            .SetSizeVertical(-140f)
            .SetScaleHorizontal(1f)
            .SetScaleVertical(1f);
        _scrollView = builder.BuildScrollView(scaleContentToWidth: true);

        builder.ResetProperties()
            .SetAnchorVertical(1f)
            .SetOffsetHorizontal(10f)
            .SetOffsetVertical(-50f)
            .SetSizeHorizontal(160f)
            .SetSizeVertical(40f)
            .SetText("Reset Defaults");
        _resetButton = builder.BuildButton("Reset all keybinds to defaults", ESleekFontSize.Medium);

        builder.SetAnchorHorizontal(1f)
            .SetOffsetHorizontal(-170f)
            .SetOffsetVertical(-50f)
            .SetText("Back");
        _closeButton = builder.BuildButton("Close keybinds menu", ESleekFontSize.Medium);

        Initialize();
    }

    public void Initialize()
    {
        if (_container == null) return;

        _container.AddChild(_openButton);
        EditorUI.window.AddChild(_root);
        _root.AddChild(_screenDimmer);
        _root.AddChild(_panel);

        _panel.AddChild(_backgroundDimmer);
        _panel.AddChild(_headerBox);
        _panel.AddChild(_hintLabel);
        _panel.AddChild(_scrollView);
        _panel.AddChild(_resetButton);
        _panel.AddChild(_closeButton);

        _root.AnimateOutOfView(1f, 0f);

        _openButton.OnClicked += OnOpenClicked;
        _closeButton.OnClicked += OnCloseClicked;
        _resetButton.OnClicked += OnResetClicked;

        KeybindManager.BindingChanged += OnBindingChanged;
        KeybindRebindManager.ActiveRebindChanged += OnActiveRebindChanged;
    }

    private void OnOpenClicked(ISleekElement button)
    {
        Open();
    }

    private void OnCloseClicked(ISleekElement button)
    {
        Close();
    }

    private void OnResetClicked(ISleekElement button)
    {
        KeybindManager.ResetAllToDefaults();
    }

    private void Open()
    {
        if (_active) return;
        _active = true;
        KeybindRebindManager.SetMenuOpen(true);
        RebuildList();
        HideOtherEditorUI();
        _container!.IsVisible = false;
        _root.AnimateIntoView();
    }

    private void Close()
    {
        if (!_active) return;
        _active = false;
        KeybindRebindManager.SetMenuOpen(false);
        KeybindRebindManager.CancelRebind();
        _root.AnimateOutOfView(1f, 0f);
        _container!.IsVisible = true;
        RestoreOtherEditorUI();
    }

    private void HideOtherEditorUI()
    {
        _hiddenElements.Clear();
        HideElement(_dashboardContainer);
        HideElement(_terrainContainer);
        HideElement(_environmentContainer);
        HideElement(_spawnsContainer);
        HideElement(_levelContainer);
        HideElement(_levelVisibilityContainer);
        HideElement(_levelPlayersContainer);
        HideElement(_environmentLightingContainer);
        HideElement(_environmentRoadsContainer);
        HideElement(_environmentNavigationContainer);
        HideElement(_spawnsAnimalsContainer);
        HideElement(_spawnsItemsContainer);
        HideElement(_spawnsZombiesContainer);
        HideElement(_spawnsVehiclesContainer);

        HideUIAccessorElement("EditorTerrainHeightUI");
        HideUIAccessorElement("EditorTerrainMaterialsUI");
        HideUIAccessorElement("EditorTerrainDetailsUI");
        HideUIAccessorElement("EditorTerrainTilesUI");
        HideUIAccessorElement("EditorEnvironmentNodesUI");
        HideUIAccessorElement("EditorLevelObjectsUI");
        HideUIAccessorElement("EditorVolumesUI");
    }

    private void HideElement(ISleekElement? element)
    {
        if (element == null) return;
        if (ReferenceEquals(element, _root)) return;
        if (_hiddenElements.ContainsKey(element)) return;
        _hiddenElements[element] = element.IsVisible;
        element.IsVisible = false;
    }

    private void HideUIAccessorElement(string propertyName)
    {
        PropertyInfo? property = typeof(UIAccessor).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
        if (property == null) return;
        if (property.GetValue(null) is ISleekElement element)
        {
            HideElement(element);
        }
    }

    private void RestoreOtherEditorUI()
    {
        foreach (KeyValuePair<ISleekElement, bool> pair in _hiddenElements)
        {
            pair.Key.IsVisible = pair.Value;
        }
        _hiddenElements.Clear();
    }

    private void RebuildList()
    {
        _scrollView.RemoveAllChildren();
        _bindingButtons.Clear();

        float offsetY = 0f;
        const float headerHeight = 26f;
        const float rowHeight = 30f;

        IEnumerable<IGrouping<string, KeybindAction>> grouped = KeybindManager.Actions
            .OrderBy(action => action.Category)
            .ThenBy(action => action.DisplayName)
            .GroupBy(action => action.Category);

        foreach (IGrouping<string, KeybindAction> group in grouped)
        {
            AddCategoryHeader(group.Key, offsetY);
            offsetY += headerHeight;

            foreach (KeybindAction action in group)
            {
                AddActionRow(action, offsetY, rowHeight);
                offsetY += rowHeight;
            }

            offsetY += 6f;
        }

        _scrollView.ContentSizeOffset = new Vector2(0f, offsetY);
    }

    private void AddCategoryHeader(string category, float offsetY)
    {
        UIBuilder builder = new(0f, 26f);
        builder.SetOffsetVertical(offsetY)
            .SetScaleHorizontal(1f)
            .SetText(category);

        ISleekBox header = builder.BuildBox(TextAnchor.MiddleLeft);
        _scrollView.AddChild(header);
    }

    private void AddActionRow(KeybindAction action, float offsetY, float height)
    {
        UIBuilder builder = new(0f, height);

        builder.SetOffsetHorizontal(10f)
            .SetOffsetVertical(offsetY)
            .SetSizeHorizontal(-180f)
            .SetSizeVertical(height)
            .SetScaleHorizontal(1f)
            .SetText(action.DisplayName);
        ISleekLabel label = builder.BuildLabel(TextAnchor.MiddleLeft);
        //label.too = action.Description;
        _scrollView.AddChild(label);

        builder.ResetProperties()
            .SetAnchorHorizontal(1f)
            .SetOffsetHorizontal(-160f)
            .SetOffsetVertical(offsetY)
            .SetSizeHorizontal(150f)
            .SetSizeVertical(height)
            .SetText(KeybindManager.GetDisplayText(action.Id));
        ISleekButton button = builder.BuildButton("Click to rebind");
        button.OnClicked += _ => BeginRebind(action.Id);
        _scrollView.AddChild(button);

        _bindingButtons[action.Id] = button;
    }

    private void BeginRebind(string actionId)
    {
        KeybindRebindManager.BeginRebind(actionId);
    }

    private void OnBindingChanged(string id)
    {
        if (_bindingButtons.TryGetValue(id, out ISleekButton? button))
        {
            button.Text = KeybindManager.GetDisplayText(id);
        }
    }

    private void OnActiveRebindChanged(string? id)
    {
        foreach (KeyValuePair<string, ISleekButton> pair in _bindingButtons)
        {
            if (pair.Key.Equals(id, StringComparison.OrdinalIgnoreCase))
            {
                pair.Value.Text = "Press a key...";
            }
            else
            {
                pair.Value.Text = KeybindManager.GetDisplayText(pair.Key);
            }
        }
    }

    public void Dispose()
    {
        if (_container == null) return;

        if (_active)
        {
            _active = false;
            RestoreOtherEditorUI();
            _container.IsVisible = true;
        }

        KeybindRebindManager.SetMenuOpen(false);
        KeybindRebindManager.CancelRebind();

        _openButton.OnClicked -= OnOpenClicked;
        _closeButton.OnClicked -= OnCloseClicked;
        _resetButton.OnClicked -= OnResetClicked;

        KeybindManager.BindingChanged -= OnBindingChanged;
        KeybindRebindManager.ActiveRebindChanged -= OnActiveRebindChanged;

        _container.RemoveChild(_openButton);
        EditorUI.window.RemoveChild(_root);
    }

    public static bool IsOpen => _instance?._active ?? false;

    public static void CloseIfOpen()
    {
        _instance?.Close();
    }
}
