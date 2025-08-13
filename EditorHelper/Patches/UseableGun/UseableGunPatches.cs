using System;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using SDG.NetTransport;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper.Patches;

[HarmonyPatch]
public class UseableGunPatches
{
    /*
    private static bool _rechamberingCurrently = false;

    [HarmonyPatch(typeof(UseableGun), nameof(UseableGun.tick))]
    [HarmonyPrefix]
    [UsedImplicitly]
    static bool tick(UseableGun __instance)
    {
        if (__instance.channel.IsLocalPlayer && __instance.firstAttachments.rope != null)
        {
            if (__instance.firstAttachments.leftHook != null)
            {
                __instance.firstAttachments.rope.SetPosition(0, __instance.firstAttachments.leftHook.position);
            }
            if (__instance.firstAttachments.nockHook != null)
            {
                if (__instance.firstAttachments.magazineModel != null && __instance.firstAttachments.magazineModel.gameObject.activeSelf)
                {
                    __instance.firstAttachments.rope.SetPosition(1, __instance.firstAttachments.nockHook.position);
                }
                else if (__instance.firstAttachments.restHook != null)
                {
                    __instance.firstAttachments.rope.SetPosition(1, __instance.firstAttachments.restHook.position);
                }
            }
            else if (__instance.isAiming)
            {
                __instance.firstAttachments.rope.SetPosition(1, __instance.player.equipment.firstRightHook.position);
            }
            else if ((__instance.isAttaching || __instance.isSprinting || __instance.player.equipment.isInspecting) && __instance.firstAttachments.magazineModel != null && __instance.firstAttachments.magazineModel.gameObject.activeSelf && __instance.firstAttachments.restHook != null)
            {
                __instance.firstAttachments.rope.SetPosition(1, __instance.firstAttachments.restHook.position);
            }
            else if (__instance.firstAttachments.leftHook != null)
            {
                __instance.firstAttachments.rope.SetPosition(1, __instance.firstAttachments.leftHook.position);
            }
            if (__instance.firstAttachments.rightHook != null)
            {
                __instance.firstAttachments.rope.SetPosition(2, __instance.firstAttachments.rightHook.position);
            }
        }
        if (!__instance.player.equipment.IsEquipAnimationFinished)
        {
            return false;
        }
        if ((double)(Time.realtimeSinceStartup - __instance.lastShot) > 0.05)
        {
            if (__instance.firstMuzzleEmitter != null)
            {
                Light component = __instance.firstMuzzleEmitter.GetComponent<Light>();
                if ((bool)component)
                {
                    component.enabled = false;
                }
            }
            if (__instance.thirdMuzzleEmitter != null)
            {
                Light component2 = __instance.thirdMuzzleEmitter.GetComponent<Light>();
                if ((bool)component2)
                {
                    component2.enabled = false;
                }
            }
            if (__instance.firstFakeLight != null)
            {
                Light component3 = __instance.firstFakeLight.GetComponent<Light>();
                if ((bool)component3)
                {
                    component3.enabled = false;
                }
            }
        }
        if ((__instance.player.stance.stance == EPlayerStance.SPRINT && __instance.player.movement.isMoving) || __instance.firemode == EFiremode.SAFETY)
        {
            if (!__instance.isShooting && !__instance.isSprinting && !__instance.isReloading && !__instance.isHammering && !__instance.isUnjamming && !__instance.isAttaching && !__instance.isAiming && !__instance.needsRechamber)
            {
                __instance.isSprinting = true;
                __instance.player.animator.play("Sprint_Start", smooth: false);
            }
        }
        else if (__instance.isSprinting)
        {
            __instance.isSprinting = false;
            if (!__instance.isAiming)
            {
                __instance.player.animator.play("Sprint_Stop", smooth: false);
            }
        }
        if (__instance.channel.IsLocalPlayer)
        {
            if (InputEx.GetKeyUp(ControlsSettings.attach) && __instance.isAttaching)
            {
                __instance.isAttaching = false;
                __instance.player.animator.play("Attach_Stop", smooth: false);
                __instance.stopAttach();
            }
            if (InputEx.GetKeyDown(ControlsSettings.tactical))
            {
                __instance.fireTacticalInput = true;
            }
            if (!PlayerUI.window.showCursor)
            {
                if (InputEx.ConsumeKeyDown(ControlsSettings.attach) && !__instance.isShooting && !__instance.isAttaching && !__instance.isSprinting && !__instance.isReloading && !__instance.isHammering && !__instance.isUnjamming && !__instance.isAiming && !__instance.needsRechamber)
                {
                    __instance.isAttaching = true;
                    __instance.player.animator.play("Attach_Start", smooth: false);
                    __instance.updateAttach();
                    __instance.startAttach();
                }
                if (InputEx.GetKeyDown(ControlsSettings.reload) && !__instance.isShooting && !__instance.isReloading && !__instance.isHammering && !__instance.isUnjamming && !__instance.isSprinting && !__instance.isAttaching && !__instance.needsRechamber)
                {
                    bool includeUnspecifiedCaliber = !__instance.equippedGunAsset.requiresNonZeroAttachmentCaliber;
                    UseableGun.magazineSearchResults.Clear();
                    __instance.player.inventory.FindAttachmentsByCaliber(UseableGun.magazineSearchResults, EItemType.MAGAZINE, __instance.equippedGunAsset.magazineCalibers, includeUnspecifiedCaliber);
                    if (UseableGun.magazineSearchResults.Count > 0)
                    {
                        byte b = 0;
                        byte b2 = byte.MaxValue;
                        for (byte b3 = 0; b3 < UseableGun.magazineSearchResults.Count; b3++)
                        {
                            if (UseableGun.magazineSearchResults[b3].Jar.item.amount > b)
                            {
                                b = UseableGun.magazineSearchResults[b3].Jar.item.amount;
                                b2 = b3;
                            }
                        }
                        if (b2 != byte.MaxValue)
                        {
                            ItemAsset asset = UseableGun.magazineSearchResults[b2].GetAsset();
                            if (asset != null)
                            {
                                UseableGun.SendAttachMagazine.Invoke(__instance.GetNetId(), ENetReliability.Unreliable, UseableGun.magazineSearchResults[b2].Page, UseableGun.magazineSearchResults[b2].Jar.x, UseableGun.magazineSearchResults[b2].Jar.y, asset.hash);
                            }
                        }
                    }
                }
                if (InputEx.GetKeyDown(ControlsSettings.firemode) && !__instance.isAiming)
                {
                    if (__instance.firemode == EFiremode.SAFETY)
                    {
                        if (__instance.equippedGunAsset.hasSemi)
                        {
                            UseableGun.SendChangeFiremode.Invoke(__instance.GetNetId(), ENetReliability.Reliable, EFiremode.SEMI);
                        }
                        else if (__instance.equippedGunAsset.hasAuto)
                        {
                            UseableGun.SendChangeFiremode.Invoke(__instance.GetNetId(), ENetReliability.Reliable, EFiremode.AUTO);
                        }
                        else if (__instance.equippedGunAsset.hasBurst)
                        {
                            UseableGun.SendChangeFiremode.Invoke(__instance.GetNetId(), ENetReliability.Reliable, EFiremode.BURST);
                        }
                        PlayerUI.message(EPlayerMessage.NONE, "");
                    }
                    else if (__instance.firemode == EFiremode.SEMI)
                    {
                        if (__instance.equippedGunAsset.hasAuto)
                        {
                            UseableGun.SendChangeFiremode.Invoke(__instance.GetNetId(), ENetReliability.Reliable, EFiremode.AUTO);
                        }
                        else if (__instance.equippedGunAsset.hasBurst)
                        {
                            UseableGun.SendChangeFiremode.Invoke(__instance.GetNetId(), ENetReliability.Reliable, EFiremode.BURST);
                        }
                        else if (__instance.equippedGunAsset.hasSafety)
                        {
                            UseableGun.SendChangeFiremode.Invoke(__instance.GetNetId(), ENetReliability.Reliable, EFiremode.SAFETY);
                            PlayerUI.message(EPlayerMessage.SAFETY, "");
                        }
                    }
                    else if (__instance.firemode == EFiremode.AUTO)
                    {
                        if (__instance.equippedGunAsset.hasBurst)
                        {
                            UseableGun.SendChangeFiremode.Invoke(__instance.GetNetId(), ENetReliability.Reliable, EFiremode.BURST);
                        }
                        else if (__instance.equippedGunAsset.hasSafety)
                        {
                            UseableGun.SendChangeFiremode.Invoke(__instance.GetNetId(), ENetReliability.Reliable, EFiremode.SAFETY);
                            PlayerUI.message(EPlayerMessage.SAFETY, "");
                        }
                        else if (__instance.equippedGunAsset.hasSemi)
                        {
                            UseableGun.SendChangeFiremode.Invoke(__instance.GetNetId(), ENetReliability.Reliable, EFiremode.SEMI);
                        }
                    }
                    else if (__instance.firemode == EFiremode.BURST)
                    {
                        if (__instance.equippedGunAsset.hasSafety)
                        {
                            UseableGun.SendChangeFiremode.Invoke(__instance.GetNetId(), ENetReliability.Reliable, EFiremode.SAFETY);
                            PlayerUI.message(EPlayerMessage.SAFETY, "");
                        }
                        else if (__instance.equippedGunAsset.hasSemi)
                        {
                            UseableGun.SendChangeFiremode.Invoke(__instance.GetNetId(), ENetReliability.Reliable, EFiremode.SEMI);
                        }
                        else if (__instance.equippedGunAsset.hasAuto)
                        {
                            UseableGun.SendChangeFiremode.Invoke(__instance.GetNetId(), ENetReliability.Reliable, EFiremode.AUTO);
                        }
                    }
                }
            }
            if (__instance.isAttaching)
            {
                if (__instance.sightButton != null)
                {
                    if (__instance.player.look.perspective == EPlayerPerspective.FIRST && !__instance.equippedGunAsset.isTurret)
                    {
                        Vector3 vector = __instance.player.animator.viewmodelCamera.WorldToViewportPoint(__instance.firstAttachments.sightHook.position + __instance.firstAttachments.sightHook.up * 0.05f + __instance.firstAttachments.sightHook.forward * 0.05f);
                        Vector2 vector2 = PlayerUI.container.ViewportToNormalizedPosition(vector);
                        __instance.sightButton.PositionScale_X = vector2.x;
                        __instance.sightButton.PositionScale_Y = vector2.y;
                    }
                    else
                    {
                        __instance.sightButton.PositionScale_X = 0.667f;
                        __instance.sightButton.PositionScale_Y = 0.75f;
                    }
                }
                if (__instance.tacticalButton != null)
                {
                    if (__instance.player.look.perspective == EPlayerPerspective.FIRST && !__instance.equippedGunAsset.isTurret)
                    {
                        Vector3 vector3 = __instance.player.animator.viewmodelCamera.WorldToViewportPoint(__instance.firstAttachments.tacticalHook.position);
                        Vector2 vector4 = PlayerUI.container.ViewportToNormalizedPosition(vector3);
                        __instance.tacticalButton.PositionScale_X = vector4.x;
                        __instance.tacticalButton.PositionScale_Y = vector4.y;
                    }
                    else
                    {
                        __instance.tacticalButton.PositionScale_X = 0.5f;
                        __instance.tacticalButton.PositionScale_Y = 0.25f;
                    }
                }
                if (__instance.gripButton != null)
                {
                    if (__instance.player.look.perspective == EPlayerPerspective.FIRST && !__instance.equippedGunAsset.isTurret)
                    {
                        Vector3 vector5 = __instance.player.animator.viewmodelCamera.WorldToViewportPoint(__instance.firstAttachments.gripHook.position + __instance.firstAttachments.gripHook.forward * -0.05f);
                        Vector2 vector6 = PlayerUI.container.ViewportToNormalizedPosition(vector5);
                        __instance.gripButton.PositionScale_X = vector6.x;
                        __instance.gripButton.PositionScale_Y = vector6.y;
                    }
                    else
                    {
                        __instance.gripButton.PositionScale_X = 0.75f;
                        __instance.gripButton.PositionScale_Y = 0.25f;
                    }
                }
                if (__instance.barrelButton != null)
                {
                    if (__instance.player.look.perspective == EPlayerPerspective.FIRST && !__instance.equippedGunAsset.isTurret)
                    {
                        Vector3 vector7 = __instance.player.animator.viewmodelCamera.WorldToViewportPoint(__instance.firstAttachments.barrelHook.position + __instance.firstAttachments.barrelHook.up * 0.05f);
                        Vector2 vector8 = PlayerUI.container.ViewportToNormalizedPosition(vector7);
                        __instance.barrelButton.PositionScale_X = vector8.x;
                        __instance.barrelButton.PositionScale_Y = vector8.y;
                    }
                    else
                    {
                        __instance.barrelButton.PositionScale_X = 0.25f;
                        __instance.barrelButton.PositionScale_Y = 0.25f;
                    }
                }
                if (__instance.magazineButton != null)
                {
                    if (__instance.player.look.perspective == EPlayerPerspective.FIRST && !__instance.equippedGunAsset.isTurret)
                    {
                        Vector2 viewportPosition = __instance.player.animator.viewmodelCamera.WorldToViewportPoint(__instance.firstAttachments.magazineHook.position + __instance.firstAttachments.magazineHook.forward * -0.1f);
                        Vector2 vector9 = PlayerUI.container.ViewportToNormalizedPosition(viewportPosition);
                        __instance.magazineButton.PositionScale_X = vector9.x;
                        __instance.magazineButton.PositionScale_Y = vector9.y;
                    }
                    else
                    {
                        __instance.magazineButton.PositionScale_X = 0.334f;
                        __instance.magazineButton.PositionScale_Y = 0.75f;
                    }
                }
            }
            if (__instance.rangeLabel != null)
            {
                if (PlayerLifeUI.scopeOverlay.IsVisible)
                {
                    __instance.rangeLabel.PositionOffset_X = -300f;
                    __instance.rangeLabel.PositionOffset_Y = 100f;
                    __instance.rangeLabel.PositionScale_X = 0.5f;
                    __instance.rangeLabel.PositionScale_Y = 0.5f;
                    __instance.rangeLabel.TextAlignment = TextAnchor.UpperRight;
                }
                else
                {
                    Vector3 vector10 = ((__instance.player.look.perspective == EPlayerPerspective.FIRST && __instance.firstAttachments.lightHook != null) ? __instance.player.animator.viewmodelCamera.WorldToViewportPoint(__instance.firstAttachments.lightHook.position) : ((!(__instance.thirdAttachments.lightHook != null)) ? Vector3.zero : MainCamera.instance.WorldToViewportPoint(__instance.thirdAttachments.lightHook.position)));
                    Vector2 vector11 = PlayerLifeUI.container.ViewportToNormalizedPosition(vector10);
                    __instance.rangeLabel.PositionOffset_X = -100f;
                    __instance.rangeLabel.PositionOffset_Y = -15f;
                    __instance.rangeLabel.PositionScale_X = vector11.x;
                    __instance.rangeLabel.PositionScale_Y = vector11.y;
                    __instance.rangeLabel.TextAlignment = TextAnchor.MiddleCenter;
                }
                __instance.rangeLabel.IsVisible = true;
            }
        }
        // Changed here
        if (__instance.needsRechamber && Time.realtimeSinceStartup - __instance.lastShot > __instance.equippedGunAsset.RechamberAfterShotDelay)
        {
            //UnturnedLog.info("[MAIN] Rechambering.");
            __instance.needsRechamber = false;
            __instance.player.equipment.isBusy = false;
            __instance.lastRechamber = Time.realtimeSinceStartup;
            _rechamberingCurrently = true;
            //UnturnedLog.info("[should be true] Rechambering Currently? : " + _rechamberingCurrently);
            if (__instance.equippedGunAsset.CasingEjectCountAfterRechamberingAfterShooting > 0)
            {
                __instance.needsEject = true;
            }

            __instance.hammer();
            //UnturnedLog.info("Hammering");
        }
        if (__instance.needsEject && Time.realtimeSinceStartup - __instance.lastRechamber > __instance.equippedGunAsset.EjectAfterHammerDelay)
        {
            __instance.needsEject = false;
            if (__instance.firstShellEmitter != null && __instance.player.look.perspective == EPlayerPerspective.FIRST && !__instance.equippedGunAsset.isTurret)
            {
                __instance.firstShellEmitter.Emit(__instance.equippedGunAsset.CasingEjectCountAfterRechamberingAfterShooting);
            }
            if (__instance.thirdShellEmitter != null)
            {
                __instance.thirdShellEmitter.Emit(__instance.equippedGunAsset.CasingEjectCountAfterRechamberingAfterShooting);
            }
        }
        if (__instance.needsUnload && Time.realtimeSinceStartup - __instance.startedReload > __instance.equippedGunAsset.EjectAfterReloadDelay)
        {
            __instance.needsUnload = false;
            if (__instance.firstShellEmitter != null && __instance.player.look.perspective == EPlayerPerspective.FIRST && !__instance.equippedGunAsset.isTurret)
            {
                __instance.firstShellEmitter.Emit(__instance.equippedGunAsset.CasingEjectCountAfterReload);
            }
            if (__instance.thirdShellEmitter != null)
            {
                __instance.thirdShellEmitter.Emit(__instance.equippedGunAsset.CasingEjectCountAfterReload);
            }
        }
        if (__instance.needsUnplace && Time.realtimeSinceStartup - __instance.startedReload > __instance.reloadTime * __instance.equippedGunAsset.unplace)
        {
            __instance.needsUnplace = false;
            if (__instance.channel.IsLocalPlayer && __instance.firstAttachments.magazineModel != null)
            {
                __instance.firstAttachments.magazineModel.gameObject.SetActive(value: false);
            }
            if (__instance.thirdAttachments.magazineModel != null)
            {
                __instance.thirdAttachments.magazineModel.gameObject.SetActive(value: false);
            }
            foreach (UseableGunEventHook item in __instance.EnumerateEventComponents())
            {
                InvokeLocalEvent(typeof(UseableGunEventHook), item, "OnMagazineHidden", __instance);
            }
        }
        if (__instance.needsReplace && Time.realtimeSinceStartup - __instance.startedReload > __instance.reloadTime * __instance.equippedGunAsset.replace)
        {
            __instance.needsReplace = false;
            if (__instance.channel.IsLocalPlayer && __instance.firstAttachments.magazineModel != null)
            {
                __instance.firstAttachments.magazineModel.gameObject.SetActive(value: true);
            }
            if (__instance.thirdAttachments.magazineModel != null)
            {
                __instance.thirdAttachments.magazineModel.gameObject.SetActive(value: true);
            }
            foreach (UseableGunEventHook item2 in __instance.EnumerateEventComponents())
            {
                InvokeLocalEvent(typeof(UseableGunEventHook), item2, "OnMagazineVisible", __instance);
            }
        }
        if (__instance.isReloading && Time.realtimeSinceStartup - __instance.startedReload > __instance.reloadTime)
        {
            __instance.isReloading = false;
            if (__instance.needsHammer)
            {
                __instance.hammer();
            }
            else
            {
                __instance.player.equipment.isBusy = false;
            }
        }
        if (__instance.isHammering && Time.realtimeSinceStartup - __instance.startedHammer > __instance.hammerTime)
        {
            __instance.isHammering = false;
            __instance.player.equipment.isBusy = false;
        }
        if (__instance.isUnjamming && Time.realtimeSinceStartup - __instance.startedUnjammingChamber > __instance.unjamChamberDuration)
        {
            __instance.isUnjamming = false;
            __instance.player.equipment.isBusy = false;
        }

        return false;
    }

    [HarmonyPatch(typeof(UseableGun), nameof(UseableGun.stopAim))]
    [HarmonyPrefix]
    [UsedImplicitly]
    private static bool stopAim(UseableGun __instance)
    {
        //UnturnedLog.info("[STOP-AIM] Stopping");
        __instance.UpdateMovementSpeedMultiplier();
        if (__instance.channel.IsLocalPlayer)
        {
            if (!__instance.equippedGunAsset.isTurret)
            {
                __instance.player.animator.viewmodelCameraLocalPositionOffset = Vector3.zero;
            }
            /*if (__instance.equippedGunAsset.driverTurretViewmodelMode == EDriverTurretViewmodelMode.OffscreenWhileAiming)
            {
                __instance.player.animator.drivingViewmodelCameraLocalPositionOffset = Vector3.zero;
            }
            __instance.player.look.ConvertScopeSwayToInputRotation();
            __instance.player.animator.viewmodelSwayMultiplier = 1f;
            __instance.player.animator.viewmodelOffsetPreferenceMultiplier = 1f;
            __instance.player.look.UpdateScopeOverlay();
            __instance.player.look.shouldUseZoomFactorForSensitivity = false;
            __instance.UpdateCrosshairEnabled();
            PlayerUI.instance.groupUI.IsVisible = true;
        }
        __instance.isMinigunSpinning = false; 
        if (!_rechamberingCurrently) __instance.player.animator.play("Aim_Stop", smooth: false);
        InvokeStaticEvent(typeof(UseableGun), "OnAimingChanged_Global", "OnAimingChanged_Global", __instance);
        VehicleTurretEventHook? vehicleTurretEventHook = __instance.GetVehicleTurretEventHook();
        if (vehicleTurretEventHook != null)
        {
            InvokeStaticEvent(typeof(VehicleTurretEventHook), "OnAimingStopped", __instance);
            
            if (__instance.channel.IsLocalPlayer)
            {
                InvokeStaticEvent(typeof(VehicleTurretEventHook), "OnAimingStopped_Local", __instance);
            }
        }
        foreach (UseableGunEventHook item in __instance.EnumerateEventComponents())
        {
            //item.OnAimingStopped?.TryInvoke(this);
            InvokeLocalEvent(typeof(UseableGunEventHook), item, "OnAimingStopped", __instance);
        }

        return false;
    }
    
    private static void InvokeStaticEvent(Type classType, string eventName, params object[] args)
    {
        if (classType == null) return;
        FieldInfo? eventField = classType.GetField(eventName, BindingFlags.Static | BindingFlags.NonPublic);
        if (eventField == null) return;
        Delegate eventDelegate = (Delegate)eventField.GetValue(null);

        eventDelegate?.DynamicInvoke(args);
    }
    
    private static void InvokeLocalEvent(Type classType, object invokeFrom, string eventName, params object[] args)
    {
        if (classType == null) return;
        FieldInfo? eventField = classType.GetField(eventName, BindingFlags.Static | BindingFlags.NonPublic);
        if (eventField == null) return;
        Delegate eventDelegate = (Delegate)eventField.GetValue(invokeFrom);

        eventDelegate?.DynamicInvoke(args);
    }
    */
}