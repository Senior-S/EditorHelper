using UnityEngine;

namespace EditorHelper2.common.Keybinds;

public readonly struct Keybind
{
    public KeyCode Key { get; }
    public bool Ctrl { get; }
    public bool Shift { get; }
    public bool Alt { get; }

    public Keybind(KeyCode key, bool ctrl = false, bool shift = false, bool alt = false)
    {
        Key = key;
        Ctrl = ctrl;
        Shift = shift;
        Alt = alt;
    }

    public static Keybind None => new(KeyCode.None);
}
