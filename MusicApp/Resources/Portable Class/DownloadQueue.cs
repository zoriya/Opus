using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using System.Threading.Tasks;

namespace MusicApp.Resources.Portable_Class
{
    [Activity(Label = "DownloadQueue", Theme = "@style/Theme")]
    public class DownloadQueue : AppCompatActivity, PopupMenu.IOnMenuItemClickListener
    {
        public static DownloadQueue instance;
        public RecyclerView ListView;
        private int morePosition;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (MainActivity.Theme == 1)
                SetTheme(Resource.Style.DarkTheme);

            instance = this;
            SetContentView(Resource.Layout.DownloadQueue);

            Toolbar ToolBar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(ToolBar);
            SupportActionBar.Title = "Download Queue";
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.Close);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            Window.SetStatusBarColor(Color.Argb(255, 33, 33, 33));


            ListView = FindViewById<RecyclerView>(Resource.Id.list);
            ListView.SetLayoutManager(new FixedLinearLayoutManager(this));
            ListView.SetAdapter(new DownloadQueueAdapter());
        }

        protected override void OnResume()
        {
            base.OnResume();
            instance = this;
        }

        protected override void OnStop()
        {
            instance = null;
            Window.SetStatusBarColor(Color.Transparent);
            base.OnStop();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home)
            {
                Finish();
            }
            return true;
        }

        public void More(int position)
        {
            morePosition = position;
            PopupMenu menu = new PopupMenu(this, ListView.GetChildAt(position - ((LinearLayoutManager)ListView.GetLayoutManager()).FindFirstVisibleItemPosition()).FindViewById<Android.Widget.ImageButton>(Resource.Id.more));
            menu.Inflate(Resource.Menu.download_more);
            menu.SetOnMenuItemClickListener(this);
            menu.Show();
        }

        public bool OnMenuItemClick(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.delete:
                    if(Downloader.queue[morePosition].State == DownloadState.Completed)
                    {
                        System.IO.File.Delete(Downloader.queue[morePosition].path);
                        Downloader.queue[morePosition].name = "Deleted file";
                        Downloader.queue[morePosition].State = DownloadState.Canceled;
                    }
                    else if(Downloader.queue[morePosition].State == DownloadState.None)
                    {
                        Downloader.queue[morePosition].name = "Deleted file";
                        Downloader.queue[morePosition].State = DownloadState.Canceled;
                    }
                    else
                    {
                        Android.Widget.Toast.MakeText(this, Resource.String.cant_delete, Android.Widget.ToastLength.Short).Show();
                    }
                    ListView.GetAdapter().NotifyItemChanged(morePosition);
                    morePosition = 0;
                    break;
                default:
                    break;
            }

            return true;
        }
    }
}