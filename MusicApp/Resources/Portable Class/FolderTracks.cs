using Android.Content;
using Android.Database;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using System.Collections.Generic;

namespace MusicApp.Resources.Portable_Class
{
    public class FolderTracks : ListFragment
    {
        public static FolderTracks instance;
        public Adapter adapter;
        public View emptyView;
        public List<Song> result;
        public bool isEmpty = false;
        public string path;

        private List<Song> tracks = new List<Song>();
        private string[] actions = new string[] { "Play", "Play Next", "Play Last", "Add To Playlist" };


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            emptyView = LayoutInflater.Inflate(Resource.Layout.NoPlaylist, null);
            ListView.EmptyView = emptyView;
            ListView.Scroll += MainActivity.instance.Scroll;
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;

            PopulateList();
            MainActivity.instance.DisplaySearch(1);
        }

        public override void OnDestroy()
        {
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
            instance = null;
            base.OnDestroy();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = base.OnCreateView(inflater, container, savedInstanceState);
            view.SetPadding(0, 0, 0, MainActivity.paddingBot);
            return view;
        }

        public static Fragment NewInstance(string path)
        {
            instance = new FolderTracks { Arguments = new Bundle() };
            instance.path = path;
            return instance;
        }

        void PopulateList()
        {
            Uri musicUri = MediaStore.Audio.Media.GetContentUriForPath(path);

            CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();


            if (musicCursor != null && musicCursor.MoveToFirst())
            {
                int titleID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Title);
                int artistID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Artist);
                int albumID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Album);
                int thisID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Id);
                int pathID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Data);
                do
                {
                    string path = musicCursor.GetString(pathID);

                    if (!path.Contains(this.path))
                        continue;

                    string Artist = musicCursor.GetString(artistID);
                    string Title = musicCursor.GetString(titleID);
                    string Album = musicCursor.GetString(albumID);
                    long AlbumArt = musicCursor.GetLong(musicCursor.GetColumnIndex(MediaStore.Audio.Albums.InterfaceConsts.AlbumId));
                    long id = musicCursor.GetLong(thisID);

                    if (Title == null)
                        Title = "Unknown Title";
                    if (Artist == null)
                        Artist = "Unknow Artist";
                    if (Album == null)
                        Album = "Unknow Album";

                    System.Console.WriteLine(Title);
                    tracks.Add(new Song(Title, Artist, Album, null, AlbumArt, id, path));
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }

            adapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, tracks);
            ListAdapter = adapter;
            ListView.TextFilterEnabled = true;
            ListView.ItemClick += ListView_ItemClick;
            ListView.ItemLongClick += ListView_ItemLongClick;

            if (adapter == null || adapter.Count == 0)
            {
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
        }

        private void OnRefresh(object sender, System.EventArgs e)
        {
            tracks.Clear();

            Uri musicUri = MediaStore.Audio.Media.GetContentUriForPath(path);

            CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();


            if (musicCursor != null && musicCursor.MoveToFirst())
            {
                int titleID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Title);
                int artistID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Artist);
                int albumID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Album);
                int thisID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Id);
                int pathID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Data);
                do
                {
                    string path = musicCursor.GetString(pathID);

                    if (!path.Contains(this.path))
                        continue;

                    string Artist = musicCursor.GetString(artistID);
                    string Title = musicCursor.GetString(titleID);
                    string Album = musicCursor.GetString(albumID);
                    long AlbumArt = musicCursor.GetLong(musicCursor.GetColumnIndex(MediaStore.Audio.Albums.InterfaceConsts.AlbumId));
                    long id = musicCursor.GetLong(thisID);

                    if (Title == null)
                        Title = "Unknown Title";
                    if (Artist == null)
                        Artist = "Unknow Artist";
                    if (Album == null)
                        Album = "Unknow Album";

                    tracks.Add(new Song(Title, Artist, Album, null, AlbumArt, id, path));
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }

            adapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, tracks);
            ListAdapter = adapter;

            if (adapter == null || adapter.Count == 0)
            {
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
            MainActivity.instance.contentRefresh.Refreshing = false;
        }

        public void Search(string search)
        {
            result = new List<Song>();
            foreach (Song item in tracks)
            {
                if (item.GetName().ToLower().Contains(search.ToLower()) || item.GetArtist().ToLower().Contains(search.ToLower()))
                {
                    result.Add(item);
                }
            }
            ListAdapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, result);
        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            Browse.act = Activity;

            Song item = tracks[e.Position];
            if (result != null)
                item = result[e.Position];

            Browse.Play(item);
        }

        private void ListView_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            Song item = tracks[e.Position];
            if (result != null)
                item = result[e.Position];

            More(item);
        }

        public void More(Song item)
        {
            Browse.act = Activity;
            Browse.inflater = LayoutInflater;

            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Pick an action");
            builder.SetItems(actions, (senderAlert, args) =>
            {
                switch (args.Which)
                {
                    case 0:
                        Browse.Play(item);
                        break;
                    case 1:
                        Browse.PlayNext(item);
                        break;
                    case 2:
                        Browse.PlayLast(item);
                        break;
                    case 3:
                        Browse.GetPlaylist(item);
                        break;
                    default:
                        break;
                }
            });
            builder.Show();
        }
    }
}