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
    public class PlaylistTracks : ListFragment
    {
        public static PlaylistTracks instance;
        public Adapter adapter;
        public View emptyView;
        public List<Song> result;
        public long playlistId = 0;
        public string ytID = "";
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
        }

        public override void OnDestroy()
        {
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
            view.SetPadding(0, MainActivity.paddinTop, 0, MainActivity.paddingBot);
            return view;
        }

        public static Fragment NewInstance(long playlistId)
        {
            instance = new PlaylistTracks { Arguments = new Bundle() };
            instance.playlistId = playlistId;
            return instance;
        }

        public static Fragment NewInstance(string ytID)
        {
            instance = new PlaylistTracks { Arguments = new Bundle() };
            instance.ytID = ytID;
            return instance;
        }

        async void PopulateList()
        {
            if(playlistId != 0)
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
            else if(ytID != null)
            {
                string nextPageToken = "";
                while(nextPageToken != null)
                {
                    var ytPlaylistRequest = YoutubeEngine.youtubeService.PlaylistItems.List("snippet");
                    ytPlaylistRequest.PlaylistId = ytID;
                    ytPlaylistRequest.MaxResults = 50;
                    ytPlaylistRequest.PageToken = nextPageToken;

                    var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                    foreach (var item in ytPlaylist.Items)
                    {
                        Song song = new Song(item.Snippet.Title, item.Snippet.ChannelTitle, item.Snippet.Thumbnails.Default__.Url, -1, -1, item.Id, true);
                        System.Console.WriteLine(song.GetName());
                        tracks.Add(song);
                    }

                    nextPageToken = ytPlaylist.NextPageToken;
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

            if (!item.IsYt)
            {
                Browse.act = Activity;
                Browse.Play(item);
            }
            else
                YoutubeEngine.Play(item.GetPath());
        }

        private void ListView_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            Song item = tracks[e.Position];
            if (result != null)
                item = result[e.Position];

            if (!item.IsYt)
            {
                Browse.act = Activity;
                Browse.inflater = LayoutInflater;
            }

            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, Resource.Style.AppCompatAlertDialogStyle);
            builder.SetTitle("Pick an action");
            builder.SetItems(actions, (senderAlert, args) =>
            {
                switch (args.Which)
                {
                    case 0:
                        if (!item.IsYt)
                            Browse.Play(item);
                        else
                            YoutubeEngine.Play(item.GetPath());
                        break;
                    case 1:
                        if (!item.IsYt)
                            Browse.PlayNext(item);
                        else
                            YoutubeEngine.PlayNext(item.GetPath());
                        break;
                    case 2:
                        if (!item.IsYt)
                            Browse.PlayLast(item);
                        else
                            YoutubeEngine.PlayLast(item.GetPath());
                        break;
                    case 3:
                        if (!item.IsYt)
                            RemoveFromPlaylist(item);
                        else
                            YoutubeEngine.RemoveFromPlaylist(item.GetPath(), ytID);
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