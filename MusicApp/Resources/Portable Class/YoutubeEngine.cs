using Android.Content;
using Android.Database;
using Android.OS;
using Android.Preferences;
using Android.Provider;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Java.Util;
using MusicApp.Resources.values;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TagLib;
using YoutubeExplode;
using YoutubeExplode.Models.MediaStreams;

namespace MusicApp.Resources.Portable_Class
{
    public class YoutubeEngine : ListFragment
    {
        public static YoutubeEngine instance;
        public static YouTubeService youtubeService;
        public static List<Song> result;

        private View emptyView;
        private bool isEmpty = true;
        private string[] actions = new string[] { "Play", "Play Next", "Play Last", "Add To Playlist", "Download" };
        private string searchKeyWorld;

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            ListView.ItemClick += ListView_ItemClick;
            ListView.ItemLongClick += ListView_ItemLongClick;
            ListView.Scroll += MainActivity.instance.Scroll;

            emptyView = LayoutInflater.Inflate(Resource.Layout.DownloadLayout, null);
            ListView.EmptyView = emptyView;
            if(result != null)
            {
                ListAdapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, result);
            }
            else
            {
                ListAdapter = null;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }

            if(youtubeService == null)
                MainActivity.instance.Login();

            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
        }

        public override void OnDestroy()
        {
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
            if (isEmpty)
            {
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(emptyView);
            }
            base.OnDestroy();
            instance = null;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = base.OnCreateView(inflater, container, savedInstanceState);
            view.SetPadding(0, 0, 0, MainActivity.paddingBot);
            return view;
        }

        public static Fragment NewInstance()
        {
            instance = new YoutubeEngine { Arguments = new Bundle() };
            return instance;
        }

        public async Task Search(string search, bool loadingBar = true)
        {
            if (search == null || search == "")
                return;

            searchKeyWorld = search;

            View empty = null;
            if (loadingBar)
            {
                ListAdapter = null;
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(emptyView);
                empty = LayoutInflater.Inflate(Resource.Layout.EmptyLoadingLayout, null);
                ListView.EmptyView = empty;
                Activity.AddContentView(empty, View.LayoutParameters);
            }

            if (MainActivity.instance.TokenHasExpire())
            {
                youtubeService = null;
                MainActivity.instance.Login();

                while (youtubeService == null)
                    await Task.Delay(500);
            }

            SearchResource.ListRequest searchResult = youtubeService.Search.List("snippet");
            searchResult.Fields = "items(id/videoId,snippet/title,snippet/thumbnails/high/url,snippet/channelTitle)";
            searchResult.Q = search;
            searchResult.Type = "video";
            searchResult.MaxResults = 20;

            var searchReponse = await searchResult.ExecuteAsync();

            result = new List<Song>();

            foreach(var video in searchReponse.Items)
            {
                System.Console.WriteLine("&" + video.Snippet.Title);
                Song videoInfo = new Song(video.Snippet.Title, video.Snippet.ChannelTitle, video.Snippet.Thumbnails.High.Url, video.Id.VideoId, -1, -1, video.Id.VideoId, true);
                result.Add(videoInfo);
            }

            if (loadingBar)
            {
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(empty);
                ListView.EmptyView = emptyView;
            }

            ListAdapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, result);
        }

        private async void OnRefresh(object sender, System.EventArgs e)
        {
            await Refresh();
            MainActivity.instance.contentRefresh.Refreshing = false;    
        }

        public async Task Refresh()
        {
            await Search(searchKeyWorld, false);
        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            Play(result[e.Position].GetPath(), result[e.Position].GetName(), result[e.Position].GetArtist(), result[e.Position].GetAlbum());
        }

        private void ListView_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            Song item = result[e.Position];
            More(item);
        }

        public void More(Song item)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Pick an action");
            builder.SetItems(actions, (senderAlert, args) =>
            {
                switch (args.Which)
                {
                    case 0:
                        Play(item.GetPath(), item.GetName(), item.GetArtist(), item.GetAlbum());
                        break;
                    case 1:
                        PlayNext(item.GetPath(), item.GetName(), item.GetArtist(), item.GetAlbum());
                        break;
                    case 2:
                        PlayLast(item.GetPath(), item.GetName(), item.GetArtist(), item.GetAlbum());
                        break;
                    case 3:
                        GetPlaylists(item.GetPath(), Activity);
                        break;
                    case 4:
                        Download(item.GetName(), item.GetPath());
                        break;
                    default:
                        break;
                }
            });
            builder.Show();
        }

        public static async void Play(string videoID, string title, string artist, string thumbnailURL, bool skipExistVerification = false)
        {
            if (!skipExistVerification && FileIsAlreadyDownloaded(videoID))
            {
                Context context = Android.App.Application.Context;
                Intent mIntent = new Intent(context, typeof(MusicPlayer));
                mIntent.PutExtra("file", GetLocalPathFromYTID(videoID));
                mIntent.SetAction("Play");
                context.StartService(mIntent);
                int localID = MusicPlayer.queue.Count;

                Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), title + " has been downloaded on your device. Playing the local file instead of the online one.", Snackbar.LengthShort).SetAction("Play the youtube file anyway.", (v) =>
                {
                    Queue.RemoveFromQueue(MusicPlayer.queue[localID]);
                    Play(videoID, title, artist, thumbnailURL, true);
                }).Show();
            }

            ProgressBar parseProgress = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress);
            parseProgress.Visibility = ViewStates.Visible;
            parseProgress.ScaleY = 6;

            YoutubeClient client = new YoutubeClient();
            var mediaStreamInfo = await client.GetVideoMediaStreamInfosAsync(videoID);
            AudioStreamInfo streamInfo = mediaStreamInfo.Audio.OrderBy(s => s.Bitrate).Last();

            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.PutExtra("file", streamInfo.Url);
            intent.PutExtra("title", title);
            intent.PutExtra("artist", artist);
            intent.PutExtra("youtubeID", videoID);
            intent.PutExtra("thumbnailURI", thumbnailURL);
            Android.App.Application.Context.StartService(intent);

            parseProgress.Visibility = ViewStates.Gone;
            MainActivity.instance.HideTabs();
            MainActivity.instance.HideSearch();
            MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, Player.NewInstance()).Commit();
        }

        public static async void PlayFiles(Song[] files, bool skipExistVerification = false)
        {
            if (files.Length < 1)
                return;

            if (!skipExistVerification)
            {
                List<int> index = new List<int>();
                List<Song> downloadedSong = new List<Song>();
                for (int i = 0; i < files.Length; i++)
                {
                    if (FileIsAlreadyDownloaded(files[i].youtubeID))
                    {
                        index.Add(i);
                        downloadedSong.Add(files[i]);
                    }
                }

                if (downloadedSong.Count > 0)
                {
                    List<Song> filesList = files.ToList();

                    for (int i = 0; i < index.Count; i++)
                    {
                        filesList.RemoveAt(index[i]);
                    }

                    files = filesList.ToArray();

                    if (downloadedSong.Count == 1)
                    {
                        Context context = Android.App.Application.Context;
                        Intent intent = new Intent(context, typeof(MusicPlayer));
                        intent.PutExtra("file", GetLocalPathFromYTID(downloadedSong[0].youtubeID));
                        intent.SetAction("PlayLast");
                        context.StartService(intent);
                        int localID = MusicPlayer.queue.Count;

                        Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), downloadedSong[0].GetName() + " has been downloaded on your device. Playing the local file instead of the online one.", Snackbar.LengthShort).SetAction("Play the youtube file anyway.", (v) =>
                        {
                            Queue.RemoveFromQueue(MusicPlayer.queue[localID]);
                            MusicPlayer.queue.Insert(MusicPlayer.queue.Count, downloadedSong[0]);
                        }).Show();
                    }
                    else
                    {
                        List<string> localPaths = new List<string>();
                        for(int i = 0; i < downloadedSong.Count; i++)
                            localPaths.Add(GetLocalPathFromYTID(downloadedSong[i].youtubeID));

                        Context context = Android.App.Application.Context;
                        Intent intent = new Intent(context, typeof(MusicPlayer));
                        intent.PutStringArrayListExtra("files", localPaths.ToArray());
                        intent.PutExtra("clearQueue", false);
                        intent.SetAction("RandomPlay");
                        context.StartService(intent);
                        int localID = MusicPlayer.queue.Count;

                        Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), downloadedSong.Count + " files are on your device. Playing locals files instead of the online version.", Snackbar.LengthShort).SetAction("Play youtube files anyway", (v) =>
                        {
                            for (int i = 0; i < downloadedSong.Count; i++)
                            {
                                Queue.RemoveFromQueue(MusicPlayer.queue[localID + i]);
                                MusicPlayer.queue.Insert(MusicPlayer.CurrentID() + i, downloadedSong[i]);
                            }

                        }).Show();
                    }
                }
            }

            Play(files[0].GetPath(), files[0].GetName(), files[0].GetArtist(), files[0].GetAlbum(), true);

            if (files.Length < 2)
                return;

            await Task.Delay(5000);

            for(int i = 1; i < files.Length; i++)
            {
                MusicPlayer.queue.Insert(MusicPlayer.CurrentID() + i, files[i]);
            }
        }


        public static async void PlayNext(string videoID, string title, string artist, string thumbnailURL, bool skipExistVerification = false)
        {
            if (!skipExistVerification && FileIsAlreadyDownloaded(videoID))
            {
                Context context = Android.App.Application.Context;
                Intent mIntent = new Intent(context, typeof(MusicPlayer));
                mIntent.PutExtra("file", GetLocalPathFromYTID(videoID));
                mIntent.SetAction("PlayNext");
                context.StartService(mIntent);
                int localID = MusicPlayer.queue.Count;

                Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), title + " has been downloaded on your device. Playing the local file instead of the online one.", Snackbar.LengthShort).SetAction("Play the youtube file anyway.", (v) =>
                {
                    Queue.RemoveFromQueue(MusicPlayer.queue[localID]);
                    PlayNext(videoID, title, artist, thumbnailURL, true);
                }).Show();
            }

            ProgressBar parseProgress = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress);
            parseProgress.Visibility = ViewStates.Visible;
            parseProgress.ScaleY = 6;

            YoutubeClient client = new YoutubeClient();
            var mediaStreamInfo = await client.GetVideoMediaStreamInfosAsync(videoID);
            AudioStreamInfo streamInfo = mediaStreamInfo.Audio.OrderBy(s => s.Bitrate).Last();

            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("PlayNext");
            intent.PutExtra("file", streamInfo.Url);
            intent.PutExtra("title", title);
            intent.PutExtra("artist", artist);
            intent.PutExtra("youtubeID", videoID);
            intent.PutExtra("thumbnailURI", thumbnailURL);
            Android.App.Application.Context.StartService(intent);

            parseProgress.Visibility = ViewStates.Gone;
        }

        public static async void PlayLast(string videoID, string title, string artist, string thumbnailURL, bool skipExistVerification = false)
        {
            if (!skipExistVerification && FileIsAlreadyDownloaded(videoID))
            {
                Context context = Android.App.Application.Context;
                Intent mIntent = new Intent(context, typeof(MusicPlayer));
                mIntent.PutExtra("file", GetLocalPathFromYTID(videoID));
                mIntent.SetAction("PlayLast");
                context.StartService(mIntent);
                int localID = MusicPlayer.queue.Count;

                Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), title + " has been downloaded on your device. Playing the local file instead of the online one.", Snackbar.LengthShort).SetAction("Play the youtube file anyway.", (v) =>
                {
                    Queue.RemoveFromQueue(MusicPlayer.queue[localID]);
                    PlayLast(videoID, title, artist, thumbnailURL, true);
                }).Show();
            }


            ProgressBar parseProgress = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress);
            parseProgress.Visibility = ViewStates.Visible;
            parseProgress.ScaleY = 6;

            YoutubeClient client = new YoutubeClient();
            var mediaStreamInfo = await client.GetVideoMediaStreamInfosAsync(videoID);
            AudioStreamInfo streamInfo = mediaStreamInfo.Audio.OrderBy(s => s.Bitrate).Last();

            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("PlayLast");
            intent.PutExtra("file", streamInfo.Url);
            intent.PutExtra("title", title);
            intent.PutExtra("artist", artist);
            intent.PutExtra("youtubeID", videoID);
            intent.PutExtra("thumbnailURI", thumbnailURL);
            Android.App.Application.Context.StartService(intent);

            parseProgress.Visibility = ViewStates.Gone;
        }

        public async static void Download(string name, string videoID, bool skipExistVerification = false)
        {
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Android.App.Application.Context);
            if (prefManager.GetString("downloadPath", null) != null)
            {
                if (FileIsAlreadyDownloaded(videoID) && !skipExistVerification)
                {
                    Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), name + " is already on your device.", Snackbar.LengthShort).SetAction("Download Anyway", (v) =>
                    {
                        Download(name, videoID, true);
                    }).Show();
                }

                Toast.MakeText(Android.App.Application.Context, "Downloading...", ToastLength.Short).Show();
                Context context = Android.App.Application.Context;
                Intent intent = new Intent(context, typeof(Downloader));
                context.StartService(intent);

                while (Downloader.instance == null)
                    await Task.Delay(10);

                Downloader.instance.downloadPath = prefManager.GetString("downloadPath", null);
                Downloader.instance.Download(new DownloadFile(name, videoID));
            }
            else
            {
                Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), "Download Path Not Set.", Snackbar.LengthShort).SetAction("Set Path", (v) =>
                {
                    Intent intent = new Intent(Android.App.Application.Context, typeof(Preferences));
                    MainActivity.instance.StartActivity(intent);
                }).Show();
            }
        }

        public static async void DownloadFiles(string[] names, string[] videoIDs, bool skipExistVerification = false)
        {
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Android.App.Application.Context);
            if (prefManager.GetString("downloadPath", null) != null)
            {
                if (!skipExistVerification)
                {
                    List<string> downloadedName = new List<string>();
                    List<string> downloadedID = new List<string>();
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (FileIsAlreadyDownloaded(videoIDs[i]))
                        {
                            downloadedName.Add(names[i]);
                            downloadedID.Add(videoIDs[i]);
                        }
                    }

                    if (downloadedName.Count > 0)
                    {
                        List<string> namesList = names.ToList();
                        List<string> idList = videoIDs.ToList();

                        for(int i = 0; i < downloadedName.Count; i++)
                        {
                            namesList.Remove(downloadedName[i]);
                            idList.Remove(downloadedID[i]);
                        }

                        names = namesList.ToArray();
                        videoIDs = idList.ToArray();

                        if (downloadedName.Count == 1)
                        {
                            Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), downloadedName[0] + " is already on your device.", Snackbar.LengthShort).SetAction("Download this file anyway", (v) =>
                            {
                                Downloader.instance.Download(new DownloadFile(downloadedName[0], downloadedID[0]));
                            }).Show();
                        }
                        else
                        {
                            Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), downloadedName.Count + " files are already on your device", Snackbar.LengthShort).SetAction("Download all this files anyway", (v) =>
                            {
                                for(int i = 0; i < downloadedName.Count; i++)
                                    Downloader.instance.Download(new DownloadFile(downloadedName[i], downloadedID[i]));

                            }).Show();
                        }
                    }
                }

                Toast.MakeText(Android.App.Application.Context, "Downloading...", ToastLength.Short).Show();
                Context context = Android.App.Application.Context;
                Intent intent = new Intent(context, typeof(Downloader));
                context.StartService(intent);

                while (Downloader.instance == null)
                    await Task.Delay(10);

                Downloader.instance.downloadPath = prefManager.GetString("downloadPath", null);

                for(int i = 0; i < names.Length; i++)
                    Downloader.instance.Download(new DownloadFile(names[i], videoIDs[i]));
            }
            else
            {
                Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.contentView), "Download Path Not Set.", Snackbar.LengthShort).SetAction("Set Path", (v) =>
                {
                    Intent intent = new Intent(Android.App.Application.Context, typeof(Preferences));
                    MainActivity.instance.StartActivity(intent);
                }).Show();
            }
        }

        public static bool FileIsAlreadyDownloaded(string youtubeID)
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

                        if (ytID == youtubeID)
                        {
                            musicCursor.Close();
                            return true;
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

            return false;
        }

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

        public static void RemoveFromPlaylist(string videoID)
        {
            youtubeService.PlaylistItems.Delete(videoID).Execute();
        }

        public static async void GetPlaylists(string videoID, Context context)
        {
            if (MainActivity.instance.TokenHasExpire())
            {
                youtubeService = null;
                MainActivity.instance.Login();

                while (youtubeService == null)
                    await Task.Delay(500);
            }

            List<string> playList = new List<string>();
            List<string> playListId = new List<string>();
            playList.Add("Create a playlist");
            playListId.Add("newPlaylist");


            HashMap parameters = new HashMap();
            parameters.Put("part", "snippet,contentDetails");
            parameters.Put("mine", "true");
            parameters.Put("maxResults", "25");
            parameters.Put("onBehalfOfContentOwner", "");
            parameters.Put("onBehalfOfContentOwnerChannel", "");

            PlaylistsResource.ListRequest ytPlaylists = youtubeService.Playlists.List(parameters.Get("part").ToString());

            if (parameters.ContainsKey("mine") && parameters.Get("mine").ToString() != "")
            {
                bool mine = (parameters.Get("mine").ToString() == "true") ? true : false;
                ytPlaylists.Mine = mine;
            }

            if (parameters.ContainsKey("maxResults"))
            {
                ytPlaylists.MaxResults = long.Parse(parameters.Get("maxResults").ToString());
            }

            if (parameters.ContainsKey("onBehalfOfContentOwner") && parameters.Get("onBehalfOfContentOwner").ToString() != "")
            {
                ytPlaylists.OnBehalfOfContentOwner = parameters.Get("onBehalfOfContentOwner").ToString();
            }

            if (parameters.ContainsKey("onBehalfOfContentOwnerChannel") && parameters.Get("onBehalfOfContentOwnerChannel").ToString() != "")
            {
                ytPlaylists.OnBehalfOfContentOwnerChannel = parameters.Get("onBehalfOfContentOwnerChannel").ToString();
            }

            PlaylistListResponse response = await ytPlaylists.ExecuteAsync();

            for (int i = 0; i < response.Items.Count; i++)
            {
                Google.Apis.YouTube.v3.Data.Playlist playlist = response.Items[i];
                playList.Add(playlist.Snippet.Title);
                playListId.Add(playlist.Id);
            }

            AlertDialog.Builder builder = new AlertDialog.Builder(context, MainActivity.dialogTheme);
            builder.SetTitle("Add to a playlist");
            builder.SetItems(playList.ToArray(), (senderAlert, args) =>
            {
                AddToPlaylist(videoID, playListId[args.Which], context);
            });
            builder.Show();
        }

        public static void AddToPlaylist(string videoID, string playlistID, Context context)
        {
            if(playlistID == "newPlaylist")
            {
                AlertDialog.Builder builder = new AlertDialog.Builder(context, MainActivity.dialogTheme);
                builder.SetTitle("Playlist name");
                View view = MainActivity.instance.LayoutInflater.Inflate(Resource.Layout.CreatePlaylistDialog, null);
                builder.SetView(view);
                builder.SetNegativeButton("Cancel", (senderAlert, args) => { });
                builder.SetPositiveButton("Create", (senderAlert, args) =>
                {
                    NewPlaylist(view.FindViewById<EditText>(Resource.Id.playlistName).Text, videoID);
                });
                builder.Show();
            }
            else
            {
                HashMap parameters = new HashMap();
                parameters.Put("part", "snippet");
                parameters.Put("onBehalfOfContentOwner", "");

                PlaylistItem playlistItem = new PlaylistItem();
                PlaylistItemSnippet snippet = new PlaylistItemSnippet();
                snippet.PlaylistId = playlistID;
                ResourceId resourceId = new ResourceId();
                resourceId.Kind = "youtube#video";
                resourceId.VideoId = videoID;
                snippet.ResourceId = resourceId;
                playlistItem.Snippet = snippet;

                var insertRequest = youtubeService.PlaylistItems.Insert(playlistItem, parameters.Get("part").ToString());

                if (parameters.ContainsKey("onBehalfOfContentOwner") && parameters.Get("onBehalfOfContentOwner").ToString() != "")
                {
                    insertRequest.OnBehalfOfContentOwner = parameters.Get("onBehalfOfContentOwner").ToString();
                }

                insertRequest.Execute();
            }
        }

        public static void NewPlaylist(string playlistName, string videoID)
        {
            HashMap parameters = new HashMap();
            parameters.Put("part", "snippet,status");
            parameters.Put("onBehalfOfContentOwner", "");


            Google.Apis.YouTube.v3.Data.Playlist playlist = new Google.Apis.YouTube.v3.Data.Playlist();
            PlaylistSnippet snippet = new PlaylistSnippet();
            PlaylistStatus status = new PlaylistStatus();
            snippet.Title = playlistName;
            playlist.Snippet = snippet;
            playlist.Status = status;

            var createRequest = youtubeService.Playlists.Insert(playlist, parameters.Get("part").ToString());

            if (parameters.ContainsKey("onBehalfOfContentOwner") && parameters.Get("onBehalfOfContentOwner").ToString() != "")
            {
                createRequest.OnBehalfOfContentOwner = parameters.Get("onBehalfOfContentOwner").ToString();
            }

            Google.Apis.YouTube.v3.Data.Playlist response = createRequest.Execute();

            parameters = new HashMap();
            parameters.Put("part", "snippet");
            parameters.Put("onBehalfOfContentOwner", "");

            PlaylistItem playlistItem = new PlaylistItem();
            PlaylistItemSnippet snippetItem = new PlaylistItemSnippet();
            snippetItem.PlaylistId = response.Id;
            ResourceId resourceId = new ResourceId();
            resourceId.Kind = "youtube#video";
            resourceId.VideoId = videoID;
            snippetItem.ResourceId = resourceId;
            playlistItem.Snippet = snippetItem;

            var insertRequest = youtubeService.PlaylistItems.Insert(playlistItem, parameters.Get("part").ToString());

            if (parameters.ContainsKey("onBehalfOfContentOwner") && parameters.Get("onBehalfOfContentOwner").ToString() != "")
            {
                insertRequest.OnBehalfOfContentOwner = parameters.Get("onBehalfOfContentOwner").ToString();
            }

            insertRequest.Execute();
        }
    }
}
 