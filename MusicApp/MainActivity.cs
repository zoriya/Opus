using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Database;
using Android.Gms.Auth.Api;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Preferences;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using MusicApp.Resources.Fragments;
using MusicApp.Resources.Portable_Class;
using MusicApp.Resources.values;
using Newtonsoft.Json.Linq;
using Square.OkHttp;
using Square.Picasso;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TagLib;
using YoutubeExplode;
using YoutubeExplode.Models;
using ICallback = Square.OkHttp.ICallback;
using Playlist = MusicApp.Resources.Portable_Class.Playlist;
using Request = Square.OkHttp.Request;
using SearchView = Android.Support.V7.Widget.SearchView;

namespace MusicApp
{
    [Activity(Label = "MusicApp", MainLauncher = true, Icon = "@drawable/launcher_icon", Theme = "@style/SplashScreen", ScreenOrientation = ScreenOrientation.Portrait, LaunchMode = LaunchMode.SingleTask)]
    [IntentFilter(new[] {Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataHost = "www.youtube.com", DataMimeType = "text/*")]
    [IntentFilter(new[] {Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataHost = "m.youtube.com", DataMimeType = "text/plain")]
    [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault }, DataMimeTypes = new[] { "audio/*", "application/ogg", "application/x-ogg", "application/itunes" })]
    public class MainActivity : AppCompatActivity, ViewPager.IOnPageChangeListener, GoogleApiClient.IOnConnectionFailedListener, ICallback, IResultCallback, IMenuItemOnActionExpandListener, View.IOnFocusChangeListener
    {
        public static MainActivity instance;
        public new static int Theme = 1;
        public static int dialogTheme;
        public static IParcelable parcelable;
        public static string parcelableSender;

        public Android.Support.V7.Widget.Toolbar ToolBar;
        public bool NoToolbarMenu = false;
        public IMenu menu;
        public ViewPager viewPager;
        public SwipeRefreshLayout contentRefresh;
        public bool usePager;
        public bool HomeDetails = false;
        public bool paused = false;
        public bool StateSaved = false;

        private bool prepared = false;
        private bool searchDisplayed;
        private bool canSwitch = true;
        private string tab;
        private bool QuickPlayOpenned = false;
        private object sender;
        private Drawable playToCross;
        private Drawable crossToPlay;
        public BottomSheetBehavior SheetBehavior;

        private const int RequestCode = 8539;
        private const string versionURI = "https://raw.githubusercontent.com/AnonymusRaccoon/MusicApp/master/MusicApp/Assets/Version.txt";

        private const string clientID = "112086459272-8m4do6aehtdg4a7nffd0a84jk94c64e8.apps.googleusercontent.com";
        public static GoogleSignInAccount account;
        public GoogleApiClient googleClient;
        private bool canAsk;
        public bool waitingForYoutube;
        public bool ResumeKiller;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(this);
            SwitchTheme(pref.GetInt("theme", 0));
            SetContentView(Resource.Layout.Main);
            instance = this;

            var bottomNavigation = FindViewById<BottomNavigationView>(Resource.Id.bottomView);
            bottomNavigation.NavigationItemSelected += PreNavigate;

            ToolBar = (Android.Support.V7.Widget.Toolbar)FindViewById(Resource.Id.toolbar);
            SetSupportActionBar(ToolBar);
            SupportActionBar.Title = "MusicApp";
            ((CoordinatorLayout.LayoutParams)FindViewById(Resource.Id.contentLayout).LayoutParameters).TopMargin = -Resources.GetDimensionPixelSize(Resources.GetIdentifier("status_bar_height", "dimen", "android"));

            contentRefresh = FindViewById<SwipeRefreshLayout>(Resource.Id.contentRefresh);

