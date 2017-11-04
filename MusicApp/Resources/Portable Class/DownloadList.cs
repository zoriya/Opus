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
using static MusicApp.Resources.Portable_Class.YoutubeExtractor;
using Android.Util;

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
        }

        public override void OnDestroy()
        {
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
            view.SetPadding(0, 100, 0, MainActivity.paddingBot);
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

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            Song song = list[e.Position];
            YoutubeExtractor extractor = new YoutubeExtractor();
            extractor.OnExtractionComplete += DownloadList_OnExtractionComplete;
            extractor.Extract(song.GetPath(), true, true, song);
        }

        private void DownloadList_OnExtractionComplete(SparseArray<YtFile> ytFiles, Song song)
        {
            System.Console.WriteLine("Extraction complete : " + ytFiles.Size());
            if(ytFiles == null || ytFiles.Size() < 1)
            {
                Toast.MakeText(Android.App.Application.Context, "Download failed, try again", ToastLength.Short).Show();
                return;
            }

            YtFile ytFile = GetBestStream(ytFiles);
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("YTPlay");
            intent.PutExtra("file", ytFile.url);
            intent.PutStringArrayListExtra("song", new string[] { song.GetName(), song.GetArtist(), song.GetAlbum() });
            Activity.StartService(intent);
        }

        private YtFile GetBestStream(SparseArray<YtFile> ytFiles)
        {
            List<int> keys = new List<int>();
            for (int i = 0; i < ytFiles.Size(); i++)
            {
                System.Console.WriteLine("Key " + i + " : " + ytFiles.KeyAt(i));
                keys.Add(ytFiles.KeyAt(i));
            }

            // WEBM Dash Audio
            if (keys.Contains(251))
                return ytFiles.Get(251);
            if (keys.Contains(171))
                return ytFiles.Get(171);
            if (keys.Contains(250))
                return ytFiles.Get(250);
            if (keys.Contains(249))
                return ytFiles.Get(249);

            // Dash Audio
            if (keys.Contains(141))
                return ytFiles.Get(141);
            if (keys.Contains(140))
                return ytFiles.Get(140);

            // Video and Audio
            if (keys.Contains(22))
                return ytFiles.Get(22);
            if (keys.Contains(43))
                return ytFiles.Get(43);
            if (keys.Contains(18))
                return ytFiles.Get(18);
            if (keys.Contains(5))
                return ytFiles.Get(5);
            if (keys.Contains(36))
                return ytFiles.Get(36);
            if (keys.Contains(17))
                return ytFiles.Get(17);

            // HLS Live Stream
            if (keys.Contains(96))
                return ytFiles.Get(96);
            if (keys.Contains(95))
                return ytFiles.Get(95);
            if (keys.Contains(94))
                return ytFiles.Get(94);
            if (keys.Contains(93))
                return ytFiles.Get(93);
            if (keys.Contains(92))
                return ytFiles.Get(92);
            if (keys.Contains(91))
                return ytFiles.Get(91);

            return null;
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