using Android.Content;
using Android.Database;
using Android.Graphics;
using Android.OS;
using Android.Preferences;
using Android.Provider;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Opus.Resources.values;
using Square.Picasso;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using TagLib;
using YoutubeExplode;
using YoutubeExplode.Models;
using CursorLoader = Android.Support.V4.Content.CursorLoader;
using SearchView = Android.Support.V7.Widget.SearchView;

namespace Opus.Resources.Portable_Class
{
    public class YoutubeEngine : Fragment
    {
        public static YoutubeEngine[] instances;
        public static YouTubeService youtubeService;
        public string Query;
        private string nextPageToken = null;
        public string querryType;

        public bool IsFocused = false;
        public RecyclerView ListView;
        public List<YtFile> result;

        private YtAdapter adapter;
        public TextView EmptyView;
        public ProgressBar LoadingView;
        private bool searching;


        public YoutubeEngine(string Query, string querryType)
        {
            this.Query = Query;
            this.querryType = querryType;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
        }

        private async void OnRefresh(object sender, EventArgs e)
        {
            if (IsFocused)
            {
                await Search(Query, querryType, false);
                MainActivity.instance.contentRefresh.Refreshing = false;
            }
        }

        private void OnScroll(object sender, View.ScrollChangeEventArgs e)
        {
            if (((LinearLayoutManager)ListView.GetLayoutManager()).FindLastVisibleItemPosition() == result.Count - 1)
                LoadMore();
        }

        async void LoadMore()
        {
            if(nextPageToken != null && !searching)
            {
                try
                {
                    searching = true;
                    SearchResource.ListRequest searchResult = youtubeService.Search.List("snippet");
                    searchResult.Q = Query.Replace(" ", "+-");
                    searchResult.PageToken = nextPageToken;
                    searchResult.TopicId = "/m/04rlf";
                    switch (querryType)
                    {
                        case "All":
                            searchResult.Type = "video,channel,playlist";
                            searchResult.EventType = null;
                            break;
                        case "Tracks":
                            searchResult.Type = "video";
                            searchResult.EventType = null;
                            break;
                        case "Playlists":
                            searchResult.Type = "playlist";
                            searchResult.EventType = null;
                            break;
                        case "Lives":
                            searchResult.Type = "video";
                            searchResult.EventType = SearchResource.ListRequest.EventTypeEnum.Live;
                            break;
                        case "Channels":
                            searchResult.Type = "channel";
                            searchResult.EventType = null;
                            break;
                        default:
                            searchResult.Type = "video";
                            searchResult.EventType = null;
                            break;
                    }
                    searchResult.MaxResults = 50;

                    var searchReponse = await searchResult.ExecuteAsync();
                    nextPageToken = searchReponse.NextPageToken;

                    int loadPos = result.Count - 1;
                    result.RemoveAt(loadPos);
                    adapter.NotifyItemRemoved(loadPos);

                    foreach (var video in searchReponse.Items)
                    {
                        Song videoInfo = new Song(video.Snippet.Title, video.Snippet.ChannelTitle, video.Snippet.Thumbnails.High.Url, null, -1, -1, null, true, false);
                        YtKind kind = YtKind.Null;

                        if (video.Snippet.LiveBroadcastContent == "live")
                            videoInfo.IsLiveStream = true;

                        switch (video.Id.Kind)
                        {
                            case "youtube#video":
                                kind = YtKind.Video;
                                videoInfo.YoutubeID = video.Id.VideoId;
                                break;
                            case "youtube#playlist":
                                kind = YtKind.Playlist;
                                videoInfo.YoutubeID = video.Id.PlaylistId;
                                break;
                            case "youtube#channel":
                                kind = YtKind.Channel;
                                videoInfo.YoutubeID = video.Id.ChannelId;
                                break;
                            default:
                                Console.WriteLine("&Kind = " + video.Id.Kind);
                                break;
                        }
                        result.Add(new YtFile(videoInfo, kind));
                    }

                    if (nextPageToken != null)
                        result.Add(new YtFile(new Song(), YtKind.Loading));

                    adapter.NotifyItemRangeInserted(loadPos, result.Count - loadPos);
                    searching = false;
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    MainActivity.instance.Timout();
                }
            }
        }

