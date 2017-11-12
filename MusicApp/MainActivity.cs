using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.Design.Widget;
using MusicApp.Resources.Fragments;
using MusicApp.Resources.Portable_Class;
using Android.Views;
using Android.Support.V4.View;
using Android.Runtime;
using Android.Widget;
using Android.Content;
using MusicApp.Resources.values;
using Square.Picasso;
using System;

using SearchView = Android.Support.V7.Widget.SearchView;

namespace MusicApp
{
    [Activity(Label = "MusicApp", MainLauncher = true, Icon = "@drawable/MusicIcon", Theme = "@style/Theme")]
    public class MainActivity : AppCompatActivity
    {
        public static MainActivity instance;
        public Android.Support.V7.Widget.Toolbar ToolBar;
        public IMenu menu;

        private Handler handler = new Handler();
        private ProgressBar bar;
        private bool prepared = false;


        public static int paddingBot
        {
            get
            {
                if (((FrameLayout)instance.FindViewById(Resource.Id.smallPlayer).Parent).Visibility == ViewStates.Gone)
                    return instance.FindViewById<BottomNavigationView>(Resource.Id.bottomView).Height;
                else
                    return instance.FindViewById<BottomNavigationView>(Resource.Id.bottomView).Height + ((FrameLayout)instance.FindViewById(Resource.Id.smallPlayer).Parent).Height;
            }
        }


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Main);

            instance = this;

            var bottomNavigation = FindViewById<BottomNavigationView>(Resource.Id.bottomView);
            bottomNavigation.NavigationItemSelected += PreNavigate;

            Navigate(Resource.Id.musicLayout);

            ToolBar = (Android.Support.V7.Widget.Toolbar) FindViewById(Resource.Id.toolbar);
            SetSupportActionBar(ToolBar);
            SupportActionBar.Title = "MusicApp";
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
                    var item2 = menu.FindItem(Resource.Id.search);
                    item2.SetVisible(false);
                    if (PlaylistTracks.instance.isEmpty)
                    {
                        ViewGroup rootView = FindViewById<ViewGroup>(Android.Resource.Id.Content);
                        rootView.RemoveView(PlaylistTracks.instance.emptyView);
                    }
                    SupportActionBar.SetHomeButtonEnabled(false);
                    SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                    SupportActionBar.Title = "MusicApp";
                    Navigate(Resource.Id.playlistLayout);
                }
                if (FolderTracks.instance != null)
                {
                    var item2 = menu.FindItem(Resource.Id.search);
                    item2.SetVisible(false);
                    if (FolderTracks.instance.isEmpty)
                    {
                        ViewGroup rootView = FindViewById<ViewGroup>(Android.Resource.Id.Content);
                        rootView.RemoveView(PlaylistTracks.instance.emptyView);
                    }
                    SupportActionBar.SetHomeButtonEnabled(false);
                    SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                    SupportActionBar.Title = "MusicApp";
                    FolderTracks.instance = null;
                    SetTabs(1);
                }
            }
            else if(item.ItemId == Resource.Id.search)
            {
                var searchItem = MenuItemCompat.GetActionView(item);
                var searchView = searchItem.JavaCast<Android.Support.V7.Widget.SearchView>();

                searchView.QueryTextChange += Search;

                searchView.QueryTextSubmit += (s, e) =>
                {
                    if (YoutubeEngine.instance != null)
                        YoutubeEngine.instance.Search(e.Query);

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
            if (menu == null)
                return;

            var item = menu.FindItem(Resource.Id.search);
            var searchItem = MenuItemCompat.GetActionView(item);
            var searchView = searchItem.JavaCast<Android.Support.V7.Widget.SearchView>();

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
            var item = menu.FindItem(Resource.Id.search);
            item.SetVisible(true);
            item.CollapseActionView();
            var searchItem = MenuItemCompat.GetActionView(item);
            var searchView = searchItem.JavaCast<Android.Support.V7.Widget.SearchView>();

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
                    HideTabs();
                    HideSearch();
                    fragment = Queue.NewInstance();
                    break;

                case Resource.Id.browseLayout:
                    SetTabs();
                    break;

                case Resource.Id.downloadLayout:
                    HideTabs();
                    DisplaySearch();
                    fragment = YoutubeEngine.NewInstance();
                    break;

                case Resource.Id.playlistLayout:
                    HideTabs();
                    HideSearch();
                    fragment = Playlist.NewInstance();
                    break;
            }

            if (fragment == null)
                fragment = EmptyFragment.NewInstance();

            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, fragment).Commit();
        }

        void SetTabs(int selectedTab = 0)
        {
            FrameLayout frame = FindViewById<FrameLayout>(Resource.Id.contentView);
            frame.Visibility = ViewStates.Gone;

            TabLayout tabs = FindViewById<TabLayout>(Resource.Id.tabs);
            tabs.Visibility = ViewStates.Visible;
            tabs.RemoveAllTabs();
            tabs.AddTab(tabs.NewTab().SetText("Songs"));
            tabs.AddTab(tabs.NewTab().SetText("Folders"));
            ViewPager pager = FindViewById<ViewPager>(Resource.Id.pager);
            pager.SetPadding(0, 200, 0, 0);
            pager.AddOnPageChangeListener(new TabLayout.TabLayoutOnPageChangeListener(tabs));
            ViewPagerAdapter adapter = new ViewPagerAdapter(SupportFragmentManager);

            adapter.AddFragment(Browse.NewInstance(), "Songs");
            adapter.AddFragment(FolderBrowse.NewInstance(), "Folders");

            pager.Adapter = adapter;
            tabs.SetupWithViewPager(pager);

            pager.CurrentItem = selectedTab;
            tabs.SetScrollPosition(selectedTab, 0f, true);
        }

        public void HideTabs()
        {
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

            FrameLayout frame = FindViewById<FrameLayout>(Resource.Id.contentView);
            frame.Visibility = ViewStates.Visible;
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
                Picasso.With(Application.Context).Load(current.GetAlbum()).Placeholder(Resource.Drawable.MusicIcon).Into(art);
            }
            else
            {
                var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                var songAlbumArtUri = ContentUris.WithAppendedId(songCover, current.GetAlbumArt());

                Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Into(art);
            }

            SetSmallPlayerProgressBar();

            if (!prepared)
            {
                smallPlayer.FindViewById<ImageButton>(Resource.Id.spLast).Click += Last_Click;
                smallPlayer.FindViewById<ImageButton>(Resource.Id.spPlay).Click += Play_Click;
                smallPlayer.FindViewById<ImageButton>(Resource.Id.spNext).Click += Next_Click;

                smallPlayer.FindViewById<LinearLayout>(Resource.Id.spContainer).Click += Container_Click;
                prepared = true;
            }
        }

        public void ShowSmallPlayer()
        {
            RelativeLayout smallPlayer = FindViewById<RelativeLayout>(Resource.Id.smallPlayer);
            FrameLayout parent = (FrameLayout)smallPlayer.Parent;
            parent.Visibility = ViewStates.Visible;
            smallPlayer.Visibility = ViewStates.Visible;
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

        public void HideSmallPlayer()
        {
            RelativeLayout smallPlayer = FindViewById<RelativeLayout>(Resource.Id.smallPlayer);
            FrameLayout parent = (FrameLayout)smallPlayer.Parent;
            parent.Visibility = ViewStates.Gone;
        }
    }
}