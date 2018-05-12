using Android.Content;
using Android.Database;
using Android.OS;
using Android.Preferences;
using Android.Provider;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
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
    public class YoutubeEngine : Fragment
    {
        public static YoutubeEngine[] instances;
        public static YouTubeService youtubeService;
        public static string searchKeyWorld;
        public string querryType;
        public bool focused = false;
        public RecyclerView ListView;
        public List<YtFile> result;

        private YtAdapter adapter;
        public View emptyView;
        public static View loadingView;
        private bool searching;
        private readonly string[] actions = new string[] { "Play", "Play Next", "Play Last", "Add To Playlist", "Download" };

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            ListView.ScrollChange += MainActivity.instance.Scroll;
            MainActivity.instance.OnPaddingChanged += OnPaddingChanged;
        }

        public void OnPaddingChanged(object sender, PaddingChange e)
        {
            if (MainActivity.paddingBot > e.oldPadding)
                adapter.listPadding = MainActivity.paddingBot - MainActivity.defaultPaddingBot;
            else
                adapter.listPadding = (int)(8 * MainActivity.instance.Resources.DisplayMetrics.Density + 0.5f);
        }

        public void OnFocus()
        {
            if (searching)
            {
                adapter = null;
                ListView.SetAdapter(null);
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(loadingView);
                loadingView = LayoutInflater.Inflate(Resource.Layout.EmptyLoadingLayout, null);
                Activity.AddContentView(loadingView, ListView.LayoutParameters);
            }
        }

        public void OnUnfocus()
        {
            if (searching)
            {
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(loadingView);
            }
        }

        public async void ResumeListView()
        {
            while (ListView == null || ListView.GetLayoutManager() == null)
                await Task.Delay(10);

            ListView.GetLayoutManager().OnRestoreInstanceState(MainActivity.instance.youtubeParcel);
            MainActivity.instance.youtubeInstanceSave = null;
            MainActivity.instance.youtubeParcel = null;
        }

        public static Fragment[] NewInstances(string searchQuery)
        {
            searchKeyWorld = searchQuery;
            instances = new YoutubeEngine[]
            {
                new YoutubeEngine { Arguments = new Bundle() },
                new YoutubeEngine { Arguments = new Bundle() },
                new YoutubeEngine { Arguments = new Bundle() },
                new YoutubeEngine { Arguments = new Bundle() },
            };
            instances[0].querryType = "All";
            instances[1].querryType = "Tracks";
            instances[2].querryType = "Playlists";
            instances[3].querryType = "Channels";
            return instances;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.RecyclerFragment, container, false);
            ListView = view.FindViewById<RecyclerView>(Resource.Id.recycler);
            view.SetPadding(0, 0, 0, MainActivity.defaultPaddingBot);
            ListView.SetLayoutManager(new LinearLayoutManager(Android.App.Application.Context));
            ListView.SetItemAnimator(new DefaultItemAnimator());
            ListView.ScrollChange += MainActivity.instance.Scroll;

