using System;
using System.Collections.Generic;
using System.Linq;
using DanielWillett.UITools.API.Extensions;
using DanielWillett.UITools.API.Extensions.Members;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Keybinds;
using EditorHelper2.Extensions.Editor.Pause;
using EditorHelper2.Extensions.Level.Objects;
using EditorHelper2.Extensions.Terrain.Foliage;
using EditorHelper2.Loader;
using EditorHelper2.UI.Builders;
using SDG.Framework.Devkit.Transactions;
using SDG.Unturned;
using UnityEngine;
using Action = System.Action;

namespace EditorHelper2.Extensions.Editor.Dashboard;

/// <summary>
/// Provides a searchable, keyboard-driven launcher for common editor actions.
/// </summary>
[UIExtension(typeof(EditorDashboardUI))]
[EHExtension("Command Palette", "Senior S", alwaysEnabled: true)]
public sealed class CommandPaletteExtension : UIExtension, IExtension
{
    private const int VisibleRowCount = 7;

    private static CommandPaletteExtension? _instance;
    private static bool _handledModalInputThisFrame;

    [ExistingMember("terrainMenu")]
    private readonly EditorTerrainUI? _terrainUI;

    [ExistingMember("levelUI")]
    private readonly EditorLevelUI? _levelUI;

    [ExistingMember("container")]
    private readonly SleekFullscreenBox? _dashboardContainer;

    private readonly SleekFullscreenBox _root;
    private readonly ISleekField _searchField;
    private readonly ISleekLabel _resultCountLabel;
    private readonly ISleekLabel _emptyStateLabel;
    private readonly ISleekLabel _footerLabel;
    private readonly ISleekButton[] _rows = new ISleekButton[VisibleRowCount];
    private readonly List<PaletteAction> _actions = [];
    private readonly List<PaletteAction> _filteredActions = [];

    private bool _isOpen;
    private bool _initialized;
    private int _selectedIndex;
    private int _firstVisibleIndex;

    /// <summary>
    /// Gets whether the command palette is currently visible.
    /// </summary>
    public static bool IsOpen => _instance?._isOpen == true;

    /// <summary>
    /// Creates the command palette UI and action catalog.
    /// </summary>
    public CommandPaletteExtension()
    {
        UIBuilder builder = new(0f, 0f);
        builder.SetScaleHorizontal(1f).SetScaleVertical(1f);
        _root = builder.BuildFullscreenBox();
        _root.IsVisible = false;

        builder.ResetProperties()
            .SetScaleHorizontal(1f)
            .SetScaleVertical(1f);
        ISleekBox backdrop = builder.BuildColoredBox(new SleekColor(ESleekTint.BACKGROUND, 0.75f));
        _root.AddChild(backdrop);

        builder.ResetProperties()
            .SetAnchorHorizontal(0.5f)
            .SetAnchorVertical(0.16f)
            .SetOffsetHorizontal(-340f)
            .SetSizeHorizontal(680f)
            .SetSizeVertical(430f);
        ISleekBox panel = builder.BuildColoredBox(new SleekColor(ESleekTint.BACKGROUND, 0.98f));
        _root.AddChild(panel);

        builder.ResetProperties()
            .SetOffsetHorizontal(20f)
            .SetOffsetVertical(14f)
            .SetSizeHorizontal(300f)
            .SetSizeVertical(30f)
            .SetText("COMMAND PALETTE");
        ISleekLabel title = builder.BuildLabel(TextAnchor.MiddleLeft, ESleekFontSize.Medium);
        panel.AddChild(title);

        builder.ResetProperties()
            .SetOffsetHorizontal(618f)
            .SetOffsetVertical(14f)
            .SetSizeHorizontal(42f)
            .SetSizeVertical(30f)
            .SetText("Close");
        ISleekButton closeButton = builder.BuildButton();
        closeButton.OnClicked += _ => Close();
        panel.AddChild(closeButton);

        builder.ResetProperties()
            .SetOffsetHorizontal(430f)
            .SetOffsetVertical(14f)
            .SetSizeHorizontal(180f)
            .SetSizeVertical(30f);
        _resultCountLabel = builder.BuildLabel(TextAnchor.MiddleRight);
        panel.AddChild(_resultCountLabel);

        builder.ResetProperties()
            .SetOffsetHorizontal(20f)
            .SetOffsetVertical(52f)
            .SetSizeHorizontal(640f)
            .SetSizeVertical(42f)
            .SetText("Search actions, tabs, and tools...");
        _searchField = builder.BuildStringField();
        panel.AddChild(_searchField);

        builder.ResetProperties()
            .SetOffsetHorizontal(20f)
            .SetOffsetVertical(188f)
            .SetSizeHorizontal(640f)
            .SetSizeVertical(30f)
            .SetText("No matching actions");
        _emptyStateLabel = builder.BuildLabel(TextAnchor.MiddleCenter);
        panel.AddChild(_emptyStateLabel);

        for (int rowIndex = 0; rowIndex < _rows.Length; rowIndex++)
        {
            builder.ResetProperties()
                .SetOffsetHorizontal(20f)
                .SetOffsetVertical(106f + rowIndex * 40f)
                .SetSizeHorizontal(640f)
                .SetSizeVertical(34f);

            ISleekButton row = builder.BuildButton();
            int capturedRowIndex = rowIndex;
            row.OnClicked += _ => ExecuteVisibleRow(capturedRowIndex);
            panel.AddChild(row);
            _rows[rowIndex] = row;
        }

        builder.ResetProperties()
            .SetOffsetHorizontal(20f)
            .SetOffsetVertical(392f)
            .SetSizeHorizontal(640f)
            .SetSizeVertical(24f)
            .SetText(string.Empty);
        _footerLabel = builder.BuildLabel(TextAnchor.MiddleLeft);
        panel.AddChild(_footerLabel);

        BuildActionCatalog();
        FilterActions(string.Empty);
        Initialize();
    }

