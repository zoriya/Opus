using Android.Content;
using Android.OS;
using Android.Preferences;
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
using System.Linq;
using System.Threading.Tasks;
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

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            ListView.ItemClick += ListView_ItemClick;
            ListView.ItemLongClick += ListView_ItemLongClick;

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
        }

        public override void OnDestroy()
        {
            if (isEmpty)
            {
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(emptyView);
            }
            base.OnDestroy();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = base.OnCreateView(inflater, container, savedInstanceState);
            view.SetPadding(0, MainActivity.paddinTop, 0, MainActivity.paddingBot);
            return view;
        }

        public static Fragment NewInstance()
        {
            instance = new YoutubeEngine { Arguments = new Bundle() };
            return instance;
        }

        public async void Search(string search)
        {
            if (search == null || search == "")
                return;

            if (MainActivity.instance.TokenHasExpire())
            {
                youtubeService = null;
                MainActivity.instance.Login();

                while (youtubeService == null)
                    await Task.Delay(500);
            }

            SearchResource.ListRequest searchResult = youtubeService.Search.List("snippet");
            searchResult.Fields = "items(id/videoId,snippet/title,snippet/thumbnails/default/url,snippet/channelTitle)";
            searchResult.Q = search;
            searchResult.Type = "video";
            searchResult.MaxResults = 20;

            var searchReponse = await searchResult.ExecuteAsync();

            result = new List<Song>();

            foreach(var video in searchReponse.Items)
            {
                Song videoInfo = new Song(video.Snippet.Title, video.Snippet.ChannelTitle, video.Snippet.Thumbnails.Default__.Url, -1, -1, video.Id.VideoId, true);
                result.Add(videoInfo);
            }

            ListAdapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, result);
        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            string videoID = result[e.Position].GetPath();
            Play(videoID);
        }

        private void ListView_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            Song item = result[e.Position];

            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, Resource.Style.AppCompatAlertDialogStyle);
            builder.SetTitle("Pick an action");
            builder.SetItems(actions, (senderAlert, args) =>
            {
                switch (args.Which)
                {
                    case 0:
                        Play(item.GetPath());
                        break;
                    case 1:
                        PlayNext(item.GetPath());
                        break;
                    case 2:
                        PlayLast(item.GetPath());
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

        public static async void Play(string videoID)
        {
            YoutubeClient client = new YoutubeClient();
            var videoInfo = await client.GetVideoAsync(videoID);
            AudioStreamInfo streamInfo = videoInfo.AudioStreamInfos.OrderBy(s => s.Bitrate).Last();

            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.PutExtra("file", streamInfo.Url);
            intent.PutExtra("title", videoInfo.Title);
            intent.PutExtra("artist", videoInfo.Author.Title);
            intent.PutExtra("thumbnailURI", videoInfo.Thumbnails.HighResUrl);
            Android.App.Application.Context.StartService(intent);

            MainActivity.instance.HideTabs();
            MainActivity.instance.HideSearch();
            MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, Player.NewInstance()).Commit();
        }

        public static async void PlayFiles(Song[] files)
        {
            if (files.Length < 1)
                return;

            Play(files[0].GetPath());

            if (files.Length < 2)
                return;

            await Task.Delay(5000);

            for(int i = 1; i < files.Length; i++)
            {
                MusicPlayer.queue.Insert(MusicPlayer.CurrentID() + i, files[i]);
            }
        }


        public static async void PlayNext(string videoID)
        {
            YoutubeClient client = new YoutubeClient();
            var videoInfo = await client.GetVideoAsync(videoID);
            AudioStreamInfo streamInfo = videoInfo.AudioStreamInfos.OrderBy(s => s.Bitrate).Last();

            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("PlayNext");
            intent.PutExtra("file", streamInfo.Url);
            intent.PutExtra("title", videoInfo.Title);
            intent.PutExtra("artist", videoInfo.Author.Title);
            intent.PutExtra("thumbnailURI", videoInfo.Thumbnails.HighResUrl);
            Android.App.Application.Context.StartService(intent);
        }

        public static async void PlayLast(string videoID)
        {
            YoutubeClient client = new YoutubeClient();
            var videoInfo = await client.GetVideoAsync(videoID);
            AudioStreamInfo streamInfo = videoInfo.AudioStreamInfos.OrderBy(s => s.Bitrate).Last();

            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("PlayLast");
            intent.PutExtra("file", streamInfo.Url);
            intent.PutExtra("title", videoInfo.Title);
            intent.PutExtra("artist", videoInfo.Author.Title);
            intent.PutExtra("thumbnailURI", videoInfo.Thumbnails.HighResUrl);
            Android.App.Application.Context.StartService(intent);
        }

        public static void Download(string name, string videoID)
        {
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Android.App.Application.Context);
            if (prefManager.GetString("downloadPath", null) != null)
            {
                Toast.MakeText(Android.App.Application.Context, "Downloading...", ToastLength.Short).Show();
                Context context = Android.App.Application.Context;
                Intent intent = new Intent(context, typeof(Downloader));
                intent.PutExtra("videoID", videoID);
                intent.PutExtra("path", prefManager.GetString("downloadPath", null));
                intent.PutExtra("name", name);
                context.StartService(intent);
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

        public static async void DownloadFiles(string[] names, string[] videoIDs)
        {
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Android.App.Application.Context);
            if (prefManager.GetString("downloadPath", null) != null)
            {
                Toast.MakeText(Android.App.Application.Context, "Downloading...", ToastLength.Short).Show();
                Context context = Android.App.Application.Context;
                for(int i = 0; i < names.Length; i++)
                {
                    Intent intent = new Intent(context, typeof(Downloader));
                    intent.PutExtra("videoID", videoIDs[i]);
                    intent.PutExtra("path", prefManager.GetString("downloadPath", null));
                    intent.PutExtra("name", names[i]);
                    context.StartService(intent);
                    await Task.Delay(10000);
                }
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

            AlertDialog.Builder builder = new AlertDialog.Builder(context, Resource.Style.AppCompatAlertDialogStyle);
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
                AlertDialog.Builder builder = new AlertDialog.Builder(context, Resource.Style.AppCompatAlertDialogStyle);
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