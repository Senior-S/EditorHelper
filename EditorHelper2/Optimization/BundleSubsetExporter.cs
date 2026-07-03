using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace EditorHelper2.Optimization;

internal static class BundleSubsetExporter
{
    public static BundleExportReport Export(MasterBundleExportPlan plan, List<string> warnings, bool enableStreamedPayloadCompaction)
    {
        if (!enableStreamedPayloadCompaction)
        {
            return ExportInternal(plan, warnings, enableStreamedPayloadCompaction: false);
        }

        try
        {
            return ExportInternal(plan, warnings, enableStreamedPayloadCompaction: true);
        }
        catch (Exception ex)
        {
            warnings.Add(
                $"Falling back to metadata-only bundle trimming for {plan.SourceBundleName} because streamed payload compaction failed: {ex.Message}");
        }

        return ExportInternal(plan, warnings, enableStreamedPayloadCompaction: false);
    }

    private static BundleExportReport ExportInternal(MasterBundleExportPlan plan, List<string> warnings, bool enableStreamedPayloadCompaction)
    {
        AssetsManager manager = new();
        BundleFileInstance bundleInstance = manager.LoadBundleFile(plan.SourceBundleFilePath, unpackIfPacked: true);

        try
        {
            int assetsFileIndex = FindPrimaryAssetsFileIndex(bundleInstance.file);
            if (assetsFileIndex < 0)
            {
                throw new InvalidOperationException($"No serialized assets file was found inside {plan.SourceBundleFilePath}.");
            }

            AssetsFileInstance assetsFileInstance = manager.LoadAssetsFileFromBundle(bundleInstance, assetsFileIndex, loadDeps: false)
                ?? throw new InvalidOperationException($"Unable to load serialized assets file from {plan.SourceBundleFilePath}.");

            AssetBundleFile bundleFile = bundleInstance.file;
            AssetsFile assetsFile = assetsFileInstance.file;

            BundleExportReport report = new()
            {
                SourceBundleName = plan.SourceBundleName,
                UsedMetadataOnlyFallback = !enableStreamedPayloadCompaction,
                OriginalSerializedAssetCount = assetsFile.Metadata.AssetInfos.Count,
                OriginalStreamedBytes = GetOriginalStreamedBytes(bundleFile),
                OriginalBundleBytes = new FileInfo(plan.SourceBundleFilePath).Length
            };

            AssetFileInfo assetBundleInfo = assetsFile.GetAssetsOfType(AssetClassID.AssetBundle).FirstOrDefault()
                ?? throw new InvalidOperationException($"Unable to find AssetBundle metadata inside {plan.SourceBundleFilePath}.");

            AssetTypeValueField assetBundleBaseField = manager.GetBaseField(assetsFileInstance, assetBundleInfo);
            Dictionary<string, AssetFileReference> containerMap = ReadContainerMap(assetBundleBaseField);
            ResolvedRootsResult resolvedRoots = ResolveRootPathIds(plan, containerMap, warnings);
            report.RootContainerCount = resolvedRoots.RootPathIds.Count;
            report.UsedFallbackRootResolution = resolvedRoots.UsedFallbackRootResolution;

            if (resolvedRoots.RequiresFullBundleCopy)
            {
                warnings.Add(
                    $"Skipping bundle trimming for {plan.SourceBundleName} because one or more root container entries point to a secondary serialized asset file.");
                return CopyBundleWithoutTrimming(plan, report, usedSafetyFallback: true);
            }

            HashSet<long> keepPathIds = [assetBundleInfo.PathId];
            BundleDependencyCollector collector = new(manager, assetsFileInstance);

            foreach (long rootPathId in resolvedRoots.RootPathIds)
            {
                keepPathIds.Add(rootPathId);
                collector.Crawl(rootPathId, keepPathIds);
            }

            if (!PreserveScriptReferences(manager, assetsFileInstance, keepPathIds, warnings))
            {
                warnings.Add(
                    $"Skipping bundle trimming for {plan.SourceBundleName} because one or more MonoBehaviour script references could not be preserved safely.");
                return CopyBundleWithoutTrimming(plan, report, usedSafetyFallback: true);
            }

            if (ContainsMonoBehaviourAssets(assetsFile, keepPathIds))
            {
                warnings.Add(
                    $"Skipping bundle trimming for {plan.SourceBundleName} because the kept asset graph contains MonoBehaviour assets, which are still unsafe to subset reliably.");
                return CopyBundleWithoutTrimming(plan, report, usedSafetyFallback: true);
            }

            if (enableStreamedPayloadCompaction)
            {
                if (collector.UnresolvedPayloadWarnings.Count > 0)
                {
                    throw new InvalidOperationException(string.Join(" ", collector.UnresolvedPayloadWarnings));
                }

                CompactedPayloadResult payloadResult = BuildCompactedPayloads(bundleFile, collector);
                ApplyStreamedPayloadRewrites(manager, assetsFileInstance, payloadResult.RewritesByAssetPathId);
                ApplyPayloadDirectoryChanges(bundleFile, assetsFileIndex, payloadResult, report);
            }
            else
            {
                report.KeptStreamedBytes = report.OriginalStreamedBytes;
            }

            FilterAssetBundleMetadata(assetBundleBaseField, keepPathIds);
            assetBundleInfo.SetNewData(assetBundleBaseField);

            for (int i = assetsFile.Metadata.AssetInfos.Count - 1; i >= 0; i--)
            {
                if (!keepPathIds.Contains(assetsFile.Metadata.AssetInfos[i].PathId))
                {
                    assetsFile.Metadata.AssetInfos.RemoveAt(i);
                }
            }

            report.KeptSerializedAssetCount = assetsFile.Metadata.AssetInfos.Count;
            bundleFile.BlockAndDirInfo.DirectoryInfos[assetsFileIndex].SetNewData(assetsFile);

            Directory.CreateDirectory(plan.OutputBundleDirectoryPath);
            string uncompressedPath = plan.OutputBundleFilePath + ".uncompressed";

            using (AssetsFileWriter writer = new(uncompressedPath))
            {
                bundleFile.Write(writer);
            }

            AssetBundleFile repackedBundle = new();
            using (AssetsFileReader reader = new(File.OpenRead(uncompressedPath)))
            {
                repackedBundle.Read(reader);
                using AssetsFileWriter packedWriter = new(plan.OutputBundleFilePath);
                repackedBundle.Pack(packedWriter, AssetBundleCompressionType.LZ4);
            }

            File.Delete(uncompressedPath);
            report.FinalBundleBytes = new FileInfo(plan.OutputBundleFilePath).Length;
            return report;
        }
        finally
        {
            manager.UnloadAllBundleFiles();
        }
    }

