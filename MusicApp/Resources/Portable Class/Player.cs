using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Widget;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Org.Adw.Library.Widgets.Discreteseekbar;
using Square.Picasso;
using System;
using System.Threading.Tasks;

namespace MusicApp.Resources.Portable_Class
{
    public class Player : Fragment
    {
        public static Player instance;
        public View playerView;
        public Handler handler = new Handler();

        private DiscreteSeekBar bar;
        private ImageView imgView;
        private readonly int[] timers = new int[] { 0, 1, 10, 30, 60, 120 };
        private readonly string[] items = new string[] { "Off", "1 minute", "10 minutes", "30 minutes", "1 hour", "2 hours" };
        private int checkedItem = 0;


        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
        }

        public override void OnDestroy()
        {
            if(MainActivity.youtubeInstanceSave == null)
            {
                MainActivity.instance.FindViewById<SwipeRefreshLayout>(Resource.Id.contentRefresh).SetEnabled(true);
            }
            MainActivity.instance.ToolBar.Visibility = ViewStates.Visible;
            MainActivity.instance.FindViewById<BottomNavigationView>(Resource.Id.bottomView).Visibility = ViewStates.Visible;
            MainActivity.instance.ShowSmallPlayer();
            MainActivity.instance.PrepareSmallPlayer();
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

        async void CreatePlayer()
        {
            MainActivity.instance.HideSmallPlayer();

            while (MusicPlayer.CurrentID() == -1)
                await Task.Delay(100);

            MainActivity.instance.ShowQuickPlay();
            MainActivity.instance.ToolBar.Visibility = ViewStates.Gone;
            MainActivity.instance.FindViewById<BottomNavigationView>(Resource.Id.bottomView).Visibility = ViewStates.Gone;
            MainActivity.instance.FindViewById<SwipeRefreshLayout>(Resource.Id.contentRefresh).SetEnabled(false);

            TextView title = playerView.FindViewById<TextView>(Resource.Id.playerTitle);
            TextView artist = playerView.FindViewById<TextView>(Resource.Id.playerArtist);
            imgView = playerView.FindViewById<ImageView>(Resource.Id.playerAlbum);

            playerView.FindViewById<ImageButton>(Resource.Id.lastButton).Click += Last_Click;
            playerView.FindViewById<ImageButton>(Resource.Id.playButton).Click += Play_Click;
            playerView.FindViewById<ImageButton>(Resource.Id.nextButton).Click += Next_Click;
            playerView.FindViewById<ImageButton>(Resource.Id.playerSleep).Click += SleepButton_Click;
            playerView.FindViewById<ImageButton>(Resource.Id.playerPlaylistAdd).Click += AddToPlaylist_Click; ;
            playerView.FindViewById<FloatingActionButton>(Resource.Id.downFAB).Click += Fab_Click;


            Song current = MusicPlayer.queue[MusicPlayer.CurrentID()];

            title.Text = current.GetName();
            artist.Text = current.GetArtist();
            title.Selected = true;
            title.SetMarqueeRepeatLimit(3);
            artist.Selected = true;
            artist.SetMarqueeRepeatLimit(3);

            if (MusicPlayer.isRunning)
                playerView.FindViewById<ImageButton>(Resource.Id.playButton).SetImageResource(Resource.Drawable.ic_pause_black_24dp);
            else
                playerView.FindViewById<ImageButton>(Resource.Id.playButton).SetImageResource(Resource.Drawable.ic_play_arrow_black_24dp);

            if (current.GetAlbum() == null)
            {
                var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                var songAlbumArtUri = ContentUris.WithAppendedId(songCover, current.GetAlbumArt());

                Picasso.With(Android.App.Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(imgView);
            }
            else
            {
                Picasso.With(Android.App.Application.Context).Load(current.GetAlbum()).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(imgView);
            }

            if (current.IsYt)
            {
                ImageView download = playerView.FindViewById<ImageView>(Resource.Id.playerDownload);
                download.Visibility = ViewStates.Visible;
                download.Click += Download_Click;
                playerView.FindViewById<ImageButton>(Resource.Id.playerPlaylistAdd).SetPadding(0, 0, 0, 0);
            }
            else
            {
                playerView.FindViewById<ImageView>(Resource.Id.playerDownload).Visibility = ViewStates.Gone;
                playerView.FindViewById<ImageButton>(Resource.Id.playerPlaylistAdd).SetPadding((int) (50 * MainActivity.instance.Resources.DisplayMetrics.Density + 0.5f), 0, 0, 0);
            }

            TextView NextTitle = playerView.FindViewById<TextView>(Resource.Id.nextTitle);
            TextView NextAlbum = playerView.FindViewById<TextView>(Resource.Id.nextArtist);
            Button ShowQueue = playerView.FindViewById<Button>(Resource.Id.showQueue);

            ShowQueue.Click += ShowQueue_Click;

            if (MainActivity.Theme == 1)
            {
                NextTitle.SetTextColor(Color.White);
                NextAlbum.SetTextColor(Color.White);
                NextAlbum.Alpha = 0.7f;
                ((GradientDrawable)ShowQueue.Background).SetStroke(5, Android.Content.Res.ColorStateList.ValueOf(Color.Argb(255, 62, 80, 180)));
                ShowQueue.SetTextColor(Color.Argb(255, 62, 80, 180));
            }
            else
            {
                ((GradientDrawable)ShowQueue.Background).SetStroke(5, Android.Content.Res.ColorStateList.ValueOf(Color.Argb(255, 21, 183, 237)));
                ShowQueue.SetTextColor(Color.Argb(255, 21, 183, 237));
            }

            bool asNext = MusicPlayer.queue.Count > MusicPlayer.CurrentID() + 1;
            if (asNext)
            {
                Song next = MusicPlayer.queue[MusicPlayer.CurrentID() + 1];
                NextTitle.Text = "Next music:";
                NextAlbum.Text = next.GetName();
                ImageView nextArt = playerView.FindViewById<ImageView>(Resource.Id.nextArt);

                if (next.GetAlbum() == null)
                {
                    var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                    var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, next.GetAlbumArt());

                    Picasso.With(Android.App.Application.Context).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
                }
                else
                {
                    Picasso.With(Android.App.Application.Context).Load(next.GetAlbum()).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
                }
            }
            else if (MusicPlayer.repeat)
            {
                Song next = MusicPlayer.queue[0];
                NextTitle.Text = "Next music:";
                NextAlbum.Text = next.GetName();
                ImageView nextArt = playerView.FindViewById<ImageView>(Resource.Id.nextArt);

                if (next.GetAlbum() == null)
                {
                    var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                    var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, next.GetAlbumArt());

                    Picasso.With(Android.App.Application.Context).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
                }
                else
                {
                    Picasso.With(Android.App.Application.Context).Load(next.GetAlbum()).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
                }
            }
            else
            {
                NextTitle.Text = "Next music:";
                NextAlbum.Text = "Nothing.";

                ImageView nextArt = playerView.FindViewById<ImageView>(Resource.Id.nextArt);
                Picasso.With(Android.App.Application.Context).Load(Resource.Drawable.noAlbum).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
            }

            while (MusicPlayer.player == null || MusicPlayer.player.Duration == 0)
                await Task.Delay(100);

            bar = playerView.FindViewById<DiscreteSeekBar>(Resource.Id.songTimer);
            bar.SetNumericTransformer(new TimerTransformer());
            bar.LayoutParameters.Width = (int)(MainActivity.instance.Resources.DisplayMetrics.WidthPixels * 1.1f);
            MusicPlayer.SetSeekBar(bar);
            handler.PostDelayed(UpdateSeekBar, 1000);

            await Task.Delay(1000);
            MusicPlayer.SetSeekBar(bar);
        }

