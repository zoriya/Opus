using Android.Content;
using Android.Database;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MusicApp.Resources.values;
using Square.Picasso;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Android.Provider.MediaStore.Audio;
using RecyclerView = Android.Support.V7.Widget.RecyclerView;
using PopupMenu = Android.Support.V7.Widget.PopupMenu;
using Android.Util;

namespace MusicApp.Resources.Portable_Class
{
    public class PlaylistTracks : Fragment, PopupMenu.IOnMenuItemClickListener, AppBarLayout.IOnOffsetChangedListener
    {
        public static PlaylistTracks instance;
        public string playlistName;
        public RecyclerView ListView;
        public PlaylistTrackAdapter adapter;
        private Android.Support.V7.Widget.Helper.ItemTouchHelper itemTouchHelper;
        public List<Song> result = null;
        private bool Synced = false;
        public long playlistId;
        public string ytID;
        private string author;
        private int count;
        private Uri thumnailURI;
        public bool hasWriteAcess;
        private bool forked;
        private string nextPageToken = null;
        public bool fullyLoadded = true;
        public bool isEmpty = false;
        public bool lastVisible = false;
        public bool useHeader = true;
        public static bool openned = false;
        public bool navigating = false;

        public List<Song> tracks = new List<Song>();
        private List<Song> SyncedTrack;
        private string[] actions = new string[] { "Play", "Play Next", "Play Last", "Remove Track from playlist", "Add To Playlist" };
        private bool loading = false;

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
            MainActivity.instance.DisplaySearch(1);

            int statusHeight = Resources.GetDimensionPixelSize(Resources.GetIdentifier("status_bar_height", "dimen", "android"));
            MainActivity.instance.FindViewById(Resource.Id.collapsingToolbar).LayoutParameters.Height = ViewGroup.LayoutParams.WrapContent;
            MainActivity.instance.FindViewById(Resource.Id.contentLayout).SetPadding(0, 0, 0, 0);
            MainActivity.instance.FindViewById(Resource.Id.toolbar).SetPadding(0, statusHeight, 0, 0);
            MainActivity.instance.FindViewById(Resource.Id.toolbar).LayoutParameters.Height += statusHeight;
        }

