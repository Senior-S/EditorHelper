using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace EditorHelper2.Optimization;

internal static class BundleSubsetExporter
{
    public static void Export(MasterBundleExportPlan plan, List<string> warnings)
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

            AssetFileInfo assetBundleInfo = assetsFile.GetAssetsOfType(AssetClassID.AssetBundle).FirstOrDefault()
                ?? throw new InvalidOperationException($"Unable to find AssetBundle metadata inside {plan.SourceBundleFilePath}.");

            AssetTypeValueField assetBundleBaseField = manager.GetBaseField(assetsFileInstance, assetBundleInfo);
            Dictionary<string, long> containerMap = ReadContainerMap(assetBundleBaseField);

            HashSet<long> keepPathIds = [assetBundleInfo.PathId];
            DependencyCrawler crawler = new(manager, assetsFileInstance);

            foreach (string relativeFolder in plan.IncludedRelativeFolders)
            {
                string folderPrefix = BuildFolderPrefix(plan.SourceAssetPrefix, relativeFolder);
                List<long> rootPathIds = containerMap
                    .Where(pair => pair.Key.StartsWith(folderPrefix + "/", StringComparison.OrdinalIgnoreCase))
                    .Select(pair => pair.Value)
                    .Distinct()
                    .ToList();

                if (rootPathIds.Count == 0)
                {
                    warnings.Add($"No bundle container entries were found for {relativeFolder} in {plan.SourceBundleName}.");
                    continue;
                }

                foreach (long pathId in rootPathIds)
                {
                    keepPathIds.Add(pathId);
                    crawler.Crawl(pathId, keepPathIds);
                }
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

    private static Dictionary<string, long> ReadContainerMap(AssetTypeValueField assetBundleBaseField)
    {
        Dictionary<string, long> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (AssetTypeValueField data in assetBundleBaseField["m_Container.Array"].Children)
        {
            string name = data[0].AsString;
            long pathId = data[1]["asset.m_PathID"].AsLong;
            result[name] = pathId;
        }

        return result;
    }

    private static string BuildFolderPrefix(string assetPrefix, string relativeFolder)
    {
        string normalizedRelativeFolder = relativeFolder.Replace('\\', '/').Trim('/');
        if (string.IsNullOrEmpty(normalizedRelativeFolder))
        {
            return assetPrefix.TrimEnd('/');
        }

        return $"{assetPrefix.TrimEnd('/')}/{normalizedRelativeFolder}";
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

    private sealed class DependencyCrawler(AssetsManager manager, AssetsFileInstance sourceFile)
    {
        private readonly HashSet<long> _visited = [];

        public void Crawl(long pathId, HashSet<long> keepPathIds)
        {
            if (!_visited.Add(pathId))
            {
                return;
            }

            AssetFileInfo info = sourceFile.file.GetAssetInfo(pathId);
            AssetTypeValueField baseField = manager.GetBaseField(sourceFile, info);
            CrawlField(baseField, keepPathIds);
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
}