        public void OnFocus() { }
        public void OnUnfocus() { }

        public static Fragment[] NewInstances(string searchQuery)
        {
            instances = new YoutubeEngine[]
            {
                new YoutubeEngine(searchQuery, "All"),
                new YoutubeEngine(searchQuery, "Tracks"),
                new YoutubeEngine(searchQuery, "Playlists"),
                new YoutubeEngine(searchQuery, "Lives"),
                new YoutubeEngine(searchQuery, "Channels")
            };
            return instances;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.CompleteRecycler, container, false);
            EmptyView = view.FindViewById<TextView>(Resource.Id.empty);
            LoadingView = view.FindViewById<ProgressBar>(Resource.Id.loading);
            ListView = view.FindViewById<RecyclerView>(Resource.Id.recycler);
            ListView.SetLayoutManager(new LinearLayoutManager(Android.App.Application.Context));
            ListView.SetItemAnimator(new DefaultItemAnimator());
            ListView.ScrollChange += OnScroll;

            if (savedInstanceState != null)
                Query = savedInstanceState.GetString("Query");

#pragma warning disable CS4014
            Search(Query, querryType, true);
            return view;
        }

        public async Task Search(string search, string querryType, bool loadingBar)
        {
            SearchableActivity.IgnoreMyself = true;
            IMenuItem searchItem = MainActivity.instance.menu.FindItem(Resource.Id.search);
            searchItem.ExpandActionView();
            SearchView searchView = (SearchView)searchItem.ActionView;
            searchView.SetQuery(search, false);
            searchView.ClearFocus();
            searchView.Focusable = false;

            if (search == null || search == "")
                return;

            searching = true;
            Query = search;

            if (loadingBar)
            {
                adapter = null;
                ListView.SetAdapter(null);
                EmptyView.Visibility = ViewStates.Gone;
                LoadingView.Visibility = ViewStates.Visible;
            }

            SearchableActivity.IgnoreMyself = false;

            if (!await MainActivity.instance.WaitForYoutube())
            {
                ListView.SetAdapter(null);
                EmptyView.Text = MainActivity.instance.GetString(Resource.String.youtube_loading_error);
                EmptyView.SetTextColor(Color.Red);
                EmptyView.Visibility = ViewStates.Visible;
                return;
            }

            try
            {
                SearchResource.ListRequest searchResult = youtubeService.Search.List("snippet");
                searchResult.Q = search.Replace(" ", "+-");
                searchResult.TopicId = "/m/04rlf";
                switch (querryType)
                {
                    case "All":
                        searchResult.Type = "video,channel,playlist";
                        searchResult.EventType = null;
                        break;
                    case "Tracks":
                        searchResult.Type = "video";
                        searchResult.EventType = null;
                        break;
                    case "Playlists":
                        searchResult.Type = "playlist";
                        searchResult.EventType = null;
                        break;
                    case "Lives":
                        searchResult.Type = "video";
                        searchResult.EventType = SearchResource.ListRequest.EventTypeEnum.Live;
                        break;
                    case "Channels":
                        searchResult.Type = "channel";
                        searchResult.EventType = null;
                        break;
                    default:
                        searchResult.Type = "video";
                        searchResult.EventType = null;
                        break;
                }
                searchResult.MaxResults = 50;

                var searchReponse = await searchResult.ExecuteAsync();
                nextPageToken = searchReponse.NextPageToken;
                result = new List<YtFile>();

                foreach (var video in searchReponse.Items)
                {
                    Song videoInfo = new Song(HttpUtility.HtmlDecode(video.Snippet.Title), HttpUtility.HtmlDecode(video.Snippet.ChannelTitle), video.Snippet.Thumbnails.High.Url, null, -1, -1, video.Snippet.ChannelId, true, false);
                    YtKind kind = YtKind.Null;

                    if (video.Snippet.LiveBroadcastContent == "live")
                        videoInfo.IsLiveStream = true;

                    switch (video.Id.Kind)
                    {
                        case "youtube#video":
                            kind = YtKind.Video;
                            videoInfo.YoutubeID = video.Id.VideoId;
                            break;
                        case "youtube#playlist":
                            kind = YtKind.Playlist;
                            videoInfo.YoutubeID = video.Id.PlaylistId;
                            break;
                        case "youtube#channel":
                            kind = YtKind.Channel;
                            videoInfo.YoutubeID = video.Id.ChannelId;
                            break;
                        default:
                            Console.WriteLine("&Kind = " + video.Id.Kind);
                            break;
                    }
                    result.Add(new YtFile(videoInfo, kind));
                }

                LoadingView.Visibility = ViewStates.Gone;
                if (nextPageToken != null)
                    result.Add(new YtFile(new Song(), YtKind.Loading));

                if(result.Count > 0 && result[0].Kind == YtKind.Channel && result.Count(x => x.item.Artist == result[0].item.Title && x.Kind == YtKind.Video) > 0)
                {
                    YtFile channelPreview = new YtFile(result[0].item, YtKind.ChannelPreview);
                    result.Insert(0, channelPreview);
                }
                else if (result.Count > 0 && querryType == "All" || querryType == "Channels")
                {
                    IEnumerable<string> artist = result.GetRange(0, (result.Count > 20 ? 20 : result.Count)).GroupBy(x => x.item.Artist).Where(x => x.Count() > 5).Select(x => x.Key);
                    if (artist.Count() == 1)
                    {
                        Song channel = null;
                        if (result.Find(x => x.Kind == YtKind.Channel && x.item.Title == artist.First()) != null)
                            channel = result.Find(x => x.item.Title == artist.First() && x.Kind == YtKind.Channel).item;
                        //else
                        //{
                        //    string channelID = result.Find(x => x.item.Artist == artist.First()).item.Path;
                        //    ChannelsResource.ListRequest request = youtubeService.Channels.List("snippet");
                        //    request.Id = channelID;
                        //    ChannelListResponse response = await request.ExecuteAsync();
                        //    channel = new Song(response.Items[0].Snippet.Title, null, response.Items[0].Snippet.Thumbnails.High.Url, channelID, -1, -1, null);
                        //}

                        if (channel != null)
                        {
                            YtFile channelPreview = new YtFile(channel, YtKind.ChannelPreview);
                            result.Insert(0, channelPreview);
                        }
                    }
                }

                adapter = new YtAdapter(result);
                adapter.ItemClick += ListView_ItemClick;
                adapter.ItemLongCLick += ListView_ItemLongClick;
                ListView.SetAdapter(adapter);
                searching = false;

                if (result.Count == 0)
                {
                    EmptyView.Visibility = ViewStates.Visible;
                    switch (querryType)
                    {
                        case "All":
                            EmptyView.Text = GetString(Resource.String.no_result) + " " + search;
                            break;
                        case "Tracks":
                            EmptyView.Text = GetString(Resource.String.no_track) + " " + search;
                            break;
                        case "Playlists":
                            EmptyView.Text = GetString(Resource.String.no_playlist) + " " + search;
                            break;
                        case "Lives":
                            EmptyView.Text = GetString(Resource.String.no_lives) + " " + search;
                            break;
                        case "Channels":
                            EmptyView.Text = GetString(Resource.String.no_channel) + " " + search;
                            break;
                        default:
                            break;
                    }
                }
                else
                    EmptyView.Visibility = ViewStates.Gone;
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
                EmptyView.Text = GetString(Resource.String.timout);
                EmptyView.Visibility = ViewStates.Visible;
            }
        }

