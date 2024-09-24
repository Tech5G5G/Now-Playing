using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using System.Windows;

namespace Now_Playing
{
    public static class Settings
    {
        public static bool AutoShowLyricsEnabled
        {
            get { return Properties.Settings.Default.AutoShowLyricsEnabled; }
            set
            {
                Properties.Settings.Default.AutoShowLyricsEnabled = value;
                Properties.Settings.Default.Save();;
            }
        }

        public static bool PauseWhenAppClosedEnabled
        {
            get { return Properties.Settings.Default.PauseWhenAppClosedEnabled; }
            set
            {
                Properties.Settings.Default.PauseWhenAppClosedEnabled = value;
                Properties.Settings.Default.Save();
            }
        }

        public static bool LyricsViewViewable
        {
            get { return Properties.Settings.Default.LyricsViewViewable; }
            set
            {
                Properties.Settings.Default.LyricsViewViewable = value;
                Properties.Settings.Default.Save();
            }
        }

        public static int BitmapCacheMode
        {
            get { return Properties.Settings.Default.BitmapCacheMode; }
            set
            {
                Properties.Settings.Default.BitmapCacheMode = value;
                Properties.Settings.Default.Save();
            }
        }

        public static string ImageResolutionThreshold
        {
            get { return Properties.Settings.Default.ImageResolutionThreshold; }
            set
            {
                Properties.Settings.Default.ImageResolutionThreshold = value;
                Properties.Settings.Default.Save();
            }
        }

        public static int StartupMode
        {
            get { return Properties.Settings.Default.StartupMode; }
            set
            {
                Properties.Settings.Default.StartupMode = value;
                Properties.Settings.Default.Save();
            }
        }
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
    }
}
