namespace EditorHelper.Models;

public class SerializableLevelObjectTag
{
    public uint InstanceID { get; set; }
    
    public string Tag { get; set; }

    public SerializableLevelObjectTag()
    {
    }
    
    public SerializableLevelObjectTag(uint instanceID, string tag)
    {
        this.InstanceID = instanceID;
        this.Tag = tag;
    }
}