        public async void RefreshPlayer()
        {
            while (MusicPlayer.CurrentID() == -1)
                await Task.Delay(100);

            if (playerView == null)
                return;

            TextView title = playerView.FindViewById<TextView>(Resource.Id.playerTitle);
            TextView artist = playerView.FindViewById<TextView>(Resource.Id.playerArtist);
            imgView = playerView.FindViewById<ImageView>(Resource.Id.playerAlbum);

            Song current = MusicPlayer.queue[MusicPlayer.CurrentID()];

            title.Text = current.GetName();
            artist.Text = current.GetArtist();

            if (MusicPlayer.isRunning)
                playerView.FindViewById<ImageButton>(Resource.Id.playButton).SetImageResource(Resource.Drawable.ic_pause_black_24dp);
            else
                playerView.FindViewById<ImageButton>(Resource.Id.playButton).SetImageResource(Resource.Drawable.ic_play_arrow_black_24dp);

            if (!current.IsYt)
            {
                var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                var songAlbumArtUri = ContentUris.WithAppendedId(songCover, current.GetAlbumArt());

                Picasso.With(Android.App.Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(imgView);
            }
            else
            {
                Picasso.With(Android.App.Application.Context).Load(current.GetAlbum()).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(imgView);
            }

            ImageView download = playerView.FindViewById<ImageView>(Resource.Id.playerDownload);

            if (current.IsYt)
            {
                download.Visibility = ViewStates.Visible;
                playerView.FindViewById<ImageButton>(Resource.Id.playerPlaylistAdd).SetPadding(0, 0, 0, 0);
            }
            else
            {
                download.Visibility = ViewStates.Gone;
                playerView.FindViewById<ImageButton>(Resource.Id.playerPlaylistAdd).SetPadding((int)(50 * MainActivity.instance.Resources.DisplayMetrics.Density + 0.5f), 0, 0, 0);
            }

            bool asNext = MusicPlayer.queue.Count > MusicPlayer.CurrentID() + 1;
            if (asNext)
            {
                Song next = MusicPlayer.queue[MusicPlayer.CurrentID() + 1];
                playerView.FindViewById<TextView>(Resource.Id.nextTitle).Text = "Next music:";
                playerView.FindViewById<TextView>(Resource.Id.nextArtist).Text = next.GetName();
                ImageView nextArt = playerView.FindViewById<ImageView>(Resource.Id.nextArt);

                if (next.GetAlbum() == null)
                {
                    var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                    var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, next.GetAlbumArt());

                    Picasso.With(Android.App.Application.Context).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
                }
                else
                {
                    Picasso.With(Android.App.Application.Context).Load(next.GetAlbum()).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
                }
            }
            else if (MusicPlayer.repeat)
            {
                Song next = MusicPlayer.queue[0];
                playerView.FindViewById<TextView>(Resource.Id.nextTitle).Text = "Next music:";
                playerView.FindViewById<TextView>(Resource.Id.nextArtist).Text = next.GetName();
                ImageView nextArt = playerView.FindViewById<ImageView>(Resource.Id.nextArt);

                if (next.GetAlbum() == null)
                {
                    var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                    var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, next.GetAlbumArt());

                    Picasso.With(Android.App.Application.Context).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
                }
                else
                {
                    Picasso.With(Android.App.Application.Context).Load(next.GetAlbum()).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
                }
            }
            else
            {
                playerView.FindViewById<TextView>(Resource.Id.nextTitle).Text = "Next music:";
                playerView.FindViewById<TextView>(Resource.Id.nextArtist).Text = "Nothing.";

                ImageView nextArt = playerView.FindViewById<ImageView>(Resource.Id.nextArt);
                Picasso.With(Android.App.Application.Context).Load(Resource.Drawable.noAlbum).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
            }

