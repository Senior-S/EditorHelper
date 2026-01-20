using System;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Table;
using SDG.Unturned;

namespace EditorHelper2.common.Helpers;

public static class ExcelExporter
{
    public static string Export()
    {
        ExcelPackage.License.SetNonCommercialPersonal("seniors.editorhelper2");
        
        string mapName = SDG.Unturned.Level.info != null ? SDG.Unturned.Level.info.name : "UnknownMap";
        string fileName = $"{mapName}_Assets_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string folderPath = Path.Combine(desktopPath, "Unturned_Exports");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, fileName);

        using ExcelPackage package = new();
        CreateObjectsSheet(package);
        CreateBuildablesSheet(package);
        CreateItemSpawnsSheet(package);
        CreateVehicleSpawnsSheet(package);
        CreateAnimalSpawnsSheet(package);
        CreateZombiesSheet(package);
        CreateRoadsSheet(package);

        package.SaveAs(new FileInfo(filePath));

        return filePath;
    }

    private static void CreateObjectsSheet(ExcelPackage package)
    {
        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Objects");
        worksheet.Cells.Style.Font.Name = "Arial";

        worksheet.Cells[1, 1].Value = "GUID";
        worksheet.Cells[1, 2].Value = "Asset Name";
        worksheet.Cells[1, 3].Value = "Legacy ID";
        worksheet.Cells[1, 4].Value = "Object Type";
        worksheet.Cells[1, 5].Value = "Count";

        Dictionary<Guid, (ObjectAsset? asset, Guid instanceGuid, int count)> objectCounts = new();

        for (byte x = 0; x < LevelObjects.objects.GetLength(0); x++)
        {
            for (byte y = 0; y < LevelObjects.objects.GetLength(1); y++)
            {
                List<LevelObject> regionObjects = LevelObjects.objects[x, y];
                if (regionObjects == null) continue;

                foreach (LevelObject obj in regionObjects)
                {
                    if (obj == null) continue;

                    Guid key = obj.asset?.GUID ?? obj.GUID;

                    if (objectCounts.TryGetValue(key, out (ObjectAsset? asset, Guid instanceGuid, int count) existing))
                        objectCounts[key] = (existing.asset ?? obj.asset, existing.instanceGuid, existing.count + 1);
                    else
                        objectCounts[key] = (obj.asset, obj.GUID, 1);
                }
            }
        }

        int row = 2;
        foreach (KeyValuePair<Guid, (ObjectAsset? asset, Guid instanceGuid, int count)> kvp in objectCounts)
        {
            ObjectAsset? asset = kvp.Value.asset;

            if (asset != null)
            {
                worksheet.Cells[row, 1].Value = asset.GUID.ToString();
                worksheet.Cells[row, 2].Value = asset.objectName;
                worksheet.Cells[row, 3].Value = asset.id;
                worksheet.Cells[row, 4].Value = asset.type;
            }
            else
            {
                worksheet.Cells[row, 1].Value = kvp.Value.instanceGuid.ToString();
                worksheet.Cells[row, 2].Value = "MISSING ASSET";
                worksheet.Cells[row, 3].Value = "0";
                worksheet.Cells[row, 4].Value = "UNKNOWN";
            }
            worksheet.Cells[row, 5].Value = kvp.Value.count;
            row++;
        }

        if (row > 2)
        {
            ExcelRange? tableRange = worksheet.Cells[1, 1, row - 1, 5];
            ExcelTable? table = worksheet.Tables.Add(tableRange, "Objects");
            table.TableStyle = TableStyles.Medium2;
        }

        worksheet.Cells.AutoFitColumns();
    }

    private static void CreateBuildablesSheet(ExcelPackage package)
    {
        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Buildables");
        worksheet.Cells.Style.Font.Name = "Arial";

        worksheet.Cells[1, 1].Value = "GUID";
        worksheet.Cells[1, 2].Value = "Asset Name";
        worksheet.Cells[1, 3].Value = "Legacy ID";

        int row = 2;
        // LevelObjects.buildables is List<LevelBuildableObject>[,]
        for (byte x = 0; x < LevelObjects.buildables.GetLength(0); x++)
        {
            for (byte y = 0; y < LevelObjects.buildables.GetLength(1); y++)
            {
                List<LevelBuildableObject> regionBuildables = LevelObjects.buildables[x, y];
                if (regionBuildables == null) continue;

                foreach (LevelBuildableObject obj in regionBuildables)
                {
                    if (obj == null) continue;

                    ItemAsset? asset = obj.asset;

                    if (asset != null)
                    {
                        worksheet.Cells[row, 1].Value = asset.GUID.ToString();
                        worksheet.Cells[row, 2].Value = asset.itemName;
                        worksheet.Cells[row, 3].Value = asset.id;
                    }
                    else
                    {
                        // LevelBuildableObject does not have a GUID property for the instance
                        worksheet.Cells[row, 1].Value = "N/A";
                        worksheet.Cells[row, 2].Value = "MISSING ASSET";
                        worksheet.Cells[row, 3].Value = obj.id;
                    }
                    row++;
                }
            }
        }

        if (row > 2)
        {
            ExcelRange? tableRange = worksheet.Cells[1, 1, row - 1, 3];
            ExcelTable? table = worksheet.Tables.Add(tableRange, "Buildables");
            table.TableStyle = TableStyles.Medium2;
        }

        worksheet.Cells.AutoFitColumns();
    }

