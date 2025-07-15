namespace EditorHelper.Models;

public class JsonWithMetadata
{
    public object _metadata { get; set; }
    
    public SchematicModel Schematic { get; set; }
}