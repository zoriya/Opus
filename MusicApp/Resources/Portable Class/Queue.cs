using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicApp.Resources.Portable_Class
{
    public class Queue : Fragment
    {
        public static Queue instance;
        public RecyclerView ListView;
        public RecyclerAdapter adapter;
        public View emptyView;
        public View recyclerFragment;

        private bool isEmpty = false;
        private string[] actions = new string[] { "Remove from queue" };

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            emptyView = LayoutInflater.Inflate(Resource.Layout.NoQueue, null);
            base.OnActivityCreated(savedInstanceState);
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
            MainActivity.instance.OnPaddingChanged += PaddingChanged;
        }

        private void ListView_ScrollChange(object sender, View.ScrollChangeEventArgs e)
        {
            
        }

        private void PaddingChanged(object sender, PaddingChange e)
        {
            //view.SetPadding(0, 0, 0, MainActivity.paddingBot); //zegizrgbnzeg
        }

        public override void OnDestroy()
        {
            MainActivity.instance.contentRefresh.Refresh -= OnRefresh;
            MainActivity.instance.OnPaddingChanged -= PaddingChanged;
            ViewGroup rootView = Activity.FindViewById<ViewGroup>(Android.Resource.Id.Content);
            rootView.RemoveView(recyclerFragment);
            if (isEmpty)
                rootView.RemoveView(emptyView);

            base.OnDestroy();
            instance = null;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View v = inflater.Inflate(Resource.Layout.RecyclerFragment, container, false);
            ListView = v.FindViewById<RecyclerView>(Resource.Id.recycler);
            System.Console.WriteLine("&ListView initated");
            //v.SetPadding(0, 0, 0, MainActivity.paddingBot);
            ListView.SetLayoutManager(new LinearLayoutManager(Android.App.Application.Context));

            if (MusicPlayer.queue != null)
                adapter = new RecyclerAdapter(MusicPlayer.queue.ToArray());
            else
                adapter = new RecyclerAdapter(new List<Song>().ToArray());
            ListView.SetAdapter(adapter);
            adapter.ItemClick += ListView_ItemClick;
            adapter.ItemLongCLick += ListView_ItemLongClick;
            ListView.SetItemAnimator(new DefaultItemAnimator());
            ListView.ScrollChange += MainActivity.instance.Scroll; //egohzogjbezgkezbjgjbezogzebglzjgbezljbg

            if (adapter == null || adapter.ItemCount == 0)
            {
                if (isEmpty)
                    return v;
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
            return v;
        }

        public static Fragment NewInstance()
        {
            instance = new Queue { Arguments = new Bundle() };
            return instance;
        }

        private void OnRefresh(object sender, System.EventArgs e)
        {
            Refresh();
            MainActivity.instance.contentRefresh.Refreshing = false;
        }

        public void Refresh()
        {
            adapter = new RecyclerAdapter(MusicPlayer.queue.ToArray());
            adapter.ItemClick += ListView_ItemClick;
            adapter.ItemLongCLick += ListView_ItemLongClick;
            ListView.SetAdapter(adapter);

            if (adapter == null || adapter.ItemCount == 0)
            {
                if (isEmpty)
                    return;
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
        }

        private async void ListView_ItemClick(object sender, int Position)
        {
            Song item = MusicPlayer.queue[Position];

            MusicPlayer.instance.SwitchQueue(item);

            if (item.IsYt && !item.isParsed)
            {
                while (MusicPlayer.queue[MusicPlayer.CurrentID()].GetName() != item.GetName())
                    await Task.Delay(10);

                ListView.GetChildAt(Position - ((LinearLayoutManager)ListView.GetLayoutManager()).FindFirstVisibleItemPosition()).FindViewById<ImageView>(Resource.Id.youtubeIcon).SetImageResource(Resource.Drawable.youtubeIcon);
            }
        }

        private void ListView_ItemLongClick(object sender, int Position)
        {
            More(MusicPlayer.queue[Position]);
        }

        public void More(Song item)
        {
            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(Activity, Resource.Style.AppCompatAlertDialogStyle);
            builder.SetTitle("Pick an action");
            builder.SetItems(actions, (senderAlert, args) =>
            {
                switch (args.Which)
                {
                    case 0:
                        RemoveFromQueue(item);
                        break;
                    default:
                        break;
                }
            });
            builder.Show();
        }

        void RemoveFromQueue(Song item)
        {
            foreach(Song song in MusicPlayer.queue)
            {
                if (song.queueSlot > item.queueSlot)
                    song.queueSlot--;
            }

            MusicPlayer.queue.Remove(item);
        }
    }
}