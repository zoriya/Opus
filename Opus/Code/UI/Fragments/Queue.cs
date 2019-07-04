using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using Opus;
using Opus.Adapter;
using Opus.Api;
using Opus.Api.Services;
using Opus.DataStructure;
using Opus.Others;
using Opus.Views;
using Square.Picasso;
using System.Collections.Generic;
using Fragment = Android.Support.V4.App.Fragment;
using PopupMenu = Android.Support.V7.Widget.PopupMenu;

[Activity(Label = "Queue", Theme = "@style/Theme", ScreenOrientation = ScreenOrientation.Portrait)]
[Register("Opus/Queue")]
public class Queue : Fragment, RecyclerView.IOnItemTouchListener, PopupMenu.IOnMenuItemClickListener
{
    public static Queue instance;
    public RecyclerView ListView;
    private QueueAdapter adapter;
    public ItemTouchHelper itemTouchHelper;
    public int HeaderHeight;
    public IMenu menu;

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        View view = inflater.Inflate(Resource.Layout.LonelyRecycler, container, false);
        instance = this;
        ListView = view.FindViewById<RecyclerView>(Resource.Id.recycler);
        ListView.SetLayoutManager(new LinearLayoutManager(Application.Context));
        adapter = new QueueAdapter();
        ListView.SetAdapter(adapter);
        adapter.ItemClick += ListView_ItemClick;
        adapter.ItemLongCLick += ListView_ItemLongCLick;
        ListView.SetItemAnimator(new DefaultItemAnimator());
        ListView.AddItemDecoration(new CurrentItemDecoration(adapter));
        ListView.AddOnItemTouchListener(this);
        ListView.ScrollChange += Scroll;

        ItemTouchHelper.Callback callback = new ItemTouchCallback(adapter, true);
        itemTouchHelper = new ItemTouchHelper(callback);
        itemTouchHelper.AttachToRecyclerView(ListView);

        ListView.ScrollToPosition(MusicPlayer.CurrentID());

