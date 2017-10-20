using Android.OS;
using System.Collections.Generic;
using Android.Widget;
using Android.Net;
using Android.Database;
using Android.Provider;
using MusicApp.Resources.values;
using Android.Content;
using Android;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Content.PM;
using Android.Support.V4.App;
using Android.Support.V7.App;

namespace MusicApp.Resources.Portable_Class
{
    public class FolderBrowse : ListFragment
    {
        public static FolderBrowse instance;
        public static Context act;
        public static LayoutInflater inflater;
        public List<string> paths = new List<string>();
        public List<int> pathUse = new List<int>();
        public ArrayAdapter adapter;
        public View emptyView;

        private View view;
        private string[] actions = new string[] { "List songs", "Add To Playlist", "Random Play" };
        private bool isEmpty = false;


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            act = Activity;
            inflater = LayoutInflater;
            emptyView = LayoutInflater.Inflate(Resource.Layout.NoSong, null);
            ListView.EmptyView = emptyView;

            if(ListView.Adapter == null)
                PopulateList();
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
            act = null;
            inflater = null;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = base.OnCreateView(inflater, container, savedInstanceState);
            this.view = view;
            view.SetPadding(0, 0, 0, 100);
            return view;
        }

        public static Fragment NewInstance()
        {
            instance = new FolderBrowse { Arguments = new Bundle() };
            return instance;
        }

        void PopulateList()
        {
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


                    if (!paths.Contains(path))
                    {
                        paths.Add(path);
                        pathUse.Add(1);
                    }
                    else
                        pathUse[paths.IndexOf(path)] += 1;
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }

            adapter = new TwoLineAdapter(Android.App.Application.Context, Resource.Layout.TwoLineLayout, paths, pathUse);
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

        public void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            ListSongs(paths[e.Position]);
        }

        private void ListView_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            string path = paths[e.Position];

            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, Resource.Style.AppCompatAlertDialogStyle);
            builder.SetTitle("Pick an action");
            builder.SetItems(actions, (senderAlert, args) =>
            {
                switch (args.Which)
                {
                    case 0:
                        ListSongs(path);
                        break;
                    case 1:
                        GetPlaylist(path);
                        break;
                    case 2:
                        //random play
                        break;
                    default:
                        break;
                }
            });
            builder.Show();
        }

        void ListSongs(string path)
        {
            AppCompatActivity act = (AppCompatActivity)Activity;
            act.SupportActionBar.SetHomeButtonEnabled(true);
            act.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            act.SupportActionBar.Title = path;
            MainActivity.instance.HideTabs();
            FragmentTransaction transaction = FragmentManager.BeginTransaction();
            transaction.Replace(Resource.Id.contentView, FolderTracks.NewInstance(path));
            transaction.AddToBackStack(null);
            transaction.Commit();
        }

        public void GetPlaylist(string item)
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

            AlertDialog.Builder builder = new AlertDialog.Builder(act, Resource.Style.AppCompatAlertDialogStyle);
            builder.SetTitle("Add to a playlist");
            builder.SetItems(playList.ToArray(), (senderAlert, args) =>
            {
                AddToPlaylist(item, playList[args.Which], playListId[args.Which]);
            });
            builder.Show();
        }

        public void AddToPlaylist(string path, string playList, long playlistID)
        {
            if (playList == "Create a playlist")
                CreatePlalistDialog(path);

            else
            {
                ContentResolver resolver = act.ContentResolver;

                Uri musicUri = MediaStore.Audio.Media.GetContentUriForPath(path);

                CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
                ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

                if (musicCursor != null && musicCursor.MoveToFirst())
                {
                    int thisID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Id);
                    do
                    {
                        long id = musicCursor.GetLong(thisID);

                        ContentValues value = new ContentValues();
                        value.Put(MediaStore.Audio.Playlists.Members.AudioId, id);
                        value.Put(MediaStore.Audio.Playlists.InterfaceConsts.Id, playlistID);
                        value.Put(MediaStore.Audio.Playlists.Members.PlayOrder, 0);
                        resolver.Insert(MediaStore.Audio.Playlists.Members.GetContentUri("external", playlistID), value);
                    }
                    while (musicCursor.MoveToNext());
                    musicCursor.Close();
                }
            }
        }

        public void CreatePlalistDialog(string path)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(act, Resource.Style.AppCompatAlertDialogStyle);
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

        public void CreatePlaylist(string name, string path)
        {
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
    }
}