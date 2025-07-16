using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EditorHelper.Models;
using Newtonsoft.Json;
using SDG.Unturned;

namespace EditorHelper.Schematics;

// 7/14/25 SeniorS: I would like at some point to move all this complex features into a "plugin" system
// Atm I'll try to keep features as organized as possible so if in a future I implement a "plugin" system
// I can migrate as fast as possible and without requiring to rewrite almost everything from scratch  
public class SchematicsManager
{
    private static string _schematicsFolder;
    
    //public List<string> SchematicsFiles = [];
    public List<SchematicModel> Schematics = [];
    
    public SchematicsManager()
    {
        _schematicsFolder = Path.Combine(Environment.CurrentDirectory, "Schematics");

        if (!Directory.Exists(_schematicsFolder))
        {
            Directory.CreateDirectory(_schematicsFolder);
        }

        ReloadSchematics();
    }

    public SchematicModel TryLoadSchematic(int index, string schematicSearchValue)
    {
        if (Schematics.Count < index)
        {
            // This should only be caused by a wrong use of an external actor so it doesn't require giving feedback back to the user
            return null;
        }

        SchematicModel schematic = schematicSearchValue.Length > 0 
            ? Schematics.Where(c => c.Name.ToLower().Contains(schematicSearchValue)).ElementAt(index) 
            : Schematics[index];

        index = Schematics.IndexOf(schematic);
        
        if (!schematic.Name.EndsWith(".json"))
        {
            return schematic;
        }
        
        string text = File.ReadAllText(Path.Combine(_schematicsFolder, schematic.Name));
        JsonWithMetadata deserializedJson = JsonConvert.DeserializeObject<JsonWithMetadata>(text);
        if (deserializedJson == null)
        {
            EditorHelper.Instance.EditorManager.DisplayAlert("Oops, something went wrong.");
            return null;
        }

        schematic = deserializedJson.Schematic;

        Schematics[index] = schematic;
        return schematic;
    }
    
    public void ReloadSchematics()
    {
        string[] files = Directory.GetFiles(_schematicsFolder, "*.json");

        var owners = files.Select(f => new
        {
            File = f,
            Owner = GetOwnerFromJsonAsync(f)
        });

        Schematics = owners.Select(c => new SchematicModel(c.File.Replace(_schematicsFolder, "").Substring(1), c.Owner)).ToList();
    }

    public void SaveSchematic(string schematicName)
    {
        string formattedName = schematicName + ".json";

        if (Schematics.Any(c => c.Name == formattedName || c.Name == schematicName))
        {
            EditorHelper.Instance.EditorManager.DisplayAlert("Error! An schematic with this name already exists!");
            return;
        }
        
        List<SerializableEditorCopy> objects = [];
        foreach (EditorCopy copy in EditorObjects.copies)
        {
            objects.Add(new SerializableEditorCopy(copy.position, copy.rotation, copy.scale, copy.objectAsset?.GUID ?? Guid.Empty, copy.itemAsset?.GUID ?? Guid.Empty));
        }

        SchematicModel schematic = new(schematicName, Provider.clientName)
        {
            Objects = objects
        };
        var metadata = new
        {
            owner = Provider.clientName
        };
        
        string? json = JsonConvert.SerializeObject(new JsonWithMetadata() { _metadata = metadata, Schematic = schematic });
        if (json == null)
        {
            EditorHelper.Instance.EditorManager.DisplayAlert("Oops, something went wrong.");
            return;
        }
        
        using StreamWriter writer =  File.CreateText(Path.Combine(_schematicsFolder, $"{schematicName}.json"));
        writer.Write(json);
        writer.Flush();
        writer.Close();
        
        Schematics.Add(schematic);
    }
    
    private string GetOwnerFromJsonAsync(string filePath)
    {
        using FileStream stream = File.OpenRead(filePath);
        using StreamReader reader = new StreamReader(stream);
    
        char[] buffer = new char[1024];
        reader.Read(buffer, 0, buffer.Length);
    
        string partial = new(buffer);
        Match metadataMatch = Regex.Match(partial, @"""owner""\s*:\s*""([^""]+)""");
        return metadataMatch.Success ? metadataMatch.Groups[1].Value : null;
    }
}