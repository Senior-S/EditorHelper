using System;
using System.Collections.Generic;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.common.Keybinds;

public static class KeybindRebindManager
{
    public static event Action<string?>? ActiveRebindChanged;

    private static int _suppressFrame = -1;
    private static bool _isMenuOpen;
    private static List<KeyCode> _pendingModifiers = new(3);

    public static string? ActiveActionId { get; private set; }
    public static bool IsRebinding => !string.IsNullOrEmpty(ActiveActionId);
    public static bool ShouldIgnoreInput => IsRebinding || _isMenuOpen || Time.frameCount == _suppressFrame;

    public static void SetMenuOpen(bool open)
    {
        _isMenuOpen = open;
        if (open)
        {
            _suppressFrame = Time.frameCount;
        }
    }

    public static void BeginRebind(string actionId)
    {
        ActiveActionId = actionId;
        _pendingModifiers.Clear();
        ActiveRebindChanged?.Invoke(ActiveActionId);
    }

    public static void CancelRebind()
    {
        ActiveActionId = null;
        _suppressFrame = Time.frameCount;
        _pendingModifiers.Clear();
        ActiveRebindChanged?.Invoke(ActiveActionId);
    }

    public static void HandleOnGUI()
    {
        if (!IsRebinding) return;
        if (Event.current == null) return;

        if (Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == KeyCode.Escape)
            {
                CancelRebind();
                return;
            }

            if (Event.current.keyCode == KeyCode.Backspace)
            {
                KeybindManager.SetBinding(ActiveActionId!, Keybind.None);
                CancelRebind();
                return;
            }

            if (IsModifierKey(Event.current.keyCode))
            {
                _pendingModifiers = BuildModifierKeys(Event.current.control, Event.current.shift, Event.current.alt);
                return;
            }

            List<KeyCode> keys = BuildModifierKeys(Event.current.control, Event.current.shift, Event.current.alt);
            keys.Add(Event.current.keyCode);
            KeybindManager.SetBinding(ActiveActionId!, new Keybind(keys));
            CancelRebind();
        }
        else if (Event.current.type == EventType.KeyUp)
        {
            if (_pendingModifiers.Count > 0 && IsModifierKey(Event.current.keyCode))
            {
                KeybindManager.SetBinding(ActiveActionId!, new Keybind(_pendingModifiers));
                CancelRebind();
            }
        }
        else if (Event.current.type == EventType.MouseDown)
        {
            KeyCode key = Event.current.button switch
            {
                0 => KeyCode.Mouse0,
                1 => KeyCode.Mouse1,
                2 => KeyCode.Mouse2,
                3 => KeyCode.Mouse3,
                4 => KeyCode.Mouse4,
                5 => KeyCode.Mouse5,
                6 => KeyCode.Mouse6,
                _ => KeyCode.None
            };

            if (key != KeyCode.None)
            {
                List<KeyCode> keys = BuildModifierKeys(Event.current.control, Event.current.shift, Event.current.alt);
                keys.Add(key);
                KeybindManager.SetBinding(ActiveActionId!, new Keybind(keys));
                CancelRebind();
            }
        }
    }

    public static void HandleUpdate()
    {
        if (!IsRebinding) return;
        if (!Glazier.Get().ShouldGameProcessKeyDown) return;

        if (Input.GetKeyDown(KeyCode.Mouse3))
        {
            List<KeyCode> keys = BuildModifierKeys(IsCtrl(), IsShift(), IsAlt());
            keys.Add(KeyCode.Mouse3);
            KeybindManager.SetBinding(ActiveActionId!, new Keybind(keys));
            CancelRebind();
        }
        else if (Input.GetKeyDown(KeyCode.Mouse4))
        {
            List<KeyCode> keys = BuildModifierKeys(IsCtrl(), IsShift(), IsAlt());
            keys.Add(KeyCode.Mouse4);
            KeybindManager.SetBinding(ActiveActionId!, new Keybind(keys));
            CancelRebind();
        }
        else if (Input.GetKeyDown(KeyCode.Mouse5))
        {
            List<KeyCode> keys = BuildModifierKeys(IsCtrl(), IsShift(), IsAlt());
            keys.Add(KeyCode.Mouse5);
            KeybindManager.SetBinding(ActiveActionId!, new Keybind(keys));
            CancelRebind();
        }
        else if (Input.GetKeyDown(KeyCode.Mouse6))
        {
            List<KeyCode> keys = BuildModifierKeys(IsCtrl(), IsShift(), IsAlt());
            keys.Add(KeyCode.Mouse6);
            KeybindManager.SetBinding(ActiveActionId!, new Keybind(keys));
            CancelRebind();
        }
    }

    private static bool IsCtrl() => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    private static bool IsShift() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    private static bool IsAlt() => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

    private static bool IsModifierKey(KeyCode key)
    {
        return Keybind.IsModifierKey(key);
    }

    private static List<KeyCode> BuildModifierKeys(bool ctrl, bool shift, bool alt)
    {
        List<KeyCode> keys = new(3);
        if (ctrl) keys.Add(KeyCode.LeftControl);
        if (shift) keys.Add(KeyCode.LeftShift);
        if (alt) keys.Add(KeyCode.LeftAlt);
        return keys;
    }
}
