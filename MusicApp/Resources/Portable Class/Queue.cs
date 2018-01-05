using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;

namespace MusicApp.Resources.Portable_Class
{
    public class Queue: ListFragment
    {
        public static Queue instance;
        public Adapter adapter;
        public View emptyView;

        private View view;
        private bool isEmpty = false;

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            emptyView = LayoutInflater.Inflate(Resource.Layout.NoQueue, null);
            ListView.EmptyView = emptyView;
            ListView.Scroll += MainActivity.instance.Scroll;
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
            MainActivity.instance.OnPaddingChanged += PaddingChanged;

            PopulateView();
        }

        private void PaddingChanged(object sender, PaddingChange e)
        {
            view.SetPadding(0, 0, 0, MainActivity.paddingBot);
            //view.Invalidate();
        }

        public override void OnDestroy()
        {
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
            MainActivity.instance.OnPaddingChanged -= PaddingChanged;
            if (isEmpty)
            {
                ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
                rootView.RemoveView(emptyView);
            }
            base.OnDestroy();
            instance = null;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = base.OnCreateView(inflater, container, savedInstanceState);
            this.view = view;
            view.SetPadding(0, 0, 0, MainActivity.paddingBot);
            return view;
        }

        public static Fragment NewInstance()
        {
            instance = new Queue { Arguments = new Bundle() };
            return instance;
        }

        void PopulateView()
        {
            adapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, MusicPlayer.queue);
            ListAdapter = adapter;

            ListView.TextFilterEnabled = true;
            ListView.ItemClick += ListView_ItemClick;
            ListView.ItemLongClick += ListView_ItemLongClick;

            if (adapter == null || adapter.Count == 0)
            {
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
        }

        private void OnRefresh(object sender, System.EventArgs e)
        {
            Refresh();
            MainActivity.instance.contentRefresh.Refreshing = false;
        }

        public void Refresh()
        {
            adapter = new Adapter(Android.App.Application.Context, Resource.Layout.SongList, MusicPlayer.queue);
            ListAdapter = adapter;

            if (adapter == null || adapter.Count == 0)
            {
                if (isEmpty)
                    return;
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            Song item = MusicPlayer.queue[e.Position];

            MusicPlayer.instance.SwitchQueue(item);
        }

        private void ListView_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            More(MusicPlayer.queue[e.Position]);
        }

        public void More(Song item)
        {
            Toast.MakeText(Android.App.Application.Context, "Comming Soon", ToastLength.Short).Show();
        }
    }
}