    private static void CreateItemSpawnsSheet(ExcelPackage package)
    {
        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Items");
        worksheet.Cells.Style.Font.Name = "Arial";
        
        worksheet.Cells[1, 1].Value = "Table ID";
        worksheet.Cells[1, 2].Value = "Name";
        worksheet.Cells[1, 3].Value = "Tier Name";
        worksheet.Cells[1, 4].Value = "Tier Chance";
        worksheet.Cells[1, 5].Value = "Item ID";
        worksheet.Cells[1, 6].Value = "Item Name";
        worksheet.Cells[1, 7].Value = "Rarity";

        int row = 2;
        foreach (ItemTable? table in LevelItems.tables)
        {
            if (table == null) continue;
            
            foreach (ItemTier? tier in table.tiers)
            {
                if (tier == null) continue;

                string tierName = tier.name;
                float chance = tier.chance;

                foreach (ItemSpawn? item in tier.table)
                {
                    if (item == null) continue;

                    worksheet.Cells[row, 1].Value = table.tableID;
                    worksheet.Cells[row, 2].Value = table.name;
                    worksheet.Cells[row, 3].Value = tierName;
                    worksheet.Cells[row, 4].Value = chance;
                    worksheet.Cells[row, 5].Value = item.item;

                    ItemAsset? asset = SDG.Unturned.Assets.find(EAssetType.ITEM, item.item) as ItemAsset;
                    worksheet.Cells[row, 6].Value = asset != null ? asset.itemName : "Unknown Item";
                    worksheet.Cells[row, 7].Value = asset != null ? asset.rarity.ToString() : "Unknown";

                    row++;
                }
            }
        }

        if (row > 2)
        {
            ExcelRange? tableRange = worksheet.Cells[1, 1, row - 1, 7];
            ExcelTable? table = worksheet.Tables.Add(tableRange, "Items");
            table.TableStyle = TableStyles.Medium2;
        }

        worksheet.Cells.AutoFitColumns();
    }

    private static void CreateVehicleSpawnsSheet(ExcelPackage package)
    {
        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Vehicles");
        worksheet.Cells.Style.Font.Name = "Arial";
        worksheet.Cells[1, 1].Value = "Table ID";
        worksheet.Cells[1, 2].Value = "Name";
        worksheet.Cells[1, 3].Value = "Tier Name";
        worksheet.Cells[1, 4].Value = "Tier Chance";
        worksheet.Cells[1, 5].Value = "Vehicle ID";
        worksheet.Cells[1, 6].Value = "Vehicle Name";

        int row = 2;
        foreach (VehicleTable? table in LevelVehicles.tables)
        {
            if (table == null) continue;

            foreach (VehicleTier? tier in table.tiers)
            {
                if (tier == null) continue;

                string tierName = tier.name;
                float chance = tier.chance;

                foreach (VehicleSpawn? vehicle in tier.table)
                {
                    if (vehicle == null) continue;

                    worksheet.Cells[row, 1].Value = table.tableID;
                    worksheet.Cells[row, 2].Value = table.name;
                    worksheet.Cells[row, 3].Value = tierName;
                    worksheet.Cells[row, 4].Value = chance;
                    worksheet.Cells[row, 5].Value = vehicle.vehicle;

                    Asset? asset = SDG.Unturned.Assets.find(EAssetType.VEHICLE, vehicle.vehicle);
                    VehicleAsset? vehicleAsset = asset switch
                    {
                        VehicleRedirectorAsset redirectorAsset => redirectorAsset.TargetVehicle.Find(),
                        VehicleAsset asset1 => asset1,
                        _ => null
                    };

                    worksheet.Cells[row, 6].Value = vehicleAsset != null ? vehicleAsset.vehicleName : "Unknown Vehicle";

                    row++;
                }
            }
        }

        if (row > 2)
        {
            ExcelRange? tableRange = worksheet.Cells[1, 1, row - 1, 6];
            ExcelTable? table = worksheet.Tables.Add(tableRange, "Vehicles");
            table.TableStyle = TableStyles.Medium2;
        }

        worksheet.Cells.AutoFitColumns();
    }

