using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.common.Keybinds;

public static class KeybindManager
{
    private static readonly Dictionary<string, KeybindAction> ActionsInternal = new(StringComparer.OrdinalIgnoreCase);
    private static readonly string KeybindsFile = Path.Combine(Globals.ExtensionsFolder, "keybinds.json");
    private static bool _initialized;

    private static int _nextInstanceId = 0;

    public static event Action<string>? BindingChanged;

    public static IReadOnlyCollection<KeybindAction> Actions => ActionsInternal.Values;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        RegisterDefaults();
        LoadBindings();
    }

    public static KeybindAction? GetAction(string id)
    {
        return ActionsInternal.GetValueOrDefault(id);
    }

    public static string GetDisplayText(string id)
    {
        KeybindAction? action = GetAction(id);
        if (action == null) return "Unassigned";
        return FormatKeybind(action.Current);
    }

    public static bool IsDown(string id)
    {
        if (KeybindRebindManager.ShouldIgnoreInput) return false;
        if (!ActionsInternal.TryGetValue(id, out KeybindAction? action)) return false;
        return IsDown(action.Current);
    }

    public static bool IsHeld(string id)
    {
        if (KeybindRebindManager.ShouldIgnoreInput) return false;
        if (!ActionsInternal.TryGetValue(id, out KeybindAction? action)) return false;
        return IsHeld(action.Current);
    }

    public static bool IsUp(string id)
    {
        if (KeybindRebindManager.ShouldIgnoreInput) return false;
        if (!ActionsInternal.TryGetValue(id, out KeybindAction? action)) return false;
        return IsUp(action.Current);
    }

    public static void SetBinding(string id, Keybind binding)
    {
        if (!ActionsInternal.TryGetValue(id, out KeybindAction? action)) return;
        action.Current = binding;
        SaveBindings();
        BindingChanged?.Invoke(id);
    }

    public static void ResetAllToDefaults()
    {
        foreach (KeybindAction action in ActionsInternal.Values)
        {
            action.Current = action.Default;
            BindingChanged?.Invoke(action.Id);
        }
        SaveBindings();
    }

    // Not work using Stringbuilder as it will probably end up allocating more memory
    // and the performance gain won't be noticeable as it isn't a high called function 
    public static string FormatKeybind(Keybind binding)
    {
        if (binding.IsNone) return "Unassigned";

        List<string> parts = new(binding.Keys.Count);
        foreach (KeyCode key in binding.Keys)
        {
            parts.Add(GetKeyDisplayText(key));
        }

        return parts.Count == 0 ? "Unassigned" : string.Join("+", parts);
    }

    public static bool IsDown(Keybind binding)
    {
        if (binding.IsNone) return false;
        KeyCode primary = binding.PrimaryKey;
        if (primary == KeyCode.None) return false;
        return IsKeyDown(primary) && AreModifiersExact(binding) && AreOtherKeysHeld(binding, primary);
    }

    /// <summary>
    /// Tests a binding with raw Unity input for a UI which already owns input.
    /// This deliberately ignores text-field focus; callers must not use it to open a modal UI.
    /// </summary>
    public static bool IsDownIgnoringTextFieldFocus(string id)
    {
        if (KeybindRebindManager.ShouldIgnoreInput) return false;
        if (!ActionsInternal.TryGetValue(id, out KeybindAction? action)) return false;
        Keybind binding = action.Current;
        if (binding.IsNone || binding.PrimaryKey == KeyCode.None) return false;

        return IsRawKeyDown(binding.PrimaryKey)
               && AreModifiersExactRaw(binding)
               && AreOtherNonModifierKeysHeldRaw(binding, binding.PrimaryKey);
    }

    public static bool IsHeld(Keybind binding)
    {
        if (binding.IsNone) return false;
        return AreModifiersExact(binding) && AreAllNonModifierKeysHeld(binding);
    }

    public static bool IsUp(Keybind binding)
    {
        if (binding.IsNone) return false;
        KeyCode primary = binding.PrimaryKey;
        if (primary == KeyCode.None) return false;
        return IsKeyUp(primary) && AreModifiersExact(binding) && AreOtherKeysHeld(binding, primary);
    }

    private static bool AreModifiersExact(Keybind binding)
    {
        bool ctrl = InputEx.GetKey(KeyCode.LeftControl) || InputEx.GetKey(KeyCode.RightControl);
        bool shift = InputEx.GetKey(KeyCode.LeftShift) || InputEx.GetKey(KeyCode.RightShift);
        bool alt = InputEx.GetKey(KeyCode.LeftAlt) || InputEx.GetKey(KeyCode.RightAlt);

        bool expectsCtrl = binding.Keys.Any(Keybind.IsCtrlKey);
        bool expectsShift = binding.Keys.Any(Keybind.IsShiftKey);
        bool expectsAlt = binding.Keys.Any(Keybind.IsAltKey);

        if (expectsCtrl != ctrl) return false;
        if (expectsShift != shift) return false;
        if (expectsAlt != alt) return false;
        return true;
    }

    private static bool AreModifiersExactRaw(Keybind binding)
    {
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        return binding.Keys.Any(Keybind.IsCtrlKey) == ctrl
               && binding.Keys.Any(Keybind.IsShiftKey) == shift
               && binding.Keys.Any(Keybind.IsAltKey) == alt;
    }

    private static bool AreAllNonModifierKeysHeld(Keybind binding)
    {
        foreach (KeyCode key in binding.Keys)
        {
            if (Keybind.IsModifierKey(key)) continue;
            if (!InputEx.GetKey(key)) return false;
        }
        return true;
    }

    private static bool AreOtherKeysHeld(Keybind binding, KeyCode primary)
    {
        foreach (KeyCode key in binding.Keys)
        {
            if (key == primary) continue;
            if (Keybind.IsModifierKey(key)) continue;
            if (!InputEx.GetKey(key)) return false;
        }
        return true;
    }

    private static bool AreOtherNonModifierKeysHeldRaw(Keybind binding, KeyCode primary)
    {
        foreach (KeyCode key in binding.Keys)
        {
            if (key == primary || Keybind.IsModifierKey(key)) continue;
            if (!Input.GetKey(key)) return false;
        }

        return true;
    }

    private static bool IsRawKeyDown(KeyCode key)
    {
        if (Keybind.IsCtrlKey(key)) return Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);
        if (Keybind.IsShiftKey(key)) return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
        if (Keybind.IsAltKey(key)) return Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt);
        return Input.GetKeyDown(key);
    }

    private static bool IsKeyDown(KeyCode key)
    {
        if (Keybind.IsCtrlKey(key))
        {
            return InputEx.GetKeyDown(KeyCode.LeftControl) || InputEx.GetKeyDown(KeyCode.RightControl);
        }

        if (Keybind.IsShiftKey(key))
        {
            return InputEx.GetKeyDown(KeyCode.LeftShift) || InputEx.GetKeyDown(KeyCode.RightShift);
        }

        if (Keybind.IsAltKey(key))
        {
            return InputEx.GetKeyDown(KeyCode.LeftAlt) || InputEx.GetKeyDown(KeyCode.RightAlt);
        }

        return InputEx.GetKeyDown(key);
    }

    private static bool IsKeyUp(KeyCode key)
    {
        if (Keybind.IsCtrlKey(key))
        {
            return InputEx.GetKeyUp(KeyCode.LeftControl) || InputEx.GetKeyUp(KeyCode.RightControl);
        }

        if (Keybind.IsShiftKey(key))
        {
            return InputEx.GetKeyUp(KeyCode.LeftShift) || InputEx.GetKeyUp(KeyCode.RightShift);
        }

        if (Keybind.IsAltKey(key))
        {
            return InputEx.GetKeyUp(KeyCode.LeftAlt) || InputEx.GetKeyUp(KeyCode.RightAlt);
        }

        return InputEx.GetKeyUp(key);
    }

    private static string GetKeyDisplayText(KeyCode key)
    {
        if (Keybind.IsCtrlKey(key)) return "Ctrl";
        if (Keybind.IsShiftKey(key)) return "Shift";
        if (Keybind.IsAltKey(key)) return "Alt";
        return MenuConfigurationControlsUI.getKeyCodeText(key);
    }

    private static void RegisterDefaults()
    {
        Register(new KeybindAction(KeybindIds.EditorSave, "Save Level", "Save the current level", "Editor",
            new Keybind(KeyCode.LeftControl, KeyCode.S)));
        Register(new KeybindAction(KeybindIds.EditorUndo, "Undo", "Undo last transaction", "Editor",
            new Keybind(KeyCode.LeftControl, KeyCode.Z)));
        Register(new KeybindAction(KeybindIds.EditorRedo, "Redo", "Redo last transaction", "Editor",
            new Keybind(KeyCode.LeftControl, KeyCode.LeftShift, KeyCode.Z)));
        Register(new KeybindAction(KeybindIds.CommandPalette, "Command Palette", "Search and run editor actions", "Editor",
            new Keybind(KeyCode.LeftControl, KeyCode.P)));

        Register(new KeybindAction(KeybindIds.TabTerrain, "Terrain Tab", "Go to the terrain tab", "Editor Tabs",
            new Keybind(KeyCode.LeftControl, KeyCode.Alpha1)));
        Register(new KeybindAction(KeybindIds.TabEnvironment, "Environment Tab", "Go to the environment tab", "Editor Tabs",
            new Keybind(KeyCode.LeftControl, KeyCode.Alpha2)));
        Register(new KeybindAction(KeybindIds.TabSpawns, "Spawns Tab", "Go to the spawns tab", "Editor Tabs",
            new Keybind(KeyCode.LeftControl, KeyCode.Alpha3)));
        Register(new KeybindAction(KeybindIds.TabLevel, "Level Tab", "Go to the level tab", "Editor Tabs",
            new Keybind(KeyCode.LeftControl, KeyCode.Alpha4)));
        Register(new KeybindAction(KeybindIds.TabIndex0, "Subtab 1", "Go to the first subtab", "Editor Tabs",
            new Keybind(KeyCode.Alpha1)));
        Register(new KeybindAction(KeybindIds.TabIndex1, "Subtab 2", "Go to the second subtab", "Editor Tabs",
            new Keybind(KeyCode.Alpha2)));
        Register(new KeybindAction(KeybindIds.TabIndex2, "Subtab 3", "Go to the third subtab", "Editor Tabs",
            new Keybind(KeyCode.Alpha3)));
        Register(new KeybindAction(KeybindIds.TabIndex3, "Subtab 4", "Go to the fourth subtab", "Editor Tabs",
            new Keybind(KeyCode.Alpha4)));

        Register(new KeybindAction(KeybindIds.VisibilityRoads, "Toggle Roads Visibility", "Show/hide roads", "Visibility",
            new Keybind(KeyCode.F1)));
        Register(new KeybindAction(KeybindIds.VisibilityNavigation, "Toggle Navigation Visibility", "Show/hide navigation", "Visibility",
            new Keybind(KeyCode.F2)));
        Register(new KeybindAction(KeybindIds.VisibilityNodes, "Toggle Nodes Visibility", "Show/hide nodes", "Visibility",
            new Keybind(KeyCode.F3)));
        Register(new KeybindAction(KeybindIds.VisibilityItems, "Toggle Items Visibility", "Show/hide items", "Visibility",
            new Keybind(KeyCode.F4)));
        Register(new KeybindAction(KeybindIds.VisibilityPlayers, "Toggle Players Visibility", "Show/hide players", "Visibility",
            new Keybind(KeyCode.F5)));
        Register(new KeybindAction(KeybindIds.VisibilityZombies, "Toggle Zombies Visibility", "Show/hide zombies", "Visibility",
            new Keybind(KeyCode.F6)));
        Register(new KeybindAction(KeybindIds.VisibilityVehicles, "Toggle Vehicles Visibility", "Show/hide vehicles", "Visibility",
            new Keybind(KeyCode.F7)));
        Register(new KeybindAction(KeybindIds.VisibilityBorder, "Toggle Border Visibility", "Show/hide border", "Visibility",
            new Keybind(KeyCode.F8)));
        Register(new KeybindAction(KeybindIds.VisibilityAnimals, "Toggle Animals Visibility", "Show/hide animals", "Visibility",
            new Keybind(KeyCode.F9)));

        Register(new KeybindAction(KeybindIds.ObjectsDelete, "Delete Selected Objects", "Delete selected objects", "Objects",
            new Keybind(KeyCode.Delete)));
        Register(new KeybindAction(KeybindIds.ObjectsDeleteAlt, "Delete Selected Objects (Alt)", "Alternate delete key", "Objects",
            new Keybind(KeyCode.Backspace)));
        Register(new KeybindAction(KeybindIds.ObjectsUndo, "Undo Object Edit", "Undo object changes", "Objects",
            new Keybind(KeyCode.LeftControl, KeyCode.Z)));
        Register(new KeybindAction(KeybindIds.ObjectsRedo, "Redo Object Edit", "Redo object changes", "Objects",
            new Keybind(KeyCode.LeftControl, KeyCode.X)));
        Register(new KeybindAction(KeybindIds.ObjectsCopyTransform, "Copy Transform", "Copy position/rotation/scale", "Objects",
            new Keybind(KeyCode.LeftControl, KeyCode.B)));
        Register(new KeybindAction(KeybindIds.ObjectsPasteTransform, "Paste Transform", "Paste position/rotation/scale", "Objects",
            new Keybind(KeyCode.LeftControl, KeyCode.N)));
        Register(new KeybindAction(KeybindIds.ObjectsCopy, "Copy Selection", "Copy selected objects", "Objects",
            new Keybind(KeyCode.LeftControl, KeyCode.C)));
        Register(new KeybindAction(KeybindIds.ObjectsPaste, "Paste Selection", "Paste copied objects", "Objects",
            new Keybind(KeyCode.LeftControl, KeyCode.V)));

        Register(new KeybindAction(KeybindIds.NavigationDelete, "Delete Navigation Flag", "Delete selected navigation flag", "Navigation",
            new Keybind(KeyCode.Delete)));
        Register(new KeybindAction(KeybindIds.NavigationDeleteAlt, "Delete Navigation Flag (Alt)", "Alternate delete key", "Navigation",
            new Keybind(KeyCode.Backspace)));

        Register(new KeybindAction(KeybindIds.RoadsDelete, "Delete Road Selection", "Delete selected road elements", "Roads",
            new Keybind(KeyCode.Delete)));
        Register(new KeybindAction(KeybindIds.RoadsDeleteAlt, "Delete Road Selection (Alt)", "Alternate delete key", "Roads",
            new Keybind(KeyCode.Backspace)));
        Register(new KeybindAction(KeybindIds.RoadsDeleteRoad, "Delete Whole Road", "Delete the entire selected road", "Roads",
            new Keybind(KeyCode.LeftControl, KeyCode.Delete)));
        Register(new KeybindAction(KeybindIds.RoadsAddToSelection, "Add To Selection", "Add to selection while dragging", "Roads",
            new Keybind(KeyCode.LeftShift)));
        Register(new KeybindAction(KeybindIds.RoadsUndo, "Undo Road Edit", "Undo road changes", "Roads",
            new Keybind(KeyCode.LeftControl, KeyCode.Z)));
        Register(new KeybindAction(KeybindIds.RoadsRedo, "Redo Road Edit", "Redo road changes", "Roads",
            new Keybind(KeyCode.LeftControl, KeyCode.X)));
        Register(new KeybindAction(KeybindIds.RoadsCopyTransform, "Copy Road Transform", "Copy road handle transform", "Roads",
            new Keybind(KeyCode.LeftControl, KeyCode.B)));
        Register(new KeybindAction(KeybindIds.RoadsPasteTransform, "Paste Road Transform", "Paste road handle transform", "Roads",
            new Keybind(KeyCode.LeftControl, KeyCode.N)));

        Register(new KeybindAction(KeybindIds.HighlightPrev, "Previous Highlight", "Focus previous highlighted object", "Highlight",
            new Keybind(KeyCode.LeftArrow)));
        Register(new KeybindAction(KeybindIds.HighlightNext, "Next Highlight", "Focus next highlighted object", "Highlight",
            new Keybind(KeyCode.RightArrow)));

        Register(new KeybindAction(KeybindIds.BrushSetMinHeight, "Set Min Height From Brush", "Set min height to brush position", "Terrain",
            new Keybind(KeyCode.Mouse3)));
        Register(new KeybindAction(KeybindIds.BrushSetMaxHeight, "Set Max Height From Brush", "Set max height to brush position", "Terrain",
            new Keybind(KeyCode.Mouse4)));

        Register(new KeybindAction(KeybindIds.ResourceReplacerUndo, "Resource Replacer Undo", "Undo resource replacement", "Foliage",
            new Keybind(KeyCode.LeftControl, KeyCode.Z)));
        Register(new KeybindAction(KeybindIds.ResourceReplacerRedo, "Resource Replacer Redo", "Redo resource replacement", "Foliage",
            new Keybind(KeyCode.LeftControl, KeyCode.X)));
    }

    private static int GetNextInstanceId() => ++_nextInstanceId;

    private static void Register(KeybindAction action)
    {
        // Set the OrderId when Registering instead of in the KeybindActions Constructor as the Order of calling Register is easier to control
        if (action.OrderId == 0) action.OrderId = GetNextInstanceId();
        ActionsInternal[action.Id] = action;
    }

    private static void LoadBindings()
    {
        try
        {
            if (!File.Exists(KeybindsFile))
            {
                SaveBindings();
                return;
            }

            string json = File.ReadAllText(KeybindsFile);
            Dictionary<string, KeybindData>? data = JsonConvert.DeserializeObject<Dictionary<string, KeybindData>>(json);
            if (data == null) return;

            foreach (KeyValuePair<string, KeybindData> pair in data)
            {
                if (!ActionsInternal.TryGetValue(pair.Key, out KeybindAction? action)) continue;
                action.Current = pair.Value.ToKeybind();
            }
        }
        catch (Exception ex)
        {
            CommandWindow.LogError($"[EditorHelper2] Failed to load keybinds: {ex}");
        }
    }

    private static void SaveBindings()
    {
        try
        {
            Directory.CreateDirectory(Globals.ExtensionsFolder);
            Dictionary<string, KeybindData> data = ActionsInternal.ToDictionary(
                pair => pair.Key,
                pair => new KeybindData(pair.Value.Current)
            );

            string json = JsonConvert.SerializeObject(data);
            File.WriteAllText(KeybindsFile, json);
        }
        catch (Exception ex)
        {
            CommandWindow.LogError($"[EditorHelper2] Failed to save keybinds: {ex}");
        }
    }

    private sealed class KeybindData
    {
        public string[] Keys { get; set; } = Array.Empty<string>();

        public KeybindData()
        {
        }

        public KeybindData(Keybind binding)
        {
            Keys = binding.Keys.Select(key => key.ToString()).ToArray();
        }

        public Keybind ToKeybind()
        {
            if (Keys.Length == 0) return Keybind.None;

            List<KeyCode> parsedKeys = new(Keys.Length);
            foreach (string key in Keys)
            {
                if (!Enum.TryParse(key, out KeyCode parsed))
                {
                    continue;
                }
                if (parsed == KeyCode.None) continue;
                parsedKeys.Add(parsed);
            }

            return parsedKeys.Count == 0 ? Keybind.None : new Keybind(parsedKeys);
        }
    }
}
