using System.ComponentModel;

namespace Now_Playing.Media;

public class Lyrics : IParsable<Lyrics>
{
    private const int TimestampLength = 9;

    public LyricLine[] Lines => lines;
    private LyricLine[] lines;

    public string GetTextForTimestamp(TimeSpan timestamp) => lines[GetIndexForTimestamp(timestamp)].Text;

    public int GetIndexForTimestamp(TimeSpan timestamp)
    {
        for (int i = 1; i < lines.Length; ++i)
        {
            if (timestamp < lines[i].Timestamp)
                return i - 1;
        }

        return lines.Length - 1;
    }

    public static Lyrics Parse(string syncedLyrics) => Parse(syncedLyrics, null);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Lyrics Parse(string syncedLyrics, IFormatProvider format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(syncedLyrics, nameof(syncedLyrics));

        if (!TryParse(syncedLyrics, format, out Lyrics lyrics))
            throw new FormatException();

        return lyrics;
    }

    public static bool TryParse(string syncedLyrics, out Lyrics lyrics) => TryParse(syncedLyrics, null, out lyrics);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool TryParse(string syncedLyrics, IFormatProvider format, out Lyrics lyrics)
    {
        string[] lines = syncedLyrics.Split('\n');
        Lyrics result = new() { lines = new LyricLine[lines.Length + 1] };

        // Empty entry for start of the song
        result.lines[0] = new(TimeSpan.Zero, string.Empty);

        int lineIndex = 1;
        foreach (string line in lines)
        {
            string timestampString = line[1..TimestampLength];

            string text = string.Empty;
            if (line.Length > TimestampLength + 1)
                text = line[(TimestampLength + 2)..];

            if (!TimeSpan.TryParse("00:" + timestampString, out TimeSpan timestamp))
            {
                lyrics = null;
                return false;
            }

            // Round the timestamp down to avoid text showing up too late
            // (it can still show up too early)
            timestamp = TimeSpan.FromSeconds(Math.Floor(timestamp.TotalSeconds));

            result.lines[lineIndex] = new(timestamp, text);
            ++lineIndex;
        }

        lyrics = result;
        return true;
    }
}

public class LyricLine(TimeSpan timestamp, string text)
{
    public TimeSpan Timestamp { get; } = timestamp;
    public string Text { get; } = text;
}
