namespace EditorHelper.Models;

public class LandscapeMaterialAssetExtension
{
    public bool useAutoHeight { get; private set; }
    public float minHeight { get; private set; }
    public float maxHeight { get; private set; }
    
    public void UpdateUseAutoHeight(bool value)
    {
        useAutoHeight = value;
    }
    
    public void UpdateMinHeight(float value)
    {
        minHeight = value;
    }
    
    public void UpdateMaxHeight(float value)
    {
        maxHeight = value;
    }

    public LandscapeMaterialAssetExtension(bool useAutoHeight, float minHeight, float maxHeight)
    {
        this.useAutoHeight = useAutoHeight;
        this.minHeight = minHeight;
        this.maxHeight = maxHeight;
    }
}