using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Media;
using Android.Support.V7.App;
using Android.Support.V4.Media.Session;
using Android.Graphics;
using MusicApp.Resources.values;
using Android.Database;
using Android.Provider;
using System.Linq;
using System.Threading.Tasks;
using Square.Picasso;

using Uri = Android.Net.Uri;
using Android.Widget;
using Com.Google.Android.Exoplayer2;
using Com.Google.Android.Exoplayer2.Trackselection;
using Com.Google.Android.Exoplayer2.Upstream;
using Com.Google.Android.Exoplayer2.Source;
using Com.Google.Android.Exoplayer2.Extractor;

namespace MusicApp.Resources.Portable_Class
{
    [Service]
    public class MusicPlayer : Service, IPlayerEventListener, AudioManager.IOnAudioFocusChangeListener
    {
        public static SimpleExoPlayer player;
        public static List<Song> queue = new List<Song>();
        public MediaSessionCompat mediaSession;
        public AudioManager audioManager;
        public NotificationManager notificationManager;
        public static bool isRunning = false;
        public static string title;

        private Notification notification;
        private const int notificationID = 1000;


        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override void OnCreate()
        {
            base.OnCreate();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            string file = intent.GetStringExtra("file");

            switch (intent.Action)
            {
                case "Previus":
                    PlayLast();
                    break;

                case "Pause":
                    if(isRunning)
                        Pause();
                    else
                        Resume();
                    break;

                case "Next":
                    PlayNext();
                    break;

                case "RandomPlay":
                    List<string> files = intent.GetStringArrayListExtra("files").ToList();
                    RandomPlay(files);
                    break;

                case "PlayNext":
                    string title = intent.GetStringExtra("title");
                    if (title != null)
                    {
                        string artist = intent.GetStringExtra("artist");
                        string thumbnailURI = intent.GetStringExtra("thumbnailURI");
                        AddToQueue(file, title, artist, thumbnailURI);
                        return StartCommandResult.Sticky;
                    }
                    AddToQueue(file);
                    break;

                case "PlayLast":
                    title = intent.GetStringExtra("title");
                    if (title != null)
                    {
                        string artist = intent.GetStringExtra("artist");
                        string thumbnailURI = intent.GetStringExtra("thumbnailURI");
                        PlayLastInQueue(file, title, artist, thumbnailURI);
                        return StartCommandResult.Sticky;
                    }
                    PlayLastInQueue(file);
                    break;

                case "QueueSwitch":
                    SwitchQueue(file);
                    break;
                case "Stop":
                    if(isRunning)
                        Stop();
                    break;
            }

            if (intent.Action != null)
                return StartCommandResult.Sticky;

            if (file != null)
            {
                string title = intent.GetStringExtra("title");
                if(title != null)
                {
                    string artist = intent.GetStringExtra("artist");
                    string thumbnailURI = intent.GetStringExtra("thumbnailURI");
                    Play(file, title, artist, thumbnailURI);
                    return StartCommandResult.Sticky;
                }
                Play(file);
            }

            return StartCommandResult.Sticky;
        }

        private void InitializeService()
        {
            audioManager = (AudioManager)Application.Context.GetSystemService(AudioService);
            notificationManager = (NotificationManager)Application.Context.GetSystemService(NotificationService);
            DefaultBandwidthMeter bandwithMeter = new DefaultBandwidthMeter();
            AdaptiveTrackSelection.Factory trackSelectionFactory = new AdaptiveTrackSelection.Factory(bandwithMeter);
            TrackSelector trackSelector = new DefaultTrackSelector(trackSelectionFactory);
            player = ExoPlayerFactory.NewSimpleInstance(Application.Context, trackSelector);
            player.PlayWhenReady = true;
            player.AddListener(this);
        }