    private static void CreateAnimalSpawnsSheet(ExcelPackage package)
    {
        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Animals");
        worksheet.Cells.Style.Font.Name = "Arial";
        worksheet.Cells[1, 1].Value = "Table ID";
        worksheet.Cells[1, 2].Value = "Name";
        worksheet.Cells[1, 3].Value = "Tier Name";
        worksheet.Cells[1, 4].Value = "Tier Chance";
        worksheet.Cells[1, 5].Value = "Animal ID";
        worksheet.Cells[1, 6].Value = "Animal Name";

        int row = 2;
        foreach (AnimalTable? table in LevelAnimals.tables)
        {
            if (table == null) continue;

            foreach (AnimalTier? tier in table.tiers)
            {
                if (tier == null) continue;

                string tierName = tier.name;
                float chance = tier.chance;

                foreach (AnimalSpawn? animal in tier.table)
                {
                    if (animal == null) continue;

                    worksheet.Cells[row, 1].Value = table.tableID;
                    worksheet.Cells[row, 2].Value = table.name;
                    worksheet.Cells[row, 3].Value = tierName;
                    worksheet.Cells[row, 4].Value = chance;
                    worksheet.Cells[row, 5].Value = animal.animal;

                    AnimalAsset? asset = SDG.Unturned.Assets.find(EAssetType.ANIMAL, animal.animal) as AnimalAsset;
                    worksheet.Cells[row, 6].Value = asset != null ? asset.animalName : "Unknown Animal";

                    row++;
                }
            }
        }

        if (row > 2)
        {
            ExcelRange? tableRange = worksheet.Cells[1, 1, row - 1, 6];
            ExcelTable? table = worksheet.Tables.Add(tableRange, "Animals");
            table.TableStyle = TableStyles.Medium2;
        }

        worksheet.Cells.AutoFitColumns();
    }

    private static void CreateZombiesSheet(ExcelPackage package)
    {
        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Zombies");
        worksheet.Cells.Style.Font.Name = "Arial";
        worksheet.Cells[1, 1].Value = "Table ID";
        worksheet.Cells[1, 2].Value = "Name";
        worksheet.Cells[1, 3].Value = "Is Mega";
        worksheet.Cells[1, 4].Value = "Health";
        worksheet.Cells[1, 5].Value = "Damage";
        worksheet.Cells[1, 6].Value = "XP";
        worksheet.Cells[1, 7].Value = "Slot 1";
        worksheet.Cells[1, 8].Value = "Slot 2";
        worksheet.Cells[1, 9].Value = "Slot 3";
        worksheet.Cells[1, 10].Value = "Slot 4";

        int row = 2;
        foreach (ZombieTable? table in LevelZombies.tables)
        {
            if (table == null) continue;

            worksheet.Cells[row, 1].Value = table.tableUniqueId;
            worksheet.Cells[row, 2].Value = table.name;
            worksheet.Cells[row, 3].Value = table.isMega;
            worksheet.Cells[row, 4].Value = table.health;
            worksheet.Cells[row, 5].Value = table.damage;
            worksheet.Cells[row, 6].Value = table.xp;

            for (int i = 0; i < 4 && i < table.slots.Length; i++)
            {
                ZombieSlot? slot = table.slots[i];
                if (slot != null && slot.table != null && slot.table.Count > 0)
                {
                    string summary = "";
                    foreach(ZombieCloth? item in slot.table)
                    {
                         ItemAsset? asset = SDG.Unturned.Assets.find(EAssetType.ITEM, item.item) as ItemAsset;
                         string name = asset != null ? asset.itemName : item.item.ToString();
                         summary += $"{name} ({item.item}), ";
                    }
                    if (summary.Length > 2) summary = summary[..^2];

                    worksheet.Cells[row, 7 + i].Value = summary;
                }
            }

            row++;
        }

        if (row > 2)
        {
            ExcelRange? tableRange = worksheet.Cells[1, 1, row - 1, 10];
            ExcelTable? table = worksheet.Tables.Add(tableRange, "Zombies");
            table.TableStyle = TableStyles.Medium2;
        }

        worksheet.Cells.AutoFitColumns();
    }

    private static void CreateRoadsSheet(ExcelPackage package)
    {
        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Roads");
        worksheet.Cells.Style.Font.Name = "Arial";
        worksheet.Cells[1, 1].Value = "Road Index";
        worksheet.Cells[1, 2].Value = "Material/Asset";
        worksheet.Cells[1, 3].Value = "Is Loop";
        worksheet.Cells[1, 4].Value = "Point Count";
        
        List<Road> roads = LevelRoads.roads;
        for (int i = 0; i < roads.Count; i++)
        {
            Road road = roads[i];
            int row = i + 2;

            worksheet.Cells[row, 1].Value = i;

            string materialName = "Unknown";
            if (road._roadAsset != null)
                materialName = road._roadAsset.name; // Use generic Asset name
            else if (road.material < LevelRoads.materials.Length)
                materialName = "Material_" + road.material;

            worksheet.Cells[row, 2].Value = materialName;
            worksheet.Cells[row, 3].Value = road.isLoop;
            worksheet.Cells[row, 4].Value = road.paths.Count;
        }

        if (roads.Count > 0)
        {
            ExcelRange? tableRange = worksheet.Cells[1, 1, roads.Count + 1, 4];
            ExcelTable? table = worksheet.Tables.Add(tableRange, "Roads");
            table.TableStyle = TableStyles.Medium2;
        }

        worksheet.Cells.AutoFitColumns();
    }
}
