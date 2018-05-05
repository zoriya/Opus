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
using System.Linq;

namespace MusicApp.Resources.Portable_Class
{
    public class PlaylistTracks : ListFragment
    {
        public static PlaylistTracks instance;
        public string playlistName;
        public Adapter adapter;
        public View emptyView;
        public List<Song> result;
        public long playlistId = 0;
        public string ytID = "";
        public bool isEmpty = false;

        private List<Song> tracks = new List<Song>();
        private List<string> ytTracksIDs = new List<string>();
        private List<string> ytTracksIdsResult;
        private string[] actions = new string[] { "Play", "Play Next", "Play Last", "Remove Track from playlist", "Add To Playlist" };


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            emptyView = LayoutInflater.Inflate(Resource.Layout.NoPlaylist, null);
            ListView.EmptyView = emptyView;
            ListView.Scroll += MainActivity.instance.Scroll;
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
            MainActivity.instance.OnPaddingChanged += OnPaddingChanged;
            MainActivity.instance.DisplaySearch(1);
            PopulateList();
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
            base.OnDestroy();
            instance = null;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = base.OnCreateView(inflater, container, savedInstanceState);
            view.SetPadding(0, 0, 0, MainActivity.defaultPaddingBot);
            return view;
        }

        public static Fragment NewInstance(long playlistId, string playlistName)
        {
            instance = new PlaylistTracks { Arguments = new Bundle() };
            instance.playlistId = playlistId;
            instance.playlistName = playlistName;
            return instance;
        }

        public static Fragment NewInstance(string ytID, string playlistName)
        {
            instance = new PlaylistTracks { Arguments = new Bundle() };
            instance.ytID = ytID;
            instance.playlistName = playlistName;
            return instance;
        }

