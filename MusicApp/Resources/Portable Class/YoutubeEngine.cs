using Android.Support.V4.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Support.Design.Widget;
using Google.Apis.YouTube.v3;
using Android.Gms.Common.Apis;
using System.Collections.Generic;
using Google.Apis.Services;
using Android.Preferences;
using YoutubeExplode;
using System.Linq;
using MusicApp.Resources.values;
using System.Threading.Tasks;
using Android.Support.V7.App;

namespace MusicApp.Resources.Portable_Class
{
    public class YoutubeEngine : ListFragment
    {
        public static YoutubeEngine instance;
        public static YouTubeService youtubeService;
        public static List<Song> result;

        private View emptyView;
        private bool isEmpty = true;
        private string[] actions = new string[] { "Play", "Play Next", "Play Last", "Download" };

        public const string ApiKey = "AIzaSyBOQyZVnBAKjur0ztBuYPSopS725Qudgc4";
        private string videoID;

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

            CreateYoutube();
        }

        public static  async void CreateYoutube()
        {
            await Task.Run(() =>
            {
                if (youtubeService == null)
                {
                    youtubeService = new YouTubeService(new BaseClientService.Initializer()
                    {
                        ApiKey = ApiKey,
                        ApplicationName = "MusicApp"
                    });
                }
            });
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
            view.SetPadding(0, 100, 0, MainActivity.paddingBot);
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
            videoID = result[e.Position].GetPath();
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
                        Download(item.GetName(), item.GetPath());
                        break;
                    default:
                        break;
                }
            });
            builder.Show();

        }

        private async void Play(string videoID)
        {
            var client = new YoutubeClient();
            var videoInfo = await client.GetVideoInfoAsync(videoID);
            var streamInfo = videoInfo.AudioStreams.OrderBy(s => s.Bitrate).Last();

            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.PutExtra("file", streamInfo.Url);
            intent.PutExtra("title", videoInfo.Title);
            intent.PutExtra("artist", videoInfo.Author.Title);
            intent.PutExtra("thumbnailURI", videoInfo.ImageThumbnailUrl);
            Android.App.Application.Context.StartService(intent);

            MainActivity.instance.HideTabs();
            MainActivity.instance.HideSearch();
            MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, Player.NewInstance()).Commit();
        }

        private async void PlayNext(string videoID)
        {
            var client = new YoutubeClient();
            var videoInfo = await client.GetVideoInfoAsync(videoID);
            var streamInfo = videoInfo.AudioStreams.OrderBy(s => s.Bitrate).Last();

            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("PlayNext");
            intent.PutExtra("file", streamInfo.Url);
            intent.PutExtra("title", videoInfo.Title);
            intent.PutExtra("artist", videoInfo.Author.Title);
            intent.PutExtra("thumbnailURI", videoInfo.ImageThumbnailUrl);
            Android.App.Application.Context.StartService(intent);
        }

        private async void PlayLast(string videoID)
        {
            var client = new YoutubeClient();
            var videoInfo = await client.GetVideoInfoAsync(videoID);
            var streamInfo = videoInfo.AudioStreams.OrderBy(s => s.Bitrate).Last();

            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("PlayLast");
            intent.PutExtra("file", streamInfo.Url);
            intent.PutExtra("title", videoInfo.Title);
            intent.PutExtra("artist", videoInfo.Author.Title);
            intent.PutExtra("thumbnailURI", videoInfo.ImageThumbnailUrl);
            Android.App.Application.Context.StartService(intent);
        }

        private void Download(string name, string videoID)
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
                Snackbar.Make(View, "Download Path Not Set.", Snackbar.LengthShort).SetAction("Set Path", (v) =>
                {
                    Intent intent = new Intent(Android.App.Application.Context, typeof(Preferences));
                    StartActivity(intent);
                }).Show();
            }
        }
    }
}