    /// <summary>
    /// Attaches the palette to the editor and subscribes to search changes.
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        _instance = this;
        _searchField.OnTextChanged += OnSearchTextChanged;
        KeybindManager.BindingChanged += OnBindingChanged;
        EditorUI.window.AddChild(_root);
        RefreshKeybindText();
    }

    /// <summary>
    /// Handles the palette keybind and keyboard navigation once per editor update.
    /// </summary>
    public void CustomUpdate()
    {
        if (_isOpen || LSystemRoadsExtension.IsOpen || CinematicModeExtension.IsOpen || KeybindsMenuExtension.IsOpen)
        {
            return;
        }

        if (Glazier.Get().ShouldGameProcessInput && KeybindManager.IsDown(KeybindIds.CommandPalette))
        {
            Open();
        }
    }

    /// <summary>
    /// Handles input while the palette owns the editor UI update.
    /// </summary>
    public static void HandleModalInput()
    {
        _handledModalInputThisFrame = true;
        _instance?.HandleInput();
    }

    /// <summary>
    /// Returns whether modal input was routed during this editor update.
    /// </summary>
    public static bool ConsumeModalInputHandledThisFrame()
    {
        bool handled = _handledModalInputThisFrame;
        _handledModalInputThisFrame = false;
        return handled;
    }

    /// <summary>
    /// Closes the active palette, if one exists.
    /// </summary>
    public static void CloseIfOpen()
    {
        _instance?.Close();
    }

    private void BuildActionCatalog()
    {
        AddAction("Save Level", "Editor", "write persist", SDG.Unturned.Level.save);
        AddAction("Undo", "Editor", "transaction history", () => { DevkitTransactionManager.undo(); });
        AddAction("Redo", "Editor", "transaction history", () => { DevkitTransactionManager.redo(); });

        AddAction("Terrain: Heights", "Terrain", "height sculpt", () => OpenTerrain(() => _terrainUI!.GoToHeightsTab()));
        AddAction("Terrain: Materials", "Terrain", "paint splatmap", () => OpenTerrain(() => _terrainUI!.GoToMaterialsTab()));
        AddAction("Terrain: Foliage", "Terrain", "details resources", () => OpenTerrain(() => _terrainUI!.GoToFoliageTab()));
        AddAction("Foliage: Grid View", "Terrain", "foliage details exact icons", OpenExactFoliageGrid, () => ExtensionManager.IsEnabled<FoliageIconsExtension>());
        AddAction("Terrain: Tiles", "Terrain", "landscape layers", () => OpenTerrain(() => _terrainUI!.GoToTilesTab()));

        AddAction("Environment: Lighting", "Environment", "ambience sky weather", () => OpenEnvironment(() => EditorEnvironmentUI.onClickedLightingButton(null)));
        AddAction("Environment: Roads", "Environment", "street path", () => OpenEnvironment(() => EditorEnvironmentUI.onClickedRoadsButton(null)));
        AddAction("Environment: Navigation", "Environment", "zombie navmesh", () => OpenEnvironment(() => EditorEnvironmentUI.onClickedNavigationButton(null)));
        AddAction("Environment: Nodes", "Environment", "location effect", () => OpenEnvironment(() => EditorEnvironmentUI.onClickedNodesButton(null)));

        AddAction("Spawns: Animals", "Spawns", "fauna", () => OpenSpawns(() => EditorSpawnsUI.onClickedAnimalsButton(null)));
        AddAction("Spawns: Items", "Spawns", "loot jars", () => OpenSpawns(() => EditorSpawnsUI.onClickItemsButton(null)));
        AddAction("Spawns: Zombies", "Spawns", "infected", () => OpenSpawns(() => EditorSpawnsUI.onClickedZombiesButton(null)));
        AddAction("Spawns: Vehicles", "Spawns", "cars", () => OpenSpawns(() => EditorSpawnsUI.onClickedVehiclesButton(null)));

        AddAction("Level: Objects", "Level", "assets placement", () => OpenLevel(() => _levelUI?.onClickedObjectsButton(null)));
        AddAction("Objects: Grid View", "Level", "objects assets icons", OpenObjectGrid, () => ExtensionManager.IsEnabled<IconsExtension>());
        AddAction("Objects: Schematics", "Level", "objects templates blueprints", OpenObjectSchematics, () => ExtensionManager.IsEnabled<SchematicsExtension>());
        AddAction("Level: Visibility", "Level", "regions performance", () => OpenLevel(() => _levelUI?.onClickedVisibilityButton(null)));
        AddAction("Level: Players", "Level", "barricades structures", () => OpenLevel(() => _levelUI?.onClickedPlayersButton(null)));
        AddAction("Level: Volumes", "Level", "deadzone clip water", () => OpenLevel(() => _levelUI?.OnClickedVolumesButton(null)));
    }

    private void AddAction(string title, string category, string keywords, Action execute, Func<bool>? isAvailable = null)
    {
        _actions.Add(new PaletteAction(title, category, keywords, execute, isAvailable, _actions.Count));
    }

    private void OpenTerrain(Action? openSubtab)
    {
        if (_terrainUI == null || openSubtab == null) return;
        _terrainUI.open();
        EditorEnvironmentUI.close();
        EditorSpawnsUI.close();
        EditorLevelUI.close();
        openSubtab();
    }

    private void OpenEnvironment(Action openSubtab)
    {
        _terrainUI?.close();
        EditorEnvironmentUI.open();
        EditorSpawnsUI.close();
        EditorLevelUI.close();
        openSubtab();
    }

    private void OpenSpawns(Action openSubtab)
    {
        _terrainUI?.close();
        EditorEnvironmentUI.close();
        EditorSpawnsUI.open();
        EditorLevelUI.close();
        openSubtab();
    }

    private void OpenLevel(Action openSubtab)
    {
        _terrainUI?.close();
        EditorEnvironmentUI.close();
        EditorSpawnsUI.close();
        EditorLevelUI.open();
        openSubtab();
    }

    private void OpenObjectGrid()
    {
        if (ExtensionManager.TryGetInstance(out IconsExtension? iconsExtension))
        {
            OpenLevel(() => _levelUI?.onClickedObjectsButton(null));
            iconsExtension.ShowIconGridContainer();
            return;
        }

        CommandWindow.LogError("[Command Palette] Objects: Grid View is unavailable because Icons Extension has no active instance.");
    }

    private void OpenObjectSchematics()
    {
        if (ExtensionManager.TryGetInstance(out SchematicsExtension? schematicsExtension))
        {
            OpenLevel(() => _levelUI?.onClickedObjectsButton(null));
            schematicsExtension.ShowSchematicsContainer();
            return;
        }

        CommandWindow.LogError("[Command Palette] Objects: Schematics is unavailable because Schematics Extension has no active instance.");
    }

    private void OpenExactFoliageGrid()
    {
        if (ExtensionManager.TryGetInstance(out FoliageIconsExtension? foliageIconsExtension))
        {
            OpenTerrain(() => _terrainUI!.GoToFoliageTab());
            foliageIconsExtension.ShowExactIconGrid();
            return;
        }

        CommandWindow.LogError("[Command Palette] Foliage: Grid View is unavailable because Foliage Icons Extension has no active instance.");
    }

    private void Open()
    {
        if (_isOpen || LSystemRoadsExtension.IsOpen || CinematicModeExtension.IsOpen || KeybindsMenuExtension.IsOpen) return;
        _isOpen = true;
        _root.IsVisible = true;
        _searchField.Text = string.Empty;
        FilterActions(string.Empty);
        _searchField.FocusControl();
    }

    private void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        _searchField.ClearFocus();
        _root.IsVisible = false;
    }

    private void OnSearchTextChanged(ISleekField field, string text)
    {
        FilterActions(text);
    }

    private void FilterActions(string query)
    {
        _filteredActions.Clear();
        string trimmedQuery = query.Trim();

        _filteredActions.AddRange(_actions
            .Where(action => action.IsAvailable && (trimmedQuery.Length == 0 || action.Matches(trimmedQuery)))
            .OrderBy(action => action.MatchRank(trimmedQuery))
            .ThenBy(action => action.CatalogIndex));

        _selectedIndex = 0;
        _firstVisibleIndex = 0;
        RefreshRows();
    }

    private void MoveSelection(int direction)
    {
        if (_filteredActions.Count == 0) return;

        _selectedIndex = (_selectedIndex + direction + _filteredActions.Count) % _filteredActions.Count;
        if (_selectedIndex < _firstVisibleIndex)
        {
            _firstVisibleIndex = _selectedIndex;
        }
        else if (_selectedIndex >= _firstVisibleIndex + VisibleRowCount)
        {
            _firstVisibleIndex = _selectedIndex - VisibleRowCount + 1;
        }

        RefreshRows();
    }

    private void RefreshRows()
    {
        _emptyStateLabel.IsVisible = _filteredActions.Count == 0;
        int visibleEnd = Math.Min(_firstVisibleIndex + VisibleRowCount, _filteredActions.Count);
        _resultCountLabel.Text = _filteredActions.Count > VisibleRowCount
            ? $"{_firstVisibleIndex + 1}-{visibleEnd} of {_filteredActions.Count} actions"
            : $"{_filteredActions.Count} action{(_filteredActions.Count == 1 ? string.Empty : "s")}";

        for (int rowIndex = 0; rowIndex < _rows.Length; rowIndex++)
        {
            ISleekButton row = _rows[rowIndex];
            int actionIndex = _firstVisibleIndex + rowIndex;
            bool hasAction = actionIndex < _filteredActions.Count;
            row.IsVisible = hasAction;
            if (!hasAction) continue;

            PaletteAction action = _filteredActions[actionIndex];
            bool selected = actionIndex == _selectedIndex;
            row.Text = $"{(selected ? "> " : string.Empty)}[{action.Category.ToUpperInvariant()}]  {action.Title}";
            row.TooltipText = action.Keywords;
            row.BackgroundColor = new SleekColor(ESleekTint.BACKGROUND, selected ? 1f : 0.45f);
        }
    }

    private void ExecuteVisibleRow(int rowIndex)
    {
        int actionIndex = _firstVisibleIndex + rowIndex;
        if (actionIndex >= _filteredActions.Count) return;
        Execute(_filteredActions[actionIndex]);
    }

    private void ExecuteSelected()
    {
        if (_selectedIndex >= _filteredActions.Count) return;
        Execute(_filteredActions[_selectedIndex]);
    }

    private void Execute(PaletteAction action)
    {
        Close();
        try
        {
            action.Execute();
        }
        catch (Exception ex)
        {
            CommandWindow.LogError($"[Command Palette] Failed to execute '{action.Title}': {ex}");
        }
    }

    /// <summary>
    /// Detaches the palette UI and input handlers.
    /// </summary>
    public void Dispose()
    {
        if (!_initialized) return;
        _initialized = false;
        Close();
        _searchField.OnTextChanged -= OnSearchTextChanged;
        KeybindManager.BindingChanged -= OnBindingChanged;
        EditorUI.window.RemoveChild(_root);
        if (ReferenceEquals(_instance, this)) _instance = null;
    }

    private void HandleInput()
    {
        if (!_isOpen) return;

        if (KeybindManager.IsDownIgnoringTextFieldFocus(KeybindIds.CommandPalette) || Input.GetKeyDown(KeyCode.Escape)) Close();
        else if (Input.GetKeyDown(KeyCode.DownArrow)) MoveSelection(1);
        else if (Input.GetKeyDown(KeyCode.UpArrow)) MoveSelection(-1);
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) ExecuteSelected();
        else if (Input.mouseScrollDelta.y > 0f) MoveSelection(-1);
        else if (Input.mouseScrollDelta.y < 0f) MoveSelection(1);
    }

    private void OnBindingChanged(string id)
    {
        if (id == KeybindIds.CommandPalette) RefreshKeybindText();
    }

    private void RefreshKeybindText()
    {
        string binding = KeybindManager.GetDisplayText(KeybindIds.CommandPalette);
        _footerLabel.Text = $"{binding} · Up/Down select · Enter run · Esc close · Wheel scroll";
    }

    private sealed class PaletteAction(string title, string category, string keywords, Action execute, Func<bool>? isAvailable, int catalogIndex)
    {
        public string Title { get; } = title;
        public string Category { get; } = category;
        public string Keywords { get; } = keywords;
        public bool IsAvailable => isAvailable?.Invoke() ?? true;
        public int CatalogIndex { get; } = catalogIndex;
        public Action Execute { get; } = execute;

        public bool Matches(string query)
        {
            return Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                   || Category.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                   || Keywords.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public int MatchRank(string query)
        {
            if (query.Length == 0) return 0;
            if (Title.Equals(query, StringComparison.OrdinalIgnoreCase)) return 0;
            if (Title.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 1;
            if (Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) return 2;
            if (Keywords.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) return 3;
            return 4;
        }
    }
}