        private void ListView_ItemClick(object sender, int position)
        {
            Song item = result[position].item;
            switch (result[position].Kind)
            {
                case YtKind.Video:
                    Play(item.YoutubeID, item.Title, item.Artist, item.Album);
                    break;
                case YtKind.Playlist:
                    SearchableActivity.IgnoreMyself = true;
                    MainActivity.instance.menu.FindItem(Resource.Id.search).CollapseActionView();
                    MainActivity.instance.FindViewById<TabLayout>(Resource.Id.tabs).Visibility = ViewStates.Gone;
                    MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, PlaylistTracks.NewInstance(item.YoutubeID, item.Title, false, false, item.Artist, -1, item.Album)).AddToBackStack("Playlist Track").Commit();
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
            BottomSheetDialog bottomSheet = new BottomSheetDialog(MainActivity.instance);
            View bottomView = MainActivity.instance.LayoutInflater.Inflate(Resource.Layout.BottomSheet, null);
            bottomView.FindViewById<TextView>(Resource.Id.bsTitle).Text = item.Title;
            bottomView.FindViewById<TextView>(Resource.Id.bsArtist).Text = item.Artist;
            Picasso.With(MainActivity.instance).Load(item.Album).Placeholder(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
            bottomSheet.SetContentView(bottomView);

            bottomSheet.FindViewById<ListView>(Resource.Id.bsItems).Adapter = new BottomSheetAdapter(MainActivity.instance, Resource.Layout.BottomSheetText, new List<BottomSheetAction>
            {
                new BottomSheetAction(Resource.Drawable.Play, Resources.GetString(Resource.String.play), (sender, eventArg) =>
                {
                    Play(item.YoutubeID, item.Title, item.Artist, item.Album);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.PlaylistPlay, Resources.GetString(Resource.String.play_next), (sender, eventArg) =>
                {
                    PlayNext(item.YoutubeID, item.Title, item.Artist, item.Album);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Queue, Resources.GetString(Resource.String.play_last), (sender, eventArg) =>
                {
                    PlayLast(item.YoutubeID, item.Title, item.Artist, item.Album);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.PlayCircle, Resources.GetString(Resource.String.create_mix_from_song), (sender, eventArg) =>
                {
                    CreateMix(item);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.PlaylistAdd, Resources.GetString(Resource.String.add_to_playlist), (sender, eventArg) => { Browse.GetPlaylist(item); bottomSheet.Dismiss(); }),
                new BottomSheetAction(Resource.Drawable.Download, Resources.GetString(Resource.String.download), (sender, eventArg) =>
                {
                    Download(item.Title, item.YoutubeID);
                    bottomSheet.Dismiss();
                })
            });
            bottomSheet.Show();
        }

        public void PlaylistMore(Song item)
        {
            BottomSheetDialog bottomSheet = new BottomSheetDialog(MainActivity.instance);
            View bottomView = MainActivity.instance.LayoutInflater.Inflate(Resource.Layout.BottomSheet, null);
            bottomView.FindViewById<TextView>(Resource.Id.bsTitle).Text = item.Title;
            bottomView.FindViewById<TextView>(Resource.Id.bsArtist).Text = item.Artist;
            Picasso.With(MainActivity.instance).Load(item.Album).Placeholder(Resource.Color.background_material_dark).Transform(new RemoveBlackBorder(true)).Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
            bottomSheet.SetContentView(bottomView);

            List<BottomSheetAction> actions = new List<BottomSheetAction>
            {
                new BottomSheetAction(Resource.Drawable.Play, MainActivity.instance.Resources.GetString(Resource.String.play_in_order), (sender, eventArg) =>
                {
                    Playlist.PlayInOrder(item.YoutubeID);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Shuffle, MainActivity.instance.Resources.GetString(Resource.String.random_play), (sender, eventArg) =>
                {
                    RandomPlay(item.YoutubeID);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Queue, MainActivity.instance.Resources.GetString(Resource.String.add_to_queue), (sender, eventArg) =>
                {
                    Playlist.AddToQueue(item.YoutubeID);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.LibraryAdd, MainActivity.instance.Resources.GetString(Resource.String.add_to_library), (sender, eventArg) =>
                {
                    ForkPlaylist(item.YoutubeID);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Download, MainActivity.instance.Resources.GetString(Resource.String.download), (sender, eventArg) =>
                {
                    DownloadPlaylist(item.Title, item.YoutubeID);
                    bottomSheet.Dismiss();
                })
            };

            bottomSheet.FindViewById<ListView>(Resource.Id.bsItems).Adapter = new BottomSheetAdapter(MainActivity.instance, Resource.Layout.BottomSheetText, actions);
            bottomSheet.Show();
        }

        public static void Play(string videoID, string title, string artist, string thumbnailURL, bool addToQueue = true, bool showPlayer = true)
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("YoutubePlay");
            intent.PutExtra("action", "Play");
            intent.PutExtra("file", videoID);
            intent.PutExtra("title", title);
            intent.PutExtra("artist", artist);
            intent.PutExtra("thumbnailURI", thumbnailURL);
            intent.PutExtra("addToQueue", addToQueue);
            intent.PutExtra("showPlayer", showPlayer);
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
            Play(files[0].Path, files[0].Title, files[0].Artist, files[0].Album);

            if (files.Length < 2)
                return;

            while (MusicPlayer.instance == null || MusicPlayer.CurrentID() == -1)
                await Task.Delay(10);

            for (int i = 1; i < files.Length; i++)
                MusicPlayer.instance.AddToQueue(files[i]);
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

        public async static void Download(string name, string videoID, string playlist = null)
        {
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Android.App.Application.Context);
            if (prefManager.GetString("downloadPath", null) == null)
            {
                Snackbar snackBar = Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), Resource.String.download_path_not_set, Snackbar.LengthIndefinite);
                snackBar.SetAction(Resource.String.set_path, (v) =>
                {
                    snackBar.Dismiss();
                    Intent prefIntent = new Intent(Android.App.Application.Context, typeof(Preferences));
                    MainActivity.instance.StartActivity(prefIntent);
                });
                snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                snackBar.Show();
            }

            Toast.MakeText(Android.App.Application.Context, Resource.String.downloading, ToastLength.Short).Show();
            Context context = Android.App.Application.Context;
            Intent intent = new Intent(context, typeof(Downloader));
            context.StartService(intent);

            while (Downloader.instance == null)
                await Task.Delay(10);

            Downloader.instance.downloadPath = prefManager.GetString("downloadPath", Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).ToString());
            Downloader.instance.maxDownload = prefManager.GetInt("maxDownload", 4);
            Downloader.queue.Add(new DownloadFile(name, videoID, playlist));
            Downloader.instance.StartDownload();
        }

        public static void DownloadFiles(Song[] items, string playlist)
        {
            string[] names = items.ToList().ConvertAll(x => x.Title).ToArray();
            string[] videoIDs = items.ToList().ConvertAll(x => x.YoutubeID).ToArray();

            DownloadFiles(names, videoIDs, playlist);
        }

        public static async void DownloadFiles(string[] names, string[] videoIDs, string playlist)
        {
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Android.App.Application.Context);
            string downloadPath = prefManager.GetString("downloadPath", null);
            if (downloadPath == null)
            {
                Snackbar snackBar = Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), Resource.String.download_path_not_set, Snackbar.LengthLong).SetAction(Resource.String.set_path, (v) =>
                {
                    Intent pref = new Intent(Android.App.Application.Context, typeof(Preferences));
                    MainActivity.instance.StartActivity(pref);
                });
                snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                snackBar.Show();

                ISharedPreferencesEditor editor = prefManager.Edit();
                editor.PutString("downloadPath", Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).ToString());
                editor.Commit();

                downloadPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).ToString();
            }

            Context context = Android.App.Application.Context;
            Intent intent = new Intent(context, typeof(Downloader));
            context.StartService(intent);

            while (Downloader.instance == null)
                await Task.Delay(10);

            List<DownloadFile> files = new List<DownloadFile>();
            for (int i = 0; i < names.Length; i++)
            {
                if(videoIDs[i] != null && videoIDs[i] != "")
                    files.Add(new DownloadFile(names[i], videoIDs[i], playlist));
            }

            Downloader.instance.downloadPath = downloadPath;
            Downloader.instance.maxDownload = prefManager.GetInt("maxDownload", 4);
            Downloader.queue.AddRange(files);

            if (playlist != null)
                Downloader.instance.SyncWithPlaylist(playlist, prefManager.GetBoolean("keepDeleted", true));

            Downloader.instance.StartDownload();
        }

