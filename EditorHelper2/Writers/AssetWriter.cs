using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SDG.Framework.Foliage;
using SDG.Unturned;

namespace EditorHelper.Writers;

public class AssetWriter
{
    public static void SaveFoliageInfoCollectionAsset(FoliageInfoCollectionAsset asset)
    {
        if (asset == null || string.IsNullOrEmpty(asset.absoluteOriginFilePath))
            return;

        string filePath = asset.absoluteOriginFilePath;
        string fileText = File.ReadAllText(filePath);

        // Regex to find the "Foliage" block
        Regex foliageRegex = new Regex(
            @"(?<indent>\t*)""Foliage""\s*\[(?:.|\n)*?\]",
            RegexOptions.Multiline);

        // Foliage block
        StringBuilder assetsBlock = new StringBuilder();
        assetsBlock.AppendLine("\t\"Foliage\"");
        assetsBlock.AppendLine("\t[");

        foreach (FoliageInfoCollectionAsset.FoliageInfoCollectionElement element in asset.elements)
        {
            string assetName = element.asset.Find()?.name ?? element.asset.GUID.ToString();
            
            assetsBlock.AppendLine("\t\t{");
            assetsBlock.AppendLine("\t\t\t\"Asset\"");
            assetsBlock.AppendLine("\t\t\t{");
            assetsBlock.AppendLine($"\t\t\t// {assetName}");
            assetsBlock.AppendLine($"\t\t\t\t\"GUID\" \"{element.asset.GUID}\"");
            assetsBlock.AppendLine("\t\t\t}");
            assetsBlock.AppendLine($"\t\t\t\"Weight\" \"{element.weight}\"");
            assetsBlock.AppendLine("\t\t},");
        }

        assetsBlock.AppendLine("\t]");

        // Replace the old foliage block with the new one
        string newFileText = foliageRegex.Replace(fileText, assetsBlock.ToString());

        File.WriteAllText(filePath, newFileText);
    }

    public static void CreateEmptyFoliageInfoCollectionAssetFile(string name)
    {
        StringBuilder sb = new StringBuilder();
        Guid guid = Guid.NewGuid();

        // Metadata block
        sb.AppendLine("\"Metadata\"");
        sb.AppendLine("{");
        sb.AppendLine($"\t\"GUID\" \"{guid:N}\"");
        sb.AppendLine("\t\"Type\" \"SDG.Framework.Foliage.FoliageInfoCollectionAsset, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null\"");
        sb.AppendLine("}");

        // Asset block
        sb.AppendLine("\"Asset\"");
        sb.AppendLine("{");
        sb.AppendLine($"\t\"ID\" \"0\"");
        sb.AppendLine("\t\"Foliage\"");
        sb.AppendLine("\t[");
        sb.AppendLine("\t]");
        sb.AppendLine("}");

        string dir = Level.info.path + "/Bundles/Assets/Landscapes/Collections/";
        string filePath = dir + name + ".asset";

        Directory.CreateDirectory(dir);
        File.WriteAllText(filePath, sb.ToString());
        
        Assets.reload(Path.GetDirectoryName(dir));
    }

    public static void SaveFoliageInfoAssetDensity(FoliageInfoAsset asset)
    {
        if (asset == null || string.IsNullOrEmpty(asset.absoluteOriginFilePath) || !File.Exists(asset.absoluteOriginFilePath))
            return;

        string filePath = asset.absoluteOriginFilePath;
        string fileText = File.ReadAllText(filePath);

        Regex densityRegex = new Regex(
            @"^(\s*""?Density""?\s+)(""[^""]*""|[^\s]+)",
            RegexOptions.Multiline);

        string newFileText = densityRegex.Replace(
            fileText,
            m => $"{m.Groups[1].Value}\"{asset.density}\"",
            1
        );

        File.WriteAllText(filePath, newFileText);
    }
}