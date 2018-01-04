using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.View;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Preferences;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using MusicApp.Resources.Fragments;
using MusicApp.Resources.Portable_Class;
using MusicApp.Resources.values;
using Square.Picasso;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Auth;
using SearchView = Android.Support.V7.Widget.SearchView;

namespace MusicApp
{
    [Activity(Label = "MusicApp", MainLauncher = true, Icon = "@drawable/MusicIcon", Theme = "@style/Theme")]
    public class MainActivity : AppCompatActivity, ViewPager.IOnPageChangeListener
    {
        public static MainActivity instance;
        public static int paddingBot = 0;

        public Android.Support.V7.Widget.Toolbar ToolBar;
        public IMenu menu;
        public SwipeRefreshLayout contentRefresh;
        public SwipeRefreshLayout pagerRefresh;
        public bool usePager;
        public View quickPlayLayout;

        private Handler handler = new Handler();
        private ProgressBar bar;
        private bool prepared = false;
        private bool searchDisplayed;
        private bool canSwitch = true;
        private string tab;
        private bool QuickPlayOpenned = false;

        private const int RequestCode = 8539;

        public const string clientID = "758089506779-tstocfigqvjsog2mq5j295b1305igle0.apps.googleusercontent.com";
        public static YouTubeService youtubeService;
        public static OAuth2Authenticator auth;
        public static string refreshToken;

        public event EventHandler<PaddingChange> OnPaddingChanged;

        public virtual void PaddingHasChanged(PaddingChange e)
        {
            paddingBot = (((FrameLayout)instance.FindViewById(Resource.Id.smallPlayer).Parent).Visibility == ViewStates.Gone) 
                ? FindViewById<BottomNavigationView>(Resource.Id.bottomView).Height 
                : FindViewById<BottomNavigationView>(Resource.Id.bottomView).Height + ((FrameLayout)instance.FindViewById(Resource.Id.smallPlayer).Parent).Height;

            OnPaddingChanged?.Invoke(this, e);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Main);

            instance = this;

            var bottomNavigation = FindViewById<BottomNavigationView>(Resource.Id.bottomView);
            bottomNavigation.NavigationItemSelected += PreNavigate;

            ToolBar = (Android.Support.V7.Widget.Toolbar) FindViewById(Resource.Id.toolbar);
            SetSupportActionBar(ToolBar);
            SupportActionBar.Title = "MusicApp";

            pagerRefresh = FindViewById<SwipeRefreshLayout>(Resource.Id.pagerRefresh);
            contentRefresh = FindViewById<SwipeRefreshLayout>(Resource.Id.contentRefresh);

            if (MusicPlayer.queue.Count > 0)
                ReCreateSmallPlayer();
            else
                PrepareApp();

            if (Intent?.Action == "Player")
                ActionPlayer();
        }

        async void PrepareApp()
        {
            await Task.Delay(100);
            paddingBot = FindViewById<BottomNavigationView>(Resource.Id.bottomView).Height;
            Navigate(Resource.Id.musicLayout);
        }

