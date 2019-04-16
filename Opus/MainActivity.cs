using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Database;
using Android.Gms.Auth.Api;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Cast.Framework;
using Android.Gms.Cast.Framework.Media;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Graphics;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Preferences;
using Android.Views;
using Android.Widget;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Newtonsoft.Json.Linq;
using Opus.Resources.Portable_Class;
using Opus.Resources.values;
using SQLite;
using Square.Picasso;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using YoutubeExplode;
using CursorLoader = Android.Support.V4.Content.CursorLoader;
using Environment = Android.OS.Environment;
using Fragment = Android.Support.V4.App.Fragment;
using Playlist = Opus.Resources.Portable_Class.Playlist;
using Request = Square.OkHttp.Request;
using SearchView = Android.Support.V7.Widget.SearchView;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using TransportType = Android.Net.TransportType;

namespace Opus
{
    [Activity(Label = "Opus", MainLauncher = true, Icon = "@drawable/Icon", Theme = "@style/SplashScreen", ScreenOrientation = ScreenOrientation.Portrait, LaunchMode = LaunchMode.SingleTask)]
    [IntentFilter(new[] {Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataHost = "www.youtube.com", DataMimeType = "text/*")]
    [IntentFilter(new[] {Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataHost = "m.youtube.com", DataMimeType = "text/plain")]
    [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault }, DataMimeTypes = new[] { "audio/*", "application/ogg", "application/x-ogg", "application/itunes" })]
    public class MainActivity : AppCompatActivity, GoogleApiClient.IOnConnectionFailedListener, IResultCallback, IMenuItemOnActionExpandListener, View.IOnFocusChangeListener, ISessionManagerListener
    {
        public static MainActivity instance;
        public new static int Theme = 1;
        public static int dialogTheme;

        public bool NoToolbarMenu = false;
        public IMenu menu;
        public SwipeRefreshLayout contentRefresh;
        public bool Paused = false;

        public bool SkipStop = false;
        public PlayerBehavior SheetBehavior;

        public const int RequestCode = 8539;
        private const int WriteRequestCode = 2659;
        public const int NotifUpdateID = 4626;
        private const string versionURI = "https://raw.githubusercontent.com/AnonymusRaccoon/Opus/master/Opus/Assets/Version.txt";

        public static GoogleSignInAccount account;
        private Intent AskIntent;
        public bool waitingForYoutube;
        private DateTime NextRefreshDate;
        private bool? PermissionGot;
        public bool ResumeKiller;

        public static CastContext CastContext;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(this);
            SwitchTheme(pref.GetInt("theme", 0));
            SetContentView(Resource.Layout.Main);
            instance = this;

            var bottomNavigation = FindViewById<BottomNavigationView>(Resource.Id.bottomView);
            bottomNavigation.NavigationItemSelected += PreNavigate;

            SetSupportActionBar(FindViewById<Toolbar>(Resource.Id.toolbar));
            SupportActionBar.SetDisplayShowTitleEnabled(false);

            contentRefresh = FindViewById<SwipeRefreshLayout>(Resource.Id.contentRefresh);

            if(savedInstanceState == null)
                Navigate(Resource.Id.musicLayout);

            SheetBehavior = (PlayerBehavior)BottomSheetBehavior.From(FindViewById(Resource.Id.playerSheet));
            SheetBehavior.State = BottomSheetBehavior.StateCollapsed;
            SheetBehavior.SetBottomSheetCallback(new PlayerCallback(this));

            PrepareSmallPlayer();
            if (MusicPlayer.queue == null || MusicPlayer.queue.Count == 0)
                MusicPlayer.RetrieveQueueFromDataBase();
            else if(SheetBehavior.State != BottomSheetBehavior.StateExpanded)
                ShowSmallPlayer();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                NotificationManager notificationManager = (NotificationManager)GetSystemService(NotificationService);
                NotificationChannel channel = new NotificationChannel("Opus.Channel", "Default Channel", NotificationImportance.Low)
                {
                    Description = "Channel used for download progress and music control notification.",
                    LockscreenVisibility = NotificationVisibility.Public
                };
                channel.EnableVibration(false);
                channel.EnableLights(false);
                notificationManager.CreateNotificationChannel(channel);
            }

