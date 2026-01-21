using System;
using System.Collections;
using System.IO;
using Discord;
using SDG.Unturned;
using UnityEngine;
using UnityEngine.Networking;

namespace EditorHelper2.common.Helpers;

public class DiscordRichPresence : MonoBehaviour
{
    private const string DiscordGameSDKURL = "https://ps.sshost.club/api/shares/kUfX2VaB/files/14dd844f-0fd0-4435-88b3-cb14307d753a";
    private const long DiscordAppID = 1424469500153299044;

    private Discord.Discord? _discord;
    private ActivityManager? _activityManager;
    private Activity _activity;
    private bool _isInitialized;

    private long _startTime;

    public DiscordRichPresence()
    {
        StartCoroutine(DelayInit());
    }

    private IEnumerator DelayInit()
    {
        string path = Path.Combine(Environment.CurrentDirectory, "discord_game_sdk.dll");
        if (!File.Exists(path))
        {
            UnturnedLog.info("[DRP] Discord game sdk not found. Downloading...");

            using UnityWebRequest request = UnityWebRequest.Get(DiscordGameSDKURL);
            string tempPath = path + ".download";

            yield return request.SendWebRequest();
                
            if (request.result != UnityWebRequest.Result.Success)
            {
                UnturnedLog.error($"Failed to download DLL: {request.error}");
                yield break;
            }

            try
            {
                File.WriteAllBytes(tempPath, request.downloadHandler.data);
                if (File.Exists(path))
                    File.Delete(path);

                File.Move(tempPath, path);
                UnturnedLog.info("[DRP] Discord game sdk downloaded successfully.");
            }
            catch (Exception ex)
            {
                UnturnedLog.error($"[DRP] Error saving DLL: {ex.Message}");
                yield break;
            }
        }
        
        _activity = new Activity
        {
            State = "In the menu",
            Assets =
            {
                LargeImage = "unturned_logo",
                SmallImage = "eh",
                SmallText = "With EditorHelper 2"
            }
        };
        
        try
        {
            _discord = new Discord.Discord(DiscordAppID, (long)CreateFlags.NoRequireDiscord);
            _activityManager = _discord.GetActivityManager();
        }
        catch (Exception e)
        {
            UnturnedLog.error($"[DRP] Failed to initialize Discord: {e.Message}");
            _discord = null;
            _activityManager = null;
            yield break;
        }

        _discord.SetLogHook(LogLevel.Info, (logLevel, msg) => { UnturnedLog.info($"[DRP-{logLevel.ToString()}] {msg}"); });
        _isInitialized = true;
        UpdateEditingPresence();
    }

    public void StartMapEditing(string mapName)
    {
        _startTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        _activity.Timestamps.Start = _startTime;
        _activity.Details = $"Editing {mapName}";
        _activity.State = "In the editor";
    }

    public void StopMapEditing()
    {
        _activity.Timestamps.Start = 0;
        _activity.Details = string.Empty;
        _activity.State = "In the menu";
    }

    public void UpdateEditingPresence()
    {
        if (_activityManager == null) return;

        try
        {
            _activityManager.UpdateActivity(_activity, (result) =>
            {
                if (result != Result.Ok) UnturnedLog.error($"[DRP] Failed to update presence: {result}");
            });
        }
        catch (Exception e)
        {
            UnturnedLog.error($"[DRP] Error updating presence: {e.Message}");
            HandleDiscordDisconnect();
        }
    }

    public void UpdateAnonymous(bool enabled)
    {
        _activity.State = enabled 
            ? "Who knows what is he doing? (:" 
            : "In the menu";

        UpdateEditingPresence();
    }

    private void Update()
    {
        if (_discord == null || !_isInitialized) return;

        try
        {
            _discord.RunCallbacks();
        }
        catch (Exception)
        {
            HandleDiscordDisconnect();
        }
    }

    private void HandleDiscordDisconnect()
    {
        _isInitialized = false;
        _activityManager = null;

        try
        {
            _discord?.Dispose();
        }
        catch { }

        _discord = null;
        StartCoroutine(RetryConnection());
    }

    private IEnumerator RetryConnection()
    {
        yield return new WaitForSeconds(5f);

        if (_discord != null) yield break;

        try
        {
            _discord = new Discord.Discord(DiscordAppID, (long)CreateFlags.NoRequireDiscord);
            _activityManager = _discord.GetActivityManager();
            _discord.SetLogHook(LogLevel.Info, (logLevel, msg) => { UnturnedLog.info($"[DRP-{logLevel.ToString()}] {msg}"); });
            _isInitialized = true;
            UpdateEditingPresence();
            UnturnedLog.info("[DRP] Reconnected to Discord");
        }
        catch (Exception)
        {
            StartCoroutine(RetryConnection());
        }
    }

    private void OnDestroy()
    {
        _isInitialized = false;
        try
        {
            _discord?.Dispose();
        }
        catch { }
        _discord = null;
        _activityManager = null;
    }
}