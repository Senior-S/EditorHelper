using System.Collections.Generic;

namespace EditorHelper.Models;

public class SchematicModel
{
    public string Name { get; set; }
    
    public string Author { get; set; }
    
    public List<SerializableEditorCopy> Objects { get; set; }

    public SchematicModel()
    {
    }

    public SchematicModel(string name, string author)
    {
        Name = name;
        Author = author;
        Objects = [];
    }
}