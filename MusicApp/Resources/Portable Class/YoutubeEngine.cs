using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Google.Apis.YouTube.v3;
using Java.Util;
using MusicApp.Resources.values;
using System.Collections.Generic;
using System.Linq;
using YoutubeExplode;
using YoutubeExplode.Models.MediaStreams;
using System;
using System.Threading.Tasks;

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
                        GetPlaylists(item.GetPath());
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

            await Task.Delay(10000);

            for(int i = 1; i < files.Length; i++)
            {
                MusicPlayer.queue.Add(files[i]);
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

        public static void RemoveFromPlaylist(string videoID, string playlistID)
        {
            HashMap parameters = new HashMap();
            parameters.Put("id", videoID);
            parameters.Put("onBehalfOfContentOwner", "");

            PlaylistItemsResource.DeleteRequest deleteRequest = youtubeService.PlaylistItems.Delete(playlistID);
            if (parameters.ContainsKey("onBehalfOfContentOwner") && parameters.Get("onBehalfOfContentOwner").ToString() != "")
            {
                deleteRequest.OnBehalfOfContentOwner = parameters.Get("onBehalfOfContentOwner").ToString();
            }

            deleteRequest.Execute();
        }


        public static async void GetPlaylists(string videoID)
        {
            if (MainActivity.instance.TokenHasExpire())
            {
                youtubeService = null;
                MainActivity.instance.Login();

                while (youtubeService == null)
                    await Task.Delay(500);
            }

            List<string> playList = new List<string>();
            List<long> playListId = new List<long>();
            playList.Add("Create a playlist");
            playListId.Add(0);

            

            //AlertDialog.Builder builder = new AlertDialog.Builder(act, Resource.Style.AppCompatAlertDialogStyle);
            //builder.SetTitle("Add to a playlist");
            //builder.SetItems(playList.ToArray(), (senderAlert, args) =>
            //{
            //    AddToPlaylist(item, playList[args.Which], playListId[args.Which]);
            //});
            //builder.Show();
        }
    }
}