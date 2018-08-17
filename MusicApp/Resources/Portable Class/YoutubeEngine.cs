using Android.Content;
using Android.Database;
using Android.Graphics;
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TagLib;

namespace MusicApp.Resources.Portable_Class
{
    public class YoutubeEngine : Fragment
    {
        public static YoutubeEngine[] instances;
        public static YouTubeService youtubeService;
        public static string searchKeyWorld;
        public static bool error = false;
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
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
        }

        private async void OnRefresh(object sender, EventArgs e)
        {
            if (focused)
            {
                await Search(searchKeyWorld, querryType, false);
                MainActivity.instance.contentRefresh.Refreshing = false;
            }
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
            if (searching && !error)
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

            ListView.GetLayoutManager().OnRestoreInstanceState(MainActivity.youtubeParcel);
            MainActivity.youtubeInstanceSave = null;
            MainActivity.youtubeParcel = null;
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
            if (search == null || search == "" || error)
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

            if(!await MainActivity.instance.WaitForYoutube())
            {
                error = true;
                if (loadingBar && focused)
                {
                    ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                    rootView.RemoveView(loadingView);
                }
                ListView.SetAdapter(null);
                emptyView = LayoutInflater.Inflate(Resource.Layout.EmptyYoutubeSearch, null);
                ((TextView)emptyView).Text = "Error while loading.\nCheck your internet connection and check if your logged in.";
                ((TextView)emptyView).SetTextColor(Color.Red);
                Activity.AddContentView(emptyView, ListView.LayoutParameters);
                return;
            }

            SearchResource.ListRequest searchResult = youtubeService.Search.List("snippet");
            searchResult.Fields = "items(id/videoId,id/playlistId,id/channelId,id/kind,snippet/title,snippet/thumbnails/high/url,snippet/channelTitle)";
            searchResult.Q = search.Replace(" ", "+-");
            searchResult.TopicId = "/m/04rlf";
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

            var searchReponse = await searchResult.ExecuteAsync();

            result = new List<YtFile>();

            foreach (var video in searchReponse.Items)
            {
                Song videoInfo = new Song(video.Snippet.Title, video.Snippet.ChannelTitle, video.Snippet.Thumbnails.High.Url, null, -1, -1, null, true, false);
                YtKind kind = YtKind.Null;

                switch (video.Id.Kind)
                {
                    case "youtube#video":
                        kind = YtKind.Video;
                        videoInfo.youtubeID = video.Id.VideoId;
                        break;
                    case "youtube#playlist":
                        kind = YtKind.Playlist;
                        videoInfo.youtubeID = video.Id.PlaylistId;
                        break;
                    case "youtube#channel":
                        kind = YtKind.Channel;
                        videoInfo.youtubeID = video.Id.ChannelId;
                        break;
                    default:
                        Console.WriteLine("&Kind = " + video.Id.Kind);
                        break;
                }
                result.Add(new YtFile(videoInfo, kind));
            }

            if (loadingBar && focused)
            {
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(loadingView);
            }

            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(MainActivity.instance);
            List<string> topics = prefManager.GetStringSet("selectedTopics", new string[] { }).ToList();
            List<string> selectedTopics = topics.ConvertAll(x => x.Substring(x.IndexOf("/#-#/") + 5));

            adapter = new YtAdapter(result)
            {
                listPadding = MainActivity.paddingBot - MainActivity.defaultPaddingBot,
                selectedTopicsID = selectedTopics
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

                Activity.AddContentView(emptyView, ListView.LayoutParameters);
            }
        }

