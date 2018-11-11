using Android.Content;
using Android.Database;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MusicApp.Resources.values;
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Android.Provider.MediaStore.Audio;

namespace MusicApp.Resources.Portable_Class
{
    public class Playlist : Fragment
    {
        public static Playlist instance;
        public RecyclerView ListView;
        private PlaylistAdapter adapter;
        private bool populating = false;

        private List<PlaylistItem> LocalPlaylists = new List<PlaylistItem>();
        private List<PlaylistItem> YoutubePlaylists = new List<PlaylistItem>();


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
        }

        public override void OnDestroy()
        {
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
            base.OnDestroy();
            instance = null;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.RecyclerFragment, container, false);
            ListView = view.FindViewById<RecyclerView>(Resource.Id.recycler);
            ListView.SetLayoutManager(new LinearLayoutManager(Android.App.Application.Context));
            instance = this;

#pragma warning disable CS4014
            populating = false;
            PopulateView();
            return view;
        }

        public async Task PopulateView()
        {
            if (!populating)
            {
                populating = true;

                //Synced playlists
                List<PlaylistItem> SyncedPlaylists = new List<PlaylistItem>();
                await Task.Run(() =>
                {
                    SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "SyncedPlaylists.sqlite"));
                    db.CreateTable<PlaylistItem>();

                    SyncedPlaylists = db.Table<PlaylistItem>().ToList();
                });

                //Initialisation
                LocalPlaylists.Clear();
                YoutubePlaylists.Clear();
                LocalPlaylists.Add(new PlaylistItem("Header", -1));
                YoutubePlaylists.Add(new PlaylistItem("Header", null));
                PlaylistItem Loading = new PlaylistItem("Loading", null);

                //Local playlists
                Android.Net.Uri uri = Playlists.ExternalContentUri;
                CursorLoader loader = new CursorLoader(Android.App.Application.Context, uri, null, null, null, null);
                ICursor cursor = (ICursor)loader.LoadInBackground();

                if (cursor != null && cursor.MoveToFirst())
                {
                    int nameID = cursor.GetColumnIndex(Playlists.InterfaceConsts.Name);
                    int listID = cursor.GetColumnIndex(Playlists.InterfaceConsts.Id);
                    do
                    {
                        string name = cursor.GetString(nameID);
                        long id = cursor.GetLong(listID);

                        PlaylistItem ytPlaylist = SyncedPlaylists.Find(x => x.LocalID == id);
                        if (ytPlaylist == null) 
                        {
                            Android.Net.Uri musicUri = Playlists.Members.GetContentUri("external", id);
                            CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
                            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

                            LocalPlaylists.Add(new PlaylistItem(name, id, musicCursor.Count));
                        }
                        else
                        {
                            if (ytPlaylist.YoutubeID == null)
                                ytPlaylist.SyncState = SyncState.Loading;
                            else
                                ytPlaylist.SyncState = SyncState.True;

                            YoutubePlaylists.Add(ytPlaylist);
                        }
                    }
                    while (cursor.MoveToNext());
                    cursor.Close();
                }

                if (LocalPlaylists.Count == 1)
                    LocalPlaylists.Add(new PlaylistItem("EMPTY - You don't have any playlist on your device.", -1));

                YoutubePlaylists.Add(Loading);
                adapter = new PlaylistAdapter(LocalPlaylists, YoutubePlaylists);
                ListView.SetAdapter(adapter);
                adapter.ItemClick += ListView_ItemClick;
                adapter.ItemLongCLick += ListView_ItemLongClick;
                ListView.SetItemAnimator(new DefaultItemAnimator());
                ListView.ScrollChange += MainActivity.instance.Scroll;

                //Youtube playlists
                if (!await MainActivity.instance.WaitForYoutube())
                {
                    YoutubePlaylists.Remove(Loading);
                    YoutubePlaylists.Add(new PlaylistItem("Error", null));
                    adapter.SetYtPlaylists(YoutubePlaylists, false);
                    return;
                }

                try
                {
                    YouTubeService youtube = YoutubeEngine.youtubeService;

                    PlaylistsResource.ListRequest request = youtube.Playlists.List("snippet,contentDetails");
                    request.Mine = true;
                    request.MaxResults = 25;
                    PlaylistListResponse response = await request.ExecuteAsync();

                    if (instance == null)
                        return;

                    for (int i = 0; i < response.Items.Count; i++)
                    {
                        Google.Apis.YouTube.v3.Data.Playlist playlist = response.Items[i];
                        PlaylistItem item = new PlaylistItem(playlist.Snippet.Title, playlist.Id, playlist, (int)playlist.ContentDetails.ItemCount)
                        {
                            Owner = playlist.Snippet.ChannelTitle,
                            ImageURL = playlist.Snippet.Thumbnails.High.Url,
                            HasWritePermission = true
                        };

                        if (SyncedPlaylists.Find(x => x.YoutubeID == item.YoutubeID) != null)
                        {
                            int position = YoutubePlaylists.FindIndex(x => x.Name == item.Name);
                            YoutubePlaylists[position].Snippet = item.Snippet;
                            YoutubePlaylists[position].Count = item.Count;
                            SyncedPlaylists.RemoveAll(x => x.YoutubeID == item.YoutubeID);
                        }
                        else if (SyncedPlaylists.Find(x => x.Name == item.Name) != null)
                        {
                            item.LocalID = SyncedPlaylists.Find(x => x.Name == item.Name).LocalID;
                            int position = YoutubePlaylists.FindIndex(x => x.Name == item.Name);
                            YoutubePlaylists[position] = item;
                            adapter.NotifyItemChanged(position);

                            Task.Run(() =>
                            {
                                SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "SyncedPlaylists.sqlite"));
                                db.CreateTable<PlaylistItem>();
                                db.InsertOrReplace(item);
                            });
                            SyncedPlaylists.RemoveAll(x => x.LocalID == item.LocalID);
                        }
                        else
                            YoutubePlaylists.Add(item);
                    }


                    YoutubePlaylists.Remove(Loading);
                    YoutubePlaylists.Add(Loading);
                    adapter.SetYtPlaylists(YoutubePlaylists, false);

                    //Saved playlists
                    ChannelSectionsResource.ListRequest forkedRequest = youtube.ChannelSections.List("snippet,contentDetails");
                    forkedRequest.Mine = true;
                    ChannelSectionListResponse forkedResponse = await forkedRequest.ExecuteAsync();
                    if (instance == null)
                        return;

                    foreach (ChannelSection section in forkedResponse.Items)
                    {
                        if (section.Snippet.Title == "Saved Playlists")
                        {
                            for (int i = 0; i < section.ContentDetails.Playlists.Count; i++)
                            {
                                PlaylistsResource.ListRequest plRequest = youtube.Playlists.List("snippet, contentDetails");
                                plRequest.Id = section.ContentDetails.Playlists[i];
                                PlaylistListResponse plResponse = await plRequest.ExecuteAsync();

                                if (instance == null)
                                    return;

                                Google.Apis.YouTube.v3.Data.Playlist playlist = plResponse.Items[i];
                                playlist.Kind = "youtube#saved";
                                PlaylistItem item = new PlaylistItem(playlist.Snippet.Title, playlist.Id, playlist, (int)playlist.ContentDetails.ItemCount)
                                {
                                    Owner = playlist.Snippet.ChannelTitle,
                                    ImageURL = playlist.Snippet.Thumbnails.High.Url,
                                    HasWritePermission = false
                                };

                                if (SyncedPlaylists.Find(x => x.YoutubeID == item.YoutubeID) != null)
                                {
                                    int position = YoutubePlaylists.FindIndex(x => x.Name == item.Name);
                                    YoutubePlaylists[position].Snippet = item.Snippet;
                                    YoutubePlaylists[position].Count = item.Count;
                                    SyncedPlaylists.RemoveAll(x => x.YoutubeID == item.YoutubeID);
                                }
                                else if (SyncedPlaylists.Find(x => x.Name == item.Name) != null)
                                {
                                    item.LocalID = SyncedPlaylists.Find(x => x.Name == item.Name).LocalID;
                                    int position = YoutubePlaylists.FindIndex(x => x.Name == item.Name);
                                    YoutubePlaylists[position] = item;
                                    adapter.NotifyItemChanged(position);

                                    Task.Run(() =>
                                    {
                                        SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "SyncedPlaylists.sqlite"));
                                        db.CreateTable<PlaylistItem>();
                                        db.InsertOrReplace(item);
                                    });
                                    SyncedPlaylists.RemoveAll(x => x.LocalID == item.LocalID);
                                }
                                else
                                    YoutubePlaylists.Add(item);
                            }
                        }
                    }

                    YoutubePlaylists.Remove(Loading);

                    if (YoutubePlaylists.Count == 1)
                    {
                        YoutubePlaylists.Add(new PlaylistItem("EMPTY", "You don't have any youtube playlist on your account. \nWarning: Only playlist from your google account are displayed"));
                    }
                    adapter.SetYtPlaylists(YoutubePlaylists, true);

                    if (SyncedPlaylists.Count > 0)
                    {
                        List<PlaylistItem> BadSync = SyncedPlaylists.FindAll(x => x.SyncState == SyncState.Loading);
                        LocalPlaylists.AddRange(BadSync);

                        for (int i = 0; i < SyncedPlaylists.Count; i++)
                            SyncedPlaylists[i].SyncState = SyncState.Error;

                        if(BadSync.Count > 0)
                        {
                            if (LocalPlaylists[1].Name.StartsWith("EMPTY - "))
                            {
                                LocalPlaylists.RemoveAt(1);
                                adapter.NotifyItemRemoved(1);
                            }
                            adapter.NotifyItemRangeInserted(LocalPlaylists.Count - SyncedPlaylists.Count, BadSync.Count);
                        }
                    }
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    YoutubePlaylists[1] = new PlaylistItem("Error", null);
                    adapter.SetYtPlaylists(YoutubePlaylists, false);
                    MainActivity.instance.Timout();
                }
                populating = false;
            }
        }

        public static Fragment NewInstance()
        {
            instance = new Playlist { Arguments = new Bundle() };
            return instance;
        }

        private async void OnRefresh(object sender, System.EventArgs e)
        {
            await Refresh();
            MainActivity.instance.contentRefresh.Refreshing = false;
        }

        public async Task Refresh()
        {
            await PopulateView();
        }

        private void ListView_ItemClick(object sender, int Position)
        {
            if(Position == LocalPlaylists.Count + YoutubePlaylists.Count)
            {
                View view = LayoutInflater.Inflate(Resource.Layout.SaveAPlaylist, null);
                AlertDialog dialog = new AlertDialog.Builder(Activity, MainActivity.dialogTheme)
                    .SetTitle("Add a Playlist")
                    .SetView(view)
                    .SetNegativeButton("Cancel", (s, eventArgs) => { })
                    .SetPositiveButton("Go", async (s, eventArgs) => 
                    {
                        string url = view.FindViewById<EditText>(Resource.Id.playlistURL).Text;
                        string shrinkedURL = url.Substring(url.IndexOf('=') + 1);
                        string playlistID = shrinkedURL;
                        if (shrinkedURL.Contains("&"))
                        {
                            playlistID = shrinkedURL.Substring(0, shrinkedURL.IndexOf("&"));
                        }
                        await YoutubeEngine.ForkPlaylist(playlistID);

                        try
                        {
                            ChannelSectionsResource.ListRequest forkedRequest = YoutubeEngine.youtubeService.ChannelSections.List("snippet,contentDetails");
                            forkedRequest.Mine = true;
                            ChannelSectionListResponse forkedResponse = await forkedRequest.ExecuteAsync();
                            if (instance == null)
                                return;

                            foreach (ChannelSection section in forkedResponse.Items)
                            {
                                if (section.Snippet.Title == "Saved Playlists")
                                {
                                    PlaylistsResource.ListRequest plRequest = YoutubeEngine.youtubeService.Playlists.List("snippet, contentDetails");
                                    plRequest.Id = section.ContentDetails.Playlists[section.ContentDetails.Playlists.Count - 1];
                                    PlaylistListResponse plResponse = await plRequest.ExecuteAsync();

                                    if (instance == null)
                                        return;

                                    Google.Apis.YouTube.v3.Data.Playlist ytPlaylist = plResponse.Items[0];
                                    ytPlaylist.Kind = "youtube#saved";
                                    PlaylistItem item = new PlaylistItem(ytPlaylist.Snippet.Title, ytPlaylist.Id, ytPlaylist, (int)ytPlaylist.ContentDetails.ItemCount)
                                    {
                                        Owner = ytPlaylist.Snippet.ChannelTitle,
                                        ImageURL = ytPlaylist.Snippet.Thumbnails.High.Url,
                                        HasWritePermission = false
                                    };
                                    YoutubePlaylists.Add(item);
                                }
                            }
                        }
                        catch (System.Net.Http.HttpRequestException)
                        {
                            MainActivity.instance.Timout();
                        }
                        catch (Google.GoogleApiException)
                        {
                            Snackbar snackBar = Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), "No playlist exist with this id or link.", Snackbar.LengthLong);
                            snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                            snackBar.Show();
                        }

                        if (YoutubePlaylists.Count == 3 && YoutubePlaylists[1].Name == "EMPTY")
                        {
                            YoutubePlaylists.RemoveAt(1);
                            adapter.NotifyItemChanged(LocalPlaylists.Count + YoutubePlaylists.Count - 1);
                        }
                        else
                            adapter.NotifyItemInserted(LocalPlaylists.Count + YoutubePlaylists.Count);
                    })
                    .Show();
                return;
            }

            bool local = Position <= LocalPlaylists.Count;
            PlaylistItem playlist = local ?
                LocalPlaylists[Position] :
                YoutubePlaylists[Position - LocalPlaylists.Count];

            if(playlist.SyncState == SyncState.Error)
            {
                //Handle sync errors
                return;
            }

            AppCompatActivity act = (AppCompatActivity)Activity;
            act.SupportActionBar.SetHomeButtonEnabled(true);
            act.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            act.SupportActionBar.Title = playlist.Name;
            instance = null;
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;

            if (local)
                MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, PlaylistTracks.NewInstance(playlist.LocalID, playlist.Name)).AddToBackStack(null).Commit();
            else if(playlist.SyncState == SyncState.True)
                MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, PlaylistTracks.NewInstance(playlist.YoutubeID, playlist.LocalID, playlist.Name, playlist.HasWritePermission, true, playlist.Owner, playlist.Count, playlist.ImageURL)).AddToBackStack(null).Commit();
            else
                MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, PlaylistTracks.NewInstance(playlist.YoutubeID, playlist.Name, playlist.HasWritePermission, true, playlist.Owner, playlist.Count, playlist.ImageURL)).AddToBackStack(null).Commit();
        }

        private void ListView_ItemLongClick(object sender, int position)
        {
            More(position);
        }

        public void More(int Position)
        {
            bool local = Position <= LocalPlaylists.Count;
            PlaylistItem playlist = local ?
                LocalPlaylists[Position] :
                YoutubePlaylists[Position - LocalPlaylists.Count];

            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Pick an action");
            if (local)
                builder.SetItems(new string[] { "Play in order", "Random play", "Add To Queue", "Rename", "Delete" }, (senderAlert, args) =>
                {
                    switch (args.Which)
                    {
                        case 0:
                            PlayInOrder(playlist.LocalID);
                            break;
                        case 1:
                            RandomPlay(playlist.LocalID, Activity);
                            break;
                        case 2:
                            AddToQueue(playlist.LocalID);
                            break;
                        case 3:
                            Rename(Position, playlist);
                            break;
                        case 4:
                            RemovePlaylist(Position, playlist.LocalID);
                            break;
                        default:
                            break;
                    }
                });
            else if(playlist.HasWritePermission)
                builder.SetItems(new string[] { "Play in order", "Random play", "Add To Queue", "Rename", "Delete", "Download" }, (senderAlert, args) =>
                {
                    switch (args.Which)
                    {
                        case 0:
                            PlayInOrder(playlist.YoutubeID);
                            break;
                        case 1:
                            YoutubeEngine.RandomPlay(playlist.YoutubeID);
                            break;
                        case 2:
                            AddToQueue(playlist.YoutubeID);
                            break;
                        case 3:
                            RenameYoutubePlaylist(Position, playlist.YoutubeID);
                            break;
                        case 4:
                            RemoveYoutubePlaylist(Position, playlist.YoutubeID);
                            break;
                        case 5:
                            YoutubeEngine.DownloadPlaylist(playlist.Name, playlist.YoutubeID);
                            break;
                        default:
                            break;
                    }
                });
            else
                builder.SetItems(new string[] { "Play in order", "Random play", "Add To Queue", "Unfork", "Download" }, (senderAlert, args) =>
                {
                    switch (args.Which)
                    {
                        case 0:
                            PlayInOrder(playlist.YoutubeID);
                            break;
                        case 1:
                            YoutubeEngine.RandomPlay(playlist.YoutubeID);
                            break;
                        case 2:
                            AddToQueue(playlist.YoutubeID);
                            break;
                        case 3:
                            Unfork(Position, playlist.YoutubeID);
                            break;
                        case 4:
                            YoutubeEngine.DownloadPlaylist(playlist.Name, playlist.YoutubeID);
                            break;
                        default:
                            break;
                    }
                });
            builder.Show();
        }

        public static async void PlayInOrder(long playlistID)
        {
            Android.Net.Uri musicUri = Playlists.Members.GetContentUri("external", playlistID);
            List<Song> songs = new List<Song>();
            CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

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

                    songs.Add(new Song(Title, Artist, Album, null, AlbumArt, id, path));
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();

                MusicPlayer.queue.Clear();
                Browse.act = MainActivity.instance;
                Browse.Play(songs[0], null);
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
        }

        public static async void PlayInOrder(string playlistID)
        {
            List<Song> songs = new List<Song>();

            if (!await MainActivity.instance.WaitForYoutube())
            {
                Toast.MakeText(Android.App.Application.Context, "Error while loading.\nCheck your internet connection and check if your logged in.", ToastLength.Long).Show();
                return;
            }


            try
            {
                string nextPageToken = "";
                while (nextPageToken != null)
                {
                    var ytPlaylistRequest = YoutubeEngine.youtubeService.PlaylistItems.List("snippet, contentDetails");
                    ytPlaylistRequest.PlaylistId = playlistID;
                    ytPlaylistRequest.MaxResults = 50;
                    ytPlaylistRequest.PageToken = nextPageToken;

                    var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                    foreach (var item in ytPlaylist.Items)
                    {
                        if (item.Snippet.Title != "[Deleted video]" && item.Snippet.Title != "Private video" && item.Snippet.Title != "Deleted video")
                        {
                            Song song = new Song(item.Snippet.Title, item.Snippet.ChannelTitle, item.Snippet.Thumbnails.Default__.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true, false);
                            songs.Add(song);
                        }
                    }

                    nextPageToken = ytPlaylist.NextPageToken;
                }

                if (MusicPlayer.isRunning)
                    MusicPlayer.queue?.Clear();

                YoutubeEngine.Play(songs[0].youtubeID, songs[0].Title, songs[0].Artist, songs[0].Album);
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
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
            }
        }

        public static void RandomPlay(long playlistID, Context context)
        {
            List<string> tracksPath = new List<string>();
            Android.Net.Uri musicUri = Playlists.Members.GetContentUri("external", playlistID);

            CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

            if (musicCursor != null && musicCursor.MoveToFirst())
            {
                int pathID = musicCursor.GetColumnIndex(Media.InterfaceConsts.Data);
                do
                {
                    tracksPath.Add(musicCursor.GetString(pathID));
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }

            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.PutStringArrayListExtra("files", tracksPath);
            intent.SetAction("RandomPlay");
            context.StartService(intent);

            MainActivity.instance.ShowSmallPlayer();
            MainActivity.instance.ShowPlayer();
        }

        public static void AddToQueue(long playlistID)
        {
            if (MusicPlayer.instance == null || MusicPlayer.queue == null || MusicPlayer.queue?.Count == 0)
            {
                PlayInOrder(playlistID);
                return;
            }

            List<string> tracksPath = new List<string>();
            Android.Net.Uri musicUri = Playlists.Members.GetContentUri("external", playlistID);

            CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

            if (musicCursor != null && musicCursor.MoveToFirst())
            {
                int pathID = musicCursor.GetColumnIndex(Media.InterfaceConsts.Data);
                do
                {
                    tracksPath.Add(musicCursor.GetString(pathID));
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }

            tracksPath.Reverse();

            foreach(string path in tracksPath)
                MusicPlayer.instance.AddToQueue(path);

            Player.instance?.UpdateNext();
        }

        public static async void AddToQueue(string playlistID)
        {
            if (MusicPlayer.instance == null || MusicPlayer.queue == null || MusicPlayer.queue?.Count == 0)
            {
                PlayInOrder(playlistID);
                return;
            }

            List<Song> songs = new List<Song>();

            if (!await MainActivity.instance.WaitForYoutube())
            {
                Toast.MakeText(Android.App.Application.Context, "Error while loading.\nCheck your internet connection and check if your logged in.", ToastLength.Long).Show();
                return;
            }


            try
            {
                string nextPageToken = "";
                while (nextPageToken != null)
                {
                    var ytPlaylistRequest = YoutubeEngine.youtubeService.PlaylistItems.List("snippet, contentDetails");
                    ytPlaylistRequest.PlaylistId = playlistID;
                    ytPlaylistRequest.MaxResults = 50;
                    ytPlaylistRequest.PageToken = nextPageToken;

                    var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                    foreach (var item in ytPlaylist.Items)
                    {
                        if (item.Snippet.Title != "[Deleted video]" && item.Snippet.Title != "Private video" && item.Snippet.Title != "Deleted video")
                        {
                            Song song = new Song(item.Snippet.Title, item.Snippet.ChannelTitle, item.Snippet.Thumbnails.Default__.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true, false);
                            songs.Add(song);
                        }
                    }

                    nextPageToken = ytPlaylist.NextPageToken;
                }

                songs.Reverse();

                foreach (Song song in songs)
                    MusicPlayer.instance.AddToQueue(song);

                Player.instance?.UpdateNext();
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
            }
        }

        void Rename(int position, PlaylistItem playlist)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Playlist name");
            View view = LayoutInflater.Inflate(Resource.Layout.CreatePlaylistDialog, null);
            builder.SetView(view);
            builder.SetNegativeButton("Cancel", (senderAlert, args) => { });
            builder.SetPositiveButton("Rename", (senderAlert, args) =>
            {
                playlist.Name = view.FindViewById<EditText>(Resource.Id.playlistName).Text;
                RenamePlaylist(position, playlist);
            });
            builder.Show();
        }

        void RenamePlaylist(int position, PlaylistItem playlist)
        {
            ContentResolver resolver = Activity.ContentResolver;
            Android.Net.Uri uri = Playlists.ExternalContentUri;
            ContentValues value = new ContentValues();
            value.Put(Playlists.InterfaceConsts.Name, playlist.Name);
            resolver.Update(Playlists.ExternalContentUri, value, Playlists.InterfaceConsts.Id + "=?", new string[] { playlist.LocalID.ToString() });
            LocalPlaylists[position].Name = playlist.Name;

            adapter.UpdateElement(position, playlist);
        }

        void RemovePlaylist(int position, long playlistID)
        {
            AlertDialog dialog = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme)
                .SetTitle("Do you want to delete the playlist \"" + LocalPlaylists[position].Name + "\"?")
                .SetPositiveButton("Yes", (sender, e) =>
                {
                    ContentResolver resolver = Activity.ContentResolver;
                    Android.Net.Uri uri = Playlists.ExternalContentUri;
                    resolver.Delete(Playlists.ExternalContentUri, Playlists.InterfaceConsts.Id + "=?", new string[] { playlistID.ToString() });
                    adapter.Remove(position);
                    //LocalPlaylists.RemoveAt(position);

                    if (LocalPlaylists.Count == 1)
                    {
                        LocalPlaylists.Add(new PlaylistItem("EMPTY - You don't have any playlist on your device.", -1));
                        adapter.NotifyItemInserted(2);
                    }
                })
                .SetNegativeButton("No", (sender, e) => { })
                .Create();
            dialog.Show();
        }

        public void RenameYoutubePlaylist(int position, string playlistID)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Playlist name");
            View view = LayoutInflater.Inflate(Resource.Layout.CreatePlaylistDialog, null);
            builder.SetView(view);
            builder.SetNegativeButton("Cancel", (senderAlert, args) => { });
            builder.SetPositiveButton("Rename", (senderAlert, args) =>
            {
                RenameYT(position, view.FindViewById<EditText>(Resource.Id.playlistName).Text, playlistID);
            });
            builder.Show();
        }

        void RenameYT(int position, string name, string playlistID)
        {
            try
            {
                YoutubePlaylists[position - LocalPlaylists.Count].Snippet.Snippet.Title = name;
                YoutubePlaylists[position - LocalPlaylists.Count].Snippet.Id = playlistID;

                YoutubeEngine.youtubeService.Playlists.Update(YoutubePlaylists[position - LocalPlaylists.Count].Snippet, "snippet").Execute();
                adapter.UpdateElement(position, YoutubePlaylists[position - LocalPlaylists.Count - 1]);
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
            }
        }

        void RemoveYoutubePlaylist(int position, string playlistID)
        {
            AlertDialog dialog = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme)
                .SetTitle("Do you want to delete the playlist \"" + YoutubePlaylists[position].Name + "\"?")
                .SetPositiveButton("Yes", async (sender, e) =>
                {
                    try
                    {
                        PlaylistsResource.DeleteRequest deleteRequest = YoutubeEngine.youtubeService.Playlists.Delete(playlistID);
                        await deleteRequest.ExecuteAsync();

                        adapter.Remove(position);
                        YoutubePlaylists.RemoveAt(position - LocalPlaylists.Count - 1);

                        if (YoutubePlaylists.Count == 1)
                        {
                            YoutubePlaylists.Add(new PlaylistItem("EMPTY", "You don't have any youtube playlist on your account. \nWarning: Only playlist from your google account are displayed", null));
                        }
                    }
                    catch (System.Net.Http.HttpRequestException)
                    {
                        MainActivity.instance.Timout();
                    }
                })
                .SetNegativeButton("No", (sender, e) => {  })
                .Create();
            dialog.Show();
        }

        void Unfork(int position, string playlistID)
        {
            AlertDialog dialog = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme)
                .SetTitle("Do you want to remove  \"" + YoutubePlaylists[position].Name + "\" from your playlists?")
                .SetPositiveButton("Yes", async (sender, e) =>
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
                                if (section.ContentDetails.Playlists.Count > 1)
                                {
                                    section.ContentDetails.Playlists.Remove(playlistID);
                                    ChannelSectionsResource.UpdateRequest request = YoutubeEngine.youtubeService.ChannelSections.Update(section, "snippet,contentDetails");
                                    ChannelSection response = await request.ExecuteAsync();
                                }
                                else
                                {
                                    ChannelSectionsResource.DeleteRequest delete = YoutubeEngine.youtubeService.ChannelSections.Delete(section.Id);
                                    await delete.ExecuteAsync();
                                }
                            }
                        }

                        YoutubePlaylists.RemoveAt(position - LocalPlaylists.Count - 1);
                        adapter.Remove(position);

                        if (YoutubePlaylists.Count == 1)
                        {
                            YoutubePlaylists.Add(new PlaylistItem("EMPTY", "You don't have any youtube playlist on your account. \nWarning: Only playlist from your google account are displayed", null));
                        }
                    }
                    catch (System.Net.Http.HttpRequestException)
                    {
                        MainActivity.instance.Timout();
                    }
                })
                .SetNegativeButton("No", (sender, e) => { })
                .Create();
            dialog.Show();
        }

        public override void OnResume()
        {
            base.OnResume();
            instance = this;
            if (MainActivity.parcelable != null)
            {
                ListView.GetLayoutManager().OnRestoreInstanceState(MainActivity.parcelable);
                MainActivity.parcelable = null;
                MainActivity.parcelableSender = null;
            }
        }
    }
}