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

namespace MusicApp
{
    [Activity(Label = "MusicApp", MainLauncher = true, Icon = "@drawable/MusicIcon", Theme = "@style/Theme")]
    public class MainActivity : AppCompatActivity
    {
        public static MainActivity instance;
        public Android.Support.V7.Widget.Toolbar ToolBar;
        public IMenu menu;

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
            HideTabs();
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
            }
            else if(item.ItemId == Resource.Id.settings)
            {
                //HideTabs();
                //SupportActionBar.SetDisplayHomeAsUpEnabled(true);
                //FindViewById<BottomNavigationView>(Resource.Id.bottomView).Visibility = ViewStates.Gone;
                //SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, fragment).AddToBackStack(null).Commit();

                //FragmentTransaction transaction = FragmentManager.BeginTransaction();
                //transaction.Replace(Resource.Id.contentView, fragment);
                //transaction.AddToBackStack(null);
                //transaction.Commit();

                Intent intent = new Intent(Android.App.Application.Context, typeof(Preferences));
                StartActivity(intent);
                //startActivity(mainIntent);
            }
            return base.OnOptionsItemSelected(item);
        }

        public void CreateSearch(int requestID)
        {
            var item = menu.FindItem(Resource.Id.search);
            item.SetVisible(true);
            var searchItem = MenuItemCompat.GetActionView(item);
            var searchView = searchItem.JavaCast<Android.Support.V7.Widget.SearchView>();

            if (requestID == 0)
                searchView.QueryTextChange += (s, e) =>
                {
                    if(Browse.instance != null)
                        Browse.instance.Search(e.NewText);
                };
            if (requestID == 1)
                searchView.QueryTextChange += (s, e) =>
                {
                    if(PlaylistTracks.instance != null)
                        PlaylistTracks.instance.Search(e.NewText);
                };
            if (requestID == 2)
                searchView.QueryTextSubmit += (s, e) =>
                {
                    if(DownloadList.instance != null)
                        DownloadList.instance.Search(e.Query);
                };

            searchView.QueryTextSubmit += (s, e) =>
            {
                e.Handled = true;
            };
        }

        public void RemoveSearchService(int requestID)
        {
            var item = menu.FindItem(Resource.Id.search);
            item.SetVisible(false);
            var searchItem = MenuItemCompat.GetActionView(item);
            var searchView = searchItem.JavaCast<Android.Support.V7.Widget.SearchView>();

            searchView.ClearFocus();

            if (requestID == 0)
                searchView.QueryTextChange -= (s, e) => Browse.instance.Search(e.NewText);
            if (requestID == 1)
                searchView.QueryTextChange -= (s, e) => PlaylistTracks.instance.Search(e.NewText);
            if (requestID == 2)
                searchView.QueryTextSubmit -= (s, e) => DownloadList.instance.Search(e.Query);
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
                    fragment = Queue.NewInstance();
                    break;

                case Resource.Id.browseLayout:
                    SetTabs();
                    break;

                case Resource.Id.downloadLayout:
                    HideTabs();
                    fragment = DownloadList.NewInstance();
                    break;

                case Resource.Id.playlistLayout:
                    HideTabs();
                    fragment = Playlist.NewInstance();
                    break;
            }

            if (fragment == null)
                fragment = EmptyFragment.NewInstance();

            SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, fragment).Commit();
        }

        void SetTabs()
        {
            FrameLayout frame = FindViewById<FrameLayout>(Resource.Id.contentView);
            frame.Visibility = ViewStates.Gone;

            TabLayout tabs = FindViewById<TabLayout>(Resource.Id.tabs);
            tabs.Visibility = ViewStates.Visible;
            tabs.RemoveAllTabs();
            tabs.AddTab(tabs.NewTab().SetText("Songs"));
            tabs.AddTab(tabs.NewTab().SetText("Artist"));
            tabs.AddTab(tabs.NewTab().SetText("Folders"));
            ViewPager pager = FindViewById<ViewPager>(Resource.Id.pager);
            pager.SetPadding(0, 200, 0, 0);
            pager.AddOnPageChangeListener(new TabLayout.TabLayoutOnPageChangeListener(tabs));
            ViewPagerAdapter adapter = new ViewPagerAdapter(SupportFragmentManager);

            adapter.AddFragment(Browse.NewInstance(), "Songs");
            adapter.AddFragment(DownloadFragment.NewInstance(), "Artists");
            adapter.AddFragment(FolderBrowse.NewInstance(), "Folders");

            pager.Adapter = adapter;
            tabs.SetupWithViewPager(pager);
            //tabs.TabSelected += (sender, e) => { Console.WriteLine("Clicked"); pager.SetCurrentItem(e.Tab.Position, true); };
        }

        void HideTabs()
        {
            TabLayout tabs = FindViewById<TabLayout>(Resource.Id.tabs);
            tabs.RemoveAllTabs();
            tabs.Visibility = ViewStates.Gone;
            ViewPager pager = FindViewById<ViewPager>(Resource.Id.pager);
            pager.Adapter = null;

            FrameLayout frame = FindViewById<FrameLayout>(Resource.Id.contentView);
            frame.Visibility = ViewStates.Visible;
        }
    }
}