            playToCross = GetDrawable(Resource.Drawable.PlayToCross);
            crossToPlay = GetDrawable(Resource.Drawable.CrossToPlay);
            if (MusicPlayer.queue == null || MusicPlayer.queue.Count == 0)
                MusicPlayer.RetrieveQueueFromDataBase();

            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.playerFrame, Player.instance ?? new Player()).Commit();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                NotificationManager notificationManager = (NotificationManager)GetSystemService(NotificationService);
                NotificationChannel channel = new NotificationChannel("MusicApp.Channel", "Default Channel", NotificationImportance.Low)
                {
                    Description = "Channel used for download progress and music control notification.",
                    LockscreenVisibility = NotificationVisibility.Public
                };
                channel.EnableVibration(false);
                channel.EnableLights(false);
                notificationManager.CreateNotificationChannel(channel);
            }

            CheckForUpdate(this, false);
            OnLateCreate(Intent);
            Login();
        }

        private async void OnLateCreate(Intent intent, bool lateSetup = true)
        {
            if (lateSetup)
            {
                await Task.Delay(500);
                FindViewById(Resource.Id.playerFrame).LayoutParameters.Height = FindViewById(Resource.Id.playerSheet).Height;
                FindViewById(Resource.Id.playerSheet).TranslationY = -DpToPx(56);
                SheetBehavior = BottomSheetBehavior.From(FindViewById(Resource.Id.playerSheet));
                SheetBehavior.State = BottomSheetBehavior.StateCollapsed;
                SheetBehavior.Hideable = true;
                SheetBehavior.SetBottomSheetCallback(new PlayerCallback(this));
                SheetBehavior.PeekHeight = DpToPx(70);

                if (MusicPlayer.queue.Count > 0)
                    ReCreateSmallPlayer();
                else 
                {
                    if (intent.Action != "Sleep" && intent.Action != "Player" && intent.Action != Intent.ActionView && Intent.Action != Intent.ActionSend)
                        HideSmallPlayer();
                    Navigate(Resource.Id.musicLayout);
                }

                FindViewById(Resource.Id.contentRefresh).Invalidate();
                FindViewById(Resource.Id.contentView).RequestLayout();
                FindViewById(Resource.Id.playerSheet).Visibility = ViewStates.Visible;
            }

            if (intent.Action == "Sleep")
            {
                ShowPlayer();
                Player.instance.SleepButton_Click("", null);
            }
            else if (intent.Action == "Player")
            {
                ShowPlayer();
                Player.instance.RefreshPlayer();
            }
            else if (intent.Action == Intent.ActionView && intent.Data != null)
            {
                Intent inte = new Intent(this, typeof(MusicPlayer));
                inte.PutExtra("file", intent.Data.ToString());
                StartService(inte);

                ShowSmallPlayer();
                ShowPlayer();
            }
            else if (intent.Action == Intent.ActionSend)
            {
                if (YoutubeClient.TryParseVideoId(intent.GetStringExtra(Intent.ExtraText), out string videoID))
                {
                    Intent inten = new Intent(this, typeof(MusicPlayer));
                    inten.SetAction("YoutubePlay");
                    inten.PutExtra("action", "Play");
                    inten.PutExtra("file", videoID);
                    StartService(inten);
                }
                else
                {
                    Toast.MakeText(this, "Can't play non youtube video.", ToastLength.Short).Show();
                    Finish();
                }
            }
        }

        public void SwitchTheme(int themeID)
        {
            Theme = themeID;
            if (themeID == 0)
            {
                dialogTheme = Resource.Style.AppCompatAlertDialogStyle;
                SetTheme(Resource.Style.Theme);
            }
            else
            {
                SetTheme(Resource.Style.DarkTheme);
                dialogTheme = Resource.Style.AppCompatDarkAlertDialogStyle;
            }
        }


        public void Login(bool canAsk = true, bool skipSilentLog = false)
        {
            if(account != null)
            {
                CreateYoutube();
                return;
            }

            Console.WriteLine("&Loggin");

            if(googleClient == null)
            {
                GoogleSignInOptions gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                    .RequestIdToken("112086459272-59scolco82ho7d6hcieq8kmdjai2i2qd.apps.googleusercontent.com")
                    .RequestServerAuthCode("112086459272-59scolco82ho7d6hcieq8kmdjai2i2qd.apps.googleusercontent.com")
                    .RequestEmail()
                    .RequestScopes(new Scope(YouTubeService.Scope.Youtube))
                    .Build();

                googleClient = new GoogleApiClient.Builder(this)
                    .AddApi(Auth.GOOGLE_SIGN_IN_API, gso)
                    .Build();

                googleClient.Connect();
            }

            if (!skipSilentLog)
            {
                OptionalPendingResult silentLog = Auth.GoogleSignInApi.SilentSignIn(googleClient);
                if (silentLog.IsDone)
                {
                    GoogleSignInResult result = (GoogleSignInResult)silentLog.Get();
                    if (result.IsSuccess)
                    {
                        account = result.SignInAccount;
                        RunOnUiThread(() => { Picasso.With(this).Load(account.PhotoUrl).Transform(new CircleTransformation()).Into(new AccountTarget()); });
                        CreateYoutube();
                    }
                }
                else if (silentLog != null)
                {
                    this.canAsk = canAsk;
                    silentLog.SetResultCallback(this);
                }
                else if (canAsk)
                {
                    ResumeKiller = true;
                    StartActivityForResult(Auth.GoogleSignInApi.GetSignInIntent(googleClient), 1598);
                }

                return;
            }
            if (canAsk)
            {
                ResumeKiller = true;
                StartActivityForResult(Auth.GoogleSignInApi.GetSignInIntent(googleClient), 1598);
            }
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if(requestCode == 1598)
            {
                GoogleSignInResult result = Auth.GoogleSignInApi.GetSignInResultFromIntent(data);
                if (result.IsSuccess)
                {
                    account = result.SignInAccount;
                    RunOnUiThread(() => { Picasso.With(this).Load(account.PhotoUrl).Transform(new CircleTransformation()).Into(new AccountTarget()); });
                    CreateYoutube();
                }
                else
                {
                    waitingForYoutube = false;
                }
            }
        }

        public void OnResult(Java.Lang.Object result) //Silent log result
        {
            account = ((GoogleSignInResult)result).SignInAccount;
            if(account != null)
            {
                RunOnUiThread(() => { Picasso.With(this).Load(account.PhotoUrl).Transform(new CircleTransformation()).Into(new AccountTarget()); });
                CreateYoutube();
            }
            else if(canAsk)
            {
                ResumeKiller = true;
                StartActivityForResult(Auth.GoogleSignInApi.GetSignInIntent(googleClient), 1598);
            }
        }

        void CreateYoutube()
        {
            OkHttpClient client = new OkHttpClient();
            RequestBody body = new FormEncodingBuilder()
                .Add("grant_type", "authorization_code")
                .Add("client_id", "112086459272-59scolco82ho7d6hcieq8kmdjai2i2qd.apps.googleusercontent.com")
                .Add("client_secret", "Q8vVJRc5Cofeuj1-BxAg5qta")
                .Add("redirect_uri", "")
                .Add("code", account.ServerAuthCode)
                .Add("id_token", account.IdToken)
                .Build();
            Request request = new Square.OkHttp.Request.Builder()
                .Url("https://www.googleapis.com/oauth2/v4/token")
                .Post(body)
                .Build();
            client.NewCall(request).Enqueue(this);
            
            Console.WriteLine("&Requesting access token");
        }
        
        public void OnResponse(Square.OkHttp.Response response)
        {
            string jsonFile = response.Body().String();
            Console.WriteLine(jsonFile);
            if (jsonFile.Contains("error"))
                return;

            JToken json = JObject.Parse(jsonFile);
            GoogleCredential credential = GoogleCredential.FromAccessToken((string)json.SelectToken("access_token"));
            YoutubeEngine.youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "MusicApp"
            });

            Console.WriteLine("&Youtube created");
        }

        public void OnFailure(Request request, Java.IO.IOException iOException)
        {
            Console.WriteLine("&Failure");
        }

        public void OnConnectionFailed(ConnectionResult result)
        {
            Console.WriteLine("&Connection Failed: " + result.ErrorMessage);
        }

        public async Task<bool> WaitForYoutube()
        {
            if(YoutubeEngine.youtubeService == null)
            {
                if(!waitingForYoutube)
                    Login(true);

                waitingForYoutube = true;

                while (YoutubeEngine.youtubeService == null)
                {
                    if (waitingForYoutube == false)
                        return false;

                    await Task.Delay(10);
                }
            }
            waitingForYoutube = false;
            return true;
        }

        public void Scroll(object sender, AbsListView.ScrollEventArgs e)
        {
            contentRefresh.SetEnabled(e.FirstVisibleItem == 0);

            if(PlaylistTracks.instance != null)
            {
                if (e.FirstVisibleItem + e.VisibleItemCount == e.TotalItemCount)
                    PlaylistTracks.instance.lastVisible = true;
                else
                    PlaylistTracks.instance.lastVisible = false;
            }
        }

        public void Scroll(object sender, View.ScrollChangeEventArgs e)
        {
            if(Home.instance != null)
            {
                contentRefresh.SetEnabled(((LinearLayoutManager)Home.instance.ListView.GetLayoutManager()).FindFirstCompletelyVisibleItemPosition() == 0);
            }
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
            if (NoToolbarMenu)
            {
                menu = null;
                return base.OnCreateOptionsMenu(menu);
            }

            MenuInflater.Inflate(Resource.Menu.toolbar_menu, menu);
            this.menu = menu;

            var item = menu.FindItem(Resource.Id.filter);
            var filterView = item.ActionView.JavaCast<SearchView>();
            filterView.QueryTextChange += Search;
            item.SetVisible(false);
            menu.FindItem(Resource.Id.search).SetOnActionExpandListener(this);
            ((SearchView)menu.FindItem(Resource.Id.search).ActionView).SetOnQueryTextFocusChangeListener(this);
            ((SearchView)menu.FindItem(Resource.Id.search).ActionView).QueryHint = "Search Youtube";
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if(item.ItemId == Android.Resource.Id.Home)
            {
                if (PlaylistTracks.instance != null)
                {
                    SupportFragmentManager.BeginTransaction().Remove(PlaylistTracks.instance).Commit();
                }
                else if (YoutubeEngine.instances != null)
                {
                    YoutubeEngine.error = false;
                    ViewGroup rootView = FindViewById<ViewGroup>(Android.Resource.Id.Content);
                    foreach (YoutubeEngine instance in YoutubeEngine.instances)
                    {
                        rootView.RemoveView(instance.emptyView);
                    }
                    rootView.RemoveView(YoutubeEngine.loadingView);

                    var searchView = menu.FindItem(Resource.Id.search).ActionView.JavaCast<SearchView>();
                    menu.FindItem(Resource.Id.search).CollapseActionView();
                    searchView.ClearFocus();
                    searchView.Iconified = true;
                    searchView.SetQuery("", false);
                    SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                    YoutubeEngine.instances = null;
                }
                else if (FolderTracks.instance != null)
                {
                    HideSearch();
                    SupportActionBar.SetHomeButtonEnabled(false);
                    SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                    SupportActionBar.Title = "MusicApp";
                    FolderTracks.instance = null;
                    SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, Pager.NewInstance(0, 1)).Commit();
                }
            }
            else if(item.ItemId == Resource.Id.settings)
            {
                Intent intent = new Intent(Application.Context, typeof(Preferences));
                StartActivity(intent);
            }
            return base.OnOptionsItemSelected(item);
        }

        public bool OnMenuItemActionCollapse(IMenuItem item) //Youtube search collapse
        {
            if (YoutubeEngine.instances != null && !PlaylistTracks.openned)
            {
                ViewGroup rootView = FindViewById<ViewGroup>(Android.Resource.Id.Content);
                foreach (YoutubeEngine instance in YoutubeEngine.instances)
                {
                    rootView.RemoveView(instance.emptyView);
                }
                rootView.RemoveView(YoutubeEngine.loadingView);
                YoutubeEngine.instances = null;
                SupportFragmentManager.PopBackStack();
            }
            return true;
        }

        public bool OnMenuItemActionExpand(IMenuItem item)
        {
            return true;
        }

        void Search(object sender, SearchView.QueryTextChangeEventArgs e)
        {
            if (Browse.instance != null)
                Browse.instance.Search(e.NewText);
            if (PlaylistTracks.instance != null)
                PlaylistTracks.instance.Search(e.NewText);
            if (FolderTracks.instance != null)
                FolderTracks.instance.Search(e.NewText);
        }

        public void OnFocusChange(View v, bool hasFocus)
        {
            if (hasFocus && !PlaylistTracks.openned)
            {
                Bundle animation = ActivityOptionsCompat.MakeCustomAnimation(this, Android.Resource.Animation.FadeIn, Android.Resource.Animation.FadeOut).ToBundle();
                StartActivity(new Intent(this, typeof(SearchableActivity)), animation);
            }
            PlaylistTracks.openned = false;
        }

        public void SearchOnYoutube(string query)
        {
            YoutubeEngine.searchKeyWorld = query;
            IMenuItem searchItem = menu.FindItem(Resource.Id.search);
            SearchView searchView = (SearchView)searchItem.ActionView;
            searchView.SetQuery(query, false);
            searchView.ClearFocus();
            searchView.Focusable = false;
        }

        public void CancelSearch()
        {
            IMenuItem searchItem = menu.FindItem(Resource.Id.search);
            searchItem.CollapseActionView();
        }

        public void HideSearch()
        {
            if (!searchDisplayed)
                return;

            searchDisplayed = false;

            if (menu == null)
                return;

            var item = menu.FindItem(Resource.Id.filter);
            var searchItem = item.ActionView;
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

            var item = menu.FindItem(Resource.Id.filter);
            item.SetVisible(true);
            item.CollapseActionView();
            var searchItem = item.ActionView;
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

        public void Navigate(int layout)
        {
            contentRefresh.Refreshing = false;

            bool youtubeSwitch = false;
            if(YoutubeEngine.instances != null)
            {
                YoutubeEngine.error = false;
                ViewGroup rootView = FindViewById<ViewGroup>(Android.Resource.Id.Content);
                foreach (YoutubeEngine instance in YoutubeEngine.instances)
                {
                    rootView.RemoveView(instance.emptyView);
                }
                rootView.RemoveView(YoutubeEngine.loadingView);

                var searchView = menu.FindItem(Resource.Id.search).ActionView.JavaCast<SearchView>();
                menu.FindItem(Resource.Id.search).CollapseActionView();
                searchView.ClearFocus();
                searchView.Iconified = true;
                searchView.SetQuery("", false);
                SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                FindViewById(Resource.Id.tabs).Visibility = ViewStates.Gone;
                YoutubeEngine.instances = null;
                youtubeSwitch = true;
            }

            if(PlaylistTracks.instance != null)
            {
                PlaylistTracks.instance.navigating = true;
                SupportFragmentManager.BeginTransaction().Remove(PlaylistTracks.instance).Commit();
            }

            Android.Support.V4.App.Fragment fragment = null;
            switch (layout)
            {
                case Resource.Id.musicLayout:
                    if (Home.instance != null)
                    {
                        Home.instance.Refresh();
                        return;
                    }

                    tab = "Home";
                    HideTabs();
                    HideSearch();
                    ShowQuickPlay();
                    fragment = Home.NewInstance();
                    break;

                case Resource.Id.browseLayout:
                    if (Browse.instance != null && !youtubeSwitch)
                    {
                        Browse.instance.Refresh();
                        return;
                    }

                    tab = "Browse";
                    DisplaySearch();
                    HideQuickPlay();
                    fragment = Pager.NewInstance(0, 0);
                    break;

                case Resource.Id.playlistLayout:
                    if (Playlist.instance != null)
                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        Playlist.instance.Refresh();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        return;
                    }

                    tab = "Playlist";
                    HideTabs();
                    HideSearch();
                    HideQuickPlay();
                    fragment = Playlist.NewInstance();
                    break;
            }

            if (fragment == null)
                fragment = EmptyFragment.NewInstance();

            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, fragment).Commit();
        }

        public async void SetBrowseTabs(ViewPager pager, int selectedTab = 0)
        {
            while (!canSwitch)
                await Task.Delay(10);

            if (tab != "Browse")
                return;

            canSwitch = false;
            usePager = true;

            viewPager = pager;
            TabLayout tabs = FindViewById<TabLayout>(Resource.Id.tabs);
            ((AppBarLayout.LayoutParams)FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsingToolbar).LayoutParameters).ScrollFlags = AppBarLayout.LayoutParams.ScrollFlagScroll | AppBarLayout.LayoutParams.ScrollFlagEnterAlways | AppBarLayout.LayoutParams.ScrollFlagSnap;

            tabs.RemoveAllTabs();
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

            pager.CurrentItem = selectedTab;
            tabs.TabMode = TabLayout.ModeFixed;
            tabs.SetScrollPosition(selectedTab, 0f, true);

            CanSwitchDelay();
        }

        public async void SetYtTabs(ViewPager pager, string querry, int selectedTab = 0)
        {
            Console.WriteLine("&Setting yt tabs");

            while (!canSwitch)
                await Task.Delay(10);

            canSwitch = false;
            usePager = true;

            viewPager = pager;
            TabLayout tabs = FindViewById<TabLayout>(Resource.Id.tabs);
            ((AppBarLayout.LayoutParams)FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsingToolbar).LayoutParameters).ScrollFlags = AppBarLayout.LayoutParams.ScrollFlagScroll | AppBarLayout.LayoutParams.ScrollFlagEnterAlways | AppBarLayout.LayoutParams.ScrollFlagSnap;

            if (YoutubeEngine.instances != null)
            {
                tabs.Visibility = ViewStates.Visible;
                for (int i = 0; i < YoutubeEngine.instances.Length; i++)
                {
                    if (YoutubeEngine.instances[i].focused)
                        selectedTab = i;
                }

                pager.CurrentItem = selectedTab;
                pager.SetCurrentItem(selectedTab, true);

                YoutubeEngine.instances[selectedTab].focused = true;
                YoutubeEngine.instances[selectedTab].OnFocus();
                pager.RefreshDrawableState();
                CanSwitchDelay();
                return;
            }

            tabs.Visibility = ViewStates.Visible;
            tabs.RemoveAllTabs();
            tabs.AddTab(tabs.NewTab().SetText("All"));
            tabs.AddTab(tabs.NewTab().SetText("Tracks"));
            tabs.AddTab(tabs.NewTab().SetText("Playlists"));
            tabs.AddTab(tabs.NewTab().SetText("Channels"));

            ViewPagerAdapter adapter = new ViewPagerAdapter(SupportFragmentManager);
            Android.Support.V4.App.Fragment[] fragment = YoutubeEngine.NewInstances(querry);
            adapter.AddFragment(fragment[0], "All");
            adapter.AddFragment(fragment[1], "Tracks");
            adapter.AddFragment(fragment[2], "Playlists");
            adapter.AddFragment(fragment[3], "Channels");

            pager.Adapter = adapter;
            pager.AddOnPageChangeListener(this);
            pager.AddOnPageChangeListener(new TabLayout.TabLayoutOnPageChangeListener(tabs));
            tabs.SetupWithViewPager(pager);
            tabs.TabReselected += OnTabReselected;

            pager.CurrentItem = selectedTab;
            tabs.TabMode = TabLayout.ModeScrollable;
            tabs.SetScrollPosition(selectedTab, 0f, true);

            YoutubeEngine.instances[selectedTab].focused = true;
            YoutubeEngine.instances[selectedTab].OnFocus();

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
            if(YoutubeEngine.instances != null)
            {
                foreach (YoutubeEngine instance in YoutubeEngine.instances)
                {
                    if (instance.focused)
                    {
                        instance.ListView.SmoothScrollToPosition(0);
                    }
                }
            }
        }

        async void CanSwitchDelay()
        {
            await Task.Delay(350);
            canSwitch = true;
        }

        public void OnPageScrollStateChanged(int state)
        {
            contentRefresh.SetEnabled( state == ViewPager.ScrollStateIdle );
        }

        public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels) { }

        public void OnPageSelected(int position)
        {
            if (Browse.instance != null)
            {
                if(!FolderBrowse.instance.populated)
                    FolderBrowse.instance.PopulateList();

                if(position == 0)
                {
                    if(Browse.instance.focused)
                        Browse.instance.ListView.SmoothScrollToPosition(0);

                    Browse.instance.focused = true;
                    FolderBrowse.instance.focused = false;
                    DisplaySearch();
                }
                if(position == 1)
                {
                    if(FolderBrowse.instance.focused)
                        FolderBrowse.instance.ListView.SmoothScrollToPosition(0);

                    Browse.instance.focused = false;
                    FolderBrowse.instance.focused = true;
                    HideSearch();
                }
            }
            else if (YoutubeEngine.instances != null)
            {
                foreach(YoutubeEngine instance in YoutubeEngine.instances)
                {
                    if (instance.focused)
                        instance.OnUnfocus();
                    instance.focused = false;
                }
                YoutubeEngine.instances[position].focused = true;
                YoutubeEngine.instances[position].OnFocus();
            }
        }

        public void HideTabs()
        {
            usePager = false;
            TabLayout tabs = FindViewById<TabLayout>(Resource.Id.tabs);
            tabs.RemoveAllTabs();
            tabs.Visibility = ViewStates.Gone;

            ((AppBarLayout.LayoutParams)FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsingToolbar).LayoutParameters).ScrollFlags = 0;

            if (viewPager == null)
                return;

            ViewPagerAdapter adapter = (ViewPagerAdapter)viewPager.Adapter;
            if (adapter != null)
            {
                for (int i = 0; i < adapter.Count; i++)
                    SupportFragmentManager.BeginTransaction().Remove(adapter.GetItem(i)).Commit();

                adapter.Dispose();
                viewPager.Adapter = null;
            }
        }

        public void PrepareSmallPlayer()
        {
            Player.instance.RefreshPlayer();

            if (!prepared)
            {
                FrameLayout smallPlayer = FindViewById<FrameLayout>(Resource.Id.smallPlayer);
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
            ShowSmallPlayer();
            ShowPlayer();
        }

        public void ShowPlayer()
        {
            FindViewById<BottomNavigationView>(Resource.Id.bottomView).TranslationY = (int)(56 * Resources.DisplayMetrics.Density + 0.5f);
            FindViewById(Resource.Id.playerView).Alpha = 1;
            FindViewById(Resource.Id.smallPlayer).Alpha = 0;
            FindViewById(Resource.Id.quickPlayLinear).ScaleX = 0;
            FindViewById(Resource.Id.quickPlayLinear).ScaleY = 0;
            SheetBehavior.State = BottomSheetBehavior.StateExpanded;
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
                    {
                        Snackbar snackBar = Snackbar.Make(FindViewById<CoordinatorLayout>(Resource.Id.snackBar), "Permission denied, can't list musics.", Snackbar.LengthLong)
                            .SetAction("Ask Again", (v) => { GetStoragePermission(); });
                        snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                        snackBar.Show();
                    }
                }
            }
            else if(requestCode == 2659)
            {
                Console.WriteLine("&Write permission authorized");
            }
        }

        void PremissionAuthorized()
        {
            Browse.instance?.PopulateList();
            Playlist.instance?.PopulateView();
            if (Browse.instance == null && Playlist.instance == null && PreferencesFragment.instance == null && Preferences.instance == null)
                LocalPlay(sender, new EventArgs());
        }

        public void HideSmallPlayer()
        {
            FindViewById<FrameLayout>(Resource.Id.contentView).SetPadding(0, 0, 0, 0);
            FindViewById<NestedScrollView>(Resource.Id.playerSheet).Alpha = 1;
            SheetBehavior.State = BottomSheetBehavior.StateHidden;
        }

        public void ShowSmallPlayer()
        {
            FindViewById(Resource.Id.playerView).Alpha = 0;
            Player.instance?.RefreshPlayer();
            FindViewById<FrameLayout>(Resource.Id.contentView).SetPadding(0, 0, 0, DpToPx(70));
        }

        public void ShowQuickPlay()
        {
            LinearLayout quickPlayLayout = FindViewById<LinearLayout>(Resource.Id.quickPlayLinear);
            quickPlayLayout.FindViewById<LinearLayout>(Resource.Id.quickPlayLinear).Animate().Alpha(1);
            if (!quickPlayLayout.FindViewById<FloatingActionButton>(Resource.Id.quickPlay).HasOnClickListeners)
            {
                quickPlayLayout.FindViewById<FloatingActionButton>(Resource.Id.quickPlay).Click += QuickPlay;
                quickPlayLayout.FindViewById<FloatingActionButton>(Resource.Id.localPlay).Click += LocalPlay;
                quickPlayLayout.FindViewById<FloatingActionButton>(Resource.Id.ytPlay).Click += YtPlay;
            }
        }

        public void HideQuickPlay()
        {
            FindViewById<LinearLayout>(Resource.Id.quickPlayLinear).Animate().Alpha(0);
        }

        public async void QuickPlay(object sender, EventArgs e)
        {
            FloatingActionButton quickPlay = FindViewById<FloatingActionButton>(Resource.Id.quickPlay);
            if (QuickPlayOpenned)
            {
                AnimatedVectorDrawable drawable = (AnimatedVectorDrawable)crossToPlay;
                quickPlay.SetImageDrawable(drawable);
                drawable.Start();
                QuickPlayOpenned = false;
                await Task.Delay(10);
                FindViewById<FloatingActionButton>(Resource.Id.ytPlay).Animate().Alpha(0);
                await Task.Delay(10);
                FindViewById<FloatingActionButton>(Resource.Id.localPlay).Visibility = ViewStates.Gone;
                FindViewById<FloatingActionButton>(Resource.Id.ytPlay).Animate().Alpha(0);
                await Task.Delay(10);
                FindViewById<FloatingActionButton>(Resource.Id.ytPlay).Visibility = ViewStates.Gone;
            }
            else
            {
                AnimatedVectorDrawable drawable = (AnimatedVectorDrawable)playToCross;
                quickPlay.SetImageDrawable(drawable);
                drawable.Start();
                QuickPlayOpenned = true;
                await Task.Delay(10);
                FindViewById<FloatingActionButton>(Resource.Id.ytPlay).Alpha = 0;
                FindViewById<FloatingActionButton>(Resource.Id.ytPlay).Visibility = ViewStates.Visible;
                FindViewById<FloatingActionButton>(Resource.Id.ytPlay).Animate().Alpha(1);
                await Task.Delay(10);
                FindViewById<FloatingActionButton>(Resource.Id.localPlay).Alpha = 0;
                FindViewById<FloatingActionButton>(Resource.Id.localPlay).Visibility = ViewStates.Visible;
                FindViewById<FloatingActionButton>(Resource.Id.localPlay).Animate().Alpha(1);
            }
        }

        private void LocalPlay(object sender, EventArgs e)
        {
            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) != (int)Permission.Granted)
            {
                this.sender = sender;
                GetStoragePermission();
                return;
            }

            if (sender != null)
                QuickPlay(this, e);

            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(this);
            string shortcut = prefManager.GetString("localPlay", "Shuffle All Audio Files");
            if (shortcut == "Shuffle All Audio Files")
            {
                List<string> paths = new List<string>();
                Android.Net.Uri musicUri = MediaStore.Audio.Media.ExternalContentUri;

                CursorLoader cursorLoader = new CursorLoader(this, musicUri, null, null, null, null);
                ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

                if (musicCursor != null && musicCursor.MoveToFirst())
                {
                    int pathID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Data);
                    do
                    {
                        paths.Add(musicCursor.GetString(pathID));
                    }
                    while (musicCursor.MoveToNext());
                    musicCursor.Close();
                }
                
                if(paths.Count == 0)
                {
                    Snackbar snackBar = Snackbar.Make(FindViewById<CoordinatorLayout>(Resource.Id.snackBar), "No music file found on this device. Can't create a mix.", Snackbar.LengthLong);
                    snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                    snackBar.Show();
                    return;
                }

                Intent intent = new Intent(this, typeof(MusicPlayer));
                intent.PutStringArrayListExtra("files", paths);
                if (sender == null)
                    intent.PutExtra("clearQueue", false);
                intent.SetAction("RandomPlay");
                StartService(intent);
                ShowSmallPlayer();
                ShowPlayer();
                Player.instance?.UpdateNext();
                Home.instance?.RefreshQueue();
            }
            else
            {
                long playlistID = prefManager.GetLong("localPlaylistID", -1);
                if (playlistID != -1)
                {
                    Playlist.RandomPlay(playlistID, this);
                    ShowSmallPlayer();
                    ShowPlayer();
                }
                else
                {
                    Snackbar snackBar = Snackbar.Make(FindViewById<View>(Resource.Id.snackBar), "No playlist set on setting.", Snackbar.LengthLong)
                        .SetAction("Set it now", (v) =>
                        {
                            Intent intent = new Intent(Application.Context, typeof(Preferences));
                            StartActivity(intent);
                        });
                    snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                    snackBar.Show();
                }
            }
        }

        private async void YtPlay(object sender, EventArgs e)
        {
            if (MusicPlayer.CurrentID() == -1 || MusicPlayer.queue[MusicPlayer.CurrentID()].youtubeID == null || MusicPlayer.queue[MusicPlayer.CurrentID()].youtubeID == "")
            {
                if (MusicPlayer.CurrentID() != -1)
                {
                    Stream stream = new FileStream(MusicPlayer.queue[MusicPlayer.CurrentID()].Path, FileMode.Open, FileAccess.Read);

                    var meta = TagLib.File.Create(new StreamFileAbstraction(MusicPlayer.queue[MusicPlayer.CurrentID()].Path, stream, stream));
                    string ytID = meta.Tag.Comment;
                    stream.Dispose();

                    MusicPlayer.queue[MusicPlayer.CurrentID()].youtubeID = ytID;
                    if (ytID != null && ytID != "")
                        goto YtMix;
                }

                QuickPlay(this, e);
                Snackbar snackBar = Snackbar.Make(FindViewById(Resource.Id.snackBar), "You need to be playing a youtube song in order to create a mix.", Snackbar.LengthLong);
                snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Android.Graphics.Color.White);
                snackBar.Show();
                return;
            }

        YtMix:

            ProgressBar parseProgress = FindViewById<ProgressBar>(Resource.Id.ytProgress);
            parseProgress.Visibility = ViewStates.Visible;
            parseProgress.ScaleY = 6;
            QuickPlay(this, e);

            if(!await WaitForYoutube())
            {
                Snackbar snackBar = Snackbar.Make(FindViewById(Resource.Id.snackBar), "Error while loading. Check your internet connection and check if your logged in.", Snackbar.LengthLong);
                snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Android.Graphics.Color.White);
                snackBar.Show();
                return;
            }

            List<Song> tracks = new List<Song>();
            try
            {
                YoutubeClient client = new YoutubeClient();
                Video video = await client.GetVideoAsync(MusicPlayer.queue[MusicPlayer.CurrentID()].youtubeID);

                var ytPlaylistRequest = YoutubeEngine.youtubeService.PlaylistItems.List("snippet, contentDetails");
                ytPlaylistRequest.PlaylistId = video.GetVideoMixPlaylistId();
                ytPlaylistRequest.MaxResults = 50;

                var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                foreach (var item in ytPlaylist.Items)
                {
                    if (item.Snippet.Title != "[Deleted video]" && item.Snippet.Title != "Private video" && item.Snippet.Title != "Deleted video" && item.ContentDetails.VideoId != MusicPlayer.queue[MusicPlayer.CurrentID()].youtubeID)
                    {
                        Song song = new Song(item.Snippet.Title, "", item.Snippet.Thumbnails.Default__.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true, false);
                        tracks.Add(song);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is YoutubeExplode.Exceptions.ParseException)
                    instance.YoutubeEndPointChanged();
                else if (ex is System.Net.Http.HttpRequestException)
                    instance.Timout();

                return;
            }

            Song current = MusicPlayer.queue[MusicPlayer.CurrentID()];
            current.queueSlot = 0;
            MusicPlayer.queue.Clear();
            MusicPlayer.queue.Add(current);
            MusicPlayer.currentID = 0;

            Random r = new Random();
            tracks = tracks.OrderBy(x => r.Next()).ToList();
            foreach (Song song in tracks)
                MusicPlayer.instance.AddToQueue(song);

            ShowSmallPlayer();
            ShowPlayer();
            Player.instance?.UpdateNext();
            Home.instance?.RefreshQueue();
            parseProgress.Visibility = ViewStates.Gone;
        }

        public void YoutubeEndPointChanged()
        {
            FindViewById<ProgressBar>(Resource.Id.ytProgress).Visibility = ViewStates.Gone;
            Snackbar snackBar = Snackbar.Make(FindViewById(Resource.Id.snackBar), "The way youtube play video has changed, the app can't play this video now. Wait for the next update.", Snackbar.LengthLong);
            snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Android.Graphics.Color.White);
            snackBar.Show();
        }

        public void Timout()
        {
            Snackbar snackBar = Snackbar.Make(FindViewById(Resource.Id.snackBar), "Timout exception, check if you're still connected to internet.", Snackbar.LengthLong);
            snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Android.Graphics.Color.White);
            snackBar.Show();
        }

        public int DpToPx(int dx)
        {
            float scale = Resources.DisplayMetrics.Density;
            return (int) (dx * scale + 0.5f);
        }

        public static bool HasInternet()
        {
            ConnectivityManager connectivityManager = (ConnectivityManager)Application.Context.GetSystemService(ConnectivityService);
            NetworkInfo activeNetworkInfo = connectivityManager.ActiveNetworkInfo;
            if (activeNetworkInfo == null || !activeNetworkInfo.IsConnected)
                return false;

            return true;
        }

        public async static void CheckForUpdate(Activity activity, bool displayToast)
        {
            if(!HasInternet())
            {
                if (displayToast)
                {
                    if (instance != null)
                    {
                        Snackbar snackBar = Snackbar.Make(instance.FindViewById(Resource.Id.snackBar), "You are not connected to internet, can't check for updates.", Snackbar.LengthLong);
                        snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                        snackBar.Show();
                    }
                    else
                        Toast.MakeText(Application.Context, "You are not connected to internet, can't check for updates.", ToastLength.Short).Show();
                }
                return;
            }

            string VersionAsset;
            AssetManager assets = Application.Context.Assets;
            using (StreamReader sr = new StreamReader(assets.Open("Version.txt")))
            {
                VersionAsset = sr.ReadToEnd();
            }

            string versionID = VersionAsset.Substring(9, 5);
            versionID = versionID.Remove(1, 1);
            int version = int.Parse(versionID.Remove(2, 1));

            string gitVersionID;
            int gitVersion;
            string downloadPath;

            using(WebClient client = new WebClient())
            {
                string GitVersion = await client.DownloadStringTaskAsync(new System.Uri(versionURI));
                gitVersionID = GitVersion.Substring(9, 5);
                gitVersionID = gitVersionID.Remove(1, 1);
                gitVersion = int.Parse(gitVersionID.Remove(2, 1));
                downloadPath = GitVersion.Substring(18);
            }

            Console.WriteLine("&Version: " + version + " GitVersion: " + gitVersion);

            if(gitVersion > version)
            {
                Console.WriteLine("&An update is available");
                Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(activity, dialogTheme);
                builder.SetTitle(string.Format("The version {0} is available", gitVersionID));
                builder.SetMessage("An update is available, do you want to download it now ?");
                builder.SetPositiveButton("Ok", (sender, e) => { InstallUpdate(gitVersionID, downloadPath); });
                builder.SetNegativeButton("Later", (sender, e) => { });
                builder.Show();
            }
            else if(displayToast)
            {
                if (instance != null)
                {
                    Snackbar snackBar = Snackbar.Make(instance.FindViewById(Resource.Id.snackBar), "Your app is up to date.", Snackbar.LengthLong);
                    snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Android.Graphics.Color.White);
                    snackBar.Show();
                }
                else
                    Toast.MakeText(Application.Context, "Your app is up to date.", ToastLength.Short).Show();
            }
        }

        public async static void InstallUpdate(string version, string downloadPath)
        {
            Toast.MakeText(Application.Context, "Downloading update, you will be prompt for the installation soon.", ToastLength.Short).Show();
            using (WebClient client = new WebClient())
            {
                await client.DownloadFileTaskAsync(downloadPath, Android.OS.Environment.ExternalStorageDirectory + "/download/" + "MusicApp-v" + version + ".apk");
            }
            Intent intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(Android.Net.Uri.FromFile(new Java.IO.File(Android.OS.Environment.ExternalStorageDirectory + "/download/" + "MusicApp-v" + version + ".apk")), "application/vnd.android.package-archive");
            intent.SetFlags(ActivityFlags.NewTask);
            Application.Context.StartActivity(intent);
        }


        protected override void OnResume()
        {
            base.OnResume();
            paused = false;
            instance = this;
            StateSaved = false;

            if(SearchableActivity.instance != null && SearchableActivity.instance.searched)
            {
                if (YoutubeEngine.instances != null)
                {
#pragma warning disable CS4014
                    foreach (YoutubeEngine instance in YoutubeEngine.instances)
                        instance.Search(YoutubeEngine.searchKeyWorld, instance.querryType, true);
                }
                else
                {
                    if(PlaylistTracks.instance != null)
                        SupportFragmentManager.BeginTransaction().Remove(PlaylistTracks.instance).CommitNow();

                    SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, Pager.NewInstance(1, 0)).AddToBackStack(null).Commit();
                }
                SearchableActivity.instance = null;
            }
        }

        //public void SaveInstance()
        //{
        //    if (Queue.instance != null)
        //        return;

        //    if (Home.instance != null)
        //    {
        //        parcelableSender = "Home";
        //        parcelable = Home.instance.ListView.GetLayoutManager().OnSaveInstanceState();
        //    }
        //    else if (Browse.instance != null && Browse.instance.focused)
        //    {
        //        parcelableSender = "Browse";
        //        parcelable = Browse.instance.ListView.OnSaveInstanceState();
        //    }
        //    else if (FolderBrowse.instance != null && FolderBrowse.instance.focused)
        //    {
        //        parcelableSender = "FolderBrowse";
        //        parcelable = FolderBrowse.instance.ListView.OnSaveInstanceState();
        //    }
        //    else if (Playlist.instance != null)
        //    {
        //        parcelableSender = "Playlist";
        //        parcelable = Playlist.instance.ListView.GetLayoutManager().OnSaveInstanceState();
        //    }
        //    //Artist instance
        //    else if (PlaylistTracks.instance != null)
        //    {
        //        parcelableSender = "PlaylistTracks";
        //        parcelable = PlaylistTracks.instance.ListView.OnSaveInstanceState();
        //    }
        //    else if(FolderTracks.instance != null)
        //    {
        //        parcelableSender = "FolderTracks";
        //        parcelable = FolderTracks.instance.ListView.OnSaveInstanceState();
        //    }
        //    else
        //    {
        //        parcelableSender = "Home";
        //        parcelable = null;
        //    }
        //}

        //public void ResumeInstance()
        //{
        //    switch (parcelableSender)
        //    {
        //        case "Home":
        //            FindViewById<BottomNavigationView>(Resource.Id.bottomView).SelectedItemId = Resource.Id.musicLayout;
        //            break;
        //        case "Browse":
        //            FindViewById<BottomNavigationView>(Resource.Id.bottomView).SelectedItemId = Resource.Id.browseLayout;
        //            break;
        //        case "FolderBrowse":
        //            resuming = true;
        //            FindViewById<BottomNavigationView>(Resource.Id.bottomView).SelectedItemId = Resource.Id.browseLayout;
        //            break;
        //        case "Playlist":
        //            FindViewById<BottomNavigationView>(Resource.Id.bottomView).SelectedItemId = Resource.Id.playlistLayout;
        //            break;
        //        case "YoutubeEngine-All":
        //            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, Pager.NewInstance(2, 0)).Commit();
        //            break;
        //        case "YoutubeEngine-Tracks":
        //            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, Pager.NewInstance(2, 1)).Commit();
        //            break;
        //        case "YoutubeEngine-Playlists":
        //            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, Pager.NewInstance(2, 2)).Commit();
        //            break;
        //        case "YoutubeEngine-Channels":
        //            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, Pager.NewInstance(2, 3)).Commit();
        //            break;
        //        case "PlaylistTracks":
        //            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, PlaylistTracks.instance).Commit();
        //            SupportActionBar.SetHomeButtonEnabled(true);
        //            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
        //            SupportActionBar.Title = PlaylistTracks.instance.playlistName;
        //            break;
        //        case "FolderTracks":
        //            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, FolderTracks.instance).Commit();
        //            SupportActionBar.SetHomeButtonEnabled(true);
        //            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
        //            SupportActionBar.Title = FolderTracks.instance.folderName;
        //            break;
        //        default:
        //            break;
        //    }
        //}

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if(MusicPlayer.instance != null && !MusicPlayer.isRunning && Preferences.instance == null && Queue.instance == null && EditMetaData.instance == null)
            {
                Intent intent = new Intent(this, typeof(MusicPlayer));
                intent.SetAction("Stop");
                StartService(intent);
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            paused = true;
        }

        public override void OnBackPressed()
        {
            if (PlaylistTracks.instance != null)
                SupportFragmentManager.BeginTransaction().Remove(PlaylistTracks.instance).Commit();
            else
                base.OnBackPressed();
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            StateSaved = true;
            base.OnSaveInstanceState(outState);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            OnLateCreate(intent, false);
        }
    }
}