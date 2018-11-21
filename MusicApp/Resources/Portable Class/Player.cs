using Android.Animation;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.Widget;
using Android.Support.V7.Graphics;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Square.Picasso;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicApp.Resources.Portable_Class
{
    [Activity(Label = "Player", Theme = "@style/Theme", ScreenOrientation = ScreenOrientation.Portrait, LaunchMode = LaunchMode.SingleTop)]
    public class Player : Android.Support.V4.App.Fragment, Palette.IPaletteAsyncListener
    {
        public static Player instance;
        public Handler handler = new Handler();
        public static bool errorState = false;
        public bool? playNext = true;

        private SeekBar bar;
        private ProgressBar spBar;
        private TextView timerStart;
        private ImageView imgView;
        private bool prepared = false;
        private readonly int[] timers = new int[] { 0, 1, 10, 30, 60, 120 };
        private readonly string[] items = new string[] { "Off", "1 minute", "10 minutes", "30 minutes", "1 hour", "2 hours" };
        private int checkedItem = 0;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.player, container, false);
            instance = this;
            CreatePlayer();
            return view;
        }

        async void CreatePlayer()
        {
            await Task.Delay(1000);

            MainActivity.instance.PrepareSmallPlayer();
            TextView title = MainActivity.instance.FindViewById<TextView>(Resource.Id.playerTitle);
            TextView artist = MainActivity.instance.FindViewById<TextView>(Resource.Id.playerArtist);
            imgView = MainActivity.instance.FindViewById<ImageView>(Resource.Id.playerAlbum);
            TextView NextTitle = MainActivity.instance.FindViewById<TextView>(Resource.Id.nextTitle);
            TextView NextAlbum = MainActivity.instance.FindViewById<TextView>(Resource.Id.nextArtist);
            Button ShowQueue = MainActivity.instance.FindViewById<Button>(Resource.Id.showQueue);
            ImageButton smallQueue = MainActivity.instance.FindViewById<ImageButton>(Resource.Id.smallQueue);

            if (!MainActivity.instance.FindViewById<ImageButton>(Resource.Id.playButton).HasOnClickListeners)
            {
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.lastButton).Click += Last_Click;
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.playButton).Click += Play_Click;
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.nextButton).Click += Next_Click;
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.playerSleep).Click += SleepButton_Click;
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.playerPlaylistAdd).Click += AddToPlaylist_Click;
                MainActivity.instance.FindViewById<FloatingActionButton>(Resource.Id.downFAB).Click += Fab_Click;
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.playerDownload).Click += Download_Click;
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.playerYoutube).Click += Youtube_Click;
                ShowQueue.Click += ShowQueue_Click;
                smallQueue.Click += ShowQueue_Click;
            }

            title.Selected = true;
            title.SetMarqueeRepeatLimit(3);
            artist.Selected = true;
            artist.SetMarqueeRepeatLimit(3);

            ((GradientDrawable)ShowQueue.Background).SetStroke(5, ColorStateList.ValueOf(Color.Argb(255, 21, 183, 237)));
            ShowQueue.SetTextColor(Color.Argb(255, 21, 183, 237));

            ((GradientDrawable)smallQueue.Background).SetStroke(5, ColorStateList.ValueOf(Color.Argb(255, 21, 183, 237)));

            if (MainActivity.Theme == 1)
            {
                NextTitle.SetTextColor(Color.White);
                NextAlbum.SetTextColor(Color.White);
                NextAlbum.Alpha = 0.7f;
                smallQueue.ImageTintList = ColorStateList.ValueOf(Color.Argb(255, 255, 255, 255));
            }
            else
            {
                smallQueue.ImageTintList = ColorStateList.ValueOf(Color.Argb(255, 0, 0, 0));
            }

            if (ShowQueue.Height < 100)
            {
                smallQueue.Visibility = ViewStates.Visible;
                ShowQueue.Visibility = ViewStates.Gone;
            }

            bar = MainActivity.instance.FindViewById<SeekBar>(Resource.Id.songTimer);
            bar.ProgressChanged += (sender, e) =>
            {
                if(MusicPlayer.CurrentID() > 0 && MusicPlayer.CurrentID() < MusicPlayer.queue.Count && !MusicPlayer.queue[MusicPlayer.CurrentID()].IsLiveStream)
                    timerStart.Text = DurationToTimer(e.Progress);
            };
            timerStart = MainActivity.instance.FindViewById<TextView>(Resource.Id.timerStart);

            spBar = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.spProgress);
        }

        public async void RefreshPlayer()
        {
            while (MusicPlayer.CurrentID() == -1)
                await Task.Delay(100);

            Song current = MusicPlayer.queue[MusicPlayer.CurrentID()];

            FrameLayout smallPlayer = MainActivity.instance.FindViewById<FrameLayout>(Resource.Id.smallPlayer);
            smallPlayer.FindViewById<TextView>(Resource.Id.spTitle).Text = current.Title;
            smallPlayer.FindViewById<TextView>(Resource.Id.spArtist).Text = current.Artist;
            smallPlayer.FindViewById<ImageView>(Resource.Id.spPlay).SetImageResource(Resource.Drawable.Pause);
            ImageView art = smallPlayer.FindViewById<ImageView>(Resource.Id.spArt);

            if (!current.IsYt)
            {
                var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, current.AlbumArt);

                Picasso.With(Application.Context).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(art);
            }
            else
            {
                Picasso.With(Application.Context).Load(current.Album).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(art);
            }

            TextView title = MainActivity.instance.FindViewById<TextView>(Resource.Id.playerTitle);
            TextView artist = MainActivity.instance.FindViewById<TextView>(Resource.Id.playerArtist);
            imgView = MainActivity.instance.FindViewById<ImageView>(Resource.Id.playerAlbum);
            title.Text = current.Title;
            artist.Text = current.Artist;

            if (!errorState)
            {
                if (MusicPlayer.isRunning)
                {
                    MainActivity.instance.FindViewById<ImageButton>(Resource.Id.playButton).SetImageResource(Resource.Drawable.Pause);
                    smallPlayer.FindViewById<ImageButton>(Resource.Id.spPlay).SetImageResource(Resource.Drawable.Pause);
                }
                else
                {
                    MainActivity.instance.FindViewById<ImageButton>(Resource.Id.playButton).SetImageResource(Resource.Drawable.Play);
                    smallPlayer.FindViewById<ImageButton>(Resource.Id.spPlay).SetImageResource(Resource.Drawable.Play);
                }
            }

            Bitmap icon = null;
            if (current.AlbumArt == -1)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        icon = Picasso.With(Application.Context).Load(current.Album).Error(Resource.Drawable.noAlbum).Placeholder(Resource.Drawable.noAlbum).Resize(1080, 1080).CenterCrop().Get();
                    }
                    catch (Exception)
                    {
                        icon = Picasso.With(Application.Context).Load(Resource.Drawable.noAlbum).Get();
                    }
                });
            }
            else
            {
                Android.Net.Uri songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                Android.Net.Uri iconURI = ContentUris.WithAppendedId(songCover, current.AlbumArt);

                await Task.Run(() =>
                {
                    try
                    {
                        icon = Picasso.With(Application.Context).Load(iconURI).Error(Resource.Drawable.noAlbum).Placeholder(Resource.Drawable.noAlbum).NetworkPolicy(NetworkPolicy.Offline).Resize(1080, 1080).CenterCrop().Get();
                    }
                    catch (Exception)
                    {
                        icon = Picasso.With(Application.Context).Load(Resource.Drawable.noAlbum).Get();
                    }
                });
            }

            imgView.SetImageBitmap(icon);
            Palette.From(icon).MaximumColorCount(28).Generate(this);

            if (current.IsYt)
            {
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.playerDownload).Visibility = ViewStates.Visible;
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.playerYoutube).Visibility = ViewStates.Visible;
            }
            else
            {
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.playerDownload).Visibility = ViewStates.Gone;
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.playerYoutube).Visibility = ViewStates.Gone;
            }

            bool asNext = MusicPlayer.queue.Count > MusicPlayer.CurrentID() + 1;
            if (asNext)
            {
                Song next = MusicPlayer.queue[MusicPlayer.CurrentID() + 1];
                MainActivity.instance.FindViewById<TextView>(Resource.Id.nextTitle).Text = "Next music:";
                MainActivity.instance.FindViewById<TextView>(Resource.Id.nextArtist).Text = next.Title;
                ImageView nextArt = MainActivity.instance.FindViewById<ImageView>(Resource.Id.nextArt);

                if (next.Album == null)
                {
                    var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                    var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, next.AlbumArt);

                    Picasso.With(MainActivity.instance).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
                }
                else
                {
                    Picasso.With(MainActivity.instance).Load(next.Album).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
                }
            }
            else if (MusicPlayer.repeat)
            {
                Song next = MusicPlayer.queue[0];
                MainActivity.instance.FindViewById<TextView>(Resource.Id.nextTitle).Text = "Next music:";
                MainActivity.instance.FindViewById<TextView>(Resource.Id.nextArtist).Text = next.Title;
                ImageView nextArt = MainActivity.instance.FindViewById<ImageView>(Resource.Id.nextArt);

                if (next.Album == null)
                {
                    var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                    var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, next.AlbumArt);

                    Picasso.With(MainActivity.instance).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
                }
                else
                {
                    Picasso.With(MainActivity.instance).Load(next.Album).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
                }
            }
            else
            {
                MainActivity.instance.FindViewById<TextView>(Resource.Id.nextTitle).Text = "Next music:";
                MainActivity.instance.FindViewById<TextView>(Resource.Id.nextArtist).Text = "Nothing.";

                ImageView nextArt = MainActivity.instance.FindViewById<ImageView>(Resource.Id.nextArt);
                Picasso.With(MainActivity.instance).Load(Resource.Drawable.noAlbum).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
            }

            if (MusicPlayer.player != null && bar != null)
            {
                while (MusicPlayer.Duration < 1)
                    await Task.Delay(100);

                if (current.IsLiveStream)
                {
                    bar.Max = 1;
                    bar.Progress = 1;
                    spBar.Max = 1;
                    spBar.Progress = 1;
                    timerStart.Text = "";
                    MainActivity.instance.FindViewById<TextView>(Resource.Id.timerEnd).Text = "🔴 LIVE";
                }
                else
                {
                    bar.Max = (int)MusicPlayer.Duration;
                    MusicPlayer.SetSeekBar(bar);
                    timerStart.Text = DurationToTimer((int)MusicPlayer.CurrentPosition);
                    MainActivity.instance.FindViewById<TextView>(Resource.Id.timerEnd).Text = DurationToTimer((int)MusicPlayer.player.Duration);
                    spBar.Max = (int)MusicPlayer.Duration;
                    spBar.Progress = (int)MusicPlayer.CurrentPosition;

                    handler.PostDelayed(UpdateSeekBar, 1000);
                }
            }
        }

        public async void UpdateNext()
        {
            await Task.Delay(10);
            bool asNext = MusicPlayer.queue.Count > MusicPlayer.CurrentID() + 1;
            if (asNext)
            {
                Song next = MusicPlayer.queue[MusicPlayer.CurrentID() + 1];
                MainActivity.instance.FindViewById<TextView>(Resource.Id.nextTitle).Text = "Next music:";
                MainActivity.instance.FindViewById<TextView>(Resource.Id.nextArtist).Text = next.Title;
                ImageView nextArt = MainActivity.instance.FindViewById<ImageView>(Resource.Id.nextArt);

                if (next.Album == null)
                {
                    var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                    var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, next.AlbumArt);

                    Picasso.With(MainActivity.instance).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
                }
                else
                {
                    Picasso.With(MainActivity.instance).Load(next.Album).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
                }
            }
            else if (MusicPlayer.repeat)
            {
                Song next = MusicPlayer.queue[0];
                MainActivity.instance.FindViewById<TextView>(Resource.Id.nextTitle).Text = "Next music:";
                MainActivity.instance.FindViewById<TextView>(Resource.Id.nextArtist).Text = next.Title;
                ImageView nextArt = MainActivity.instance.FindViewById<ImageView>(Resource.Id.nextArt);

                if (next.Album == null)
                {
                    var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                    var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, next.AlbumArt);

                    Picasso.With(MainActivity.instance).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
                }
                else
                {
                    Picasso.With(MainActivity.instance).Load(next.Album).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
                }
            }
            else
            {
                MainActivity.instance.FindViewById<TextView>(Resource.Id.nextTitle).Text = "Next music:";
                MainActivity.instance.FindViewById<TextView>(Resource.Id.nextArtist).Text = "Nothing.";

                ImageView nextArt = MainActivity.instance.FindViewById<ImageView>(Resource.Id.nextArt);
                Picasso.With(MainActivity.instance).Load(Resource.Drawable.noAlbum).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(nextArt);
            }
        }

        public void Buffering()
        {
            ImageButton play = MainActivity.instance.FindViewById<ImageButton>(Resource.Id.playButton);
            ProgressBar buffer = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.playerBuffer);
            buffer.Visibility = ViewStates.Visible;
            buffer.SetY(play.GetY());
            play.Visibility = ViewStates.Gone;

            ProgressBar smallBuffer = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.spBuffer);
            ImageButton smallPlay = MainActivity.instance.FindViewById<ImageButton>(Resource.Id.spPlay);
            smallBuffer.Visibility = ViewStates.Visible;
            smallPlay.Visibility = ViewStates.Invisible;
        }

        public void Error()
        {
            ImageButton play = MainActivity.instance.FindViewById<ImageButton>(Resource.Id.playButton);
            ProgressBar buffer = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.playerBuffer);
            buffer.Visibility = ViewStates.Gone;
            play.Visibility = ViewStates.Visible;
            play.SetImageResource(Resource.Drawable.Error);

            ProgressBar smallBuffer = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.spBuffer);
            ImageButton smallPlay = MainActivity.instance.FindViewById<ImageButton>(Resource.Id.spPlay);
            smallBuffer.Visibility = ViewStates.Gone;
            smallPlay.Visibility = ViewStates.Visible;
            smallPlay.SetImageResource(Resource.Drawable.Error);

            errorState = true;
        }

        public void Ready()
        {
            ImageButton play = MainActivity.instance.FindViewById<ImageButton>(Resource.Id.playButton);
            ProgressBar buffer = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.playerBuffer);
            if (buffer == null || play == null)
                return;
            buffer.Visibility = ViewStates.Gone;
            play.Visibility = ViewStates.Visible;
            if(MusicPlayer.isRunning)
                play.SetImageResource(Resource.Drawable.Pause);
            else
                play.SetImageResource(Resource.Drawable.Play);

            errorState = false;
            ProgressBar smallBuffer = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.spBuffer);
            ImageButton smallPlay = MainActivity.instance.FindViewById<ImageButton>(Resource.Id.spPlay);
            smallBuffer.Visibility = ViewStates.Gone;
            smallPlay.Visibility = ViewStates.Visible;

        }

        private void Download_Click(object sender, EventArgs e)
        {
            Song song = MusicPlayer.queue[MusicPlayer.CurrentID()];
            YoutubeEngine.Download(song.Title, song.youtubeID);
        }

        private void Youtube_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse("vnd.youtube://" + MusicPlayer.queue[MusicPlayer.CurrentID()].youtubeID));
            StartActivity(intent);
        }

        public void Stoped()
        {
            MainActivity.instance.ShowSmallPlayer();
            MainActivity.instance.SheetBehavior.State = BottomSheetBehavior.StateCollapsed;
        }

        private void AddToPlaylist_Click(object sender, EventArgs e)
        {
            Browse.act = MainActivity.instance;
            Browse.inflater = MainActivity.instance.LayoutInflater;
            Browse.GetPlaylist(MusicPlayer.queue[MusicPlayer.CurrentID()]);
        }

        public void UpdateSeekBar()
        {
            if (!MusicPlayer.isRunning)
            {
                handler.RemoveCallbacks(UpdateSeekBar);
                return;
            }
            if(MusicPlayer.autoUpdateSeekBar)
            {
                bar.Progress = (int)MusicPlayer.CurrentPosition;
                timerStart.Text = DurationToTimer((int)MusicPlayer.CurrentPosition);
            }
            spBar.Progress = (int)MusicPlayer.CurrentPosition;
            handler.PostDelayed(UpdateSeekBar, 1000);
        }

        private string DurationToTimer(int duration)
        {
            int hours = duration / 600000;
            int minutes = duration / 60000 % 60;
            int seconds = duration / 1000 % 60;

            string hour = hours.ToString();
            string min = minutes.ToString();
            string sec = seconds.ToString();
            if (hour.Length == 1)
                hour = "0" + hour;
            if (min.Length == 1)
                min = "0" + min;
            if (sec.Length == 1)
                sec = "0" + sec;

            return (hours == 0) ? (min + ":" + sec) : (hour + ":" + min + ":" + sec);
        }

        private void Fab_Click(object sender, EventArgs e)
        {
            MainActivity.instance.ShowSmallPlayer();
            MainActivity.instance.SheetBehavior.State = BottomSheetBehavior.StateCollapsed;
        }

        private void ShowQueue_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(MainActivity.instance, typeof(Queue));
            MainActivity.instance.StartActivity(intent);
        }

        private void Last_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(MainActivity.instance, typeof(MusicPlayer));
            intent.SetAction("Previus");
            MainActivity.instance.StartService(intent);
        }

        private void Play_Click(object sender, EventArgs e)
        {
            if (errorState)
            {
                MusicPlayer.instance?.Resume();
                errorState = false;
                return;
            }

            Intent intent = new Intent(MainActivity.instance, typeof(MusicPlayer));
            intent.SetAction("Pause");
            MainActivity.instance.StartService(intent);
        }

        private void Next_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(MainActivity.instance, typeof(MusicPlayer));
            intent.SetAction("Next");
            MainActivity.instance.StartService(intent);
        }

        public void SleepButton_Click(object sender, EventArgs e)
        {
            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme);
            builder.SetTitle("Sleep in :");
            builder.SetSingleChoiceItems(items, checkedItem, ((senders, eventargs) => { checkedItem = eventargs.Which; }));
            builder.SetPositiveButton("Ok", ((senders, args) => { Sleep(timers[checkedItem]); }));
            builder.SetNegativeButton("Cancel", ((senders, args) => { }));
            builder.Show();
        }

        void Sleep(int time)
        {
            Intent intent = new Intent(MainActivity.instance, typeof(Sleeper));
            intent.PutExtra("time", time);
            MainActivity.instance.StartService(intent);
        }

        public void OnGenerated(Palette palette)
        {
            List<Palette.Swatch> swatches = palette.Swatches.OrderBy(x => x.Population).ToList();
            int i = swatches.Count - 1;
            Palette.Swatch swatch = palette.MutedSwatch;

            if (swatch == null && swatches.Count == 0)
                return;

            while (swatch == null)
            {
                swatch = swatches[i];
                i--;

                if (i == -1 && swatch == null)
                    return;
            }

            Palette.Swatch accent = null;
            if (IsColorDark(swatch.Rgb))
            {
                accent = palette.LightVibrantSwatch;

                if (accent == null)
                    accent = palette.LightMutedSwatch;

                if (accent == null)
                    accent = swatch;
            }
            else
            {
                accent = palette.DarkVibrantSwatch;

                if (accent == null)
                    accent = palette.DarkMutedSwatch;

                if (accent == null)
                    accent = swatch;
            }

            Color text = Color.Argb(Color.GetAlphaComponent(swatch.BodyTextColor), Color.GetRedComponent(swatch.BodyTextColor), Color.GetGreenComponent(swatch.BodyTextColor), Color.GetBlueComponent(swatch.BodyTextColor));
            Color background = Color.Argb(Color.GetAlphaComponent(swatch.Rgb), Color.GetRedComponent(swatch.Rgb), Color.GetGreenComponent(swatch.Rgb), Color.GetBlueComponent(swatch.Rgb));
            Color accentColor = Color.Argb(Color.GetAlphaComponent(accent.Rgb), Color.GetRedComponent(accent.Rgb), Color.GetGreenComponent(accent.Rgb), Color.GetBlueComponent(accent.Rgb));
            MainActivity.instance.FindViewById<TextView>(Resource.Id.playerTitle).SetTextColor(text);
            MainActivity.instance.FindViewById<TextView>(Resource.Id.playerArtist).SetTextColor(text);
            MainActivity.instance.FindViewById<TextView>(Resource.Id.spTitle).SetTextColor(text);
            MainActivity.instance.FindViewById<TextView>(Resource.Id.spArtist).SetTextColor(text);
            MainActivity.instance.FindViewById<FloatingActionButton>(Resource.Id.downFAB).BackgroundTintList = ColorStateList.ValueOf(accentColor);
            MainActivity.instance.FindViewById<FloatingActionButton>(Resource.Id.downFAB).RippleColor = accent.Rgb;

            //Reveal for the player
            View reveal = MainActivity.instance.FindViewById<View>(Resource.Id.reveal);
            int centerX, centerY;
            float endRadius;
            if (playNext == true)
            {
                centerX = 0;
                centerY = reveal.Height / 2;
                endRadius = reveal.Width * 1.3f;
            }
            else if(playNext == null)
            {
                centerX = reveal.Width / 2;
                centerY = reveal.Height;
                endRadius = reveal.Width / 1.5f;
            }
            else
            {
                centerX = reveal.Width;
                centerY = reveal.Height / 2;
                endRadius = reveal.Width * 1.3f;
            }
            Animator anim = ViewAnimationUtils.CreateCircularReveal(reveal, centerX, centerY, 0, endRadius);
            anim.AnimationStart += (sender, e) => { reveal.SetBackgroundColor(background); };
            anim.AnimationEnd += (sender, e) => { MainActivity.instance?.FindViewById<RelativeLayout>(Resource.Id.infoPanel).SetBackgroundColor(background); };
            anim.SetDuration(500);
            anim.StartDelay = 200;
            anim.Start();

            //Reveal for the smallPlayer
            if (prepared)
            {
                View spReveal = MainActivity.instance.FindViewById<View>(Resource.Id.spReveal);
                Animator spAnim = ViewAnimationUtils.CreateCircularReveal(spReveal, playNext == false ? spReveal.Width : 0, spReveal.Height / 2, 0, spReveal.Width);
                spAnim.AnimationStart += (sender, e) => { spReveal.SetBackgroundColor(background); };
                spAnim.AnimationEnd += (sender, e) => { MainActivity.instance.FindViewById<NestedScrollView>(Resource.Id.playerSheet).SetBackgroundColor(background); };
                spAnim.SetDuration(500);
                spAnim.StartDelay = 10;
                spAnim.Start();
            }
            else
            {
                prepared = true;
                MainActivity.instance.FindViewById<NestedScrollView>(Resource.Id.playerSheet).SetBackgroundColor(background);
            }
            playNext = null;

            if (bar == null)
                bar = MainActivity.instance.FindViewById<SeekBar>(Resource.Id.songTimer);

            if(spBar == null)
                spBar = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.spProgress);

            bar.ProgressTintList = ColorStateList.ValueOf(accentColor);
            bar.ThumbTintList = ColorStateList.ValueOf(accentColor);
            bar.ProgressBackgroundTintList = ColorStateList.ValueOf(Color.Argb(87, accentColor.R, accentColor.G, accentColor.B));
            spBar.ProgressTintList = ColorStateList.ValueOf(accentColor);
            spBar.ProgressBackgroundTintList = ColorStateList.ValueOf(Color.Argb(87, accentColor.R, accentColor.G, accentColor.B));

            if (IsColorDark(accent.Rgb))
            {
                MainActivity.instance.FindViewById<FloatingActionButton>(Resource.Id.downFAB).ImageTintList = ColorStateList.ValueOf(Color.White);
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.spNext).ImageTintList = ColorStateList.ValueOf(Color.White);
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.spPlay).ImageTintList = ColorStateList.ValueOf(Color.White);
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.spLast).ImageTintList = ColorStateList.ValueOf(Color.White);
            }
            else
            {
                MainActivity.instance.FindViewById<FloatingActionButton>(Resource.Id.downFAB).ImageTintList = ColorStateList.ValueOf(Color.Black);
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.spNext).ImageTintList = ColorStateList.ValueOf(Color.Black);
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.spPlay).ImageTintList = ColorStateList.ValueOf(Color.Black);
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.spLast).ImageTintList = ColorStateList.ValueOf(Color.Black);
            }
        }

        public bool IsColorDark(int color)
        {
            double darkness = 1 - (0.299 * Color.GetRedComponent(color) + 0.587 * Color.GetGreenComponent(color) + 0.114 * Color.GetBlueComponent(color)) / 255;
            if (darkness < 0.7)
            {
                return false; // It's a light color
            }
            else
            {
                return true; // It's a dark color
            }
        }
    }

    public class PlayerCallback : BottomSheetBehavior.BottomSheetCallback
    {
        private Activity context;
        private NestedScrollView sheet;
        private BottomNavigationView bottomView;
        private FrameLayout smallPlayer;
        private View playerView;
        private LinearLayout quickPlay;
        private CoordinatorLayout snackBar;
        private bool Refreshed = false;
        private SheetMovement movement = SheetMovement.Unknow;

        public PlayerCallback(Activity context)
        {
            this.context = context;
            sheet = context.FindViewById<NestedScrollView>(Resource.Id.playerSheet);
            bottomView = context.FindViewById<BottomNavigationView>(Resource.Id.bottomView);
            smallPlayer = context.FindViewById<FrameLayout>(Resource.Id.smallPlayer);
            playerView = context.FindViewById(Resource.Id.playerView);
            quickPlay = context.FindViewById<LinearLayout>(Resource.Id.quickPlayLinear);
            snackBar = context.FindViewById<CoordinatorLayout>(Resource.Id.snackBar);
        }

        public override void OnSlide(View bottomSheet, float slideOffset)
        {
            if(movement == SheetMovement.Unknow)
            {
                if (slideOffset > 0)
                    movement = SheetMovement.Expanding;
                else if (slideOffset < 0)
                    movement = SheetMovement.Hidding;
            }

            if(movement == SheetMovement.Expanding && 0 <= slideOffset && slideOffset <= 1)
            {
                sheet.Alpha = 1;
                bottomView.TranslationY = (int)((56 * context.Resources.DisplayMetrics.Density + 0.5f) * slideOffset);
                sheet.TranslationY = -(int)((56 * context.Resources.DisplayMetrics.Density + 0.5f) * (1 - slideOffset));

                playerView.Alpha = Math.Max(0, (slideOffset - 0.5f) * 2.5f);
                smallPlayer.Alpha = Math.Max(0, 1 - slideOffset * 2);
                quickPlay.ScaleX = Math.Max(0, 1 - slideOffset * 2);
                quickPlay.ScaleY = Math.Max(0, 1 - slideOffset * 2);
                snackBar.TranslationY = (int)((50 * context.Resources.DisplayMetrics.Density + 0.5f) * slideOffset);

                if (!Refreshed && slideOffset > .3)
                {
                    Refreshed = true;
                    Player.instance.RefreshPlayer();
                }
                else if (slideOffset < .3)
                    Refreshed = false;
            }
            else if(movement == SheetMovement.Hidding && - 1 <= slideOffset && slideOffset < 0)
            {
                sheet.Alpha = 1 + slideOffset;
                MusicPlayer.instance?.ChangeVolume(MusicPlayer.instance.volume * (1 + slideOffset));
            }
        }

        public override void OnStateChanged(View bottomSheet, int newState)
        {
            if (newState == BottomSheetBehavior.StateExpanded)
            {
                sheet.Alpha = 1;
                playerView.Alpha = 1;
                smallPlayer.Alpha = 0;
                bottomSheet.TranslationY = (int)(56 * context.Resources.DisplayMetrics.Density + 0.5f);
                sheet.TranslationY = 0;
                snackBar.TranslationY = (int)(50 * context.Resources.DisplayMetrics.Density + 0.5f);
                movement = SheetMovement.Unknow;
            }
            else if (newState == BottomSheetBehavior.StateCollapsed)
                movement = SheetMovement.Unknow;
            else if(newState == BottomSheetBehavior.StateHidden)
            {
                movement = SheetMovement.Unknow;
                if (MusicPlayer.userStopped)
                {
                    MusicPlayer.queue = new List<Song>();
                    MusicPlayer.UpdateQueueDataBase();
                    if (Home.adapterItems.Count > 0 && Home.adapterItems[0].SectionTitle == "Queue")
                    {
                        Home.instance?.adapter.NotifyItemRemoved(0);
                        Home.adapterItems.RemoveAt(0);
                    }
                }
                Intent intent = new Intent(context, typeof(MusicPlayer));
                intent.SetAction("Stop");
                context.StartService(intent);
                sheet.Alpha = 1;
                MusicPlayer.instance?.ChangeVolume(MusicPlayer.instance.volume);
                MusicPlayer.userStopped = true;
            }
        }
    }

    public enum SheetMovement { Expanding, Hidding, Unknow }
}