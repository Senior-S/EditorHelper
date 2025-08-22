using System;

namespace EditorHelper2.common.API.Interfaces;

public interface IExtension : IDisposable
{
    /// <summary>
    /// UI should always be added here and this method must be called in the constructor.
    /// </summary>
    public void Initialize();
}