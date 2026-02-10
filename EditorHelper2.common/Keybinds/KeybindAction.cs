namespace EditorHelper2.common.Keybinds;

public sealed class KeybindAction
{
    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public string Category { get; }
    public Keybind Default { get; }
    public Keybind Current { get; set; }

    public KeybindAction(string id, string displayName, string description, string category, Keybind defaultBinding)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        Category = category;
        Default = defaultBinding;
        Current = defaultBinding;
    }
}