            CheckForUpdate(this, false);
            HandleIntent(Intent);
            Login();
            SyncPlaylists();
        }

        private void HandleIntent(Intent intent)
        {
            if (intent.Action == "Sleep")
            {
                ShowPlayer();
                Player.instance.SleepDialog();
            }
            else if (intent.Action == "Player")
            {
                ShowPlayer();
                Player.instance.RefreshPlayer();
            }
            else if (intent.Action == Intent.ActionView && intent.Data != null)
            {
                MusicPlayer.queue.Clear();
                Intent inte = new Intent(this, typeof(MusicPlayer));
                inte.PutExtra("file", intent.Data.ToString());
                StartService(inte);

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
                    Toast.MakeText(this, Resource.String.cant_play_non_youtube, ToastLength.Short).Show();
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


        public  void Login(bool canAsk = true, bool skipSilentLog = false, bool skipLastSigned = false)
        {
            waitingForYoutube = true;

            if (!skipLastSigned)
            {
                if (account == null)
                    account = GoogleSignIn.GetLastSignedInAccount(this);


                if (account != null)
                {
                    CreateYoutube();
                    return;
                }
            }

            //This will be used only when the access has been revoked, when the refresh token has been lost or for the first loggin. 
            //In each case we want a refresh token so we call RequestServerAuthCode with true as the second parameter.
            GoogleSignInOptions gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                .RequestIdToken(GetString(Resource.String.clientID))
                .RequestServerAuthCode(GetString(Resource.String.clientID), true)
                .RequestScopes(new Scope(YouTubeService.Scope.Youtube))
                .Build();

            GoogleApiClient googleClient = new GoogleApiClient.Builder(this)
                .AddApi(Auth.GOOGLE_SIGN_IN_API, gso)
                .Build();

            googleClient.Connect();

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
                    AskIntent = Auth.GoogleSignInApi.GetSignInIntent(googleClient);
                    silentLog.SetResultCallback(this);
                }
                else if (canAsk)
                {
                    ResumeKiller = true;
                    StartActivityForResult(Auth.GoogleSignInApi.GetSignInIntent(googleClient), 1598);
                }
            }
            else if (canAsk)
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
            if (account != null)
            {
                RunOnUiThread(() => { Picasso.With(this).Load(account.PhotoUrl).Transform(new CircleTransformation()).Into(new AccountTarget()); });
                CreateYoutube();
            }
            else if(AskIntent != null)
            {
                ResumeKiller = true;
                StartActivityForResult(AskIntent, 1598);
                AskIntent = null;
            }
        }

        public async void CreateYoutube()
        {
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(this);
            string refreshToken = prefManager.GetString("refresh-token", null);
            Console.WriteLine("&Current refresh token: " + refreshToken);

            //This method do not return refresh-token if the app has already been aprouved by google for this user, should force request
            if (refreshToken == null)
            {
                Console.WriteLine("&Getting refresh-token and creating a youtube service");
                Console.WriteLine("&Code = " + account.ServerAuthCode);

                if (account.ServerAuthCode == null)
                {
                    Login(true, false, true);
                    return;
                }

                Dictionary<string, string> fields = new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "client_id", GetString(Resource.String.clientID) },
                    { "client_secret", GetString(Resource.String.clientSecret) },
                    { "redirect_uri", "" },
                    { "code", account.ServerAuthCode },
                    { "id_token", account.IdToken },
                };

                var items = from kvp in fields
                            select kvp.Key + "=" + kvp.Value;

                string content = string.Join("&", items);

                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/oauth2/v4/token");
                    request.Host = "www.googleapis.com";

                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = content.Length;

                    using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                    {
                        writer.Write(content);
                    }

                    Console.WriteLine("&Content: " + content);

                    HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync();

                    string response;
                    using (StreamReader responseReader = new StreamReader(request.GetResponse().GetResponseStream()))
                    {
                        response = responseReader.ReadToEnd();
                    }
                    Console.WriteLine("&Response: " + response);

                    JToken json = JObject.Parse(response);
                    GoogleCredential credential = GoogleCredential.FromAccessToken((string)json.SelectToken("access_token"));
                    YoutubeEngine.youtubeService = new YouTubeService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = "Opus"
                    });

                    refreshToken = (string)json.SelectToken("refresh_token");
                    if (refreshToken != null)
                    {
                        ISharedPreferencesEditor editor = prefManager.Edit();
                        editor.PutString("refresh-token", refreshToken);
                        editor.Apply();
                    }

                    int expireIn = (int)json.SelectToken("expires_in");
                    NextRefreshDate = DateTime.UtcNow.AddSeconds(expireIn - 30); //Should refresh a bit before the expiration of the acess token
                }
                catch (WebException ex)
                {
                    Console.WriteLine("&Refresh token get error: " + ex.Message);
                    UnknowError(new Action(() => { CreateYoutube(); }));
                }
            }
            else if (account != null)
            {
                Console.WriteLine("&Getting a new access-token and creating a youtube service");
                Dictionary<string, string> fields = new Dictionary<string, string>
                {
                    { "refresh_token", refreshToken },
                    { "client_id", GetString(Resource.String.clientID) },
                    { "client_secret", GetString(Resource.String.clientSecret) },
                    { "grant_type", "refresh_token" },
                };

                var items = from kvp in fields
                            select kvp.Key + "=" + kvp.Value;

                string content = string.Join("&", items);

                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/oauth2/v4/token");
                    request.Host = "www.googleapis.com";

                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = content.Length;

                    using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                    {
                        writer.Write(content);
                    }

                    Console.WriteLine("&Content: " + content);

                    HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync();

                    string response;
                    using (StreamReader responseReader = new StreamReader(request.GetResponse().GetResponseStream()))
                    {
                        response = responseReader.ReadToEnd();
                    }
                    Console.WriteLine("&Response: " + response);

                    JToken json = JObject.Parse(response);
                    GoogleCredential credential = GoogleCredential.FromAccessToken((string)json.SelectToken("access_token"));
                    YoutubeEngine.youtubeService = new YouTubeService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = "Opus"
                    });

                    int expireIn = (int)json.SelectToken("expires_in");
                    NextRefreshDate = DateTime.UtcNow.AddSeconds(expireIn - 30); //Should refresh a bit before the expiration of the acess token
                }
                catch (WebException ex)
                {
                    Console.WriteLine("&New access token get error: " + ex.Message);
                    UnknowError(new Action(() => { CreateYoutube(); }));
                }
            }
            else
            {
                Login(true);
            }
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
            else if(NextRefreshDate == null || NextRefreshDate <= DateTime.UtcNow) //Acess token has expired
            {
                waitingForYoutube = true;
                CreateYoutube();
            }
            waitingForYoutube = false;
            return true;
        }

        public void Scroll(object sender, AbsListView.ScrollEventArgs e)
        {
            contentRefresh.Enabled = e.FirstVisibleItem == 0;

            if(PlaylistTracks.instance != null)
            {
                if (e.FirstVisibleItem + e.VisibleItemCount == e.TotalItemCount)
                    PlaylistTracks.instance.lastVisible = true;
                else
                    PlaylistTracks.instance.lastVisible = false;
            }
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

            if(account != null)
                Picasso.With(this).Load(account.PhotoUrl).Transform(new CircleTransformation()).Into(new AccountTarget());

            var item = menu.FindItem(Resource.Id.filter);
            var filterView = item.ActionView.JavaCast<SearchView>();
            filterView.QueryTextChange += Search;
            item.SetVisible(false);
            menu.FindItem(Resource.Id.search).SetOnActionExpandListener(this);
            ((SearchView)menu.FindItem(Resource.Id.search).ActionView).SetOnQueryTextFocusChangeListener(this);
            ((SearchView)menu.FindItem(Resource.Id.search).ActionView).QueryHint = GetString(Resource.String.youtube_search);

            CastButtonFactory.SetUpMediaRouteButton(this, menu, Resource.Id.media_route_menu_item);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if(item.ItemId == Android.Resource.Id.Home)
            {
                if (PlaylistTracks.instance != null  || FolderTracks.instance != null)
                {
                    Console.WriteLine("&PlaylistTrack back");
                    for (int i = 0; i < SupportFragmentManager.BackStackEntryCount; i++)
                    {
                        Console.WriteLine("&Back stack entry " + i + ": " + SupportFragmentManager.GetBackStackEntryAt(i));
                    }

                    SupportFragmentManager.PopBackStack();
                    //SupportFragmentManager.BeginTransaction().Remove(PlaylistTracks.instance).Commit();
                }
                else if (YoutubeEngine.instances != null)
                {
                    var searchView = menu.FindItem(Resource.Id.search).ActionView.JavaCast<SearchView>();
                    menu.FindItem(Resource.Id.search).CollapseActionView();
                    searchView.ClearFocus();
                    searchView.Iconified = true;
                    searchView.SetQuery("", false);
                    SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                    YoutubeEngine.instances = null;
                }
                //else if (FolderTracks.instance != null)
                //{
                //    SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, Pager.NewInstance(0, 1)).Commit();
                //}
            }
            else if(item.ItemId == Resource.Id.search)
            {
                menu.FindItem(Resource.Id.filter).CollapseActionView();
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
            Console.WriteLine("&Youtube Search Collapse");
            if (YoutubeEngine.instances == null || SearchableActivity.IgnoreMyself)
                return true;

            Console.WriteLine("&Youtube instnace != null");
            for (int i = 0; i < SupportFragmentManager.BackStackEntryCount; i++)
            {
                Console.WriteLine("&Back stack entry " + i + ": " + SupportFragmentManager.GetBackStackEntryAt(i));
            }
            SupportFragmentManager.PopBackStack();
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
            if (hasFocus && !SearchableActivity.IgnoreMyself)
            {
                Bundle animation = ActivityOptionsCompat.MakeCustomAnimation(this, Android.Resource.Animation.FadeIn, Android.Resource.Animation.FadeOut).ToBundle();
                StartActivity(new Intent(this, typeof(SearchableActivity)), animation);
            }
            SearchableActivity.IgnoreMyself = false;
        }

        public void CancelSearch() //SearchableActivity is finishing and no search has been made
        {
            IMenuItem searchItem = menu.FindItem(Resource.Id.search);
            searchItem.CollapseActionView();
        }

        public void HideSearch()
        {
            if (menu == null)
                return;

            var item = menu.FindItem(Resource.Id.filter);
            var searchItem = item.ActionView;
            var searchView = searchItem.JavaCast<SearchView>();

            searchView.ClearFocus();
            searchView.OnActionViewCollapsed();

            item.SetVisible(false);
            item.CollapseActionView();
        }

        public void DisplaySearch()
        {
            var item = menu.FindItem(Resource.Id.filter);
            item.SetVisible(true);
            item.CollapseActionView();
            var searchItem = item.ActionView;
            var searchView = searchItem.JavaCast<SearchView>();

            searchView.ClearFocus();
            searchView.OnActionViewCollapsed();
        }

        private void PreNavigate(object sender, BottomNavigationView.NavigationItemSelectedEventArgs e)
        {
            Navigate(e.Item.ItemId);
        }

        public void Navigate(int layout)
        {
            contentRefresh.Refreshing = false;

            if (YoutubeEngine.instances != null)
            {
                var searchView = menu.FindItem(Resource.Id.search).ActionView.JavaCast<SearchView>();
                menu.FindItem(Resource.Id.search).CollapseActionView();
                searchView.ClearFocus();
                searchView.Iconified = true;
                searchView.SetQuery("", false);
                SupportActionBar.SetDisplayHomeAsUpEnabled(false);

                SupportFragmentManager.PopBackStack(null, Android.Support.V4.App.FragmentManager.PopBackStackInclusive);
            }

            if (PlaylistTracks.instance != null)
            {
                PlaylistTracks.instance.navigating = true;
                SupportFragmentManager.BeginTransaction().Remove(PlaylistTracks.instance).Commit();
                SupportFragmentManager.PopBackStack(null, Android.Support.V4.App.FragmentManager.PopBackStackInclusive);
            }

            Fragment fragment = null;
            switch (layout)
            {
                case Resource.Id.musicLayout:
                    if (Home.instance != null && Home.instance.ListView != null)
                    {
                        Home.instance.ListView.ScrollToPosition(0);
                        return;
                    }

                    HideSearch();
                    fragment = Home.NewInstance();
                    break;

                case Resource.Id.browseLayout:
                    if (Browse.instance != null && Pager.instance != null && Browse.instance.ListView != null)
                    {
                        Pager.instance.ScrollToFirst();
                        Browse.instance.ListView.ScrollToPosition(0);
                        return;
                    }

                    DisplaySearch();
                    fragment = Pager.NewInstance(0, 0);
                    break;

                case Resource.Id.playlistLayout:
                    if (Playlist.instance != null)
                    {
                        Playlist.instance.ListView.ScrollToPosition(0);
                        return;
                    }

                    HideSearch();
                    fragment = Playlist.NewInstance();
                    break;
            }

            if (fragment == null)
                return;

            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, fragment).SetCustomAnimations(Android.Resource.Animation.FadeIn, Android.Resource.Animation.FadeOut).Commit();
        }

        public void PrepareSmallPlayer()
        {
            FrameLayout smallPlayer = FindViewById<FrameLayout>(Resource.Id.smallPlayer);
            if (!smallPlayer.FindViewById<ImageButton>(Resource.Id.spPlay).HasOnClickListeners)
            {
                smallPlayer.FindViewById<ImageButton>(Resource.Id.spLast).Click += Last_Click;
                smallPlayer.FindViewById<ImageButton>(Resource.Id.spPlay).Click += Play_Click;
                smallPlayer.FindViewById<ImageButton>(Resource.Id.spNext).Click += Next_Click;

                smallPlayer.FindViewById<LinearLayout>(Resource.Id.spContainer).Click += Container_Click;
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
            if (Player.errorState == true)
            {
                MusicPlayer.instance?.Resume();
                Player.errorState = false;
                return;
            }

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
            FindViewById<NestedScrollView>(Resource.Id.playerSheet).Visibility = ViewStates.Visible;
            FindViewById<NestedScrollView>(Resource.Id.playerSheet).TranslationY = 0;
            FindViewById<BottomNavigationView>(Resource.Id.bottomView).TranslationY = (int)(56 * Resources.DisplayMetrics.Density + 0.5f);
            FindViewById(Resource.Id.playerContainer).Alpha = 1;
            FindViewById(Resource.Id.smallPlayer).Alpha = 0;
            SheetBehavior.State = BottomSheetBehavior.StateExpanded;
            FindViewById<FrameLayout>(Resource.Id.contentView).SetPadding(0, 0, 0, DpToPx(70));
        }

        public void ShowSmallPlayer()
        {
            FindViewById<NestedScrollView>(Resource.Id.playerSheet).Visibility = ViewStates.Visible;
            FindViewById(Resource.Id.playerContainer).Alpha = 0;
            FindViewById(Resource.Id.smallPlayer).Alpha = 1;
            SheetBehavior.State = BottomSheetBehavior.StateCollapsed;
            Player.instance.RefreshPlayer();
            FindViewById<FrameLayout>(Resource.Id.contentView).SetPadding(0, 0, 0, DpToPx(70));
        }

        public void HideSmallPlayer()
        {
            SkipStop = true;
            FindViewById<FrameLayout>(Resource.Id.contentView).SetPadding(0, 0, 0, 0);
            SheetBehavior.State = BottomSheetBehavior.StateHidden;
            FindViewById<NestedScrollView>(Resource.Id.playerSheet).Visibility = ViewStates.Gone;
        }

        public bool HasReadPermission()
        {
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) == (int)Permission.Granted)
                return true;
            else
                return false;
        }

        public async Task<bool> GetReadPermission()
        {
            if (HasReadPermission())
                return true;
            PermissionGot = null;
            string[] permissions = new string[] { Manifest.Permission.ReadExternalStorage };
            RequestPermissions(permissions, RequestCode);

            while (PermissionGot == null)
                await Task.Delay(10);

            return (bool)PermissionGot;
        }

        public async Task<bool> GetWritePermission()
        {
            const string permission = Manifest.Permission.WriteExternalStorage;
            if (ContextCompat.CheckSelfPermission(this, permission) == (int)Permission.Granted)
            {
                return true;
            }
            PermissionGot = null;
            string[] permissions = new string[] { permission };
            RequestPermissions(permissions, RequestCode);

            while (PermissionGot == null)
                await Task.Delay(10);

            return (bool)PermissionGot;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == RequestCode)
            {
                if (grantResults.Length > 0)
                {
                    if (grantResults[0] == Permission.Granted)
                        PermissionGot = true;
                    else
                    {
                        PermissionGot = false;
                        Snackbar snackBar = Snackbar.Make(FindViewById<CoordinatorLayout>(Resource.Id.snackBar), Resource.String.no_permission, Snackbar.LengthLong);
                        snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                        snackBar.Show();
                    }
                }
            }
            else if (requestCode == WriteRequestCode)
            {
                if (grantResults[0] == Permission.Granted)
                    PermissionGot = true;
                else
                {
                    PermissionGot = false;
                    Snackbar snackBar = Snackbar.Make(FindViewById<CoordinatorLayout>(Resource.Id.snackBar), Resource.String.no_permission, Snackbar.LengthLong);
                    snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                    snackBar.Show();
                }
            }
        }

        public async static Task<string> GetBestThumb(string[] thumbnails)
        {
            foreach (string thumb in thumbnails)
            {
                HttpWebRequest request = new HttpWebRequest(new System.Uri(thumb))
                {
                    Method = "HEAD"
                };
                try
                {
                    HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();

                    if (response.StatusCode == HttpStatusCode.OK)
                        return thumb;
                }
                catch (WebException) { }
            }

            return thumbnails.Last();
        }

        public async void ShuffleAll()
        {
            List<Song> songs = new List<Song>();
            Android.Net.Uri musicUri = MediaStore.Audio.Media.ExternalContentUri;

            CursorLoader cursorLoader = new CursorLoader(this, musicUri, null, null, null, null);
            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();
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

                    songs.Add(new Song(Title, Artist, Album, null, AlbumArt, id, path));
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }

            if (songs.Count == 0)
            {
                Snackbar snackBar = Snackbar.Make(FindViewById<CoordinatorLayout>(Resource.Id.snackBar), Resource.String.no_song_mix, Snackbar.LengthLong);
                snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                snackBar.Show();
                return;
            }

            Intent intent = new Intent(this, typeof(MusicPlayer));
            StartService(intent);

            while (MusicPlayer.instance == null)
                await Task.Delay(10);

            MusicPlayer.instance.RandomPlay(songs, true);

            ShowSmallPlayer();
            ShowPlayer();
        }

        public void YoutubeEndPointChanged()
        {
            FindViewById<ProgressBar>(Resource.Id.ytProgress).Visibility = ViewStates.Gone;
            Snackbar snackBar = Snackbar.Make(FindViewById(Resource.Id.snackBar), Resource.String.youtube_endpoint, Snackbar.LengthLong);
            snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
            snackBar.Show();

            Player.instance.Ready();
        }

        public void Timout()
        {
            Snackbar snackBar = Snackbar.Make(FindViewById(Resource.Id.snackBar), Resource.String.timout, Snackbar.LengthLong);
            snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
            snackBar.Show();
        }

        public void UnknowError(Action action = null)
        {
            Snackbar snackBar = Snackbar.Make(FindViewById(Resource.Id.snackBar), Resource.String.unknow, Snackbar.LengthIndefinite);
            if (action != null)
                snackBar.SetAction("Try Again", (sender) => { action.Invoke();  snackBar.Dismiss(); });
            else
                snackBar.SetAction("Dismiss", (sender) => { snackBar.Dismiss(); });
            snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
            snackBar.Show();
        }

        public void Unplayable(string title, string msg)
        {
            if (msg.Contains("country"))
            {
                Snackbar snackBar = Snackbar.Make(FindViewById(Resource.Id.snackBar), title + " " + GetString(Resource.String.country_blocked), Snackbar.LengthLong);
                snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                snackBar.Show();
            }
            else if(msg.Contains("not available"))
            {
                Snackbar snackBar = Snackbar.Make(FindViewById(Resource.Id.snackBar), title + " " + GetString(Resource.String.not_streamable), Snackbar.LengthLong);
                snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                snackBar.Show();
            }
            else
                UnknowError();
        }

        public void NotStreamable(string title)
        {
            Snackbar snackBar = Snackbar.Make(FindViewById(Resource.Id.snackBar), title + GetString(Resource.String.not_streamable), Snackbar.LengthLong);
            snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
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

        bool HasWifi()
        {
            ConnectivityManager connectivityManager = (ConnectivityManager)Application.Context.GetSystemService(ConnectivityService);
            if(Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                Network network = connectivityManager.ActiveNetwork;
                if (network == null)
                    return false;

                NetworkCapabilities capabilities = connectivityManager.GetNetworkCapabilities(network);
                if (capabilities.HasTransport(TransportType.Wifi) || capabilities.HasTransport(TransportType.Ethernet))
                    return true;
            }
            else
            {
                Network[] allNetworks = connectivityManager.GetAllNetworks();
                for (int i = 0; i < allNetworks.Length; i++)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    if (allNetworks[i] != null && connectivityManager.GetNetworkInfo(allNetworks[i]).IsConnected && connectivityManager.GetNetworkInfo(allNetworks[i]).Type == ConnectivityType.Wifi)
#pragma warning restore CS0618 // Type or member is obsolete
                        return true;
                }
            }

            return false;
        }
        
        private async void SyncPlaylists()
        {
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(this);
            DateTime lastSync = DateTime.Parse(prefManager.GetString("syncDate", DateTime.MinValue.ToString()));

            if (lastSync.AddHours(1) > DateTime.Now || !HasWifi()) //Make a time check, do not check if the user is downloading or if the user has just started the app two times
                return;

            ISharedPreferencesEditor editor = prefManager.Edit();
            editor.PutString("syncDate", DateTime.Now.ToString());

            List<PlaylistItem> SyncedPlaylists = new List<PlaylistItem>();
            await Task.Run(() =>
            {
                SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "SyncedPlaylists.sqlite"));
                db.CreateTable<PlaylistItem>();

                SyncedPlaylists = db.Table<PlaylistItem>().ToList();
            });

            foreach (PlaylistItem item in SyncedPlaylists)
            {
                if (item.YoutubeID != null)
                {
                    YoutubeEngine.DownloadPlaylist(item.Name, item.YoutubeID, false);
                }
            }
        }

        public async static void CheckForUpdate(Activity activity, bool displayToast)
        {
            if(!HasInternet())
            {
                if (displayToast)
                {
                    if (instance != null && !instance.Paused)
                    {
                        Snackbar snackBar = Snackbar.Make(instance.FindViewById(Resource.Id.snackBar), activity.GetString(Resource.String.update_no_internet), Snackbar.LengthLong);
                        snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                        snackBar.Show();
                    }
                    if(Preferences.instance != null)
                    {
                        Snackbar snackBar = Snackbar.Make(Preferences.instance.FindViewById(Android.Resource.Id.Content), activity.GetString(Resource.String.update_no_internet), Snackbar.LengthLong);
                        snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                        snackBar.Show();
                    }
                    else
                        Toast.MakeText(Application.Context, Resource.String.update_no_internet, ToastLength.Short).Show();
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
            bool beta = false;

            using(WebClient client = new WebClient())
            {
                string GitVersion = await client.DownloadStringTaskAsync(new System.Uri(versionURI));
                gitVersionID = GitVersion.Substring(9, 5);
                string gitID = gitVersionID.Remove(1, 1);
                gitVersion = int.Parse(gitID.Remove(2, 1));
                bool.TryParse(GitVersion.Substring(GitVersion.IndexOf("Beta: ") + 6, GitVersion.IndexOf("Link: ")), out beta);
                downloadPath = GitVersion.Substring(GitVersion.IndexOf("Link: ") + 6);
            }

            if (gitVersion > version && !beta)
            {
                Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(activity, dialogTheme);
                builder.SetTitle(activity.GetString(Resource.String.update, gitVersionID));
                builder.SetMessage(activity.GetString(Resource.String.update_message));
                builder.SetPositiveButton(activity.GetString(Resource.String.ok), (sender, e) => { InstallUpdate(gitVersionID, false, downloadPath); });
                builder.SetNegativeButton(activity.GetString(Resource.String.later), (sender, e) => { });
                builder.Show();
            }
            else if (displayToast)
            {
                if (!beta)
                {
                    if ((instance != null && !instance.Paused) || Preferences.instance != null)
                    {
                        Snackbar snackBar;
                        if (Preferences.instance != null)
                            snackBar = Snackbar.Make(Preferences.instance.FindViewById(Android.Resource.Id.Content), activity.GetString(Resource.String.up_to_date), Snackbar.LengthLong);
                        else
                            snackBar = Snackbar.Make(instance.FindViewById(Resource.Id.snackBar), activity.GetString(Resource.String.up_to_date), Snackbar.LengthLong);
                        snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                        snackBar.Show();
                    }
                    else
                        Toast.MakeText(Application.Context, Resource.String.up_to_date, ToastLength.Short).Show();
                }
                else
                {
                    if ((instance != null && !instance.Paused) || Preferences.instance != null)
                    {
                        Snackbar snackBar;
                        if (Preferences.instance != null)
                            snackBar = Snackbar.Make(Preferences.instance.FindViewById(Android.Resource.Id.Content), activity.GetString(Resource.String.beta_available), Snackbar.LengthLong);
                        else
                            snackBar = Snackbar.Make(instance.FindViewById(Resource.Id.snackBar), activity.GetString(Resource.String.beta_available), Snackbar.LengthLong);
                        snackBar.SetAction(activity.GetString(Resource.String.download), (sender) =>
                        {
                            InstallUpdate(gitVersionID, true, downloadPath);
                        });
                        snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                        snackBar.Show();
                    }
                    else
                        Toast.MakeText(Application.Context, Resource.String.beta_available, ToastLength.Short).Show();
                }
            }
        }

        public async static void InstallUpdate(string version, bool beta, string downloadPath)
        {
            if (await instance.GetWritePermission())
            {
                string localPath = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDownloads).AbsolutePath + "/Opus-v" + version + (beta ? "-beta" : "") + ".apk";

                Toast.MakeText(Application.Context, Application.Context.GetString(Resource.String.downloading_update), ToastLength.Short).Show();

                NotificationCompat.Builder notification = new NotificationCompat.Builder(Application.Context, "Opus.Channel")
                    .SetVisibility(NotificationCompat.VisibilityPublic)
                    .SetSmallIcon(Resource.Drawable.NotificationIcon)
                    .SetContentTitle(Application.Context.GetString(Resource.String.updating))
                    .SetOngoing(true);

                NotificationManager notificationManager = (NotificationManager)Application.Context.GetSystemService(NotificationService);
                notificationManager.Notify(NotifUpdateID, notification.Build());

                using (WebClient client = new WebClient())
                {
                    await client.DownloadFileTaskAsync(downloadPath, localPath);
                }

                notificationManager.Cancel(NotifUpdateID);

                Intent intent = new Intent(Intent.ActionView);
                intent.SetDataAndType(FileProvider.GetUriForFile(instance, Application.Context.PackageName + ".provider", new Java.IO.File(localPath)), "application/vnd.android.package-archive");
                intent.SetFlags(ActivityFlags.NewTask);
                intent.AddFlags(ActivityFlags.GrantReadUriPermission);
                Application.Context.StartActivity(intent);
            }
        }

        protected override void OnStart()
        {
            base.OnStart();
            CastContext = CastContext.GetSharedInstance(this);
            CastContext.SessionManager.AddSessionManagerListener(this);
        }

        protected override void OnResume()
        {
            base.OnResume();
            Paused = false;
            instance = this;

            if (CastContext.SessionManager.CurrentSession == null && MusicPlayer.CurrentID() == -1)
                MusicPlayer.currentID = MusicPlayer.RetrieveQueueSlot();
            else if(MusicPlayer.UseCastPlayer)
                MusicPlayer.GetQueueFromCast();

            if (SearchableActivity.instance != null && SearchableActivity.instance.SearchQuery != null && SearchableActivity.instance.SearchQuery != "")
            {
//                if (YoutubeEngine.instances != null)
//                {
//#pragma warning disable CS4014
//                    foreach (YoutubeEngine instance in YoutubeEngine.instances)
//                        instance.Search(SearchableActivity.instance.SearchQuery, instance.querryType, true);

//                    SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, Pager.instance).AddToBackStack("Youtube").Commit();
//                }
//                else
                    SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, Pager.NewInstance(SearchableActivity.instance.SearchQuery, 0)).AddToBackStack("Youtube").Commit();

                SearchableActivity.instance = null;
            }

            if (SheetBehavior != null && SheetBehavior.State == BottomSheetBehavior.StateExpanded)
                FindViewById<NestedScrollView>(Resource.Id.playerSheet).Visibility = ViewStates.Visible;
        }

        protected override void OnDestroy()
        {
            YoutubeEngine.instances = null;

            if (MusicPlayer.instance != null && !MusicPlayer.isRunning && Preferences.instance == null && EditMetaData.instance == null)
            {
                Intent intent = new Intent(this, typeof(MusicPlayer));
                intent.SetAction("Stop");
                StartService(intent);
            }
            base.OnDestroy();
        }

        protected override void OnPause()
        {
            base.OnPause();
            Paused = true;
        }

        public override void OnBackPressed()
        {
            if (Player.instance?.DrawerLayout.IsDrawerOpen((int)GravityFlags.Start) == true)
                Player.instance?.DrawerLayout.CloseDrawer((int)GravityFlags.Start);
            else if (SheetBehavior.State == BottomSheetBehavior.StateExpanded)
                SheetBehavior.State = BottomSheetBehavior.StateCollapsed;
            else
                base.OnBackPressed();
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            HandleIntent(intent);
        }


        //SessionManagerListener
        public void OnSessionEnded(Java.Lang.Object session, int error)
        {
            Console.WriteLine("&Session Ended");
            SwitchRemote(null);
        }

        public void OnSessionEnding(Java.Lang.Object session) { }

        public void OnSessionResumeFailed(Java.Lang.Object session, int error) { }

        public void OnSessionResumed(Java.Lang.Object session, bool wasSuspended)
        {
            Console.WriteLine("&Session Resumed");
            SwitchRemote(((CastSession)session).RemoteMediaClient, false);
        }

        public void OnSessionResuming(Java.Lang.Object session, string sessionId) { }

        public void OnSessionStartFailed(Java.Lang.Object session, int error) { }

        public void OnSessionStarted(Java.Lang.Object session, string sessionId)
        {
            Console.WriteLine("&Session Started");
            SwitchRemote(((CastSession)session).RemoteMediaClient, true);
        }

        public void OnSessionStarting(Java.Lang.Object session) { }

        public void OnSessionSuspended(Java.Lang.Object session, int reason)
        {
            Console.WriteLine("&Session Suspended");
            SwitchRemote(null);
        }

        private async void SwitchRemote(RemoteMediaClient remoteClient, bool justStarted = true)
        {
            Console.WriteLine("&Switching to another remote player: (null check)" + (remoteClient == null));

            MusicPlayer.Initialized = false;
            if (remoteClient != null)
            {
                MusicPlayer.RemotePlayer = remoteClient;

                if (MusicPlayer.CastCallback == null)
                {
                    MusicPlayer.CastCallback = new CastCallback();
                    MusicPlayer.RemotePlayer.RegisterCallback(MusicPlayer.CastCallback);
                }
                if (MusicPlayer.CastQueueManager == null)
                {
                    MusicPlayer.CastQueueManager = new CastQueueManager();
                    MusicPlayer.RemotePlayer.MediaQueue.RegisterCallback(MusicPlayer.CastQueueManager);
                }
            }
            else
            {
                if (MusicPlayer.CastCallback != null)
                {
                    MusicPlayer.RemotePlayer.UnregisterCallback(MusicPlayer.CastCallback);
                    MusicPlayer.CastCallback = null;
                }
                if (MusicPlayer.CastQueueManager != null)
                {
                    MusicPlayer.RemotePlayer.MediaQueue.UnregisterCallback(MusicPlayer.CastQueueManager);
                    MusicPlayer.CastQueueManager = null;
                }

                MusicPlayer.RemotePlayer = remoteClient;
                MusicPlayer.isRunning = false;
                Player.instance.RefreshPlayer();
            }

            MusicPlayer.UseCastPlayer = MusicPlayer.RemotePlayer != null;

            await Task.Delay(1000);
            if (MusicPlayer.UseCastPlayer)
            {
                if (justStarted && MusicPlayer.RemotePlayer.MediaQueue.ItemCount == 0)
                {
                    Intent intent = new Intent(this, typeof(MusicPlayer));
                    intent.SetAction("StartCasting");
                    StartService(intent);
                }
                else
                {
                    MusicPlayer.Initialized = true;
                    MusicPlayer.GetQueueFromCast();
                }
            }
        }
    }
}