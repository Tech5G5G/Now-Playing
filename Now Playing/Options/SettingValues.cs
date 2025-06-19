namespace Now_Playing.Options;

public static class SettingValues
{
    private readonly static Settings settings = new();

    public static bool PauseWhenAppClosedEnabled
    {
        get => settings.PauseWhenAppClosedEnabled;
        set
        {
            settings.PauseWhenAppClosedEnabled = value;
            settings.Save();
        }
    }

    public static bool AutoShowLyricsEnabled
    {
        get => settings.AutoShowLyricsEnabled;
        set
        {
            settings.AutoShowLyricsEnabled = value;
            settings.Save();
        }
    }

    public static bool LyricsViewViewable
    {
        get => settings.LyricsViewViewable;
        set
        {
            settings.LyricsViewViewable = value;
            settings.Save();
        }
    }

    public static BitmapCacheOption BitmapCacheMode
    {
        get => (BitmapCacheOption)settings.BitmapCacheMode;
        set
        {
            settings.BitmapCacheMode = (int)value;
            settings.Save();
        }
    }

    public static StartupMode StartupMode
    {
        get => (StartupMode)settings.StartupMode;
        set
        {
            settings.StartupMode = (int)value;
            settings.Save();
        }
    }

    public static string ImageResolutionThreshold
    {
        get => settings.ImageResolutionThreshold;
        set
        {
            settings.ImageResolutionThreshold = value;
            settings.Save();
        }
    }

    public static void ForceSave() => settings.Save();
}

public enum StartupMode
{
    Fullscreen,
    Miniplayer
}
