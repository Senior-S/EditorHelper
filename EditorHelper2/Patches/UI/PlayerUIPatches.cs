using System;
using System.Reflection;
using System.Text;
using EditorHelper2.Extensions.PlayerUIs;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;

namespace EditorHelper2.Patches.UI;

[HarmonyPatch(typeof(PlayerUI), "Update")]
public static class PlayerUIPatches
{
    [HarmonyPrefix]
    [UsedImplicitly]
    private static void PrefixUpdate()
    {
        PlayerCinematicModeExtension.Current?.HandleInput();
    }

    [HarmonyPostfix]
    [UsedImplicitly]
    private static void PostfixUpdate()
    {
        PlayerCinematicModeExtension.Current?.AfterPlayerUiUpdate();
    }
}

[HarmonyPatch(typeof(PlayerLook), "Update")]
public static class PlayerCinematicLookPatches
{
    [HarmonyPrefix]
    [UsedImplicitly]
    private static void PrefixUpdate()
    {
        PlayerCinematicModeExtension.Current?.PrepareGameInput();
    }
}

[HarmonyPatch(typeof(PlayerMovement), "Update")]
public static class PlayerCinematicMovementPatches
{
    [HarmonyPrefix]
    [UsedImplicitly]
    private static void PrefixUpdate()
    {
        PlayerCinematicModeExtension.Current?.PrepareGameInput();
    }
}

[HarmonyPatch(typeof(LightingManager), "Update")]
public static class PlayerCinematicLightingPatches
{
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void PostfixUpdate()
    {
        PlayerCinematicModeExtension.Current?.ApplyEnvironmentOverrides();
    }
}

[HarmonyPatch]
public static class PlayerCinematicDebugStringPatches
{
    private static readonly Type? GlazierBaseType = typeof(PlayerUI).Assembly.GetType("SDG.Unturned.GlazierBase");
    private static readonly FieldInfo? DebugBuilderField = GlazierBaseType?
        .GetField("debugBuilder", BindingFlags.Instance | BindingFlags.NonPublic);

    private static MethodBase? TargetMethod()
    {
        return GlazierBaseType?.GetMethod("UpdateDebugString", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    [HarmonyPostfix]
    [UsedImplicitly]
    private static void PostfixUpdateDebugString(object __instance)
    {
        if (PlayerCinematicModeExtension.Current == null
            || Player.LocalPlayer == null
            || !Player.LocalPlayer.look.canUseFreecam)
        {
            return;
        }

        if (DebugBuilderField?.GetValue(__instance) is StringBuilder builder)
        {
            builder.Append(" F8");
        }
    }
}

[HarmonyPatch(typeof(PlayerPauseUI), "open")]
public static class PlayerCinematicPausePatches
{
    [HarmonyPrefix]
    [UsedImplicitly]
    private static void PrefixOpen()
    {
        PlayerCinematicModeExtension.Current?.ExitMode();
    }
}

[HarmonyPatch(typeof(PlayerDashboardUI), "open")]
public static class PlayerCinematicDashboardPatches
{
    [HarmonyPrefix]
    [UsedImplicitly]
    private static void PrefixOpen()
    {
        PlayerCinematicModeExtension.Current?.ExitMode();
    }
}
