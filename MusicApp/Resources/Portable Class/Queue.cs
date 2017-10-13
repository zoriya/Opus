using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Android.Support.V4.App;
using System;
using Square.Picasso;
using Android.Graphics;
using Android.Support.Design.Widget;
using MusicApp.Resources.Fragments;
using Android.Transitions;
using Android.Animation;
using Android.App.Job;
using System.Threading.Tasks;
using System.Threading;
using Android.Support.V7.App;

namespace MusicApp.Resources.Portable_Class
{
    public class Queue: ListFragment
    {
        public static Queue instance;
        public Adapter adapter;
        public View emptyView;

        private View view;
        private ImageView imgView;
        private View playerView;
        private View controllerView;
        private bool isEmpty = false;
        private CancellationTokenSource cancelToken;
        private int[] timers = new int[] { 0, 1, 10, 30, 60, 120 };
        private string[] items = new string[] { "Off", "1 minute", "10 minutes", "30 minutes", "1 hour", "2 hours" };
        private int checkedItem = 0;


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            emptyView = LayoutInflater.Inflate(Resource.Layout.NoQueue, null);
            ListView.EmptyView = emptyView;

            PopulateView();
        }

        public override void OnDestroy()
        {
            MainActivity.instance.ToolBar.Visibility = ViewStates.Visible;
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
                view.SetPadding(0, 100, 0, 0);
                isEmpty = true;
                Activity.AddContentView(emptyView, View.LayoutParameters);
            }
            else
            {
                MainActivity.instance.ToolBar.Visibility = ViewStates.Gone;
                playerView = LayoutInflater.Inflate(Resource.Layout.player, null);
                TextView title = playerView.FindViewById<TextView>(Resource.Id.playerTitle);
                TextView artist = playerView.FindViewById<TextView>(Resource.Id.playerArtist);
                imgView = playerView.FindViewById<ImageView>(Resource.Id.playerAlbum);
                ImageButton sleepButton = playerView.FindViewById<ImageButton>(Resource.Id.playerSleep);
                sleepButton.Click += SleepButton_Click;


                Song current = MusicPlayer.queue[MusicPlayer.CurentID()];

                title.Text = current.GetName();
                artist.Text = current.GetArtist();
                title.Selected = true;
                title.SetMarqueeRepeatLimit(3);
                artist.Selected = true;
                artist.SetMarqueeRepeatLimit(3);

                var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                var songAlbumArtUri = ContentUris.WithAppendedId(songCover, current.GetAlbumArt());

                Picasso.With(Android.App.Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Into(imgView);

                ListView.AddHeaderView(playerView);

                FloatingActionButton fab = playerView.FindViewById<FloatingActionButton>(Resource.Id.playFAB);
                fab.Click += Fab_Click;
            }
        }