        public bool OnMenuItemClick(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.download:
                    YoutubeEngine.DownloadPlaylist(playlistName, ytID);
                    break;

                case Resource.Id.fork:
#pragma warning disable CS4014
                    YoutubeEngine.ForkPlaylist(ytID);
                    break;

                case Resource.Id.addToQueue:
                    if (ytID != null)
                        Playlist.AddToQueue(ytID);
                    else if (playlistId != 0)
                        Playlist.AddToQueue(playlistId);
                    else
                        AddToQueue();
                    break;

                case Resource.Id.rename:
                    AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
                    builder.SetTitle("Playlist name");
                    View view = LayoutInflater.Inflate(Resource.Layout.CreatePlaylistDialog, null);
                    builder.SetView(view);
                    builder.SetNegativeButton("Cancel", (senderAlert, args) => { });
                    builder.SetPositiveButton("Rename", (senderAlert, args) =>
                    {
                        Rename(view.FindViewById<EditText>(Resource.Id.playlistName).Text);
                    });
                    builder.Show();
                    break;

                case Resource.Id.delete:
                    Delete();
                    break;
            }
            return true;
        }

        async void AddToQueue()
        {
            List<Song> songs = tracks;
            if (MusicPlayer.instance == null || MusicPlayer.queue == null || MusicPlayer.queue?.Count == 0)
            {
                Song first = songs[0];
                if (!first.IsYt)
                {
                    Browse.act = Activity;
                    Browse.Play(first, null);
                }
                else
                    YoutubeEngine.Play(first.youtubeID, first.Title, first.Artist, first.Album);

                songs.RemoveAt(0);
            }

            while (MusicPlayer.instance == null)
                await Task.Delay(10);

            songs.Reverse();
            foreach (Song song in songs)
                MusicPlayer.instance.AddToQueue(song);
        }

        async void Rename(string newName)
        {
            if(playlistId == 0)
            {
                try
                {
                    Google.Apis.YouTube.v3.Data.Playlist playlist = new Google.Apis.YouTube.v3.Data.Playlist
                    {
                        Snippet = new PlaylistSnippet()
                    };
                    playlist.Snippet.Title = newName;
                    playlist.Id = ytID;

                    await YoutubeEngine.youtubeService.Playlists.Update(playlist, "snippet").ExecuteAsync();
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    MainActivity.instance.Timout();
                }
            }
            else
            {
                ContentValues value = new ContentValues();
                value.Put(Playlists.InterfaceConsts.Name, newName);
                Activity.ContentResolver.Update(Playlists.ExternalContentUri, value, Playlists.InterfaceConsts.Id + "=?", new string[] { playlistId.ToString() });
            }

            playlistName = newName;
            Activity.FindViewById<TextView>(Resource.Id.headerTitle).Text = playlistName;
        }

        void Delete()
        {
            AlertDialog dialog = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme)
                .SetTitle("Do you want to delete the playlist \"" + playlistName + "\" ?")
                .SetPositiveButton("Yes", async (sender, e) =>
                {
                    if (playlistId == 0)
                    {
                        if (hasWriteAcess)
                        {
                            try
                            {
                                PlaylistsResource.DeleteRequest deleteRequest = YoutubeEngine.youtubeService.Playlists.Delete(ytID);
                                await deleteRequest.ExecuteAsync();
                            }
                            catch (System.Net.Http.HttpRequestException)
                            {
                                MainActivity.instance.Timout();
                            }
                        }
                        else
                        {
                            try
                            {
                                ChannelSectionsResource.ListRequest forkedRequest = YoutubeEngine.youtubeService.ChannelSections.List("snippet,contentDetails");
                                forkedRequest.Mine = true;
                                ChannelSectionListResponse forkedResponse = await forkedRequest.ExecuteAsync();

                                foreach (ChannelSection section in forkedResponse.Items)
                                {
                                    if (section.Snippet.Title == "Saved Playlists")
                                    {
                                        section.ContentDetails.Playlists.Remove(ytID);
                                        ChannelSectionsResource.UpdateRequest request = YoutubeEngine.youtubeService.ChannelSections.Update(section, "snippet,contentDetails");
                                        ChannelSection response = await request.ExecuteAsync();
                                    }
                                }
                            }
                            catch (System.Net.Http.HttpRequestException)
                            {
                                MainActivity.instance.Timout();
                            }
                        }
                    }
                    else
                    {
                        ContentResolver resolver = Activity.ContentResolver;
                        Uri uri = Playlists.ExternalContentUri;
                        resolver.Delete(Playlists.ExternalContentUri, Playlists.InterfaceConsts.Id + "=?", new string[] { playlistId.ToString() });
                    }
                    MainActivity.instance.SupportFragmentManager.PopBackStack();
                })
                .SetNegativeButton("No", (sender, e) => { })
                .Create();
            dialog.Show();
        }

        public override void OnStop()
        {
            Activity.FindViewById<ImageButton>(Resource.Id.headerMore).Click -= PlaylistMore;

            if (!MainActivity.instance.StateSaved)
            {
                int statusHeight = Resources.GetDimensionPixelSize(Resources.GetIdentifier("status_bar_height", "dimen", "android"));
                MainActivity.instance.FindViewById(Resource.Id.toolbar).SetPadding(0, 0, 0, 0);
                TypedValue tv = new TypedValue();
                Activity.Theme.ResolveAttribute(Resource.Attribute.actionBarSize, tv, true);
                int actionBarHeight = Resources.GetDimensionPixelSize(tv.ResourceId);
                MainActivity.instance.FindViewById(Resource.Id.toolbar).LayoutParameters.Height = actionBarHeight;
                MainActivity.instance.FindViewById(Resource.Id.toolbar).RequestLayout();
                MainActivity.instance.FindViewById(Resource.Id.contentLayout).SetPadding(0, statusHeight, 0, 0);
                Activity.FindViewById<RelativeLayout>(Resource.Id.playlistHeader).Visibility = ViewStates.Gone;
                MainActivity.instance.FindViewById(Resource.Id.collapsingToolbar).LayoutParameters.Height = actionBarHeight;

                MainActivity.instance.HideSearch();
                MainActivity.instance.SupportActionBar.SetHomeButtonEnabled(false);
                MainActivity.instance.SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                MainActivity.instance.SupportActionBar.SetDisplayShowTitleEnabled(true);
                MainActivity.instance.SupportActionBar.Title = "MusicApp";

                MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
                Activity.FindViewById<AppBarLayout>(Resource.Id.appbar).RemoveOnOffsetChangedListener(this);


                if (YoutubeEngine.instances != null)
                {
                    MainActivity.instance.FindViewById<TabLayout>(Resource.Id.tabs).Visibility = ViewStates.Visible;
                    Android.Support.V7.Widget.SearchView searchView = (Android.Support.V7.Widget.SearchView)MainActivity.instance.menu.FindItem(Resource.Id.search).ActionView;
                    searchView.Focusable = false;
                    MainActivity.instance.menu.FindItem(Resource.Id.search).ExpandActionView();
                    searchView.SetQuery(YoutubeEngine.searchKeyWorld, false);
                    searchView.ClearFocus();

                    int selectedTab = 0;
                    for (int i = 0; i < YoutubeEngine.instances.Length; i++)
                    {
                        if (YoutubeEngine.instances[i].focused)
                            selectedTab = i;
                    }
                    if (!navigating)
                    {
                        MainActivity.instance?.SupportFragmentManager.BeginTransaction().Attach(YoutubeEngine.instances[selectedTab]).Commit();
                        MainActivity.instance?.SupportFragmentManager.BeginTransaction().Remove(instance).Commit();
                    }
                }
                else if (!navigating)
                    MainActivity.instance?.SupportFragmentManager.PopBackStack();

                instance = null;
            }
            base.OnStop();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.RecyclerFragment, container, false);
            ListView = view.FindViewById<RecyclerView>(Resource.Id.recycler);
            ListView.SetLayoutManager(new Android.Support.V7.Widget.LinearLayoutManager(MainActivity.instance));
            ListView.SetAdapter(new PlaylistTrackAdapter(new List<Song>()));
            ListView.ScrollChange += ListView_ScrollChange;

            PopulateList();
            CreateHeader();
            return view;
        }

        private void ListView_ScrollChange(object sender, View.ScrollChangeEventArgs e)
        {
            if (((Android.Support.V7.Widget.LinearLayoutManager)ListView?.GetLayoutManager())?.FindLastVisibleItemPosition() == adapter?.ItemCount - 1)
                LoadMore();
        }

        void CreateHeader()
        {
            if (useHeader)
            {
                Activity.FindViewById<RelativeLayout>(Resource.Id.playlistHeader).Visibility = ViewStates.Visible;
                ((AppBarLayout.LayoutParams)Activity.FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsingToolbar).LayoutParameters).ScrollFlags = AppBarLayout.LayoutParams.ScrollFlagScroll | AppBarLayout.LayoutParams.ScrollFlagExitUntilCollapsed;
                Activity.FindViewById<AppBarLayout>(Resource.Id.appbar).AddOnOffsetChangedListener(this);
                Activity.FindViewById<TextView>(Resource.Id.headerTitle).Text = playlistName;

                if(!Activity.FindViewById<ImageButton>(Resource.Id.headerPlay).HasOnClickListeners)
                    Activity.FindViewById<ImageButton>(Resource.Id.headerPlay).Click += (sender, e0) => { PlayInOrder(0, false); };
                if(!Activity.FindViewById<ImageButton>(Resource.Id.headerShuffle).HasOnClickListeners)
                    Activity.FindViewById<ImageButton>(Resource.Id.headerShuffle).Click += (sender, e0) => { RandomPlay(); };
                if (!Activity.FindViewById<ImageButton>(Resource.Id.headerMore).HasOnClickListeners)
                    Activity.FindViewById<ImageButton>(Resource.Id.headerMore).Click += PlaylistMore;

                if (playlistId != 0)
                {
                    Activity.FindViewById<TextView>(Resource.Id.headerAuthor).Text = MainActivity.account == null ? "by me" : "by " + MainActivity.account.DisplayName;
                }
                else if (ytID != null && ytID != "")
                {
                    Activity.FindViewById<TextView>(Resource.Id.headerAuthor).Text = author;
                    Activity.FindViewById<TextView>(Resource.Id.headerNumber).Text = count.ToString() + " songs";
                    if (count == -1)
                        Activity.FindViewById<TextView>(Resource.Id.headerNumber).Text = "NaN songs";

                    Picasso.With(Android.App.Application.Context).Load(thumnailURI).Placeholder(Resource.Drawable.noAlbum).Resize(1080, 1080).CenterCrop().Into(Activity.FindViewById<ImageView>(Resource.Id.headerArt));
                }
                Activity.FindViewById(Resource.Id.playlistDark).LayoutParameters.Height = Activity.FindViewById<ImageView>(Resource.Id.headerArt).Height / 2;
            }
        }

        void RandomPlay()
        {
            if (instance.playlistId != 0)
                Playlist.RandomPlay(instance.playlistId, MainActivity.instance);
            else
                YoutubeEngine.RandomPlay(instance.ytID);
        }

        void PlaylistMore(object sender,  System.EventArgs eventArgs)
        {
            PopupMenu menu = new PopupMenu(MainActivity.instance, MainActivity.instance.FindViewById<ImageButton>(Resource.Id.headerMore));
            if (playlistId == 0 && hasWriteAcess)
                menu.Inflate(Resource.Menu.ytplaylist_header_more);
            else if (playlistId == 0 && forked)
                menu.Inflate(Resource.Menu.ytplaylistnowrite_header_more);
            else if (playlistId == 0)
                menu.Inflate(Resource.Menu.ytplaylist_nowrite_nofork_header_more);
            else
                menu.Inflate(Resource.Menu.playlist_header_more);
            menu.SetOnMenuItemClickListener(this);
            menu.Show();
        }

        public static Fragment NewInstance(List<Song> songs, string playlistName)
        {
            instance = new PlaylistTracks { Arguments = new Bundle() };
            instance.tracks = songs;
            instance.playlistName = playlistName;
            instance.useHeader = false;
            instance.fullyLoadded = true;
            instance.hasWriteAcess = false;
            return instance;
        }

        public static Fragment NewInstance(long playlistId, string playlistName)
        {
            instance = new PlaylistTracks { Arguments = new Bundle() };
            instance.playlistId = playlistId;
            instance.playlistName = playlistName;
            instance.fullyLoadded = true;
            instance.hasWriteAcess = true;
            return instance;
        }

        public static Fragment NewInstance(string ytID, long LocalID, string playlistName, bool hasWriteAcess, bool forked, string author, int count, string thumbnailURI)
        {
            instance = new PlaylistTracks { Arguments = new Bundle() };
            instance.ytID = ytID;
            instance.playlistId = LocalID;
            instance.hasWriteAcess = hasWriteAcess;
            instance.forked = forked;
            instance.playlistName = playlistName;
            instance.author = author;
            instance.count = count;
            instance.thumnailURI = Uri.Parse(thumbnailURI);
            instance.fullyLoadded = false;
            return instance;
        }

        public static Fragment NewInstance(string ytID, string playlistName, bool hasWriteAcess, bool forked, string author, int count, string thumbnailURI)
        {
            instance = new PlaylistTracks { Arguments = new Bundle() };
            instance.Synced = true;
            instance.ytID = ytID;
            instance.hasWriteAcess = hasWriteAcess;
            instance.forked = forked;
            instance.playlistName = playlistName;
            instance.author = author;
            instance.count = count;
            instance.thumnailURI = thumbnailURI == null ? null : Uri.Parse(thumbnailURI);
            instance.fullyLoadded = false;
            return instance;
        }

        async Task PopulateList()
        {
            if (playlistId == 0 && ytID == "" && tracks.Count == 0)
                return;

            if (playlistId != 0)
            {
                Uri musicUri = Playlists.Members.GetContentUri("external", playlistId);

                CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
                ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

                tracks = new List<Song>();

                if (musicCursor != null && musicCursor.MoveToFirst())
                {
                    int titleID = musicCursor.GetColumnIndex(Media.InterfaceConsts.Title);
                    int artistID = musicCursor.GetColumnIndex(Media.InterfaceConsts.Artist);
                    int albumID = musicCursor.GetColumnIndex(Media.InterfaceConsts.Album);
                    int thisID = musicCursor.GetColumnIndex(Media.InterfaceConsts.Id);
                    int pathID = musicCursor.GetColumnIndex(Media.InterfaceConsts.Data);
                    do
                    {
                        string Artist = musicCursor.GetString(artistID);
                        string Title = musicCursor.GetString(titleID);
                        string Album = musicCursor.GetString(albumID);
                        long AlbumArt = musicCursor.GetLong(musicCursor.GetColumnIndex(Albums.InterfaceConsts.AlbumId));
                        long id = musicCursor.GetLong(thisID);
                        string path = musicCursor.GetString(pathID);

                        if (Title == null)
                            Title = "Unknown Title";
                        if (Artist == null)
                            Artist = "Unknow Artist";
                        if (Album == null)
                            Album = "Unknow Album";

                        Song song = new Song(Title, Artist, Album, null, AlbumArt, id, path);
                        if (Synced)
                            song = Browse.CompleteItem(song);

                        tracks.Add(song);
                    }
                    while (musicCursor.MoveToNext());
                    musicCursor.Close();
                }

                adapter = new PlaylistTrackAdapter(tracks);
                adapter.ItemClick += ListView_ItemClick;
                adapter.ItemLongClick += ListView_ItemLongClick;
                ListView.SetAdapter(adapter);

                Android.Support.V7.Widget.Helper.ItemTouchHelper.Callback callback = new ItemTouchCallback(adapter);
                itemTouchHelper = new Android.Support.V7.Widget.Helper.ItemTouchHelper(callback);
                itemTouchHelper.AttachToRecyclerView(ListView);

                Activity.FindViewById<TextView>(Resource.Id.headerNumber).Text = tracks.Count.ToString() + " songs";
                var songCover = Uri.Parse("content://media/external/audio/albumart");
                var songAlbumArtUri = ContentUris.WithAppendedId(songCover, tracks[0].AlbumArt);

                Picasso.With(Android.App.Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.noAlbum).Resize(1080, 1080).CenterCrop().Into(Activity.FindViewById<ImageView>(Resource.Id.headerArt));

                if(Synced && ytID != null)
                {
                    SyncedTrack = new List<Song>();
                    try
                    {
                        nextPageToken = "";
                        while (nextPageToken != null)
                        {
                            var ytPlaylistRequest = YoutubeEngine.youtubeService.PlaylistItems.List("snippet, contentDetails");
                            ytPlaylistRequest.PlaylistId = ytID;
                            ytPlaylistRequest.MaxResults = 50;

                            var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                            foreach (var item in ytPlaylist.Items)
                            {
                                if (item.Snippet.Title != "[Deleted video]" && item.Snippet.Title != "Private video" && item.Snippet.Title != "Deleted video")
                                {
                                    Song song = new Song(item.Snippet.Title, item.Snippet.ChannelTitle, item.Snippet.Thumbnails.Default__.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true, false)
                                    {
                                        TrackID = item.Id
                                    };
                                    SyncedTrack.Add(song);
                                }
                            }

                            nextPageToken = ytPlaylist.NextPageToken;
                        }
                    }
                    catch (System.Net.Http.HttpRequestException)
                    {
                        MainActivity.instance.Timout();
                    }

                    List<Song> newFiles = SyncedTrack.FindAll(ytTrack => tracks.Find(track => track.youtubeID == ytTrack.youtubeID) != null);
                    if (newFiles.Count > 0)
                    {
                        Toast.MakeText(MainActivity.instance, "Downloading missing tracks", ToastLength.Short).Show();
                        YoutubeEngine.DownloadFiles(newFiles.ConvertAll(SongToName).ToArray(), newFiles.ConvertAll(SongToYtID).ToArray(), playlistName);
                    }

                    //Set yt id to local songs
                }

            }
            else if (ytID != null)
            {
                try
                {
                    tracks = new List<Song>();
                    var ytPlaylistRequest = YoutubeEngine.youtubeService.PlaylistItems.List("snippet, contentDetails");
                    ytPlaylistRequest.PlaylistId = ytID;
                    ytPlaylistRequest.MaxResults = 50;

                    var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                    foreach (var item in ytPlaylist.Items)
                    {
                        if (item.Snippet.Title != "[Deleted video]" && item.Snippet.Title != "Private video" && item.Snippet.Title != "Deleted video")
                        {
                            Song song = new Song(item.Snippet.Title, item.Snippet.ChannelTitle, item.Snippet.Thumbnails.Default__.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true, false)
                            {
                                TrackID = item.Id
                            };
                            tracks.Add(song);
                        }
                    }

                    nextPageToken = ytPlaylist.NextPageToken;
                    adapter = new PlaylistTrackAdapter(tracks);
                    adapter.ItemClick += ListView_ItemClick;
                    adapter.ItemLongClick += ListView_ItemLongClick;
                    ListView.SetAdapter(adapter);

                    Android.Support.V7.Widget.Helper.ItemTouchHelper.Callback callback = new ItemTouchCallback(adapter);
                    itemTouchHelper = new Android.Support.V7.Widget.Helper.ItemTouchHelper(callback);
                    itemTouchHelper.AttachToRecyclerView(ListView);
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    MainActivity.instance.Timout();
                }
            }
            else if(tracks.Count != 0)
            {
                adapter = new PlaylistTrackAdapter(tracks);
                adapter.ItemClick += ListView_ItemClick;
                adapter.ItemLongClick += ListView_ItemLongClick;
                ListView.SetAdapter(adapter);
            }
        }

        string SongToName(Song song)
        {
            return song.Title;
        }

        string SongToYtID(Song song)
        {
            return song.youtubeID;
        }

        public async void OnRefresh(object sender, System.EventArgs e)
        {
            await PopulateList();
            MainActivity.instance.contentRefresh.Refreshing = false;
        }

        public async Task LoadMore()
        {
            if (nextPageToken == null || loading)
                return;

            loading = true;
            try
            {
                var ytPlaylistRequest = YoutubeEngine.youtubeService.PlaylistItems.List("snippet, contentDetails");
                ytPlaylistRequest.PlaylistId = ytID;
                ytPlaylistRequest.MaxResults = 50;
                ytPlaylistRequest.PageToken = nextPageToken;

                var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                if (instance == null)
                    return;

                foreach (var item in ytPlaylist.Items)
                {
                    if (item.Snippet.Title != "[Deleted video]" && item.Snippet.Title != "Private video" && item.Snippet.Title != "Deleted video")
                    {
                        Song song = new Song(item.Snippet.Title, item.Snippet.ChannelTitle, item.Snippet.Thumbnails.Default__.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true, false)
                        {
                            TrackID = item.Id
                        };
                        tracks.Add(song);
                    }
                }

                nextPageToken = ytPlaylist.NextPageToken;
                if (nextPageToken == null)
                    fullyLoadded = true;
                adapter.NotifyDataSetChanged();
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
            }
            loading = false;
        }

        public void Search(string search)
        {
            System.Console.WriteLine("&FullyLoaded: " + fullyLoadded);
            result = new List<Song>();
            for(int i = 0; i < tracks.Count; i++)
            {
                Song item = tracks[i];
                if (item.Title.ToLower().Contains(search.ToLower()) || item.Artist.ToLower().Contains(search.ToLower()))
                {
                    result.Add(item);
                }
            }
            adapter = new PlaylistTrackAdapter(result);
            adapter.ItemClick += ListView_ItemClick;
            adapter.ItemLongClick += ListView_ItemLongClick;

            Android.Support.V7.Widget.Helper.ItemTouchHelper.Callback callback = new ItemTouchCallback(adapter);
            itemTouchHelper = new Android.Support.V7.Widget.Helper.ItemTouchHelper(callback);
            itemTouchHelper.AttachToRecyclerView(ListView);

            ListView.SetAdapter(adapter);
        }

        private void ListView_ItemClick(object sender, int Position)
        {
            if (!useHeader)
                Position--;

            PlayInOrder(Position, true);
        }

        private void ListView_ItemLongClick(object sender, int Position)
        {
            if (!useHeader)
                Position--;

            Song item = tracks[Position];
            if (result != null && result.Count > Position)
                item = result[Position];

            More(item, Position);
        }

        public void More(Song item, int position)
        {
            if (!useHeader)
                position--;

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
            if (hasWriteAcess && ytID != "")
            {
                builder.SetItems(action.ToArray(), (senderAlert, args) =>
                {
                    switch (args.Which)
                    {
                        case 0:
                            PlayInOrder(position, true);
                            break;

                        case 1:
                            if (!item.IsYt)
                                Browse.PlayNext(item);
                            else
                                YoutubeEngine.PlayNext(item.Path, item.Title, item.Artist, item.Album);
                            break;

                        case 2:
                            if (!item.IsYt)
                                Browse.PlayLast(item);
                            else
                                YoutubeEngine.PlayLast(item.Path, item.Title, item.Artist, item.Album);
                            break;

                        case 3:
                            if (playlistId != 0)
                                RemoveFromPlaylist(item);
                            else if(ytID != null)
                            {
                                YoutubeEngine.RemoveFromPlaylist(item.TrackID);
                                RemoveFromYtPlaylist(item, item.TrackID);
                            }
                            break;

                        case 4:
                            if (item.IsYt)
                                YoutubeEngine.GetPlaylists(item.Path, Activity);
                            else
                                Browse.GetPlaylist(item);
                            break;

                        case 5:
                            if (item.IsYt)
                                YoutubeEngine.Download(item.Title, item.Path);
                            else
                                Browse.EditMetadata(item, "PlaylistTracks", ListView.GetLayoutManager().OnSaveInstanceState());
                            break;

                        default:
                            break;
                    }
                });
            }
            else
            {
                action.Remove("Remove Track from playlist");
                builder.SetItems(action.ToArray(), (senderAlert, args) =>
                {
                    switch (args.Which)
                    {
                        case 0:
                            PlayInOrder(position, true);
                            break;

                        case 1:
                            if (!item.IsYt)
                                Browse.PlayNext(item);
                            else
                                YoutubeEngine.PlayNext(item.Path, item.Title, item.Artist, item.Album);
                            break;

                        case 2:
                            if (!item.IsYt)
                                Browse.PlayLast(item);
                            else
                                YoutubeEngine.PlayLast(item.Path, item.Title, item.Artist, item.Album);
                            break;

                        case 3:
                            if (item.IsYt)
                                YoutubeEngine.GetPlaylists(item.Path, Activity);
                            else
                                Browse.GetPlaylist(item);
                            break;

                        case 4:
                            if (item.IsYt)
                                YoutubeEngine.Download(item.Title, item.Path);
                            else
                                Browse.EditMetadata(item, "PlaylistTracks", ListView.GetLayoutManager().OnSaveInstanceState());
                            break;

                        default:
                            break;
                    }
                });
            }
            builder.Show();
        }

        public async void PlayInOrder(int fromPosition, bool useTransition)
        {
            MusicPlayer.queue?.Clear();
            MusicPlayer.currentID = -1;

            if (ytID != null)
            {
                if (result != null && result.Count > fromPosition)
                    YoutubeEngine.Play(result[fromPosition].youtubeID, result[fromPosition].Title, result[fromPosition].Artist, result[0].Album);
                else
                    YoutubeEngine.Play(tracks[fromPosition].youtubeID, tracks[fromPosition].Title, tracks[fromPosition].Artist, tracks[0].Album);

                while (nextPageToken != null)
                    await LoadMore();
            }

            List<Song> songs = tracks.GetRange(fromPosition, tracks.Count - fromPosition);
            if (result != null && result.Count > fromPosition)
                songs = result.GetRange(fromPosition, result.Count - fromPosition);

            if (!songs[0].IsYt)
            {
                Browse.act = Activity;
                Browse.Play(songs[0], useTransition ? ListView.GetChildAt(fromPosition - ((Android.Support.V7.Widget.LinearLayoutManager)ListView.GetLayoutManager()).FindFirstVisibleItemPosition()).FindViewById<ImageView>(Resource.Id.albumArt) : null);
            }

            songs.RemoveAt(0);
            songs.Reverse();

            while (MusicPlayer.instance == null)
                await Task.Delay(10);

            foreach (Song song in songs)
            {
                MusicPlayer.instance.AddToQueue(song);
            }
            Player.instance?.UpdateNext();
        }

        private void RemoveFromYtPlaylist(Song item, string ytTrackID)
        {
            adapter.Remove(item);
            tracks.Remove(item);
            result?.Remove(item);
        }

        void RemoveFromPlaylist(Song item)
        {
            ContentResolver resolver = Activity.ContentResolver;
            Uri uri = MediaStore.Audio.Playlists.Members.GetContentUri("external", playlistId);
            resolver.Delete(uri, MediaStore.Audio.Playlists.Members.Id + "=?", new string[] { item.Id.ToString() });
            adapter.Remove(item);
            tracks.Remove(item);
            result?.Remove(item);
        }

        public override void OnResume()
        {
            base.OnResume();
            instance = this;
            if (MainActivity.parcelable != null && MainActivity.parcelableSender == "PlaylistTrack")
            {
                ListView.GetLayoutManager().OnRestoreInstanceState(MainActivity.parcelable);
                MainActivity.parcelable = null;
                MainActivity.parcelableSender = null;
            }
        }

        public void OnOffsetChanged(AppBarLayout appBarLayout, int verticalOffset)
        {
            if (instance == null)
                return;

            if (System.Math.Abs(verticalOffset) <= appBarLayout.TotalScrollRange - MainActivity.instance.ToolBar.Height)
            {
                Activity.FindViewById<RelativeLayout>(Resource.Id.playlistHeader).Visibility = ViewStates.Visible;
                MainActivity.instance.SupportActionBar.SetDisplayShowTitleEnabled(false);
            }
            else
            {
                Activity.FindViewById<RelativeLayout>(Resource.Id.playlistHeader).Visibility = ViewStates.Invisible;
                MainActivity.instance.SupportActionBar.SetDisplayShowTitleEnabled(true);
            }
        }

        public void DeleteDialog(int position)
        {
            if (!useHeader)
                position--;

            Song song = tracks[position];
            if (result != null && result.Count > position)
                song = result[position];

            AlertDialog dialog = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme)
                .SetTitle("Remove " + song.Title + " from playlist ?")
                .SetPositiveButton("Yes", (sender, e) =>
                {
                    SnackbarCallback callback = null;
                    if(ytID != null)
                        callback = new SnackbarCallback(position, ytID, song.TrackID);
                    else if(playlistId != 0)
                        callback = new SnackbarCallback(position, song, playlistId);

                    Snackbar snackBar = Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), (song.Title.Length > 20 ? song.Title.Substring(0, 17) + "..." : song.Title) + " has been removed from the playlist.", Snackbar.LengthLong)
                        .SetAction("Undo", (v) =>
                        {
                            callback.canceled = true;
                            if (ytID != null)
                            {
                                if (result != null && result.Count >= position)
                                    result?.Insert(position, song);

                                adapter.Insert(position, song);
                                tracks.Insert(position, song);
                            }
                            if (playlistId != 0)
                            {
                                adapter.Insert(position, song);
                                tracks.Insert(position, song);
                                if (result != null && result.Count >= position)
                                    result?.Insert(position, song);
                            }
                        });
                    snackBar.AddCallback(callback);
                    snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Android.Graphics.Color.White);
                    snackBar.Show();

                    if(ytID != null)
                    {
                        RemoveFromYtPlaylist(song, song.TrackID);
                    }
                    else
                    {
                        adapter.Remove(song);
                        tracks.Remove(song);
                        result?.Remove(song);
                    }
                })
                .SetNegativeButton("No", (sender, e) => { adapter.NotifyItemChanged(position); })
                .Create();
            dialog.Show();
        }
    }
}