        async void PopulateList()
        {
            if (playlistId == 0 && ytID == "")
                return;

            if (playlistId != 0)
            {
                Uri musicUri = MediaStore.Audio.Playlists.Members.GetContentUri("external", playlistId);

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
                ListView.Adapter = adapter;
                ListView.TextFilterEnabled = true;
                ListView.ItemClick += ListView_ItemClick;
                ListView.ItemLongClick += ListView_ItemLongClick;

                if (adapter == null || adapter.Count == 0)
                {
                    isEmpty = true;
                    Activity.AddContentView(emptyView, View.LayoutParameters);
                }
            }
            else if (ytID != null)
            {
                tracks = new List<Song>();
                string nextPageToken = "";
                while (nextPageToken != null)
                {
                    var ytPlaylistRequest = YoutubeEngine.youtubeService.PlaylistItems.List("snippet, contentDetails");
                    ytPlaylistRequest.PlaylistId = ytID;
                    ytPlaylistRequest.MaxResults = 50;
                    ytPlaylistRequest.PageToken = nextPageToken;

                    var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                    foreach (var item in ytPlaylist.Items)
                    {
                        Song song = new Song(item.Snippet.Title, item.Snippet.ChannelTitle, item.Snippet.Thumbnails.Default__.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true);
                        tracks.Add(song);
                        ytTracksIDs.Add(item.Id);
                    }

                    nextPageToken = ytPlaylist.NextPageToken;
                }

                adapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, tracks)
                {
                    listPadding = MainActivity.paddingBot - MainActivity.defaultPaddingBot
                };
                ListAdapter = adapter;
                ListView.Adapter = adapter;
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

        private async void OnRefresh(object sender, System.EventArgs e)
        {
            tracks.Clear();
            if (playlistId != 0)
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
            }
            else if (ytID != null)
            {
                string nextPageToken = "";
                while (nextPageToken != null)
                {
                    var ytPlaylistRequest = YoutubeEngine.youtubeService.PlaylistItems.List("snippet, contentDetails");
                    ytPlaylistRequest.PlaylistId = ytID;
                    ytPlaylistRequest.MaxResults = 50;
                    ytPlaylistRequest.PageToken = nextPageToken;

                    var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                    foreach (var item in ytPlaylist.Items)
                    {
                        Song song = new Song(item.Snippet.Title, item.Snippet.ChannelTitle, item.Snippet.Thumbnails.Default__.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true);
                        tracks.Add(song);
                        ytTracksIDs.Add(item.Id);
                    }

                    nextPageToken = ytPlaylist.NextPageToken;
                }
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
        }

        public void Search(string search)
        {
            result = new List<Song>();
            ytTracksIdsResult = new List<string>();
            for(int i = 0; i < tracks.Count; i++)
            {
                Song item = tracks[i];
                if (item.GetName().ToLower().Contains(search.ToLower()) || item.GetArtist().ToLower().Contains(search.ToLower()))
                {
                    result.Add(item);
                    if (ytID != null)
                        ytTracksIdsResult.Add(ytTracksIDs[i]);
                }
            }
            adapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, result)
            {
                listPadding = MainActivity.paddingBot - MainActivity.defaultPaddingBot
            };
            ListAdapter = adapter;
        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            Song item = tracks[e.Position];
            if (result != null && result.Count - 1 >= e.Position)
                item = result[e.Position];

            if (!item.IsYt)
            {
                Browse.act = Activity;
                Browse.Play(item);
            }
            else
                YoutubeEngine.Play(item.GetPath(), item.GetName(), item.GetArtist(), item.GetAlbum());
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
            List<string> action = actions.ToList();

            if (!item.IsYt)
            {
                action.Add("Edit Metadata");
                Browse.act = Activity;
                Browse.inflater = LayoutInflater;
            }
            else
            {
                action.Add("Download");
            }

            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Pick an action");
            builder.SetItems(action.ToArray(), (senderAlert, args) =>
            {
                switch (args.Which)
                {
                    case 0:
                        if (!item.IsYt)
                            Browse.Play(item);
                        else
                            YoutubeEngine.Play(item.GetPath(), item.GetName(), item.GetArtist(), item.GetAlbum());
                        break;

                    case 1:
                        if (!item.IsYt)
                            Browse.PlayNext(item);
                        else
                            YoutubeEngine.PlayNext(item.GetPath(), item.GetName(), item.GetArtist(), item.GetAlbum());
                        break;

                    case 2:
                        if (!item.IsYt)
                            Browse.PlayLast(item);
                        else
                            YoutubeEngine.PlayLast(item.GetPath(), item.GetName(), item.GetArtist(), item.GetAlbum());
                        break;

                    case 3:
                        if (!item.IsYt)
                            RemoveFromPlaylist(item);
                        else
                        {
                            string ytTrackID = ytTracksIDs[position];
                            if (ytTracksIdsResult != null)
                                ytTrackID = ytTracksIdsResult[position];

                            YoutubeEngine.RemoveFromPlaylist(ytTrackID);
                            RemoveFromYtPlaylist(item, ytTrackID);
                        }
                        break;

                    case 4:
                        YoutubeEngine.GetPlaylists(item.GetPath(), Activity);
                        break;

                    case 5:
                        if (item.IsYt)
                            YoutubeEngine.Download(item.GetName(), item.GetPath());
                        else
                            Browse.EditMetadata(item, "PlaylistTracks", ListView.OnSaveInstanceState());
                        break;

                    default:
                        break;
                }
            });
            builder.Show();
        }

        private void RemoveFromYtPlaylist(Song item, string ytTrackID)
        {
            tracks.Remove(item);
            ytTracksIDs.Remove(ytTrackID);
            ytTracksIdsResult?.Remove(ytTrackID);
            adapter.Remove(item);
            if (adapter == null || adapter.Count == 0)
            {
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
        }

        void RemoveFromPlaylist(Song item)
        {
            ContentResolver resolver = Activity.ContentResolver;
            Uri uri = MediaStore.Audio.Playlists.Members.GetContentUri("external", playlistId);
            resolver.Delete(uri, MediaStore.Audio.Playlists.Members.Id + "=?", new string[] { item.GetID().ToString() });
            tracks.Remove(item);
            adapter.Remove(item);
            if (adapter == null || adapter.Count == 0)
            {
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
        }

        public override void OnResume()
        {
            base.OnResume();
            if (MainActivity.parcelable != null && MainActivity.parcelableSender == "PlaylistTrack")
            {
                ListView.OnRestoreInstanceState(MainActivity.parcelable);
                MainActivity.parcelable = null;
                MainActivity.parcelableSender = null;
            }
        }
    }
}