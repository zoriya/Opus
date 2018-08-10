using Android.Content;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MusicApp.Resources.values;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Android.Provider.MediaStore.Audio;

namespace MusicApp.Resources.Portable_Class
{
    public class Playlist : Fragment
    {
        public static Playlist instance;
        public RecyclerView ListView;

        //Local playlists
        private List<string> playList = new List<string>();
        private List<int> playListCount = new List<int>();
        private List<long> playlistId = new List<long>();

        //Yt Playlists
        private List<Song> ytPlaylists = new List<Song>();
        private List<Google.Apis.YouTube.v3.Data.Playlist> YtPlaylists = new List<Google.Apis.YouTube.v3.Data.Playlist>();

        private PlaylistAdapter adapter;
        private View emptyView;

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
            emptyView = LayoutInflater.Inflate(Resource.Layout.NoPlaylist, null);
            MainActivity.instance.OnPaddingChanged += OnPaddingChanged;
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
            View view = inflater.Inflate(Resource.Layout.RecyclerFragment, container, false);
            view.SetPadding(0, 0, 0, MainActivity.defaultPaddingBot);
            ListView = view.FindViewById<RecyclerView>(Resource.Id.recycler);
            ListView.SetLayoutManager(new LinearLayoutManager(Android.App.Application.Context));

#pragma warning disable CS4014
            PopulateView();
            return view;
        }

