using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDG.Unturned;

namespace EditorHelper2.Optimization;

internal static class MapModOptimizationExecutor
{
    private static readonly HashSet<string> TextFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".asset",
        ".dat",
        ".json"
    };

    public static ModOptimizationResult Execute(ModOptimizationPlan plan, Action<string>? reportProgress = null)
    {
        ReportProgress(reportProgress, "Preparing output folders...");
        Directory.CreateDirectory(plan.OutputRootPath);
        List<BundleExportReport> bundleReports = [];

        for (int i = 0; i < plan.Assets.Count; i++)
        {
            OptimizedAssetRecord asset = plan.Assets[i];
            if (ShouldReportItemProgress(i + 1, plan.Assets.Count))
            {
                ReportProgress(reportProgress, $"Copying asset folders {i + 1}/{plan.Assets.Count}: {asset.AssetName}");
            }

            CopyDirectory(asset.SourceFolderPath, asset.OutputFolderPath);
        }

        ExportMasterBundles(plan, bundleReports, reportProgress);

        for (int i = 0; i < plan.Assets.Count; i++)
        {
            OptimizedAssetRecord asset = plan.Assets[i];
            if (ShouldReportItemProgress(i + 1, plan.Assets.Count))
            {
                ReportProgress(reportProgress, $"Rewriting asset references {i + 1}/{plan.Assets.Count}: {asset.AssetName}");
            }

            RewriteTextFiles(asset.OutputFolderPath, plan.GuidMap, plan.BundleNameMap);
        }

        ReportProgress(reportProgress, "Backing up map files...");
        string mapBackupPath = BackupMapFiles(plan);

        ReportProgress(reportProgress, "Patching Level/Objects.dat...");
        int patchedObjectCount = PatchObjectsFile(Path.Combine(plan.LevelPath, "Level", "Objects.dat"), plan.GuidMap);

        ReportProgress(reportProgress, "Patching Terrain/Trees.dat...");
        int patchedResourceCount = PatchTreesFile(Path.Combine(plan.LevelPath, "Terrain", "Trees.dat"), plan.GuidMap);

        ReportProgress(reportProgress, "Writing optimization report...");
        string reportPath = WriteReport(plan, bundleReports, patchedObjectCount, patchedResourceCount, mapBackupPath);

        ModOptimizationResult result = new()
        {
            ExportedAssetCount = plan.Assets.Count,
            MasterBundleCount = plan.MasterBundles.Count,
            PatchedObjectCount = patchedObjectCount,
            PatchedResourceCount = patchedResourceCount,
            ReportPath = reportPath,
            MapBackupPath = mapBackupPath
        };

        result.BundleReports.AddRange(bundleReports);
        result.Warnings.AddRange(plan.Warnings);
        ReportProgress(reportProgress, "Optimization finished.");
        return result;
    }

    private static void WriteMasterBundleConfig(MasterBundleExportPlan plan)
    {
        string configPath = Path.Combine(plan.OutputBundleDirectoryPath, "MasterBundle.dat");
        StringBuilder builder = new();
        builder.AppendLine($"Asset_Bundle_Name {plan.OutputBundleName}");
        builder.AppendLine();
        builder.AppendLine($"Asset_Prefix {plan.SourceAssetPrefix}");
        builder.AppendLine();
        builder.AppendLine($"Asset_Bundle_Version {plan.BundleVersion}");
        builder.AppendLine();
        builder.AppendLine("Has_Clip_Prefab false");
        File.WriteAllText(configPath, builder.ToString());
    }

    private static void ExportMasterBundles(
        ModOptimizationPlan plan,
        List<BundleExportReport> bundleReports,
        Action<string>? reportProgress)
    {
        if (plan.MasterBundles.Count == 0)
        {
            return;
        }

        int maxParallelism = Math.Max(1, plan.MaxParallelMasterBundleExports);
        bool enableStreamedPayloadCompaction = !plan.UseMetadataOnlyBundleTrim;
        BundleExportReport?[] reports = new BundleExportReport?[plan.MasterBundles.Count];
        List<string>[] warningsByBundle = new List<string>[plan.MasterBundles.Count];

        Parallel.For(
            0,
            plan.MasterBundles.Count,
            new ParallelOptions { MaxDegreeOfParallelism = maxParallelism },
            i =>
            {
                MasterBundleExportPlan masterBundle = plan.MasterBundles[i];
                ReportProgress(reportProgress, $"Trimming master bundle {i + 1}/{plan.MasterBundles.Count}: {masterBundle.SourceBundleName}");

                List<string> warnings = [];
                Directory.CreateDirectory(masterBundle.OutputBundleDirectoryPath);
                WriteMasterBundleConfig(masterBundle);
                reports[i] = BundleSubsetExporter.Export(masterBundle, warnings, enableStreamedPayloadCompaction);
                warningsByBundle[i] = warnings;
            });

        for (int i = 0; i < reports.Length; i++)
        {
            if (reports[i] != null)
            {
                bundleReports.Add(reports[i]!);
            }

            if (warningsByBundle[i] != null)
            {
                plan.Warnings.AddRange(warningsByBundle[i]);
            }
        }
    }

    private static void RewriteTextFiles(string rootPath, IReadOnlyDictionary<Guid, Guid> guidMap, IReadOnlyDictionary<string, string> bundleNameMap)
    {
        if (!Directory.Exists(rootPath))
        {
            return;
        }

        foreach (string filePath in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
        {
            if (!TextFileExtensions.Contains(Path.GetExtension(filePath)))
            {
                continue;
            }

            string contents = File.ReadAllText(filePath);
            string updatedContents = contents;

            foreach ((Guid sourceGuid, Guid targetGuid) in guidMap)
            {
                updatedContents = ReplaceOrdinalIgnoreCase(updatedContents, sourceGuid.ToString("N"), targetGuid.ToString("N"));
                updatedContents = ReplaceOrdinalIgnoreCase(updatedContents, sourceGuid.ToString("D"), targetGuid.ToString("D"));
            }

            foreach ((string sourceBundleName, string targetBundleName) in bundleNameMap)
            {
                updatedContents = ReplaceOrdinalIgnoreCase(updatedContents, sourceBundleName, targetBundleName);
            }

            if (!string.Equals(contents, updatedContents, StringComparison.Ordinal))
            {
                File.WriteAllText(filePath, updatedContents);
            }
        }
    }

    private static string BackupMapFiles(ModOptimizationPlan plan)
    {
        string backupRootPath = Path.Combine(plan.OutputRootPath, "_Original_Map_Backup");
        CopyMapFileToBackup(Path.Combine(plan.LevelPath, "Level", "Objects.dat"), backupRootPath, "Level", "Objects.dat");
        CopyMapFileToBackup(Path.Combine(plan.LevelPath, "Terrain", "Trees.dat"), backupRootPath, "Terrain", "Trees.dat");
        return backupRootPath;
    }

    private static void CopyMapFileToBackup(string sourceFilePath, string backupRootPath, params string[] relativeSegments)
    {
        if (!File.Exists(sourceFilePath))
        {
            return;
        }

        string destinationDirectoryPath = backupRootPath;
        for (int i = 0; i < relativeSegments.Length - 1; i++)
        {
            destinationDirectoryPath = Path.Combine(destinationDirectoryPath, relativeSegments[i]);
        }

        Directory.CreateDirectory(destinationDirectoryPath);
        string destinationFilePath = Path.Combine(destinationDirectoryPath, relativeSegments[^1]);

        using FileStream source = new(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using FileStream destination = new(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        source.CopyTo(destination);
        destination.Flush();
    }

    private static int PatchObjectsFile(string filePath, IReadOnlyDictionary<Guid, Guid> guidMap)
    {
        if (!File.Exists(filePath))
        {
            return 0;
        }

        int patchedCount = 0;
        byte[] patchedBytes;

        using (FileStream input = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (BinaryReader reader = new(input))
        using (MemoryStream output = new())
        using (BinaryWriter writer = new(output))
        {
            byte version = reader.ReadByte();
            if (version != LevelObjects.SAVEDATA_VERSION)
            {
                throw new InvalidOperationException(
                    $"Unsupported Objects.dat version {version}. Expected {LevelObjects.SAVEDATA_VERSION}.");
            }

            writer.Write(version);
            uint availableInstanceId = reader.ReadUInt32();
            writer.Write(availableInstanceId);

            for (int x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (int y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    ushort objectCount = reader.ReadUInt16();
                    writer.Write(objectCount);

                    for (int i = 0; i < objectCount; i++)
                    {
                        string entryLabel = $"Objects.dat region ({x}, {y}) object {i}";
                        CopyBytesExact(reader, writer, 12 + 12 + 12 + 2, entryLabel + " header");

                        Guid sourceGuid = ReadRiverGuid(reader, entryLabel + " asset GUID");
                        Guid targetGuid = guidMap.TryGetValue(sourceGuid, out Guid replacementGuid)
                            ? replacementGuid
                            : sourceGuid;

                        if (targetGuid != sourceGuid)
                        {
                            patchedCount++;
                        }

                        WriteRiverGuid(writer, targetGuid);
                        CopyBytesExact(reader, writer, 1 + 4, entryLabel + " placement and instance");

                        Guid customMaterialGuid = ReadRiverGuid(reader, entryLabel + " material override GUID");
                        WriteRiverGuid(writer, customMaterialGuid);
                        CopyBytesExact(reader, writer, 4 + 1, entryLabel + " material index and culling flag");
                    }
                }
            }

            patchedBytes = output.ToArray();
        }

        OverwriteFileShared(filePath, patchedBytes);
        return patchedCount;
    }

    private static int PatchTreesFile(string filePath, IReadOnlyDictionary<Guid, Guid> guidMap)
    {
        if (!File.Exists(filePath))
        {
            return 0;
        }

        int patchedCount = 0;
        byte[] patchedBytes;

        using (FileStream input = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (BinaryReader reader = new(input))
        using (MemoryStream output = new())
        using (BinaryWriter writer = new(output))
        {
            byte version = reader.ReadByte();
            if (version != LevelGround.SAVEDATA_TREES_VERSION)
            {
                throw new InvalidOperationException(
                    $"Unsupported Trees.dat version {version}. Expected {LevelGround.SAVEDATA_TREES_VERSION}.");
            }

            writer.Write(version);
            int count = reader.ReadInt32();
            writer.Write(count);

            for (int i = 0; i < count; i++)
            {
                string entryLabel = $"Trees.dat resource {i}";
                Guid sourceGuid = ReadRiverGuid(reader, entryLabel + " asset GUID");
                Guid targetGuid = guidMap.TryGetValue(sourceGuid, out Guid replacementGuid)
                    ? replacementGuid
                    : sourceGuid;

                if (targetGuid != sourceGuid)
                {
                    patchedCount++;
                }

                WriteRiverGuid(writer, targetGuid);
                CopyBytesExact(reader, writer, 12 + 12 + 12 + 1, entryLabel + " transform and generated flag");
            }

            patchedBytes = output.ToArray();
        }

        OverwriteFileShared(filePath, patchedBytes);
        return patchedCount;
    }

    private static string WriteReport(
        ModOptimizationPlan plan,
        IReadOnlyCollection<BundleExportReport> bundleReports,
        int patchedObjectCount,
        int patchedResourceCount,
        string mapBackupPath)
    {
        string reportPath = Path.Combine(plan.OutputRootPath, "EditorHelper2_Optimization_Report.txt");
        StringBuilder builder = new();
        builder.AppendLine($"Level: {plan.LevelName}");
        builder.AppendLine($"Exported assets: {plan.Assets.Count}");
        builder.AppendLine($"Root objects: {plan.RootObjectAssetCount}");
        builder.AppendLine($"Root resources: {plan.RootResourceAssetCount}");
        builder.AppendLine($"Root item spawn assets: {plan.RootItemSpawnAssetCount}");
        builder.AppendLine($"Root vehicle spawn assets: {plan.RootVehicleSpawnAssetCount}");
        builder.AppendLine($"Master bundles: {plan.MasterBundles.Count}");
        builder.AppendLine($"Master bundle parallel jobs: {plan.MaxParallelMasterBundleExports}");
        builder.AppendLine($"Save items and vehicles: {plan.SaveItemsAndVehicles}");
        builder.AppendLine($"Metadata-only bundle trim: {plan.UseMetadataOnlyBundleTrim}");
        builder.AppendLine($"Patched object instances: {patchedObjectCount}");
        builder.AppendLine($"Patched resource instances: {patchedResourceCount}");
        builder.AppendLine($"Map backup: {mapBackupPath}");
        builder.AppendLine();
        builder.AppendLine("Generated bundle remaps:");
        foreach ((string sourceBundle, string targetBundle) in plan.BundleNameMap.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"{sourceBundle} -> {targetBundle}");
        }

        if (bundleReports.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Bundle optimization details:");
            foreach (BundleExportReport bundleReport in bundleReports.OrderBy(report => report.SourceBundleName, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine($"{bundleReport.SourceBundleName}:");
                builder.AppendLine($"  Roots kept: {bundleReport.RootContainerCount}");
                builder.AppendLine($"  Root resolution fallback: {bundleReport.UsedFallbackRootResolution}");
                builder.AppendLine($"  Metadata-only fallback: {bundleReport.UsedMetadataOnlyFallback}");
                builder.AppendLine($"  Safety fallback: {bundleReport.UsedSafetyFallback}");
                builder.AppendLine($"  Serialized assets: {bundleReport.KeptSerializedAssetCount}/{bundleReport.OriginalSerializedAssetCount}");
                builder.AppendLine($"  Streamed payload bytes: {bundleReport.KeptStreamedBytes}/{bundleReport.OriginalStreamedBytes}");
                builder.AppendLine($"  Bundle bytes: {bundleReport.FinalBundleBytes}/{bundleReport.OriginalBundleBytes}");
            }
        }

        if (plan.Warnings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Warnings:");
            foreach (string warning in plan.Warnings)
            {
                builder.AppendLine($"- {warning}");
            }
        }

        File.WriteAllText(reportPath, builder.ToString());
        return reportPath;
    }

    private static void CopyDirectory(string sourceDirectoryPath, string destinationDirectoryPath)
    {
        Directory.CreateDirectory(destinationDirectoryPath);

        foreach (string filePath in Directory.EnumerateFiles(sourceDirectoryPath))
        {
            string destinationPath = Path.Combine(destinationDirectoryPath, Path.GetFileName(filePath));
            File.Copy(filePath, destinationPath, overwrite: true);
        }

        foreach (string childDirectoryPath in Directory.EnumerateDirectories(sourceDirectoryPath))
        {
            string destinationChildPath = Path.Combine(destinationDirectoryPath, Path.GetFileName(childDirectoryPath));
            CopyDirectory(childDirectoryPath, destinationChildPath);
        }
    }

    private static Guid ReadRiverGuid(BinaryReader reader, string context)
    {
        ushort byteCount = reader.ReadUInt16();
        if (byteCount != 16)
        {
            throw new InvalidOperationException($"{context} uses GUID byte length {byteCount}, expected 16.");
        }

        byte[] guidBytes = ReadExact(reader, byteCount, context);
        return new Guid(guidBytes);
    }

    private static void WriteRiverGuid(BinaryWriter writer, Guid guid)
    {
        byte[] guidBytes = guid.ToByteArray();
        writer.Write((ushort)guidBytes.Length);
        writer.Write(guidBytes);
    }

    private static void CopyBytesExact(BinaryReader reader, BinaryWriter writer, int count, string context)
    {
        writer.Write(ReadExact(reader, count, context));
    }

    private static byte[] ReadExact(BinaryReader reader, int count, string context)
    {
        byte[] bytes = reader.ReadBytes(count);
        if (bytes.Length != count)
        {
            throw new InvalidOperationException($"Unexpected end of file while reading {context}. Expected {count} bytes, got {bytes.Length}.");
        }

        return bytes;
    }

    private static void OverwriteFileShared(string filePath, byte[] bytes)
    {
        using FileStream output = new(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        output.Write(bytes, 0, bytes.Length);
        output.Flush();
    }

    private static void ReportProgress(Action<string>? reportProgress, string message)
    {
        reportProgress?.Invoke(message);
    }

    private static bool ShouldReportItemProgress(int index, int total)
    {
        return index == 1 || index == total || index % 10 == 0;
    }

    private static string ReplaceOrdinalIgnoreCase(string input, string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(oldValue) || string.Equals(oldValue, newValue, StringComparison.Ordinal))
        {
            return input;
        }

        int searchIndex = 0;
        int matchIndex = input.IndexOf(oldValue, searchIndex, StringComparison.OrdinalIgnoreCase);
        if (matchIndex < 0)
        {
            return input;
        }

        StringBuilder builder = new(input.Length + Math.Max(0, newValue.Length - oldValue.Length) * 4);
        while (matchIndex >= 0)
        {
            builder.Append(input, searchIndex, matchIndex - searchIndex);
            builder.Append(newValue);
            searchIndex = matchIndex + oldValue.Length;
            matchIndex = input.IndexOf(oldValue, searchIndex, StringComparison.OrdinalIgnoreCase);
        }

        builder.Append(input, searchIndex, input.Length - searchIndex);
        return builder.ToString();
    }
}