using System;
using System.IO;
using System.Net;
using EditorHelper2.common.Types;
using SDG.Unturned;

namespace EditorHelper2.common.Helpers;

// https://github.com/ShimmyMySherbet/ShimmysAdminTools/blob/master/ShimmysAdminTools/Components/UpdaterCore.cs
public static class UpdaterCore
{
    private const string GlobalConfigURL = "https://gist.sshost.club/seniors/b855c981b37940c69fc6e3c8ea63f032/raw/HEAD/gistfile1.ini";
    private static IniFile _globalConfig;
    private static readonly Version CurrentVersion = typeof(UpdaterCore).Assembly.GetName().Version;
    private static bool _hasConfig;

    public static void Init()
    {
        HttpWebRequest req = WebRequest.CreateHttp(GlobalConfigURL);
        req.Method = "GET";
        req.Timeout = 2500;
        using (WebResponse resp = req.GetResponse())
        using (Stream network = resp.GetResponseStream())
        using (StreamReader reader = new(network))
        {
            string ini = reader.ReadToEnd();
            _globalConfig = new IniFile(ini);
        }

        _hasConfig = true;
    }

    public static bool IsOutDated => LatestVersion > CurrentVersion;

    public static bool TryGetTexts(out string message, out string title, out string subtitle)
    {
        message = null;
        title = null;
        subtitle = null;
        if (_hasConfig && _globalConfig.KeySet("UpdateMessage")
                       && _globalConfig.KeySet("Title")
                       && _globalConfig.KeySet("Subtitle"))
        {
            message = _globalConfig["UpdateMessage"].Replace("\\n", "\n");
            title = _globalConfig["Title"].Replace("\\n", "\n");
            subtitle = _globalConfig["Subtitle"].Replace("\\n", "\n");
            return true;
        }

        return false;
    }

    public static Version LatestVersion
    {
        get
        {
            if (_hasConfig && _globalConfig != null && _globalConfig.KeySet("LatestVersion") &&
                Version.TryParse(_globalConfig["LatestVersion"], out Version? latest))
            {
                return latest;
            }

            return CurrentVersion;
        }
    }
}