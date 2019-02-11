using Android.Animation;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Gms.Cast.Framework;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.Widget;
using Android.Support.V7.Graphics;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Text.Style;
using Android.Util;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.Portable_Class;
using MusicApp.Resources.values;
using Square.Picasso;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaRouteButton = Android.Support.V7.App.MediaRouteButton;

namespace MusicApp
{
    [Register("MusicApp/Player")]
    [Activity(Label = "Player", Theme = "@style/Theme", ScreenOrientation = ScreenOrientation.Portrait, LaunchMode = LaunchMode.SingleTop)]
    public class Player : Android.Support.V4.App.Fragment, Palette.IPaletteAsyncListener
    {
        public static Player instance;
        public Handler handler = new Handler();
        public static bool errorState = false;
        public bool? playNext = true;

        private SeekBar bar;
        private ProgressBar spBar;
        private TextView timer;
        private ImageView imgView;
        private DrawerLayout DrawerLayout;
        private bool prepared = false;
        private readonly int[] timers = new int[] { 0, 2, 10, 30, 60, 120 };
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
            await Task.Delay(300);

            DisplayMetrics metrics = new DisplayMetrics();
            MainActivity.instance.WindowManager.DefaultDisplay.GetMetrics(metrics);
            MainActivity.instance.FindViewById(Resource.Id.playerContainer).LayoutParameters.Height = metrics.HeightPixels;

            await Task.Delay(700);

            CastButtonFactory.SetUpMediaRouteButton(MainActivity.instance, MainActivity.instance.FindViewById<MediaRouteButton>(Resource.Id.castButton));
            MainActivity.instance.PrepareSmallPlayer();