        public void Login()
        {
            AccountStore accountStore = AccountStore.Create();
            Account account = accountStore.FindAccountsForService("Google").FirstOrDefault();
            if (account != null)
            {
                if (!TokenHasExpire(account.Properties["refresh_token"]))
                {
                    refreshToken = account.Properties["refresh_token"];

                    if (YoutubeEngine.youtubeService != null)
                        return;

                    GoogleCredential credential = GoogleCredential.FromAccessToken(account.Properties["access_token"]);
                    YoutubeEngine.youtubeService = new YouTubeService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential
                    });
                }
            }
            else
            {
                auth = new OAuth2Authenticator(
                   clientID,
                   string.Empty,
                   YouTubeService.Scope.Youtube,
                   new Uri("https://accounts.google.com/o/oauth2/v2/auth"),
                   new Uri("com.musicapp.android:/oauth2redirect"),
                   new Uri("https://www.googleapis.com/oauth2/v4/token"),
                   isUsingNativeUI: true);

                auth.Completed += (s, e) =>
                {
                    if (e.IsAuthenticated)
                    {
                        string tokenType = e.Account.Properties["token_type"];
                        string accessToken = e.Account.Properties["access_token"];
                        string refreshToken = e.Account.Properties["refresh_token"];
                        string expiresIN = e.Account.Properties["expires_in"];
                        MainActivity.refreshToken = refreshToken;

                        DateTime expireDate = DateTime.UtcNow.AddSeconds(double.Parse(expiresIN));
                        ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(this);
                        ISharedPreferencesEditor editor = pref.Edit();
                        editor.PutString("expireDate", expireDate.ToString());
                        editor.Apply();

                        GoogleCredential credential = GoogleCredential.FromAccessToken(accessToken);

                        YoutubeEngine.youtubeService = new YouTubeService(new BaseClientService.Initializer()
                        {
                            HttpClientInitializer = credential,
                            ApplicationName = "MusicApp"
                        });

                        AccountStore.Create().Save(e.Account, "Google");
                    }
                    else
                    {
                        Toast.MakeText(this, "Error in authentification.", ToastLength.Short).Show();
                    }
                };

                StartActivity(auth.GetUI(this));
            }
        }

        public bool TokenHasExpire(string refreshToken = null)
        {
            Console.WriteLine("Checking if token has expired with:" + refreshToken);
            if (refreshToken == null)
                refreshToken = MainActivity.refreshToken;

            ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(this);
            string expireDate = pref.GetString("expireDate", null);
            if (expireDate != null)
            {
                Console.WriteLine("expiredatae: " + expireDate);
                DateTime expiresDate = DateTime.Parse(expireDate);

                if (expiresDate > DateTime.UtcNow)
                {
                    Console.WriteLine("token hasn't expired");
                    return false;
                }
                else
                {
                    RequestNewToken(refreshToken);
                    return true;
                }
            }
            return true;
        }

        public void RequestNewToken(string refreshToken)
        {
            Console.WriteLine("Token has expire getting a new one");

            IAuthorizationCodeFlow flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer { ClientSecrets = new ClientSecrets() { ClientId = clientID } });
            Console.WriteLine("Flow created");
            TokenResponse token = new TokenResponse { RefreshToken = refreshToken };
            Console.WriteLine("Token created");

            UserCredential credential = new UserCredential(flow, "user", token);
            Console.WriteLine("credential created");
            YoutubeEngine.youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "MusicApp"
            });
        }

        public void Scroll(object sender, AbsListView.ScrollEventArgs e)
        {
            if (usePager)
                pagerRefresh.SetEnabled(e.FirstVisibleItem == 0);
            else
                contentRefresh.SetEnabled(e.FirstVisibleItem == 0);
        }

        private async void ActionPlayer()
        {
            await Task.Delay(100);
            HideTabs();
            HideSearch();
            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, Player.NewInstance()).Commit();
        }

        private async void ReCreateSmallPlayer()
        {
            await Task.Delay(100);
            PrepareSmallPlayer();
            ShowSmallPlayer();
            Navigate(Resource.Id.musicLayout);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.toolbar_menu, menu);
            this.menu = menu;
            var item = menu.FindItem(Resource.Id.search);
            item.SetVisible(false);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if(item.ItemId == Android.Resource.Id.Home)
            {
                if (PlaylistTracks.instance != null)
                {
                    HideSearch();
                    if (PlaylistTracks.instance.isEmpty)
                    {
                        ViewGroup rootView = FindViewById<ViewGroup>(Android.Resource.Id.Content);
                        rootView.RemoveView(PlaylistTracks.instance.emptyView);
                    }
                    SupportActionBar.SetHomeButtonEnabled(false);
                    SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                    SupportActionBar.Title = "MusicApp";
                    SetYtTabs(PlaylistTracks.instance.ytID == "" ? 0 : 1);
                    PlaylistTracks.instance = null;
                }
                if (FolderTracks.instance != null)
                {
                    HideSearch();
                    if (FolderTracks.instance.isEmpty)
                    {
                        ViewGroup rootView = FindViewById<ViewGroup>(Android.Resource.Id.Content);
                        rootView.RemoveView(PlaylistTracks.instance.emptyView);
                    }
                    SupportActionBar.SetHomeButtonEnabled(false);
                    SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                    SupportActionBar.Title = "MusicApp";
                    FolderTracks.instance = null;
                    SetBrowseTabs(1);
                }
            }
            else if(item.ItemId == Resource.Id.search)
            {
                var searchItem = MenuItemCompat.GetActionView(item);
                var searchView = searchItem.JavaCast<SearchView>();

                searchView.QueryTextChange += Search;

                searchView.QueryTextSubmit += (s, e) =>
                {
                    if (YoutubeEngine.instance != null)
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        YoutubeEngine.instance.Search(e.Query);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                    e.Handled = true;
                };

                searchView.Close += SearchClose;
            }
            else if(item.ItemId == Resource.Id.settings)
            {
                Intent intent = new Intent(Application.Context, typeof(Preferences));
                StartActivity(intent);
            }
            return base.OnOptionsItemSelected(item);
        }

        void Search(object sender, SearchView.QueryTextChangeEventArgs e)
        {
            if (Browse.instance != null)
                Browse.instance.Search(e.NewText);
            if (PlaylistTracks.instance != null)
                PlaylistTracks.instance.Search(e.NewText);
            if (PlaylistTracks.instance != null)
                PlaylistTracks.instance.Search(e.NewText);
            if (FolderTracks.instance != null)
                FolderTracks.instance.Search(e.NewText);
        }

        void SearchClose(object sender, SearchView.CloseEventArgs e)
        {
            if (Browse.instance != null)
                Browse.instance.result = null;
            if (PlaylistTracks.instance != null)
                PlaylistTracks.instance.result = null;
            if (FolderTracks.instance != null)
                FolderTracks.instance.result = null;
            //if (YoutubeEngine.instance != null)
            //    YoutubeEngine.result = null;
        }

        public void HideSearch()
        {
            if (!searchDisplayed)
                return;

            searchDisplayed = false;

            if (menu == null)
                return;

            var item = menu.FindItem(Resource.Id.search);
            var searchItem = MenuItemCompat.GetActionView(item);
            var searchView = searchItem.JavaCast<SearchView>();

            searchView.SetQuery("", false);
            searchView.ClearFocus();
            searchView.OnActionViewCollapsed();

            item.SetVisible(false);
            item.CollapseActionView();

            SupportActionBar.SetHomeButtonEnabled(false);
            SupportActionBar.SetDisplayHomeAsUpEnabled(false);
            SupportActionBar.Title = "MusicApp";
        }

        public void DisplaySearch(int id = 0)
        {
            if (searchDisplayed)
                return;

            searchDisplayed = true;

            var item = menu.FindItem(Resource.Id.search);
            item.SetVisible(true);
            item.CollapseActionView();
            var searchItem = MenuItemCompat.GetActionView(item);
            var searchView = searchItem.JavaCast<SearchView>();

            searchView.SetQuery("", false);
            searchView.ClearFocus();
            searchView.OnActionViewCollapsed();

            if (id != 0)
                return;

            SupportActionBar.SetHomeButtonEnabled(false);
            SupportActionBar.SetDisplayHomeAsUpEnabled(false);
            SupportActionBar.Title = "MusicApp";
        }

        private void PreNavigate(object sender, BottomNavigationView.NavigationItemSelectedEventArgs e)
        {
            Navigate(e.Item.ItemId);
        }

        private void Navigate(int layout)
        {
            Android.Support.V4.App.Fragment fragment = null;
            switch (layout)
            {
                case Resource.Id.musicLayout:
                    if (Queue.instance != null)
                    {
                        Queue.instance.Refresh();
                        return;
                    }

                    tab = "Queue";
                    HideTabs();
                    HideSearch();
                    ShowQuickPlay();
                    fragment = Queue.NewInstance();
                    break;

                case Resource.Id.browseLayout:
                    if (Browse.instance != null)
                    {
                        Browse.instance.Refresh();
                        return;
                    }

                    tab = "Browse";
                    SetBrowseTabs();
                    DisplaySearch();
                    HideQuickPlay();
                    break;

                case Resource.Id.downloadLayout:
                    if (YoutubeEngine.instance != null)
                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        YoutubeEngine.instance.Refresh();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        return;
                    }

                    tab = "Youtube";
                    HideTabs();
                    DisplaySearch();
                    HideQuickPlay();
                    fragment = YoutubeEngine.NewInstance();
                    break;

                case Resource.Id.playlistLayout:
                    if (Playlist.instance != null)
                    {
                        Playlist.instance.Refresh();
                        return;
                    }

                    tab = "Playlist";
                    SetYtTabs();
                    HideSearch();
                    HideQuickPlay();
                    break;
            }

            if (fragment == null)
                fragment = EmptyFragment.NewInstance();

            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, fragment).Commit();
        }

        async void SetBrowseTabs(int selectedTab = 0)
        {
            if (Browse.instance != null)
                return;
            while (!canSwitch)
                await Task.Delay(10);

            if (tab != "Browse" || Browse.instance != null)
                return;

            Console.WriteLine("Switching: " + canSwitch);
            canSwitch = false;
            usePager = true;

            TabLayout tabs = FindViewById<TabLayout>(Resource.Id.tabs);
            ViewPager pager = FindViewById<ViewPager>(Resource.Id.pager);

            if (Playlist.instance != null)
            {
                ViewPagerAdapter oldAdapter = (ViewPagerAdapter)pager.Adapter;
                SupportFragmentManager.BeginTransaction().Remove(oldAdapter.GetItem(0)).Commit();
                SupportFragmentManager.BeginTransaction().Remove(oldAdapter.GetItem(1)).Commit();

                oldAdapter.Clear();

                tabs.AddTab(tabs.NewTab().SetText("Songs"));
                tabs.AddTab(tabs.NewTab().SetText("Folders"));

                oldAdapter.AddFragment(Browse.NewInstance(), "Songs");
                oldAdapter.AddFragment(FolderBrowse.NewInstance(), "Folders");

                pager.Adapter = oldAdapter;
            }
            else
            {
                contentRefresh.Visibility = ViewStates.Gone;
                pagerRefresh.Visibility = ViewStates.Visible;
                tabs.Visibility = ViewStates.Visible;
                tabs.AddTab(tabs.NewTab().SetText("Songs"));
                tabs.AddTab(tabs.NewTab().SetText("Folders"));

                ViewPagerAdapter adapter = new ViewPagerAdapter(SupportFragmentManager);
                adapter.AddFragment(Browse.NewInstance(), "Songs");
                adapter.AddFragment(FolderBrowse.NewInstance(), "Folders");

                pager.Adapter = adapter;
                pager.AddOnPageChangeListener(this);
                pager.AddOnPageChangeListener(new TabLayout.TabLayoutOnPageChangeListener(tabs));

                tabs.SetupWithViewPager(pager);
                tabs.TabReselected += OnTabReselected;
            }

            pager.CurrentItem = selectedTab;
            tabs.SetScrollPosition(selectedTab, 0f, true);

            CanSwitchDelay();
        }

        async void SetYtTabs(int selectedTab = 0)
        {
            if (Playlist.instance != null)
                return;

            while (!canSwitch)
                await Task.Delay(10);

            if (tab != "Playlist" || Playlist.instance != null)
                return;

            Console.WriteLine("Switching: " + canSwitch);
            canSwitch = false;
            usePager = true;

            TabLayout tabs = FindViewById<TabLayout>(Resource.Id.tabs);
            ViewPager pager = FindViewById<ViewPager>(Resource.Id.pager);

            if (Browse.instance != null)
            {
                ViewPagerAdapter oldAdapter = (ViewPagerAdapter)pager.Adapter;
                SupportFragmentManager.BeginTransaction().Remove(oldAdapter.GetItem(0)).Commit();
                SupportFragmentManager.BeginTransaction().Remove(oldAdapter.GetItem(1)).Commit();

                oldAdapter.Clear();

                tabs.AddTab(tabs.NewTab().SetText("Songs"));
                tabs.AddTab(tabs.NewTab().SetText("Folders"));

                oldAdapter.AddFragment(Playlist.NewInstance(), "Playlists");
                oldAdapter.AddFragment(YtPlaylist.NewInstance(), "Youtube playlists");

                pager.Adapter = oldAdapter;
            }
            else
            {
                contentRefresh.Visibility = ViewStates.Gone;
                pagerRefresh.Visibility = ViewStates.Visible;
                tabs.Visibility = ViewStates.Visible;
                tabs.AddTab(tabs.NewTab().SetText("Playlists"));
                tabs.AddTab(tabs.NewTab().SetText("Youtube playlists"));

                ViewPagerAdapter adapter = new ViewPagerAdapter(SupportFragmentManager);
                adapter.AddFragment(Playlist.NewInstance(), "Playlists");
                adapter.AddFragment(YtPlaylist.NewInstance(), "Youtube playlists");

                pager.Adapter = adapter;
                pager.AddOnPageChangeListener(this);
                pager.AddOnPageChangeListener(new TabLayout.TabLayoutOnPageChangeListener(tabs));
                tabs.SetupWithViewPager(pager);
                tabs.TabReselected += OnTabReselected;
            }

            pager.CurrentItem = selectedTab;
            tabs.SetScrollPosition(selectedTab, 0f, true);

            CanSwitchDelay();
        }

        private void OnTabReselected(object sender, TabLayout.TabReselectedEventArgs e)
        {
            if (Browse.instance != null)
            {
                if (Browse.instance.focused)
                    Browse.instance.ListView.SmoothScrollToPosition(0);
                else
                    FolderBrowse.instance.ListView.SmoothScrollToPosition(0);
            }
            else if (Playlist.instance != null)
            {
                if (Playlist.instance.focused)
                    Playlist.instance.ListView.SmoothScrollToPosition(0);
                else
                    YtPlaylist.instance.ListView.SmoothScrollToPosition(0);
            }
        }

        async void CanSwitchDelay()
        {
            await Task.Delay(350);
            canSwitch = true;
        }

        public void OnPageScrollStateChanged(int state)
        {
            pagerRefresh.SetEnabled( state == ViewPager.ScrollStateIdle );
        }

        public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels) { }

        public void OnPageSelected(int position)
        {
            if (Playlist.instance != null)
            {
                if (position == 0)
                {
                    if (Playlist.instance.isEmpty)
                        Playlist.instance.AddEmptyView();
                    if (YtPlaylist.instance.isEmpty)
                        YtPlaylist.instance.RemoveEmptyView();

                    Playlist.instance.focused = true;
                    YtPlaylist.instance.focused = false;

                    Playlist.instance.ListView.SmoothScrollToPosition(0);
                }
                if (position == 1)
                {
                    if (Playlist.instance.isEmpty)
                        Playlist.instance.RemoveEmptyView();
                    if (YtPlaylist.instance.isEmpty)
                        YtPlaylist.instance.AddEmptyView();

                    Playlist.instance.focused = false;
                    YtPlaylist.instance.focused = true;

                    YtPlaylist.instance.ListView.SmoothScrollToPosition(0);
                }
            }

            if (Browse.instance != null)
            {
                if(!FolderBrowse.instance.populated)
                    FolderBrowse.instance.PopulateList();

                if(position == 0)
                {
                    Browse.instance.focused = true;
                    FolderBrowse.instance.focused = false;
                    DisplaySearch();
                    Browse.instance.ListView.SmoothScrollToPosition(0);
                }
                if(position == 1)
                {
                    Browse.instance.focused = false;
                    FolderBrowse.instance.focused = true;
                    HideSearch();
                    Browse.instance.ListView.SmoothScrollToPosition(0);
                }
            }
        }

        public void HideTabs()
        {
            usePager = false;
            TabLayout tabs = FindViewById<TabLayout>(Resource.Id.tabs);
            tabs.RemoveAllTabs();
            tabs.Visibility = ViewStates.Gone;
            ViewPager pager = FindViewById<ViewPager>(Resource.Id.pager);

            ViewPagerAdapter adapter = (ViewPagerAdapter) pager.Adapter;
            if (adapter != null)
            {
                for(int i = 0; i < adapter.Count; i++)
                    SupportFragmentManager.BeginTransaction().Remove(adapter.GetItem(i)).Commit();

                adapter.Dispose();
                pager.Adapter = null;
            }

            pagerRefresh.Visibility = ViewStates.Gone;
            contentRefresh.Visibility = ViewStates.Visible;
        }

        public void PrepareSmallPlayer()
        {
            Song current = MusicPlayer.queue[MusicPlayer.CurrentID()];

            RelativeLayout smallPlayer = FindViewById<RelativeLayout>(Resource.Id.smallPlayer);
            smallPlayer.FindViewById<TextView>(Resource.Id.spTitle).Text = current.GetName();
            smallPlayer.FindViewById<TextView>(Resource.Id.spArtist).Text = current.GetArtist();
            ImageView art = smallPlayer.FindViewById<ImageView>(Resource.Id.spArt);

            if (current.IsYt)
            {
                Picasso.With(Application.Context).Load(current.GetAlbum()).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(art);
            }
            else
            {
                var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                var songAlbumArtUri = ContentUris.WithAppendedId(songCover, current.GetAlbumArt());

                Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(art);
            }

            SetSmallPlayerProgressBar();

            if (MusicPlayer.isRunning)
                smallPlayer.FindViewById<ImageButton>(Resource.Id.spPlay).SetImageResource(Resource.Drawable.ic_pause_black_24dp);
            else
                smallPlayer.FindViewById<ImageButton>(Resource.Id.spPlay).SetImageResource(Resource.Drawable.ic_play_arrow_black_24dp);

            if (!prepared)
            {
                smallPlayer.FindViewById<ImageButton>(Resource.Id.spLast).Click += Last_Click;
                smallPlayer.FindViewById<ImageButton>(Resource.Id.spPlay).Click += Play_Click;
                smallPlayer.FindViewById<ImageButton>(Resource.Id.spNext).Click += Next_Click;

                smallPlayer.FindViewById<LinearLayout>(Resource.Id.spContainer).Click += Container_Click;
                prepared = true;
            }
        }

        private void Last_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(Application.Context, typeof(MusicPlayer));
            intent.SetAction("Previus");
            StartService(intent);
        }

        private void Play_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(Application.Context, typeof(MusicPlayer));
            intent.SetAction("Pause");
            StartService(intent);
        }

        private void Next_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(Application.Context, typeof(MusicPlayer));
            intent.SetAction("Next");
            StartService(intent);
        }

        private void Container_Click(object sender, EventArgs e)
        {
            HideTabs();
            HideSearch();
            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, Player.NewInstance()).Commit(); 
        }

        public void GetStoragePermission()
        {
            const string permission = Manifest.Permission.ReadExternalStorage;
            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(this, permission) == (int)Permission.Granted)
            {
                PremissionAuthorized();
                return;
            }
            string[] permissions = new string[] { permission };
            RequestPermissions(permissions, RequestCode);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == RequestCode)
            {
                if (grantResults.Length > 0)
                {
                    if (grantResults[0] == Permission.Granted)
                        PremissionAuthorized();
                    else
                        Snackbar.Make(FindViewById<View>(Resource.Id.contentView), "Permission denied, can't list musics.", Snackbar.LengthShort).Show();
                }
            }
        }

        void PremissionAuthorized()
        {
            Browse.instance?.PopulateList();
            Playlist.instance?.PopulateView();
        }

        public void Transition(int Resource, Android.Support.V4.App.Fragment fragment, bool backStack)
        {
            if (backStack)
                SupportFragmentManager.BeginTransaction().Replace(Resource, fragment).AddToBackStack(null).Commit();
            else
                SupportFragmentManager.BeginTransaction().Replace(Resource, fragment).Commit();
        }

        void SetSmallPlayerProgressBar()
        {
            bar = FindViewById<ProgressBar>(Resource.Id.spProgress);
            bar.Max = MusicPlayer.Duration;
            bar.Progress = MusicPlayer.CurrentPosition;
            handler.PostDelayed(UpdateProgressBar, 1000);
        }

        private void UpdateProgressBar()
        {
            if (!MusicPlayer.isRunning)
            {
                handler.RemoveCallbacks(UpdateProgressBar);
                return;
            }

            bar.Progress = MusicPlayer.CurrentPosition;
            handler.PostDelayed(UpdateProgressBar, 1000);
        }

        public async void HideSmallPlayer()
        {
            RelativeLayout smallPlayer = FindViewById<RelativeLayout>(Resource.Id.smallPlayer);
            FrameLayout parent = (FrameLayout)smallPlayer.Parent;
            bool hasChanged = parent.Visibility != ViewStates.Gone;
            parent.Visibility = ViewStates.Gone;

            await Task.Delay(75);
            if (hasChanged)
                PaddingHasChanged(new PaddingChange(paddingBot));
        }

        public async void ShowSmallPlayer()
        {
            RelativeLayout smallPlayer = FindViewById<RelativeLayout>(Resource.Id.smallPlayer);
            FrameLayout parent = (FrameLayout)smallPlayer.Parent;
            bool hasChanged = parent.Visibility == ViewStates.Gone;
            parent.Visibility = ViewStates.Visible;
            smallPlayer.Visibility = ViewStates.Visible;

            await Task.Delay(75);
            if (hasChanged)
                PaddingHasChanged(new PaddingChange(paddingBot));
        }

        public void ShowQuickPlay()
        {
            if(quickPlayLayout != null)
            {
                quickPlayLayout.Visibility = ViewStates.Visible;
                FloatingActionButton fab = quickPlayLayout.FindViewById<FloatingActionButton>(Resource.Id.quickPlay);
                fab.Animate().Alpha(1);
                return;
            }
            quickPlayLayout = LayoutInflater.Inflate(Resource.Layout.QuickPlayLayout, null);
            ViewGroup rootView = FindViewById<ViewGroup>(Android.Resource.Id.Content);
            AddContentView(quickPlayLayout, rootView.LayoutParameters);
            Console.WriteLine("&Starting padding bot: " + paddingBot);
            quickPlayLayout.SetPadding(0, 0, 0, paddingBot + PxToDp(6));
            FloatingActionButton QuickPlayFAB = quickPlayLayout.FindViewById<FloatingActionButton>(Resource.Id.quickPlay);
            QuickPlayFAB.Click += QuickPlay;
            OnPaddingChanged += QuickPlayChangePosition;
        }

        public void HideQuickPlay()
        {
            FloatingActionButton QuickPlayFAB = quickPlayLayout.FindViewById<FloatingActionButton>(Resource.Id.quickPlay);
            QuickPlayFAB.Animate().Alpha(0);
        }

        private void QuickPlayChangePosition(object sender, PaddingChange e)
        {
            Console.WriteLine("&Padding bot: " + paddingBot);
            FloatingActionButton QuickPlayFab = quickPlayLayout.FindViewById<FloatingActionButton>(Resource.Id.quickPlay);
            QuickPlayFab.Animate().TranslationYBy(-(paddingBot - e.oldPadding));
        }

        public async void QuickPlay(object sender, EventArgs e)
        {
            FloatingActionButton quickPlay = FindViewById<FloatingActionButton>(Resource.Id.quickPlay);
            if (QuickPlayOpenned)
            {
                Drawable icon = quickPlay.Drawable;
                ((AnimatedVectorDrawable)icon).Start();
                QuickPlayOpenned = false;
                await Task.Delay(200);
                quickPlay.SetImageResource(Resource.Drawable.PlayToCross);
            }
            else
            {
                Drawable icon = quickPlay.Drawable;
                ((AnimatedVectorDrawable)icon).Start();
                QuickPlayOpenned = true;
                await Task.Delay(200);
                quickPlay.SetImageResource(Resource.Drawable.CrossToPlay);
            }
        }

        int PxToDp(int px)
        {
            float scale = Resources.DisplayMetrics.Density;
            return (int) (px * scale + 0.5f);
        }
    }
}