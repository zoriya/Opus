using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Android.Support.V4.App;
using System.Collections.Generic;
using Android.Provider;
using Android.Database;
using Android.Content.PM;
using Android.Support.Design.Widget;
using Android;
using Android.Net;
using Android.Support.V7.App;

namespace MusicApp.Resources.Portable_Class
{
    public class PlaylistTracks : ListFragment
    {
        public static PlaylistTracks instance;
        public Adapter adapter;
        public View emptyView;
        public long playlistId;
        public bool isEmpty = false;

        private List<Song> tracks = new List<Song>();
        private string[] actions = new string[] { "Play", "Play Next", "Play Last", "Remove Track from playlist" };


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            emptyView = LayoutInflater.Inflate(Resource.Layout.NoPlaylist, null);
            ListView.EmptyView = emptyView;
            ListAdapter = adapter;

            InitialiseSearchService();
            PopulateList();
        }

        public override void OnDestroy()
        {
            instance = null;
            MainActivity.instance.RemoveSearchService(1);
            base.OnDestroy();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = base.OnCreateView(inflater, container, savedInstanceState);
            view.SetPadding(0, 100, 0, 0);
            return view;
        }

        public static Fragment NewInstance(long playlistId)
        {
            instance = new PlaylistTracks { Arguments = new Bundle() };
            instance.playlistId = playlistId;
            return instance;
        }

        private void InitialiseSearchService()
        {
            MainActivity.instance.CreateSearch(1);
        }

        void PopulateList()
        {
            Uri musicUri = MediaStore.Audio.Playlists.Members.GetContentUri("external", playlistId);

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
                    string Artist = musicCursor.GetString(artistID);
                    string Title = musicCursor.GetString(titleID);
                    string Album = musicCursor.GetString(albumID);
                    long AlbumArt = musicCursor.GetLong(musicCursor.GetColumnIndex(MediaStore.Audio.Albums.InterfaceConsts.AlbumId));
                    long id = musicCursor.GetLong(thisID);
                    string path = musicCursor.GetString(pathID);

                    if (Title == null)
                        Title = "Unknown Title";
                    if (Artist == null)
                        Artist = "Unknow Artist";
                    if (Album == null)
                        Album = "Unknow Album";

                    tracks.Add(new Song(Title, Artist, Album, AlbumArt, id, path));
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

        public void Search(string search)
        {
            List<Song> result = new List<Song>();
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
        }

        private void ListView_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            Browse.act = Activity;
            Browse.inflater = LayoutInflater;
            Song item = tracks[e.Position];
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, Resource.Style.AppCompatAlertDialogStyle);
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
                        RemoveFromPlaylist(item);
                        break;
                    default:
                        break;
                }
            });
            builder.Show();
        }

        void RemoveFromPlaylist(Song item)
        {
            ContentResolver resolver = Activity.ContentResolver;
            Uri uri = MediaStore.Audio.Playlists.Members.GetContentUri("external", playlistId);
            resolver.Delete(uri, MediaStore.Audio.Playlists.Members.Id + "=?", new string[] { item.GetID().ToString() });
            tracks.Remove(item);
            adapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, tracks);
            ListAdapter = adapter;
            if (adapter == null || adapter.Count == 0)
            {
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
        }
    }
}