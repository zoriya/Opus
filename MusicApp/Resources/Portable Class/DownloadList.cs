using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Support.V4.App;
using System.Collections.Generic;
using Android.Support.Design.Widget;
using YoutubeSearch;
using MusicApp.Resources.values;
using Android.Support.V7.Preferences;
using YoutubeExtractor;
using System.Linq;
using System.Threading.Tasks;
using Square.Picasso;
using System;

namespace MusicApp.Resources.Portable_Class
{
    public class DownloadList : ListFragment
    {
        public static DownloadList instance;

        private View emptyView;
        private bool isEmpty = true;
        private List<Song> list = new List<Song>();


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            emptyView = LayoutInflater.Inflate(Resource.Layout.DownloadLayout, null);
            ListView.EmptyView = emptyView;
            ListView.ItemClick += ListView_ItemClick;
            ListView.ItemLongClick += ListView_ItemLongClick; ;
            ListAdapter = null;
            Activity.AddContentView(emptyView, View.LayoutParameters);
            if (MusicPlayer.isRunning)
            {
                Song current = MusicPlayer.queue[MusicPlayer.CurrentID()];

                RelativeLayout smallPlayer = Activity.FindViewById<RelativeLayout>(Resource.Id.smallPlayer);
                FrameLayout parent = (FrameLayout)smallPlayer.Parent;
                parent.Visibility = ViewStates.Visible;
                smallPlayer.Visibility = ViewStates.Visible;
                smallPlayer.FindViewById<TextView>(Resource.Id.spTitle).Text = current.GetName();
                smallPlayer.FindViewById<TextView>(Resource.Id.spArtist).Text = current.GetArtist();
                ImageView art = smallPlayer.FindViewById<ImageView>(Resource.Id.spArt);

                var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                var songAlbumArtUri = ContentUris.WithAppendedId(songCover, current.GetAlbumArt());

                Picasso.With(Android.App.Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Into(art);

                smallPlayer.FindViewById<ImageButton>(Resource.Id.spLast).Click += Last_Click;
                smallPlayer.FindViewById<ImageButton>(Resource.Id.spPlay).Click += Play_Click;
                smallPlayer.FindViewById<ImageButton>(Resource.Id.spNext).Click += Next_Click;
            }
        }

        private void Last_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("Previus");
            Activity.StartService(intent);
        }

        private void Play_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("Pause");
            Activity.StartService(intent);
        }

        private void Next_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("Next");
            Activity.StartService(intent);
        }

        public override void OnDestroy()
        {
            RelativeLayout smallPlayer = Activity.FindViewById<RelativeLayout>(Resource.Id.smallPlayer);
            FrameLayout parent = (FrameLayout)smallPlayer.Parent;
            parent.Visibility = ViewStates.Gone;
            smallPlayer.FindViewById<ImageButton>(Resource.Id.spLast).Click -= Last_Click;
            smallPlayer.FindViewById<ImageButton>(Resource.Id.spPlay).Click -= Play_Click;
            smallPlayer.FindViewById<ImageButton>(Resource.Id.spNext).Click -= Next_Click;

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
            if (!MusicPlayer.isRunning)
                view.SetPadding(0, 100, 0, 0);
            else
                view.SetPadding(0, 360, 0, 0);
            return view;
        }

        public static Fragment NewInstance()
        {
            instance = new DownloadList { Arguments = new Bundle() };
            return instance;
        }

        public void Search(string search)
        {
            if(search == null || search == "")
            {
                if(!isEmpty)
                    Activity.AddContentView(emptyView, View.LayoutParameters);
                return;
            }
            if (!isEmpty)
            {
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(emptyView);
                isEmpty = false;
            }

            list.Clear();
            var items = new VideoSearch();

            foreach(var item in items.SearchQuery(search, 1))
            {
                new YTitemToSong(item, out Song song);
                list.Add(song); 
            }

            ListAdapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, list);
        }

        private /*async*/ void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            //await Task.Run(() =>
            //{
            Toast.MakeText(Android.App.Application.Context, "Playing : " + list[e.Position].GetPath(), ToastLength.Short).Show();
            IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(list[e.Position].GetPath(), false);
            //VideoInfo video = videoInfos.Where(info => info.VideoType == VideoType.Mp4 && info.Resolution == 0).OrderByDescending(info => info.AudioBitrate).First();
            VideoInfo video = videoInfos.Where(info => info.AudioBitrate > 0 && info.AdaptiveType == AdaptiveType.Audio).OrderByDescending(info => info.AudioBitrate).First();

            if (video.RequiresDecryption)
            {
                DownloadUrlResolver.DecryptDownloadUrl(video);
            }

            System.Console.WriteLine(video.DownloadUrl);

            Song song = list[e.Position];

            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("YTPlay");
            intent.PutExtra("file", video.DownloadUrl);
            intent.PutStringArrayListExtra("song", new string[] { song.GetName(), song.GetArtist(), song.GetAlbum() });
            Activity.StartService(intent);
            //});
        }

        private void ListView_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Android.App.Application.Context);
            if(prefManager.GetString("downloadPath", null) != null)
            {
                Toast.MakeText(Android.App.Application.Context, "Downloading...", ToastLength.Short).Show();
                Context context = Android.App.Application.Context;
                Intent intent = new Intent(context, typeof(Downloader));
                intent.PutExtra("file", list[e.Position].GetPath());
                intent.PutExtra("path", prefManager.GetString("downloadPath", null));
                intent.PutExtra("name", list[e.Position].GetName());
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