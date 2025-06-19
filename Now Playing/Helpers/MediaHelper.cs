using Windows.Media.Control;

namespace Now_Playing.Helpers;

/// <summary>Contains events and methods for responding to and controlling media.</summary>
public static class MediaHelper
{
    public static bool SessionExists => session is not null;

    public static event Action<GlobalSystemMediaTransportControlsSessionMediaProperties> MediaPropertiesChanged;

    public static event Action<GlobalSystemMediaTransportControlsSessionPlaybackInfo> PlaybackInfoChanged;

    public static event Action<GlobalSystemMediaTransportControlsSessionTimelineProperties> TimelinePropertiesChanged;

    public static void TogglePlayback() => session?.TryTogglePlayPauseAsync();

    public static void SkipPrevious() => session?.TrySkipPreviousAsync();

    public static void SkipNext() => session?.TrySkipNextAsync();

    public static void SetPlaybackPositon(long position) => session?.TryChangePlaybackPositionAsync(position);

    public static GlobalSystemMediaTransportControlsSessionPlaybackInfo GetPlaybackInfo() => session?.GetPlaybackInfo();

    public static GlobalSystemMediaTransportControlsSessionTimelineProperties GetTimelineProperties() => session?.GetTimelineProperties();

    public async static Task<GlobalSystemMediaTransportControlsSessionMediaProperties> GetMediaPropertiesAsync()
    {
        if (session is null)
            return null;

        GlobalSystemMediaTransportControlsSessionMediaProperties props = null;
        try
        {
            props = await session.TryGetMediaPropertiesAsync();
        }
        catch { }

        if (props is null && tries <= 10)
        {
            ++tries;
            return await GetMediaPropertiesAsync();
        }
        tries = 0;
        return props;
    }
    static int tries = 0; //Avoids stack overflows :)

    private static GlobalSystemMediaTransportControlsSession session;

    private static GlobalSystemMediaTransportControlsSessionManager sessionManager;

    static MediaHelper() => CreateManager();

    private static async void CreateManager()
    {
        sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        sessionManager.CurrentSessionChanged += (s, e) => GetSession();
        GetSession();
    }

    private static void GetSession()
    {
        session = sessionManager.GetCurrentSession();
        if (session is null)
            return;

        session.MediaPropertiesChanged += (s, e) => UpdateMediaProperties();
        session.PlaybackInfoChanged += (s, e) => UpdatePlaybackInfo();
        session.TimelinePropertiesChanged += (s, e) => UpdateTimeline();
        UpdateMediaProperties();
    }

    private static void UpdatePlaybackInfo() => PlaybackInfoChanged?.Invoke(GetPlaybackInfo());

    private static void UpdateTimeline() => TimelinePropertiesChanged?.Invoke(GetTimelineProperties());

    private static async void UpdateMediaProperties()
    {
        MediaPropertiesChanged?.Invoke(await GetMediaPropertiesAsync());
        UpdatePlaybackInfo();
    }
}