            while (MusicPlayer.player.Duration < 1)
                await Task.Delay(100);

            bar.Progress = 0;
            bar.Max = (int)MusicPlayer.player.Duration;
        }

        public async void UpdateNext()
        {
            await Task.Delay(10);
            bool asNext = MusicPlayer.queue.Count > MusicPlayer.CurrentID() + 1;
            if (asNext)
            {
                Song next = MusicPlayer.queue[MusicPlayer.CurrentID() + 1];
                playerView.FindViewById<TextView>(Resource.Id.nextTitle).Text = "Next music:";
                playerView.FindViewById<TextView>(Resource.Id.nextArtist).Text = next.GetName();
                ImageView nextArt = playerView.FindViewById<ImageView>(Resource.Id.nextArt);

                if (next.GetAlbum() == null)
                {
                    var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                    var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, next.GetAlbumArt());

                    Picasso.With(Android.App.Application.Context).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
                }
                else
                {
                    Picasso.With(Android.App.Application.Context).Load(next.GetAlbum()).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
                }
            }
            else if (MusicPlayer.repeat)
            {
                Song next = MusicPlayer.queue[0];
                playerView.FindViewById<TextView>(Resource.Id.nextTitle).Text = "Next music:";
                playerView.FindViewById<TextView>(Resource.Id.nextArtist).Text = next.GetName();
                ImageView nextArt = playerView.FindViewById<ImageView>(Resource.Id.nextArt);

                if (next.GetAlbum() == null)
                {
                    var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                    var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, next.GetAlbumArt());

                    Picasso.With(Android.App.Application.Context).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
                }
                else
                {
                    Picasso.With(Android.App.Application.Context).Load(next.GetAlbum()).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
                }
            }
            else
            {
                playerView.FindViewById<TextView>(Resource.Id.nextTitle).Text = "Next music:";
                playerView.FindViewById<TextView>(Resource.Id.nextArtist).Text = "Nothing.";

                ImageView nextArt = playerView.FindViewById<ImageView>(Resource.Id.nextArt);
                Picasso.With(Android.App.Application.Context).Load(Resource.Drawable.noAlbum).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
            }
        }

        private void Download_Click(object sender, EventArgs e)
        {
            Song song = MusicPlayer.queue[MusicPlayer.CurrentID()];
            YoutubeEngine.Download(song.GetName(), song.youtubeID);
        }

        public void Stoped()
        {
            MainActivity.instance.FindViewById<BottomNavigationView>(Resource.Id.bottomView).SelectedItemId = Resource.Id.musicLayout;
        }

        private void AddToPlaylist_Click(object sender, EventArgs e)
        {
            Browse.act = Activity;
            Browse.inflater = LayoutInflater;
            Browse.GetPlaylist(MusicPlayer.queue[MusicPlayer.CurrentID()]);
        }

        public void UpdateSeekBar()
        {
            if (!MusicPlayer.isRunning)
            {
                handler.RemoveCallbacks(UpdateSeekBar);
                return;
            }

            bar.Progress = MusicPlayer.CurrentPosition;
            handler.PostDelayed(UpdateSeekBar, 1000);
        }

        private async void Fab_Click(object sender, EventArgs e)
        {
            MainActivity.instance.SupportFragmentManager.PopBackStack();
            if (MainActivity.youtubeInstanceSave != null)
            {
                int selectedTab = 0;
                switch (MainActivity.youtubeInstanceSave)
                {
                    case "YoutubeEngine-All":
                        selectedTab = 0;
                        break;
                    case "YoutubeEngine-Tracks":
                        selectedTab = 1;
                        break;
                    case "YoutubeEngine-Playlists":
                        selectedTab = 2;
                        break;
                    case "YoutubeEngine-Channels":
                        selectedTab = 3;
                        break;
                    default:
                        break;
                }
                await Task.Delay(750);
                MainActivity.instance.SupportFragmentManager.BeginTransaction().Replace(Resource.Id.contentView, Pager.NewInstance(1, selectedTab)).Commit();
                YoutubeEngine.instances[selectedTab].focused = true;
                YoutubeEngine.instances[selectedTab].OnFocus();
                YoutubeEngine.instances[selectedTab].ResumeListView();
            }
            else
            {
                MainActivity.instance.ResumeInstance();
            }
        }

        private void ShowQueue_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(Activity, typeof(Queue));
            StartActivity(intent);
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
            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(Activity, MainActivity.dialogTheme);
            builder.SetTitle("Sleep in :");
            builder.SetSingleChoiceItems(items, checkedItem, ((senders, eventargs) => { checkedItem = eventargs.Which; }));
            builder.SetPositiveButton("Ok", ((senders, args) => { Sleep(timers[checkedItem]); }));
            builder.SetNegativeButton("Cancel", ((senders, args) => { }));
            builder.Show();
        }

        void Sleep(int time)
        {
            Console.WriteLine("&Going to sleep in " + time + ", slected item is the " + checkedItem + " one.");
            Intent intent = new Intent(Activity, typeof(Sleeper));
            intent.PutExtra("time", time);
            Activity.StartService(intent);
        }

        public override void OnResume()
        {
            base.OnResume();
            instance = this;
        }
    }
}