        if (MusicPlayer.UseCastPlayer)
        {
            Snackbar snackBar = Snackbar.Make(ListView, "Queue management with chromecast is currently in beta, expect some bugs.", (int)ToastLength.Short);
            snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
            snackBar.Show();
        }
        return view;
    }

    private void Scroll(object sender, View.ScrollChangeEventArgs e) { }

    public void Refresh()
    {
        adapter.NotifyDataSetChanged();
    }

    public void NotifyItemInserted(int position)
    {
        position++;
        adapter.NotifyItemInserted(position);
    }

    public void NotifyItemRangeInserted(int position, int count)
    {
        position++;
        adapter.NotifyItemRangeInserted(position, count);
    }

    public void NotifyItemChanged(int position)
    {
        position++;
        adapter.NotifyItemChanged(position);
    }

    public void NotifyItemChanged(int position, Java.Lang.Object payload)
    {
        position++;
        adapter.NotifyItemChanged(position, payload);
    }

    public void NotifyItemRemoved(int position)
    {
        position++;
        adapter.NotifyItemRemoved(position);
    }

    public void RefreshCurrent()
    {
        System.Console.WriteLine("&Queue current refreshing, isPlaying: " + MusicPlayer.isRunning);
        ListView.InvalidateItemDecorations();

        int first = ((LinearLayoutManager)ListView.GetLayoutManager()).FindFirstVisibleItemPosition();
        int last = ((LinearLayoutManager)ListView.GetLayoutManager()).FindLastVisibleItemPosition();
        for (int i = first; i <= last; i++)
        {
            View child = ((LinearLayoutManager)ListView.GetLayoutManager()).FindViewByPosition(i);
            if (child != null && ListView.GetChildViewHolder(child) is SongHolder holder)
            {
                if (SongParser.playPosition == i - 1)
                {
                    holder.status.Visibility = ViewStates.Visible;
                    holder.status.SetTextColor(Color.Argb(255, 0, 255, 255));

                    string status = MainActivity.instance.GetString(Resource.String.loading);
                    SpannableString statusText = new SpannableString(status);
                    statusText.SetSpan(new BackgroundColorSpan(Color.ParseColor("#8C000000")), 0, status.Length, SpanTypes.InclusiveInclusive);
                    holder.status.TextFormatted = statusText;
                }
                else if (MusicPlayer.CurrentID() == i - 1) //The -1 is because the first displayed item of the queue is a header.
                {
                    holder.status.Visibility = ViewStates.Visible;
                    holder.status.SetTextColor(MusicPlayer.isRunning ? Color.Argb(255, 244, 81, 30) : Color.Argb(255, 66, 165, 245));

                    string status = MusicPlayer.isRunning ? MainActivity.instance.GetString(Resource.String.playing) : MainActivity.instance.GetString(Resource.String.paused);
                    SpannableString statusText = new SpannableString(status);
                    statusText.SetSpan(new BackgroundColorSpan(Color.ParseColor("#8C000000")), 0, status.Length, SpanTypes.InclusiveInclusive);
                    holder.status.TextFormatted = statusText;
                }
                else
                    holder.status.Visibility = ViewStates.Gone;
            }
        }
    }

    public void RefreshAP()
    {
        adapter.NotifyItemChanged(MusicPlayer.queue.Count + 1);
    }

    private void ListView_ItemClick(object sender, int Position)
    {
        if (Position == MusicPlayer.CurrentID())
        {
            Intent intent = new Intent(Activity, typeof(MusicPlayer));
            intent.SetAction("Pause");
            Activity.StartService(intent);
        }
        else if(MusicPlayer.instance != null)
            MusicPlayer.instance.SwitchQueue(Position);
        else
        {
            Intent intent = new Intent(Activity, typeof(MusicPlayer));
            intent.SetAction("SwitchQueue");
            intent.PutExtra("queueSlot", Position);
            Activity.StartService(intent);
        }
    }

    private void ListView_ItemLongCLick(object sender, int position)
    {
        More(position);
    }

    public void More(int position)
    {
        Song item = MusicPlayer.queue[position];
        BottomSheetAction endAction = new BottomSheetAction(Resource.Drawable.Close, MainActivity.instance.GetString(Resource.String.remove_from_queue), (sender, eventArg) =>
        {
            MusicPlayer.RemoveFromQueue(position);
        });
        MainActivity.instance.More(item, () => { ListView_ItemClick(null, position); }, endAction);
    }

    void HeaderClick()
    {
        Intent intent = new Intent(Activity, typeof(MusicPlayer));
        intent.SetAction("Pause");
        Activity.StartService(intent);
    }

    void HeaderMoreClick()
    {
        More(MusicPlayer.CurrentID());
    }

    public static void InsertToQueue(int position, Song item)
    {
        if (MusicPlayer.CurrentID() >= position)
        {
            MusicPlayer.currentID++;
            MusicPlayer.SaveQueueSlot();
        }

        MusicPlayer.InsertToQueue(position, item);
    }

    public override void OnResume()
    {
        base.OnResume();
        instance = this;
    }

    public bool OnInterceptTouchEvent(RecyclerView recyclerView, MotionEvent motionEvent)
    {
        if (HeaderHeight == 0)
            return false;

        if (motionEvent.GetY() <= HeaderHeight)
        {
            if (motionEvent.ActionMasked == MotionEventActions.Down) //The up motion is never triggered so i use the down here.
            {
                if (motionEvent.GetX() < recyclerView.MeasuredWidth * 0.8)
                    HeaderClick();
                else
                    HeaderMoreClick();
            }
            return true;
        }
        if(motionEvent.GetY() >= recyclerView.Height + HeaderHeight) //When the header is at the bottom, the HeaderHeight is negative
        {
            if (motionEvent.ActionMasked == MotionEventActions.Down)
            {
                if (motionEvent.GetX() < recyclerView.MeasuredWidth * 0.8)
                    HeaderClick();
                else
                    HeaderMoreClick();
            }
            return true;
        }

        return false;
    }

    public void OnRequestDisallowInterceptTouchEvent(bool disallow) { }

    public void OnTouchEvent(RecyclerView recyclerView, MotionEvent @event) { }

    public bool OnMenuItemClick(IMenuItem item)
    {
        switch(item.ItemId)
        {
            case Resource.Id.saveAsPlaylist:
                SaveQueueToPlaylist();
                break;
        }
        return true;
    }

    public void SaveQueueToPlaylist()
    {
        PlaylistManager.CreatePlalistDialog(MusicPlayer.queue.ToArray());
    }
}