using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Flow.Launcher.Plugin.Cider2
{

    /// <inheritdoc />
    public class Cider2 : IPlugin
    {
        private const string ApiBase = @"http://localhost:10767/api/v1/playback";
        /// <summary>
        /// Temporary icon path generated from the artwork
        /// </summary>
        private const string TmpIcon = "Images/tmp.png";
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
            try
            {
                // if (!GetMethod("active"))
                if (!IsCiderRunning())
                {
                    return new List<Result>
                    {
                        new()
                        {
                            Title = "Cider2 is not active",
                            SubTitle = "Please start Cider2",
                            IcoPath = CiderIcon
                        }
                    };
                }

            }
            catch (Exception)
            {
                return new List<Result>
                {
                    new()
                    {
                        Title = "Cider2 is not active",
                        SubTitle = "Please start Cider2",
                        IcoPath = CiderIcon
                    }
                };
            }
            var playback = GetPlayback();
            _context.API.LogDebug("Cider2", "Got playback info");
            _context.API.LogWarn("Cider2", "Got playback info");
            var res = new List<Result>();

            if (playback == null)
            {
                res.Add(new Result
                {
                    Title = "No music is playing",
                    SubTitle = "Please start playing music",
                    IcoPath = CiderIcon
                });
                return res;
            }
            res.Add(new Result
            {
                // TODO: Use the artwork as the icon
                SubTitle = $"{playback.ArtistName} - {playback.Name}",
                Title = "Play/Pause",
                IcoPath = TmpIcon,
                Action = c => PostMethod("playpause")
            });
            res.Add(new Result
            {
                Title = "Next Track",
                SubTitle = "Next track",
                IcoPath = CiderIcon,
                Action = c => PostMethod("next")
            });
            res.Add(new Result
            {
                Title = "Previous",
                SubTitle = "Previous track",
                IcoPath = CiderIcon,
                Action = c => PostMethod("previous")
            });
            if (!playback.InLibrary)
            {
                res.Add(new Result
                {
                    Title = "Add",
                    SubTitle = "Add to library",
                    IcoPath = CiderIcon,
                    Action = c => PostMethod("add-to-library")
                });
            }
            if (!playback.InFavourite)
            {
                res.Add(new Result
                {
                    Title = "Favourite",
                    SubTitle = "Add to favourites",
                    IcoPath = CiderIcon,
                    Action = c => ToggleFavourite(1)
                });
                res.Add(new Result
                {
                    Title = "Dislike",
                    SubTitle = "Mark as less suggested",
                    IcoPath = CiderIcon,
                    Action = c => ToggleFavourite(-1)
                });
            }
            else
            {
                res.Add(new Result
                {
                    Title = "Remove",
                    SubTitle = "Remove from favourites",
                    IcoPath = CiderIcon,
                    Action = c => ToggleFavourite(0)
                });
            }
            if (!playback.RepeatMode)
            {
                res.Add(new Result
                {
                    Title = "Repeat mode",
                    SubTitle = "Toggle repeat mode",
                    IcoPath = CiderIcon,
                    Action = c => PostMethod("toggle-repeat")
                });
            }

            if (!playback.ShuffleMode)
            {
                res.Add(new Result
                {
                    Title = "Shuffle mode",
                    SubTitle = "Toggle shuffle mode",
                    IcoPath = CiderIcon,
                    Action = c => PostMethod("toggle-shuffle")
                });
            }
            return res;
        }

        private static bool IsCiderRunning()
        {
            Process[] processes = Process.GetProcessesByName("Cider");
            return processes.Length > 0;
        }
        // private static bool GetMethod(string method)
        // {
        //     using HttpClient client = new();
        //     var response = client.GetAsync($"{ApiBase}/{method}").Result;
        //     return response.IsSuccessStatusCode;
        // }

        private static bool PostMethod(string method)
        {
            using HttpClient client = new();
            var response = client.PostAsync($"{ApiBase}/{method}", null).Result;
            return response.IsSuccessStatusCode;
        }
        /// <param name="ratingValue"><br/>
        /// 1 for favourite <br/>
        /// 0 for not favourite <br/>
        /// -1 for less suggested
        /// </param>
        /// <returns></returns>
        private static bool ToggleFavourite(int ratingValue)
        {
            using HttpClient client = new();
            var ratingData = new { rating = ratingValue };
            var json = JsonSerializer.Serialize(ratingData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = client.PostAsync($"{ApiBase}/set-rating", content).Result;
            return response.IsSuccessStatusCode;
        }
        // private static async Task<(bool repeatMode, bool ShuffleMode)> GetModesAsync()
        // {
        //     using HttpClient client = new();
        //     var repeatTask = client.GetAsync($"{ApiBase}/repeat-mode");
        //     var shuffleTask = client.GetAsync($"{ApiBase}/shuffle-mode");
        //     var results = await Task.WhenAll(repeatTask, shuffleTask);
        //     return (results[0].IsSuccessStatusCode, results[1].IsSuccessStatusCode);
        // }

        private Playback GetPlayback()
        {
            using HttpClient client = new();
            var response = client.GetAsync($"{ApiBase}/now-playing").Result;
            if (!response.IsSuccessStatusCode) return null;
            var content = response.Content.ReadAsStringAsync().Result;
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            var info = root.GetProperty("info");
            try
            {
                var artworkUrl = info.GetProperty("artwork").GetProperty("url").GetString();
                var artistName = info.GetProperty("artistName").GetString();
                var name = info.GetProperty("name").GetString();
                var albumName = info.GetProperty("albumName").GetString();
                var shuffleMode = info.GetProperty("shuffleMode").GetInt32() == 1;
                var repeatMode = info.GetProperty("repeatMode").GetInt32() == 1;
                var inLibrary = info.GetProperty("inLibrary").GetBoolean();
                var inFavourite = info.GetProperty("inFavorites").GetBoolean();
                try
                {
                    using HttpClient imgClient = new();
                    byte[] imgBytes = imgClient.GetByteArrayAsync(artworkUrl).Result;
                    File.WriteAllBytes(TmpIcon, imgBytes);
                }
                catch (Exception e)
                {
                    _context.API.LogWarn("Cider2", e.ToString());
                }
                return new Playback
                {
                    ArtworkUrl = artworkUrl,
                    ArtistName = artistName,
                    Name = name,
                    AlbumName = albumName,
                    ShuffleMode = shuffleMode,
                    RepeatMode = repeatMode,
                    InLibrary = inLibrary,
                    InFavourite = inFavourite
                };
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
    /// <summary>
    /// This class represents the current playback information
    /// </summary>
    public class Playback
    {

        public string ArtworkUrl { get; set; }
        public string ArtistName { get; set; }
        public string Name { get; set; }
        public string AlbumName { get; set; }
        public bool ShuffleMode { get; set; }
        public bool RepeatMode { get; set; }
        public bool InLibrary { get; set; }
        public bool InFavourite { get; set; }
    }
}