#pragma warning disable CS4014
            Search(searchKeyWorld, querryType, true);
            return view;
        }

        public async Task Search(string search, string querryType, bool loadingBar)
        {
            if (search == null || search == "")
                return;

            searching = true;
            searchKeyWorld = search;

            if (loadingBar && focused)
            {
                adapter = null;
                ListView.SetAdapter(null);
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(loadingView);
                loadingView = LayoutInflater.Inflate(Resource.Layout.EmptyLoadingLayout, null);
                Activity.AddContentView(loadingView, ListView.LayoutParameters);
            }

            await MainActivity.instance.WaitForYoutube();

            SearchResource.ListRequest searchResult = youtubeService.Search.List("snippet");
            searchResult.Fields = "items(id/videoId,id/playlistId,id/kind,snippet/title,snippet/thumbnails/default/url,snippet/channelTitle)";
            searchResult.Q = search.Replace(" ", "+-");
            searchResult.Type = "video";
            switch (querryType)
            {
                case "All":
                    searchResult.Type = "video,channel,playlist";
                    break;
                case "Tracks":
                    searchResult.Type = "video";
                    break;
                case "Playlists":
                    searchResult.Type = "playlist";
                    break;
                case "Channels":
                    searchResult.Type = "channel";
                    break;
                default:
                    searchResult.Type = "video";
                    break;
            }
            searchResult.MaxResults = 20;

            System.Console.WriteLine("&Search created");
            var searchReponse = await searchResult.ExecuteAsync();
            System.Console.WriteLine("&Search waited");

            result = new List<YtFile>();

            foreach (var video in searchReponse.Items)
            {
                Song videoInfo = new Song(video.Snippet.Title, video.Snippet.ChannelTitle, video.Snippet.Thumbnails.Default__.Url, video.Id.VideoId ?? video.Id.PlaylistId, -1, -1, video.Id.VideoId ?? video.Id.PlaylistId, true);
                YtKind kind = YtKind.Null;

                switch (video.Id.Kind)
                {
                    case "youtube#video":
                        kind = YtKind.Video;
                        break;
                    case "youtube#playlist":
                        kind = YtKind.Playlist;
                        break;
                    case "youtube#channel":
                        kind = YtKind.Channel;
                        break;
                    default:
                        System.Console.WriteLine("&Kind = " + video.Id.Kind);
                        break;
                }
                result.Add(new YtFile(videoInfo, kind));
            }

            if (loadingBar && focused)
            {
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(loadingView);
            }

            adapter = new YtAdapter(result)
            {
                listPadding = MainActivity.paddingBot - MainActivity.defaultPaddingBot
            };
            adapter.ItemClick += ListView_ItemClick;
            adapter.ItemLongCLick += ListView_ItemLongClick;
            ListView.SetAdapter(adapter);
            searching = false;

            if (adapter == null || adapter.ItemCount == 0)
            {
                emptyView = LayoutInflater.Inflate(Resource.Layout.EmptyYoutubeSearch, null);

                switch (querryType)
                {
                    case "All":
                        ((TextView)emptyView).Text = "No result for " + search;
                        break;
                    case "Tracks":
                        ((TextView)emptyView).Text = "No tracks for " + search;
                        break;
                    case "Playlists":
                        ((TextView)emptyView).Text = "No Playlist for " + search;
                        break;
                    case "Channels":
                        ((TextView)emptyView).Text = "No channel for " + search;
                        break;
                    default:
                        break;
                }

                Activity.AddContentView(loadingView, ListView.LayoutParameters);
            }
        }

        private void ListView_ItemClick(object sender, int position)
        {
            Song item = result[position].item;
            switch (result[position].Kind)
            {
                case YtKind.Video:
                    Play(item.GetPath(), item.GetName(), item.GetArtist(), item.GetAlbum(), false, this);
                    break;
                case YtKind.Playlist:
                    ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                    rootView.RemoveView(loadingView);
                    foreach(YoutubeEngine instance in instances)
                    {
                        rootView.RemoveView(instance.emptyView);
                        MainActivity.instance.OnPaddingChanged -= instance.OnPaddingChanged;
                    }

                    var searchView = MainActivity.instance.menu.FindItem(Resource.Id.search).ActionView.JavaCast<Android.Support.V7.Widget.SearchView>();
                    MainActivity.instance.menu.FindItem(Resource.Id.search).CollapseActionView();
                    searchView.ClearFocus();
                    searchView.Iconified = true;
                    searchView.SetQuery("", false);
                    MainActivity.instance.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
                    MainActivity.instance.SupportActionBar.Title = item.GetName();
                    MainActivity.instance.HideTabs();
                    instances = null;
                    MainActivity.instance.youtubeParcel = ListView.GetLayoutManager().OnSaveInstanceState();
                    MainActivity.instance.youtubeInstanceSave = "YoutubeEngine" + "-" + querryType;
                    MainActivity.instance.Transition(Resource.Id.contentView, PlaylistTracks.NewInstance(item.youtubeID, item.GetName()), true);
                    break;
                case YtKind.Channel:
                    Toast.MakeText(Activity, "Action comming soon", ToastLength.Short).Show();
                    break;
                default:
                    break;
            }
        }

        private void ListView_ItemLongClick(object sender, int position)
        {
            if(result[position].Kind == YtKind.Video)
            {
                Song item = result[position].item;
                More(item);
            }
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
                        Play(item.GetPath(), item.GetName(), item.GetArtist(), item.GetAlbum(), false, this);
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

        public static async void Play(string videoID, string title, string artist, string thumbnailURL, bool skipExistVerification = false, YoutubeEngine instance = null)
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

            if (instance != null)
            {
                MainActivity.instance.youtubeParcel = instance.ListView.GetLayoutManager().OnSaveInstanceState();
                MainActivity.instance.youtubeInstanceSave = "YoutubeEngine" + "-" + instance.querryType;

                ViewGroup rootView = instance.Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                foreach (YoutubeEngine inst in instances)
                {
                    MainActivity.instance.OnPaddingChanged -= inst.OnPaddingChanged;
                    rootView.RemoveView(inst.emptyView);
                }
                rootView.RemoveView(loadingView);
                instances = null;
            }
            else
                MainActivity.instance.SaveInstance();

            MainActivity.instance.HideTabs();
            MainActivity.instance.HideSearch();
            MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, Player.NewInstance()).AddToBackStack(null).Commit();
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
                Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), "Download Path Not Set.", Snackbar.LengthShort).SetAction("Set Path", (v) =>
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
            await MainActivity.instance.WaitForYoutube();

            List<string> playList = new List<string>();
            List<string> playListId = new List<string>();
            playList.Add("Create a playlist");
            playListId.Add("newPlaylist");


            PlaylistsResource.ListRequest ytPlaylists = youtubeService.Playlists.List("snippet,contentDetails");
            ytPlaylists.Mine = true;
            ytPlaylists.MaxResults = 25;
            PlaylistListResponse response = await ytPlaylists.ExecuteAsync();

            for (int i = 0; i < response.Items.Count; i++)
            {
                Google.Apis.YouTube.v3.Data.Playlist playlist = response.Items[i];
                playList.Add(playlist.Snippet.Title);
                playListId.Add(playlist.Id);
            }

            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(context, MainActivity.dialogTheme);
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
                Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(context, MainActivity.dialogTheme);
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
                PlaylistItem playlistItem = new PlaylistItem();
                PlaylistItemSnippet snippet = new PlaylistItemSnippet
                {
                    PlaylistId = playlistID
                };
                ResourceId resourceId = new ResourceId
                {
                    Kind = "youtube#video",
                    VideoId = videoID
                };
                snippet.ResourceId = resourceId;
                playlistItem.Snippet = snippet;

                var insertRequest = youtubeService.PlaylistItems.Insert(playlistItem, "snippet");
                insertRequest.Execute();
            }
        }

        public static void NewPlaylist(string playlistName, string videoID)
        {
            Google.Apis.YouTube.v3.Data.Playlist playlist = new Google.Apis.YouTube.v3.Data.Playlist();
            PlaylistSnippet snippet = new PlaylistSnippet();
            PlaylistStatus status = new PlaylistStatus();
            snippet.Title = playlistName;
            playlist.Snippet = snippet;
            playlist.Status = status;

            var createRequest = youtubeService.Playlists.Insert(playlist, "snippet, status");
            Google.Apis.YouTube.v3.Data.Playlist response = createRequest.Execute();


            PlaylistItem playlistItem = new PlaylistItem();
            PlaylistItemSnippet snippetItem = new PlaylistItemSnippet
            {
                PlaylistId = response.Id
            };
            ResourceId resourceId = new ResourceId
            {
                Kind = "youtube#video",
                VideoId = videoID
            };
            snippetItem.ResourceId = resourceId;
            playlistItem.Snippet = snippetItem;

            var insertRequest = youtubeService.PlaylistItems.Insert(playlistItem, "snippet");
            insertRequest.Execute();
        }

        public static async Task ForkPlaylist(string playlistID)
        {
            ChannelSectionsResource.ListRequest forkedRequest = youtubeService.ChannelSections.List("snippet,contentDetails");
            forkedRequest.Mine = true;
            ChannelSectionListResponse forkedResponse = await forkedRequest.ExecuteAsync();

            foreach (ChannelSection section in forkedResponse.Items)
            {
                if (section.Snippet.Title == "Saved Playlists")
                {
                    //AddToSection
                    if (section.ContentDetails.Playlists.Contains(playlistID))
                    {
                        Snackbar.Make(MainActivity.instance.FindViewById<View>(Resource.Id.snackBar), "You've already added this playlist.", 1).Show();
                        return;
                    }
                    else
                    {
                        section.ContentDetails.Playlists.Add(playlistID);
                        ChannelSectionsResource.UpdateRequest request = youtubeService.ChannelSections.Update(section, "snippet,contentDetails");
                        ChannelSection response = await request.ExecuteAsync();
                        return;
                    }
                }
            }
            //CreateSection and add to it
            ChannelSection newSection = new ChannelSection();
            ChannelSectionContentDetails details = new ChannelSectionContentDetails();
            ChannelSectionSnippet snippet = new ChannelSectionSnippet();

            details.Playlists = new List<string>() { playlistID };
            snippet.Title = "Saved Playlists";
            snippet.Type = "multiplePlaylists";
            snippet.Style = "horizontalRow";

            newSection.ContentDetails = details;
            newSection.Snippet = snippet;

            ChannelSectionsResource.InsertRequest insert = youtubeService.ChannelSections.Insert(newSection, "snippet,contentDetails");
            ChannelSection insertResponse = await insert.ExecuteAsync();
        }

        public static async void RandomPlay(string playlistID)
        {
            List<Song> tracks = new List<Song>();
            string nextPageToken = "";
            while (nextPageToken != null)
            {
                var ytPlaylistRequest = youtubeService.PlaylistItems.List("snippet, contentDetails");
                ytPlaylistRequest.PlaylistId = playlistID;
                ytPlaylistRequest.MaxResults = 50;
                ytPlaylistRequest.PageToken = nextPageToken;

                var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                foreach (var item in ytPlaylist.Items)
                {
                    Song song = new Song(item.Snippet.Title, "", item.Snippet.Thumbnails.Default__.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true, false);
                    tracks.Add(song);
                }

                nextPageToken = ytPlaylist.NextPageToken;
            }
            PlayFiles(tracks.ToArray());
        }

        public static async void DownloadPlaylist(string playlistID)
        {
            List<string> names = new List<string>();
            List<string> videoIDs = new List<string>();
            string nextPageToken = "";
            while (nextPageToken != null)
            {
                var ytPlaylistRequest = youtubeService.PlaylistItems.List("snippet, contentDetails");
                ytPlaylistRequest.PlaylistId = playlistID;
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
            DownloadFiles(names.ToArray(), videoIDs.ToArray());
        }
    }
}
 