        public async Task PopulateView()
        {
            //Local playlists
            playList.Clear();
            playlistId.Clear();

            playList.Add("Header");
            playlistId.Add(-1);
            playListCount.Add(-1);

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
                    playList.Add(name);
                    playlistId.Add(id);

                    Android.Net.Uri musicUri = Playlists.Members.GetContentUri("external", id);
                    CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
                    ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

                    playListCount.Add(musicCursor.Count);
                }
                while (cursor.MoveToNext());
                cursor.Close();
            }

            if(playList.Count == 1)
            {
                playList.Add("EMPTY - You don't have any playlist on your device.");
                playlistId.Add(-1);
                playListCount.Add(-1);
            }

            adapter = new PlaylistAdapter(playList, playListCount, new List<Song>())
            {
                listPadding = MainActivity.paddingBot - MainActivity.defaultPaddingBot
            };
            ListView.SetAdapter(adapter);
            adapter.ItemClick += ListView_ItemClick;
            adapter.ItemLongCLick += ListView_ItemLongClick;
            ListView.SetItemAnimator(new DefaultItemAnimator());
            ListView.ScrollChange += MainActivity.instance.Scroll;

            //Youtube playlists
            ytPlaylists = new List<Song>
            {
                new Song("Header", null, null, null, -1, -1, null),
                new Song("Loading", null, null, null, -1, -1, null)
            };
            adapter.SetYtPlaylists(ytPlaylists, false);

            if (!await MainActivity.instance.WaitForYoutube())
            {
                ytPlaylists[1] = new Song("Error", null, null, null, -1, -1, null);
                adapter.SetYtPlaylists(ytPlaylists, false);
                return;
            }

            YouTubeService youtube = YoutubeEngine.youtubeService;

            if (instance == null)
                return;

            PlaylistsResource.ListRequest request = youtube.Playlists.List("snippet,contentDetails");
            request.Mine = true;
            request.MaxResults = 25;
            PlaylistListResponse response = await request.ExecuteAsync();

            if (instance == null)
                return;

            for (int i = 0; i < response.Items.Count; i++)
            {
                Google.Apis.YouTube.v3.Data.Playlist playlist = response.Items[i];
                YtPlaylists.Add(playlist);
                Song song = new Song(playlist.Snippet.Title, playlist.Snippet.ChannelTitle, playlist.Snippet.Thumbnails.High.Url, playlist.Id, -1, -1, playlist.Id, true, true, (int)playlist.ContentDetails.ItemCount);
                ytPlaylists.Add(song);
            }

            ytPlaylists.RemoveAt(1);
            Song loading = new Song("Loading", null, null, null, -1, -1, null);
            ytPlaylists.Add(loading);

            adapter.SetYtPlaylists(ytPlaylists, false);

            //Saved playlists
            ChannelSectionsResource.ListRequest forkedRequest = youtube.ChannelSections.List("snippet,contentDetails");
            forkedRequest.Mine = true;
            ChannelSectionListResponse forkedResponse = await forkedRequest.ExecuteAsync();
            if (instance == null)
                return;

            foreach (ChannelSection section in forkedResponse.Items)
            {
                if(section.Snippet.Title == "Saved Playlists")
                {
                    for (int i = 0; i < section.ContentDetails.Playlists.Count; i++)
                    {
                        PlaylistsResource.ListRequest plRequest = youtube.Playlists.List("snippet, contentDetails");
                        plRequest.Id = section.ContentDetails.Playlists[i];

                        if (instance == null)
                            return;

                        PlaylistListResponse plResponse = await plRequest.ExecuteAsync();

                        if (instance == null)
                            return;

                        Google.Apis.YouTube.v3.Data.Playlist playlist = plResponse.Items[0];
                        playlist.Kind = "youtube#saved";
                        YtPlaylists.Add(playlist);
                        Song song = new Song(playlist.Snippet.Title, playlist.Snippet.ChannelTitle, playlist.Snippet.Thumbnails.High.Url, playlist.Id, -1, -1, playlist.Id, true, false, (int)playlist.ContentDetails.ItemCount);
                        ytPlaylists.Add(song);
                    }
                }
            }

            ytPlaylists.Remove(loading);

            if (ytPlaylists.Count == 1)
            {
                ytPlaylists.Add(new Song("EMPTY", "You don't have any youtube playlist on your account. \nWarning: Only playlist from your google account are displayed", null, null, -1, -1, null));
            }

            adapter.SetYtPlaylists(ytPlaylists, true);
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
            if(Position == playList.Count + ytPlaylists.Count)
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
                                YtPlaylists.Add(ytPlaylist);
                                Song song = new Song(ytPlaylist.Snippet.Title, ytPlaylist.Snippet.ChannelTitle, ytPlaylist.Snippet.Thumbnails.Default__.Url, ytPlaylist.Id, -1, -1, ytPlaylist.Id, true, false);
                                ytPlaylists.Add(song);
                            }
                        }
                        adapter.NotifyItemInserted(playList.Count + ytPlaylists.Count);
                    })
                    .Show();
                return;
            }

            bool local = Position <= playList.Count;
            Song playlist = local ?
                new Song(playList[Position], null, null, null, -1, playlistId[Position], null) :
                ytPlaylists[Position - playList.Count];

            AppCompatActivity act = (AppCompatActivity)Activity;
            act.SupportActionBar.SetHomeButtonEnabled(true);
            act.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            act.SupportActionBar.Title = playlist.GetName();
            instance = null;
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
            MainActivity.instance.OnPaddingChanged -= OnPaddingChanged;
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;

            if (local)
                MainActivity.instance.Transition(Resource.Id.contentView, PlaylistTracks.NewInstance(playlist.GetID(), playlist.GetName()), true);
            else
                MainActivity.instance.Transition(Resource.Id.contentView, PlaylistTracks.NewInstance(playlist.youtubeID, playlist.GetName(), playlist.isParsed, playlist.GetArtist(), playlist.queueSlot, playlist.GetAlbum()), true);
        }

        private void ListView_ItemLongClick(object sender, int position)
        {
            More(position);
        }

        public void More(int Position)
        {
            bool local = Position <= playList.Count;
            Song playlist = local ?
                new Song(playList[Position], null, playListCount[Position].ToString(), null, -1, playlistId[Position], null) :
                ytPlaylists[Position - playList.Count];

            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Pick an action");
            if (local)
                builder.SetItems(new string[] { "Play in order", "Random play", "Add To Queue", "Rename", "Delete" }, (senderAlert, args) =>
                {
                    switch (args.Which)
                    {
                        case 0:
                            PlayInOrder(playlist.GetID());
                            break;
                        case 1:
                            RandomPlay(playlist.GetID(), Activity);
                            break;
                        case 2:
                            AddToQueue(playlist.GetID());
                            break;
                        case 3:
                            Rename(Position, playlist);
                            break;
                        case 4:
                            RemovePlaylist(Position, playlist.GetID());
                            break;
                        default:
                            break;
                    }
                });
            else if(playlist.isParsed)
                builder.SetItems(new string[] { "Play in order", "Random play", "Add To Queue", "Rename", "Delete", "Download" }, (senderAlert, args) =>
                {
                    switch (args.Which)
                    {
                        case 0:
                            PlayInOrder(playlist.GetPath());
                            break;
                        case 1:
                            YoutubeEngine.RandomPlay(playlist.GetPath());
                            break;
                        case 2:
                            AddToQueue(playlist.GetPath());
                            break;
                        case 3:
                            RenameYoutubePlaylist(Position, playlist.GetPath());
                            break;
                        case 4:
                            RemoveYoutubePlaylist(Position, playlist.GetPath());
                            break;
                        case 5:
                            YoutubeEngine.DownloadPlaylist(playlist.GetName(), playlist.GetPath());
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
                            PlayInOrder(playlist.GetPath());
                            break;
                        case 1:
                            YoutubeEngine.RandomPlay(playlist.GetPath());
                            break;
                        case 2:
                            AddToQueue(playlist.GetPath());
                            break;
                        case 3:
                            Unfork(Position, playlist.GetPath());
                            break;
                        case 4:
                            YoutubeEngine.DownloadPlaylist(playlist.GetName(), playlist.GetPath());
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

            if(!await MainActivity.instance.WaitForYoutube())
            {
                Toast.MakeText(Android.App.Application.Context, "Error while loading.\nCheck your internet connection and check if your logged in.", ToastLength.Long).Show();
                return;
            }


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
                    Song song = new Song(item.Snippet.Title, item.Snippet.ChannelTitle, item.Snippet.Thumbnails.Default__.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true, false);
                    songs.Add(song);
                }

                nextPageToken = ytPlaylist.NextPageToken;
            }

            if (MusicPlayer.isRunning)
                MusicPlayer.queue.Clear();

            YoutubeEngine.Play(songs[0].youtubeID, songs[0].GetName(), songs[0].GetArtist(), songs[0].GetAlbum());
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

            Intent inte = new Intent(context, typeof(Player));
            context.StartActivity(inte);
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

            if(!await MainActivity.instance.WaitForYoutube())
            {
                Toast.MakeText(Android.App.Application.Context, "Error while loading.\nCheck your internet connection and check if your logged in.", ToastLength.Long).Show();
                return;
            }


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
                    Song song = new Song(item.Snippet.Title, item.Snippet.ChannelTitle, item.Snippet.Thumbnails.Default__.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true, false);
                    songs.Add(song);
                }

                nextPageToken = ytPlaylist.NextPageToken;
            }

            songs.Reverse();

            foreach (Song song in songs)
                MusicPlayer.instance.AddToQueue(song);

            Player.instance?.UpdateNext();
        }

        void Rename(int position, Song playlist)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Playlist name");
            View view = LayoutInflater.Inflate(Resource.Layout.CreatePlaylistDialog, null);
            builder.SetView(view);
            builder.SetNegativeButton("Cancel", (senderAlert, args) => { });
            builder.SetPositiveButton("Rename", (senderAlert, args) =>
            {
                playlist.SetName(view.FindViewById<EditText>(Resource.Id.playlistName).Text);
                RenamePlaylist(position, playlist);
            });
            builder.Show();
        }

        void RenamePlaylist(int position, Song playlist)
        {
            ContentResolver resolver = Activity.ContentResolver;
            Android.Net.Uri uri = Playlists.ExternalContentUri;
            ContentValues value = new ContentValues();
            value.Put(Playlists.InterfaceConsts.Name, playlist.GetName());
            resolver.Update(Playlists.ExternalContentUri, value, Playlists.InterfaceConsts.Id + "=?", new string[] { playlist.GetID().ToString() });
            playList[position] = playlist.GetName();

            adapter.UpdateElement(position, playlist);
        }

        void RemovePlaylist(int position, long playlistID)
        {
            ContentResolver resolver = Activity.ContentResolver;
            Android.Net.Uri uri = Playlists.ExternalContentUri;
            resolver.Delete(Playlists.ExternalContentUri, Playlists.InterfaceConsts.Id + "=?", new string[] { playlistID.ToString() });
            adapter.Remove(position);
            playList.RemoveAt(position);
            playListCount.RemoveAt(position);
            playlistId.RemoveAt(position);

            if (playList.Count == 1)
            {
                playList.Add("EMPTY - You don't have any playlist on your device.");
                playlistId.Add(-1);
                playListCount.Add(-1);
            }
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
            Google.Apis.YouTube.v3.Data.Playlist playlist = new Google.Apis.YouTube.v3.Data.Playlist
            {
                Snippet = YtPlaylists[position - playList.Count].Snippet
            };
            playlist.Snippet.Title = name;
            playlist.Id = playlistID;

            YtPlaylists[position - playList.Count].Snippet.Title = name;
            YoutubeEngine.youtubeService.Playlists.Update(playlist, "snippet").Execute();

            ytPlaylists[position - playList.Count - 1].SetName(name);
            adapter.UpdateElement(position, ytPlaylists[position - playList.Count - 1]);
        }

        void RemoveYoutubePlaylist(int position, string playlistID)
        {
            PlaylistsResource.DeleteRequest deleteRequest = YoutubeEngine.youtubeService.Playlists.Delete(playlistID);
            deleteRequest.Execute();

            adapter.Remove(position);
            YtPlaylists.RemoveAt(position - playList.Count - 1);

            if (ytPlaylists.Count == 1)
            {
                ytPlaylists.Add(new Song("EMPTY", "You don't have any youtube playlist on your account. \nWarning: Only playlist from your google account are displayed", null, null, -1, -1, null));
            }
        }

        async void Unfork(int position, string playlistID)
        {
            ChannelSectionsResource.ListRequest forkedRequest = YoutubeEngine.youtubeService.ChannelSections.List("snippet,contentDetails");
            forkedRequest.Mine = true;
            ChannelSectionListResponse forkedResponse = await forkedRequest.ExecuteAsync();

            foreach (ChannelSection section in forkedResponse.Items)
            {
                if (section.Snippet.Title == "Saved Playlists")
                {
                    section.ContentDetails.Playlists.Remove(playlistID);
                    ChannelSectionsResource.UpdateRequest request = YoutubeEngine.youtubeService.ChannelSections.Update(section, "snippet,contentDetails");
                    ChannelSection response = await request.ExecuteAsync();
                }
            }

            YtPlaylists.RemoveAt(position - playList.Count - 1);
            adapter.Remove(position);

            if (ytPlaylists.Count == 1)
            {
                ytPlaylists.Add(new Song("EMPTY", "You don't have any youtube playlist on your account. \nWarning: Only playlist from your google account are displayed", null, null, -1, -1, null));
            }
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