        public void Play(string filePath, string title = null, string artist = null, string thumbnailURI = null, bool addToQueue = true)
        {
            isRunning = true;
            if (player == null)
                InitializeService();

            if(mediaSession == null)
            {
                mediaSession = new MediaSessionCompat(Application.Context, "MusicApp");
                mediaSession.SetFlags(MediaSessionCompat.FlagHandlesMediaButtons | MediaSessionCompat.FlagHandlesTransportControls);
                PlaybackStateCompat.Builder builder = new PlaybackStateCompat.Builder().SetActions(PlaybackStateCompat.ActionPlay | PlaybackStateCompat.ActionPause);
                mediaSession.SetPlaybackState(builder.Build());
            }

            DefaultDataSourceFactory dataSourceFactory = new DefaultDataSourceFactory(Application.Context, "MusicApp");
            IExtractorsFactory extractorFactory = new DefaultExtractorsFactory();
            Handler handler = new Handler();
            IMediaSource mediaSource = new ExtractorMediaSource(Uri.Parse(filePath), dataSourceFactory, extractorFactory, handler, null);
            var audioFocus = audioManager.RequestAudioFocus(this, Stream.Music, AudioFocus.Gain);
            if (audioFocus != AudioFocusRequest.Granted)
            {
                Console.WriteLine("Can't Get Audio Focus");
                return;
            }
            player.PlayWhenReady = true;
            player.Prepare(mediaSource, true, true);

            Song song = null;
            if(title == null)
                GetTrackSong(filePath, out song);
            else
            {
                song = new Song(title, artist, thumbnailURI, -1, -1, filePath, true);
            }
            CreateNotification(song.GetName(), song.GetArtist(), song.GetAlbumArt(), song.GetAlbum());

            if(addToQueue)
                AddToQueue(song);
        }

        public async void RandomPlay(List<string> filePath)
        {
            Random r = new Random();
            filePath = filePath.OrderBy(x => r.Next()).ToList();
            await Task.Delay(1000);
            Play(filePath[0]);
            await Task.Delay(1000);
            for (int i = 1; i < filePath.Count; i++)
            {
                GetTrackSong(filePath[i], out Song song);
                queue.Add(song);
                await Task.Delay(10);
            }
        }

        public void AddToQueue(string filePath, string title = null, string artist = null, string thumbnailURI = null)
        {
            Song song = null;
            if(title == null)
                GetTrackSong(filePath, out song);
            else
                song = new Song(title, artist, thumbnailURI, -1, -1, filePath, true);
            if (CurrentID() == -1)
                queue.Add(song);
            else
                queue.Insert(CurrentID() + 1, song);
        }

        public void AddToQueue(Song song)
        {
            if (CurrentID() == -1)
                queue.Add(song);
            else
                queue.Insert(CurrentID() + 1, song);
        }

        public void PlayLastInQueue(string filePath)
        {
            GetTrackSong(filePath, out Song song);
            queue.Add(song);
        }

        public void PlayLastInQueue(string filePath, string title, string artist, string thumbnailURI)
        {
            Song song = new Song(title, artist, thumbnailURI, -1, -1, filePath, true);
            queue.Add(song);
        }

        public void PlayLast()
        {
            if (CurrentID() - 1 < 0)
                return;

            Song last = queue[CurrentID() - 1];
            string filePath = last.GetPath();
            SwitchQueue(filePath);
        }

        public void PlayNext()
        {
            if (CurrentID() + 1 > queue.Count - 1)
            {
                Pause();
                return;
            }

            Song next = queue[CurrentID() + 1];
            string filePath = next.GetPath();
            SwitchQueue(filePath);
        }

        void SwitchQueue(string filePath)
        {
            Play(filePath, null, null, null, false);

            GetTrackSong(filePath, out Song song);

            if (Player.instance != null)
                Player.instance.RefreshPlayer();

            RelativeLayout smallPlayer = MainActivity.instance.FindViewById<RelativeLayout>(Resource.Id.smallPlayer);
            smallPlayer.FindViewById<TextView>(Resource.Id.spTitle).Text = song.GetName();
            smallPlayer.FindViewById<TextView>(Resource.Id.spArtist).Text = song.GetArtist();
            ImageView art = smallPlayer.FindViewById<ImageView>(Resource.Id.spArt);

            if(!song.IsYt)
            {
                var songCover = Uri.Parse("content://media/external/audio/albumart");
                var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, song.GetAlbumArt());

                Picasso.With(Application.Context).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Into(art);
            }
            else
            {
                Picasso.With(Application.Context).Load(song.GetAlbum()).Placeholder(Resource.Drawable.MusicIcon).Into(art);
            }
        }

        public static int CurrentID()
        {
            int id = 0;
            foreach (Song song in queue)
            {
                if (song.GetName() == title)
                    return id;
                id++;
            }
            return -1;
        }

        public static void SetSeekBar(SeekBar bar)
        {
            bar.Max = (int) player.Duration;
            bar.Progress = (int) player.CurrentPosition;
            bar.ProgressChanged += (sender, e) =>
            {
                if (player != null && e.FromUser)
                    player.SeekTo(e.Progress);
            };
        }

