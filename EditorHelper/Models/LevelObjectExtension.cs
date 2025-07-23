namespace EditorHelper.Models;

public class LevelObjectExtension
{
    public string Tag { get; private set; }

    public void UpdateTag(string tag)
    {
        Tag = tag;
    }

    public LevelObjectExtension(string tag)
    {
        Tag = tag;
    }
}