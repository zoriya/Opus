using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Views;

namespace MusicApp.Resources.Portable_Class
{
    public class Pager : Fragment, ViewPager.IOnPageChangeListener
    {
        public static Pager instance;
        private ViewPagerAdapter adapter;
        private int type;
        private int pos;

        public static Fragment NewInstance(int type, int pos)
        {
            instance = new Pager { Arguments = new Bundle() };
            instance.type = type;
            instance.pos = pos;
            return instance;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            //if(savedInstanceState != null)
            //{
            //    System.Console.WriteLine("&Instance state restored");
            //    type = savedInstanceState.GetInt("type");
            //    pos = savedInstanceState.GetInt("pos");
            //}

            View view = inflater.Inflate(Resource.Layout.ViewPager, container, false);
            TabLayout tabs = Activity.FindViewById<TabLayout>(Resource.Id.tabs);
            ViewPager pager = view.FindViewById<ViewPager>(Resource.Id.pager);

            ((AppBarLayout.LayoutParams)Activity.FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsingToolbar).LayoutParameters).ScrollFlags = AppBarLayout.LayoutParams.ScrollFlagScroll | AppBarLayout.LayoutParams.ScrollFlagEnterAlways | AppBarLayout.LayoutParams.ScrollFlagSnap;
            tabs.Visibility = ViewStates.Visible;
            tabs.RemoveAllTabs();

            if (type == 0)
            {
                tabs.AddTab(tabs.NewTab().SetText(Resources.GetString(Resource.String.songs)));
                tabs.AddTab(tabs.NewTab().SetText(Resources.GetString(Resource.String.folders)));

                adapter = new ViewPagerAdapter(ChildFragmentManager);
                adapter.AddFragment(Browse.NewInstance(), Resources.GetString(Resource.String.songs));
                adapter.AddFragment(FolderBrowse.NewInstance(), Resources.GetString(Resource.String.folders));

                pager.Adapter = adapter;
                pager.AddOnPageChangeListener(this);
                pager.AddOnPageChangeListener(new TabLayout.TabLayoutOnPageChangeListener(tabs));

                tabs.SetupWithViewPager(pager);
                tabs.TabReselected += OnTabReselected;

                pager.CurrentItem = pos;
                tabs.TabMode = TabLayout.ModeFixed;
                tabs.SetScrollPosition(pos, 0f, true);
            }
            else if (type == 1 || type == 2)
            {
                tabs.AddTab(tabs.NewTab().SetText(Resources.GetString(Resource.String.all)));
                tabs.AddTab(tabs.NewTab().SetText(Resources.GetString(Resource.String.tracks)));
                tabs.AddTab(tabs.NewTab().SetText(Resources.GetString(Resource.String.playlists)));
                tabs.AddTab(tabs.NewTab().SetText(Resources.GetString(Resource.String.lives)));
                tabs.AddTab(tabs.NewTab().SetText(Resources.GetString(Resource.String.channels)));

                ViewPagerAdapter adapter = new ViewPagerAdapter(ChildFragmentManager);
                Fragment[] fragment = YoutubeEngine.NewInstances(YoutubeEngine.searchKeyWorld);
                adapter.AddFragment(fragment[0], Resources.GetString(Resource.String.all));
                adapter.AddFragment(fragment[1], Resources.GetString(Resource.String.tracks));
                adapter.AddFragment(fragment[2], Resources.GetString(Resource.String.playlists));
                adapter.AddFragment(fragment[3], Resources.GetString(Resource.String.lives));
                adapter.AddFragment(fragment[4], Resources.GetString(Resource.String.channels));

                pager.Adapter = adapter;
                pager.AddOnPageChangeListener(this);
                pager.AddOnPageChangeListener(new TabLayout.TabLayoutOnPageChangeListener(tabs));
                tabs.SetupWithViewPager(pager);
                tabs.TabReselected += OnTabReselected;

                pager.CurrentItem = pos;
                tabs.TabMode = TabLayout.ModeScrollable;
                tabs.SetScrollPosition(pos, 0f, true);

                YoutubeEngine.instances[pos].focused = true;
                YoutubeEngine.instances[pos].OnFocus();
            }
            return view;
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
            if (YoutubeEngine.instances != null)
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

        public void OnPageScrollStateChanged(int state)
        {
            MainActivity.instance.contentRefresh.Enabled = state == ViewPager.ScrollStateIdle;
        }

        public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels) { }

        public void OnPageSelected(int position)
        {
            if (Browse.instance != null)
            {
                if (!FolderBrowse.instance.populated)
                    FolderBrowse.instance.PopulateList();

                if (position == 0)
                {
                    if (Browse.instance.focused)
                        Browse.instance.ListView.SmoothScrollToPosition(0);

                    Browse.instance.focused = true;
                    FolderBrowse.instance.focused = false;
                    MainActivity.instance.DisplaySearch();
                }
                else if (position == 1)
                {
                    if (FolderBrowse.instance.focused)
                        FolderBrowse.instance.ListView.SmoothScrollToPosition(0);

                    Browse.instance.focused = false;
                    FolderBrowse.instance.focused = true;
                    MainActivity.instance.HideSearch();
                }
            }
            else if (YoutubeEngine.instances != null)
            {
                foreach (YoutubeEngine instance in YoutubeEngine.instances)
                {
                    if (instance.focused)
                        instance.OnUnfocus();
                    instance.focused = false;
                }
                YoutubeEngine.instances[position].focused = true;
                YoutubeEngine.instances[position].OnFocus();
            }
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();
            Browse.instance = null;
            FolderBrowse.instance = null;
            YoutubeEngine.instances = null;

            adapter?.Dispose();

            TabLayout tabs = Activity.FindViewById<TabLayout>(Resource.Id.tabs);
            tabs.RemoveAllTabs();
            tabs.Visibility = ViewStates.Gone;

            ((AppBarLayout.LayoutParams)Activity.FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsingToolbar).LayoutParameters).ScrollFlags = 0;

            instance = null;
        }

        //public override void OnSaveInstanceState(Bundle outState)
        //{
        //    base.OnSaveInstanceState(outState);
        //    System.Console.WriteLine("&Pager insatnce state saved");
        //    outState.PutInt("type", type);
        //    outState.PutInt("pos", View.FindViewById<ViewPager>(Resource.Id.pager).CurrentItem);
        //}
    }
}