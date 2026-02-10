using System;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.common.Keybinds;

public static class KeybindRebindManager
{
    public static event Action<string?>? ActiveRebindChanged;

    private static int _suppressFrame = -1;
    private static bool _isMenuOpen;
    private static KeyCode _pendingModifier = KeyCode.None;

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
        _pendingModifier = KeyCode.None;
        ActiveRebindChanged?.Invoke(ActiveActionId);
    }

    public static void CancelRebind()
    {
        ActiveActionId = null;
        _suppressFrame = Time.frameCount;
        _pendingModifier = KeyCode.None;
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
                _pendingModifier = Event.current.keyCode;
                return;
            }

            KeybindManager.SetBinding(ActiveActionId!, new Keybind(
                Event.current.keyCode,
                ctrl: Event.current.control,
                shift: Event.current.shift,
                alt: Event.current.alt
            ));
            CancelRebind();
        }
        else if (Event.current.type == EventType.KeyUp)
        {
            if (_pendingModifier != KeyCode.None && Event.current.keyCode == _pendingModifier)
            {
                KeybindManager.SetBinding(ActiveActionId!, new Keybind(_pendingModifier));
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
                KeybindManager.SetBinding(ActiveActionId!, new Keybind(
                    key,
                    ctrl: Event.current.control,
                    shift: Event.current.shift,
                    alt: Event.current.alt
                ));
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
            KeybindManager.SetBinding(ActiveActionId!, new Keybind(KeyCode.Mouse3, ctrl: IsCtrl(), shift: IsShift(), alt: IsAlt()));
            CancelRebind();
        }
        else if (Input.GetKeyDown(KeyCode.Mouse4))
        {
            KeybindManager.SetBinding(ActiveActionId!, new Keybind(KeyCode.Mouse4, ctrl: IsCtrl(), shift: IsShift(), alt: IsAlt()));
            CancelRebind();
        }
        else if (Input.GetKeyDown(KeyCode.Mouse5))
        {
            KeybindManager.SetBinding(ActiveActionId!, new Keybind(KeyCode.Mouse5, ctrl: IsCtrl(), shift: IsShift(), alt: IsAlt()));
            CancelRebind();
        }
        else if (Input.GetKeyDown(KeyCode.Mouse6))
        {
            KeybindManager.SetBinding(ActiveActionId!, new Keybind(KeyCode.Mouse6, ctrl: IsCtrl(), shift: IsShift(), alt: IsAlt()));
            CancelRebind();
        }
    }

    private static bool IsCtrl() => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    private static bool IsShift() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    private static bool IsAlt() => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

    private static bool IsModifierKey(KeyCode key)
    {
        return key is KeyCode.LeftControl or KeyCode.RightControl or
               KeyCode.LeftShift or KeyCode.RightShift or
               KeyCode.LeftAlt or KeyCode.RightAlt;
    }
}
