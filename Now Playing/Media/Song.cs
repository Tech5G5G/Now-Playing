using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Now_Playing.Media;

public class Song
{
    #region JSON Properties

    [JsonPropertyName("id")]
    public string ID { get; }

    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("trackName")]
    public string TrackName { get; }

    [JsonPropertyName("artistName")]
    public string ArtistName { get; }

    [JsonPropertyName("albumName")]
    public string AlbumName { get; }

    [JsonPropertyName("duration")]
    public string Duration { get; }

    [JsonPropertyName("instrumental")]
    public string Instrumental { get; }

    [JsonPropertyName("plainLyrics")]
    public string PlainLyrics { get; }

    [JsonPropertyName("syncedLyrics")]
    public string SyncedLyrics { get; }

    #endregion

    public Lyrics Lyrics { get; }

    public Song()
    {
         if (SyncedLyrics is not null)
            Lyrics = Lyrics.Parse(SyncedLyrics);
    }

    public static async Task<Song> FromSongID(string songID)
    {
        HttpResponseMessage response = await MakeAPIRequest($"/api/get/{songID}");

        return response.IsSuccessStatusCode ? JsonSerializer.Deserialize<Song>(await response.Content.ReadAsStringAsync()) :
            throw new HttpListenerException((int)response.StatusCode, response.ReasonPhrase);
    }

    public static async Task<Song> FromTrackSignature(string trackSignature, bool getCached)
    {
        HttpResponseMessage response = await MakeAPIRequest($"/{(getCached ? "get-cached?" : "get?")}{trackSignature}");

        return response.IsSuccessStatusCode ? JsonSerializer.Deserialize<Song>(await response.Content.ReadAsStringAsync()) :
            throw new HttpListenerException((int)response.StatusCode, response.ReasonPhrase);
    }

    public static async Task<Song[]> FromSearch(string trackName, string artistName = null)
    {
        HttpResponseMessage response = await MakeAPIRequest(
            $"/search?track_name={WebUtility.UrlEncode(trackName).ToLower()}{(artistName is null ? string.Empty : $"&artist_name={WebUtility.UrlEncode(artistName).ToLower()}")}");

        return response.IsSuccessStatusCode ? JsonSerializer.Deserialize<Song[]>(await response.Content.ReadAsStringAsync()) :
            throw new HttpListenerException((int)response.StatusCode, response.ReasonPhrase);
    }

    private const string LRCLIB = "https://lrclib.net/";

    private static Task<HttpResponseMessage> MakeAPIRequest(string path)
    {
        HttpClient client = new()
        {
            Timeout = TimeSpan.FromMinutes(1),
            BaseAddress = new(LRCLIBAPI)
        };

        return client.GetAsync($"/api/{path}");
    }
}
