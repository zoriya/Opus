using Android.Content;
using Android.Database;
using Android.Net;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using Java.Lang;
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
    public class PlaylistTracks : Fragment, PopupMenu.IOnMenuItemClickListener, AppBarLayout.IOnOffsetChangedListener, LoaderManager.ILoaderCallbacks
    {
        public static PlaylistTracks instance;
        public PlaylistItem item;

        public TextView EmptyView;
        public RecyclerView ListView;
        public PlaylistTrackAdapter adapter;
        public Android.Support.V7.Widget.Helper.ItemTouchHelper itemTouchHelper;

        private string query;
        private string nextPageToken = null;
        public bool fullyLoadded = true;
        public bool lastVisible = false;
        public bool useHeader = true;
        public bool isInEditMode = true;
        private bool isForked;
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
            View view = inflater.Inflate(Resource.Layout.LonelyRecycler, container, false);
            ListView = view.FindViewById<RecyclerView>(Resource.Id.recycler);
            ListView.SetLayoutManager(new Android.Support.V7.Widget.LinearLayoutManager(MainActivity.instance));
            ListView.SetAdapter(new PlaylistTrackAdapter(new SearchableList<Song>()));
            ListView.ScrollChange += ListView_ScrollChange;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            PopulateList();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            if(useHeader)
                CreateHeader();
            //if (item.SyncState == SyncState.Error)
            //    CreateSyncBanner();
            return view;
        }

        private void ListView_ScrollChange(object sender, View.ScrollChangeEventArgs e)
        {
            if (((Android.Support.V7.Widget.LinearLayoutManager)ListView?.GetLayoutManager())?.FindLastVisibleItemPosition() == adapter?.ItemCount - 1)
                LoadMore();
        }

        void CreateHeader()
        {
            Activity.FindViewById<RelativeLayout>(Resource.Id.playlistHeader).Visibility = ViewStates.Visible;
            ((AppBarLayout.LayoutParams)Activity.FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsingToolbar).LayoutParameters).ScrollFlags = AppBarLayout.LayoutParams.ScrollFlagScroll | AppBarLayout.LayoutParams.ScrollFlagExitUntilCollapsed;
            Activity.FindViewById<AppBarLayout>(Resource.Id.appbar).AddOnOffsetChangedListener(this);
            Activity.FindViewById<TextView>(Resource.Id.headerTitle).Text = item.Name;

            if (!Activity.FindViewById<ImageButton>(Resource.Id.headerPlay).HasOnClickListeners)
                Activity.FindViewById<ImageButton>(Resource.Id.headerPlay).Click += HeaderPlay;
            if (!Activity.FindViewById<ImageButton>(Resource.Id.headerShuffle).HasOnClickListeners)
                Activity.FindViewById<ImageButton>(Resource.Id.headerShuffle).Click += HeaderShuffle;
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

        //void CreateSyncBanner()
        //{
        //    MainActivity.instance.FindViewById(Resource.Id.banner).Visibility = ViewStates.Visible;
        //}

        void HeaderPlay(object sender, System.EventArgs e)
        {
            PlaylistManager.PlayInOrder(item);
        }

        void HeaderShuffle(object sender, System.EventArgs e)
        {
            PlaylistManager.Shuffle(item);
        }

        public override void OnDestroyView()
        {
            Activity.FindViewById<ImageButton>(Resource.Id.headerPlay).Click -= HeaderPlay;
            Activity.FindViewById<ImageButton>(Resource.Id.headerShuffle).Click -= HeaderShuffle;
            Activity.FindViewById<ImageButton>(Resource.Id.headerMore).Click -= PlaylistMore;

            if (!MainActivity.instance.Paused)
            {
                Activity.FindViewById<RelativeLayout>(Resource.Id.playlistHeader).Visibility = ViewStates.Gone;

                MainActivity.instance.HideSearch();
                MainActivity.instance.SupportActionBar.SetHomeButtonEnabled(false);
                MainActivity.instance.SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                MainActivity.instance.SupportActionBar.SetDisplayShowTitleEnabled(true);
                MainActivity.instance.SupportActionBar.SetDisplayShowTitleEnabled(false);

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
                    YoutubeManager.DownloadPlaylist(item, true, true);
                    break;

                case Resource.Id.fork:
                    if (isForked)
                        PlaylistManager.Unfork(item);
                    else
                        PlaylistManager.ForkPlaylist(item);

                    isForked = !isForked;
                    break;

                case Resource.Id.addToQueue:
                    if (useHeader)
                        PlaylistManager.AddToQueue(item);
                    else
                        SongManager.AddToQueue(adapter.tracks);
                    break;

                case Resource.Id.name:
                    PlaylistManager.Rename(item, () => 
                    {
                        MainActivity.instance.FindViewById<TextView>(Resource.Id.headerTitle).Text = item.Name;
                    });
                    break;

                case Resource.Id.sync:
                    PlaylistManager.StopSyncingDialog(item, () => 
                    {
                        MainActivity.instance.SupportFragmentManager.PopBackStack();
                    });
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
            menu.Inflate(Resource.Menu.playlist_header_more); //Contains "add to queue"

            if (item.SyncState == SyncState.True)
            {
                menu.Menu.Add(Menu.None, Resource.Id.download, 1, MainActivity.instance.GetString(Resource.String.sync_now));
                menu.Menu.Add(Menu.None, Resource.Id.sync, 5, MainActivity.instance.GetString(Resource.String.stop_sync));
            }
            else if(item.YoutubeID != null)
            {
                menu.Menu.Add(Menu.None, Resource.Id.download, 1, MainActivity.instance.GetString(Resource.String.sync));
            }

            if(item.YoutubeID != null)
            {
                if(isForked)
                    menu.Menu.Add(Menu.None, Resource.Id.fork, 2, MainActivity.instance.GetString(Resource.String.unfork));
                else
                    menu.Menu.Add(Menu.None, Resource.Id.fork, 2, MainActivity.instance.GetString(Resource.String.add_to_library));
            }

            if(item.HasWritePermission)
                menu.Menu.Add(Menu.None, Resource.Id.name, 3, MainActivity.instance.GetString(Resource.String.rename));

            menu.Menu.Add(Menu.None, Resource.Id.delete, 4, MainActivity.instance.GetString(Resource.String.delete));
            menu.SetOnMenuItemClickListener(this);
            menu.Show();
        }

        public static Fragment NewInstance(List<Song> songs, string playlistName)
        {
            instance = new PlaylistTracks { Arguments = new Bundle() };
            instance.item = new PlaylistItem() { Name = playlistName, Count = songs.Count, HasWritePermission = false, LocalID = -1, YoutubeID = null };
            instance.useHeader = false;
            instance.fullyLoadded = true;
            instance.adapter = new PlaylistTrackAdapter(new SearchableList<Song>(songs));
            return instance;
        }

        public static Fragment NewInstance(PlaylistItem item)
        {
            instance = new PlaylistTracks { Arguments = new Bundle() };
            instance.item = item;
            instance.useHeader = true;
            instance.fullyLoadded = item.LocalID != 0 && item.LocalID != -1;

            Task.Run(async () =>
            {
                instance.isForked = await PlaylistManager.IsForked(item);
            });
            return instance;
        }

        async Task PopulateList()
        {
            if (item.LocalID == -1 && item.YoutubeID == null && adapter?.tracks.Count == 0)
                return;

            if (item.LocalID != -1)
            {
                if (await MainActivity.instance.GetReadPermission() == false)
                {
                    MainActivity.instance.FindViewById(Resource.Id.loading).Visibility = ViewStates.Gone;
                    return;
                }

                adapter = new PlaylistTrackAdapter();
                ListView.SetAdapter(adapter);

                Android.Support.V7.Widget.Helper.ItemTouchHelper.Callback callback = new ItemTouchCallback(adapter, false);
                itemTouchHelper = new Android.Support.V7.Widget.Helper.ItemTouchHelper(callback);
                itemTouchHelper.AttachToRecyclerView(ListView);

                LoaderManager.GetInstance(this).InitLoader(0, null, this);
            }
            else if (item.YoutubeID != null)
            {
                fullyLoadded = false;
                SearchableList<Song> tracks = new SearchableList<Song>();
                adapter = new PlaylistTrackAdapter(tracks);
                ListView.SetAdapter(adapter);

                Android.Support.V7.Widget.Helper.ItemTouchHelper.Callback callback = new ItemTouchCallback(adapter, false);
                itemTouchHelper = new Android.Support.V7.Widget.Helper.ItemTouchHelper(callback);
                itemTouchHelper.AttachToRecyclerView(ListView);

                try
                {
                    var ytPlaylistRequest = YoutubeManager.YoutubeService.PlaylistItems.List("snippet, contentDetails");
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
                    if (nextPageToken == null)
                        fullyLoadded = true;

                    tracks.Invalidate();
                    adapter.NotifyDataSetChanged();

                }
                catch (System.Net.Http.HttpRequestException)
                {
                    MainActivity.instance.Timout();
                }
            }
            else if (adapter?.tracks.Count != 0)
            {
                ListView.SetAdapter(adapter);
            }
        }

        public Android.Support.V4.Content.Loader OnCreateLoader(int id, Bundle args)
        {
            Uri musicUri = Playlists.Members.GetContentUri("external", item.LocalID);
            string selection;
            if (query != null)
            {
                selection = Media.InterfaceConsts.Title + " LIKE \"%" + query + "%\" OR " + Media.InterfaceConsts.Artist + " LIKE \"%" + query + "%\"";
                isInEditMode = false;
            }
            else
            {
                selection = null;
                isInEditMode = true;
            }

            return new CursorLoader(Android.App.Application.Context, musicUri, null, selection, null, Playlists.Members.PlayOrder);
        }

        public void OnLoadFinished(Android.Support.V4.Content.Loader loader, Object data)
        {
            adapter.SwapCursor((ICursor)data, false);

            if (query == null && item.LocalID != -1 && adapter.BaseCount > 0)
            {
                Activity.FindViewById<TextView>(Resource.Id.headerNumber).Text = item.Count.ToString() + " " + GetString(Resource.String.elements);
                var songCover = Uri.Parse("content://media/external/audio/albumart");
                var songAlbumArtUri = ContentUris.WithAppendedId(songCover, adapter.GetItem(0).AlbumArt);

                if (item.ImageURL == null)
                    Picasso.With(Android.App.Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.noAlbum).Resize(1080, 1080).CenterCrop().Into(Activity.FindViewById<ImageView>(Resource.Id.headerArt));
            }
        }

        public void OnLoaderReset(Android.Support.V4.Content.Loader loader)
        {
            adapter.SwapCursor(null, false);
        }

        public async void OnRefresh(object sender, System.EventArgs e)
        {
            await PopulateList();
            MainActivity.instance.contentRefresh.Refreshing = false;
        }

        public async void LoadMore()
        {
            if (nextPageToken == null || loading)
                return;

            loading = true;
            try
            {
                var ytPlaylistRequest = YoutubeManager.YoutubeService.PlaylistItems.List("snippet, contentDetails");
                ytPlaylistRequest.PlaylistId = item.YoutubeID;
                ytPlaylistRequest.MaxResults = 50;
                ytPlaylistRequest.PageToken = nextPageToken;

                var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                if (instance == null)
                    return;

                int countBefore = adapter.BaseCount;

                foreach (var item in ytPlaylist.Items)
                {
                    if (item.Snippet.Title != "[Deleted video]" && item.Snippet.Title != "Private video" && item.Snippet.Title != "Deleted video")
                    {
                        Song song = new Song(item.Snippet.Title, item.Snippet.ChannelTitle, item.Snippet.Thumbnails.Default__.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true, false)
                        {
                            TrackID = item.Id
                        };
                        adapter.tracks.Add(song);
                    }
                }

                adapter.tracks.Invalidate();
                adapter.NotifyItemRangeInserted(countBefore, adapter.tracks.Count - countBefore);

                nextPageToken = ytPlaylist.NextPageToken;
                if (nextPageToken == null)
                {
                    fullyLoadded = true;
                    adapter.NotifyItemRemoved(adapter.ItemCount);
                }
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
            }
            loading = false;

            //We are still at the end of the list, should load the rest (normaly because of the search).
            if (!fullyLoadded && ((Android.Support.V7.Widget.LinearLayoutManager)ListView?.GetLayoutManager())?.FindLastVisibleItemPosition() == adapter?.ItemCount - 1)
                LoadMore();
        }

        public void Search(string search)
        {
            if (search == "")
                query = null;
            else
                query = search.ToLower();

            if(item.LocalID != -1)
                LoaderManager.GetInstance(this).RestartLoader(0, null, this);
            else
            {
                if (query == null)
                    adapter.tracks.SetFilter(x => true);
                else
                    adapter.tracks.SetFilter(x => x.Title.ToLower().Contains(query) || x.Artist.ToLower().Contains(query));

                adapter.NotifyDataSetChanged();
            }
        }

        public void More(Song song, int position)
        {
            BottomSheetDialog bottomSheet = new BottomSheetDialog(MainActivity.instance);
            View bottomView = LayoutInflater.Inflate(Resource.Layout.BottomSheet, null);
            bottomView.FindViewById<TextView>(Resource.Id.bsTitle).Text = song.Title;
            bottomView.FindViewById<TextView>(Resource.Id.bsArtist).Text = song.Artist;
            if (song.Album == null)
            {
                var songCover = Uri.Parse("content://media/external/audio/albumart");
                var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, song.AlbumArt);

                Picasso.With(MainActivity.instance).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.noAlbum).Resize(400, 400).CenterCrop().Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
            }
            else
            {
                Picasso.With(MainActivity.instance).Load(song.Album).Placeholder(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
            }
            bottomSheet.SetContentView(bottomView);

            List<BottomSheetAction> actions = new List<BottomSheetAction>
            {
                new BottomSheetAction(Resource.Drawable.Play, Resources.GetString(Resource.String.play), (sender, eventArg) => 
                {
                    if(useHeader)
                        PlaylistManager.PlayInOrder(item, position);
                    else
                        SongManager.Play(song);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.PlaylistPlay, Resources.GetString(Resource.String.play_next), (sender, eventArg) =>
                {
                    SongManager.PlayNext(song);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.Queue, Resources.GetString(Resource.String.play_last), (sender, eventArg) =>
                {
                    SongManager.PlayLast(song);
                    bottomSheet.Dismiss();
                }),
                new BottomSheetAction(Resource.Drawable.PlaylistAdd, Resources.GetString(Resource.String.add_to_playlist), (sender, eventArg) => 
                {
                    PlaylistManager.AddSongToPlaylistDialog(song);
                    bottomSheet.Dismiss();
                })
            };

            if (item.HasWritePermission)
            {
                actions.Add(new BottomSheetAction(Resource.Drawable.Close, Resources.GetString(Resource.String.remove_track_from_playlist), (sender, eventArg) =>
                {
                    RemoveFromPlaylist(song, position);
                    bottomSheet.Dismiss();
                }));
            }

            if (!song.IsYt)
            {
                actions.Add(new BottomSheetAction(Resource.Drawable.Edit, Resources.GetString(Resource.String.edit_metadata), (sender, eventArg) =>
                {
                    LocalManager.EditMetadata(song);
                    bottomSheet.Dismiss();
                }));
            }
            else
            {
                actions.AddRange(new BottomSheetAction[]
                {
                    new BottomSheetAction(Resource.Drawable.PlayCircle, Resources.GetString(Resource.String.create_mix_from_song), (sender, eventArg) =>
                    {
                        YoutubeManager.CreateMixFromSong(song);
                        bottomSheet.Dismiss();
                    }),
                    new BottomSheetAction(Resource.Drawable.Download, Resources.GetString(Resource.String.download), (sender, eventArg) =>
                    {
                        YoutubeManager.Download(new[] { song });
                        bottomSheet.Dismiss();
                    })
                });
            }

            bottomSheet.FindViewById<ListView>(Resource.Id.bsItems).Adapter = new BottomSheetAdapter(MainActivity.instance, Resource.Layout.BottomSheetText, actions);
            bottomSheet.Show();
        }

        public void RemoveFromPlaylist(Song item, int position)
        {
            PlaylistManager.RemoveTrackFromPlaylistDialog(this.item, item, position, () =>
            {
                if (position == 0)
                    adapter.NotifyItemChanged(0);
                else
                    adapter.NotifyItemRemoved(position);
            }, () =>
            {
                adapter.NotifyItemChanged(position);
            }, () =>
            {
                if (position == 0)
                    adapter.NotifyItemChanged(position);
                else
                    adapter.NotifyItemInserted(position);
            });
        }

        public override void OnResume()
        {
            base.OnResume();
            instance = this;

            if (useHeader)
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

        public override void OnDestroy()
        {
            base.OnDestroy();
            MainActivity.instance.FindViewById(Resource.Id.toolbarLogo).Visibility = ViewStates.Visible;
        }
    }
}
