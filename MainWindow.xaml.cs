using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.ApplicationModel.Background;
using Windows.Media.Control;
using Windows.Storage.FileProperties;
using System.Windows.Media;
using Windows.Phone.Notification.Management;
using System.Windows.Media.Animation;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Media.Effects;
using Windows.Devices.Radios;
using Windows.UI.ViewManagement;
using Windows.Media.Devices;
using System.Diagnostics;
using CoreAudio;
using System.Windows.Controls.Primitives;
using System.Net.Http;
using Newtonsoft.Json;
using System.Reflection.Emit;
using Windows.Web.Syndication;
using System.Net;
using System.ComponentModel;
using System.Threading;
using Wpf.Ui.Controls;
using System.Timers;
using System.Runtime.Remoting.Channels;
using System.Globalization;
using Windows.Foundation.Collections;
using static System.Net.Mime.MediaTypeNames;
using static Now_Playing.Lyrics;
using Windows.UI.Text;

namespace Now_Playing
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GlobalSystemMediaTransportControlsSession session;

        private GlobalSystemMediaTransportControlsSessionManager sessionManager;

        private MMDevice device;

        private Song currentSong;

        public MainWindow()
        {
            InitializeComponent();
            this.Closed += async (object sender, EventArgs e) =>
            {
                Properties.Settings.Default.Save();

                if (Settings.PauseWhenAppClosedEnabled && session != null)
                    await session.TryPauseAsync();
            };
            lyricsView.Loaded += (object sender, RoutedEventArgs e) => this.Dispatcher.Invoke(() =>
            {
                if (Settings.AutoShowLyricsEnabled)
                {
                    if (Settings.LyricsViewViewable && lyrics.Text != "No lyrics available")
                        lyricsView.Visibility = Visibility.Visible;
                    else
                        lyricsView.Visibility = Visibility.Collapsed;
                }
                else
                    lyricsView.Visibility = Settings.LyricsViewViewable ? Visibility.Visible : Visibility.Collapsed;
            });
            this.Dispatcher.Invoke(() => toggleLyricsButton.IsChecked = Settings.LyricsViewViewable);

            MakeManager();
            MakeAudioManager();
            Wpf.Ui.Appearance.SystemThemeWatcher.Watch(this, WindowBackdropType.None, true);
        }

        private void TimerEnd(object sender, ElapsedEventArgs e)
        {
            lyricTimer.Stop();

            Grid sp = null;
            this.Dispatcher.Invoke(() => sp = lyricsGrid.Children[currentLyric] as Grid);
            ScrollToLyric(sp);

            var alreadyPlayed = currentSong.ParsedLyrics.lyricLines[currentLyric].Timestamp.TotalMilliseconds;
            ++currentLyric;
            var interval = currentSong.ParsedLyrics.lyricLines[currentLyric].Timestamp.TotalMilliseconds - alreadyPlayed;
            lyricTimer = new System.Timers.Timer(interval);
            lyricTimer.Elapsed += TimerEnd;
            lyricTimer.Start();

            //var timestamp = TimeSpan.FromMilliseconds(alreadyPlayed);
            //string lyricsToDisplay = currentSong.ParsedLyrics.GetTextForTimestamp(timestamp);
            //this.Dispatcher.Invoke(() =>
            //{
            //    lyrics.Text = lyricsTint.Text = lyricsToDisplay;
            //});
        }

        private void ScrollToLyric(Grid lyricToScrollTo)
        {
            var math = (lyricsView.ActualHeight + (lyricToScrollTo.ActualHeight + 10)) / 2;
            this.Dispatcher.Invoke(() => lyricToScrollTo.BringIntoView(new Rect(new Point(), new Size(lyricToScrollTo.ActualWidth, math))));

            this.Dispatcher.Invoke(() =>
            {
                foreach (Grid grid in lyricsGrid.Children)
                {
                    var tint = grid.Children[1] as System.Windows.Controls.TextBlock;

                    if (tint.Opacity != 0.5)
                        tint.Opacity = 0.5;
                }
            });

            this.Dispatcher.Invoke(() => (lyricToScrollTo.Children[1] as System.Windows.Controls.TextBlock).Opacity = 1);
        }

        private System.Timers.Timer lyricTimer;

        private int currentLyric = 1;

        private void SetSongLyrics(Song song)
        {
            if (song.syncedLyrics != null)
            {
                try
                {
                    this.Dispatcher.Invoke(() => lyricsGrid.Children.Clear());

                    Lyrics parsedLyrics = Lyrics.ParseSyncedLyrics(song.syncedLyrics);
                    song.ParsedLyrics = parsedLyrics;
                    this.currentSong = song;

                    foreach (LyricsLine lyricLine in currentSong.ParsedLyrics.lyricLines)
                    {
                        if (currentSong.ParsedLyrics.lyricLines[0] != lyricLine)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                var grid = new Grid() { Tag = lyricLine.Timestamp.TotalMilliseconds, Margin = new Thickness(0, 10, 0, 0) };

                                string text = lyricLine.Text;
                                if (string.IsNullOrWhiteSpace(text))
                                    text = "loading placeholder";

                                var textBlock = new System.Windows.Controls.TextBlock() { Text = text, TextWrapping = TextWrapping.Wrap, FontWeight = System.Windows.FontWeights.Bold, FontSize = 40, Foreground = new SolidColorBrush(accentColor) };
                                var textBlockTint = new System.Windows.Controls.TextBlock() { Text = text, TextWrapping = TextWrapping.Wrap, FontWeight = System.Windows.FontWeights.Bold, FontSize = 40, Foreground = Brushes.White, Opacity = 0.5 };

                                grid.Children.Add(textBlock);
                                grid.Children.Add(textBlockTint);

                                lyricsGrid.Children.Add(grid);
                            });
                        }
                        else
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                var grid = new Grid() { Tag = lyricLine.Timestamp.TotalMilliseconds, Height = 0, Margin = new Thickness(0, 0, 0, -10) };

                                var textBlock = new System.Windows.Controls.TextBlock() { TextWrapping = TextWrapping.Wrap, FontWeight = System.Windows.FontWeights.Bold, FontSize = 40, Foreground = new SolidColorBrush(accentColor) };
                                var textBlockTint = new System.Windows.Controls.TextBlock() { TextWrapping = TextWrapping.Wrap, FontWeight = System.Windows.FontWeights.Bold, FontSize = 40, Foreground = Brushes.White, Opacity = 0.5 };

                                grid.Children.Add(textBlock);
                                grid.Children.Add(textBlockTint);

                                lyricsGrid.Children.Add(grid);
                            });
                        }
                    }

                    currentLyric = 1;
                    lyricTimer = new System.Timers.Timer(currentSong.ParsedLyrics.lyricLines[currentLyric].Timestamp.TotalMilliseconds);
                    lyricTimer.Start();
                    lyricTimer.Elapsed += TimerEnd;

                    //this.Dispatcher.Invoke(() =>
                    //{
                    //    lyrics.Text = lyricsTint.Text = "";

                    //    if (Settings.AutoShowLyricsEnabled && Settings.LyricsViewViewable)
                    //        lyricsView.Visibility = Visibility.Visible;
                    //});
                }
                catch (Exception)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        lyrics.Text = lyricsTint.Text = song.syncedLyrics;

                        if (Settings.AutoShowLyricsEnabled && Settings.LyricsViewViewable)
                            lyricsView.Visibility = Visibility.Visible;
                    });
                }
            }
            else if (song.plainLyrics != null)
            {
                this.Dispatcher.Invoke(() =>
                {
                    lyrics.Text = lyricsTint.Text = song.plainLyrics;

                    if (Settings.AutoShowLyricsEnabled && Settings.LyricsViewViewable)
                        lyricsView.Visibility = Visibility.Visible;
                });
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    lyrics.Text = lyricsTint.Text = "No lyrics available";

                    if (Settings.AutoShowLyricsEnabled)
                        lyricsView.Visibility = Visibility.Collapsed;
                });
            }
        }

        private void MakeAudioManager()
        {
            MMDeviceEnumerator devEnum = new MMDeviceEnumerator(Guid.NewGuid());
            device = devEnum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            device.AudioEndpointVolume.OnVolumeNotification += (AudioVolumeNotificationData data) => this.Dispatcher.Invoke(() => volume.Value = (int)(data.MasterVolume * 100));

            int currentVolume = (int)(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            volume.Value = currentVolume;
            ToolTipService.SetToolTip(volume, currentVolume);
            volume.ValueChanged += (object sender, RoutedPropertyChangedEventArgs<double> e) => ToolTipService.SetToolTip(volume, e.NewValue);
        }

        private async void MakeManager()
        {
            sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            sessionManager.CurrentSessionChanged += CurrentSessionChanged;

            MakeSession();
        }

        private void MakeSession()
        {
            session = sessionManager.GetCurrentSession();

            if (session != null)
            {
                session.MediaPropertiesChanged += MediaPropertiesChanged;
                session.PlaybackInfoChanged += PlaybackInfoChanged;
                session.TimelinePropertiesChanged += TimelinePropertiesChanged;

                SetNowPlayingDetails();
            }
        }

        private void Window_Close(object sender, RoutedEventArgs e) => this.Close();
        private void CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args) => MakeSession();
        private void PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args) => UpdatePlaybackStatus();
        private void MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args) => SetNowPlayingDetails();
        private void Adjust_Volume(object sender, DragDeltaEventArgs e) => device.AudioEndpointVolume.MasterVolumeLevelScalar = ((float)volume.Value / 100.0f);
        private void VolumeUp_Executed(object sender, ExecutedRoutedEventArgs e) => device.AudioEndpointVolume.MasterVolumeLevelScalar = device.AudioEndpointVolume.MasterVolumeLevelScalar + (float)0.01;
        private void VolumeDown_Executed(object sender, ExecutedRoutedEventArgs e) => device.AudioEndpointVolume.MasterVolumeLevelScalar = device.AudioEndpointVolume.MasterVolumeLevelScalar - (float)0.01;

        private void TimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
        {
            this.Dispatcher.Invoke(() =>
            {
                var timelineProperties = sender.GetTimelineProperties();
                timeline.Maximum = timelineProperties.EndTime.TotalSeconds;
                timeline.Minimum = timelineProperties.StartTime.TotalSeconds;

                timeline.Value = timelineProperties.Position.TotalSeconds;

                //if (currentSong != null && currentSong.ParsedLyrics != null)
                //{
                //    lyrics.Text = currentSong.ParsedLyrics.GetTextForTimestamp(timelineProperties.Position);
                //    lyricsTint.Text = lyrics.Text;
                //}
            });
        }

        Color accentColor = Colors.Black;

        private async void SetNowPlayingDetails()
        {
            UpdatePlaybackStatus();

            if (session != null)
            {
                GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties = null;

                try
                {
                    mediaProperties = await session.TryGetMediaPropertiesAsync();
                }
                catch (FileNotFoundException)
                {
                    SetNowPlayingDetails();
                    return;
                }

                var thumbnail = mediaProperties.Thumbnail;

                if (thumbnail != null)
                {
                    var openedThumbnail = await thumbnail.OpenReadAsync();
                    BitmapFrame albumArtwork = null;

                    using (var stream = openedThumbnail.AsStream())
                    {
                        if (stream != null && stream.Length > 0)
                        {
                            stream.Seek(0, SeekOrigin.Current);

                            //var policy = new System.Net.Cache.RequestCachePolicy();
                            //BitmapFrame.Create()

                            albumArtwork = BitmapFrame.Create(stream, BitmapCreateOptions.None, (BitmapCacheOption)Settings.BitmapCacheMode);
                            accentColor = AverageColor(stream);

                            this.Dispatcher.Invoke(() =>
                            {
                                var backgroundImage = new System.Windows.Controls.Image() { Source = albumArtwork, Effect = new BlurEffect() { Radius = 20 }, Opacity = 0.5 };

                                var grid = new Grid();
                                grid.Children.Add(new Rectangle() { Fill = Brushes.Black });
                                grid.Children.Add(backgroundImage);

                                this.Background = new VisualBrush(grid) { Stretch = Stretch.UniformToFill };

                                this.Background.Transform = new ScaleTransform() { ScaleX = 2, ScaleY = 2, CenterX = 200 };
                            });
                        }
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        imageSource.Source = albumArtwork;

                        Album.Foreground = Artist.Foreground = title.Foreground = lyrics.Foreground = new SolidColorBrush(accentColor);

                        if (!string.IsNullOrEmpty(mediaProperties.Title))
                        {
                            titleGrid.Visibility = Visibility.Visible;
                            title.Text = mediaProperties.Title;
                            titleTint.Text = mediaProperties.Title;
                        }
                        else
                            titleGrid.Visibility = Visibility.Collapsed;

                        if (!string.IsNullOrEmpty(mediaProperties.AlbumTitle))
                        {
                            albumGrid.Visibility = Visibility.Visible;
                            Album.Text = mediaProperties.AlbumTitle;
                            albumTint.Text = mediaProperties.AlbumTitle;
                        }
                        else
                            albumGrid.Visibility = Visibility.Collapsed;

                        if (!string.IsNullOrEmpty(mediaProperties.Artist))
                        {
                            artistGrid.Visibility = Visibility.Visible;
                            Artist.Text = mediaProperties.Artist;
                            artistTint.Text = mediaProperties.Artist;
                        }
                        else if (!string.IsNullOrEmpty(mediaProperties.AlbumArtist))
                        {
                            artistGrid.Visibility = Visibility.Visible;
                            Artist.Text = mediaProperties.AlbumArtist;
                            artistTint.Text= mediaProperties.AlbumArtist;
                        }
                        else
                            artistGrid.Visibility = Visibility.Collapsed;
                    });

                    string artist = string.Empty;

                    if (!string.IsNullOrEmpty(mediaProperties.Artist))
                        artist = mediaProperties.Artist;
                    else if (!string.IsNullOrEmpty(mediaProperties.AlbumArtist))
                        artist = mediaProperties.AlbumArtist;

                    double duration = session.GetTimelineProperties().EndTime.TotalSeconds;
                    var songs = Song.FromSearch(mediaProperties.Title, artist);
                    if (songs.Count > 0)
                        SetSongLyrics(songs[0]);
                    else
                    {
                        songs = Song.FromSearch(mediaProperties.Title);
                        if (songs.Count > 0)
                            SetSongLyrics(songs[0]);
                        else
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                lyrics.Text = lyricsTint.Text = "No lyrics available";

                                if (Settings.AutoShowLyricsEnabled)
                                    lyricsView.Visibility = Visibility.Collapsed;
                            });
                        }
                    }
                }
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    imageSource.Source = new BitmapImage(new Uri("pack://application:,,,/Now Playing;component/Assets/Placeholder.png"));
                    this.Background = null;

                    lyrics.Text = lyricsTint.Text = "No lyrics available";
                    if (Settings.AutoShowLyricsEnabled)
                        lyricsView.Visibility = Visibility.Collapsed;

                    Artist.Text = artistTint.Text = "Artist";
                    artistGrid.Visibility = Visibility.Collapsed;

                    title.Text = titleTint.Text = "Title";
                    titleGrid.Visibility = Visibility.Collapsed;

                    Album.Text = albumTint.Text = "Album";
                    albumGrid.Visibility = Visibility.Collapsed;

                    togglePlayback.Content = new FontIcon() { FontFamily = (FontFamily)this.Resources["SymbolThemeFontFamily"], Glyph = "\uEA3A" };
                    timeline.Value = 0;
                    Album.Foreground = Artist.Foreground = title.Foreground = lyrics.Foreground = new SolidColorBrush(Colors.Black);
                });
            }
        }

        static Color AverageColor(Stream stream)
        {
            long threshold;
            try
            {
                threshold = long.Parse(Settings.ImageResolutionThreshold);
            }
            catch (Exception)
            {
                threshold = 1000000;
            }
            
            if (stream.Length < threshold)
            {
                using (var bmp = new System.Drawing.Bitmap(stream))
                {
                    int width = bmp.Width;
                    int height = bmp.Height;
                    int red = 0;
                    int green = 0;
                    int blue = 0;
                    int alpha = 0;
                    for (int x = 0; x < width; x++)
                        for (int y = 0; y < height; y++)
                        {
                            var pixel = bmp.GetPixel(x, y);
                            red += pixel.R;
                            green += pixel.G;
                            blue += pixel.B;
                            alpha += pixel.A;
                        }

                    Func<int, int> avg = c => c / (width * height);

                    red = avg(red);
                    green = avg(green);
                    blue = avg(blue);
                    alpha = avg(alpha);

                    var color = Color.FromArgb((byte)alpha, (byte)red, (byte)green, (byte)blue);

                    return color;
                }
            }
            else return Colors.Black;
        }

        private void UpdatePlaybackStatus()
        {
            if (session != null)
            {
                var playbackInfo = session.GetPlaybackInfo().PlaybackStatus;

                this.Dispatcher.Invoke(() =>
                {
                    switch (playbackInfo)
                    {
                        case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing:
                            togglePlayback.Content = new FontIcon() { FontFamily = (FontFamily)this.Resources["SymbolThemeFontFamily"], Glyph = "\uE769" };
                            if (lyricTimer != null)
                                lyricTimer.Start();
                            break;
                        case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused:
                            togglePlayback.Content = new FontIcon() { FontFamily = (FontFamily)this.Resources["SymbolThemeFontFamily"], Glyph = "\uE768" };
                            if (lyricTimer != null)
                                lyricTimer.Stop();
                            break;
                    }
                });
            }
        }

        private void togglePlayback_Click(object sender, RoutedEventArgs e) => TogglePlayback();
        private void PlayPause_Executed(object sender, ExecutedRoutedEventArgs e) => TogglePlayback();
        private void forwardtrack_Click(object sender, RoutedEventArgs e) => SkipNext();
        private void Next_Executed(object sender, ExecutedRoutedEventArgs e) => SkipNext();
        private void backtrack_Click(object sender, RoutedEventArgs e) => SkipPrevious();
        private void Previous_Executed(object sender, ExecutedRoutedEventArgs e) => SkipPrevious();

        private async void TogglePlayback()
        {
            if (session != null)
            {
                await session.TryTogglePlayPauseAsync();
            }
        }

        private async void SkipPrevious()
        {
            if (session != null)
            {
                await session.TrySkipPreviousAsync();
            }
        }

        private async void SkipNext()
        {
            if (session != null)
            {
                await session.TrySkipNextAsync();
            }
        }

        private async void Adjust_Position(object sender, DragDeltaEventArgs e)
        {
            if (session != null)
            {
                await session.TryChangePlaybackPositionAsync((long)(sender as Slider).Value);
            }
        }

        private async void OpenSettings_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var contentDialog = new ContentDialog(dialogPresenter);
            contentDialog.SetCurrentValue(ContentDialog.TitleProperty, "Settings");
            contentDialog.SetCurrentValue(ContentProperty, (StackPanel)this.Resources["SettingsContent"]);
            contentDialog.SetCurrentValue(ContentDialog.CloseButtonTextProperty, "Done");

            await contentDialog.ShowAsync();
        }        

        private void ToggleLyricsButton_Click(object sender, RoutedEventArgs e)
        {
            bool valueToSet = (sender as ToggleButton).IsChecked == true;

            Settings.LyricsViewViewable = valueToSet;

            this.Dispatcher.Invoke(() =>
            {
                if (Settings.AutoShowLyricsEnabled)
                {
                    if (valueToSet && lyrics.Text != "No lyrics available")
                        lyricsView.Visibility = Visibility.Visible;
                    else
                        lyricsView.Visibility = Visibility.Collapsed;
                }
                else
                    lyricsView.Visibility = valueToSet ? Visibility.Visible : Visibility.Collapsed;
            });
        }
    }

    public class HttpClientFactory
    {
        private string webServiceUrl = "https://lrclib.net/api/";

        public HttpClient CreateClient()
        {
            var client = new HttpClient();
            SetupClientDefaults(client);
            return client;
        }

        protected virtual void SetupClientDefaults(HttpClient client)
        {
            //This is global for all REST web service calls
            client.Timeout = TimeSpan.FromSeconds(60);
            client.BaseAddress = new Uri(webServiceUrl);
        }
    }

    public class Lyrics
    {
        public class LyricsLine
        {
            internal TimeSpan Timestamp { get; set; }
            internal string Text { get; set; }
        }

        public LyricsLine[] lyricLines;

        private const int TimestampLength = 9;

        public string GetTextForTimestamp(TimeSpan timestamp)
        {
            for (int i = 1; i < lyricLines.Length; ++i)
            {
                if (timestamp < lyricLines[i].Timestamp)
                {
                    return lyricLines[i - 1].Text; // + " [" + timestamp.ToString() + "]";
                }
            }

            return lyricLines[lyricLines.Length - 1].Text;
        }

        public static Lyrics ParseSyncedLyrics(string syncedLyrics)
        {
            if (string.IsNullOrWhiteSpace(syncedLyrics))
            {
                throw new ArgumentNullException();
            }

            string[] lines = syncedLyrics.Split('\n');

            Lyrics result = new Lyrics();
            result.lyricLines = new LyricsLine[lines.Length + 1];

            // Empty entry for start of the song
            result.lyricLines[0] = new LyricsLine
            {
                Timestamp = TimeSpan.Zero,
                Text = String.Empty
            };

            int lineIndex = 1;
            foreach (string line in lines)
            {
                string timestampString = line.Substring(1, TimestampLength - 1);

                string text = String.Empty;
                if (line.Length > TimestampLength + 1)
                {
                    text = line.Substring(TimestampLength + 2);
                }

                TimeSpan timestamp = TimeSpan.Parse("00:" + timestampString);

                // Round the timestamp down to avoid text showing up too late
                // (it can still show up too early)
                //timestamp = TimeSpan.FromSeconds(Math.Floor(timestamp.TotalSeconds));

                result.lyricLines[lineIndex] = new LyricsLine
                {
                    Timestamp = timestamp,
                    Text = text
                };

                ++lineIndex;
            }

            return result;
        }
    };

    public class Song
    {
        public string id { get; set; }
        public string name { get; set; }
        public string trackName { get; set; }
        public string artistName { get; set; }
        public string albumName { get; set; }
        public string duration { get; set; }
        public string instrumental { get; set; }
        public string plainLyrics { get; set; }
        public string syncedLyrics { get; set; }

        public Lyrics ParsedLyrics { get; set; }

        public enum GetMode
        {
            get = 1,

            getcached = 2
        }

        public static Song FromSongID(string songID)
        {
            Song apiresponse = new Song();
            string result = string.Empty;
            HttpClientFactory clientFactory = new HttpClientFactory();
            var client = clientFactory.CreateClient();
            HttpResponseMessage response = client.GetAsync("/api/get/" + songID).Result;
            if (response.IsSuccessStatusCode)
            {
                result = response.Content.ReadAsStringAsync().Result;
                apiresponse = JsonConvert.DeserializeObject<Song>(result);
            }
            else
            {
                throw new Exception(response.ReasonPhrase);
            }
            return apiresponse;
        }

        public static Song FromTrackSignature(string trackSignature, GetMode getMode)
        {
            string apiGetMode = "get?";

            if (getMode == GetMode.get)
            {
                apiGetMode = "get?";
            }
            else
            {
                apiGetMode = "get-cached?";
            }

            Song apiresponse = new Song();
            string result = string.Empty;
            HttpClientFactory clientFactory = new HttpClientFactory();
            var client = clientFactory.CreateClient();
            HttpResponseMessage response = client.GetAsync("/api/" + apiGetMode + trackSignature).Result;
            if (response.IsSuccessStatusCode)
            {
                result = response.Content.ReadAsStringAsync().Result;
                apiresponse = JsonConvert.DeserializeObject<Song>(result);
            }
            else
            {
                throw new Exception(response.ReasonPhrase);
            }
            return apiresponse;
        }

        public static List<Song> FromSearch(string trackName)
        {
            List<Song> apiresponse = new List<Song>();
            string result = string.Empty;
            HttpClientFactory clientFactory = new HttpClientFactory();
            var client = clientFactory.CreateClient();

            var updatedTrackName = WebUtility.UrlEncode(trackName).ToLower();

            HttpResponseMessage response = client.GetAsync("/api/search?track_name=" + updatedTrackName).Result;
            if (response.IsSuccessStatusCode)
            {
                result = response.Content.ReadAsStringAsync().Result;
                apiresponse = JsonConvert.DeserializeObject<List<Song>>(result);
            }
            else
            {
                throw new Exception(response.ReasonPhrase);
            }
            return apiresponse;
        }

        public static List<Song> FromSearch(string trackName, string artistName)
        {
            List<Song> apiresponse = new List<Song>();
            string result = string.Empty;
            HttpClientFactory clientFactory = new HttpClientFactory();
            var client = clientFactory.CreateClient();

            var updatedTrackName = WebUtility.UrlEncode(trackName).ToLower();
            var updatedArtistName = WebUtility.UrlEncode(artistName).ToLower();

            HttpResponseMessage response = client.GetAsync("/api/search?track_name=" + updatedTrackName + "&artist_name=" + updatedArtistName).Result;
            if (response.IsSuccessStatusCode)
            {
                result = response.Content.ReadAsStringAsync().Result;
                apiresponse = JsonConvert.DeserializeObject<List<Song>>(result);
            }
            else
            {
                throw new Exception(response.ReasonPhrase);
            }
            return apiresponse;
        }
    }
}
