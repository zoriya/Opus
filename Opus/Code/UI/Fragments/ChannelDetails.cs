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
using YoutubeExplode;
using static Android.Provider.MediaStore.Audio;
using CursorLoader = Android.Support.V4.Content.CursorLoader;
using PlaylistItem = Opus.DataStructure.PlaylistItem;
using PopupMenu = Android.Support.V7.Widget.PopupMenu;
using RecyclerView = Android.Support.V7.Widget.RecyclerView;
using SearchView = Android.Support.V7.Widget.SearchView;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Opus.Fragments
{
    public class ChannelDetails : Fragment, /*PopupMenu.IOnMenuItemClickListener,*/ AppBarLayout.IOnOffsetChangedListener
    {
        public static ChannelDetails instance;
        private Channel item;

        private TextView EmptyView;
        private RecyclerView ListView;
        private SectionAdapter adapter;
        private List<Section> sections = new List<Section>();

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            if (item == null)
            {
                MainActivity.instance.SupportFragmentManager.PopBackStack();
                return;
            }

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
            if(item != null)
                CreateHeader();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            PopulateList();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            return view;
        }

        void CreateHeader()
        {
            Activity.FindViewById<RelativeLayout>(Resource.Id.playlistHeader).Visibility = ViewStates.Visible;
            ((AppBarLayout.LayoutParams)Activity.FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsingToolbar).LayoutParameters).ScrollFlags = AppBarLayout.LayoutParams.ScrollFlagScroll | AppBarLayout.LayoutParams.ScrollFlagExitUntilCollapsed;
            Activity.FindViewById<AppBarLayout>(Resource.Id.appbar).AddOnOffsetChangedListener(this);
            Activity.FindViewById<TextView>(Resource.Id.headerTitle).Text = item.Name;
            Activity.FindViewById<TextView>(Resource.Id.headerAuthor).Text = null;
            Activity.FindViewById<TextView>(Resource.Id.headerNumber).Text = null;
            Activity.FindViewById(Resource.Id.playlistButtons).Visibility = ViewStates.Gone;
            Picasso.With(Android.App.Application.Context).Load(item.ImageURL).Placeholder(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).Into(Activity.FindViewById<ImageView>(Resource.Id.headerArt));
            Activity.FindViewById(Resource.Id.collapsingToolbar).RequestLayout();
        }

        public override void OnDestroyView()
        {
            //MainActivity.instance.RemoveFilterListener(Search);
            if (!MainActivity.instance.Paused)
            {
                Activity.FindViewById(Resource.Id.playlistButtons).Visibility = ViewStates.Visible;
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
                    SearchView searchView = (SearchView)MainActivity.instance.menu.FindItem(Resource.Id.search).ActionView;
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

        public static Fragment NewInstance(Channel item)
        {
            instance = new ChannelDetails { Arguments = new Bundle() };
            instance.item = item;
            return instance;
        }

        async Task PopulateList()
        {
            sections.Clear();
            try
            {
                var request = YoutubeManager.YoutubeService.ChannelSections.List("snippet,contentDetails");
                request.ChannelId = item.YoutubeID;

                var response = await request.ExecuteAsync();

                for (int i = 0; i < response.Items.Count; i++)
                {
                    if (response.Items[i].ContentDetails?.Playlists.Count == 1)
                    {
                        System.Console.WriteLine("&Single playlsit");
                        sections.Add(new Section(null, SectionType.SinglePlaylist));
                        LoadPlaylist(sections.Count - 1, response.Items[i].ContentDetails.Playlists[0]);
                    }

                    //else if (item.ContentDetails.Playlists.Count > 1)
                    //    sections.Add(new Section(item.Snippet.Title, SectionType.PlaylistList, new List<PlaylistItem>()));

                    //else if (item.ContentDetails.Channels.Count > 1)
                    //    sections.Add(new Section(item.Snippet.Title, SectionType.ChannelList, new List<Channel>()));
                }

                adapter = new SectionAdapter(sections);
                ListView.SetAdapter(adapter);
            }
            catch (System.Net.Http.HttpRequestException) { System.Console.WriteLine("&Channel section list time out"); }
        }

        async void LoadPlaylist(int slot, string playlistID)
        {
            System.Console.WriteLine("&Loading playlist: " + playlistID + " slot: " + slot + " sections count " + sections.Count);
            var pl = await new YoutubeClient().GetPlaylistAsync(playlistID, 1);
            sections[slot] = new Section(pl.Title, SectionType.SinglePlaylist, Song.FromVideoArray(pl.Videos));
            adapter.NotifyItemChanged(slot);
        }

        public async void OnRefresh(object sender, System.EventArgs e)
        {
            await PopulateList();
            MainActivity.instance.contentRefresh.Refreshing = false;
        }

        //public void Search(object sender, SearchView.QueryTextChangeEventArgs e)
        //{
        //    if (e.NewText == "")
        //        query = null;
        //    else
        //        query = e.NewText.ToLower();

        //    if (item.LocalID != -1)
        //        LoaderManager.GetInstance(this).RestartLoader(0, null, this);
        //    else
        //    {
        //        if (query == null)
        //            adapter.tracks.SetFilter(x => true);
        //        else
        //            adapter.tracks.SetFilter(x => x.Title.ToLower().Contains(query) || x.Artist.ToLower().Contains(query));

        //        adapter.NotifyDataSetChanged();
        //    }
        //}

        public void More(Song song, int position)
        {
            //BottomSheetDialog bottomSheet = new BottomSheetDialog(MainActivity.instance);
            //View bottomView = LayoutInflater.Inflate(Resource.Layout.BottomSheet, null);
            //bottomView.FindViewById<TextView>(Resource.Id.bsTitle).Text = song.Title;
            //bottomView.FindViewById<TextView>(Resource.Id.bsArtist).Text = song.Artist;
            //if (song.AlbumArt == -1 || song.IsYt)
            //{
            //    Picasso.With(MainActivity.instance).Load(song.Album).Placeholder(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
            //}
            //else
            //{
            //    var songCover = Uri.Parse("content://media/external/audio/albumart");
            //    var songAlbumArtUri = ContentUris.WithAppendedId(songCover, song.AlbumArt);

            //    Picasso.With(MainActivity.instance).Load(songAlbumArtUri).Placeholder(Resource.Drawable.noAlbum).Resize(400, 400).CenterCrop().Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
            //}
            //bottomSheet.SetContentView(bottomView);

            //List<BottomSheetAction> actions = new List<BottomSheetAction>
            //{
            //    new BottomSheetAction(Resource.Drawable.Play, Resources.GetString(Resource.String.play), (sender, eventArg) =>
            //    {
            //        if(useHeader)
            //            PlaylistManager.PlayInOrder(item, position);
            //        else
            //            SongManager.Play(song);
            //        bottomSheet.Dismiss();
            //    }),
            //    new BottomSheetAction(Resource.Drawable.PlaylistPlay, Resources.GetString(Resource.String.play_next), (sender, eventArg) =>
            //    {
            //        SongManager.PlayNext(song);
            //        bottomSheet.Dismiss();
            //    }),
            //    new BottomSheetAction(Resource.Drawable.Queue, Resources.GetString(Resource.String.play_last), (sender, eventArg) =>
            //    {
            //        SongManager.PlayLast(song);
            //        bottomSheet.Dismiss();
            //    }),
            //    new BottomSheetAction(Resource.Drawable.PlaylistAdd, Resources.GetString(Resource.String.add_to_playlist), (sender, eventArg) =>
            //    {
            //        PlaylistManager.AddSongToPlaylistDialog(song);
            //        bottomSheet.Dismiss();
            //    })
            //};

            //if (item.HasWritePermission)
            //{
            //    actions.Add(new BottomSheetAction(Resource.Drawable.Close, Resources.GetString(Resource.String.remove_track_from_playlist), (sender, eventArg) =>
            //    {
            //        RemoveFromPlaylist(song, position);
            //        bottomSheet.Dismiss();
            //    }));
            //}

            //if (!song.IsYt)
            //{
            //    actions.Add(new BottomSheetAction(Resource.Drawable.Edit, Resources.GetString(Resource.String.edit_metadata), (sender, eventArg) =>
            //    {
            //        LocalManager.EditMetadata(song);
            //        bottomSheet.Dismiss();
            //    }));
            //}
            //else
            //{
            //    actions.AddRange(new BottomSheetAction[]
            //    {
            //        new BottomSheetAction(Resource.Drawable.PlayCircle, Resources.GetString(Resource.String.create_mix_from_song), (sender, eventArg) =>
            //        {
            //            YoutubeManager.CreateMixFromSong(song);
            //            bottomSheet.Dismiss();
            //        }),
            //        new BottomSheetAction(Resource.Drawable.Download, Resources.GetString(Resource.String.download), (sender, eventArg) =>
            //        {
            //            YoutubeManager.Download(new[] { song });
            //            bottomSheet.Dismiss();
            //        })
            //    });
            //}

            //bottomSheet.FindViewById<ListView>(Resource.Id.bsItems).Adapter = new BottomSheetAdapter(MainActivity.instance, Resource.Layout.BottomSheetText, actions);
            //bottomSheet.Show();
        }


        public override void OnResume()
        {
            base.OnResume();
            instance = this;

            //if (!Activity.FindViewById<ImageButton>(Resource.Id.headerPlay).HasOnClickListeners)
            //    Activity.FindViewById<ImageButton>(Resource.Id.headerPlay).Click += (sender, e0) => { PlaylistManager.PlayInOrder(item); };
            //if (!Activity.FindViewById<ImageButton>(Resource.Id.headerShuffle).HasOnClickListeners)
            //    Activity.FindViewById<ImageButton>(Resource.Id.headerShuffle).Click += (sender, e0) => { PlaylistManager.Shuffle(item); };
            //if (!Activity.FindViewById<ImageButton>(Resource.Id.headerMore).HasOnClickListeners)
            //    Activity.FindViewById<ImageButton>(Resource.Id.headerMore).Click += PlaylistMore;
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
