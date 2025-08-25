namespace EditorHelper2.common.Types;

public class LevelObjectExtension(string tag)
{
    public string Tag { get; private set; } = tag;

    public void UpdateTag(string tag)
    {
        Tag = tag;
    }
}