        private void SleepButton_Click(object sender, EventArgs e)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, Resource.Style.AppCompatAlertDialogStyle);
            builder.SetTitle("Sleep in :");
            builder.SetSingleChoiceItems(items, checkedItem, ((senders, eventargs) => { checkedItem = eventargs.Which; } ));
            builder.SetPositiveButton("Ok", ((senders, args) => { Sleep(timers[checkedItem]); } ));
            builder.SetNegativeButton("Cancel", ((senders, args) => { }));
            builder.Show();
        }

        async void Sleep(int time)
        {
            cancelToken?.Cancel();

            if (time == 0)
                return;

            using (cancelToken = new CancellationTokenSource())
            {
                try
                {
                    await Task.Run(() =>
                    {
                        Thread.Sleep(time * 60 * 1000);
                        Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
                        intent.SetAction("Stop");
                        Activity.StartService(intent);
                    });
                }
                catch(TaskCanceledException)
                {
                    Console.WriteLine("Sleep Timer Canceled");
                }
            }
        }

        private void Fab_Click(object sender, EventArgs e)
        {
            playerView.FindViewById(Resource.Id.playerTitle).Visibility = ViewStates.Gone;
            playerView.FindViewById(Resource.Id.playerArtist).Visibility = ViewStates.Gone;
            FloatingActionButton fab = playerView.FindViewById<FloatingActionButton>(Resource.Id.playFAB);

            LinearLayout layout = playerView.FindViewById<LinearLayout>(Resource.Id.infoPanel);
            layout.SetPadding(0, 15, 0, 0);

            controllerView = LayoutInflater.Inflate(Resource.Layout.playerControl, layout, true);
            layout.SetBackgroundColor(new Color(76, 181, 174));

            controllerView.Visibility = ViewStates.Gone;
            int centerX = controllerView.Top;
            int centerY = controllerView.Left;
            float endRadius = (float)Math.Sqrt(controllerView.Width * controllerView.Width + controllerView.Height + controllerView.Height) * 2;

            Animator animator = ViewAnimationUtils.CreateCircularReveal(controllerView, centerX, centerY, 0, endRadius);
            animator.SetDuration(750);

            controllerView.Visibility = ViewStates.Visible;
            animator.Start();

            fab.Hide();

            controllerView.FindViewById<ImageButton>(Resource.Id.controllerLast).Click += Last_Click;
            controllerView.FindViewById<ImageButton>(Resource.Id.controllerPlay).Click += Play_Click;
            controllerView.FindViewById<ImageButton>(Resource.Id.controllerNext).Click += Next_Click;

            TextView text = controllerView.FindViewById<TextView>(Resource.Id.controllerTitle);
            string Title = playerView.FindViewById<TextView>(Resource.Id.playerTitle).Text;
            string Artist = playerView.FindViewById<TextView>(Resource.Id.playerArtist).Text;

            text.Text = Title + " - " + Artist;
        }

        private void CloseFab()
        {
            FloatingActionButton fab = playerView.FindViewById<FloatingActionButton>(Resource.Id.playFAB);

            LinearLayout layout = playerView.FindViewById<LinearLayout>(Resource.Id.infoPanel);
            layout.SetBackgroundColor(new Color(43, 85, 104));

            int centerX = layout.Top;
            int centerY = layout.Left;
            float endRadius = (float)Math.Sqrt(layout.Width * layout.Width + layout.Height + layout.Height) * 2;

            Animator animator = ViewAnimationUtils.CreateCircularReveal(controllerView, centerX, centerY, endRadius, 0);
            animator.SetDuration(750);
            animator.AnimationEnd += Animator_AnimationEnd;
            animator.Start();

            LinearLayout controller = playerView.FindViewById<LinearLayout>(Resource.Id.playerControl);
            controller.Visibility = ViewStates.Invisible;

        }

        private void Animator_AnimationEnd(object sender, EventArgs e)
        {
            FloatingActionButton fab = playerView.FindViewById<FloatingActionButton>(Resource.Id.playFAB);
            fab.Show();
            playerView.FindViewById(Resource.Id.playerTitle).Visibility = ViewStates.Visible;
            playerView.FindViewById(Resource.Id.playerArtist).Visibility = ViewStates.Visible;

            LinearLayout layout = playerView.FindViewById<LinearLayout>(Resource.Id.infoPanel);
            layout.SetPadding(0, 15, 0, 20);
            LinearLayout controller = playerView.FindViewById<LinearLayout>(Resource.Id.playerControl);
            layout.RemoveView(controller);
        }

        private void Last_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("Previus");
            Activity.StartService(intent);
            CloseFab();
        }

        private void Play_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("Pause");
            Activity.StartService(intent);
            CloseFab();
        }

        private void Next_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("Next");
            Activity.StartService(intent);
            CloseFab();
        }

        private void ListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            Song item = MusicPlayer.queue[e.Position];

            Context context = Android.App.Application.Context;
            Intent intent = new Intent(context, typeof(MusicPlayer));
            intent.PutExtra("file", item.GetPath());
            intent.SetAction("QueueSwitch");
            context.StartService(intent);
        }
    }
}