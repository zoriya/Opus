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

namespace MusicApp.Resources.Portable_Class
{
    public class PlaylistTracks : ListFragment, PopupMenu.IOnMenuItemClickListener, AppBarLayout.IOnOffsetChangedListener
    {
        public static PlaylistTracks instance;
        public string playlistName;
        public Adapter adapter;
        public View emptyView;
        public List<Song> result;
        public long playlistId = 0;
        public string ytID = "";
        private string author;
        private int count;
        private Uri thumnailURI;
        private bool hasWriteAcess;
        private string nextPageToken = null;
        public bool isEmpty = false;
        public bool lastVisible = false;

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
            ListView.ScrollStateChanged += ListView_ScrollStateChanged;
            ListView.NestedScrollingEnabled = true;
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
            MainActivity.instance.OnPaddingChanged += OnPaddingChanged;
            MainActivity.instance.DisplaySearch(1);

#pragma warning disable CS4014
            PopulateList();


            Activity.FindViewById<RelativeLayout>(Resource.Id.playlistHeader).Visibility = ViewStates.Visible;
            ((AppBarLayout.LayoutParams)Activity.FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsingToolbar).LayoutParameters).ScrollFlags = AppBarLayout.LayoutParams.ScrollFlagScroll | AppBarLayout.LayoutParams.ScrollFlagExitUntilCollapsed | AppBarLayout.LayoutParams.ScrollFlagSnap;
            Activity.FindViewById<AppBarLayout>(Resource.Id.appbar).AddOnOffsetChangedListener(this);
            Activity.FindViewById<TextView>(Resource.Id.headerTitle).Text = playlistName;
            Activity.FindViewById<ImageButton>(Resource.Id.headerPlay).Click += (sender, e0) => { PlayInOrder(0); };
            Activity.FindViewById<ImageButton>(Resource.Id.headerShuffle).Click += (sender, e0) => 
            {
                if (playlistId != 0)
                    Playlist.RandomPlay(playlistId, Activity);
                else
                    YoutubeEngine.RandomPlay(ytID);
            };
            Activity.FindViewById<ImageButton>(Resource.Id.headerMore).Click += (sender, e0) => 
            {
                PopupMenu menu = new PopupMenu(Activity, Activity.FindViewById<ImageButton>(Resource.Id.headerMore));
                if (playlistId == 0 && hasWriteAcess)
                    menu.Inflate(Resource.Menu.ytplaylist_header_more);
                else if (playlistId == 0)
                    menu.Inflate(Resource.Menu.ytplaylistnowrite_header_more);
                else
                    menu.Inflate(Resource.Menu.playlist_header_more);
                menu.SetOnMenuItemClickListener(this);
                menu.Show();
            };


