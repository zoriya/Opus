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
        private string[] actions = new string[] { "Add To Playlist" };
        private bool isEmpty = false;


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            act = Activity;
            inflater = LayoutInflater;
            emptyView = LayoutInflater.Inflate(Resource.Layout.NoSong, null);
            ListView.EmptyView = emptyView;

            GetStoragePermission();
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

        void GetStoragePermission()
        {
            const string permission = Manifest.Permission.ReadExternalStorage;
            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(Android.App.Application.Context, permission) == (int)Permission.Granted)
            {
                PopulateList();
                return;
            }
            string[] permissions = new string[] { permission };
            RequestPermissions(permissions, 0);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            switch (requestCode)
            {
                case 0:
                    if (grantResults[0] == Permission.Granted)
                        PopulateList();

                    else
                        Snackbar.Make(View, "Permission denied, can't list musics.", Snackbar.LengthShort).Show();

                    break;
            }
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
            
        }

        void ListSongs(string paths)
        {
            //AppCompatActivity act = (AppCompatActivity)Activity;
            //act.SupportActionBar.SetHomeButtonEnabled(true);
            //act.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            //act.SupportActionBar.Title = playList[e.Position];
            //FragmentTransaction transaction = FragmentManager.BeginTransaction();
            //transaction.Replace(Resource.Id.contentView, PlaylistTracks.NewInstance(playlistId[e.Position]));
            //transaction.AddToBackStack(null);
            //transaction.Commit();
        }
    }
}