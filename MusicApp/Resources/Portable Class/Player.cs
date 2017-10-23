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
    public class Player : Fragment
    {
        public static Player instance;
        public View playerView;

        private View view;
        private ImageView imgView;
        private CancellationTokenSource cancelToken;
        private int[] timers = new int[] { 0, 1, 10, 30, 60, 120 };
        private string[] items = new string[] { "Off", "1 minute", "10 minutes", "30 minutes", "1 hour", "2 hours" };
        private int checkedItem = 0;


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
        }

        public override void OnDestroy()
        {
            MainActivity.instance.ToolBar.Visibility = ViewStates.Visible;
            base.OnDestroy();
            instance = null;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var useless = base.OnCreateView(inflater, container, savedInstanceState);
            playerView = LayoutInflater.Inflate(Resource.Layout.player, null);
            CreatePlayer();
            return playerView;
        }

        public static Fragment NewInstance()
        {
            instance = new Player { Arguments = new Bundle() };
            return instance;
        }

        void CreatePlayer()
        {
            MainActivity.instance.ToolBar.Visibility = ViewStates.Gone;
            MainActivity.instance.FindViewById<BottomNavigationView>(Resource.Id.bottomView).Visibility = ViewStates.Gone;
            TextView title = playerView.FindViewById<TextView>(Resource.Id.playerTitle);
            TextView artist = playerView.FindViewById<TextView>(Resource.Id.playerArtist);
            imgView = playerView.FindViewById<ImageView>(Resource.Id.playerAlbum);
            playerView.FindViewById<ImageButton>(Resource.Id.playerSleep).Click += SleepButton_Click;
            playerView.FindViewById<ImageButton>(Resource.Id.lastButton).Click += Last_Click;
            playerView.FindViewById<ImageButton>(Resource.Id.playButton).Click += Play_Click;
            playerView.FindViewById<ImageButton>(Resource.Id.nextButton).Click += Next_Click;
            playerView.FindViewById<FloatingActionButton>(Resource.Id.downFAB).Click += Fab_Click;


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
        }

        private void Fab_Click(object sender, EventArgs e)
        {
            Activity.SupportFragmentManager.BeginTransaction()./*SetCustomAnimations(Resource.Animation.SlideInUp, Resource.Animation.SlideOutUp)/*/Replace(Resource.Id.contentView, Queue.NewInstance()).Commit();
        }

        private void Last_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("Previus");
            Activity.StartService(intent);
        }

        private void Play_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("Pause");
            Activity.StartService(intent);
        }

        private void Next_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("Next");
            Activity.StartService(intent);
        }

        private void SleepButton_Click(object sender, EventArgs e)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(Activity, Resource.Style.AppCompatAlertDialogStyle);
            builder.SetTitle("Sleep in :");
            builder.SetSingleChoiceItems(items, checkedItem, ((senders, eventargs) => { checkedItem = eventargs.Which; }));
            builder.SetPositiveButton("Ok", ((senders, args) => { Sleep(timers[checkedItem]); }));
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
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Sleep Timer Canceled");
                }
            }
        }
    }
}