        public static int Duration
        {
            get
            {
                return (int) player.Duration;
            }
        }

        public static int CurrentPosition
        {
            get
            {
                return (int) player.CurrentPosition;
            }
        }

        void GetTrackSong(string filePath, out Song song)
        {
            string Title = "Unknow";
            string Artist = "Unknow";
            long AlbumArt = 0;
            long id = 0;
            string path = filePath;

            Uri musicUri = MediaStore.Audio.Media.ExternalContentUri;

            CursorLoader cursorLoader = new CursorLoader(Application.Context, musicUri, null, null, null, null);
            ICursor musicCursor = (ICursor)cursorLoader.LoadInBackground();

            if (musicCursor != null && musicCursor.MoveToFirst())
            {
                int titleID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Title);
                int artistID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Artist);
                int thisID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Id);
                int pathID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Data);
                do
                {
                    path = musicCursor.GetString(pathID);

                    if (path == filePath)
                    {
                        Artist = musicCursor.GetString(artistID);
                        Title = musicCursor.GetString(titleID);
                        AlbumArt = musicCursor.GetLong(musicCursor.GetColumnIndex(MediaStore.Audio.Albums.InterfaceConsts.AlbumId));
                        id = musicCursor.GetLong(thisID);

                        if (Title == null)
                            Title = "Unknown Title";
                        if (Artist == null)
                            Artist = "Unknow Artist";
                        break;
                    }
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }
            song = new Song(Title, Artist, null, AlbumArt, id, filePath);
        }

        async void CreateNotification(string title, string artist, long albumArt = 0, string imageURI = "")
        {
            MusicPlayer.title = title;
            Bitmap icon = null;

            if (imageURI == null)
            {
                if (albumArt != 0)
                {
                    Uri songCover = Uri.Parse("content://media/external/audio/albumart");
                    Uri iconURI = ContentUris.WithAppendedId(songCover, albumArt);

                    await Task.Run(() =>
                    {
                        try
                        {
                            icon = Picasso.With(Application.Context).Load(iconURI).Error(Resource.Drawable.MusicIcon).Placeholder(Resource.Drawable.MusicIcon).NetworkPolicy(NetworkPolicy.Offline).Resize(400, 400).CenterCrop().Get();
                        }
                        catch (Exception)
                        {
                            icon = Picasso.With(Application.Context).Load(Resource.Drawable.MusicIcon).Get();
                        }
                    });
                }
                else
                {
                    await Task.Run(() =>
                    {
                        icon = Picasso.With(Application.Context).Load(imageURI).Get();
                    });
                }
            }
            else
            {
                await Task.Run(() =>
                {
                    try
                    {
                        icon = Picasso.With(Application.Context).Load(imageURI).Error(Resource.Drawable.MusicIcon).Placeholder(Resource.Drawable.MusicIcon).NetworkPolicy(NetworkPolicy.Offline).Resize(400, 400).CenterCrop().Get();
                    }
                    catch (Exception)
                    {
                        icon = Picasso.With(Application.Context).Load(Resource.Drawable.MusicIcon).Get();
                    }
                });
            }

            Intent tmpPreviusIntent = new Intent(Application.Context, typeof(MusicPlayer));
            tmpPreviusIntent.SetAction("Previus");
            PendingIntent previusIntent = PendingIntent.GetService(Application.Context, 0, tmpPreviusIntent, PendingIntentFlags.UpdateCurrent);

            Intent tmpPauseIntent = new Intent(Application.Context, typeof(MusicPlayer));
            tmpPauseIntent.SetAction("Pause");
            PendingIntent pauseIntent = PendingIntent.GetService(Application.Context, 0, tmpPauseIntent, PendingIntentFlags.UpdateCurrent);

            Intent tmpNextIntent = new Intent(Application.Context, typeof(MusicPlayer));
            tmpNextIntent.SetAction("Next");
            PendingIntent nextIntent = PendingIntent.GetService(Application.Context, 0, tmpNextIntent, PendingIntentFlags.UpdateCurrent);

            notification = new NotificationCompat.Builder(Application.Context)
                .SetVisibility(NotificationCompat.VisibilityPublic)
                .SetSmallIcon(Resource.Drawable.MusicIcon)

                .AddAction(Resource.Drawable.ic_skip_previous_black_24dp, "Previous", previusIntent)
                .AddAction(Resource.Drawable.ic_pause_black_24dp, "Pause", pauseIntent)
                .AddAction(Resource.Drawable.ic_skip_next_black_24dp, "Next", nextIntent)

                .SetStyle(new NotificationCompat.MediaStyle()
                    .SetShowActionsInCompactView(1)
                    .SetMediaSession(mediaSession.SessionToken))
                .SetContentTitle(title)
                .SetContentText(artist)
                .SetLargeIcon(icon)
                .Build();
            StartForeground(notificationID, notification);
        }

        public void Pause()
        {
            if(player != null && isRunning)
            {
                isRunning = false;
                Intent tmpPauseIntent = new Intent(Application.Context, typeof(MusicPlayer));
                tmpPauseIntent.SetAction("Pause");
                PendingIntent pauseIntent = PendingIntent.GetService(Application.Context, 0, tmpPauseIntent, PendingIntentFlags.UpdateCurrent);

                notification.Actions[1] = new Notification.Action(Resource.Drawable.ic_play_arrow_black_24dp, "Play", pauseIntent);
                notificationManager.Notify(notificationID, notification);

                player.PlayWhenReady = false;
                StopForeground(false);

                RelativeLayout smallPlayer = MainActivity.instance.FindViewById<RelativeLayout>(Resource.Id.smallPlayer);
                smallPlayer.FindViewById<ImageButton>(Resource.Id.spPlay).SetImageResource(Resource.Drawable.ic_play_arrow_black_24dp);

                if (Player.instance != null)
                {
                    Player.instance.playerView.FindViewById<ImageButton>(Resource.Id.playButton).SetImageResource(Resource.Drawable.ic_play_arrow_black_24dp);
                }
            }
        }

        public void Resume()
        {
            if(player != null && !isRunning)
            {
                isRunning = true;
                Intent tmpPauseIntent = new Intent(Application.Context, typeof(MusicPlayer));
                tmpPauseIntent.SetAction("Pause");
                PendingIntent pauseIntent = PendingIntent.GetService(Application.Context, 0, tmpPauseIntent, PendingIntentFlags.UpdateCurrent);

                notification.Actions[1] = new Notification.Action(Resource.Drawable.ic_pause_black_24dp, "Pause", pauseIntent);

                player.PlayWhenReady = true;
                StartForeground(notificationID, notification);

                RelativeLayout smallPlayer = MainActivity.instance.FindViewById<RelativeLayout>(Resource.Id.smallPlayer);
                smallPlayer.FindViewById<ImageButton>(Resource.Id.spPlay).SetImageResource(Resource.Drawable.ic_pause_black_24dp);

                if (Player.instance != null)
                {
                    Player.instance.playerView.FindViewById<ImageButton>(Resource.Id.playButton).SetImageResource(Resource.Drawable.ic_pause_black_24dp);
                }
            }
        }

        public void Stop()
        {
            isRunning = false;
            MainActivity.instance.HideSmallPlayer();
            if (player != null)
            {
                if (isRunning)
                    player.Stop();
                player.Release();
                StopForeground(true);
            }
        }

        public void OnAudioFocusChange(AudioFocus focusChange)
        {
            switch (focusChange)
            {
                case AudioFocus.Gain:
                    if (player == null)
                        InitializeService();

                    if (!isRunning)
                        player.PlayWhenReady = true;

                    player.Volume = 1;
                    break;

                case AudioFocus.Loss:
                    Stop();
                    break;

                case AudioFocus.LossTransient:
                    Pause();
                    break;

                case AudioFocus.LossTransientCanDuck:
                    if (isRunning)
                        player.Volume = 0.2f;
                    break;

                default:
                    break;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            isRunning = false;

            if (player != null)
                player.Release();
        }

        public void OnLoadingChanged(bool p0)
        {

        }

        public void OnPlaybackParametersChanged(PlaybackParameters p0)
        {

        }

        public void OnPlayerError(ExoPlaybackException args)
        {
            Console.WriteLine("Error in playback resetting: " + args.Cause);
        }

        public void OnPlayerStateChanged(bool p0, int p1)
        {

        }

        public void OnPositionDiscontinuity()
        {

        }

        public void OnRepeatModeChanged(int p0)
        {

        }

        public void OnTimelineChanged(Timeline p0, Java.Lang.Object p1)
        {

        }

        public void OnTracksChanged(TrackGroupArray p0, TrackSelectionArray p1)
        {

        }
    }
}