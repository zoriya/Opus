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
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using PlaylistItem = Opus.DataStructure.PlaylistItem;
using RecyclerView = Android.Support.V7.Widget.RecyclerView;
using SearchView = Android.Support.V7.Widget.SearchView;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Opus.Fragments
{
    public class ChannelDetails : Fragment, AppBarLayout.IOnOffsetChangedListener
    {
        public static ChannelDetails instance;
        private Channel item;

        private RecyclerView ListView;
        private SectionAdapter adapter;
        private readonly List<Section> sections = new List<Section>();

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            if (item == null)
            {
                MainActivity.instance.SupportFragmentManager.PopBackStack();
                return;
            }

            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
            MainActivity.instance.DisplayFilter();

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
            if (!MainActivity.instance.Paused)
            {
                Activity.FindViewById(Resource.Id.playlistButtons).Visibility = ViewStates.Visible;
                Activity.FindViewById<RelativeLayout>(Resource.Id.playlistHeader).Visibility = ViewStates.Gone;

                MainActivity.instance.HideFilter();
                MainActivity.instance.SupportActionBar.SetHomeButtonEnabled(false);
                MainActivity.instance.SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                MainActivity.instance.SupportActionBar.SetDisplayShowTitleEnabled(false);
                MainActivity.instance.FindViewById(Resource.Id.toolbarLogo).Visibility = ViewStates.Visible;

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
                    if (response.Items[i].ContentDetails?.Playlists?.Count == 1)
                    {
                        sections.Add(new Section(null, SectionType.SinglePlaylist));
                        LoadPlaylist(sections.Count - 1, response.Items[i].ContentDetails.Playlists[0]);
                    }

                    else if (response.Items[i].ContentDetails?.Playlists?.Count > 1)
                    {
                        sections.Add(new Section(response.Items[i].Snippet.Title, SectionType.PlaylistList));
                        LoadMulitplePlaylists(sections.Count - 1, response.Items[i].ContentDetails.Playlists);
                    }

                    else if (response.Items[i].ContentDetails?.Channels?.Count > 1)
                    {
                        sections.Add(new Section(response.Items[i].Snippet.Title, SectionType.ChannelList));
                        LoadChannels(sections.Count - 1, response.Items[i].ContentDetails.Channels);
                    }
                }

                adapter = new SectionAdapter(sections);
                ListView.SetAdapter(adapter);
            }
            catch (System.Net.Http.HttpRequestException) { System.Console.WriteLine("&Channel section list time out"); }
        }

        async void LoadPlaylist(int slot, string playlistID)
        {
            var pl = await new YoutubeClient().GetPlaylistAsync(playlistID, 1);
            sections[slot] = new Section(pl.Title, SectionType.SinglePlaylist, Song.FromVideoArray(pl.Videos), new PlaylistItem(pl.Title, -1, playlistID) { HasWritePermission = false, Owner = item.Name });
            adapter.NotifyItemChanged(slot);
        }

        async void LoadMulitplePlaylists(int slot, IList<string> playlistIDs)
        {
            List<PlaylistItem> playlists = new List<PlaylistItem>();
            foreach (string playlistID in playlistIDs)
            {
                PlaylistItem playlist = await PlaylistManager.GetPlaylist(playlistID);
                if(playlist != null)
                    playlists.Add(playlist);
            }
            sections[slot] = new Section(sections[slot].SectionTitle, SectionType.PlaylistList, playlists);
            adapter.NotifyItemChanged(slot);
        }

        async void LoadChannels(int slot, IList<string> channelIDs)
        {
            IEnumerable<Channel> channels = await ChannelManager.GetChannels(channelIDs);
            sections[slot] = new Section(sections[slot].SectionTitle, SectionType.ChannelList, channels.ToList());
            adapter.NotifyItemChanged(slot);
        }

        public async void OnRefresh(object sender, System.EventArgs e)
        {
            await PopulateList();
            MainActivity.instance.contentRefresh.Refreshing = false;
        }

        public override void OnResume()
        {
            base.OnResume();
            instance = this;
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
