using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using DanielWillett.UITools.Util;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Extensions;
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
            .SetScaleVertical(1f)
            .SetSizeHorizontal(40f)
            .SetSizeVertical(40f)
            .SetOffsetHorizontal(-20f)
            .SetOffsetVertical(-20f);
        _screenDimmer = builder.BuildColoredBox(new SleekColor(ESleekTint.BACKGROUND, 0.5f));

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
        _backgroundDimmer = builder.BuildColoredBox(new SleekColor(ESleekTint.BACKGROUND, 0.8f));

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
        _root.AnimateIntoView();
    }

    private void Close()
    {
        if (!_active) return;
        _active = false;
        KeybindRebindManager.SetMenuOpen(false);
        KeybindRebindManager.CancelRebind();
        _root.AnimateOutOfView(1f, 0f);
        RestoreOtherEditorUI();
    }

    private void HideOtherEditorUI()
    {
        _hiddenElements.Clear();

        for (int i = 0; i < EditorUI.window.GetChildCount(); i++)
            HideElement(EditorUI.window.GetChildAtIndexEx(i));
    }

    private void HideElement(ISleekElement? element)
    {
        if (element == null) return;
        if (ReferenceEquals(element, _root)) return;
        if (_hiddenElements.ContainsKey(element)) return;
        _hiddenElements[element] = element.IsVisible;
        element.IsVisible = false;
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
            .OrderBy(action => action.OrderId)
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
            .SetScaleHorizontal(1f);
        
        ISleekBox header = builder.BuildBox();

        builder.SetOffsetVertical(0f)
            .SetOffsetHorizontal(4f)
            .SetText(category);

        ISleekLabel label = builder.BuildLabel(TextAnchor.MiddleLeft);
        header.AddChild(label);

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
