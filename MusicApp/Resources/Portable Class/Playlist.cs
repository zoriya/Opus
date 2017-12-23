using Android.Content;
using Android.Database;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using static Android.Provider.MediaStore.Audio;

namespace MusicApp.Resources.Portable_Class
{
    public class Playlist : ListFragment
    {
        public static Playlist instance;
        public Adapter adapter;
        public View emptyView;
        public bool isEmpty = false;

        private List<string> playList = new List<string>();
        private List<int> playListCount = new List<int>();
        private List<long> playlistId = new List<long>();
        private string[] actions = new string[] { "Random play", "Rename", "Delete" };


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            emptyView = LayoutInflater.Inflate(Resource.Layout.NoPlaylist, null);
            ListView.EmptyView = emptyView;
            ListView.TextFilterEnabled = true;
            ListView.ItemClick += ListView_ItemClick;
            ListView.ItemLongClick += ListView_ItemLongClick;

            if (ListView.Adapter == null)
                MainActivity.instance.GetStoragePermission();
        }

        public void AddEmptyView()
        {
            if (emptyView.Parent != null)
                ((ViewGroup)emptyView.Parent).RemoveView(emptyView);

            Activity.AddContentView(emptyView, View.LayoutParameters);
        }

        public void RemoveEmptyView()
        {
            ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
            rootView.RemoveView(emptyView);
        }

        public override void OnDestroy()
        {
            if (isEmpty)
            {
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(emptyView);
            }
            System.Console.WriteLine("Playlist destroyed");
            base.OnDestroy();
            instance = null;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = base.OnCreateView(inflater, container, savedInstanceState);
            view.SetPadding(0, 0, 0, MainActivity.paddingBot);
            return view;
        }

        public static Fragment NewInstance()
        {
            instance = new Playlist { Arguments = new Bundle() };
            return instance;
        }

        public void PopulateView()
        {
            Uri uri = Playlists.ExternalContentUri;
            CursorLoader loader = new CursorLoader(Android.App.Application.Context, uri, null, null, null, null);
            ICursor cursor = (ICursor)loader.LoadInBackground();

            if (cursor != null && cursor.MoveToFirst())
            {
                int nameID = cursor.GetColumnIndex(Playlists.InterfaceConsts.Name);
                //int countID = cursor.GetColumnIndex(MediaStore.Audio.Playlists.InterfaceConsts.Count);
                int listID = cursor.GetColumnIndex(Playlists.InterfaceConsts.Id);
                do
                {
                    string name = cursor.GetString(nameID);
                    //int count = cursor.GetInt(countID);
                    long id = cursor.GetLong(listID);
                    playList.Add(name);
                    //playListCount.Add(count);
                    playlistId.Add(id);

                }
                while (cursor.MoveToNext());
                cursor.Close();
            }
            //ListAdapter = new TwoLineAdapter(Android.App.Application.Context, Resource.Layout.TwoLineLayout, playList, playListCount);
            ListAdapter = new ArrayAdapter(Android.App.Application.Context, Resource.Layout.PlaylistList, playList);

            if (adapter == null || adapter.Count == 0)
            {
                if (isEmpty)
                    return;
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
        }

        public void Refresh()
        {

        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            AppCompatActivity act = (AppCompatActivity)Activity;
            act.SupportActionBar.SetHomeButtonEnabled(true);
            act.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            act.SupportActionBar.Title = playList[e.Position];

            MainActivity.instance.HideTabs();
            MainActivity.instance.Transition(Resource.Id.contentView, PlaylistTracks.NewInstance(playlistId[e.Position]), true);
        }

        private void ListView_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, Resource.Style.AppCompatAlertDialogStyle);
            builder.SetTitle("Pick an action");
            builder.SetItems(actions, (senderAlert, args) => 
            {
                switch (args.Which)
                {
                    case 0:
                        RandomPlay(playlistId[e.Position]);
                        break;
                    case 1:
                        Rename(e.Position, playlistId[e.Position]);
                        break;
                    case 2:
                        RemovePlaylist(e.Position, playlistId[e.Position]);
                        break;
                    default:
                        break;
                }
            });
            builder.Show();
        }

        void RandomPlay(long playlistID)
        {
            List<string> tracksPath = new List<string>();
            Uri musicUri = MediaStore.Audio.Playlists.Members.GetContentUri("external", playlistID);

            CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();


            if (musicCursor != null && musicCursor.MoveToFirst())
            {
                int pathID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Data);
                do
                {
                    string path = musicCursor.GetString(pathID);

                    tracksPath.Add(path);
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }

            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.PutStringArrayListExtra("files", tracksPath);
            intent.SetAction("RandomPlay");
            Activity.StartService(intent);
        }

        void Rename(int position, long playlistID)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, Resource.Style.AppCompatAlertDialogStyle);
            builder.SetTitle("Playlist name");
            View view = LayoutInflater.Inflate(Resource.Layout.CreatePlaylistDialog, null);
            builder.SetView(view);
            builder.SetNegativeButton("Cancel", (senderAlert, args) => { });
            builder.SetPositiveButton("Rename", (senderAlert, args) =>
            {
                RenamePlaylist(position, view.FindViewById<EditText>(Resource.Id.playlistName).Text, playlistID);
            });
            builder.Show();
        }

        void RenamePlaylist(int position, string name, long playlistID)
        {
            ContentResolver resolver = Activity.ContentResolver;
            Uri uri = MediaStore.Audio.Playlists.ExternalContentUri;
            ContentValues value = new ContentValues();
            value.Put(MediaStore.Audio.Playlists.InterfaceConsts.Name, name);
            resolver.Update(MediaStore.Audio.Playlists.ExternalContentUri, value, MediaStore.Audio.Playlists.InterfaceConsts.Id + "=?", new string[] { playlistID.ToString() });
            playList[position] = name;
            ListAdapter = new ArrayAdapter(Android.App.Application.Context, Resource.Layout.PlaylistList, playList);
            if (ListAdapter.Count == 0)
            {
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
        }

        void RemovePlaylist(int position, long playlistID)
        {
            ContentResolver resolver = Activity.ContentResolver;
            Uri uri = Playlists.ExternalContentUri;
            resolver.Delete(Playlists.ExternalContentUri, Playlists.InterfaceConsts.Id + "=?", new string[] { playlistID.ToString() });
            playList.RemoveAt(position);
            playlistId.RemoveAt(position);
            ListAdapter = new ArrayAdapter(Android.App.Application.Context, Resource.Layout.PlaylistList, playList);
            if(ListAdapter.Count == 0)
            {
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
        }
    }
}