            if (!MainActivity.instance.FindViewById<ImageButton>(Resource.Id.playButton).HasOnClickListeners)
            {
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.downButton).Click += Down_Click;
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.showQueue).Click += ShowQueue_Click;
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.lastButton).Click += Last_Click;
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.playButton).Click += Play_Click;
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.nextButton).Click += Next_Click;
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.moreButton).Click += More;
            }

            imgView = MainActivity.instance.FindViewById<ImageView>(Resource.Id.playerAlbum);
            timer = MainActivity.instance.FindViewById<TextView>(Resource.Id.timer);
            bar = MainActivity.instance.FindViewById<SeekBar>(Resource.Id.songTimer);
            bar.ProgressChanged += (sender, e) =>
            {
                if(!MusicPlayer.isLiveStream)
                    timer.Text = string.Format("{0} | {1}", DurationToTimer(e.Progress), DurationToTimer((int)MusicPlayer.Duration));
            };

            spBar = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.spProgress);

            DrawerLayout = (DrawerLayout)MainActivity.instance.FindViewById(Resource.Id.playerView).Parent;
            MainActivity.instance.FindViewById(Resource.Id.queueParent).LayoutParameters.Width = (int)(DrawerLayout.Width * 0.75f);
            ((FrameLayout.LayoutParams)MainActivity.instance.FindViewById(Resource.Id.queue).LayoutParameters).TopMargin = Resources.GetDimensionPixelSize(Resources.GetIdentifier("status_bar_height", "dimen", "android"));
        }

        public async void RefreshPlayer()
        {
            while (MainActivity.instance == null || MusicPlayer.CurrentID() == -1)
                await Task.Delay(100);

            Song current = await MusicPlayer.GetItem();
            
            FrameLayout smallPlayer = MainActivity.instance.FindViewById<FrameLayout>(Resource.Id.smallPlayer);
            smallPlayer.FindViewById<TextView>(Resource.Id.spTitle).Text = current.Title;
            smallPlayer.FindViewById<TextView>(Resource.Id.spArtist).Text = current.Artist;
            smallPlayer.FindViewById<ImageView>(Resource.Id.spPlay).SetImageResource(Resource.Drawable.Pause);
            ImageView art = smallPlayer.FindViewById<ImageView>(Resource.Id.spArt);

            if (!current.IsYt)
            {
                var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, current.AlbumArt);

                Picasso.With(Application.Context).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.noAlbum).Resize(400, 400).CenterCrop().Into(art);
            }
            else
            {
                Picasso.With(Application.Context).Load(current.Album).Placeholder(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).Into(art);
            }

            TextView title = MainActivity.instance.FindViewById<TextView>(Resource.Id.playerTitle);
            TextView artist = MainActivity.instance.FindViewById<TextView>(Resource.Id.playerArtist);
            imgView = MainActivity.instance.FindViewById<ImageView>(Resource.Id.playerAlbum);
            SpannableString titleText = new SpannableString(current.Title);
            titleText.SetSpan(new BackgroundColorSpan(Color.ParseColor("#BF000000")), 0, current.Title.Length, SpanTypes.InclusiveInclusive);
            title.TextFormatted = titleText;
            SpannableString artistText = new SpannableString(current.Artist);
            artistText.SetSpan(new BackgroundColorSpan(Color.ParseColor("#BF000000")), 0, current.Artist.Length, SpanTypes.InclusiveInclusive);
            artist.TextFormatted = artistText;

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
                        icon = Picasso.With(Application.Context).Load(current.Album).Error(Resource.Drawable.noAlbum).Placeholder(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).Get();
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

            if (bar != null)
            {
                while (MusicPlayer.Duration < 2)
                    await Task.Delay(100);

                if (current.IsLiveStream)
                {
                    bar.Max = 1;
                    bar.Progress = 1;
                    spBar.Max = 1;
                    spBar.Progress = 1;
                    timer.Text = "🔴 LIVE";
                }
                else
                {
                    bar.Max = (int)MusicPlayer.Duration;
                    MusicPlayer.SetSeekBar(bar);
                    timer.Text = string.Format("{0} | {1}", DurationToTimer((int)MusicPlayer.CurrentPosition), DurationToTimer((int)MusicPlayer.Duration));
                    spBar.Max = (int)MusicPlayer.Duration;
                    spBar.Progress = (int)MusicPlayer.CurrentPosition;

                    handler.PostDelayed(UpdateSeekBar, 1000);
                }
            }
        }

        public async void UpdateNext()
        {
            await Task.Delay(10);
            //bool asNext = MusicPlayer.queue.Count > MusicPlayer.CurrentID() + 1;
            //if (asNext)
            //{
            //    Song next = await MusicPlayer.GetItem(MusicPlayer.CurrentID() + 1);
            //    MainActivity.instance.FindViewById<TextView>(Resource.Id.nextTitle).Text = Resources.GetString(Resource.String.up_next);
            //    MainActivity.instance.FindViewById<TextView>(Resource.Id.nextArtist).Text = next.Title;
            //    ImageView nextArt = MainActivity.instance.FindViewById<ImageView>(Resource.Id.nextArt);

            //    if (next.Album == null)
            //    {
            //        var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
            //        var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, next.AlbumArt);

            //        Picasso.With(MainActivity.instance).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.noAlbum).Resize(400, 400).CenterCrop().Into(nextArt);
            //    }
            //    else
            //    {
            //        Picasso.With(MainActivity.instance).Load(next.Album).Placeholder(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).Into(nextArt);
            //    }
            //}
            //else if (MusicPlayer.useAutoPlay)
            //{
            //    MainActivity.instance.FindViewById<TextView>(Resource.Id.nextTitle).Text = Resources.GetString(Resource.String.up_next);
            //    ImageView nextArt = MainActivity.instance.FindViewById<ImageView>(Resource.Id.nextArt);

            //    Song next = await MusicPlayer.GetItem(MusicPlayer.CurrentID() + 1);
            //    if(next != null)
            //    {
            //        MainActivity.instance.FindViewById<TextView>(Resource.Id.nextArtist).Text = next.Title;

            //        if (next.Album == null)
            //        {
            //            var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
            //            var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, next.AlbumArt);

            //            Picasso.With(MainActivity.instance).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.noAlbum).Resize(400, 400).CenterCrop().Into(nextArt);
            //        }
            //        else
            //        {
            //            Picasso.With(MainActivity.instance).Load(next.Album).Placeholder(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).Into(nextArt);
            //        }
            //    }
            //    else
            //    {
            //        MainActivity.instance.FindViewById<TextView>(Resource.Id.nextArtist).Text = Resources.GetString(Resource.String.next_loading);
            //        Picasso.With(MainActivity.instance).Load(Resource.Drawable.noAlbum).Into(nextArt);
            //        MusicPlayer.instance?.GenerateAutoPlay(false);
            //    }
            //}
            //else if (MusicPlayer.repeat)
            //{
            //    Song next = await MusicPlayer.GetItem(0);
            //    MainActivity.instance.FindViewById<TextView>(Resource.Id.nextTitle).Text = Resources.GetString(Resource.String.up_next);
            //    MainActivity.instance.FindViewById<TextView>(Resource.Id.nextArtist).Text = next.Title;
            //    ImageView nextArt = MainActivity.instance.FindViewById<ImageView>(Resource.Id.nextArt);

            //    if (next.Album == null)
            //    {
            //        var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
            //        var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, next.AlbumArt);

            //        Picasso.With(MainActivity.instance).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.noAlbum).Resize(400, 400).CenterCrop().Into(nextArt);
            //    }
            //    else
            //    {
            //        Picasso.With(MainActivity.instance).Load(next.Album).Placeholder(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).Into(nextArt);
            //    }
            //}
            //else
            //{
            //    MainActivity.instance.FindViewById<TextView>(Resource.Id.nextTitle).Text = Resources.GetString(Resource.String.up_next);
            //    MainActivity.instance.FindViewById<TextView>(Resource.Id.nextArtist).Text = Resources.GetString(Resource.String.nothing);

            //    ImageView nextArt = MainActivity.instance.FindViewById<ImageView>(Resource.Id.nextArt);
            //    Picasso.With(MainActivity.instance).Load(Resource.Drawable.noAlbum).Placeholder(Resource.Drawable.noAlbum).Resize(400, 400).CenterCrop().Into(nextArt);
            //}
        }

        public void Buffering()
        {
            ImageButton play = MainActivity.instance.FindViewById<ImageButton>(Resource.Id.playButton);
            ProgressBar buffer = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.playerBuffer);
            buffer.Visibility = ViewStates.Visible;
            buffer.IndeterminateTintList = ColorStateList.ValueOf(Color.White);
            buffer.SetY(play.GetY());
            play.Visibility = ViewStates.Gone;

            MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.spBuffer).Visibility = ViewStates.Visible;
            MainActivity.instance.FindViewById<ImageButton>(Resource.Id.spPlay).Visibility = ViewStates.Invisible;
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

        public void Stoped()
        {
            MainActivity.instance.ShowSmallPlayer();
            MainActivity.instance.SheetBehavior.State = BottomSheetBehavior.StateCollapsed;
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
                timer.Text = string.Format("{0} | {1}", DurationToTimer((int)MusicPlayer.CurrentPosition), DurationToTimer((int)MusicPlayer.Duration));
            }
            spBar.Progress = (int)MusicPlayer.CurrentPosition;
            handler.PostDelayed(UpdateSeekBar, 1000);
        }

        private string DurationToTimer(int duration)
        {
            int hours = duration / 3600000;
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

        private void Down_Click(object sender, EventArgs e)
        {
            MainActivity.instance.ShowSmallPlayer();
            MainActivity.instance.SheetBehavior.State = BottomSheetBehavior.StateCollapsed;
        }

        private void ShowQueue_Click(object sender, EventArgs e)
        {
            Queue.instance.Refresh();
            DrawerLayout.OpenDrawer((int)GravityFlags.Start);
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

        private async void More(object s, EventArgs e)
        {
            Song item = await MusicPlayer.GetItem();

            BottomSheetDialog bottomSheet = new BottomSheetDialog(MainActivity.instance);
            View bottomView = MainActivity.instance.LayoutInflater.Inflate(Resource.Layout.BottomSheet, null);
            bottomView.FindViewById<TextView>(Resource.Id.bsTitle).Text = item.Title;
            bottomView.FindViewById<TextView>(Resource.Id.bsArtist).Text = item.Artist;
            if (item.Album == null)
            {
                var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
                var songAlbumArtUri = ContentUris.WithAppendedId(songCover, item.AlbumArt);

                Picasso.With(MainActivity.instance).Load(songAlbumArtUri).Placeholder(Resource.Drawable.noAlbum).Resize(400, 400).CenterCrop().Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
            }
            else
            {
                Picasso.With(MainActivity.instance).Load(item.Album).Placeholder(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).Into(bottomView.FindViewById<ImageView>(Resource.Id.bsArt));
            }
            bottomSheet.SetContentView(bottomView);

            List<BottomSheetAction> actions = new List<BottomSheetAction>
            {
                new BottomSheetAction(Resource.Drawable.Timer, Resources.GetString(Resource.String.timer), (sender, eventArg) => { SleepDialog(); bottomSheet.Dismiss(); }),
                new BottomSheetAction(Resource.Drawable.PlaylistAdd, Resources.GetString(Resource.String.add_to_playlist), (sender, eventArg) => { Browse.GetPlaylist(item); bottomSheet.Dismiss(); })
            };

            if (item.IsYt)
            {
                actions.AddRange(new BottomSheetAction[]
                {
                    new BottomSheetAction(Resource.Drawable.PlayCircle, Resources.GetString(Resource.String.create_mix_from_song), (sender, eventArg) =>
                    {
                        YoutubeEngine.CreateMix(item);
                        bottomSheet.Dismiss();
                    }),
                    new BottomSheetAction(Resource.Drawable.Download, Resources.GetString(Resource.String.download), (sender, eventArg) =>
                    {
                        YoutubeEngine.Download(item.Title, item.YoutubeID);
                        bottomSheet.Dismiss();
                    }),
                    new BottomSheetAction(Resource.Drawable.OpenInBrowser, Resources.GetString(Resource.String.open_youtube), (sender, eventArg) =>
                    {
                        Intent intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse("vnd.youtube://" + MusicPlayer.queue[MusicPlayer.CurrentID()].YoutubeID));
                        StartActivity(intent);
                        bottomSheet.Dismiss();
                    })
                });
            }
            else
            {
                actions.Add(new BottomSheetAction(Resource.Drawable.Edit, Resources.GetString(Resource.String.edit_metadata), (sender, eventArg) =>
                {
                    Browse.EditMetadata(item);
                    bottomSheet.Dismiss();
                }));
            }

            bottomSheet.FindViewById<ListView>(Resource.Id.bsItems).Adapter = new BottomSheetAdapter(MainActivity.instance, Resource.Layout.BottomSheetText, actions);
            bottomSheet.Show();
        }

        public void SleepDialog()
        {
            string minutes = GetString(Resource.String.minutes);
            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(MainActivity.instance, MainActivity.dialogTheme);
            builder.SetTitle(Resource.String.sleep_timer);
            builder.SetSingleChoiceItems(new string[] { GetString(Resource.String.off), "2 " + minutes, "10 " + minutes, "30 " + minutes, "1 " + GetString(Resource.String.hour), "2 " + GetString(Resource.String.hours) }, checkedItem, ((senders, eventargs) => { checkedItem = eventargs.Which; }));
            builder.SetPositiveButton(Resource.String.ok, ((senders, args) => { Sleep(timers[checkedItem]); }));
            builder.SetNegativeButton(Resource.String.cancel, ((senders, args) => { }));
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
            MainActivity.instance.FindViewById<TextView>(Resource.Id.spTitle).SetTextColor(text);
            MainActivity.instance.FindViewById<TextView>(Resource.Id.spArtist).SetTextColor(text);

            //Reveal for the smallPlayer
            if (prepared)
            {
                View spReveal = MainActivity.instance.FindViewById<View>(Resource.Id.spReveal);
                Animator spAnim = ViewAnimationUtils.CreateCircularReveal(spReveal, playNext == false ? spReveal.Width : 0, spReveal.Height / 2, 0, spReveal.Width);
                spAnim.AnimationStart += (sender, e) => { spReveal.SetBackgroundColor(background); };
                spAnim.AnimationEnd += (sender, e) => { MainActivity.instance.FindViewById(Resource.Id.playersHolder).SetBackgroundColor(background); };
                spAnim.SetDuration(500);
                spAnim.StartDelay = 10;
                spAnim.Start();
            }
            else
            {
                prepared = true;
                MainActivity.instance.FindViewById(Resource.Id.playersHolder).SetBackgroundColor(background);
            }
            playNext = null;

            if (bar == null)
                bar = MainActivity.instance.FindViewById<SeekBar>(Resource.Id.songTimer);

            if (spBar == null)
                spBar = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.spProgress);

            bar.ProgressTintList = ColorStateList.ValueOf(accentColor);
            bar.ThumbTintList = ColorStateList.ValueOf(accentColor);
            bar.ProgressBackgroundTintList = ColorStateList.ValueOf(Color.Argb(87, accentColor.R, accentColor.G, accentColor.B));
            spBar.ProgressTintList = ColorStateList.ValueOf(accentColor);
            spBar.ProgressBackgroundTintList = ColorStateList.ValueOf(Color.Argb(87, accentColor.R, accentColor.G, accentColor.B));

            if (IsColorDark(accent.Rgb))
            {
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.spNext).ImageTintList = ColorStateList.ValueOf(Color.White);
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.spPlay).ImageTintList = ColorStateList.ValueOf(Color.White);
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.spLast).ImageTintList = ColorStateList.ValueOf(Color.White);
                MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.spBuffer).IndeterminateTintList = ColorStateList.ValueOf(Color.White);
            }
            else
            {
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.spNext).ImageTintList = ColorStateList.ValueOf(Color.Black);
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.spPlay).ImageTintList = ColorStateList.ValueOf(Color.Black);
                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.spLast).ImageTintList = ColorStateList.ValueOf(Color.Black);
                MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.spBuffer).IndeterminateTintList = ColorStateList.ValueOf(Color.Black);
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
        private View playerContainer;
        private CoordinatorLayout snackBar;
        private bool Refreshed = false;
        private SheetMovement movement = SheetMovement.Unknow;

        public PlayerCallback(Activity context)
        {
            this.context = context;
            sheet = context.FindViewById<NestedScrollView>(Resource.Id.playerSheet);
            bottomView = context.FindViewById<BottomNavigationView>(Resource.Id.bottomView);
            smallPlayer = context.FindViewById<FrameLayout>(Resource.Id.smallPlayer);
            playerContainer = context.FindViewById(Resource.Id.playerContainer);
            snackBar = context.FindViewById<CoordinatorLayout>(Resource.Id.snackBar);
        }

        public override void OnSlide(View bottomSheet, float slideOffset)
        {
            smallPlayer.Visibility = ViewStates.Visible;

            if (movement == SheetMovement.Unknow)
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

                playerContainer.Alpha = Math.Max(0, (slideOffset - 0.5f) * 2.5f);
                smallPlayer.Alpha = Math.Max(0, 1 - slideOffset * 2);
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
                playerContainer.Alpha = 1;
                smallPlayer.Alpha = 0;
                smallPlayer.Visibility = ViewStates.Gone;
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
                if (!MainActivity.instance.SkipStop)
                {
                    Intent intent = new Intent(context, typeof(MusicPlayer));
                    intent.SetAction("Stop");
                    intent.PutExtra("saveQueue", false);
                    context.StartService(intent);
                }
                MainActivity.instance.SkipStop = false;
                sheet.Alpha = 1;
                MusicPlayer.instance?.ChangeVolume(MusicPlayer.instance.volume);
            }
        }
    }

    public enum SheetMovement { Expanding, Hidding, Unknow }
}