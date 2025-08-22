using System;

namespace EditorHelper2.common.API.Attributes;

/// <summary>
/// Required attribute for all extensions
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class EHExtensionAttribute(string extensionName, string author, bool alwaysEnabled = false) : Attribute
{
    public string Name { get; } = extensionName;

    public string Author { get; } = author;
    
    public bool AlwaysEnabled { get; } = alwaysEnabled;
}