using Android.Content;
using Android.Database;
using Android.Graphics;
using Android.Provider;
using Android.Support.Design.Widget;
using Android.Support.V7.Preferences;
using Android.Views;
using Android.Widget;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Opus.Api.Services;
using Opus.DataStructure;
using Opus.Fragments;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TagLib;
using YoutubeExplode;
using YoutubeExplode.Models;
using static Android.Provider.MediaStore.Audio;
using CursorLoader = Android.Support.V4.Content.CursorLoader;
using Playlist = Opus.Fragments.Playlist;
using PlaylistItem = Opus.DataStructure.PlaylistItem;

namespace Opus.Api
{
    public class YoutubeManager
    {
        public static YouTubeService YoutubeService;
        public static bool IsUsingAPI = false;

        #region Playback
        /// <summary>
        /// Handle playback of a youtube song
        /// </summary>
        /// <param name="item"></param>
        public static void Play(Song item)
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("YoutubePlay");
            intent.PutExtra("action", "Play");
            intent.PutExtra("file", item.YoutubeID);
            intent.PutExtra("title", item.Title);
            intent.PutExtra("artist", item.Artist);
            intent.PutExtra("thumbnailURI", item.Album);
            intent.PutExtra("addToQueue", true);
            intent.PutExtra("showPlayer", true);
            Android.App.Application.Context.StartService(intent);
        }

