using Android.Content;
using Android.Content.Res;
using Android.Database;
using Android.Graphics;
using Android.OS;
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
using Square.Picasso;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Android.Provider.MediaStore.Audio;
using CursorLoader = Android.Support.V4.Content.CursorLoader;

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
                    LocalPlaylists.Add(new PlaylistItem("EMPTY", -1) { Owner = Resources.GetString(Resource.String.local_playlist_empty) });

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
                    adapter.NotifyItemRemoved(LocalPlaylists.Count + YoutubePlaylists.Count);

                    for (int i = 1; i < YoutubePlaylists.Count; i++)
                    {
                        YoutubePlaylists[i].SyncState = SyncState.Error;
                        PlaylistHolder holder = (PlaylistHolder)ListView.GetChildViewHolder(ListView.GetChildAt(LocalPlaylists.Count + i));
                        holder.sync.SetImageResource(Resource.Drawable.SyncError);
                        holder.sync.Visibility = ViewStates.Visible;
                        holder.SyncLoading.Visibility = ViewStates.Gone;
                        if (MainActivity.Theme == 1)
                            holder.sync.SetColorFilter(Color.White);
                    }

                    YoutubePlaylists.Add(new PlaylistItem("Error", null));
                    adapter.NotifyItemInserted(LocalPlaylists.Count + YoutubePlaylists.Count);
                    populating = false;
                    return;
                }
                int YtCount = YoutubePlaylists.Count;

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

                        PlaylistItem syncedItem = SyncedPlaylists.Find(x => x.YoutubeID == item.YoutubeID);
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
                            YoutubePlaylists[position].SyncState = SyncState.True;

                            PlaylistHolder holder = (PlaylistHolder)ListView.GetChildViewHolder(ListView.GetChildAt(LocalPlaylists.Count + position));
                            holder.Owner.Text = item.Owner;
                            Picasso.With(Android.App.Application.Context).Load(item.ImageURL).Placeholder(Resource.Color.background_material_dark).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);
                            holder.edit.Visibility = ViewStates.Visible;
                            if (MainActivity.Theme == 1)
                                holder.edit.SetColorFilter(Color.White);
                            holder.sync.SetImageResource(Resource.Drawable.Sync);
                            holder.sync.Visibility = ViewStates.Visible;
                            holder.SyncLoading.Visibility = ViewStates.Gone;
                            if (MainActivity.Theme == 1)
                                holder.sync.SetColorFilter(Color.White);

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

                    if(YtCount != YoutubePlaylists.Count)
                    {
                        YoutubePlaylists.Remove(Loading);
                        YoutubePlaylists.Add(Loading);
                        adapter.NotifyItemMoved(LocalPlaylists.Count + YoutubePlaylists.IndexOf(Loading), LocalPlaylists.Count + YoutubePlaylists.Count);
                    }
                    adapter.NotifyItemRangeInserted(LocalPlaylists.Count + YoutubePlaylists.Count + 1 - YtCount, YoutubePlaylists.Count - YtCount);
                    YtCount = YoutubePlaylists.Count;

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
                                    YoutubePlaylists[position].SyncState = SyncState.True;
                                    SyncedPlaylists.RemoveAll(x => x.YoutubeID == item.YoutubeID);
                                }
                                else if (SyncedPlaylists.Find(x => x.Name == item.Name) != null)
                                {
                                    item.LocalID = SyncedPlaylists.Find(x => x.Name == item.Name).LocalID;
                                    int position = YoutubePlaylists.FindIndex(x => x.Name == item.Name);
                                    YoutubePlaylists[position] = item;
                                    YoutubePlaylists[position].SyncState = SyncState.True;

                                    PlaylistHolder holder = (PlaylistHolder)ListView.GetChildViewHolder(ListView.GetChildAt(LocalPlaylists.Count + position));
                                    holder.Owner.Text = item.Owner;
                                    Picasso.With(Android.App.Application.Context).Load(item.ImageURL).Placeholder(Resource.Color.background_material_dark).Resize(400, 400).CenterCrop().Into(holder.AlbumArt);
                                    holder.edit.Visibility = ViewStates.Gone;
                                    holder.sync.SetImageResource(Resource.Drawable.Sync);
                                    holder.sync.Visibility = ViewStates.Visible;
                                    holder.SyncLoading.Visibility = ViewStates.Gone;
                                    if (MainActivity.Theme == 1)
                                        holder.sync.SetColorFilter(Color.White);

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
                    adapter.NotifyItemRemoved(LocalPlaylists.Count + YtCount);

                    if (YoutubePlaylists.Count == 1)
                    {
                        YoutubePlaylists.Add(new PlaylistItem("EMPTY", null) { Owner = Resources.GetString(Resource.String.youtube_playlist_empty) });
                    }
                    adapter.NotifyItemRangeInserted(LocalPlaylists.Count + YoutubePlaylists.Count + 1 - YtCount, YoutubePlaylists.Count - YtCount);
                    adapter.forkSaved = true;

                    if (SyncedPlaylists.Count > 0)
                    {
                        List<PlaylistItem> BadSync = SyncedPlaylists.FindAll(x => x.SyncState == SyncState.Loading);
                        LocalPlaylists.AddRange(BadSync);

                        for (int i = 0; i < SyncedPlaylists.Count; i++)
                            SyncedPlaylists[i].SyncState = SyncState.Error;

                        if(BadSync.Count > 0)
                        {
                            if (LocalPlaylists[1].Name == "EMPTY")
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
                    YoutubePlaylists.Remove(Loading);
                    adapter.NotifyItemRemoved(LocalPlaylists.Count + YoutubePlaylists.Count);

                    for (int i = 1; i < YoutubePlaylists.Count; i++)
                    {
                        YoutubePlaylists[i].SyncState = SyncState.Error;
                        PlaylistHolder holder = (PlaylistHolder)ListView.GetChildViewHolder(ListView.GetChildAt(LocalPlaylists.Count + i));
                        holder.sync.SetImageResource(Resource.Drawable.SyncError);
                        holder.sync.Visibility = ViewStates.Visible;
                        holder.SyncLoading.Visibility = ViewStates.Gone;
                        if (MainActivity.Theme == 1)
                            holder.sync.SetColorFilter(Color.White);
                    }

                    YoutubePlaylists.Add(new PlaylistItem("Error", null));
                    adapter.NotifyItemInserted(LocalPlaylists.Count + YoutubePlaylists.Count);
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

            if(playlist.SyncState == SyncState.Error && local)
            {
                //Handle sync errors
                /*Shouldn't do this but for now, will do this.*/MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, PlaylistTracks.NewInstance(playlist.LocalID, playlist.Name)).AddToBackStack(null).Commit();
                return;
            }

            AppCompatActivity act = (AppCompatActivity)Activity;
            act.SupportActionBar.SetHomeButtonEnabled(true);
            act.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            act.SupportActionBar.Title = playlist.Name;
            instance = null;
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;


            if (playlist.SyncState == SyncState.True || playlist.SyncState == SyncState.Loading)
                MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, PlaylistTracks.NewInstance(playlist.YoutubeID, playlist.LocalID, playlist.Name, playlist.HasWritePermission, true, playlist.Owner, playlist.Count, playlist.ImageURL)).AddToBackStack(null).Commit();
            else if (local || (playlist.SyncState == SyncState.Error && playlist.LocalID != 0 && playlist.LocalID != -1))
                MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, PlaylistTracks.NewInstance(playlist.LocalID, playlist.Name)).AddToBackStack(null).Commit();
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
            PlaylistItem item = local ?
                LocalPlaylists[Position] :
                YoutubePlaylists[Position - LocalPlaylists.Count];

            BottomSheetDialog bottomSheet = new BottomSheetDialog(MainActivity.instance);
            View bottomView = LayoutInflater.Inflate(Resource.Layout.BottomSheet, null);
            bottomView.FindViewById<TextView>(Resource.Id.bsTitle).Text = item.Name;
            if (!local || item.SyncState != SyncState.False)
            {
                bottomView.FindViewById<TextView>(Resource.Id.bsArtist).Text = item.Owner;
                Picasso.With(MainActivity.instance).Load(item.ImageURL).Placeholder(Resource.Color.background_material_dark).Transform(new RemoveBlackBorder(true)).Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
            }
            else
            {
                bottomView.FindViewById<TextView>(Resource.Id.bsArtist).Text = item.Count + " element" + (item.Count == 1 ? "" : "s");
                bottomView.FindViewById<ImageView>(Resource.Id.bsArt).Visibility = ViewStates.Gone;
            }
            bottomSheet.SetContentView(bottomView);

            List<BottomSheetAction> actions = new List<BottomSheetAction>
            {
                new BottomSheetAction(Resource.Drawable.Play, Resources.GetString(Resource.String.play_in_order), (sender, eventArg) =>
                {
                    if (local || item.SyncState == SyncState.True)
                        PlayInOrder(item.LocalID);
                    else
                        PlayInOrder(item.YoutubeID);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Shuffle, Resources.GetString(Resource.String.random_play), (sender, eventArg) =>
                {
                    if (local || item.SyncState == SyncState.True)
                        RandomPlay(item.LocalID, Activity);
                    else
                        YoutubeEngine.RandomPlay(item.YoutubeID);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Queue, Resources.GetString(Resource.String.add_to_queue), (sender, eventArg) =>
                {
                    if (local || item.SyncState == SyncState.True)
                        AddToQueue(item.LocalID);
                    else
                        AddToQueue(item.YoutubeID);
                    bottomSheet.Dismiss();
                })
            };

            if (local || item.HasWritePermission)
            {
                actions.AddRange(new BottomSheetAction[]{ new BottomSheetAction(Resource.Drawable.Edit, Resources.GetString(Resource.String.rename), (sender, eventArg) =>
                {
                    if (local)
                        Rename(Position, item);
                    else
                        RenameYoutubePlaylist(Position, item.YoutubeID, item.LocalID);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Delete, Resources.GetString(Resource.String.delete), (sender, eventArg) =>
                {
                    if (local)
                        RemovePlaylist(Position, item.LocalID);
                    else
                        RemoveYoutubePlaylist(Position, item.YoutubeID);
                    bottomSheet.Dismiss();
                })});
            }
            

            if(item.SyncState == SyncState.True)
            {
                actions.AddRange(new BottomSheetAction[]{ new BottomSheetAction(Resource.Drawable.Sync, Resources.GetString(Resource.String.sync_now), (sender, eventArg) =>
                {
                    YoutubeEngine.DownloadPlaylist(item.Name, item.YoutubeID);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.SyncDisabled, Resources.GetString(Resource.String.stop_sync), (sender, eventArg) =>
                {
                    StopSyncing(Position, item.LocalID);
                    bottomSheet.Dismiss();
                })});
            }
            else if (!local && item.HasWritePermission)
            {
                actions.Add(new BottomSheetAction(Resource.Drawable.Sync, Resources.GetString(Resource.String.sync), (sender, eventArg) => 
                {
                    YoutubeEngine.DownloadPlaylist(item.Name, item.YoutubeID);
                    bottomSheet.Dismiss();
                }));
            }
            else if(!local)
            {
                actions.AddRange(new BottomSheetAction[]{ new BottomSheetAction(Resource.Drawable.Sync, Resources.GetString(Resource.String.sync), (sender, eventArg) =>
                {
                    YoutubeEngine.DownloadPlaylist(item.Name, item.YoutubeID);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Delete, Resources.GetString(Resource.String.unfork), (sender, eventArg) =>
                {
                    Unfork(Position, item.YoutubeID);
                    bottomSheet.Dismiss();
                })});
            }

            bottomSheet.FindViewById<ListView>(Resource.Id.bsItems).Adapter = new BottomSheetAdapter(MainActivity.instance, Resource.Layout.BottomSheetText, actions);
            bottomSheet.Show();
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
                Browse.Play(songs[0]);
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

                YoutubeEngine.Play(songs[0].YoutubeID, songs[0].Title, songs[0].Artist, songs[0].Album);
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

        async void RemovePlaylist(int position, long playlistID)
        {
            if (await MainActivity.instance.GetWritePermission())
            {
                AlertDialog dialog = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme)
                    .SetTitle("Do you want to delete the playlist \"" + LocalPlaylists[position].Name + "\"?")
                    .SetPositiveButton("Yes", (sender, e) =>
                    {
                        ContentResolver resolver = Activity.ContentResolver;
                        Android.Net.Uri uri = Playlists.ExternalContentUri;
                        resolver.Delete(Playlists.ExternalContentUri, Playlists.InterfaceConsts.Id + "=?", new string[] { playlistID.ToString() });
                        LocalPlaylists.RemoveAt(position);
                        adapter.NotifyItemRemoved(position);

                        if (LocalPlaylists.Count == 1)
                        {
                            LocalPlaylists.Add(new PlaylistItem("EMPTY", -1) { Owner = Resources.GetString(Resource.String.local_playlist_empty) });
                            adapter.NotifyItemInserted(1);
                        }
                    })
                    .SetNegativeButton("No", (sender, e) => { })
                    .Create();
                dialog.Show();
            }
        }

        public async void StartSyncing(string playlistName)
        {
            int LocalIndex = LocalPlaylists.FindIndex(x => x.Name == playlistName);
            if(LocalIndex != -1)
            {
                LocalPlaylists.RemoveAt(LocalIndex);
                adapter.NotifyItemRemoved(LocalIndex);
                if (LocalPlaylists.Count == 1)
                {
                    LocalPlaylists.Add(new PlaylistItem("EMPTY", -1) { Owner = Resources.GetString(Resource.String.local_playlist_empty) });
                    adapter.NotifyItemInserted(1);
                }
                await Task.Delay(500);
            }

            int YoutubeIndex = YoutubePlaylists.FindIndex(x => x.Name == playlistName);
            YoutubePlaylists[YoutubeIndex].SyncState = SyncState.Loading;
            PlaylistHolder holder = (PlaylistHolder)ListView.GetChildViewHolder(ListView.GetChildAt(LocalPlaylists.Count + YoutubeIndex));
            holder.sync.Visibility = ViewStates.Gone;
            holder.SyncLoading.Visibility = ViewStates.Visible;
            if (MainActivity.Theme == 1)
                holder.SyncLoading.IndeterminateTintList = ColorStateList.ValueOf(Color.White);
        }

        public void CheckForSync()
        {
            for (int i = 1; i < YoutubePlaylists.Count; i++)
            {
                if (YoutubePlaylists[i].SyncState != SyncState.False && Downloader.queue.Find(x => x.playlist == YoutubePlaylists[i].Name && (x.State == DownloadState.Downloading || x.State == DownloadState.Initialization || x.State == DownloadState.MetaData || x.State == DownloadState.None)) == null)
                {
                    YoutubePlaylists[i].SyncState = SyncState.True;
                    PlaylistHolder holder = (PlaylistHolder)ListView.GetChildViewHolder(ListView.GetChildAt(LocalPlaylists.Count + i));
                    holder.SyncLoading.Visibility = ViewStates.Gone;
                    holder.sync.SetImageResource(Resource.Drawable.Sync);
                    holder.sync.Visibility = ViewStates.Visible;
                    if (MainActivity.Theme == 1)
                        holder.sync.SetColorFilter(Color.White);
                }
            }
        }
        public void SyncCanceled()
        {
            for (int i = 0; i < YoutubePlaylists.Count; i++)
            {
                if(YoutubePlaylists[i].SyncState == SyncState.Loading)
                {
                    YoutubePlaylists[i].SyncState = SyncState.True;
                    adapter.NotifyItemChanged(i);
                }
            }
        }

        async void StopSyncing(int position, long LocalID)
        {
            await Task.Run(() =>
            {
                SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "SyncedPlaylists.sqlite"));
                db.CreateTable<PlaylistItem>();

                db.Delete(db.Table<PlaylistItem>().ToList().Find(x => x.LocalID == LocalID));
            });
            YoutubePlaylists[position - LocalPlaylists.Count].LocalID = 0;
            YoutubePlaylists[position - LocalPlaylists.Count].SyncState = SyncState.False;
            PlaylistHolder holder = (PlaylistHolder)ListView.GetChildViewHolder(ListView.GetChildAt(position));
            holder.sync.Visibility = ViewStates.Gone;
            holder.SyncLoading.Visibility = ViewStates.Gone;

            PlaylistItem LocalPlaylist = new PlaylistItem(YoutubePlaylists[position - LocalPlaylists.Count].Name, LocalID, YoutubePlaylists[position - LocalPlaylists.Count].Count);
            if (LocalPlaylists.Count == 2 && LocalPlaylists[1].Name == "EMPTY")
            {
                LocalPlaylists.RemoveAt(1);
                adapter.NotifyItemRemoved(1);
            }

            LocalPlaylists.Add(LocalPlaylist);
            adapter.NotifyItemInserted(LocalPlaylists.Count);
        }

        public void RenameYoutubePlaylist(int position, string YoutubeID, long LocalID = -1)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Playlist name");
            View view = LayoutInflater.Inflate(Resource.Layout.CreatePlaylistDialog, null);
            builder.SetView(view);
            builder.SetNegativeButton("Cancel", (senderAlert, args) => { });
            builder.SetPositiveButton("Rename", (senderAlert, args) =>
            {
                RenameYT(position, view.FindViewById<EditText>(Resource.Id.playlistName).Text, YoutubeID, LocalID);
            });
            builder.Show();
        }

        void RenameYT(int position, string name, string YoutubeID, long LocalID = -1)
        {
            try
            {
                YoutubePlaylists[position - LocalPlaylists.Count].Snippet.Snippet.Title = name;
                YoutubePlaylists[position - LocalPlaylists.Count].Snippet.Id = YoutubeID;

                YoutubeEngine.youtubeService.Playlists.Update(YoutubePlaylists[position - LocalPlaylists.Count].Snippet, "snippet").Execute();
                adapter.UpdateElement(position, YoutubePlaylists[position - LocalPlaylists.Count - 1]);
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
                return;
            }

            if(LocalID != -1)
            {
                ContentResolver resolver = Activity.ContentResolver;
                Android.Net.Uri uri = Playlists.ExternalContentUri;
                ContentValues value = new ContentValues();
                value.Put(Playlists.InterfaceConsts.Name, name);
                resolver.Update(Playlists.ExternalContentUri, value, Playlists.InterfaceConsts.Id + "=?", new string[] { LocalID.ToString() });
            }
        }

        void RemoveYoutubePlaylist(int position, string playlistID)
        {
            AlertDialog dialog = new AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme)
                .SetTitle("Do you want to delete the playlist \"" + YoutubePlaylists[position - LocalPlaylists.Count].Name + "\"?")
                .SetPositiveButton("Yes", async (sender, e) =>
                {
                    try
                    {
                        PlaylistsResource.DeleteRequest deleteRequest = YoutubeEngine.youtubeService.Playlists.Delete(playlistID);
                        await deleteRequest.ExecuteAsync();

                        YoutubePlaylists.RemoveAt(position - LocalPlaylists.Count);
                        adapter.NotifyItemRemoved(position);

                        foreach (PlaylistItem item in YoutubePlaylists)
                            System.Console.WriteLine(item.Name);

                        if (YoutubePlaylists.Count == 1)
                        {
                            YoutubePlaylists.Add(new PlaylistItem("EMPTY", null) { Owner = Resources.GetString(Resource.String.youtube_playlist_empty) });
                            adapter.NotifyItemInserted(LocalPlaylists.Count + YoutubePlaylists.Count);
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
                        adapter.NotifyItemRemoved(position);

                        if (YoutubePlaylists.Count == 1)
                        {
                            YoutubePlaylists.Add(new PlaylistItem("EMPTY", null) { Owner = Resources.GetString(Resource.String.youtube_playlist_empty) });
                            adapter.NotifyItemInserted(LocalPlaylists.Count + YoutubePlaylists.Count);
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
        }
    }
}