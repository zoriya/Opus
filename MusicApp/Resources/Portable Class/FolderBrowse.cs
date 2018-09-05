using Android;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicApp.Resources.Portable_Class
{
    public class FolderBrowse : ListFragment
    {
        public static FolderBrowse instance;
        public static Context act;
        public static LayoutInflater inflater;
        public List<string> pathDisplay = new List<string>();
        public List<string> paths = new List<string>();
        public List<int> pathUse = new List<int>();
        public TwoLineAdapter adapter;
        public View emptyView;
        public bool populated = false;
        public bool focused = false;

        private View view;
        private readonly string[] actions = new string[] { "List songs", "Play in order", "Add To Playlist", "Random Play" };
        private bool isEmpty = false;


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            act = Activity;
            inflater = LayoutInflater;
            emptyView = LayoutInflater.Inflate(Resource.Layout.NoSong, null);
            ListView.EmptyView = emptyView;
            ListView.Scroll += MainActivity.instance.Scroll;
            ListView.ItemClick += ListView_ItemClick;
            ListView.ItemLongClick += ListView_ItemLongClick;
            ListView.NestedScrollingEnabled = true;
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;

            if (ListView.Adapter == null)
                PopulateList();
        }

        public override void OnDestroy()
        {
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
            if (isEmpty)
            {
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(emptyView);
            }
            base.OnDestroy();
            instance = null;
            act = null;
            inflater = null;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = base.OnCreateView(inflater, container, savedInstanceState);
            this.view = view;
            return view;
        }

        public static Fragment NewInstance()
        {
            instance = new FolderBrowse { Arguments = new Bundle() };
            return instance;
        }

        public void PopulateList()
        {
            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(Android.App.Application.Context, Manifest.Permission.ReadExternalStorage) != (int)Permission.Granted)
                return;

            populated = true;

            Uri musicUri = MediaStore.Audio.Media.ExternalContentUri;

            CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();


            if (musicCursor != null && musicCursor.MoveToFirst())
            {
                int pathID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Data);
                do
                {
                    string path = musicCursor.GetString(pathID);
                    path = path.Substring(0, path.LastIndexOf("/"));
                    string displayPath = path.Substring(path.LastIndexOf("/") + 1, path.Length - (path.LastIndexOf("/") + 1));

                    if (!paths.Contains(path))
                    {
                        pathDisplay.Add(displayPath);
                        paths.Add(path);
                        pathUse.Add(1);
                    }
                    else
                        pathUse[paths.IndexOf(path)] += 1;
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }

            adapter = new TwoLineAdapter(Android.App.Application.Context, Resource.Layout.TwoLineLayout, pathDisplay, pathUse);
            ListAdapter = adapter;

            if (adapter == null || adapter.Count == 0)
            {
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
        }

        private void OnRefresh(object sender, System.EventArgs e)
        {
            if (!focused)
                return;
            Refresh();
            MainActivity.instance.contentRefresh.Refreshing = false;
        }

        public void Refresh()
        {
            Uri musicUri = MediaStore.Audio.Media.ExternalContentUri;

            CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

            paths.Clear();
            pathDisplay.Clear();
            pathUse.Clear();


            if (musicCursor != null && musicCursor.MoveToFirst())
            {
                int pathID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Data);
                do
                {
                    string path = musicCursor.GetString(pathID);
                    path = path.Substring(0, path.LastIndexOf("/"));
                    string displayPath = path.Substring(path.LastIndexOf("/") + 1, path.Length - (path.LastIndexOf("/") + 1));

                    if (!paths.Contains(path))
                    {
                        pathDisplay.Add(displayPath);
                        paths.Add(path);
                        pathUse.Add(1);
                    }
                    else
                        pathUse[paths.IndexOf(path)] += 1;
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }

            adapter = new TwoLineAdapter(Android.App.Application.Context, Resource.Layout.TwoLineLayout, pathDisplay, pathUse);
            ListAdapter = adapter;

            if (adapter == null || adapter.Count == 0)
            {
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
        }

        public void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            ListSongs(pathDisplay[e.Position], paths[e.Position]);
        }

        private void ListView_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            More(e.Position);
        }

        public void More(int position)
        {
            string path = paths[position];
            string displayPath = pathDisplay[position];

            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Pick an action");
            builder.SetItems(actions, (senderAlert, args) =>
            {
                switch (args.Which)
                {
                    case 0:
                        ListSongs(displayPath, path);
                        break;
                    case 1:
                        PlayInOrder(path);
                        break;
                    case 2:
                        GetPlaylist(path);
                        break;
                    case 3:
                        RandomPlay(path);
                        break;
                    default:
                        break;
                }
            });
            builder.Show();
        }

        void ListSongs(string displayPath, string path)
        {
            AppCompatActivity act = (AppCompatActivity)Activity;
            act.SupportActionBar.SetHomeButtonEnabled(true);
            act.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            act.SupportActionBar.Title = displayPath;

            MainActivity.instance.HideTabs();
            MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, FolderTracks.NewInstance(path, displayPath)).AddToBackStack(null).Commit();
        }

        async void PlayInOrder(string folderPath)
        {
            List<Song> songs = new List<Song>();
            Uri musicUri = MediaStore.Audio.Media.GetContentUriForPath(folderPath);
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

                    if (!path.Contains(folderPath))
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

                    songs.Add(new Song(Title, Artist, Album, null, AlbumArt, id, path));
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();

                songs.Reverse();
                Browse.act = Activity;
                Browse.Play(songs[0], null);

                while (MusicPlayer.instance == null)
                    await Task.Delay(10);

                foreach (Song song in songs)
                {
                    MusicPlayer.instance.AddToQueue(song);
                }

                Player.instance.UpdateNext();
            }
        }

        public void GetPlaylist(string path)
        {
            List<string> playList = new List<string>();
            List<long> playListId = new List<long>();
            playList.Add("Create a playlist");
            playListId.Add(0);

            Uri uri = MediaStore.Audio.Playlists.ExternalContentUri;
            CursorLoader loader = new CursorLoader(Android.App.Application.Context, uri, null, null, null, null);
            ICursor cursor = (ICursor)loader.LoadInBackground();

            if (cursor != null && cursor.MoveToFirst())
            {
                int nameID = cursor.GetColumnIndex(MediaStore.Audio.Playlists.InterfaceConsts.Name);
                int playlistID = cursor.GetColumnIndex(MediaStore.Audio.Playlists.InterfaceConsts.Id);
                do
                {
                    string name = cursor.GetString(nameID);
                    long id = cursor.GetLong(playlistID);
                    playList.Add(name);
                    playListId.Add(id);
                }
                while (cursor.MoveToNext());
                cursor.Close();
            }

            AlertDialog.Builder builder = new AlertDialog.Builder(act, MainActivity.dialogTheme);
            builder.SetTitle("Add to a playlist");
            builder.SetItems(playList.ToArray(), (senderAlert, args) =>
            {
                AddToPlaylist(path, playList[args.Which], playListId[args.Which]);
            });
            builder.Show();
        }

        public async void AddToPlaylist(string path, string playList, long playlistID)
        {
            if (playList == "Create a playlist")
                CreatePlalistDialog(path);

            else
            {
                await Browse.CheckWritePermission();

                List<ContentValues> values = new List<ContentValues>();
                ContentResolver resolver = act.ContentResolver;

                Uri musicUri = MediaStore.Audio.Media.GetContentUriForPath(path);
                CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
                ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

                if (musicCursor != null && musicCursor.MoveToFirst())
                {
                    int thisID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Id);
                    int pathID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Data);
                    do
                    {
                        string songPath = musicCursor.GetString(pathID);

                        if (!songPath.Contains(path))
                            continue;

                        long id = musicCursor.GetLong(thisID);

                        ContentValues value = new ContentValues();
                        value.Put(MediaStore.Audio.Playlists.Members.AudioId, id);
                        value.Put(MediaStore.Audio.Playlists.Members.PlayOrder, 0);
                        values.Add(value);
                    }
                    while (musicCursor.MoveToNext());
                    musicCursor.Close();
                }
                resolver.BulkInsert(MediaStore.Audio.Playlists.Members.GetContentUri("external", playlistID), values.ToArray());
            }
        }

        public void CreatePlalistDialog(string path)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(act, MainActivity.dialogTheme);
            builder.SetTitle("Playlist name");
            View view = inflater.Inflate(Resource.Layout.CreatePlaylistDialog, null);
            builder.SetView(view);
            builder.SetNegativeButton("Cancel", (senderAlert, args) => { });
            builder.SetPositiveButton("Create", (senderAlert, args) =>
            {
                CreatePlaylist(view.FindViewById<EditText>(Resource.Id.playlistName).Text, path);
            });
            builder.Show();
        }

        public async void CreatePlaylist(string name, string path)
        {
            await Browse.CheckWritePermission();

            ContentResolver resolver = act.ContentResolver;
            Uri uri = MediaStore.Audio.Playlists.ExternalContentUri;
            ContentValues value = new ContentValues();
            value.Put(MediaStore.Audio.Playlists.InterfaceConsts.Name, name);
            resolver.Insert(uri, value);

            long playlistID = 0;

            CursorLoader loader = new CursorLoader(Android.App.Application.Context, uri, null, null, null, null);
            ICursor cursor = (ICursor)loader.LoadInBackground();

            if (cursor != null && cursor.MoveToFirst())
            {
                int nameID = cursor.GetColumnIndex(MediaStore.Audio.Playlists.InterfaceConsts.Name);
                int getplaylistID = cursor.GetColumnIndex(MediaStore.Audio.Playlists.InterfaceConsts.Id);
                do
                {
                    string playlistName = cursor.GetString(nameID);
                    long id = cursor.GetLong(getplaylistID);

                    if (playlistName == name)
                        playlistID = id;
                }
                while (cursor.MoveToNext());
                cursor.Close();
            }

            AddToPlaylist(path, "foo", playlistID);
        }

        void RandomPlay(string folderPath)
        {
            List<string> trackPaths = new List<string>();
            Uri musicUri = MediaStore.Audio.Media.ExternalContentUri;

            CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();


            if (musicCursor != null && musicCursor.MoveToFirst())
            {
                int pathID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Data);
                do
                {
                    string path = musicCursor.GetString(pathID);

                    if (path.Contains(folderPath))
                        trackPaths.Add(path);
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }


            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.PutStringArrayListExtra("files", trackPaths.ToArray());
            intent.SetAction("RandomPlay");
            Activity.StartService(intent);
            MainActivity.instance.ShowSmallPlayer();
            MainActivity.instance.ShowPlayer();
        }

        public override void OnResume()
        {
            base.OnResume();
            instance = this;
            if (MainActivity.parcelable != null && MainActivity.parcelableSender == "FolderBrowse")
            {
                ListView.OnRestoreInstanceState(MainActivity.parcelable);
                MainActivity.parcelable = null;
                MainActivity.parcelableSender = null;
            }
        }
    }
}