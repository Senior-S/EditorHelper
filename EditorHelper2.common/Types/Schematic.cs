using System.Collections.Generic;

namespace EditorHelper2.common.Types;

public class Schematic
{
    public string Name { get; set; }
    
    public string Author { get; set; }
    
    public List<SerializableEditorCopy> Objects { get; set; }

    public Schematic()
    {
    }

    public Schematic(string name, string author)
    {
        Name = name;
        Author = author;
        Objects = [];
    }
}