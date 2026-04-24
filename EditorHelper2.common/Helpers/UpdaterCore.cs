using EditorHelper2.common.Types;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Action = System.Action;

namespace EditorHelper2.common.Helpers;

public enum EVersionStatus
{
    Loading,
    Unknown,
    Outdated,
    Latest
}

public enum EGetLatestVersionResult
{
    Loading,
    Failed,
    Success
}

// Originally based on https://github.com/ShimmyMySherbet/ShimmysAdminTools/blob/master/ShimmysAdminTools/Components/UpdaterCore.cs
public static class UpdaterCore
{
    private const string GlobalConfigURL = "https://gist.sshost.club/seniors/b855c981b37940c69fc6e3c8ea63f032/raw/HEAD/gistfile1.ini";
    private static readonly HttpClient _client = new()
    {
        Timeout = TimeSpan.FromSeconds(2.5)
    };
    private static readonly SemaphoreSlim _requestLock = new SemaphoreSlim(1, 1);

    private static IniFile? _globalConfig = null;
    public static bool ConfigLoaded()
    {
        if (_requestLock.Wait(0))
        {
            _requestLock.Release();
            return true;
        }
        return false;
    }
    public static event Action? OnConfigLoaded;

    private static readonly Version CurrentVersion = typeof(UpdaterCore).Assembly.GetName().Version;

    public static async Task LoadConfigAsync(CancellationToken cancellationToken = default)
    {
        await _requestLock.WaitAsync(cancellationToken);
        try
        {
            _globalConfig = null;

            using (var response = await _client.GetAsync(GlobalConfigURL, cancellationToken))
            {
                if (response.IsSuccessStatusCode)
                {
                    string config = await response.Content.ReadAsStringAsync();
                    _globalConfig = new IniFile(config);
                }
            }

            // This should be Awaited instead of Fire & Forget because otherwise _globalConfig and _loadedConfig could be invalid (null and false, when the event is dispatched on the Main thread)
            await TaskDispatcher.QueueOnMainThreadAsync(() =>
            {
                OnConfigLoaded?.Invoke();
            }, cancellationToken);
        }
        finally { _requestLock.Release(); }
    }

    public static EVersionStatus GetVersionStatus()
    {
        switch (TryGetLatestVersion(out Version latestVersion))
        {
            case EGetLatestVersionResult.Loading:
                return EVersionStatus.Loading;
            case EGetLatestVersionResult.Failed:
                return EVersionStatus.Unknown;
        }
        if (latestVersion > CurrentVersion) return EVersionStatus.Outdated;
        return EVersionStatus.Latest;
    }

    public static bool TryGetTexts(
        [NotNullWhen(true)] [MaybeNullWhen(false)] out string? message,
        [NotNullWhen(true)] [MaybeNullWhen(false)] out string? title,
        [NotNullWhen(true)] [MaybeNullWhen(false)] out string? subtitle)
    {
        message = null;
        title = null;
        subtitle = null;

        if (!_requestLock.Wait(0)) return false;

        try
        {
            if (_globalConfig != null &&
                _globalConfig.KeySet("UpdateMessage") &&
                _globalConfig.KeySet("Title") &&
                _globalConfig.KeySet("Subtitle"))
            {
                message = _globalConfig["UpdateMessage"].Replace("\\n", "\n");
                title = _globalConfig["Title"].Replace("\\n", "\n");
                subtitle = _globalConfig["Subtitle"].Replace("\\n", "\n");
                return true;
            }

            return false;
        }
        finally { _requestLock.Release(); }
    }

    public static EGetLatestVersionResult TryGetLatestVersion(out Version latestVersion)
    {
        latestVersion = CurrentVersion;

        if (!_requestLock.Wait(0)) return EGetLatestVersionResult.Loading;

        try
        {
            if (_globalConfig != null &&
                _globalConfig.KeySet("LatestVersion") && Version.TryParse(_globalConfig["LatestVersion"], out latestVersion))
            {
                return EGetLatestVersionResult.Success;
            }

            return EGetLatestVersionResult.Failed;
        }
        finally { _requestLock.Release(); }
    }

    public static Version LatestVersion
    {
        get
        {
            TryGetLatestVersion(out Version latestVersion);
            return latestVersion;
        }
    }
}