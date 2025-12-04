using DanielWillett.UITools.API.Extensions;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using SDG.Unturned;

namespace EditorHelper2.Extensions.Editor.Dashboard;

[UIExtension(typeof(EditorDashboardUI))]
[EHExtension("Discord extension", "Senior S")]
public class DiscordExtension : UIExtension, IExtension
{
    public DiscordExtension()
    {
        EditorHelper.GetRichPresence().UpdateAnonymous(false);
        Initialize();
    }

    public void Initialize()
    {
        EditorHelper.GetRichPresence().StartMapEditing(SDG.Unturned.Level.info.name);
        EditorHelper.GetRichPresence().UpdateEditingPresence();
    }

    public void Dispose()
    {
        EditorHelper.GetRichPresence().StopMapEditing();
        EditorHelper.GetRichPresence().UpdateEditingPresence();
    }
}