using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Support.V7.App;
using Android.Support.V4.App;
using System.Collections.Generic;
using Android.Provider;
using Android.Database;
using Android.Content.PM;
using Android.Support.Design.Widget;
using Android;
using Android.Net;
using YoutubeSearch;
using MusicApp.Resources.values;
using System.Linq;
using System;
using System.IO;
using System.Threading.Tasks;
using Android.Media;
using YoutubeExplode;
using YoutubeExplode.Models;
using Android.Support.V7.Preferences;

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

            InitialiseSearchService();
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
            MainActivity.instance.RemoveSearchService(2);
        }

        private void InitialiseSearchService()
        {
            MainActivity.instance.CreateSearch(2);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = base.OnCreateView(inflater, container, savedInstanceState);
            view.SetPadding(0, 100, 0, 0);
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
            Toast.MakeText(Android.App.Application.Context, "Play: comming soon", ToastLength.Short).Show();

        }

        private void ListView_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Android.App.Application.Context);
            if(prefManager.GetString("downloadPath", null) != null)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                DownloadAudio(list[e.Position], prefManager.GetString("downloadPath", null));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            else
            {
                Toast.MakeText(Android.App.Application.Context, "Download path not set", ToastLength.Short).Show();
            }




            //ContentResolver resolver = Activity.ContentResolver;
            //ContentValues value = new ContentValues();
            //value.Put(MediaStore.Audio.Media.InterfaceConsts.Title, title);
            //value.Put(MediaStore.Audio.Media.InterfaceConsts.Artist, artist);
            //value.Put(MediaStore.Audio.Media.InterfaceConsts.Album, album);
            //value.Put(MediaStore.Audio.Media.InterfaceConsts.Data, path);
            //resolver.Insert(MediaStore.Audio.Media.ExternalContentUri, value);
        }

        private async Task DownloadAudio(Song song, string path)
        {
            Console.WriteLine("Download Started");
            string videoID = song.GetPath().Remove(0, song.GetPath().IndexOf("=") + 1);

            var client = new YoutubeClient();
            var videoInfo = await client.GetVideoInfoAsync(videoID);

            Toast.MakeText(Android.App.Application.Context, "Dowloading: " + videoInfo.Title, ToastLength.Short).Show();


            // Select the highest quality mixed stream
            // (can also use VideoStreams or AudioStreams, if needed)
            var streamInfo = videoInfo.AudioStreams.OrderBy(s => s.Bitrate).Last();

            Console.WriteLine("Document: " + path);

            // Download it to file
            string fileExtension = streamInfo.Container.GetFileExtension();
            string fileName = $"{videoInfo.Title}.{fileExtension}";

            string filePath = Path.Combine(path, fileName);
            Console.WriteLine("Path: " + filePath);

            var input = await client.GetMediaStreamAsync(streamInfo);
            Console.WriteLine("Input done");

            var output = File.Create(filePath);
            Console.WriteLine("File created");
            await input.CopyToAsync(output);
            output.Dispose();

            Toast.MakeText(Android.App.Application.Context, "Download finish: " + videoInfo.Title, ToastLength.Long).Show();

            /*It's working, actualy download .webm file to the good path but .webm is a video file. Downloading only audio but do not convert to a mp3 file
             *Check for a converter add on or check if this downloader can convert to a mp3 file
             *Add a progress bar
             */
        }
    }
}