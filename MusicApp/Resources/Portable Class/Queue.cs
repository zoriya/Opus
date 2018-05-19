using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
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
    [Activity(Label = "Queue", Theme = "@style/Theme", ScreenOrientation = ScreenOrientation.Portrait)]
    public class Queue : AppCompatActivity
    {
        public static Queue instance;
        public RecyclerView ListView;
        public RecyclerAdapter adapter;
        public ItemTouchHelper itemTouchHelper;

        private readonly string[] actions = new string[] { "Remove from queue", "Edit Metadata" };


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (MainActivity.Theme == 1)
                SetTheme(Resource.Style.DarkTheme);

            SetContentView(Resource.Layout.ListPopupLayout);
            instance = this;

            SetSupportActionBar(FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar));
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.Close);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            ListView = FindViewById<RecyclerView>(Resource.Id.recycler);
            ListView.SetLayoutManager(new LinearLayoutManager(Application.Context));

            if (MusicPlayer.queue != null)
                adapter = new RecyclerAdapter(MusicPlayer.queue);
            else
                adapter = new RecyclerAdapter(new List<Song>());

            adapter.listPadding = MainActivity.paddingBot - MainActivity.defaultPaddingBot;
            ListView.SetAdapter(adapter);
            adapter.ItemClick += ListView_ItemClick;
            adapter.ItemLongCLick += ListView_ItemLongCLick;
            ListView.SetItemAnimator(new DefaultItemAnimator());
            ListView.ScrollChange += Scroll; ;

            ItemTouchHelper.Callback callback = new ItemTouchCallback(adapter);
            itemTouchHelper = new ItemTouchHelper(callback);
            itemTouchHelper.AttachToRecyclerView(ListView);

            ListView.ScrollToPosition(MusicPlayer.CurrentID());
        }

        private void Scroll(object sender, View.ScrollChangeEventArgs e)
        {
            if (((LinearLayoutManager)ListView.GetLayoutManager()).FindLastCompletelyVisibleItemPosition() == adapter.songList.Count)
                LoadMore();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            instance = null;
        }

        public void Refresh()
        {
            adapter.UpdateList(MusicPlayer.queue);
        }

        public void RefreshCurrent()
        {

        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.QueueItems, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home)
            {
                Finish();
            }
            else if(item.ItemId == Resource.Id.shuffle)
            {
                ShuffleQueue();
            }
            else if(item.ItemId == Resource.Id.repeat)
            {
                Repeat();
            }
            return base.OnOptionsItemSelected(item);
        }

        void ShuffleQueue()
        {
            Intent intent = new Intent(this, typeof(MusicPlayer));
            intent.SetAction("RandomizeQueue");
            StartService(intent);
        }

        void Repeat()
        {
            MusicPlayer.repeat = true;
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
            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this, MainActivity.dialogTheme);
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

        protected override void OnResume()
        {
            base.OnResume();
            if (MainActivity.parcelableSender == "Queue" && !MainActivity.instance.ResumeKiller)
            {
                ListView.GetLayoutManager().OnRestoreInstanceState(MainActivity.parcelable);
                MainActivity.parcelable = null;
                MainActivity.parcelableSender = null;
            }
        }
    }
}