            if (playlistId != 0)
            {
                Activity.FindViewById<TextView>(Resource.Id.headerAuthor).Text = MainActivity.account == null ? "by me" : "by " + MainActivity.account.DisplayName;
            }
            else if(ytID != null && ytID != "")
            {
                Activity.FindViewById<TextView>(Resource.Id.headerAuthor).Text = author;
                Activity.FindViewById<TextView>(Resource.Id.headerNumber).Text = count.ToString() + " songs";
                if(count == -1)
                    Activity.FindViewById<TextView>(Resource.Id.headerNumber).Text = "NaN songs";

                Picasso.With(Android.App.Application.Context).Load(thumnailURI).Placeholder(Resource.Drawable.noAlbum).Resize(1080, 1080).CenterCrop().Into(Activity.FindViewById<ImageView>(Resource.Id.headerArt));
            }
        }

        public bool OnMenuItemClick(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.download:
                    YoutubeEngine.DownloadPlaylist(playlistName, ytID);
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

        async void Rename(string newName)
        {
            if(playlistId == 0)
            {
                Google.Apis.YouTube.v3.Data.Playlist playlist = new Google.Apis.YouTube.v3.Data.Playlist
                {
                    Snippet = new PlaylistSnippet()
                };
                playlist.Snippet.Title = newName;
                playlist.Id = ytID;

                await YoutubeEngine.youtubeService.Playlists.Update(playlist, "snippet").ExecuteAsync();
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

        async void Delete()
        {
            if(playlistId == 0)
            {
                if (hasWriteAcess)
                {
                    PlaylistsResource.DeleteRequest deleteRequest = YoutubeEngine.youtubeService.Playlists.Delete(ytID);
                    await deleteRequest.ExecuteAsync();
                }
                else
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
            }
            else
            {
                ContentResolver resolver = Activity.ContentResolver;
                Uri uri = Playlists.ExternalContentUri;
                resolver.Delete(Playlists.ExternalContentUri, Playlists.InterfaceConsts.Id + "=?", new string[] { playlistId.ToString() });
            }
            MainActivity.instance.SupportFragmentManager.PopBackStack();
        }

        private void ListView_ScrollStateChanged(object sender, AbsListView.ScrollStateChangedEventArgs e)
        {
            if (lastVisible && e.ScrollState == ScrollState.Idle)
            {
                lastVisible = false;
                LoadMore();
            }
        }

        public void OnPaddingChanged(object sender, PaddingChange e)
        {
            if (MainActivity.paddingBot > e.oldPadding)
                adapter.listPadding = MainActivity.paddingBot - MainActivity.defaultPaddingBot;
            else
                adapter.listPadding = (int)(8 * MainActivity.instance.Resources.DisplayMetrics.Density + 0.5f);
        }

        public override void OnDestroy()
        {
            MainActivity.instance.HideSearch();
            if (isEmpty)
            {
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(emptyView);
            }
            MainActivity.instance.SupportActionBar.SetHomeButtonEnabled(false);
            MainActivity.instance.SupportActionBar.SetDisplayHomeAsUpEnabled(false);
            MainActivity.instance.SupportActionBar.SetDisplayShowTitleEnabled(true);
            MainActivity.instance.SupportActionBar.Title = "MusicApp";

            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
            MainActivity.instance.OnPaddingChanged -= OnPaddingChanged;
            Activity.FindViewById<AppBarLayout>(Resource.Id.appbar).RemoveOnOffsetChangedListener(this);
            Activity.FindViewById<RelativeLayout>(Resource.Id.playlistHeader).Visibility = ViewStates.Gone;

            MainActivity.instance.SupportFragmentManager.PopBackStack();

            //if (MainActivity.instance.HomeDetails)
            //{
            //    Home.instance = null;
            //    MainActivity.instance.Navigate(Resource.Id.musicLayout);
            //    MainActivity.instance.HomeDetails = false;
            //}
            //else if (MainActivity.youtubeInstanceSave != null)
            //{
            //    int selectedTab = 0;
            //    switch (MainActivity.youtubeInstanceSave)
            //    {
            //        case "YoutubeEngine-All":
            //            selectedTab = 0;
            //            break;
            //        case "YoutubeEngine-Tracks":
            //            selectedTab = 1;
            //            break;
            //        case "YoutubeEngine-Playlists":
            //            selectedTab = 2;
            //            break;
            //        case "YoutubeEngine-Channels":
            //            selectedTab = 3;
            //            break;
            //        default:
            //            break;
            //    }
            //    MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, Pager.NewInstance(2, selectedTab)).Commit();
            //    YoutubeEngine.instances[selectedTab].focused = true;
            //    YoutubeEngine.instances[selectedTab].OnFocus();
            //    YoutubeEngine.instances[selectedTab].ResumeListView();
            //}
            //else
            //{
            //    MainActivity.instance.Navigate(Resource.Id.playlistLayout);
            //}

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

        public static Fragment NewInstance(string ytID, string playlistName, bool hasWriteAcess, string author, int count, string thumbnailURI)
        {
            instance = new PlaylistTracks { Arguments = new Bundle() };
            instance.ytID = ytID;
            instance.hasWriteAcess = hasWriteAcess;
            instance.playlistName = playlistName;
            instance.author = author;
            instance.count = count;
            instance.thumnailURI = Uri.Parse(thumbnailURI);
            return instance;
        }

        async Task PopulateList()
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

                Activity.FindViewById<TextView>(Resource.Id.headerNumber).Text = tracks.Count.ToString() + " songs";
                var songCover = Uri.Parse("content://media/external/audio/albumart");
                var songAlbumArtUri = ContentUris.WithAppendedId(songCover, tracks[0].GetAlbumArt());

                Picasso.With(Android.App.Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.noAlbum).Resize(1080, 1080).CenterCrop().Into(Activity.FindViewById<ImageView>(Resource.Id.headerArt));
            }
            else if (ytID != null)
            {
                tracks = new List<Song>();
                var ytPlaylistRequest = YoutubeEngine.youtubeService.PlaylistItems.List("snippet, contentDetails");
                ytPlaylistRequest.PlaylistId = ytID;
                ytPlaylistRequest.MaxResults = 50;

                var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                foreach (var item in ytPlaylist.Items)
                {
                    Song song = new Song(item.Snippet.Title, item.Snippet.ChannelTitle, item.Snippet.Thumbnails.Default__.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true, false);
                    tracks.Add(song);
                    ytTracksIDs.Add(item.Id);
                }

                nextPageToken = ytPlaylist.NextPageToken;
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

        public async void OnRefresh(object sender, System.EventArgs e)
        {
            await PopulateList();
            MainActivity.instance.contentRefresh.Refreshing = false;
        }

        public async void LoadMore()
        {
            if (nextPageToken == null)
                return;

            var ytPlaylistRequest = YoutubeEngine.youtubeService.PlaylistItems.List("snippet, contentDetails");
            ytPlaylistRequest.PlaylistId = ytID;
            ytPlaylistRequest.MaxResults = 50;
            ytPlaylistRequest.PageToken = nextPageToken;

            var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

            if (instance == null)
                return;

            foreach (var item in ytPlaylist.Items)
            {
                Song song = new Song(item.Snippet.Title, item.Snippet.ChannelTitle, item.Snippet.Thumbnails.Default__.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true, false);
                tracks.Add(song);
                ytTracksIDs.Add(item.Id);
            }

            nextPageToken = ytPlaylist.NextPageToken;
            adapter.NotifyDataSetChanged();
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
            PlayInOrder(e.Position);
        }

        private void ListView_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            Song item = tracks[e.Position];
            if (result != null && result.Count > e.Position)
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
            if (hasWriteAcess && ytID != "")
            {
                builder.SetItems(action.ToArray(), (senderAlert, args) =>
                {
                    switch (args.Which)
                    {
                        case 0:
                            int Position = tracks.IndexOf(item);
                            PlayInOrder(Position);
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
            }
            else
            {
                action.Remove("Remove Track from playlist");
                builder.SetItems(action.ToArray(), (senderAlert, args) =>
                {
                    switch (args.Which)
                    {
                        case 0:
                            int Position = tracks.IndexOf(item);
                            PlayInOrder(Position);
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
                            YoutubeEngine.GetPlaylists(item.GetPath(), Activity);
                            break;

                        case 4:
                            if (item.IsYt)
                                YoutubeEngine.Download(item.GetName(), item.GetPath());
                            else
                                Browse.EditMetadata(item, "PlaylistTracks", ListView.OnSaveInstanceState());
                            break;

                        default:
                            break;
                    }
                });
            }
            builder.Show();
        }

        async void PlayInOrder(int fromPosition)
        {
            List<Song> songs = tracks.GetRange(fromPosition, tracks.Count - fromPosition);
            if (result != null && result.Count - 1 >= fromPosition)
                songs = result.GetRange(fromPosition, result.Count - fromPosition);

            if (MusicPlayer.isRunning)
                MusicPlayer.queue.Clear();

            if (!songs[0].IsYt)
            {
                Browse.act = Activity;
                Browse.Play(songs[0]);
            }
            else
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
            instance = this;
            if (MainActivity.parcelable != null && MainActivity.parcelableSender == "PlaylistTrack")
            {
                ListView.OnRestoreInstanceState(MainActivity.parcelable);
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
    }
}