        public static bool FileIsAlreadyDownloaded(string youtubeID)
        {
            //Android.Net.Uri musicUri = MediaStore.Audio.Media.ExternalContentUri;

            //CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
            //ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

            //if (musicCursor != null && musicCursor.MoveToFirst())
            //{
            //    int pathKey = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Data);
            //    do
            //    {
            //        string path = musicCursor.GetString(pathKey);

            //        try
            //        {
            //            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            //            var meta = TagLib.File.Create(new StreamFileAbstraction(path, stream, stream));
            //            string ytID = meta.Tag.Comment;
            //            stream.Dispose();

            //            if (ytID == youtubeID)
            //            {
            //                musicCursor.Close();
            //                return true;
            //            }
            //        }
            //        catch (CorruptFileException)
            //        {
            //            continue;
            //        }
            //    }
            //    while (musicCursor.MoveToNext());
            //    musicCursor.Close();
            //}

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

        public static async void RemoveFromPlaylist(string TrackID)
        {
            try
            {
                await youtubeService.PlaylistItems.Delete(TrackID).ExecuteAsync();
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
            }
        }

        public async static void AddToPlaylist(Song item, string PlaylistYtID)
        {
            try
            {
                Google.Apis.YouTube.v3.Data.PlaylistItem playlistItem = new Google.Apis.YouTube.v3.Data.PlaylistItem();
                PlaylistItemSnippet snippet = new PlaylistItemSnippet
                {
                    PlaylistId = PlaylistYtID
                };
                ResourceId resourceId = new ResourceId
                {
                    Kind = "youtube#video",
                    VideoId = item.YoutubeID
                };
                snippet.ResourceId = resourceId;
                playlistItem.Snippet = snippet;

                var insertRequest = youtubeService.PlaylistItems.Insert(playlistItem, "snippet");
                await insertRequest.ExecuteAsync();
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
            }
        }

        public async static void AddToPlaylist(Song[] items, string PlaylistYtID)
        {
            Google.Apis.YouTube.v3.Data.PlaylistItem playlistItem = new Google.Apis.YouTube.v3.Data.PlaylistItem();
            PlaylistItemSnippet snippet = new PlaylistItemSnippet
            {
                PlaylistId = PlaylistYtID
            };

            foreach(Song item in items)
            {
                if(item != null && item.IsYt)
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

                        var insertRequest = youtubeService.PlaylistItems.Insert(playlistItem, "snippet");
                        await insertRequest.ExecuteAsync();
                    }
                    catch (System.Net.Http.HttpRequestException)
                    {
                        MainActivity.instance.Timout();
                    }
                }
            }
        }

        public async static void CreatePlaylist(string playlistName, Song item)
        {
            try
            {
                Google.Apis.YouTube.v3.Data.Playlist playlist = new Google.Apis.YouTube.v3.Data.Playlist();
                PlaylistSnippet snippet = new PlaylistSnippet();
                PlaylistStatus status = new PlaylistStatus();
                snippet.Title = playlistName;
                playlist.Snippet = snippet;
                playlist.Status = status;

                var createRequest = youtubeService.Playlists.Insert(playlist, "snippet, status");
                Google.Apis.YouTube.v3.Data.Playlist response = await createRequest.ExecuteAsync();

                AddToPlaylist(item, response.Id);
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
            }
        }

        public async static void CreatePlaylist(string playlistName, Song[] items)
        {
            try
            {
                Google.Apis.YouTube.v3.Data.Playlist playlist = new Google.Apis.YouTube.v3.Data.Playlist();
                PlaylistSnippet snippet = new PlaylistSnippet();
                PlaylistStatus status = new PlaylistStatus();
                snippet.Title = playlistName;
                playlist.Snippet = snippet;
                playlist.Status = status;

                var createRequest = youtubeService.Playlists.Insert(playlist, "snippet, status");
                Google.Apis.YouTube.v3.Data.Playlist response = await createRequest.ExecuteAsync();

                AddToPlaylist(items, response.Id);
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
            }
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
                        Snackbar snackBar = Snackbar.Make(MainActivity.instance.FindViewById<CoordinatorLayout>(Resource.Id.snackBar), Resource.String.playlist_already_saved, Snackbar.LengthLong);
                        snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                        snackBar.Show();
                        return;
                    }
                    else
                    {
                        try
                        {
                            section.ContentDetails.Playlists.Add(playlistID);
                            ChannelSectionsResource.UpdateRequest request = youtubeService.ChannelSections.Update(section, "snippet,contentDetails");
                            ChannelSection response = await request.ExecuteAsync();
                        }
                        catch (System.Net.Http.HttpRequestException)
                        {
                            MainActivity.instance.Timout();
                        }
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
            try
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

                if (tracks.Count == 0)
                    return;

                Random r = new Random();
                tracks = tracks.OrderBy(x => r.Next()).ToList();
                PlayFiles(tracks.ToArray());
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
            }
        }

        public async void MixFromChannel(string ChannelID)
        {
            if (!await MainActivity.instance.WaitForYoutube())
                return;

            List<Song> songs = new List<Song>();
            try
            {
                SearchResource.ListRequest searchRequest = youtubeService.Search.List("snippet");
                searchRequest.Fields = "items(id/videoId,snippet/title,snippet/thumbnails/high/url,snippet/channelTitle)";
                searchRequest.Type = "video";
                searchRequest.ChannelId = ChannelID;
                searchRequest.MaxResults = 20;
                var searchReponse = await searchRequest.ExecuteAsync();


                foreach (var video in searchReponse.Items)
                {
                    Song song = new Song(video.Snippet.Title, video.Snippet.ChannelTitle, video.Snippet.Thumbnails.High.Url, video.Id.VideoId, -1, -1, null, true, false);
                    songs.Add(song);
                }
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
            }

            int index = new Random().Next(0, songs.Count);
            Play(songs[index].YoutubeID, songs[index].Title, songs[index].Artist, songs[index].Album);
            songs.RemoveAt(index);

            while (MusicPlayer.instance == null)
                await Task.Delay(10);

            MusicPlayer.instance.RandomPlay(songs, false);
        }

        public static async void CreateMix(Song item)
        {
            bool AddItemToQueue = true;
            if(MusicPlayer.queue.Count == 0)
            {
                Play(item.YoutubeID, item.Title, item.Artist, item.Album, true, true);
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

                var ytPlaylistRequest = youtubeService.PlaylistItems.List("snippet, contentDetails");
                ytPlaylistRequest.PlaylistId = video.GetVideoMixPlaylistId();
                ytPlaylistRequest.MaxResults = 25;

                var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                foreach (var ytItem in ytPlaylist.Items)
                {
                    if (ytItem.Snippet.Title != "[Deleted video]" && ytItem.Snippet.Title != "Private video" && ytItem.Snippet.Title != "Deleted video" && !MusicPlayer.queue.Exists(x => x.YoutubeID == ytItem.ContentDetails.VideoId))
                    {
                        Song song = new Song(ytItem.Snippet.Title, ytItem.Snippet.ChannelTitle, ytItem.Snippet.Thumbnails.High.Url, ytItem.ContentDetails.VideoId, -1, -1, ytItem.ContentDetails.VideoId, true, false);
                        tracks.Add(song);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is System.Net.Http.HttpRequestException)
                    MainActivity.instance.Timout();
                else
                    MainActivity.instance.UnknowError();

                return;
            }

            Random r = new Random();
            tracks = tracks.OrderBy(x => r.Next()).ToList();
            if(AddItemToQueue && !MusicPlayer.queue.Exists(x => x.YoutubeID == item.YoutubeID))
                tracks.Add(item);

            Intent intent = new Intent(MainActivity.instance, typeof(MusicPlayer));
            MainActivity.instance.StartService(intent);

            while (MusicPlayer.instance == null)
                await Task.Delay(100);

            MusicPlayer.instance.AddToQueue(tracks.ToArray());
            MainActivity.instance.ShowPlayer();
            Home.instance?.RefreshQueue();
            Queue.instance?.Refresh();
            parseProgress.Visibility = ViewStates.Gone;
        }

        public static async void DownloadPlaylist(string playlist, string playlistID, bool showToast = true)
        {
            if (!await MainActivity.instance.WaitForYoutube())
                return;

            if (showToast)
                Toast.MakeText(Android.App.Application.Context, Resource.String.syncing, ToastLength.Short).Show();

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

            if(names.Count > 0)
                DownloadFiles(names.ToArray(), videoIDs.ToArray(), playlist);
        }

        public override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutString("Query", Query);
            base.OnSaveInstanceState(outState);
        }
    }
}
 