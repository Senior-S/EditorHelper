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

    public static string FormatKeybind(Keybind binding)
    {
        if (binding.Key == KeyCode.None) return "Unassigned";

        string keyText = MenuConfigurationControlsUI.getKeyCodeText(binding.Key);
        List<string> parts = [];
        if (binding.Ctrl) parts.Add("Ctrl");
        if (binding.Shift) parts.Add("Shift");
        if (binding.Alt) parts.Add("Alt");
        parts.Add(keyText);
        return string.Join("+", parts);
    }

    public static bool IsDown(Keybind binding)
    {
        if (binding.Key == KeyCode.None) return false;
        return InputEx.GetKeyDown(binding.Key) && AreModifiersExact(binding);
    }

    public static bool IsHeld(Keybind binding)
    {
        if (binding.Key == KeyCode.None) return false;
        return InputEx.GetKey(binding.Key) && AreModifiersExact(binding);
    }

    public static bool IsUp(Keybind binding)
    {
        if (binding.Key == KeyCode.None) return false;
        return InputEx.GetKeyUp(binding.Key) && AreModifiersExact(binding);
    }

    private static bool AreModifiersExact(Keybind binding)
    {
        bool ctrl = InputEx.GetKey(KeyCode.LeftControl) || InputEx.GetKey(KeyCode.RightControl);
        bool shift = InputEx.GetKey(KeyCode.LeftShift) || InputEx.GetKey(KeyCode.RightShift);
        bool alt = InputEx.GetKey(KeyCode.LeftAlt) || InputEx.GetKey(KeyCode.RightAlt);

        if (binding.Ctrl != ctrl) return false;
        if (binding.Shift != shift) return false;
        if (binding.Alt != alt) return false;
        return true;
    }

    private static void RegisterDefaults()
    {
        Register(new KeybindAction(KeybindIds.EditorSave, "Save Level", "Save the current level", "Editor",
            new Keybind(KeyCode.S, ctrl: true)));
        Register(new KeybindAction(KeybindIds.EditorUndo, "Undo", "Undo last transaction", "Editor",
            new Keybind(KeyCode.Z, ctrl: true)));
        Register(new KeybindAction(KeybindIds.EditorRedo, "Redo", "Redo last transaction", "Editor",
            new Keybind(KeyCode.Z, ctrl: true, shift: true)));

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
            new Keybind(KeyCode.Z, ctrl: true)));
        Register(new KeybindAction(KeybindIds.ObjectsRedo, "Redo Object Edit", "Redo object changes", "Objects",
            new Keybind(KeyCode.X, ctrl: true)));
        Register(new KeybindAction(KeybindIds.ObjectsCopyTransform, "Copy Transform", "Copy position/rotation/scale", "Objects",
            new Keybind(KeyCode.B, ctrl: true)));
        Register(new KeybindAction(KeybindIds.ObjectsPasteTransform, "Paste Transform", "Paste position/rotation/scale", "Objects",
            new Keybind(KeyCode.N, ctrl: true)));
        Register(new KeybindAction(KeybindIds.ObjectsCopy, "Copy Selection", "Copy selected objects", "Objects",
            new Keybind(KeyCode.C, ctrl: true)));
        Register(new KeybindAction(KeybindIds.ObjectsPaste, "Paste Selection", "Paste copied objects", "Objects",
            new Keybind(KeyCode.V, ctrl: true)));

        Register(new KeybindAction(KeybindIds.NavigationDelete, "Delete Navigation Flag", "Delete selected navigation flag", "Navigation",
            new Keybind(KeyCode.Delete)));
        Register(new KeybindAction(KeybindIds.NavigationDeleteAlt, "Delete Navigation Flag (Alt)", "Alternate delete key", "Navigation",
            new Keybind(KeyCode.Backspace)));

        Register(new KeybindAction(KeybindIds.RoadsDelete, "Delete Road Selection", "Delete selected road elements", "Roads",
            new Keybind(KeyCode.Delete)));
        Register(new KeybindAction(KeybindIds.RoadsDeleteAlt, "Delete Road Selection (Alt)", "Alternate delete key", "Roads",
            new Keybind(KeyCode.Backspace)));
        Register(new KeybindAction(KeybindIds.RoadsAddToSelection, "Add To Selection", "Add to selection while dragging", "Roads",
            new Keybind(KeyCode.LeftShift)));
        Register(new KeybindAction(KeybindIds.RoadsUndo, "Undo Road Edit", "Undo road changes", "Roads",
            new Keybind(KeyCode.Z, ctrl: true)));
        Register(new KeybindAction(KeybindIds.RoadsRedo, "Redo Road Edit", "Redo road changes", "Roads",
            new Keybind(KeyCode.X, ctrl: true)));
        Register(new KeybindAction(KeybindIds.RoadsCopyTransform, "Copy Road Transform", "Copy road handle transform", "Roads",
            new Keybind(KeyCode.B, ctrl: true)));
        Register(new KeybindAction(KeybindIds.RoadsPasteTransform, "Paste Road Transform", "Paste road handle transform", "Roads",
            new Keybind(KeyCode.N, ctrl: true)));

        Register(new KeybindAction(KeybindIds.HighlightPrev, "Previous Highlight", "Focus previous highlighted object", "Highlight",
            new Keybind(KeyCode.LeftArrow)));
        Register(new KeybindAction(KeybindIds.HighlightNext, "Next Highlight", "Focus next highlighted object", "Highlight",
            new Keybind(KeyCode.RightArrow)));

        Register(new KeybindAction(KeybindIds.BrushSetMinHeight, "Set Min Height From Brush", "Set min height to brush position", "Terrain",
            new Keybind(KeyCode.Mouse3)));
        Register(new KeybindAction(KeybindIds.BrushSetMaxHeight, "Set Max Height From Brush", "Set max height to brush position", "Terrain",
            new Keybind(KeyCode.Mouse4)));

        Register(new KeybindAction(KeybindIds.ResourceReplacerUndo, "Resource Replacer Undo", "Undo resource replacement", "Foliage",
            new Keybind(KeyCode.Z, ctrl: true)));
        Register(new KeybindAction(KeybindIds.ResourceReplacerRedo, "Resource Replacer Redo", "Redo resource replacement", "Foliage",
            new Keybind(KeyCode.X, ctrl: true)));
    }

    private static void Register(KeybindAction action)
    {
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

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(KeybindsFile, json);
        }
        catch (Exception ex)
        {
            CommandWindow.LogError($"[EditorHelper2] Failed to save keybinds: {ex}");
        }
    }

    private sealed class KeybindData
    {
        public string Key { get; set; } = KeyCode.None.ToString();
        public bool Ctrl { get; set; }
        public bool Shift { get; set; }
        public bool Alt { get; set; }

        public KeybindData()
        {
        }

        public KeybindData(Keybind binding)
        {
            Key = binding.Key.ToString();
            Ctrl = binding.Ctrl;
            Shift = binding.Shift;
            Alt = binding.Alt;
        }

        public Keybind ToKeybind()
        {
            if (!Enum.TryParse(Key, out KeyCode parsed))
            {
                parsed = KeyCode.None;
            }
            return new Keybind(parsed, Ctrl, Shift, Alt);
        }
    }
}
