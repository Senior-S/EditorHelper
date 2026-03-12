using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Extensions;
using EditorHelper2.common.Keybinds;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using UnityEngine;
using Action = System.Action;

namespace EditorHelper2.Extensions.Editor.Dashboard;

[UIExtension(typeof(EditorDashboardUI))]
[EHExtension("Editor Tabs Hotkeys Extension", "Gamingtoday093")]
public class DashboardHotkeysExtension : UIExtension, IExtension
{
    private const int EDITOR_TAB_CATEGORIES = 4;
    private enum EEditorTabCategory
    {
        None = -1,
        Terrain = 0,
        Environment = 1,
        Spawns = 2,
        Level = 3
    }

    [ExistingMember("terrainButton")]
    private readonly SleekButtonIcon? _terrainButton;
    [ExistingMember("container", OwningType = typeof(EditorTerrainUI))]
    private readonly SleekFullscreenBox? _terrainContainer;
    [ExistingMember("environmentButton")]
    private readonly SleekButtonIcon? _environmentButton;
    [ExistingMember("container", OwningType = typeof(EditorEnvironmentUI))]
    private readonly SleekFullscreenBox? _environmentContainer;
    [ExistingMember("spawnsButton")]
    private readonly SleekButtonIcon? _spawnsButton;
    [ExistingMember("container", OwningType = typeof(EditorSpawnsUI))]
    private readonly SleekFullscreenBox? _spawnsContainer;
    [ExistingMember("levelButton")]
    private readonly SleekButtonIcon? _levelButton;
    [ExistingMember("container", OwningType = typeof(EditorLevelUI))]
    private readonly SleekFullscreenBox? _levelContainer;

    private readonly Dictionary<ISleekElement, string> _oldButtonText = new();

    [ExistingMember("terrainMenu")]
    private readonly EditorTerrainUI? _terrainUI;
    [ExistingMember("levelUI")]
    private readonly EditorLevelUI? _levelUI;

    /// <summary>
    /// Do not index directly into this Array use <see cref="GoToEditorTab(EEditorTabCategory, int)"/> instead
    /// </summary>
    private readonly Action[][] _goToTabActions;

    public DashboardHotkeysExtension()
    {
        _goToTabActions = new Action[EDITOR_TAB_CATEGORIES][];
        
        _goToTabActions[(int)EEditorTabCategory.Terrain] =
        [
            _terrainUI!.GoToHeightsTab,
            _terrainUI!.GoToMaterialsTab,
            _terrainUI!.GoToFoliageTab,
            _terrainUI!.GoToTilesTab
        ];

        _goToTabActions[(int)EEditorTabCategory.Environment] =
        [
            () => EditorEnvironmentUI.onClickedLightingButton(null),
            () => EditorEnvironmentUI.onClickedRoadsButton(null),
            () => EditorEnvironmentUI.onClickedNavigationButton(null),
            () => EditorEnvironmentUI.onClickedNodesButton(null)
        ];

        _goToTabActions[(int)EEditorTabCategory.Spawns] =
        [
            () => EditorSpawnsUI.onClickedAnimalsButton(null),
            () => EditorSpawnsUI.onClickItemsButton(null),
            () => EditorSpawnsUI.onClickedZombiesButton(null),
            () => EditorSpawnsUI.onClickedVehiclesButton(null)
        ];

        _goToTabActions[(int)EEditorTabCategory.Level] =
        [
            () => _levelUI!.onClickedObjectsButton(null),
            () => _levelUI!.onClickedVisibilityButton(null),
            () => _levelUI!.onClickedPlayersButton(null),
            () => _levelUI!.OnClickedVolumesButton(null)
        ];

        Initialize();
    }

    public void Initialize()
    {
        _oldButtonText.Clear();

        _terrainButton?.text = FormatTabButton(_terrainButton, _terrainButton.text, KeybindIds.TabTerrain);
        _environmentButton?.text = FormatTabButton(_environmentButton, _environmentButton.text, KeybindIds.TabEnvironment);
        _spawnsButton?.text = FormatTabButton(_spawnsButton, _spawnsButton.text, KeybindIds.TabSpawns);
        _levelButton?.text = FormatTabButton(_levelButton, _levelButton.text, KeybindIds.TabLevel);

        if (_terrainContainer != null) FormatTabContainer(_terrainContainer);
        if (_environmentContainer != null) FormatTabContainer(_environmentContainer);
        if (_spawnsContainer != null) FormatTabContainer(_spawnsContainer);
        if (_levelContainer != null) FormatTabContainer(_levelContainer);

        KeybindManager.BindingChanged += OnKeybindChanged;
    }

    private void OnKeybindChanged(string keybindId)
    {
        switch (keybindId)
        {
            case KeybindIds.TabTerrain:
                _terrainButton?.text = FormatTabButton(_terrainButton, _terrainButton.text, keybindId);
                break;
            case KeybindIds.TabEnvironment:
                _environmentButton?.text = FormatTabButton(_environmentButton, _environmentButton.text, keybindId);
                break;
            case KeybindIds.TabSpawns:
                _spawnsButton?.text = FormatTabButton(_spawnsButton, _spawnsButton.text, keybindId);
                break;
            case KeybindIds.TabLevel:
                _levelButton?.text = FormatTabButton(_levelButton, _levelButton.text, keybindId);
                break;

            case KeybindIds.TabIndex0:
                FormatTabButtonChildren(0);
                break;
            case KeybindIds.TabIndex1:
                FormatTabButtonChildren(1);
                break;
            case KeybindIds.TabIndex2:
                FormatTabButtonChildren(2);
                break;
            case KeybindIds.TabIndex3:
                FormatTabButtonChildren(3);
                break;
        }

        void FormatTabButtonChildren(int index)
        {
            if (_terrainContainer != null) FormatTabButtonChild(_terrainContainer.GetChildAtIndexEx(index), keybindId);
            if (_environmentContainer != null) FormatTabButtonChild(_environmentContainer.GetChildAtIndexEx(index), keybindId);
            if (_spawnsContainer != null) FormatTabButtonChild(_spawnsContainer.GetChildAtIndexEx(index), keybindId);
            if (_levelContainer != null) FormatTabButtonChild(_levelContainer.GetChildAtIndexEx(index), keybindId);
        }
    }

