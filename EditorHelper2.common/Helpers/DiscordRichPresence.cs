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
    
    private Discord.Discord _discord = null!;
    private ActivityManager _activityManager = null!;
    private Activity _activity;

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
            _discord = new Discord.Discord(DiscordAppID, (long)CreateFlags.Default);
            _activityManager = _discord.GetActivityManager();
        }
        catch (Exception e)
        {
            UnturnedLog.error(e.ToString());
            yield break;
        }
        
        _discord.SetLogHook(LogLevel.Info, (logLevel, msg) => { UnturnedLog.info($"[DRP-{logLevel.ToString()}] {msg}"); });
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
        _activityManager.UpdateActivity(_activity, (result) =>
        {
            if (result != Result.Ok) UnturnedLog.error("[DRP] Failed to update presence");
        });
    }

    private void Update()
    {
        _discord.RunCallbacks();
    }
    
    private void OnDestroy()
    {
        _discord.Dispose();
    }
}