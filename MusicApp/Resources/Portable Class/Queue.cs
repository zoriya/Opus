using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicApp.Resources.Portable_Class
{
    public class Queue : Fragment
    {
        public static Queue instance;
        public RecyclerView ListView;
        public RecyclerAdapter adapter;
        public bool isEmpty = false;
        public View emptyView;
        public View recyclerFragment;
        public ItemTouchHelper itemTouchHelper;
        public View view;

        private string[] actions = new string[] { "Remove from queue", "Edit Metadata" };

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            MainActivity.instance.contentRefresh.Refresh += OnRefresh;
            MainActivity.instance.OnPaddingChanged += PaddingChanged;
            emptyView = LayoutInflater.Inflate(Resource.Layout.NoQueue, null);
        }

        private void PaddingChanged(object sender, PaddingChange e)
        {
            view.SetPadding(0, 0, 0, MainActivity.paddingBot);
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
            view = inflater.Inflate(Resource.Layout.RecyclerFragment, container, false);
            ListView = view.FindViewById<RecyclerView>(Resource.Id.recycler);
            view.SetPadding(0, 0, 0, MainActivity.paddingBot);
            ListView.SetLayoutManager(new LinearLayoutManager(Android.App.Application.Context));

            if (MusicPlayer.queue != null)
                adapter = new RecyclerAdapter(MusicPlayer.queue);
            else
                adapter = new RecyclerAdapter(new List<Song>());

            ListView.SetAdapter(adapter);
            adapter.ItemClick += ListView_ItemClick;
            adapter.ItemLongCLick += ListView_ItemLongCLick;
            ListView.SetItemAnimator(new DefaultItemAnimator());
            ListView.ScrollChange += MainActivity.instance.Scroll;

            ItemTouchHelper.Callback callback = new ItemTouchCallback(adapter);
            itemTouchHelper = new ItemTouchHelper(callback);
            itemTouchHelper.AttachToRecyclerView(ListView);


            if (adapter == null || adapter.ItemCount == 0)
            {
                if (isEmpty)
                    return view;

                isEmpty = true;
                return LayoutInflater.Inflate(Resource.Layout.NoQueue, container, false);
            }
            return view;
        }

        public async void AddEmptyView()
        {
            await Task.Delay(500);
            Activity.AddContentView(emptyView, View.LayoutParameters);
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
            adapter.UpdateList(MusicPlayer.queue);

            if (adapter == null || adapter.ItemCount == 0)
            {
                if (isEmpty)
                    return;
                isEmpty = true;
                if(emptyView == null)
                    emptyView = LayoutInflater.Inflate(Resource.Layout.NoQueue, null);
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
        }

        public void LoadMore()
        {
            List<Song> songList = MusicPlayer.queue.Except(adapter.songList).ToList();
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

        private void ListView_ItemLongCLick(object sender, int e)
        {
            MainActivity.instance.contentRefresh.SetEnabled(false);
            adapter.DisableRefresh(true);
        }

        public void More(Song item)
        {
            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Pick an action");
            builder.SetItems(actions, (senderAlert, args) =>
            {
                switch (args.Which)
                {
                    case 0:
                        RemoveFromQueue(item);
                        break;
                    case 1:
                        if (!item.IsYt)
                            Browse.EditMetadata(item, "Queue", ListView.GetLayoutManager().OnSaveInstanceState());
                        break;
                    default:
                        break;
                }
            });
            builder.Show();
        }

        public static void RemoveFromQueue(Song item)
        {
            if(item == MusicPlayer.queue[MusicPlayer.CurrentID()])
            {
                Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), "You are trying to remove the current music from the queue.", Snackbar.LengthShort).Show();
                return;
            }

            foreach(Song song in MusicPlayer.queue)
            {
                if (song.queueSlot > item.queueSlot)
                    song.queueSlot--;
            }

            MusicPlayer.queue.Remove(item);
        }

        public override void OnResume()
        {
            base.OnResume();
            if (MainActivity.parcelable != null)
            {
                ListView.GetLayoutManager().OnRestoreInstanceState(MainActivity.parcelable);
                MainActivity.parcelable = null;
                MainActivity.parcelableSender = null;
            }
        }
    }
}