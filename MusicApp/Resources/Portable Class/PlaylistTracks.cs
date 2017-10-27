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

using EventArgs = System.EventArgs;
using Square.Picasso;

namespace MusicApp.Resources.Portable_Class
{
    public class PlaylistTracks : ListFragment
    {
        public static PlaylistTracks instance;
        public Adapter adapter;
        public View emptyView;
        public List<Song> result;
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

            PopulateList();
            MainActivity.instance.DisplaySearch(1);

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

            ActionBar toolbar = MainActivity.instance.SupportActionBar;
            if (toolbar != null)
            {
                toolbar.Title = "MusicApp";
                toolbar.SetDisplayHomeAsUpEnabled(false);
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

        public static Fragment NewInstance(long playlistId)
        {
            instance = new PlaylistTracks { Arguments = new Bundle() };
            instance.playlistId = playlistId;
            return instance;
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
            Song item = tracks[e.Position];
            if (result != null)
                item = result[e.Position];

            Browse.act = Activity;
            Browse.Play(item);
        }

        private void ListView_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            Browse.act = Activity;
            Browse.inflater = LayoutInflater;
            Song item = tracks[e.Position];
            if (result != null)
                item = result[e.Position];

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