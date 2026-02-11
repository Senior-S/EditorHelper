using System.Collections.Generic;
using UnityEngine;

namespace EditorHelper2.common.Keybinds;

public readonly struct Keybind
{
    private readonly KeyCode[]? _keys;

    public IReadOnlyList<KeyCode> Keys => _keys ?? [];
    public KeyCode PrimaryKey { get; }

    public bool IsNone => _keys == null || _keys.Length == 0;

    public Keybind(params KeyCode[] keys)
        : this((IEnumerable<KeyCode>)keys)
    {
    }

    public Keybind(IEnumerable<KeyCode>? keys)
    {
        if (keys == null)
        {
            _keys = [];
            PrimaryKey = KeyCode.None;
            return;
        }

        List<KeyCode> orderedKeys = [];
        HashSet<KeyCode> seen = [];

        bool hasCtrl = false;
        bool hasShift = false;
        bool hasAlt = false;

        foreach (KeyCode key in keys)
        {
            if (key == KeyCode.None) continue;

            if (IsCtrlKey(key))
            {
                hasCtrl = true;
                continue;
            }

            if (IsShiftKey(key))
            {
                hasShift = true;
                continue;
            }

            if (IsAltKey(key))
            {
                hasAlt = true;
                continue;
            }

            KeyCode normalized = NormalizeKey(key);
            if (seen.Add(normalized))
            {
                orderedKeys.Add(normalized);
            }
        }

        List<KeyCode> finalKeys = [];
        if (hasCtrl) finalKeys.Add(KeyCode.LeftControl);
        if (hasShift) finalKeys.Add(KeyCode.LeftShift);
        if (hasAlt) finalKeys.Add(KeyCode.LeftAlt);
        finalKeys.AddRange(orderedKeys);

        _keys = finalKeys.ToArray();
        PrimaryKey = _keys.Length > 0 ? _keys[^1] : KeyCode.None;
    }

    public static Keybind None => new([]);

    public static bool IsCtrlKey(KeyCode key)
    {
        return key is KeyCode.LeftControl or KeyCode.RightControl;
    }

    public static bool IsShiftKey(KeyCode key)
    {
        return key is KeyCode.LeftShift or KeyCode.RightShift;
    }

    public static bool IsAltKey(KeyCode key)
    {
        return key is KeyCode.LeftAlt or KeyCode.RightAlt or KeyCode.AltGr;
    }

    public static bool IsModifierKey(KeyCode key)
    {
        return IsCtrlKey(key) || IsShiftKey(key) || IsAltKey(key);
    }

    public static KeyCode NormalizeKey(KeyCode key)
    {
        if (IsCtrlKey(key)) return KeyCode.LeftControl;
        if (IsShiftKey(key)) return KeyCode.LeftShift;
        if (IsAltKey(key)) return KeyCode.LeftAlt;
        return key;
    }
}
