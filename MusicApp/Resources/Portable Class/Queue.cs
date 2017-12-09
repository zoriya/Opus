using Android.Content;
using Android.OS;
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

            PopulateView();
        }

        public override void OnDestroy()
        {
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
            view.SetPadding(0, MainActivity.paddinTop, 0, MainActivity.paddingBot);
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


            if (adapter == null || adapter.Count == 0)
            {
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
        }

        public void Refresh()
        {

        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            Song item = MusicPlayer.queue[e.Position];

            MusicPlayer.instance.SwitchQueue(item);
        }
    }
}