        /// <summary>
        /// Add a youtube item to the next slot of the queue. 
        /// </summary>
        /// <param name="item"></param>
        public static void PlayNext(Song item)
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("YoutubePlay");
            intent.PutExtra("action", "PlayNext");
            intent.PutExtra("file", item.YoutubeID);
            intent.PutExtra("title", item.Title);
            intent.PutExtra("artist", item.Artist);
            intent.PutExtra("thumbnailURI", item.Album);
            Android.App.Application.Context.StartService(intent);
        }

        /// <summary>
        /// Add a youtube item to the last slot of the queue. 
        /// </summary>
        /// <param name="item"></param>
        public static void PlayLast(Song item)
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("YoutubePlay");
            intent.PutExtra("action", "PlayLast");
            intent.PutExtra("file", item.YoutubeID);
            intent.PutExtra("title", item.Title);
            intent.PutExtra("artist", item.Artist);
            intent.PutExtra("thumbnailURI", item.Album);
            Android.App.Application.Context.StartService(intent);
        }
        #endregion

        #region Downloader
        /// <summary>
        /// Download every youtube song in the items array.
        /// </summary>
        /// <param name="items"></param>
        public static void Download(Song[] items)
        {
            string[] names = items.ToList().Where(x => x.LocalID == -1 || x.LocalID == 0).ToList().ConvertAll(x => x.Title).ToArray();
            string[] videoIDs = items.ToList().Where(x => x.LocalID == -1 || x.LocalID == 0).ToList().ConvertAll(x => x.YoutubeID).ToArray();

            if(names.Count() > 0)
                DownloadFiles(names, videoIDs);
        }

        /// <summary>
        /// Download songs using there id and there name (for the queue).
        /// </summary>
        /// <param name="names"></param>
        /// <param name="videoIDs"></param>
        public static async void DownloadFiles(string[] names, string[] videoIDs)
        {
            List<DownloadFile> files = new List<DownloadFile>();
            for (int i = 0; i < names.Length; i++)
            {
                if (videoIDs[i] != null && videoIDs[i] != "")
                    files.Add(new DownloadFile(names[i], videoIDs[i], null));
            }

            if(files.Count > 0)
                await DownloadFiles(files);
        }

        public static async Task DownloadFiles(IEnumerable<DownloadFile> files)
        {
            if (!await MainActivity.instance.GetReadPermission())
                return;

            if (!await MainActivity.instance.GetWritePermission())
                return;

            Toast.MakeText(MainActivity.instance, Resource.String.downloading, ToastLength.Short).Show();
            await Downloader.Init();
            Console.WriteLine("&Trying to download " + files.Count());
            Console.WriteLine("&First item: " + files.First().Name);

            Downloader.queue.AddRange(files);
            Downloader.instance.StartDownload();
            Console.WriteLine("&Downloader started");
        }

        /// <summary>
        /// Download a playlist or update the local playlist with new songs.
        /// </summary>
        /// <param name="playlist">The playlist you want to download.</param>
        /// <param name="keepPlaylistSynced">True if you want to add the playlist in the keep synced database (warning, this wont add the playlist to the saved ones) </param>
        /// <param name="showToast">True if you want this method to display that the download has started</param>
        public static async void DownloadPlaylist(PlaylistItem playlist, bool keepPlaylistSynced, bool showToast)
        {
            if (!await MainActivity.instance.WaitForYoutube())
                return;

            if (!await MainActivity.instance.GetReadPermission())
                return;

            if (!await MainActivity.instance.GetWritePermission())
                return;

            if (showToast)
                Toast.MakeText(Android.App.Application.Context, Resource.String.syncing, ToastLength.Short).Show();


            long LocalID = playlist.LocalID != 0 ? playlist.LocalID : await PlaylistManager.GetPlaylistID(playlist.Name);
            if(LocalID == -1)
            {
                ContentValues value = new ContentValues();
                value.Put(Playlists.InterfaceConsts.Name, playlist.Name);
                MainActivity.instance.ContentResolver.Insert(Playlists.ExternalContentUri, value);

                LocalID = await PlaylistManager.GetPlaylistID(playlist.Name);
            }
            playlist.LocalID = LocalID;

            Playlist.instance?.StartSyncing(playlist.Name);

            if (keepPlaylistSynced)
            {
                await Task.Run(() =>
                {
                    SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "SyncedPlaylists.sqlite"));
                    db.CreateTable<PlaylistItem>();
                    db.InsertOrReplace(playlist);
                });
            }

            List<string> names = new List<string>();
            List<string> videoIDs = new List<string>();
            string nextPageToken = "";
            while (nextPageToken != null)
            {
                var ytPlaylistRequest = YoutubeService.PlaylistItems.List("snippet, contentDetails");
                ytPlaylistRequest.PlaylistId = playlist.YoutubeID;
                ytPlaylistRequest.MaxResults = 50;
                ytPlaylistRequest.PageToken = nextPageToken;

                var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                foreach (var item in ytPlaylist.Items)
                {
                    names.Add(item.Snippet.Title);
                    videoIDs.Add(item.ContentDetails.VideoId);
                }

                nextPageToken = ytPlaylist.NextPageToken;
            }

            if (names.Count > 0)
            {
                await Downloader.Init();

                List<DownloadFile> files = new List<DownloadFile>();
                for (int i = 0; i < names.Count; i++)
                {
                    if (videoIDs[i] != null && videoIDs[i] != "")
                        files.Add(new DownloadFile(names[i], videoIDs[i], playlist.Name));
                }

                ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Android.App.Application.Context);
                Downloader.instance.DownloadPlaylist(files, LocalID, prefManager.GetBoolean("keepDeleted", true));
            }
        }

        /// <summary>
        /// Update all playlists that should be synced.
        /// </summary>
        public async static void SyncPlaylists()
        {
            if (!await MainActivity.instance.WaitForYoutube())
                return;

            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(MainActivity.instance);
            DateTime lastSync = DateTime.Parse(prefManager.GetString("syncDate", DateTime.MinValue.ToString()));

            if (lastSync.AddHours(1) > DateTime.Now || !MainActivity.instance.HasWifi()) //Make a time check, do not check if the user is downloading or if the user has just started the app two times
                return;

            ISharedPreferencesEditor editor = prefManager.Edit();
            editor.PutString("syncDate", DateTime.Now.ToString());

            List<PlaylistItem> SyncedPlaylists = new List<PlaylistItem>();
            await Task.Run(() =>
            {
                SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "SyncedPlaylists.sqlite"));
                db.CreateTable<PlaylistItem>();

                SyncedPlaylists = db.Table<PlaylistItem>().ToList();
            });

            foreach (PlaylistItem item in SyncedPlaylists)
            {
                if (item.YoutubeID != null)
                {
                    DownloadPlaylist(item, false, false);
                }
            }
        }
        #endregion

        #region PlaylistsImplementation
        /// <summary>
        /// Return true if the youtube song of id audioID is contained in the youtube playlist with the id of playlistID.
        /// </summary>
        /// <param name="audioID"></param>
        /// <param name="playlistID"></param>
        /// <returns></returns>
        public async static Task<bool> SongIsContained(string audioID, string playlistID)
        {
            try
            {
                var request = YoutubeService.PlaylistItems.List("snippet, contentDetails");
                request.PlaylistId = playlistID;
                request.VideoId = audioID;
                request.MaxResults = 1;

                var response = await request.ExecuteAsync();
                if (response.Items.Count > 0)
                    return true;
            }
            catch (System.Net.Http.HttpRequestException) { }
            return false;
        }


        /// <summary>
        /// Add an array of youtube song to a playlist.
        /// </summary>
        /// <param name="items">The array of songs you want to add to the playlist. Will only add youtube file, if you input local file in this array, they will be ignored</param>
        /// <param name="PlaylistYtID">The id of the youtube playlist</param>
        public async static void AddToPlaylist(Song[] items, string PlaylistYtID)
        {
            Google.Apis.YouTube.v3.Data.PlaylistItem playlistItem = new Google.Apis.YouTube.v3.Data.PlaylistItem();
            PlaylistItemSnippet snippet = new PlaylistItemSnippet
            {
                PlaylistId = PlaylistYtID
            };

            foreach (Song item in items)
            {
                if (item != null && item.YoutubeID != null)
                {
                    try
                    {
                        ResourceId resourceId = new ResourceId
                        {
                            Kind = "youtube#video",
                            VideoId = item.YoutubeID
                        };
                        snippet.ResourceId = resourceId;
                        playlistItem.Snippet = snippet;

                        var insertRequest = YoutubeService.PlaylistItems.Insert(playlistItem, "snippet");
                        await insertRequest.ExecuteAsync();
                    }
                    catch (System.Net.Http.HttpRequestException)
                    {
                        MainActivity.instance.Timout();
                    }
                }
            }
        }
        #endregion

        #region Mix
        /// <summary>
        /// Add a list of related songs to the queue.
        /// </summary>
        /// <param name="item"></param>
        public static async void CreateMixFromSong(Song item)
        {
            bool AddItemToQueue = true;
            if (MusicPlayer.queue.Count == 0)
            {
                SongManager.Play(item);
                AddItemToQueue = false;
            }

            ProgressBar parseProgress = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress);
            parseProgress.Visibility = ViewStates.Visible;
            parseProgress.ScaleY = 6;

            if (!await MainActivity.instance.WaitForYoutube())
            {
                Snackbar snackBar = Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), Resource.String.youtube_loading_error, Snackbar.LengthLong);
                snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                snackBar.Show();
                return;
            }

            List<Song> tracks = new List<Song>();
            try
            {
                YoutubeClient client = new YoutubeClient();
                var video = await client.GetVideoAsync(item.YoutubeID);

                var ytPlaylistRequest = YoutubeService.PlaylistItems.List("snippet, contentDetails");
                ytPlaylistRequest.PlaylistId = video.GetVideoMixPlaylistId();
                ytPlaylistRequest.MaxResults = 25;

                var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                foreach (var ytItem in ytPlaylist.Items)
                {
                    if (ytItem.Snippet.Title != "[Deleted video]" && ytItem.Snippet.Title != "Private video" && ytItem.Snippet.Title != "Deleted video" && !MusicPlayer.queue.Exists(x => x.YoutubeID == ytItem.ContentDetails.VideoId))
                    {
                        Song song = new Song(WebUtility.HtmlDecode(ytItem.Snippet.Title), ytItem.Snippet.ChannelTitle, ytItem.Snippet.Thumbnails.High.Url, ytItem.ContentDetails.VideoId, -1, -1, ytItem.ContentDetails.VideoId, true, false)
                        {
                            ChannelID = ytItem.Snippet.ChannelId
                        };
                        tracks.Add(song);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is System.Net.Http.HttpRequestException)
                    MainActivity.instance.Timout();
                else
                    MainActivity.instance.UnknowError(ErrorCode.SM1);

                return;
            }

            Random r = new Random();
            tracks = tracks.OrderBy(x => r.Next()).ToList();
            if (AddItemToQueue && !MusicPlayer.queue.Exists(x => x.YoutubeID == item.YoutubeID))
                tracks.Add(item);

            Intent intent = new Intent(MainActivity.instance, typeof(MusicPlayer));
            MainActivity.instance.StartService(intent);

            while (MusicPlayer.instance == null)
                await Task.Delay(100);

            MusicPlayer.instance.AddToQueue(tracks);
            MainActivity.instance.ShowPlayer();
            Home.instance?.RefreshQueue();
            Queue.instance?.Refresh();
            parseProgress.Visibility = ViewStates.Gone;
        }

        /// <summary>
        /// Play songs from the channel of your choice.
        /// </summary>
        /// <param name="ChannelID"></param>
        public async static void MixFromChannel(string ChannelID)
        {
            if (!await MainActivity.instance.WaitForYoutube())
                return;

            List<Song> songs = new List<Song>();
            try
            {
                SearchResource.ListRequest searchRequest = YoutubeService.Search.List("snippet");
                searchRequest.Fields = "items(id/videoId,snippet/title,snippet/thumbnails/high/url,snippet/channelTitle)";
                searchRequest.Type = "video";
                searchRequest.ChannelId = ChannelID;
                searchRequest.MaxResults = 20;
                var searchReponse = await searchRequest.ExecuteAsync();


                foreach (var video in searchReponse.Items)
                {
                    Song song = new Song(WebUtility.HtmlDecode(video.Snippet.Title), video.Snippet.ChannelTitle, video.Snippet.Thumbnails.High.Url, video.Id.VideoId, -1, -1, null, true, false)
                    {
                        ChannelID = video.Snippet.ChannelId
                    };
                    songs.Add(song);
                }
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
            }

            Random r = new Random();
            songs = songs.OrderBy(x => r.Next()).ToList();
            SongManager.Play(songs[0]);
            songs.RemoveAt(0);

            while (MusicPlayer.instance == null)
                await Task.Delay(10);

            MusicPlayer.instance.AddToQueue(songs);
        }
        #endregion

        #region Metadata
        /// <summary>
        /// Return the thumbnail with the greatest quality.
        /// </summary>
        /// <param name="thumbnails">This will create an array of the thumbnails in this order: MaxResUrl, StandardResUrl, HighResUrl</param>
        /// <returns></returns>
        public async static Task<string> GetBestThumb(ThumbnailSet Thumbnails)
        {
            string[] thumbnails = new string[] { Thumbnails.MaxResUrl, Thumbnails.StandardResUrl, Thumbnails.HighResUrl };
            foreach (string thumb in thumbnails)
            {
                HttpWebRequest request = new HttpWebRequest(new Uri(thumb))
                {
                    Method = "HEAD"
                };
                try
                {
                    HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();

                    if (response.StatusCode == HttpStatusCode.OK)
                        return thumb;
                }
                catch (WebException) { }
            }

            return thumbnails.Last();
        }


        /// <summary>
        /// Return the local path of a youtube video (if downloaded). If the video is not downloaded, return null.
        /// </summary>
        /// <param name="videoID"></param>
        /// <returns></returns>
        public static string GetLocalPathFromYTID(string videoID)
        {
            Android.Net.Uri musicUri = MediaStore.Audio.Media.ExternalContentUri;
            CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

            if (musicCursor != null && musicCursor.MoveToFirst())
            {
                int pathKey = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Data);
                do
                {
                    string path = musicCursor.GetString(pathKey);

                    try
                    {
                        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                        var meta = TagLib.File.Create(new StreamFileAbstraction(path, stream, stream));
                        string ytID = meta.Tag.Comment;
                        stream.Dispose();

                        if (ytID == videoID)
                        {
                            musicCursor.Close();
                            return path;
                        }
                    }
                    catch (CorruptFileException)
                    {
                        continue;
                    }
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }

            return null;
        }
        #endregion
    }
}