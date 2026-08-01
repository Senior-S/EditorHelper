using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SDG.Unturned;

namespace EditorHelper2.Helpers;

/// <summary>
/// Loads and saves EditorHelper2 settings that belong to the current map.
/// </summary>
public static class MapEditorConfigHelper
{
    private const int CurrentVersion = 1;
    private const string FileName = "EditorHelper2.json";

    private static readonly JsonSerializer Serializer = JsonSerializer.CreateDefault();
    private static readonly Dictionary<string, ExtensionSettingsRegistration> Registrations = new(StringComparer.Ordinal);

    private static JObject _extensions = new();
    private static string? _filePath;
    private static bool _isInitialized;
    private static bool _canSave;

    /// <summary>
    /// Gets whether configuration is available for the current editor map.
    /// </summary>
    public static bool IsLoaded => _filePath != null && _canSave;

    /// <summary>
    /// Gets the path of the current map configuration file, or <see langword="null"/> outside the editor.
    /// </summary>
    public static string? FilePath => _filePath;

    internal static void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        Level.onLevelLoaded += OnLevelLoaded;
        Level.onLevelExited += OnLevelExited;
        _isInitialized = true;
    }

    internal static void Shutdown()
    {
        if (!_isInitialized)
        {
            return;
        }

        Save();
        Level.onLevelLoaded -= OnLevelLoaded;
        Level.onLevelExited -= OnLevelExited;
        ClearCurrentMap();
        Registrations.Clear();
        _isInitialized = false;
    }

    /// <summary>
    /// Gets an extension's settings from the current map configuration.
    /// </summary>
    /// <typeparam name="T">The serializable settings type.</typeparam>
    /// <param name="extensionId">A stable identifier owned by the extension.</param>
    /// <returns>The stored settings, or a new instance when the section has not been saved.</returns>
    public static T GetExtensionSettings<T>(string extensionId) where T : new()
    {
        ValidateExtensionId(extensionId);

        if (_extensions.TryGetValue(extensionId, StringComparison.Ordinal, out JToken? token))
        {
            try
            {
                T? value = token.ToObject<T>(Serializer);
                if (value != null)
                {
                    return value;
                }
            }
            catch (Exception ex)
            {
                UnturnedLog.exception(ex, $"[EditorHelper2] Failed to read map settings for extension '{extensionId}':");
            }
        }

        return new T();
    }

    /// <summary>
    /// Updates an extension's settings in memory. They are written alongside the next map save.
    /// </summary>
    /// <typeparam name="T">The serializable settings type.</typeparam>
    /// <param name="extensionId">A stable identifier owned by the extension.</param>
    /// <param name="settings">The settings to store.</param>
    /// <returns><see langword="true"/> when a current editor map accepted the settings.</returns>
    public static bool SetExtensionSettings<T>(string extensionId, T settings)
    {
        ValidateExtensionId(extensionId);
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (!IsLoaded)
        {
            return false;
        }

        _extensions[extensionId] = JToken.FromObject(settings, Serializer);
        return true;
    }

    /// <summary>
    /// Registers an extension's map settings. Registered settings are applied after loading and captured during map saves.
    /// </summary>
    /// <typeparam name="T">The serializable settings type.</typeparam>
    /// <param name="extensionId">A stable identifier owned by the extension.</param>
    /// <param name="captureSettings">Captures the extension's current settings.</param>
    /// <param name="applySettings">Applies settings loaded from the map.</param>
    public static void RegisterExtensionSettings<T>(
        string extensionId,
        Func<T> captureSettings,
        Action<T> applySettings)
    {
        ValidateExtensionId(extensionId);
        if (captureSettings == null)
        {
            throw new ArgumentNullException(nameof(captureSettings));
        }

        if (applySettings == null)
        {
            throw new ArgumentNullException(nameof(applySettings));
        }

        ExtensionSettingsRegistration registration = new(
            () => JToken.FromObject(captureSettings()!, Serializer),
            token =>
            {
                T? settings = token.ToObject<T>(Serializer);
                if (settings != null)
                {
                    applySettings(settings);
                }
            });

        Registrations[extensionId] = registration;
        if (IsLoaded && _extensions.TryGetValue(extensionId, StringComparison.Ordinal, out JToken? token))
        {
            ApplyRegistration(extensionId, registration, token);
        }
    }

    /// <summary>
    /// Stops capturing and applying an extension's map settings.
    /// </summary>
    /// <param name="extensionId">The extension identifier passed during registration.</param>
    public static void UnregisterExtensionSettings(string extensionId)
    {
        ValidateExtensionId(extensionId);
        Registrations.Remove(extensionId);
    }

    /// <summary>
    /// Removes an extension's settings from the current map configuration.
    /// </summary>
    /// <param name="extensionId">A stable identifier owned by the extension.</param>
    /// <returns><see langword="true"/> when a stored section was removed.</returns>
    public static bool RemoveExtensionSettings(string extensionId)
    {
        ValidateExtensionId(extensionId);
        if (!IsLoaded || !_extensions.Remove(extensionId))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Writes pending settings to the current map's Editor folder.
    /// </summary>
    public static void Save()
    {
        if (!IsLoaded && !TryLoadCurrentMap(applySettings: false))
        {
            return;
        }

        if (_filePath == null)
        {
            return;
        }

        CaptureRegisteredSettings();

        try
        {
            string? directoryPath = Path.GetDirectoryName(_filePath);
            if (string.IsNullOrEmpty(directoryPath))
            {
                throw new InvalidOperationException("The map configuration path has no parent directory.");
            }

            Directory.CreateDirectory(directoryPath);

            JObject root = new()
            {
                ["Version"] = CurrentVersion,
                ["Extensions"] = _extensions
            };

            string json = JsonConvert.SerializeObject(root, Formatting.Indented);
            File.WriteAllText(_filePath, json);
            UnturnedLog.info($"[EditorHelper2] Saved map configuration to '{_filePath}'.");
        }
        catch (Exception ex)
        {
            UnturnedLog.exception(ex, "[EditorHelper2] Failed to save the current map configuration:");
        }
    }

    private static void OnLevelLoaded(int level)
    {
        TryLoadCurrentMap(applySettings: true);
    }

    private static bool TryLoadCurrentMap(bool applySettings)
    {
        if (!Level.isEditor || Level.info == null)
        {
            return false;
        }

        string filePath = Path.Combine(Level.info.path, "Editor", FileName);
        if (string.Equals(_filePath, filePath, StringComparison.OrdinalIgnoreCase))
        {
            if (_canSave && applySettings)
            {
                ApplyRegisteredSettings();
            }

            return _canSave;
        }

        ClearCurrentMap();
        _filePath = filePath;
        _canSave = true;

        if (File.Exists(_filePath))
        {
            try
            {
                JObject root = JObject.Parse(File.ReadAllText(_filePath));
                _extensions = root["Extensions"] as JObject ?? new JObject();
            }
            catch (Exception ex)
            {
                _canSave = false;
                UnturnedLog.exception(
                    ex,
                    $"[EditorHelper2] Failed to load map configuration '{_filePath}'. The file will be left unchanged:");
            }
        }

        if (_canSave && applySettings)
        {
            ApplyRegisteredSettings();
        }

        return _canSave;
    }

    private static void OnLevelExited()
    {
        ClearCurrentMap();
    }

    private static void ClearCurrentMap()
    {
        _extensions = new JObject();
        _filePath = null;
        _canSave = false;
    }

    private static void ApplyRegisteredSettings()
    {
        foreach (KeyValuePair<string, ExtensionSettingsRegistration> pair in Registrations)
        {
            if (_extensions.TryGetValue(pair.Key, StringComparison.Ordinal, out JToken? token))
            {
                ApplyRegistration(pair.Key, pair.Value, token);
            }
        }
    }

    private static void ApplyRegistration(string extensionId, ExtensionSettingsRegistration registration, JToken token)
    {
        try
        {
            registration.Apply(token);
        }
        catch (Exception ex)
        {
            UnturnedLog.exception(ex, $"[EditorHelper2] Failed to apply map settings for extension '{extensionId}':");
        }
    }

    private static void CaptureRegisteredSettings()
    {
        foreach (KeyValuePair<string, ExtensionSettingsRegistration> pair in Registrations)
        {
            try
            {
                JToken captured = pair.Value.Capture();
                if (_extensions.TryGetValue(pair.Key, StringComparison.Ordinal, out JToken? existing)
                    && JToken.DeepEquals(existing, captured))
                {
                    continue;
                }

                _extensions[pair.Key] = captured;
            }
            catch (Exception ex)
            {
                UnturnedLog.exception(ex, $"[EditorHelper2] Failed to capture map settings for extension '{pair.Key}':");
            }
        }
    }

    private static void ValidateExtensionId(string extensionId)
    {
        if (string.IsNullOrWhiteSpace(extensionId))
        {
            throw new ArgumentException("Extension identifier cannot be empty.", nameof(extensionId));
        }
    }

    private sealed class ExtensionSettingsRegistration(Func<JToken> capture, Action<JToken> apply)
    {
        public Func<JToken> Capture { get; } = capture;
        public Action<JToken> Apply { get; } = apply;
    }
}
