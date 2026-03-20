using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    private static readonly HashSet<string> TextFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".asset",
        ".dat",
        ".json"
    };

    public static ModOptimizationPlan CreatePlan(string outputRootPath)
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
                    if (levelObject.asset == null && (levelObject.GUID != Guid.Empty || levelObject.id != 0))
                    {
                        missingMapAssets.Add(BuildMissingObjectDescription(levelObject, x, y));
                        continue;
                    }

                    if (ShouldOptimizeAsset(levelObject.asset))
                    {
                        rootObjectAssets.Add(levelObject.asset);
                    }
                }
            }
        }

        LevelGround.GatherAllTrees(resources);
        foreach (ResourceSpawnpoint resource in resources)
        {
            if (resource.asset == null && (resource.guid != Guid.Empty || resource.id != 0))
            {
                missingMapAssets.Add(BuildMissingResourceDescription(resource));
            }
        }

        List<Asset> rootResourceAssets = resources
            .Select(resource => resource.asset)
            .Where(ShouldOptimizeAsset)
            .Cast<Asset>()
            .ToList();

        foreach (Asset asset in rootObjectAssets.Cast<Asset>().Concat(rootResourceAssets))
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
            RootResourceAssetCount = rootResourceAssets.Select(asset => asset.GUID).Distinct().Count()
        };
        AddMissingAssetWarnings(missingMapAssets, plan.Warnings);

        Dictionary<string, MasterBundleExportPlan> masterBundlePlans = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> standaloneFolderIndices = new(StringComparer.OrdinalIgnoreCase);

        while (pendingAssets.Count > 0)
        {
            Asset asset = pendingAssets.Dequeue();
            if (!visited.Add(asset.GUID))
            {
                continue;
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

            EnqueueReferencedAssets(sourceFolderPath, pendingAssets, plan.Warnings);
        }

        return plan;
    }

    private static void EnqueueReferencedAssets(string sourceFolderPath, Queue<Asset> pendingAssets, List<string> warnings)
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
                if (ShouldOptimizeAsset(referencedAsset))
                {
                    pendingAssets.Enqueue(referencedAsset!);
                }
            }
        }
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

    private static bool IsModAsset(Asset asset)
    {
        if (asset.originMasterBundle != null &&
            string.Equals(asset.originMasterBundle.assetBundleName, "core.masterbundle", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return asset.assetOrigin != EAssetOrigin.OFFICIAL;
    }

    private static string BuildMissingObjectDescription(LevelObject levelObject, int x, int y)
    {
        string guidText = levelObject.GUID == Guid.Empty ? "none" : levelObject.GUID.ToString("N");
        return $"Object in region ({x}, {y}) with GUID {guidText} and ID {levelObject.id}";
    }

    private static string BuildMissingResourceDescription(ResourceSpawnpoint resource)
    {
        string guidText = resource.guid == Guid.Empty ? "none" : resource.guid.ToString("N");
        return $"Resource at {resource.point} with GUID {guidText} and ID {resource.id}";
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
            $"Skipped {missingMapAssets.Count} object/resource reference(s) because their mod assets are not installed locally. " +
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
        private readonly string _prefix;
        private long _counter;

        public GuidTemplateGenerator(string levelName, string outputRootPath)
        {
            byte[] hashBytes = ComputeSha1($"{levelName}|{outputRootPath}");
            _prefix = $"fade21001{hashBytes[0]:x2}{hashBytes[1]:x2}{hashBytes[2]:x2}{hashBytes[3]:x2}";
            _counter = 0;
        }

        public Guid Next()
        {
            _counter++;
            string guidValue = $"{_prefix}{_counter:x16}";
            return Guid.ParseExact(guidValue, "N");
        }
    }
}