    private static int FindPrimaryAssetsFileIndex(AssetBundleFile bundleFile)
    {
        for (int i = 0; i < bundleFile.BlockAndDirInfo.DirectoryInfos.Count; i++)
        {
            if (bundleFile.IsAssetsFile(i))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool ContainsMonoBehaviourAssets(AssetsFile assetsFile, HashSet<long> keepPathIds)
    {
        foreach (long pathId in keepPathIds)
        {
            AssetFileInfo info = assetsFile.GetAssetInfo(pathId);
            if ((AssetClassID)info.TypeId == AssetClassID.MonoBehaviour)
            {
                return true;
            }
        }

        return false;
    }

    private static bool PreserveScriptReferences(
        AssetsManager manager,
        AssetsFileInstance assetsFileInstance,
        HashSet<long> keepPathIds,
        List<string> warnings)
    {
        AssetsFile assetsFile = assetsFileInstance.file;
        foreach (long pathId in keepPathIds.ToArray())
        {
            AssetFileInfo info;
            try
            {
                info = assetsFile.GetAssetInfo(pathId);
            }
            catch (Exception ex)
            {
                warnings.Add($"Unable to inspect kept asset {pathId} in {assetsFileInstance.name}: {ex.Message}");
                return false;
            }

            if ((AssetClassID)info.TypeId != AssetClassID.MonoBehaviour)
            {
                continue;
            }

            AssetTypeValueField baseField;
            try
            {
                baseField = manager.GetBaseField(assetsFileInstance, info);
            }
            catch (Exception ex)
            {
                warnings.Add($"Unable to read MonoBehaviour {pathId} in {assetsFileInstance.name}: {ex.Message}");
                return false;
            }

            AssetTypeValueField scriptReference = baseField["m_Script"];
            if (scriptReference.IsDummy)
            {
                warnings.Add($"MonoBehaviour {pathId} in {assetsFileInstance.name} is missing an m_Script reference.");
                return false;
            }

            int fileId = scriptReference["m_FileID"].AsInt;
            long scriptPathId = scriptReference["m_PathID"].AsLong;

            if (scriptPathId == 0 || fileId != 0)
            {
                continue;
            }

            AssetFileInfo scriptInfo;
            try
            {
                scriptInfo = assetsFile.GetAssetInfo(scriptPathId);
            }
            catch (Exception ex)
            {
                warnings.Add(
                    $"MonoBehaviour {pathId} in {assetsFileInstance.name} references local script asset {scriptPathId}, but it could not be found: {ex.Message}");
                return false;
            }

            if ((AssetClassID)scriptInfo.TypeId != AssetClassID.MonoScript)
            {
                warnings.Add(
                    $"MonoBehaviour {pathId} in {assetsFileInstance.name} references local asset {scriptPathId}, but it is {scriptInfo.TypeId} instead of MonoScript.");
                return false;
            }

            keepPathIds.Add(scriptPathId);
        }

        return true;
    }

    private static BundleExportReport CopyBundleWithoutTrimming(
        MasterBundleExportPlan plan,
        BundleExportReport report,
        bool usedSafetyFallback)
    {
        Directory.CreateDirectory(plan.OutputBundleDirectoryPath);
        File.Copy(plan.SourceBundleFilePath, plan.OutputBundleFilePath, overwrite: true);

        report.UsedSafetyFallback = usedSafetyFallback;
        report.KeptSerializedAssetCount = report.OriginalSerializedAssetCount;
        report.KeptStreamedBytes = report.OriginalStreamedBytes;
        report.FinalBundleBytes = new FileInfo(plan.OutputBundleFilePath).Length;
        return report;
    }

    private static long GetOriginalStreamedBytes(AssetBundleFile bundleFile)
    {
        long total = 0;
        foreach (AssetBundleDirectoryInfo directoryInfo in bundleFile.BlockAndDirInfo.DirectoryInfos)
        {
            if (!directoryInfo.IsSerialized)
            {
                total += directoryInfo.DecompressedSize;
            }
        }

        return total;
    }

    private static Dictionary<string, AssetFileReference> ReadContainerMap(AssetTypeValueField assetBundleBaseField)
    {
        Dictionary<string, AssetFileReference> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (AssetTypeValueField data in assetBundleBaseField["m_Container.Array"].Children)
        {
            string name = data[0].AsString;
            result[name] = new AssetFileReference
            {
                FileId = data[1]["asset.m_FileID"].AsInt,
                PathId = data[1]["asset.m_PathID"].AsLong
            };
        }

        return result;
    }

    private static ResolvedRootsResult ResolveRootPathIds(
        MasterBundleExportPlan plan,
        Dictionary<string, AssetFileReference> containerMap,
        List<string> warnings)
    {
        HashSet<long> rootPathIds = [];
        bool usedFallback = false;
        bool requiresFullBundleCopy = false;

        foreach (string exactRootContainerKey in plan.ExactRootContainerKeys)
        {
            if (containerMap.TryGetValue(exactRootContainerKey, out AssetFileReference assetReference))
            {
                if (assetReference.FileId != 0)
                {
                    requiresFullBundleCopy = true;
                    warnings.Add(
                        $"Root container '{exactRootContainerKey}' in {plan.SourceBundleName} points to external file {assetReference.FileId}, which is not safe to subset yet.");
                    continue;
                }

                rootPathIds.Add(assetReference.PathId);
            }
        }

        if (rootPathIds.Count == 0 && plan.ExactRootContainerKeys.Count > 0)
        {
            warnings.Add(
                $"Exact root resolution matched none of {plan.ExactRootContainerKeys.Count} candidate entries in {plan.SourceBundleName}. Falling back to folder-prefix matching.");
        }

        if (plan.IncludedRelativeFolders.Count > 0)
        {
            usedFallback = true;
            foreach (string relativeFolder in plan.IncludedRelativeFolders)
            {
                List<string> folderPrefixes = BuildFolderPrefixes(plan.SourceAssetPrefix, relativeFolder)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                List<long> fallbackPathIds = containerMap
                    .Where(pair => folderPrefixes.Any(folderPrefix => IsSameOrChildBundlePath(pair.Key, folderPrefix)))
                    .Select(pair =>
                    {
                        if (pair.Value.FileId != 0)
                        {
                            requiresFullBundleCopy = true;
                            warnings.Add(
                                $"Fallback container '{pair.Key}' in {plan.SourceBundleName} points to external file {pair.Value.FileId}, which is not safe to subset yet.");
                            return -1L;
                        }

                        return pair.Value.PathId;
                    })
                    .Where(pathId => pathId != -1L)
                    .Distinct()
                    .ToList();

                if (fallbackPathIds.Count == 0)
                {
                    warnings.Add($"No bundle container entries were found for {relativeFolder} in {plan.SourceBundleName}.");
                    continue;
                }

                foreach (long fallbackPathId in fallbackPathIds)
                {
                    rootPathIds.Add(fallbackPathId);
                }
            }
        }

        if (rootPathIds.Count == 0)
        {
            throw new InvalidOperationException($"Unable to resolve any bundle roots in {plan.SourceBundleName}.");
        }

        return new ResolvedRootsResult
        {
            RootPathIds = rootPathIds,
            UsedFallbackRootResolution = usedFallback,
            RequiresFullBundleCopy = requiresFullBundleCopy
        };
    }

    private static IEnumerable<string> BuildFolderPrefixes(string assetPrefix, string relativeFolder)
    {
        string normalizedPrefix = NormalizeBundlePathSegment(assetPrefix);
        string normalizedRelativeFolder = NormalizeBundlePathSegment(relativeFolder);
        if (string.IsNullOrEmpty(normalizedRelativeFolder))
        {
            if (!string.IsNullOrEmpty(normalizedPrefix))
            {
                yield return normalizedPrefix;
            }

            yield break;
        }

        if (!string.IsNullOrEmpty(normalizedPrefix) &&
            normalizedRelativeFolder.StartsWith(normalizedPrefix + "/", StringComparison.OrdinalIgnoreCase))
        {
            yield return normalizedRelativeFolder;
            yield break;
        }

        if (LooksLikeAbsoluteUnityPath(normalizedRelativeFolder))
        {
            yield return normalizedRelativeFolder;
        }

        if (string.IsNullOrEmpty(normalizedPrefix))
        {
            yield return normalizedRelativeFolder;
            yield break;
        }

        yield return $"{normalizedPrefix}/{normalizedRelativeFolder}";

        if (!EndsWithBundleSegment(normalizedPrefix))
        {
            yield return $"{normalizedPrefix}/Bundles/{normalizedRelativeFolder}";
        }
    }

    private static bool IsSameOrChildBundlePath(string candidatePath, string folderPrefix)
    {
        string normalizedCandidatePath = NormalizeBundlePathSegment(candidatePath);
        string normalizedFolderPrefix = NormalizeBundlePathSegment(folderPrefix);
        return string.Equals(normalizedCandidatePath, normalizedFolderPrefix, StringComparison.OrdinalIgnoreCase) ||
               normalizedCandidatePath.StartsWith(normalizedFolderPrefix + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeBundlePathSegment(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : path.Replace('\\', '/').Trim('/');
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

    private static CompactedPayloadResult BuildCompactedPayloads(AssetBundleFile bundleFile, BundleDependencyCollector collector)
    {
        Dictionary<long, List<StreamedPayloadRewrite>> rewritesByAssetPathId = [];
        Dictionary<string, byte[]> compactedEntryDataByDirectoryName = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, byte[]> originalEntryDataCache = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, MemoryStream> compactedStreamsByDirectoryName = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, Dictionary<PayloadRangeKey, long>> offsetMapsByDirectoryName = new(StringComparer.OrdinalIgnoreCase);

        foreach (StreamedPayloadReference reference in collector.StreamedPayloadReferences
                     .OrderBy(reference => reference.ReferencePath, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(reference => reference.Offset)
                     .ThenBy(reference => reference.Size))
        {
            DirectoryEntryMatch entryMatch = ResolvePayloadDirectory(bundleFile, reference.ReferencePath)
                ?? throw new InvalidOperationException($"Unable to find streamed payload entry '{reference.ReferencePath}' in bundle.");

            if (!originalEntryDataCache.TryGetValue(entryMatch.DirectoryName, out byte[]? originalBytes))
            {
                originalBytes = BundleHelper.LoadAssetDataFromBundle(bundleFile, entryMatch.DirectoryIndex)
                    ?? throw new InvalidOperationException($"Unable to read streamed payload entry '{entryMatch.DirectoryName}'.");
                originalEntryDataCache[entryMatch.DirectoryName] = originalBytes;
            }

            if (!offsetMapsByDirectoryName.TryGetValue(entryMatch.DirectoryName, out Dictionary<PayloadRangeKey, long>? offsetMap))
            {
                offsetMap = [];
                offsetMapsByDirectoryName[entryMatch.DirectoryName] = offsetMap;
            }

            if (!compactedStreamsByDirectoryName.TryGetValue(entryMatch.DirectoryName, out MemoryStream? compactedStream))
            {
                compactedStream = new MemoryStream();
                compactedStreamsByDirectoryName[entryMatch.DirectoryName] = compactedStream;
            }

            PayloadRangeKey rangeKey = new(reference.Offset, reference.Size);
            if (!offsetMap.TryGetValue(rangeKey, out long newOffset))
            {
                if (reference.Offset < 0 || reference.Size < 0 || reference.Offset + reference.Size > originalBytes.Length)
                {
                    throw new InvalidOperationException(
                        $"Invalid streamed payload range {reference.Offset}+{reference.Size} for '{entryMatch.DirectoryName}'.");
                }

                newOffset = compactedStream.Position;
                compactedStream.Write(originalBytes, (int)reference.Offset, (int)reference.Size);
                offsetMap[rangeKey] = newOffset;
            }

            if (!rewritesByAssetPathId.TryGetValue(reference.AssetPathId, out List<StreamedPayloadRewrite>? assetRewrites))
            {
                assetRewrites = [];
                rewritesByAssetPathId[reference.AssetPathId] = assetRewrites;
            }

            assetRewrites.Add(new StreamedPayloadRewrite
            {
                Kind = reference.Kind,
                OriginalOffset = reference.Offset,
                Size = reference.Size,
                NewOffset = newOffset
            });
        }

        foreach ((string directoryName, MemoryStream compactedStream) in compactedStreamsByDirectoryName)
        {
            compactedEntryDataByDirectoryName[directoryName] = compactedStream.ToArray();
            compactedStream.Dispose();
        }

        return new CompactedPayloadResult
        {
            RewritesByAssetPathId = rewritesByAssetPathId,
            CompactedEntryDataByDirectoryName = compactedEntryDataByDirectoryName
        };
    }

    private static void ApplyStreamedPayloadRewrites(
        AssetsManager manager,
        AssetsFileInstance assetsFileInstance,
        IReadOnlyDictionary<long, List<StreamedPayloadRewrite>> rewritesByAssetPathId)
    {
        foreach ((long assetPathId, List<StreamedPayloadRewrite> rewrites) in rewritesByAssetPathId)
        {
            AssetFileInfo info = assetsFileInstance.file.GetAssetInfo(assetPathId);
            AssetTypeValueField baseField = manager.GetBaseField(assetsFileInstance, info);
            bool changed = false;

            foreach (StreamedPayloadRewrite rewrite in rewrites)
            {
                switch (rewrite.Kind)
                {
                    case StreamedPayloadKind.StreamData:
                    {
                        AssetTypeValueField streamData = baseField["m_StreamData"];
                        if (!streamData.IsDummy &&
                            streamData["size"].AsLong == rewrite.Size &&
                            streamData["offset"].AsLong == rewrite.OriginalOffset)
                        {
                            streamData["offset"].AsLong = rewrite.NewOffset;
                            changed = true;
                        }

                        break;
                    }
                    case StreamedPayloadKind.AudioResource:
                    {
                        AssetTypeValueField resource = baseField["m_Resource"];
                        if (!resource.IsDummy &&
                            resource["m_Size"].AsLong == rewrite.Size &&
                            resource["m_Offset"].AsLong == rewrite.OriginalOffset)
                        {
                            resource["m_Offset"].AsLong = rewrite.NewOffset;
                            changed = true;
                        }

                        break;
                    }
                }
            }

            if (changed)
            {
                info.SetNewData(baseField);
            }
        }
    }

    private static void ApplyPayloadDirectoryChanges(
        AssetBundleFile bundleFile,
        int assetsFileIndex,
        CompactedPayloadResult payloadResult,
        BundleExportReport report)
    {
        foreach (AssetBundleDirectoryInfo directoryInfo in bundleFile.BlockAndDirInfo.DirectoryInfos)
        {
            if (directoryInfo.IsSerialized || string.Equals(directoryInfo.Name, bundleFile.GetFileName(assetsFileIndex), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (payloadResult.CompactedEntryDataByDirectoryName.TryGetValue(directoryInfo.Name, out byte[]? compactedData))
            {
                directoryInfo.SetNewData(compactedData);
                report.KeptStreamedBytes += compactedData.LongLength;
            }
            else if (IsPayloadDirectoryName(directoryInfo.Name))
            {
                directoryInfo.SetRemoved();
            }
            else
            {
                report.KeptStreamedBytes += directoryInfo.DecompressedSize;
            }
        }
    }

    private static void FilterAssetBundleMetadata(AssetTypeValueField assetBundleBaseField, HashSet<long> keepPathIds)
    {
        AssetTypeValueField preloadArray = assetBundleBaseField["m_PreloadTable.Array"];
        for (int i = preloadArray.Children.Count - 1; i >= 0; i--)
        {
            long pathId = preloadArray.Children[i]["m_PathID"].AsLong;
            if (!keepPathIds.Contains(pathId))
            {
                preloadArray.Children.RemoveAt(i);
            }
        }

        AssetTypeValueField containerArray = assetBundleBaseField["m_Container.Array"];
        for (int i = containerArray.Children.Count - 1; i >= 0; i--)
        {
            long pathId = containerArray.Children[i][1]["asset.m_PathID"].AsLong;
            if (!keepPathIds.Contains(pathId))
            {
                containerArray.Children.RemoveAt(i);
            }
        }

        AssetTypeValueField mainAsset = assetBundleBaseField["m_MainAsset"];
        if (!mainAsset.IsDummy && !keepPathIds.Contains(mainAsset["asset.m_PathID"].AsLong))
        {
            mainAsset["asset.m_FileID"].AsInt = 0;
            mainAsset["asset.m_PathID"].AsLong = 0;
        }
    }

    private static DirectoryEntryMatch? ResolvePayloadDirectory(AssetBundleFile bundleFile, string referencePath)
    {
        string normalizedReferencePath = NormalizePayloadReferencePath(referencePath);
        string fileName = Path.GetFileName(normalizedReferencePath);

        for (int i = 0; i < bundleFile.BlockAndDirInfo.DirectoryInfos.Count; i++)
        {
            AssetBundleDirectoryInfo directoryInfo = bundleFile.BlockAndDirInfo.DirectoryInfos[i];
            if (directoryInfo.IsSerialized)
            {
                continue;
            }

            string normalizedDirectoryName = NormalizePayloadReferencePath(directoryInfo.Name);
            if (string.Equals(normalizedDirectoryName, normalizedReferencePath, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileName(normalizedDirectoryName), fileName, StringComparison.OrdinalIgnoreCase))
            {
                return new DirectoryEntryMatch
                {
                    DirectoryIndex = i,
                    DirectoryName = directoryInfo.Name
                };
            }
        }

        return null;
    }

    private static bool IsPayloadDirectoryName(string directoryName)
    {
        string extension = Path.GetExtension(directoryName);
        return extension.Equals(".resS", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".resource", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".resources", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class BundleDependencyCollector(AssetsManager manager, AssetsFileInstance sourceFile)
    {
        private readonly HashSet<long> _visited = [];

        public List<StreamedPayloadReference> StreamedPayloadReferences { get; } = [];
        public List<string> UnresolvedPayloadWarnings { get; } = [];

        public void Crawl(long pathId, HashSet<long> keepPathIds)
        {
            if (!_visited.Add(pathId))
            {
                return;
            }

            AssetFileInfo info = sourceFile.file.GetAssetInfo(pathId);
            AssetTypeValueField baseField = manager.GetBaseField(sourceFile, info);
            CollectStreamedPayloadReferences(pathId, info, baseField);
            CrawlField(baseField, keepPathIds);
        }

        private void CollectStreamedPayloadReferences(long assetPathId, AssetFileInfo info, AssetTypeValueField baseField)
        {
            AssetClassID assetTypeId = (AssetClassID)info.TypeId;

            if (assetTypeId == AssetClassID.Texture2D || assetTypeId == AssetClassID.Mesh)
            {
                AssetTypeValueField streamData = baseField["m_StreamData"];
                if (!streamData.IsDummy)
                {
                    long size = streamData["size"].AsLong;
                    if (size <= 0)
                    {
                        return;
                    }

                    string referencePath = NormalizePayloadReferencePath(streamData["path"].AsString);
                    if (string.IsNullOrWhiteSpace(referencePath))
                    {
                        UnresolvedPayloadWarnings.Add(
                            $"Texture/Mesh asset {assetPathId} in {sourceFile.name} has streamed data but no payload path.");
                        return;
                    }

                    StreamedPayloadReferences.Add(new StreamedPayloadReference
                    {
                        AssetPathId = assetPathId,
                        ReferencePath = referencePath,
                        Offset = streamData["offset"].AsLong,
                        Size = size,
                        Kind = StreamedPayloadKind.StreamData
                    });
                }
            }
            else if (assetTypeId == AssetClassID.AudioClip)
            {
                AssetTypeValueField resource = baseField["m_Resource"];
                if (!resource.IsDummy)
                {
                    long size = resource["m_Size"].AsLong;
                    if (size <= 0)
                    {
                        return;
                    }

                    string referencePath = NormalizePayloadReferencePath(GetAudioResourcePath(resource));
                    if (string.IsNullOrWhiteSpace(referencePath))
                    {
                        UnresolvedPayloadWarnings.Add(
                            $"AudioClip asset {assetPathId} in {sourceFile.name} has streamed data but no payload path.");
                        return;
                    }

                    StreamedPayloadReferences.Add(new StreamedPayloadReference
                    {
                        AssetPathId = assetPathId,
                        ReferencePath = referencePath,
                        Offset = resource["m_Offset"].AsLong,
                        Size = size,
                        Kind = StreamedPayloadKind.AudioResource
                    });
                }
            }
        }

        private void CrawlField(AssetTypeValueField field, HashSet<long> keepPathIds)
        {
            foreach (AssetTypeValueField child in field)
            {
                AssetTypeTemplateField templateField = child.TemplateField;
                if (templateField.HasValue && !templateField.IsArray)
                {
                    continue;
                }

                if (templateField.IsArray && templateField.Children[1].ValueType != AssetValueType.None)
                {
                    continue;
                }

                string typeName = templateField.Type;
                if (typeName.StartsWith("PPtr<", StringComparison.Ordinal) &&
                    typeName.EndsWith(">", StringComparison.Ordinal))
                {
                    int fileId = child["m_FileID"].AsInt;
                    long pointedPathId = child["m_PathID"].AsLong;
                    if (pointedPathId == 0 || fileId != 0)
                    {
                        continue;
                    }

                    if (keepPathIds.Add(pointedPathId))
                    {
                        Crawl(pointedPathId, keepPathIds);
                    }
                }
                else
                {
                    CrawlField(child, keepPathIds);
                }
            }
        }
    }

    private static string GetAudioResourcePath(AssetTypeValueField resource)
    {
        AssetTypeValueField sourceField = resource["m_Source"];
        if (!sourceField.IsDummy)
        {
            return sourceField.AsString;
        }

        AssetTypeValueField pathField = resource["m_Path"];
        if (!pathField.IsDummy)
        {
            return pathField.AsString;
        }

        return string.Empty;
    }

    private static string NormalizePayloadReferencePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        string normalizedPath = path.Replace('\\', '/');
        if (normalizedPath.StartsWith("archive:/", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath = normalizedPath.Substring("archive:/".Length);
        }

        return normalizedPath.TrimStart('/');
    }

    private sealed class ResolvedRootsResult
    {
        public HashSet<long> RootPathIds { get; set; } = [];
        public bool UsedFallbackRootResolution { get; set; }
        public bool RequiresFullBundleCopy { get; set; }
    }

    private sealed class AssetFileReference
    {
        public int FileId { get; set; }
        public long PathId { get; set; }
    }

    private sealed class StreamedPayloadReference
    {
        public long AssetPathId { get; set; }
        public string ReferencePath { get; set; } = string.Empty;
        public long Offset { get; set; }
        public long Size { get; set; }
        public StreamedPayloadKind Kind { get; set; }
    }

    private sealed class StreamedPayloadRewrite
    {
        public StreamedPayloadKind Kind { get; set; }
        public long OriginalOffset { get; set; }
        public long Size { get; set; }
        public long NewOffset { get; set; }
    }

    private sealed class CompactedPayloadResult
    {
        public IReadOnlyDictionary<long, List<StreamedPayloadRewrite>> RewritesByAssetPathId { get; set; } =
            new Dictionary<long, List<StreamedPayloadRewrite>>();

        public IReadOnlyDictionary<string, byte[]> CompactedEntryDataByDirectoryName { get; set; } =
            new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class DirectoryEntryMatch
    {
        public int DirectoryIndex { get; set; }
        public string DirectoryName { get; set; } = string.Empty;
    }

    private readonly record struct PayloadRangeKey(long Offset, long Size);

    private enum StreamedPayloadKind
    {
        StreamData,
        AudioResource
    }
}