        private void ListView_ItemClick(object sender, int position)
        {
            Song item = result[position].item;
            switch (result[position].Kind)
            {
                case YtKind.Video:
                    Play(item.youtubeID, item.Name, item.Artist, item.Album);
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
                    MainActivity.instance.SupportActionBar.Title = item.Name;
                    MainActivity.instance.HideTabs();
                    instances = null;
                    MainActivity.youtubeParcel = ListView.GetLayoutManager().OnSaveInstanceState();
                    MainActivity.youtubeInstanceSave = "YoutubeEngine" + "-" + querryType;
                    MainActivity.instance.Transition(Resource.Id.contentView, PlaylistTracks.NewInstance(item.youtubeID, item.Name, false, false, item.Artist, -1, item.Album), true);
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
            else if(result[position].Kind == YtKind.Playlist)
            {
                Song item = result[position].item;
                PlaylistMore(item);
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
                        Play(item.youtubeID, item.Name, item.Artist, item.Album);
                        break;
                    case 1:
                        PlayNext(item.youtubeID, item.Name, item.Artist, item.Album);
                        break;
                    case 2:
                        PlayLast(item.youtubeID, item.Name, item.Artist, item.Album);
                        break;
                    case 3:
                        GetPlaylists(item.youtubeID, Activity);
                        break;
                    case 4:
                        Download(item.Name, item.youtubeID);
                        break;
                    default:
                        break;
                }
            });
            builder.Show();
        }

        public void PlaylistMore(Song playlist)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme);
            builder.SetTitle("Pick an action");
            builder.SetItems(new string[] { "Random play", "Fork playlist", "Download" }, (senderAlert, args) =>
            {
                switch (args.Which)
                {
                    case 0:
                        RandomPlay(playlist.Path);
                        break;
                    case 1:
#pragma warning disable CS4014
                        ForkPlaylist(playlist.Path);
                        break;
                    case 2:
                        DownloadPlaylist(playlist.Name, playlist.Path);
                        break;
                    default:
                        break;
                }
            });
            builder.Show();
        }

        public static void Play(string videoID, string title, string artist, string thumbnailURL)
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("YoutubePlay");
            intent.PutExtra("action", "Play");
            intent.PutExtra("file", videoID);
            intent.PutExtra("title", title);
            intent.PutExtra("artist", artist);
            intent.PutExtra("thumbnailURI", thumbnailURL);
            Android.App.Application.Context.StartService(intent);
            ShowRecomandations(videoID);
        }

        public static async void PlayFiles(Song[] files)
        {
            if (files.Length < 1)
                return;

            if (MusicPlayer.isRunning)
                MusicPlayer.queue.Clear();

            MusicPlayer.currentID = -1;
            Play(files[0].Path, files[0].Name, files[0].Artist, files[0].Album);

            if (files.Length < 2)
                return;

            while (MusicPlayer.instance == null || MusicPlayer.CurrentID() == -1)
                await Task.Delay(10);

            foreach (Song song in files)
            {
                MusicPlayer.instance.AddToQueue(song);
            }
        }


        public static void PlayNext(string videoID, string title, string artist, string thumbnailURL)
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("YoutubePlay");
            intent.PutExtra("action", "PlayNext");
            intent.PutExtra("file", videoID);
            intent.PutExtra("title", title);
            intent.PutExtra("artist", artist);
            intent.PutExtra("thumbnailURI", thumbnailURL);
            Android.App.Application.Context.StartService(intent);
            ShowRecomandations(videoID);
        }

        public static void PlayLast(string videoID, string title, string artist, string thumbnailURL)
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("YoutubePlay");
            intent.PutExtra("action", "PlayLast");
            intent.PutExtra("file", videoID);
            intent.PutExtra("title", title);
            intent.PutExtra("artist", artist);
            intent.PutExtra("thumbnailURI", thumbnailURL);
            Android.App.Application.Context.StartService(intent);
            ShowRecomandations(videoID);
        }

        public static void ShowRecomandations(string videoID)
        {
            //Diplay a card with related video that the user might want to play

            //SearchResource.ListRequest searchResult = YoutubeEngine.youtubeService.Search.List("snippet");
            //searchResult.Fields = "items(id/videoId,snippet/title,snippet/thumbnails/default/url,snippet/channelTitle)";
            //searchResult.Type = "video";
            //searchResult.MaxResults = 20;
            //searchResult.RelatedToVideoId = MusicPlayer.queue[MusicPlayer.CurrentID()].youtubeID;

            //var searchReponse = await searchResult.ExecuteAsync();

            //List<Song> result = new List<Song>();

            //foreach (var video in searchReponse.Items)
            //{
            //    Song videoInfo = new Song(video.Snippet.Title, video.Snippet.ChannelTitle, video.Snippet.Thumbnails.Default__.Url, video.Id.VideoId, -1, -1, video.Id.VideoId, true, false);
            //    result.Add(videoInfo);
            //}
        }

        public async static void Download(string name, string videoID)
        {
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Android.App.Application.Context);
            if (prefManager.GetString("downloadPath", null) != null)
            {
                Toast.MakeText(Android.App.Application.Context, "Downloading...", ToastLength.Short).Show();
                Context context = Android.App.Application.Context;
                Intent intent = new Intent(context, typeof(Downloader));
                context.StartService(intent);

                while (Downloader.instance == null)
                    await Task.Delay(10);

                Downloader.instance.downloadPath = prefManager.GetString("downloadPath", null);
                Downloader.instance.Download(new DownloadFile(name, videoID, null));
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

        public static async void DownloadFiles(string[] names, string[] videoIDs, string playlist)
        {
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Android.App.Application.Context);
            if (prefManager.GetString("downloadPath", null) != null)
            {
                Toast.MakeText(Android.App.Application.Context, "Downloading...", ToastLength.Short).Show();
                Context context = Android.App.Application.Context;
                Intent intent = new Intent(context, typeof(Downloader));
                context.StartService(intent);

                while (Downloader.instance == null)
                    await Task.Delay(10);

                Downloader.instance.downloadPath = prefManager.GetString("downloadPath", null);

                for(int i = 0; i < names.Length; i++)
                    Downloader.instance.Download(new DownloadFile(names[i], videoIDs[i], playlist));
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
            if(!await MainActivity.instance.WaitForYoutube())
            {
                Toast.MakeText(context, "Error while loading.\nCheck your internet connection and check if your logged in.", ToastLength.Long).Show();
                return;
            }

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
                    Console.WriteLine("&Section found");
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
                    if (item.Snippet.Title != "[Deleted video]" && item.Snippet.Title != "Private video" && item.Snippet.Title != "Deleted video")
                    {
                        Song song = new Song(item.Snippet.Title, "", item.Snippet.Thumbnails.Default__.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true, false);
                        tracks.Add(song);
                    }
                }

                nextPageToken = ytPlaylist.NextPageToken;
            }

            Random r = new Random();
            tracks = tracks.OrderBy(x => r.Next()).ToList();
            PlayFiles(tracks.ToArray());
        }

        public static async void DownloadPlaylist(string playlist, string playlistID)
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
            DownloadFiles(names.ToArray(), videoIDs.ToArray(), playlist);
        }
    }
}
 