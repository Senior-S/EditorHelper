using System;

namespace EditorHelper2.API.Attributes;

/// <summary>
/// Required attribute for all extensions
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class EHExtensionAttribute(string extensionName, bool alwaysEnabled = false) : Attribute
{
    public string Name { get; } = extensionName;
    
    public bool AlwaysEnabled { get; } = alwaysEnabled;
}