using Android.Content;
using Android.Database;
using Android.Net;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using Opus.Adapter;
using Opus.Api;
using Opus.DataStructure;
using Opus.Others;
using Square.Picasso;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Android.Provider.MediaStore.Audio;
using CursorLoader = Android.Support.V4.Content.CursorLoader;
using PlaylistItem = Opus.DataStructure.PlaylistItem;
using PopupMenu = Android.Support.V7.Widget.PopupMenu;
using RecyclerView = Android.Support.V7.Widget.RecyclerView;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Opus.Fragments
{
    public class PlaylistTracks : Fragment, PopupMenu.IOnMenuItemClickListener, AppBarLayout.IOnOffsetChangedListener
    {
        public static PlaylistTracks instance;
        public PlaylistItem item;
        public RecyclerView ListView;
        public PlaylistTrackAdapter adapter;
        private Android.Support.V7.Widget.Helper.ItemTouchHelper itemTouchHelper;
        public List<Song> result = null;
        private string nextPageToken = null;
        public bool fullyLoadded = true;
        public bool forked;
        public bool lastVisible = false;
        public bool useHeader = true;
        public bool navigating = false;

        public List<Song> tracks = new List<Song>();
        private bool loading = false;

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
            MainActivity.instance.DisplaySearch();

            MainActivity.instance.SupportActionBar.SetHomeButtonEnabled(true);
            MainActivity.instance.SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            MainActivity.instance.SupportActionBar.SetDisplayShowTitleEnabled(true);
            MainActivity.instance.FindViewById(Resource.Id.toolbarLogo).Visibility = ViewStates.Gone;
            MainActivity.instance.SupportActionBar.Title = item.Name;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.RecyclerFragment, container, false);
            ListView = view.FindViewById<RecyclerView>(Resource.Id.recycler);
            ListView.SetLayoutManager(new Android.Support.V7.Widget.LinearLayoutManager(MainActivity.instance));
            ListView.SetAdapter(new PlaylistTrackAdapter(new List<Song>()));
            ListView.ScrollChange += ListView_ScrollChange;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            PopulateList();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            if(useHeader)
                CreateHeader();
            return view;
        }

        private void ListView_ScrollChange(object sender, View.ScrollChangeEventArgs e)
        {
            if (((Android.Support.V7.Widget.LinearLayoutManager)ListView?.GetLayoutManager())?.FindLastVisibleItemPosition() == adapter?.ItemCount - 1)
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                LoadMore();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        void CreateHeader()
        {
            Activity.FindViewById<RelativeLayout>(Resource.Id.playlistHeader).Visibility = ViewStates.Visible;
            ((AppBarLayout.LayoutParams)Activity.FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsingToolbar).LayoutParameters).ScrollFlags = AppBarLayout.LayoutParams.ScrollFlagScroll | AppBarLayout.LayoutParams.ScrollFlagExitUntilCollapsed;
            Activity.FindViewById<AppBarLayout>(Resource.Id.appbar).AddOnOffsetChangedListener(this);
            Activity.FindViewById<TextView>(Resource.Id.headerTitle).Text = item.Name;

            if (!Activity.FindViewById<ImageButton>(Resource.Id.headerPlay).HasOnClickListeners)
                Activity.FindViewById<ImageButton>(Resource.Id.headerPlay).Click += (sender, e0) => { PlaylistManager.PlayInOrder(item); };
            if (!Activity.FindViewById<ImageButton>(Resource.Id.headerShuffle).HasOnClickListeners)
                Activity.FindViewById<ImageButton>(Resource.Id.headerShuffle).Click += (sender, e0) => { PlaylistManager.Shuffle(item); };
            if (!Activity.FindViewById<ImageButton>(Resource.Id.headerMore).HasOnClickListeners)
                Activity.FindViewById<ImageButton>(Resource.Id.headerMore).Click += PlaylistMore;

            if (item.LocalID != 0 && item.ImageURL == null)
            {
                Activity.FindViewById<TextView>(Resource.Id.headerAuthor).Text = MainActivity.account == null ? "by me" : "by " + MainActivity.account.DisplayName;
            }
            else if (item.YoutubeID != null && item.YoutubeID != "")
            {
                Activity.FindViewById<TextView>(Resource.Id.headerAuthor).Text = item.Owner;
                Activity.FindViewById<TextView>(Resource.Id.headerNumber).Text = item.Count.ToString() + " " + GetString(Resource.String.songs);
                if (item.Count == -1)
                    Activity.FindViewById<TextView>(Resource.Id.headerNumber).Text = "NaN" + " " + GetString(Resource.String.songs);

                Picasso.With(Android.App.Application.Context).Load(item.ImageURL).Placeholder(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).Into(Activity.FindViewById<ImageView>(Resource.Id.headerArt));
            }
            Activity.FindViewById(Resource.Id.collapsingToolbar).RequestLayout();
        }

        public override void OnDestroyView()
        {
            Activity.FindViewById<ImageButton>(Resource.Id.headerMore).Click -= PlaylistMore;

            if (!MainActivity.instance.Paused)
            {
                Activity.FindViewById<RelativeLayout>(Resource.Id.playlistHeader).Visibility = ViewStates.Gone;

                MainActivity.instance.HideSearch();
                MainActivity.instance.SupportActionBar.SetHomeButtonEnabled(false);
                MainActivity.instance.SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                MainActivity.instance.SupportActionBar.SetDisplayShowTitleEnabled(true);
                MainActivity.instance.SupportActionBar.SetDisplayShowTitleEnabled(false);
                MainActivity.instance.FindViewById(Resource.Id.toolbarLogo).Visibility = ViewStates.Visible;

                MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
                Activity.FindViewById<AppBarLayout>(Resource.Id.appbar).RemoveOnOffsetChangedListener(this);


                if (YoutubeSearch.instances != null)
                {
                    MainActivity.instance.FindViewById<TabLayout>(Resource.Id.tabs).Visibility = ViewStates.Visible;
                    Android.Support.V7.Widget.SearchView searchView = (Android.Support.V7.Widget.SearchView)MainActivity.instance.menu.FindItem(Resource.Id.search).ActionView;
                    searchView.Focusable = false;
                    MainActivity.instance.menu.FindItem(Resource.Id.search).ExpandActionView();
                    searchView.SetQuery(YoutubeSearch.instances[0].Query, false);
                    searchView.ClearFocus();

                    int selectedTab = 0;
                    for (int i = 0; i < YoutubeSearch.instances.Length; i++)
                    {
                        if (YoutubeSearch.instances[i].IsFocused)
                            selectedTab = i;
                    }
                }
                instance = null;
            }
            base.OnDestroyView();
        }


        //Header more click
        public bool OnMenuItemClick(IMenuItem menuItem)
        {
            switch (menuItem.ItemId)
            {
                case Resource.Id.download:
                    YoutubeManager.DownloadPlaylist(item.Name, item.YoutubeID);
                    break;

                case Resource.Id.fork:
#pragma warning disable CS4014
                    PlaylistManager.ForkPlaylist(item.YoutubeID);
                    break;

                case Resource.Id.addToQueue:
                    PlaylistManager.AddToQueue(item);
                    break;

                case Resource.Id.rename:
                    PlaylistManager.Rename(item, () => 
                    {
                        MainActivity.instance.FindViewById<TextView>(Resource.Id.headerTitle).Text = item.Name;
                    });
                    break;

                case Resource.Id.sync:
                    PlaylistManager.StopSyncingDialog(item, () => { /*SHOULD RECREATE CURSOR LOADER HERE*/ });
                    break;

                case Resource.Id.delete:
                    PlaylistManager.Delete(item, () => 
                    {
                        MainActivity.instance.SupportFragmentManager.PopBackStack();
                    });
                    break;
            }
            return true;
        }

        void PlaylistMore(object sender,  System.EventArgs eventArgs)
        {
            PopupMenu menu = new PopupMenu(MainActivity.instance, MainActivity.instance.FindViewById<ImageButton>(Resource.Id.headerMore));
            if (item.LocalID == 0 && item.HasWritePermission)
                menu.Inflate(Resource.Menu.ytplaylist_header_more);
            else if (item.LocalID == 0 && forked)
                menu.Inflate(Resource.Menu.ytplaylistnowrite_header_more);
            else if (item.LocalID == 0)
                menu.Inflate(Resource.Menu.ytplaylist_nowrite_nofork_header_more);
            else
                menu.Inflate(Resource.Menu.playlist_header_more);

            if (item.SyncState == SyncState.True)
            {
                menu.Menu.GetItem(0).SetTitle("Sync Now");
                menu.Menu.Add(Menu.None, Resource.Id.sync, menu.Menu.Size() - 3, "Stop Syncing");
            }
            menu.SetOnMenuItemClickListener(this);
            menu.Show();
        }

        //public static Fragment NewInstance(List<Song> songs, string playlistName)
        //{
        //    instance = new PlaylistTracks { Arguments = new Bundle() };
        //    instance.tracks = songs;
        //    instance.playlistName = playlistName;
        //    instance.useHeader = false;
        //    instance.fullyLoadded = true;
        //    instance.hasWriteAcess = false;
        //    return instance;
        //}

        public static Fragment NewInstance(PlaylistItem item, bool forked)
        {
            instance = new PlaylistTracks { Arguments = new Bundle() };
            instance.item = item;
            instance.forked = forked;
            instance.useHeader = true;
            instance.fullyLoadded = item.LocalID != 0 && item.LocalID != -1;
            return instance;
        }

        async Task PopulateList()
        {
            if (item.LocalID == 0 && item.YoutubeID == "" && tracks.Count == 0)
                return;

            if (item.LocalID != 0)
            {
                Uri musicUri = Playlists.Members.GetContentUri("external", item.LocalID);

                CursorLoader cursorLoader = new CursorLoader(Android.App.Application.Context, musicUri, null, null, null, null);
                ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

                tracks = new List<Song>();

                if (musicCursor != null && musicCursor.MoveToFirst())
                {
                    int titleID = musicCursor.GetColumnIndex(Media.InterfaceConsts.Title);
                    int artistID = musicCursor.GetColumnIndex(Media.InterfaceConsts.Artist);
                    int albumID = musicCursor.GetColumnIndex(Media.InterfaceConsts.Album);
                    int albumArtID = musicCursor.GetColumnIndex(Albums.InterfaceConsts.AlbumId);
                    int thisID = musicCursor.GetColumnIndex(Playlists.Members.AudioId);
                    int pathID = musicCursor.GetColumnIndex(Media.InterfaceConsts.Data);
                    do
                    {
                        string Artist = musicCursor.GetString(artistID);
                        string Title = musicCursor.GetString(titleID);
                        string Album = musicCursor.GetString(albumID);
                        long AlbumArt = musicCursor.GetLong(albumArtID);
                        long id = musicCursor.GetLong(thisID);
                        string path = musicCursor.GetString(pathID);

                        if (Title == null)
                            Title = "Unknown Title";
                        if (Artist == null)
                            Artist = "Unknow Artist";
                        if (Album == null)
                            Album = "Unknow Album";

                        Song song = new Song(Title, Artist, Album, null, AlbumArt, id, path);
                        if (item.SyncState == SyncState.True)
                            song = LocalManager.CompleteItem(song);

                        tracks.Add(song);
                    }
                    while (musicCursor.MoveToNext());
                    musicCursor.Close();
                }

                adapter = new PlaylistTrackAdapter(tracks);
                adapter.ItemClick += ListView_ItemClick;
                adapter.ItemLongClick += ListView_ItemLongClick;
                ListView.SetAdapter(adapter);

                Android.Support.V7.Widget.Helper.ItemTouchHelper.Callback callback = new ItemTouchCallback(adapter, false);
                itemTouchHelper = new Android.Support.V7.Widget.Helper.ItemTouchHelper(callback);
                itemTouchHelper.AttachToRecyclerView(ListView);

                Activity.FindViewById<TextView>(Resource.Id.headerNumber).Text = tracks.Count.ToString() + " songs";
                var songCover = Uri.Parse("content://media/external/audio/albumart");
                var songAlbumArtUri = ContentUris.WithAppendedId(songCover, tracks[0].AlbumArt);

                if(item.ImageURL == null)
                    Picasso.With(Android.App.Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.noAlbum).Resize(1080, 1080).CenterCrop().Into(Activity.FindViewById<ImageView>(Resource.Id.headerArt));
            }
            else if (item.YoutubeID != null)
            {
                try
                {
                    tracks = new List<Song>();
                    var ytPlaylistRequest = YoutubeSearch.youtubeService.PlaylistItems.List("snippet, contentDetails");
                    ytPlaylistRequest.PlaylistId = item.YoutubeID;
                    ytPlaylistRequest.MaxResults = 50;

                    var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                    foreach (var item in ytPlaylist.Items)
                    {
                        if (item.Snippet.Title != "[Deleted video]" && item.Snippet.Title != "Private video" && item.Snippet.Title != "Deleted video")
                        {
                            Song song = new Song(item.Snippet.Title, item.Snippet.ChannelTitle, item.Snippet.Thumbnails.High.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true, false)
                            {
                                TrackID = item.Id
                            };
                            tracks.Add(song);
                        }
                    }

                    nextPageToken = ytPlaylist.NextPageToken;
                    if(nextPageToken == null)
                        fullyLoadded = true;

                    adapter = new PlaylistTrackAdapter(tracks);
                    adapter.ItemClick += ListView_ItemClick;
                    adapter.ItemLongClick += ListView_ItemLongClick;
                    ListView.SetAdapter(adapter);

                    Android.Support.V7.Widget.Helper.ItemTouchHelper.Callback callback = new ItemTouchCallback(adapter, false);
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
            return song.YoutubeID;
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
                var ytPlaylistRequest = YoutubeSearch.youtubeService.PlaylistItems.List("snippet, contentDetails");
                ytPlaylistRequest.PlaylistId = item.YoutubeID;
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
            result = new List<Song>();
            for (int i = 0; i < tracks.Count; i++)
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

            Android.Support.V7.Widget.Helper.ItemTouchHelper.Callback callback = new ItemTouchCallback(adapter, false);
            itemTouchHelper = new Android.Support.V7.Widget.Helper.ItemTouchHelper(callback);
            itemTouchHelper.AttachToRecyclerView(ListView);

            ListView.SetAdapter(adapter);
        }

        private void ListView_ItemClick(object sender, int Position)
        {
            if (!useHeader)
                Position--;

            PlaylistManager.PlayInOrder(item, Position);
        }

        private void ListView_ItemLongClick(object sender, int Position)
        {
            More(Position);
        }

        public void More(int position)
        {
            if (!useHeader)
                position--;

            Song item = tracks[position];
            if (result != null && result.Count > position)
                item = result[position];

            BottomSheetDialog bottomSheet = new BottomSheetDialog(MainActivity.instance);
            View bottomView = LayoutInflater.Inflate(Resource.Layout.BottomSheet, null);
            bottomView.FindViewById<TextView>(Resource.Id.bsTitle).Text = item.Title;
            bottomView.FindViewById<TextView>(Resource.Id.bsArtist).Text = item.Artist;
            if (item.Album == null)
            {
                var songCover = Uri.Parse("content://media/external/audio/albumart");
                var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, item.AlbumArt);

                Picasso.With(MainActivity.instance).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.noAlbum).Resize(400, 400).CenterCrop().Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
            }
            else
            {
                Picasso.With(MainActivity.instance).Load(item.Album).Placeholder(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
            }
            bottomSheet.SetContentView(bottomView);

            List<BottomSheetAction> actions = new List<BottomSheetAction>
            {
                new BottomSheetAction(Resource.Drawable.Play, Resources.GetString(Resource.String.play), (sender, eventArg) => 
                {
                    PlaylistManager.PlayInOrder(this.item, position);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.PlaylistPlay, Resources.GetString(Resource.String.play_next), (sender, eventArg) =>
                {
                    SongManager.PlayNext(item);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Queue, Resources.GetString(Resource.String.play_last), (sender, eventArg) =>
                {
                    SongManager.PlayLast(item);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.PlaylistAdd, Resources.GetString(Resource.String.add_to_playlist), (sender, eventArg) => 
                {
                    PlaylistManager.AddSongToPlaylistDialog(item);
                    bottomSheet.Dismiss();
                })
            };

            if (this.item.HasWritePermission)
            {
                actions.Add(new BottomSheetAction(Resource.Drawable.Close, Resources.GetString(Resource.String.remove_track_from_playlist), (sender, eventArg) =>
                {
                    RemoveFromPlaylist(item, position);
                    bottomSheet.Dismiss();
                }));
            }

            if (!item.IsYt)
            {
                actions.Add(new BottomSheetAction(Resource.Drawable.Edit, Resources.GetString(Resource.String.edit_metadata), (sender, eventArg) =>
                {
                    LocalManager.EditMetadata(item);
                    bottomSheet.Dismiss();
                }));
            }
            else
            {
                actions.AddRange(new BottomSheetAction[]
                {
                    new BottomSheetAction(Resource.Drawable.PlayCircle, Resources.GetString(Resource.String.create_mix_from_song), (sender, eventArg) =>
                    {
                        YoutubeManager.CreateMixFromSong(item);
                        bottomSheet.Dismiss();
                    }),
                    new BottomSheetAction(Resource.Drawable.Download, Resources.GetString(Resource.String.download), (sender, eventArg) =>
                    {
                        YoutubeManager.Download(new[] { item }, null);
                        bottomSheet.Dismiss();
                    })
                });
            }

            bottomSheet.FindViewById<ListView>(Resource.Id.bsItems).Adapter = new BottomSheetAdapter(MainActivity.instance, Resource.Layout.BottomSheetText, actions);
            bottomSheet.Show();
        }

        public void RemoveFromPlaylist(Song item, int position)
        {
            PlaylistManager.RemoveTrackFromPlaylistDialog(this.item, item, () =>
            {
                adapter.NotifyItemRemoved(position);
            }, () =>
            {
                adapter.NotifyItemChanged(position);
            }, () =>
            {
                adapter.NotifyItemInserted(position);
            });
        }

        public override void OnResume()
        {
            base.OnResume();
            instance = this;

            if(useHeader)
            {
                if (!Activity.FindViewById<ImageButton>(Resource.Id.headerPlay).HasOnClickListeners)
                    Activity.FindViewById<ImageButton>(Resource.Id.headerPlay).Click += (sender, e0) => { PlaylistManager.PlayInOrder(item); };
                if (!Activity.FindViewById<ImageButton>(Resource.Id.headerShuffle).HasOnClickListeners)
                    Activity.FindViewById<ImageButton>(Resource.Id.headerShuffle).Click += (sender, e0) => { PlaylistManager.Shuffle(item); };
                if (!Activity.FindViewById<ImageButton>(Resource.Id.headerMore).HasOnClickListeners)
                    Activity.FindViewById<ImageButton>(Resource.Id.headerMore).Click += PlaylistMore;
            }
        }

        public void OnOffsetChanged(AppBarLayout appBarLayout, int verticalOffset)
        {
            if (instance == null)
                return;

            if (System.Math.Abs(verticalOffset) <= appBarLayout.TotalScrollRange - MainActivity.instance.FindViewById<Toolbar>(Resource.Id.toolbar).Height)
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
