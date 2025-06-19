using System.Timers;
using System.Collections.ObjectModel;
using Windows.Media.Control;
using CoreAudio;

namespace Now_Playing
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Color accentColor = Colors.Black;

        private Song currentSong;

        private readonly ObservableCollection<LyricLine> lyricLines = [];

        private const string NoLyricsPlaceholder = "No lyrics available";

        public MainWindow()
        {
            InitializeComponent();

            Closed += (s, e) =>
            {
                SettingValues.ForceSave();

                if (SettingValues.PauseWhenAppClosedEnabled)
                    MediaHelper.TogglePlayback();
            };

            if (SettingValues.AutoShowLyricsEnabled)
                lyricsView.Visibility = SettingValues.LyricsViewViewable && lyrics.Text != NoLyricsPlaceholder ? Visibility.Visible : Visibility.Collapsed;
            else
                lyricsView.Visibility = SettingValues.LyricsViewViewable ? Visibility.Visible : Visibility.Collapsed;

            toggleLyricsButton.IsChecked = SettingValues.LyricsViewViewable;

            ListenToMedia();
            MakeAudioManager();
        }

        private MMDevice device;

        private void MakeAudioManager()
        {
            MMDeviceEnumerator devices = new(Guid.NewGuid());
            device = devices.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            device.AudioEndpointVolume.OnVolumeNotification += (data) => volume.Value = (int)(data.MasterVolume * 100);

            int currentVolume = (int)(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            volume.Value = currentVolume;
            ToolTipService.SetToolTip(volume, currentVolume);
            volume.ValueChanged += (s, e) => ToolTipService.SetToolTip(volume, e.NewValue);
        }

        private void Adjust_Volume(object sender, DragDeltaEventArgs args) => device.AudioEndpointVolume.MasterVolumeLevelScalar = (float)volume.Value / 100.0f;
        private void VolumeUp_Executed(EventCommand sender, object args) => device.AudioEndpointVolume.MasterVolumeLevelScalar += 0.01f;
        private void VolumeDown_Executed(EventCommand sender, object args) => device.AudioEndpointVolume.MasterVolumeLevelScalar -= 0.01f;

        private void PlayPause_Executed(EventCommand sender, object args) => MediaHelper.TogglePlayback();
        private void Next_Executed(EventCommand sender, object args) => MediaHelper.SkipNext();
        private void Previous_Executed(EventCommand sender, object args) => MediaHelper.SkipPrevious();

        private void Adjust_Position(object sender, DragDeltaEventArgs e) => MediaHelper.SetPlaybackPositon((long)(sender as Slider).Value);

        private async void ListenToMedia()
        {
            MediaHelper.MediaPropertiesChanged += SetNowPlayingDetails;
            MediaHelper.PlaybackInfoChanged += (e) => UpdatePlaybackStatus();
            MediaHelper.TimelinePropertiesChanged += (e) => this.Dispatcher.Invoke(() =>
            {
                timeline.Maximum = e.EndTime.TotalSeconds;
                timeline.Minimum = e.StartTime.TotalSeconds;
                timeline.Value = e.Position.TotalSeconds;

                if (currentSong != null && currentSong.Lyrics != null)
                    //{
                    ScrollToLyric(currentSong.Lyrics.GetIndexForTimestamp(e.Position));
                //lyrics.Text = currentSong.ParsedLyrics.GetTextForTimestamp(timelineProperties.Position);
                //lyricsTint.Text = lyrics.Text;
                //}
            });

            SetNowPlayingDetails(await MediaHelper.GetMediaPropertiesAsync());
        }

        private void UpdatePlaybackStatus()
        {
            if (!MediaHelper.SessionExists)
                return;

            this.Dispatcher.Invoke(() =>
            {
                bool playing = MediaHelper.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

                togglePlayback.Content = new FontIcon
                {
                    FontFamily = (FontFamily)Resources["SymbolThemeFontFamily"],
                    Glyph = playing ? "\uE769" : "\uE768"
                }.ProvideValue(null);

                if (playing)
                    lyricTimer?.Start();
                else
                    lyricTimer?.Stop();
            });
        }

        private void SetNowPlayingDetails(GlobalSystemMediaTransportControlsSessionMediaProperties props)
        {
            UpdatePlaybackStatus();

            this.Dispatcher.Invoke(async () =>
            {
                if (!MediaHelper.SessionExists || props is null)
                {
                    imageSource.Source = new BitmapImage(new("pack://application:,,,/Now Playing;component/Assets/Placeholder.png"));
                    accentColor = Colors.Black;
                    Background = null;

                    lyrics.Text = lyricsTint.Text = NoLyricsPlaceholder;
                    if (SettingValues.AutoShowLyricsEnabled)
                        lyricsView.Visibility = Visibility.Collapsed;

                    Artist.Text = artistTint.Text = "Artist";
                    artistGrid.Visibility = Visibility.Collapsed;

                    title.Text = titleTint.Text = "Title";
                    titleGrid.Visibility = Visibility.Collapsed;

                    Album.Text = albumTint.Text = "Album";
                    albumGrid.Visibility = Visibility.Collapsed;

                    timeline.Value = 0;
                    togglePlayback.Content = new FontIcon { FontFamily = (FontFamily)Resources["SymbolThemeFontFamily"], Glyph = "\uEA3A" }.ProvideValue(null);
                    Album.Foreground = Artist.Foreground = title.Foreground = lyrics.Foreground = new SolidColorBrush(Colors.Black);

                    return;
                }

                using var stream = (await props.Thumbnail?.OpenReadAsync())?.AsStreamForRead();
                bool thumbnailExists = stream is not null && stream.Length > 0;

                BitmapImage artwork = new();
                artwork.BeginInit();

                if (thumbnailExists)
                {
                    artwork.StreamSource = stream;
                    artwork.EndInit();

                    accentColor = AverageColor(stream);

                    Grid grid = new();
                    grid.Children.Add(new Rectangle { Fill = Brushes.Black });
                    grid.Children.Add(new Image { Source = artwork, Effect = new BlurEffect { Radius = 20 }, Opacity = 0.5 });

                    Background = new VisualBrush(grid)
                    {
                        Stretch = Stretch.UniformToFill,
                        Transform = new ScaleTransform { ScaleX = 2, ScaleY = 2, CenterX = Width / 2, CenterY = Height / 2 }
                    };
                }
                else
                {
                    artwork.UriSource = new("pack://application:,,,/Now Playing;component/Assets/Placeholder.png");
                    artwork.EndInit();

                    Background = null;
                    accentColor = Colors.Black;
                }

                imageSource.Source = artwork;
                Album.Foreground = Artist.Foreground = title.Foreground = lyrics.Foreground = new SolidColorBrush(accentColor);

                if (!string.IsNullOrEmpty(props.Title))
                {
                    titleGrid.Visibility = Visibility.Visible;
                    title.Text = titleTint.Text = props.Title;
                }
                else
                    titleGrid.Visibility = Visibility.Collapsed;

                if (!string.IsNullOrEmpty(props.AlbumTitle))
                {
                    albumGrid.Visibility = Visibility.Visible;
                    Album.Text = albumTint.Text = props.AlbumTitle;
                }
                else
                    albumGrid.Visibility = Visibility.Collapsed;

                if (!string.IsNullOrEmpty(props.Artist))
                {
                    artistGrid.Visibility = Visibility.Visible;
                    Artist.Text = artistTint.Text = props.Artist;
                }
                else if (!string.IsNullOrEmpty(props.AlbumArtist))
                {
                    artistGrid.Visibility = Visibility.Visible;
                    Artist.Text = artistTint.Text = props.AlbumArtist;
                }
                else
                {
                    artistGrid.Visibility = Visibility.Collapsed;
                    Artist.Text = artistTint.Text = string.Empty;
                }

                var songs = await Song.FromSearch(props.Title, Artist.Text);
                if (songs.Length > 0)
                    SetSongLyrics(songs[0]);
                else
                {
                    songs = await Song.FromSearch(props.Title);
                    if (songs.Length > 0)
                        SetSongLyrics(songs[0]);
                    else
                    {
                        lyrics.Text = lyricsTint.Text = NoLyricsPlaceholder;

                        if (SettingValues.AutoShowLyricsEnabled)
                            lyricsView.Visibility = Visibility.Collapsed;
                    }
                }
            });
        }

        private System.Timers.Timer lyricTimer;

        private void TimerEnd(object sender, ElapsedEventArgs e)
        {
            //lyricTimer.Stop();

            //Grid sp = null;
            //this.Dispatcher.Invoke(() => sp = lyricsGrid.Children[currentLyric] as Grid);
            //ScrollToLyric(sp);

            //var alreadyPlayed = currentSong.ParsedLyrics.lyricLines[currentLyric].Timestamp.TotalMilliseconds;
            //++currentLyric;
            //var interval = currentSong.ParsedLyrics.lyricLines[currentLyric].Timestamp.TotalMilliseconds - alreadyPlayed;
            //lyricTimer = new System.Timers.Timer(interval);
            //lyricTimer.Elapsed += TimerEnd;
            //lyricTimer.Start();

            //var timestamp = TimeSpan.FromMilliseconds(alreadyPlayed);
            //string lyricsToDisplay = currentSong.Lyrics.GetTextForTimestamp(timestamp);
            //this.Dispatcher.Invoke(() =>
            //{
            //    lyrics.Text = lyricsTint.Text = lyricsToDisplay;
            //});
        }

        private int currentLyric = 1;

        private void SetSongLyrics(Song song)
        {
            if (song.SyncedLyrics is not null)
            {
                try
                {
                    currentSong = song;
                    lyricLines.Clear();
                    currentLyric = 1;

                    foreach (LyricLine line in song.Lyrics.Lines)
                        //{
                        lyricLines.Add(line);
                    //    if (song.Lyrics.Lines[0] != line)
                    //    {
                    //        this.Dispatcher.Invoke(() =>
                    //        {
                    //            var grid = new Grid() { Tag = line.Timestamp.TotalMilliseconds, Margin = new(0, 10, 0, 0) };

                    //            string text = line.Text;
                    //            if (string.IsNullOrWhiteSpace(text))
                    //                text = "loading placeholder";

                    //            var textBlock = new TextBlock() { Text = text, TextWrapping = TextWrapping.Wrap, FontWeight = System.Windows.FontWeights.Bold, FontSize = 40, Foreground = new SolidColorBrush(accentColor) };
                    //            var textBlockTint = new TextBlock() { Text = text, TextWrapping = TextWrapping.Wrap, FontWeight = System.Windows.FontWeights.Bold, FontSize = 40, Foreground = Brushes.White, Opacity = 0.5 };

                    //            grid.Children.Add(textBlock);
                    //            grid.Children.Add(textBlockTint);

                    //            lyricsGrid.Children.Add(grid);
                    //        });
                    //    }
                    //    else
                    //    {
                    //        this.Dispatcher.Invoke(() =>
                    //        {
                    //            var grid = new Grid() { Tag = line.Timestamp.TotalMilliseconds, Height = 0, Margin = new(0, 0, 0, -10) };

                    //            var textBlock = new TextBlock() { TextWrapping = TextWrapping.Wrap, FontWeight = System.Windows.FontWeights.Bold, FontSize = 40, Foreground = new SolidColorBrush(accentColor) };
                    //            var textBlockTint = new TextBlock() { TextWrapping = TextWrapping.Wrap, FontWeight = System.Windows.FontWeights.Bold, FontSize = 40, Foreground = Brushes.White, Opacity = 0.5 };

                    //            grid.Children.Add(textBlock);
                    //            grid.Children.Add(textBlockTint);

                    //            lyricsGrid.Children.Add(grid);
                    //        });
                    //    }
                    //}

                    //lyricTimer = new(song.Lyrics.Lines[currentLyric].Timestamp.TotalMilliseconds);
                    //lyricTimer.Start();
                    //lyricTimer.Elapsed += TimerEnd;

                    //this.Dispatcher.Invoke(() =>
                    //{
                    //    lyrics.Text = lyricsTint.Text = "";

                    //    if (Settings.AutoShowLyricsEnabled && Settings.LyricsViewViewable)
                    //        lyricsView.Visibility = Visibility.Visible;
                    //});
                }
                catch
                {
                    lyrics.Text = lyricsTint.Text = song.SyncedLyrics;

                    if (SettingValues.AutoShowLyricsEnabled && SettingValues.LyricsViewViewable)
                        lyricsView.Visibility = Visibility.Visible;
                }
            }
            else
            {
                lyrics.Text = lyricsTint.Text = song.PlainLyrics is null ? NoLyricsPlaceholder : song.PlainLyrics;

                if (SettingValues.AutoShowLyricsEnabled)
                    return;

                if (song.PlainLyrics is null)
                    lyricsView.Visibility = Visibility.Collapsed;
                else if (SettingValues.LyricsViewViewable)
                    lyricsView.Visibility = Visibility.Visible;
            }
        }

        private void ScrollToLyric(int index)
        {
            Grid lyricGrid = (Grid)lyricsView.ItemContainerGenerator.ContainerFromIndex(index);

            lyricGrid.BringIntoView(new(new Point(), new Size(lyricGrid.ActualWidth,
                (lyricsView.ActualHeight + (lyricGrid.ActualHeight + 10)) / 2)));

            foreach (Grid grid in lyricsView.Items)
            {
                var tint = grid.Children[1] as TextBlock;

                if (tint.Opacity != 0.5)
                    tint.Opacity = 0.5;
            }

            (lyricGrid.Children[1] as TextBlock).Opacity = 1;
        }

        private void SetLyricsText(string lyrics, bool synced)
        {

        }

        private void ToggleLyricsButton_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = (sender as ToggleButton).IsChecked == true;

            SettingValues.LyricsViewViewable = isChecked;

            if (SettingValues.AutoShowLyricsEnabled)
                lyricsView.Visibility = isChecked && lyrics.Text != NoLyricsPlaceholder ? Visibility.Visible : Visibility.Collapsed;
            else
                lyricsView.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
        }

        SettingsWindow settingsWindow;

        private void OpenSettings_Executed(EventCommand sender, object args)
        {
            settingsWindow?.Close();
            settingsWindow = new() { Owner = this };
            settingsWindow.Show();
        }

        private static Color AverageColor(Stream stream)
        {
            if (!long.TryParse(SettingValues.ImageResolutionThreshold, out long threshold))
                threshold = 1000000;

            if (stream.Length < threshold)
            {
                stream.Seek(0, SeekOrigin.Begin);

                using System.Drawing.Bitmap bmp = new(stream);

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

                int Avg(int c) => c / (width * height);

                red = Avg(red);
                green = Avg(green);
                blue = Avg(blue);
                alpha = Avg(alpha);

                return Color.FromArgb((byte)alpha, (byte)red, (byte)green, (byte)blue);
            }
            else
                return Colors.Black;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