    private void FormatTabContainer(SleekFullscreenBox container)
    {
        FormatTabButtonChild(container.GetChildAtIndexEx(0), KeybindIds.TabIndex0);
        FormatTabButtonChild(container.GetChildAtIndexEx(1), KeybindIds.TabIndex1);
        FormatTabButtonChild(container.GetChildAtIndexEx(2), KeybindIds.TabIndex2);
        FormatTabButtonChild(container.GetChildAtIndexEx(3), KeybindIds.TabIndex3);
    }

    private void FormatTabButtonChild(ISleekElement child, string keybindId)
    {
        if (child is SleekButtonIcon buttonIcon) buttonIcon.text = FormatTabButton(buttonIcon, buttonIcon.text, keybindId);
        else if (child is ISleekButton button) button.Text = FormatTabButton(button, button.Text, keybindId);
    }

    private string FormatTabButton(ISleekElement element, string text, string keybindId)
    {
        if (!_oldButtonText.ContainsKey(element)) _oldButtonText.Add(element, text);

        text = RemoveKeybindText(text);

        var keybind = KeybindManager.GetAction(keybindId);
        if (keybind != null && !keybind.Current.IsNone)
            text += $" [{KeybindManager.FormatKeybind(keybind.Current)}]";

        return text;
    }

    private string RemoveKeybindText(string text)
    {
        int index = text.LastIndexOf('[');
        if (index == -1) return text;
        if (index == 0) return string.Empty;
        return text.Substring(0, char.IsWhiteSpace(text[index - 1]) ? index - 1 : index);
    }

    private EEditorTabCategory GetCurrentTabCategory()
    {
        if (EditorTerrainUI.active) return EEditorTabCategory.Terrain;
        if (EditorEnvironmentUI.active) return EEditorTabCategory.Environment;
        if (EditorSpawnsUI.active) return EEditorTabCategory.Spawns;
        if (EditorLevelUI.active) return EEditorTabCategory.Level;

        return EEditorTabCategory.None;
    }

    private void GoToEditorTab(EEditorTabCategory category)
    {
        switch (category)
        {
            case EEditorTabCategory.None:
                _terrainUI!.close();
                EditorEnvironmentUI.close();
                EditorSpawnsUI.close();
                EditorLevelUI.close();
                break;
            case EEditorTabCategory.Terrain:
                if (EditorTerrainUI.active) break;

                _terrainUI!.open();
                EditorEnvironmentUI.close();
                EditorSpawnsUI.close();
                EditorLevelUI.close();
                break;
            case EEditorTabCategory.Environment:
                if (EditorEnvironmentUI.active) break;

                _terrainUI!.close();
                EditorEnvironmentUI.open();
                EditorSpawnsUI.close();
                EditorLevelUI.close();
                break;
            case EEditorTabCategory.Spawns:
                if (EditorSpawnsUI.active) break;

                _terrainUI!.close();
                EditorEnvironmentUI.close();
                EditorSpawnsUI.open();
                EditorLevelUI.close();
                break;
            case EEditorTabCategory.Level:
                if (EditorLevelUI.active) break;

                _terrainUI!.close();
                EditorEnvironmentUI.close();
                EditorSpawnsUI.close();
                EditorLevelUI.open();
                break;
            default:
                CommandWindow.LogError($"[EditorHelper2] Unsupported EEditorTabCategory {category}");
                break;
        }
    }

    private void GoToEditorTab(EEditorTabCategory category, int index)
    {
        GoToEditorTab(category);

        if (category == EEditorTabCategory.None) return;
        _goToTabActions[(int)category][index].Invoke();
    }

    public void CustomUpdate()
    {
        if (KeybindManager.IsDown(KeybindIds.TabTerrain)) GoToEditorTab(EEditorTabCategory.Terrain);
        else if (KeybindManager.IsDown(KeybindIds.TabEnvironment)) GoToEditorTab(EEditorTabCategory.Environment);
        else if (KeybindManager.IsDown(KeybindIds.TabSpawns)) GoToEditorTab(EEditorTabCategory.Spawns);
        else if (KeybindManager.IsDown(KeybindIds.TabLevel)) GoToEditorTab(EEditorTabCategory.Level);
        
        else if (KeybindManager.IsDown(KeybindIds.TabIndex0)) GoToEditorTab(GetCurrentTabCategory(), 0);
        else if (KeybindManager.IsDown(KeybindIds.TabIndex1)) GoToEditorTab(GetCurrentTabCategory(), 1);
        else if (KeybindManager.IsDown(KeybindIds.TabIndex2)) GoToEditorTab(GetCurrentTabCategory(), 2);
        else if (KeybindManager.IsDown(KeybindIds.TabIndex3)) GoToEditorTab(GetCurrentTabCategory(), 3);
    }

    public void Dispose()
    {
        KeybindManager.BindingChanged -= OnKeybindChanged;

        foreach (var btn in _oldButtonText)
        {
            if (btn.Key is SleekButtonIcon buttonIcon) buttonIcon.text = btn.Value;
            else if (btn.Key is ISleekButton button) button.Text = btn.Value;
        }

        _oldButtonText.Clear();
    }
}
