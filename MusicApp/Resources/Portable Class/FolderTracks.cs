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
using System.Threading.Tasks;

namespace MusicApp.Resources.Portable_Class
{
    public class FolderTracks : ListFragment
    {
        public static FolderTracks instance;
        public string folderName;
        public Adapter adapter;
        public View emptyView;
        public List<Song> result;
        public bool isEmpty = false;
        public string path;

        private List<Song> tracks = new List<Song>();
        private readonly string[] actions = new string[] { "Play", "Play Next", "Play Last", "Add To Playlist" };


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            emptyView = LayoutInflater.Inflate(Resource.Layout.NoPlaylist, null);
            ListView.EmptyView = emptyView;
            ListView.Scroll += MainActivity.instance.Scroll;
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
            MainActivity.instance.OnPaddingChanged += OnPaddingChanged;

            PopulateList();
            MainActivity.instance.DisplaySearch(1);
        }

        private void OnPaddingChanged(object sender, PaddingChange e)
        {
            if (MainActivity.paddingBot > e.oldPadding)
                adapter.listPadding = MainActivity.paddingBot - MainActivity.defaultPaddingBot;
            else
                adapter.listPadding = (int)(8 * MainActivity.instance.Resources.DisplayMetrics.Density + 0.5f);
        }

        public override void OnDestroy()
        {
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
            MainActivity.instance.OnPaddingChanged -= OnPaddingChanged;
            instance = null;
            base.OnDestroy();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = base.OnCreateView(inflater, container, savedInstanceState);
            view.SetPadding(0, 0, 0, MainActivity.defaultPaddingBot);
            return view;
        }

        public static Fragment NewInstance(string path, string folderName)
        {
            instance = new FolderTracks { Arguments = new Bundle() };
            instance.path = path;
            instance.folderName = folderName;
            return instance;
        }

        void PopulateList()
        {
            Uri musicUri = MediaStore.Audio.Media.GetContentUriForPath(path);

            CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

            tracks = new List<Song>();

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

            adapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, tracks)
            {
                listPadding = MainActivity.paddingBot - MainActivity.defaultPaddingBot
            };
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

            adapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, tracks)
            {
                listPadding = MainActivity.paddingBot - MainActivity.defaultPaddingBot
            };
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
                if (item.Name.ToLower().Contains(search.ToLower()) || item.Artist.ToLower().Contains(search.ToLower()))
                {
                    result.Add(item);
                }
            }
            adapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, result)
            {
                listPadding = MainActivity.paddingBot - MainActivity.defaultPaddingBot
            };
            ListAdapter = adapter;
        }

        private async void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            Browse.act = Activity;

            Song item = tracks[e.Position];
            List<Song> queue = tracks.GetRange(e.Position + 1, tracks.Count - e.Position - 1);
            if (result != null)
            {
                item = result[e.Position];
                queue = result.GetRange(e.Position + 1, result.Count - e.Position - 1);
            }
            queue.Reverse();

            Browse.Play(item, ListView.GetChildAt(e.Position - ListView.FirstVisiblePosition).FindViewById<ImageView>(Resource.Id.albumArt));

            while(MusicPlayer.instance == null)
                await Task.Delay(10);

            foreach(Song song in queue)
            {
                MusicPlayer.instance.AddToQueue(song);
            }
            Player.instance.UpdateNext();
        }

        private void ListView_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            Song item = tracks[e.Position];
            if (result != null)
                item = result[e.Position];

            More(item, e.Position);
        }

        public void More(Song item, int position)
        {
            Browse.act = Activity;
            Browse.inflater = LayoutInflater;

            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Pick an action");
            builder.SetItems(actions, async (senderAlert, args) =>
            {
                switch (args.Which)
                {
                    case 0:
                        Browse.act = Activity;
                        int Position = tracks.IndexOf(item);

                        List<Song> queue = tracks.GetRange(Position + 1, tracks.Count - Position - 1);
                        if (result != null)
                        {
                            queue = result.GetRange(Position + 1, result.Count - Position - 1);
                        }
                        queue.Reverse();

                        Browse.Play(item, ListView.GetChildAt(position - ListView.FirstVisiblePosition).FindViewById<ImageView>(Resource.Id.albumArt));

                        while (MusicPlayer.instance == null)
                            await Task.Delay(10);

                        foreach (Song song in queue)
                        {
                            MusicPlayer.instance.AddToQueue(song);
                        }
                        Player.instance.UpdateNext();
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

        public override void OnResume()
        {
            base.OnResume();
            instance = this;
        }
    }
}