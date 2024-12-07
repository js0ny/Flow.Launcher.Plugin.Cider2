using System;
using System.Collections.Generic;
using System.Net.Http;
using Flow.Launcher.Plugin;
using System.Text.Json;
using System.Net;
using System.Windows;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.Cider2
{

    public class Cider2 : IPlugin
    {
        private const string ApiBase = @"http://localhost:10767/api/v1/playback";
        /// <summary>
        /// Temporary icon path generated from the artwork
        /// </summary>
        public const string TmpIcon = "Images/tmp.png";
        private const string CiderIcon = "Images/icon.png";
        private PluginInitContext _context;

        /// <inheritdoc />
        public void Init(PluginInitContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public List<Result> Query(Query query)
        {
            if (!GetMethod("active"))
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "Cider2 is not active",
                        SubTitle = "Please start Cider2",
                        IcoPath = "Images/icon.png"
                    }
                };
            }
            var playback = GetPlayback();
            _context.API.LogDebug("Cider2", "Got playback info");
            _context.API.LogWarn("Cider2", "Got playback info");
            if (playback == null)
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "No music is playing",
                        SubTitle = "Please start playing music",
                        IcoPath = CiderIcon
                    }
                };
            }
            var res = new List<Result>();
            var currentPlaying = new Result
            {
                Title = $"{playback.ArtistName} - {playback.Name}",
                SubTitle = "Click me to toggle play/pause",
                IcoPath = TmpIcon,
                Action = c => GetMethod("playpause")
            };
            res.Add(currentPlaying);
            return res;
        }

        private static bool GetMethod(string method)
        {
            using HttpClient client = new();
            var response = client.GetAsync($"{ApiBase}/{method}").Result;
            return response.IsSuccessStatusCode;
        }

        private static Playback GetPlayback()
        {
            using HttpClient client = new();
            var response = client.GetAsync($"{ApiBase}/now-playing").Result;
            if (!response.IsSuccessStatusCode) return null;
            var content = response.Content.ReadAsStringAsync().Result;
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            var info = root.GetProperty("info");
            var artworkUrl = info.GetProperty("artwork").GetProperty("url").GetString();
            var artistName = info.GetProperty("artistName").GetString();
            var name = info.GetProperty("name").GetString();
            var albumName = info.GetProperty("albumName").GetString();
            using HttpClient imgClient = new();
            byte[] imgBytes = imgClient.GetByteArrayAsync(artworkUrl).Result;
            File.WriteAllBytes(TmpIcon, imgBytes);
            return new Playback
            {
                ArtworkUrl = artworkUrl,
                ArtistName = artistName,
                Name = name,
                AlbumName = albumName
            };
        }
    }
    /// <summary>
    /// This class represents the current playback information
    /// </summary>
    public class Playback
    {

        /// <summary>
        /// Artwork URL, used to generate the icon
        /// </summary>
        public string ArtworkUrl { get; set; }
        public string ArtistName { get; set; }
        public string Name { get; set; }
        public string AlbumName { get; set; }
    }
}