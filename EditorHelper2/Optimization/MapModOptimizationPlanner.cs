using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using SDG.Unturned;

namespace EditorHelper2.Optimization;

internal static class MapModOptimizationPlanner
{
    private static readonly Regex GuidRegex = new(
        @"(?i)\b[0-9a-f]{32}\b|\b[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}\b",
        RegexOptions.Compiled);
    private static readonly Regex QuotedPathRegex = new(
        @"""Path""\s+""(?<path>[^""]+)""",
        RegexOptions.Compiled);
    private static readonly Regex InlineBundlePathRegex = new(
        @"(?m)^[^\r\n""]+\s+(?<path>[^ \t\r\n""]+\.(?:mat|mp3|ogg|wav|png|jpg|jpeg|asset|prefab|anim|controller|shader))\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> TextFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".asset",
        ".dat",
        ".json"
    };

    public static ModOptimizationPlan CreatePlan(
        string outputRootPath,
        bool saveItemsAndVehicles = true,
        bool keepAllModItems = false)
    {
        string normalizedOutputRoot = Path.GetFullPath(outputRootPath);
        if (string.IsNullOrWhiteSpace(normalizedOutputRoot))
        {
            throw new InvalidOperationException("Output path cannot be empty.");
        }

        HashSet<Guid> visited = [];
        Queue<Asset> pendingAssets = new();
        List<Asset> rootObjectAssets = [];
        List<ResourceSpawnpoint> resources = [];
        List<string> missingMapAssets = [];

        for (int x = 0; x < Regions.WORLD_SIZE; x++)
        {
            for (int y = 0; y < Regions.WORLD_SIZE; y++)
            {
                foreach (LevelObject levelObject in LevelObjects.objects[x, y])
                {
                    Asset? objectAsset = levelObject.asset ?? ResolveObjectAsset(levelObject.GUID, levelObject.id);
                    if (objectAsset == null && (levelObject.GUID != Guid.Empty || levelObject.id != 0))
                    {
                        missingMapAssets.Add(BuildMissingObjectDescription(levelObject, x, y));
                        continue;
                    }

                    if (ShouldOptimizeAsset(objectAsset))
                    {
                        rootObjectAssets.Add(objectAsset!);
                    }
                }
            }
        }

        LevelGround.GatherAllTrees(resources);
        foreach (ResourceSpawnpoint resource in resources)
        {
            Asset? resourceAsset = resource.asset ?? ResolveResourceAsset(resource);
            if (resourceAsset == null && resource.guid != Guid.Empty)
            {
                missingMapAssets.Add(BuildMissingResourceDescription(resource));
            }
        }

        List<Asset> rootResourceAssets = resources
            .Select(resource => resource.asset ?? ResolveResourceAsset(resource))
            .Where(ShouldOptimizeAsset)
            .Cast<Asset>()
            .ToList();

        List<Asset> rootItemSpawnAssets = saveItemsAndVehicles ? GatherItemSpawnAssets(missingMapAssets) : [];
        List<Asset> rootVehicleSpawnAssets = saveItemsAndVehicles ? GatherVehicleSpawnAssets(missingMapAssets) : [];

        foreach (Asset asset in rootObjectAssets.Cast<Asset>().Concat(rootResourceAssets).Concat(rootItemSpawnAssets).Concat(rootVehicleSpawnAssets))
        {
            pendingAssets.Enqueue(asset);
        }

        GuidTemplateGenerator guidGenerator = new(Level.info.name, normalizedOutputRoot);
        ModOptimizationPlan plan = new()
        {
            OutputRootPath = normalizedOutputRoot,
            LevelPath = Level.info.path,
            LevelName = Level.info.name,
            RootObjectAssetCount = rootObjectAssets.Select(asset => asset.GUID).Distinct().Count(),
            RootResourceAssetCount = rootResourceAssets.Select(asset => asset.GUID).Distinct().Count(),
            RootItemSpawnAssetCount = rootItemSpawnAssets.Select(asset => asset.GUID).Distinct().Count(),
            RootVehicleSpawnAssetCount = rootVehicleSpawnAssets.Select(asset => asset.GUID).Distinct().Count(),
            SaveItemsAndVehicles = saveItemsAndVehicles,
            KeepAllModItems = keepAllModItems
        };

        Dictionary<string, MasterBundleExportPlan> masterBundlePlans = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> standaloneFolderIndices = new(StringComparer.OrdinalIgnoreCase);
        HashSet<AssetOrigin> itemOriginsEnqueued = [];

        while (pendingAssets.Count > 0)
        {
            Asset asset = pendingAssets.Dequeue();
            if (!visited.Add(asset.GUID))
            {
                continue;
            }

            if (keepAllModItems && asset.origin != null && itemOriginsEnqueued.Add(asset.origin))
            {
                EnqueueAllItemsFromOrigin(asset.origin, pendingAssets);
            }

            if (string.IsNullOrWhiteSpace(asset.absoluteOriginFilePath))
            {
                plan.Warnings.Add($"Skipping {asset.FriendlyName} because it does not have a source file path.");
                continue;
            }

            string sourceDatFilePath = Path.GetFullPath(asset.absoluteOriginFilePath);
            string sourceFolderPath = Path.GetDirectoryName(sourceDatFilePath) ?? string.Empty;
            if (!Directory.Exists(sourceFolderPath))
            {
                plan.Warnings.Add($"Skipping {asset.FriendlyName} because its source folder is missing: {sourceFolderPath}");
                continue;
            }

            EnsureOutputPathIsSafe(normalizedOutputRoot, sourceFolderPath);

            Guid targetGuid = guidGenerator.Next();
            plan.GuidMap[asset.GUID] = targetGuid;

            bool usesMasterBundle = asset.originMasterBundle != null;
            string outputFolderPath;
            string relativeFolderPath;
            string? sourceBundleDirectoryPath = null;
            string? sourceBundleFilePath = null;
            string? sourceBundleName = null;
            string? sourceBundleAssetPrefix = null;
            string? outputBundleDirectoryPath = null;
            string? outputBundleFileName = null;

            if (usesMasterBundle)
            {
                MasterBundleConfig bundle = asset.originMasterBundle!;
                sourceBundleDirectoryPath = Path.GetFullPath(bundle.directoryPath);
                sourceBundleFilePath = Path.GetFullPath(bundle.getAssetBundlePath());
                sourceBundleName = bundle.assetBundleName;
                sourceBundleAssetPrefix = bundle.assetPrefix;
                relativeFolderPath = NormalizeRelativeFolderPath(Path.GetRelativePath(sourceBundleDirectoryPath, sourceFolderPath));

                if (!masterBundlePlans.TryGetValue(sourceBundleDirectoryPath, out MasterBundleExportPlan? bundlePlan))
                {
                    string bundleSlug = BuildBundleSlug(bundle.assetBundleNameWithoutExtension, sourceBundleDirectoryPath);
                    outputBundleDirectoryPath = Path.Combine(normalizedOutputRoot, "Bundles", bundleSlug);
                    outputBundleFileName = $"{bundleSlug}.masterbundle";
                    bundlePlan = new MasterBundleExportPlan
                    {
                        SourceBundleDirectoryPath = sourceBundleDirectoryPath,
                        SourceBundleFilePath = sourceBundleFilePath,
                        SourceBundleName = bundle.assetBundleName,
                        SourceAssetPrefix = bundle.assetPrefix,
                        OutputBundleDirectoryPath = outputBundleDirectoryPath,
                        OutputBundleFilePath = Path.Combine(outputBundleDirectoryPath, outputBundleFileName),
                        OutputBundleName = outputBundleFileName,
                        BundleVersion = bundle.version
                    };
                    masterBundlePlans[sourceBundleDirectoryPath] = bundlePlan;
                    plan.MasterBundles.Add(bundlePlan);

                    if (plan.BundleNameMap.TryGetValue(bundle.assetBundleName, out string? existingOutputBundleName) &&
                        !string.Equals(existingOutputBundleName, outputBundleFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        plan.Warnings.Add(
                            $"Multiple source master bundles share the same bundle name '{bundle.assetBundleName}'. " +
                            $"Text remapping may be ambiguous between '{existingOutputBundleName}' and '{outputBundleFileName}'.");
                    }
                    else
                    {
                        plan.BundleNameMap[bundle.assetBundleName] = outputBundleFileName;
                    }
                }
                else
                {
                    outputBundleDirectoryPath = bundlePlan.OutputBundleDirectoryPath;
                    outputBundleFileName = bundlePlan.OutputBundleName;
                }

                if (!bundlePlan.IncludedRelativeFolders.Contains(relativeFolderPath, StringComparer.OrdinalIgnoreCase))
                {
                    bundlePlan.IncludedRelativeFolders.Add(relativeFolderPath);
                }

                foreach (string rootContainerKey in BuildExactRootContainerKeys(bundle, sourceDatFilePath))
                {
                    if (!bundlePlan.ExactRootContainerKeys.Contains(rootContainerKey, StringComparer.OrdinalIgnoreCase))
                    {
                        bundlePlan.ExactRootContainerKeys.Add(rootContainerKey);
                    }
                }

                AddBundlePathReferenceRoots(sourceFolderPath, bundle, bundlePlan, plan.Warnings);

                outputFolderPath = Path.Combine(outputBundleDirectoryPath, relativeFolderPath);
            }
            else
            {
                relativeFolderPath = Path.GetFileName(sourceFolderPath);
                string parentKey = Path.GetDirectoryName(sourceFolderPath) ?? sourceFolderPath;
                if (!standaloneFolderIndices.TryGetValue(parentKey, out int index))
                {
                    index = standaloneFolderIndices.Count;
                    standaloneFolderIndices[parentKey] = index;
                }

                string standaloneGroupName = $"{index:D2}_{SanitizePathSegment(Path.GetFileName(parentKey))}";
                outputFolderPath = Path.Combine(normalizedOutputRoot, "Standalone", standaloneGroupName, relativeFolderPath);
            }

            plan.Assets.Add(new OptimizedAssetRecord
            {
                SourceGuid = asset.GUID,
                TargetGuid = targetGuid,
                AssetName = asset.FriendlyName,
                SourceDatFilePath = sourceDatFilePath,
                SourceFolderPath = sourceFolderPath,
                OutputFolderPath = outputFolderPath,
                UsesMasterBundle = usesMasterBundle,
                RelativeFolderPath = relativeFolderPath,
                SourceBundleDirectoryPath = sourceBundleDirectoryPath,
                SourceBundleFilePath = sourceBundleFilePath,
                SourceBundleName = sourceBundleName,
                SourceBundleAssetPrefix = sourceBundleAssetPrefix,
                OutputBundleDirectoryPath = outputBundleDirectoryPath,
                OutputBundleFileName = outputBundleFileName
            });

            EnqueueReferencedAssets(sourceFolderPath, pendingAssets, plan.Warnings, saveItemsAndVehicles, keepAllModItems);

            if (saveItemsAndVehicles)
            {
                EnqueueNpcItemVehicleAssets(asset, pendingAssets, missingMapAssets);
            }
        }

        AddMissingAssetWarnings(missingMapAssets, plan.Warnings);
        return plan;
    }

    private static void EnqueueAllItemsFromOrigin(AssetOrigin origin, Queue<Asset> pendingAssets)
    {
        foreach (Asset originAsset in origin.GetAssets())
        {
            if (originAsset is ItemAsset && ShouldOptimizeAsset(originAsset))
            {
                pendingAssets.Enqueue(originAsset);
            }
        }
    }

#pragma warning disable CS0612 // NPC assets still expose legacy IDs for older content; use them only as fallback references.
    private static void EnqueueNpcItemVehicleAssets(Asset asset, Queue<Asset> pendingAssets, List<string> missingMapAssets)
    {
        switch (asset)
        {
            case ObjectNPCAsset npcAsset:
                EnqueueNpcOutfitItems(npcAsset.defaultOutfit, pendingAssets, missingMapAssets, $"{npcAsset.FriendlyName} default outfit");
                EnqueueNpcOutfitItems(npcAsset.halloweenOutfit, pendingAssets, missingMapAssets, $"{npcAsset.FriendlyName} Halloween outfit");
                EnqueueNpcOutfitItems(npcAsset.christmasOutfit, pendingAssets, missingMapAssets, $"{npcAsset.FriendlyName} Christmas outfit");
                EnqueueItemAsset(npcAsset.primaryWeaponGuid, npcAsset.primary, pendingAssets, missingMapAssets, $"{npcAsset.FriendlyName} primary weapon");
                EnqueueItemAsset(npcAsset.secondaryWeaponGuid, npcAsset.secondary, pendingAssets, missingMapAssets, $"{npcAsset.FriendlyName} secondary weapon");
                EnqueueItemAsset(npcAsset.tertiaryWeaponGuid, npcAsset.tertiary, pendingAssets, missingMapAssets, $"{npcAsset.FriendlyName} tertiary weapon");
                EnqueueIfOptimizable(npcAsset.FindDialogueAsset(), pendingAssets);
                break;

            case DialogueAsset dialogueAsset:
                foreach (DialogueMessage message in dialogueAsset.messages ?? [])
                {
                    EnqueueDialogueElementAssets(message, pendingAssets, missingMapAssets);
                    EnqueueIfOptimizable(message.FindPrevDialogueAsset(), pendingAssets);
                }

                foreach (DialogueResponse response in dialogueAsset.responses ?? [])
                {
                    EnqueueDialogueElementAssets(response, pendingAssets, missingMapAssets);
                }

                break;

            case QuestAsset questAsset:
                EnqueueNpcConditionAssets(questAsset.conditions, pendingAssets, missingMapAssets);
                EnqueueNpcRewardAssets(questAsset.rewards, pendingAssets, missingMapAssets);
                EnqueueNpcRewardAssets(GetRewardsListField(questAsset, "abandonmentRewardsList"), pendingAssets, missingMapAssets);
                break;

            case VendorAsset vendorAsset:
                foreach (VendorBuying buying in vendorAsset.buying ?? [])
                {
                    EnqueueVendorElementAssets(buying, pendingAssets, missingMapAssets);
                    EnqueueItemAsset(buying.TargetAssetGuid, buying.id, buying.FindItemAsset(), pendingAssets, missingMapAssets, $"{vendorAsset.FriendlyName} vendor buying entry");
                }

                foreach (VendorSellingBase selling in vendorAsset.selling ?? [])
                {
                    EnqueueVendorElementAssets(selling, pendingAssets, missingMapAssets);

                    if (selling is VendorSellingItem sellingItem)
                    {
                        EnqueueItemAsset(sellingItem.TargetAssetGuid, sellingItem.id, sellingItem.FindItemAsset(), pendingAssets, missingMapAssets, $"{vendorAsset.FriendlyName} vendor selling item");
                    }
                    else if (selling is VendorSellingVehicle sellingVehicle)
                    {
                        EnqueueVehicleAsset(
                            sellingVehicle.TargetAssetGuid,
                            sellingVehicle.id,
                            sellingVehicle.FindVehicleAssetAndHandleRedirects(),
                            pendingAssets,
                            missingMapAssets,
                            $"{vendorAsset.FriendlyName} vendor selling vehicle");
                    }
                }

                break;

            case NPCRewardsAsset rewardsAsset:
                EnqueueNpcConditionAssets(rewardsAsset.conditions, pendingAssets, missingMapAssets);
                EnqueueNpcRewardAssets(GetRewardsListField(rewardsAsset, "rewardsList"), pendingAssets, missingMapAssets);
                break;
        }
    }

    private static void EnqueueDialogueElementAssets(DialogueElement element, Queue<Asset> pendingAssets, List<string> missingMapAssets)
    {
        EnqueueNpcConditionAssets(element.conditions, pendingAssets, missingMapAssets);
        EnqueueNpcRewardAssets(element.rewards, pendingAssets, missingMapAssets);

        if (element is DialogueResponse response)
        {
            EnqueueIfOptimizable(response.FindDialogueAsset(), pendingAssets);
            EnqueueIfOptimizable(response.FindQuestAsset(), pendingAssets);
            EnqueueIfOptimizable(response.FindVendorAsset(), pendingAssets);
        }
    }

    private static void EnqueueVendorElementAssets(VendorElement element, Queue<Asset> pendingAssets, List<string> missingMapAssets)
    {
        EnqueueNpcConditionAssets(element.conditions, pendingAssets, missingMapAssets);
        EnqueueNpcRewardAssets(element.rewards, pendingAssets, missingMapAssets);
    }

    private static void EnqueueNpcConditionAssets(INPCCondition[]? conditions, Queue<Asset> pendingAssets, List<string> missingMapAssets)
    {
        if (conditions == null)
        {
            return;
        }

        foreach (INPCCondition condition in conditions)
        {
            if (condition is NPCItemCondition itemCondition)
            {
                EnqueueItemAsset(Guid.Empty, itemCondition.id, itemCondition.GetItemAsset(), pendingAssets, missingMapAssets, "NPC item condition");
            }
            else if (condition is NPCQuestCondition questCondition)
            {
                EnqueueIfOptimizable(questCondition.GetQuestAsset(), pendingAssets);
            }
        }
    }

    private static void EnqueueNpcRewardAssets(INPCReward[]? rewards, Queue<Asset> pendingAssets, List<string> missingMapAssets)
    {
        if (rewards == null)
        {
            return;
        }

        foreach (INPCReward reward in rewards)
        {
            switch (reward)
            {
                case NPCItemReward itemReward:
                    EnqueueItemAsset(itemReward.itemGuid, itemReward.id, itemReward.GetItemAsset(), pendingAssets, missingMapAssets, "NPC item reward");
                    break;

                case NPCRandomItemReward randomItemReward:
                    EnqueueItemSpawnAsset(randomItemReward.SpawnTableGuid, randomItemReward.id, randomItemReward.FindSpawnAsset(), pendingAssets, missingMapAssets, "NPC random item reward");
                    break;

                case NPCVehicleReward vehicleReward:
                    EnqueueVehicleAsset(
                        vehicleReward.VehicleGuid,
                        vehicleReward.id,
                        vehicleReward.FindVehicleAssetAndHandleRedirects(),
                        pendingAssets,
                        missingMapAssets,
                        "NPC vehicle reward");
                    break;

                case NPCQuestReward questReward:
                    EnqueueIfOptimizable(questReward.GetQuestAsset(), pendingAssets);
                    break;

                case NPCRewardsListAssetReward rewardsListAssetReward:
                    EnqueueIfOptimizable(ResolveAssetReference(rewardsListAssetReward.assetRef), pendingAssets);
                    break;
            }
        }
    }

    private static void EnqueueNpcOutfitItems(NPCAssetOutfit? outfit, Queue<Asset> pendingAssets, List<string> missingMapAssets, string description)
    {
        if (outfit == null)
        {
            return;
        }

        EnqueueItemAsset(outfit.shirtGuid, outfit.shirt, pendingAssets, missingMapAssets, $"{description} shirt");
        EnqueueItemAsset(outfit.pantsGuid, outfit.pants, pendingAssets, missingMapAssets, $"{description} pants");
        EnqueueItemAsset(outfit.hatGuid, outfit.hat, pendingAssets, missingMapAssets, $"{description} hat");
        EnqueueItemAsset(outfit.backpackGuid, outfit.backpack, pendingAssets, missingMapAssets, $"{description} backpack");
        EnqueueItemAsset(outfit.vestGuid, outfit.vest, pendingAssets, missingMapAssets, $"{description} vest");
        EnqueueItemAsset(outfit.maskGuid, outfit.mask, pendingAssets, missingMapAssets, $"{description} mask");
        EnqueueItemAsset(outfit.glassesGuid, outfit.glasses, pendingAssets, missingMapAssets, $"{description} glasses");
    }

    private static void EnqueueItemAsset(Guid guid, ushort id, Queue<Asset> pendingAssets, List<string> missingMapAssets, string description)
    {
        EnqueueItemAsset(guid, id, ResolveItemAsset(guid, id), pendingAssets, missingMapAssets, description);
    }

    private static void EnqueueItemAsset(Guid guid, ItemAsset? itemAsset, Queue<Asset> pendingAssets, List<string> missingMapAssets, string description)
    {
        EnqueueItemAsset(guid, 0, itemAsset, pendingAssets, missingMapAssets, description);
    }

    private static void EnqueueItemAsset(Guid guid, ushort id, ItemAsset? itemAsset, Queue<Asset> pendingAssets, List<string> missingMapAssets, string description)
    {
        if (itemAsset != null)
        {
            EnqueueIfOptimizable(itemAsset, pendingAssets);
            return;
        }

        AddMissingNpcReferenceWarning(guid, id, "item", description, missingMapAssets);
    }

    private static void EnqueueVehicleAsset(Guid guid, ushort id, VehicleAsset? vehicleAsset, Queue<Asset> pendingAssets, List<string> missingMapAssets, string description)
    {
        if (vehicleAsset != null)
        {
            EnqueueIfOptimizable(vehicleAsset, pendingAssets);
            return;
        }

        AddMissingNpcReferenceWarning(guid, id, "vehicle", description, missingMapAssets);
    }

    private static void EnqueueItemSpawnAsset(Guid guid, ushort id, Asset? spawnAsset, Queue<Asset> pendingAssets, List<string> missingMapAssets, string description)
    {
        Asset? asset = spawnAsset ?? ResolveAsset(guid, EAssetType.SPAWN, id);
        if (asset != null)
        {
            List<Asset> assets = [];
            HashSet<Guid> visitedSpawnAssets = [];
            AddSpawnAsset(asset, EAssetType.ITEM, "NPC item", assets, visitedSpawnAssets, missingMapAssets);

            foreach (Asset childAsset in assets)
            {
                EnqueueIfOptimizable(childAsset, pendingAssets);
            }

            return;
        }

        AddMissingNpcReferenceWarning(guid, id, "item spawn table", description, missingMapAssets);
    }

    private static ItemAsset? ResolveItemAsset(Guid guid, ushort id)
    {
        return ResolveAsset(guid, EAssetType.ITEM, id) as ItemAsset;
    }

    private static Asset? ResolveAsset(Guid guid, EAssetType assetType, ushort id)
    {
        if (guid != Guid.Empty)
        {
            Asset? asset = SDG.Unturned.Assets.find(guid);
            if (asset != null)
            {
                return asset;
            }
        }

        return id == 0 ? null : SDG.Unturned.Assets.find(assetType, id);
    }

    private static void EnqueueIfOptimizable(Asset? asset, Queue<Asset> pendingAssets)
    {
        if (ShouldOptimizeAsset(asset))
        {
            pendingAssets.Enqueue(asset!);
        }
    }

    private static Asset? ResolveAssetReference(object? assetReference)
    {
        if (assetReference == null)
        {
            return null;
        }

        Type referenceType = assetReference.GetType();
        MethodInfo? findMethod = referenceType.GetMethod("Find", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
        if (findMethod?.Invoke(assetReference, null) is Asset asset)
        {
            return asset;
        }

        MethodInfo? getMethod = referenceType.GetMethod("Get", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
        if (getMethod?.Invoke(assetReference, null) is Asset getAsset)
        {
            return getAsset;
        }

        return null;
    }

    private static INPCReward[]? GetRewardsListField(object owner, string fieldName)
    {
        FieldInfo? listField = owner.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        object? list = listField?.GetValue(owner);
        if (list == null)
        {
            return null;
        }

        FieldInfo? rewardsField = list.GetType().GetField("rewards", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return rewardsField?.GetValue(list) as INPCReward[];
    }

    private static void AddMissingNpcReferenceWarning(Guid guid, ushort id, string assetKind, string description, List<string> missingMapAssets)
    {
        if (guid == Guid.Empty && id == 0)
        {
            return;
        }

        string guidText = guid == Guid.Empty ? "none" : guid.ToString("N");
        missingMapAssets.Add($"NPC {description} with {assetKind} GUID {guidText} and ID {id}");
    }
#pragma warning restore CS0612

    private static List<Asset> GatherItemSpawnAssets(List<string> missingMapAssets)
    {
        List<Asset> assets = [];
        HashSet<Guid> visitedSpawnAssets = [];

        foreach (ItemTable itemTable in LevelItems.tables)
        {
            if (itemTable.tableID != 0)
            {
                Asset? asset = SDG.Unturned.Assets.find(EAssetType.SPAWN, itemTable.tableID);
                if (asset == null)
                {
                    missingMapAssets.Add(BuildMissingItemSpawnTableDescription(itemTable));
                }
                else
                {
                    AddSpawnAsset(asset, EAssetType.ITEM, "Item", assets, visitedSpawnAssets, missingMapAssets);
                }
            }

            foreach (ItemTier tier in itemTable.tiers)
            {
                foreach (ItemSpawn itemSpawn in tier.table)
                {
                    Asset? asset = SDG.Unturned.Assets.find(EAssetType.ITEM, itemSpawn.item);
                    if (asset == null)
                    {
                        missingMapAssets.Add(BuildMissingItemDescription(itemTable, tier, itemSpawn.item));
                    }
                    else if (ShouldOptimizeAsset(asset))
                    {
                        assets.Add(asset);
                    }
                }
            }
        }

        return assets;
    }

    private static List<Asset> GatherVehicleSpawnAssets(List<string> missingMapAssets)
    {
        List<Asset> assets = [];
        HashSet<Guid> visitedSpawnAssets = [];

        foreach (VehicleTable vehicleTable in LevelVehicles.tables)
        {
            if (vehicleTable.tableID != 0)
            {
                Asset? asset = SDG.Unturned.Assets.find(EAssetType.SPAWN, vehicleTable.tableID);
                if (asset == null)
                {
                    missingMapAssets.Add(BuildMissingVehicleSpawnTableDescription(vehicleTable));
                }
                else
                {
                    AddSpawnAsset(asset, EAssetType.VEHICLE, "Vehicle", assets, visitedSpawnAssets, missingMapAssets);
                }
            }

            foreach (VehicleTier tier in vehicleTable.tiers)
            {
                foreach (VehicleSpawn vehicleSpawn in tier.table)
                {
                    Asset? asset = SDG.Unturned.Assets.find(EAssetType.VEHICLE, vehicleSpawn.vehicle);
                    if (asset == null)
                    {
                        missingMapAssets.Add(BuildMissingVehicleDescription(vehicleTable, tier, vehicleSpawn.vehicle));
                    }
                    else if (ShouldOptimizeAsset(asset))
                    {
                        assets.Add(asset);
                    }
                }
            }
        }

        return assets;
    }

    private static void AddSpawnAsset(
        Asset asset,
        EAssetType legacyAssetType,
        string label,
        List<Asset> assets,
        HashSet<Guid> visitedSpawnAssets,
        List<string> missingMapAssets)
    {
        if (ShouldOptimizeAsset(asset))
        {
            assets.Add(asset);
        }

        if (asset is not SpawnAsset spawnAsset || !visitedSpawnAssets.Add(spawnAsset.GUID))
        {
            return;
        }

        foreach (SpawnTable spawnTable in spawnAsset.tables)
        {
            Asset? childAsset = spawnTable.FindAsset(legacyAssetType);
            if (childAsset == null)
            {
                missingMapAssets.Add($"{label} spawn table {spawnAsset.FriendlyName} has unresolved entry {spawnTable}");
                continue;
            }

            AddSpawnAsset(childAsset, legacyAssetType, label, assets, visitedSpawnAssets, missingMapAssets);
        }
    }

    private static void EnqueueReferencedAssets(
        string sourceFolderPath,
        Queue<Asset> pendingAssets,
        List<string> warnings,
        bool saveItemsAndVehicles,
        bool keepAllModItems)
    {
        foreach (string filePath in Directory.EnumerateFiles(sourceFolderPath, "*", SearchOption.AllDirectories))
        {
            if (!TextFileExtensions.Contains(Path.GetExtension(filePath)))
            {
                continue;
            }

            string contents;
            try
            {
                contents = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                warnings.Add($"Unable to read dependency file {filePath}: {ex.Message}");
                continue;
            }

            foreach (Match match in GuidRegex.Matches(contents))
            {
                if (!Guid.TryParse(match.Value, out Guid guid))
                {
                    continue;
                }

                Asset? referencedAsset = SDG.Unturned.Assets.find(guid);
                if (ShouldOptimizeAsset(referencedAsset) &&
                    ShouldSaveReferencedAsset(referencedAsset!, saveItemsAndVehicles, keepAllModItems))
                {
                    pendingAssets.Enqueue(referencedAsset!);
                }
            }
        }
    }

    private static bool ShouldSaveReferencedAsset(Asset asset, bool saveItemsAndVehicles, bool keepAllModItems)
    {
        return saveItemsAndVehicles ||
               (keepAllModItems && asset is ItemAsset) ||
               asset is not ItemAsset and not VehicleAsset;
    }

    private static void AddBundlePathReferenceRoots(
        string sourceFolderPath,
        MasterBundleConfig bundle,
        MasterBundleExportPlan bundlePlan,
        List<string> warnings)
    {
        foreach (string filePath in Directory.EnumerateFiles(sourceFolderPath, "*", SearchOption.AllDirectories))
        {
            if (!TextFileExtensions.Contains(Path.GetExtension(filePath)))
            {
                continue;
            }

            string contents;
            try
            {
                contents = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                warnings.Add($"Unable to read dependency file {filePath}: {ex.Message}");
                continue;
            }

            foreach (string bundlePath in EnumerateBundlePaths(contents))
            {
                foreach (string rootKey in BuildBundleRootKeysFromReferencedPath(bundle.assetPrefix, bundlePath))
                {
                    if (!bundlePlan.ExactRootContainerKeys.Contains(rootKey, StringComparer.OrdinalIgnoreCase))
                    {
                        bundlePlan.ExactRootContainerKeys.Add(rootKey);
                    }
                }

                string? relativeFolder = TryBuildRelativeFolderFromReferencedPath(bundlePath);
                if (!string.IsNullOrWhiteSpace(relativeFolder) &&
                    !bundlePlan.IncludedRelativeFolders.Contains(relativeFolder, StringComparer.OrdinalIgnoreCase))
                {
                    bundlePlan.IncludedRelativeFolders.Add(relativeFolder);
                }
            }
        }
    }

    private static IEnumerable<string> EnumerateBundlePaths(string contents)
    {
        foreach (Match match in QuotedPathRegex.Matches(contents))
        {
            string path = match.Groups["path"].Value.Trim();
            if (LooksLikeBundlePath(path))
            {
                yield return path;
            }
        }

        foreach (Match match in InlineBundlePathRegex.Matches(contents))
        {
            string path = match.Groups["path"].Value.Trim();
            if (LooksLikeBundlePath(path))
            {
                yield return path;
            }
        }
    }

    private static bool LooksLikeBundlePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        string normalizedPath = path.Replace('\\', '/').Trim();
        if (normalizedPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return normalizedPath.Contains('/');
    }

    private static IEnumerable<string> BuildBundleRootKeysFromReferencedPath(string assetPrefix, string bundlePath)
    {
        string normalizedPrefix = NormalizeBundlePathSegment(assetPrefix);
        string normalizedPath = NormalizeBundlePathSegment(bundlePath);
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(normalizedPrefix) &&
            normalizedPath.StartsWith(normalizedPrefix + "/", StringComparison.OrdinalIgnoreCase))
        {
            yield return normalizedPath;
            yield break;
        }

        if (LooksLikeAbsoluteUnityPath(normalizedPath))
        {
            yield return normalizedPath;
        }

        if (string.IsNullOrWhiteSpace(normalizedPrefix))
        {
            yield return normalizedPath;
            yield break;
        }

        yield return $"{normalizedPrefix}/{normalizedPath}";

        if (!EndsWithBundleSegment(normalizedPrefix))
        {
            yield return $"{normalizedPrefix}/Bundles/{normalizedPath}";
        }
    }

    private static string? TryBuildRelativeFolderFromReferencedPath(string bundlePath)
    {
        string normalizedPath = NormalizeBundlePathSegment(bundlePath);
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return null;
        }

        string? directoryPath = Path.GetDirectoryName(normalizedPath.Replace('/', Path.DirectorySeparatorChar));
        return string.IsNullOrWhiteSpace(directoryPath)
            ? null
            : NormalizeRelativeFolderPath(directoryPath.Replace(Path.DirectorySeparatorChar, '/'));
    }

    private static bool ShouldOptimizeAsset(Asset? asset)
    {
        if (asset == null)
        {
            return false;
        }

        if (!IsModAsset(asset))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(asset.absoluteOriginFilePath);
    }

    private static Asset? ResolveObjectAsset(Guid guid, ushort id)
    {
        if (guid != Guid.Empty && SDG.Unturned.Assets.find(guid) is ObjectAsset objectAssetByGuid)
        {
            return objectAssetByGuid;
        }

        return id == 0 ? null : SDG.Unturned.Assets.find(EAssetType.OBJECT, id);
    }

    private static Asset? ResolveResourceAsset(ResourceSpawnpoint resource)
    {
        if (resource.guid != Guid.Empty && SDG.Unturned.Assets.find(resource.guid) is ResourceAsset resourceAssetByGuid)
        {
            return resourceAssetByGuid;
        }

#pragma warning disable CS0618
        ushort id = resource.id;
#pragma warning restore CS0618
        return id == 0 ? null : SDG.Unturned.Assets.find(EAssetType.RESOURCE, id);
    }

    private static bool IsModAsset(Asset asset)
    {
        if (asset.originMasterBundle != null &&
            string.Equals(asset.originMasterBundle.assetBundleName, "core.masterbundle", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        AssetOrigin? origin = asset.origin;
        return origin == null || !ReferenceEquals(origin, SDG.Unturned.Assets.legacyOfficialOrigin);
    }

    private static string BuildMissingObjectDescription(LevelObject levelObject, int x, int y)
    {
        string guidText = levelObject.GUID == Guid.Empty ? "none" : levelObject.GUID.ToString("N");
        return $"Object in region ({x}, {y}) with GUID {guidText} and ID {levelObject.id}";
    }

    private static string BuildMissingResourceDescription(ResourceSpawnpoint resource)
    {
        string guidText = resource.guid == Guid.Empty ? "none" : resource.guid.ToString("N");
        return $"Resource at {resource.point} with GUID {guidText}";
    }

    private static string BuildMissingItemSpawnTableDescription(ItemTable itemTable)
    {
        return $"Item spawn table \"{itemTable.name}\" with spawn table ID {itemTable.tableID}";
    }

    private static string BuildMissingItemDescription(ItemTable itemTable, ItemTier tier, ushort itemId)
    {
        return $"Item spawn table \"{itemTable.name}\" tier \"{tier.name}\" with item ID {itemId}";
    }

    private static string BuildMissingVehicleSpawnTableDescription(VehicleTable vehicleTable)
    {
        return $"Vehicle spawn table \"{vehicleTable.name}\" with spawn table ID {vehicleTable.tableID}";
    }

    private static string BuildMissingVehicleDescription(VehicleTable vehicleTable, VehicleTier tier, ushort vehicleId)
    {
        return $"Vehicle spawn table \"{vehicleTable.name}\" tier \"{tier.name}\" with vehicle ID {vehicleId}";
    }

    private static void AddMissingAssetWarnings(List<string> missingMapAssets, List<string> warnings)
    {
        if (missingMapAssets.Count == 0)
        {
            return;
        }

        const int previewCount = 6;
        IEnumerable<string> previewLines = missingMapAssets.Take(previewCount);
        warnings.Add(
            $"Skipped {missingMapAssets.Count} object/resource/item spawn/vehicle spawn reference(s) because their mod assets are not installed locally. " +
            $"Optimization continued with the assets available on this machine.");

        foreach (string line in previewLines)
        {
            warnings.Add($"Missing map asset skipped: {line}");
        }

        if (missingMapAssets.Count > previewCount)
        {
            warnings.Add($"Missing map asset skipped: ...and {missingMapAssets.Count - previewCount} more");
        }
    }

    private static string BuildBundleSlug(string baseName, string uniqueSeed)
    {
        string safeName = SanitizePathSegment(baseName);
        string hash = ComputeShortHash(uniqueSeed);
        return $"{safeName}_{hash}";
    }

    private static IEnumerable<string> BuildExactRootContainerKeys(MasterBundleConfig bundle, string sourceDatFilePath)
    {
        string normalizedDatPath = Path.GetFullPath(sourceDatFilePath);
        string normalizedBundleDirectoryPath = Path.GetFullPath(bundle.directoryPath);
        string sourceDirectoryPath = Path.GetDirectoryName(normalizedDatPath) ?? normalizedBundleDirectoryPath;
        string assetName = normalizedDatPath.EndsWith("Asset.dat", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFileName(sourceDirectoryPath)
            : Path.GetFileNameWithoutExtension(normalizedDatPath);

        if (string.IsNullOrWhiteSpace(assetName))
        {
            yield break;
        }

        foreach (string relativePath in BuildRootRelativePaths(normalizedBundleDirectoryPath, normalizedDatPath, sourceDirectoryPath))
        {
            foreach (string rootKey in BuildBundleContainerPathCandidates(bundle.assetPrefix, relativePath, assetName))
            {
                if (!string.IsNullOrWhiteSpace(rootKey))
                {
                    yield return rootKey;
                }
            }
        }
    }

    private static IEnumerable<string> BuildRootRelativePaths(string bundleDirectoryPath, string sourceDatFilePath, string sourceDirectoryPath)
    {
        if (sourceDirectoryPath.StartsWith(bundleDirectoryPath, StringComparison.OrdinalIgnoreCase))
        {
            foreach (string relativePath in ExpandRootRelativePath(Path.GetRelativePath(bundleDirectoryPath, sourceDirectoryPath)))
            {
                yield return relativePath;
            }
        }

        string datWithoutExtension = Path.ChangeExtension(sourceDatFilePath, null) ?? sourceDatFilePath;
        if (datWithoutExtension.StartsWith(bundleDirectoryPath, StringComparison.OrdinalIgnoreCase))
        {
            foreach (string relativePath in ExpandRootRelativePath(Path.GetRelativePath(bundleDirectoryPath, datWithoutExtension)))
            {
                yield return relativePath;
            }
        }
    }

    private static IEnumerable<string> ExpandRootRelativePath(string relativePath)
    {
        string normalizedRelativePath = NormalizeBundlePathSegment(relativePath);
        if (string.IsNullOrWhiteSpace(normalizedRelativePath))
        {
            yield break;
        }

        yield return normalizedRelativePath;

        if (normalizedRelativePath.StartsWith("Bundles/", StringComparison.OrdinalIgnoreCase))
        {
            yield return normalizedRelativePath.Substring("Bundles/".Length);
        }
    }

    private static IEnumerable<string> BuildBundleContainerPathCandidates(string assetPrefix, string relativePath, string assetName)
    {
        string normalizedPrefix = NormalizeBundlePathSegment(assetPrefix);
        string normalizedRelativePath = NormalizeBundlePathSegment(relativePath);
        string normalizedAssetName = NormalizeBundlePathSegment(assetName);
        string relativeRoot = CombineBundleContainerPath(string.Empty, normalizedRelativePath, normalizedAssetName);

        if (string.IsNullOrWhiteSpace(relativeRoot))
        {
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(normalizedPrefix) &&
            relativeRoot.StartsWith(normalizedPrefix + "/", StringComparison.OrdinalIgnoreCase))
        {
            yield return relativeRoot;
            yield break;
        }

        if (LooksLikeAbsoluteUnityPath(relativeRoot))
        {
            yield return relativeRoot;
        }

        if (string.IsNullOrWhiteSpace(normalizedPrefix))
        {
            yield return relativeRoot;
            yield break;
        }

        yield return CombineBundleContainerPath(normalizedPrefix, normalizedRelativePath, normalizedAssetName);

        if (!EndsWithBundleSegment(normalizedPrefix))
        {
            yield return CombineBundleContainerPath($"{normalizedPrefix}/Bundles", normalizedRelativePath, normalizedAssetName);
        }
    }

    private static string CombineBundleContainerPath(string assetPrefix, string relativePath, string assetName)
    {
        List<string> segments = new(3);
        AddBundlePathSegment(segments, assetPrefix);
        AddBundlePathSegment(segments, relativePath);
        AddBundlePathSegment(segments, assetName);
        return string.Join("/", segments);
    }

    private static void AddBundlePathSegment(List<string> segments, string value)
    {
        string normalizedValue = NormalizeBundlePathSegment(value);
        if (!string.IsNullOrWhiteSpace(normalizedValue))
        {
            segments.Add(normalizedValue);
        }
    }

    private static string ComputeShortHash(string value)
    {
        byte[] hashBytes = ComputeSha1(value);
        StringBuilder builder = new(12);
        for (int i = 0; i < 6; i++)
        {
            builder.Append(hashBytes[i].ToString("x2"));
        }
        return builder.ToString();
    }

    private static string SanitizePathSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "asset";
        }

        StringBuilder builder = new(value.Length);
        foreach (char c in value)
        {
            builder.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '_');
        }

        string sanitized = builder.ToString().Trim('_');
        return string.IsNullOrEmpty(sanitized) ? "asset" : sanitized;
    }

    private static string NormalizeRelativeFolderPath(string relativeFolderPath)
    {
        return string.Equals(relativeFolderPath, ".", StringComparison.Ordinal)
            ? string.Empty
            : relativeFolderPath;
    }

    private static string NormalizeBundlePathSegment(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return path.Replace('\\', '/').Trim('/');
    }

    private static bool LooksLikeAbsoluteUnityPath(string path)
    {
        return NormalizeBundlePathSegment(path).StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EndsWithBundleSegment(string path)
    {
        string normalizedPath = NormalizeBundlePathSegment(path);
        return string.Equals(normalizedPath, "Bundles", StringComparison.OrdinalIgnoreCase) ||
               normalizedPath.EndsWith("/Bundles", StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] ComputeSha1(string value)
    {
        using SHA1 sha1 = SHA1.Create();
        return sha1.ComputeHash(Encoding.UTF8.GetBytes(value));
    }

    private static void EnsureOutputPathIsSafe(string outputRootPath, string sourceFolderPath)
    {
        if (IsSameOrChildPath(outputRootPath, sourceFolderPath))
        {
            throw new InvalidOperationException(
                $"Output path '{outputRootPath}' cannot be inside source asset folder '{sourceFolderPath}'. Choose a folder outside the source mods.");
        }
    }

    private static bool IsSameOrChildPath(string candidatePath, string parentPath)
    {
        string normalizedCandidate = Path.GetFullPath(candidatePath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string normalizedParent = Path.GetFullPath(parentPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (string.Equals(normalizedCandidate, normalizedParent, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string parentWithSeparator = normalizedParent + Path.DirectorySeparatorChar;
        return normalizedCandidate.StartsWith(parentWithSeparator, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class GuidTemplateGenerator
    {
        private const string GuidPrefixMarker = "fade2100";
        private readonly string _prefix;
        private ulong _counter;

        public GuidTemplateGenerator(string levelName, string outputRootPath)
        {
            byte[] hashBytes = ComputeSha1($"{levelName}|{outputRootPath}");
            _prefix = $"{GuidPrefixMarker}{hashBytes[0]:x2}{hashBytes[1]:x2}{hashBytes[2]:x2}{hashBytes[3]:x2}";
            if (_prefix.Length != 16)
            {
                throw new InvalidOperationException("Generated GUID prefix must be 16 hexadecimal characters.");
            }

            _counter = 0;
        }

        public Guid Next()
        {
            checked
            {
                _counter++;
            }

            string guidValue = $"{_prefix}{_counter:x16}";
            return Guid.ParseExact(guidValue, "N");
        }
    }
}
