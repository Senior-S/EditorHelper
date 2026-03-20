using System;
using System.Collections.Generic;

namespace EditorHelper2.Optimization;

internal sealed class ModOptimizationPlan
{
    public string OutputRootPath { get; set; } = string.Empty;
    public string LevelPath { get; set; } = string.Empty;
    public string LevelName { get; set; } = string.Empty;
    public int RootObjectAssetCount { get; set; }
    public int RootResourceAssetCount { get; set; }
    public List<OptimizedAssetRecord> Assets { get; } = [];
    public List<MasterBundleExportPlan> MasterBundles { get; } = [];
    public Dictionary<Guid, Guid> GuidMap { get; } = [];
    public Dictionary<string, string> BundleNameMap { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Warnings { get; } = [];
}

internal sealed class OptimizedAssetRecord
{
    public Guid SourceGuid { get; set; }
    public Guid TargetGuid { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public string SourceDatFilePath { get; set; } = string.Empty;
    public string SourceFolderPath { get; set; } = string.Empty;
    public string OutputFolderPath { get; set; } = string.Empty;
    public bool UsesMasterBundle { get; set; }
    public string RelativeFolderPath { get; set; } = string.Empty;
    public string? SourceBundleDirectoryPath { get; set; }
    public string? SourceBundleFilePath { get; set; }
    public string? SourceBundleName { get; set; }
    public string? SourceBundleAssetPrefix { get; set; }
    public string? OutputBundleDirectoryPath { get; set; }
    public string? OutputBundleFileName { get; set; }
}

internal sealed class MasterBundleExportPlan
{
    public string SourceBundleDirectoryPath { get; set; } = string.Empty;
    public string SourceBundleFilePath { get; set; } = string.Empty;
    public string SourceBundleName { get; set; } = string.Empty;
    public string SourceAssetPrefix { get; set; } = string.Empty;
    public string OutputBundleDirectoryPath { get; set; } = string.Empty;
    public string OutputBundleFilePath { get; set; } = string.Empty;
    public string OutputBundleName { get; set; } = string.Empty;
    public int BundleVersion { get; set; }
    public List<string> IncludedRelativeFolders { get; } = [];
}

internal sealed class ModOptimizationResult
{
    public int ExportedAssetCount { get; set; }
    public int MasterBundleCount { get; set; }
    public int PatchedObjectCount { get; set; }
    public int PatchedResourceCount { get; set; }
    public string ReportPath { get; set; } = string.Empty;
    public string MapBackupPath { get; set; } = string.Empty;
    public List<string> Warnings { get; } = [];
}