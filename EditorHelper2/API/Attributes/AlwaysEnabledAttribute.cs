using System;

namespace EditorHelper2.API.Attributes;

/// <summary>
/// Any extension with this attribute won't be able to be disabled.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AlwaysEnabledAttribute : Attribute
{
}