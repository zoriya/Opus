using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Views;
using MusicApp.Resources.values;
using System.Collections.Generic;
using System.Linq;

namespace MusicApp.Resources.Portable_Class
{
    [Activity(Label = "Queue", Theme = "@style/Theme", ScreenOrientation = ScreenOrientation.Portrait)]
    public class Queue : AppCompatActivity
    {
        public static Queue instance;
        public RecyclerView ListView;
        public RecyclerAdapter adapter;
        public ItemTouchHelper itemTouchHelper;
        public IMenu menu;

        private readonly string[] actions = new string[] { "Remove from queue", "Edit Metadata" };


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (MainActivity.Theme == 1)
                SetTheme(Resource.Style.DarkTheme);

            SetContentView(Resource.Layout.ListPopupLayout);
            instance = this;

            SetSupportActionBar(FindViewById<Toolbar>(Resource.Id.toolbar));
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.Close);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            Window.SetStatusBarColor(Color.Argb(255, 33, 33, 33));

            ListView = FindViewById<RecyclerView>(Resource.Id.recycler);
            ListView.SetLayoutManager(new LinearLayoutManager(Application.Context));

            if (MusicPlayer.queue != null)
                adapter = new RecyclerAdapter(MusicPlayer.queue);
            else
                adapter = new RecyclerAdapter(new List<Song>());

            ListView.SetAdapter(adapter);
            adapter.ItemClick += ListView_ItemClick;
            adapter.ItemLongCLick += ListView_ItemLongCLick;
            ListView.SetItemAnimator(new DefaultItemAnimator());
            ListView.ScrollChange += Scroll;

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

        protected override void OnStop()
        {
            Player.instance?.UpdateNext();
            Home.instance?.RefreshQueue();
            MusicPlayer.ParseNextSong();
            Window.SetStatusBarColor(Color.Transparent);
            base.OnStop();
            instance = null;
        }

        public void Refresh()
        {

            if (!MusicPlayer.UseCastPlayer)
                adapter.UpdateList(MusicPlayer.queue);
            else
            {
                adapter.NotifyDataSetChanged();
                MusicPlayer.RemotePlayer.MediaQueue.RegisterCallback(new QueueCallback(adapter));
            }
        }

        public void RefreshCurrent()
        {
            int first = ((LinearLayoutManager)ListView.GetLayoutManager()).FindFirstVisibleItemPosition();
            int last = ((LinearLayoutManager)ListView.GetLayoutManager()).FindLastVisibleItemPosition() - 1;
            for (int i = first; i <= last; i++)
            {
                Song song = MusicPlayer.queue[i];
                RecyclerHolder holder = (RecyclerHolder)ListView.GetChildViewHolder(((LinearLayoutManager)ListView.GetLayoutManager()).FindViewByPosition(i));
                if (MusicPlayer.queue[MusicPlayer.CurrentID()] == song)
                {
                    holder.status.Text = MusicPlayer.isRunning ? "Playing" : "Paused";
                    holder.status.SetTextColor(MusicPlayer.isRunning ? Color.Argb(255, 244, 81, 30) : Color.Argb(255, 66, 165, 245));
                    holder.status.Visibility = ViewStates.Visible;
                }
                else
                    holder.status.Visibility = ViewStates.Gone;
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.QueueItems, menu);
            this.menu = menu;
            menu.FindItem(Resource.Id.shuffle).Icon.SetColorFilter(Color.White, PorterDuff.Mode.Multiply);
            if (MusicPlayer.repeat)
                menu.FindItem(Resource.Id.repeat).Icon.SetColorFilter(Color.Argb(255, 21, 183, 237), PorterDuff.Mode.Multiply);
            
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
                Repeat(item);
            }
            return base.OnOptionsItemSelected(item);
        }

        void ShuffleQueue()
        {
            Intent intent = new Intent(this, typeof(MusicPlayer));
            intent.SetAction("RandomizeQueue");
            StartService(intent);
        }

        void Repeat(IMenuItem item)
        {
            MusicPlayer.repeat = !MusicPlayer.repeat;

            if (MusicPlayer.repeat)
                item.Icon.SetColorFilter(Color.Argb(255, 21, 183, 237), PorterDuff.Mode.Multiply);
            else
                item.Icon.ClearColorFilter();
        }

        public void LoadMore()
        {
            List<Song> songList = MusicPlayer.queue.Except(adapter.songList).ToList();
        }

        private void ListView_ItemClick(object sender, int Position)
        {
            Song item = MusicPlayer.queue[Position];

            if (Position == MusicPlayer.CurrentID())
            {
                Intent intent = new Intent(this, typeof(MusicPlayer));
                intent.SetAction("Pause");
                StartService(intent);
            }
            else if(MusicPlayer.instance != null)
                MusicPlayer.instance.SwitchQueue(Position);
            else
            {
                Intent intent = new Intent(this, typeof(MusicPlayer));
                intent.SetAction("SwitchQueue");
                intent.PutExtra("queueSlot", Position);
                StartService(intent);
            }
        }

        private void ListView_ItemLongCLick(object sender, int e)
        {
            MainActivity.instance.contentRefresh.SetEnabled(false);
        }

        public void More(int position)
        {
            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this, MainActivity.dialogTheme);
            builder.SetTitle("Pick an action");
            builder.SetItems(actions, (senderAlert, args) =>
            {
                switch (args.Which)
                {
                    case 0:
                        RemoveFromQueue(position);
                        break;
                    case 1:
                        if (!MusicPlayer.queue[position].IsYt)
                            Browse.EditMetadata(MusicPlayer.queue[position], "Queue", ListView.GetLayoutManager().OnSaveInstanceState());
                        break;
                    default:
                        break;
                }
            });
            builder.Show();
        }

        public static void InsertToQueue(int position, Song item)
        {
            if (MusicPlayer.CurrentID() > position)
            {
                MusicPlayer.currentID--;
                MusicPlayer.SaveQueueSlot();
            }

            MusicPlayer.queue.Insert(position, item);
            MusicPlayer.UpdateQueueSlots();
        }

        public static void RemoveFromQueue(int position)
        {
            if (MusicPlayer.CurrentID() > position)
            {
                MusicPlayer.currentID--;
                MusicPlayer.SaveQueueSlot();
            }

            MusicPlayer.queue.RemoveAt(position);
            MusicPlayer.UpdateQueueSlots();
        }

        protected override void OnResume()
        {
            base.OnResume();
            Window.SetStatusBarColor(Color.Argb(255, 33, 33, 33));
            instance = this;
            if (MainActivity.parcelableSender == "Queue" && !MainActivity.instance.ResumeKiller)
            {
                ListView.GetLayoutManager().OnRestoreInstanceState(MainActivity.parcelable);
                MainActivity.parcelable = null;
                MainActivity.parcelableSender = null;
            }
            RefreshCurrent();
        }
    }
}