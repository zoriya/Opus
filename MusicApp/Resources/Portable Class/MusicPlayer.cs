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
using FileNotFoundException = System.IO.FileNotFoundException;
using Android.Widget;

namespace MusicApp.Resources.Portable_Class
{
    [Service]
    public class MusicPlayer : Service, AudioManager.IOnAudioFocusChangeListener
    {
        public static MediaPlayer player;
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
                    if (player.IsPlaying)
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
                    AddToQueue(file);
                    break;

                case "PlayLast":
                    PlayLastInQueue(file);
                    break;

                case "QueueSwitch":
                    SwitchQueue(file);
                    break;
                case "Stop":
                    if (player.IsPlaying)
                        Stop();
                    break;
                case "YTPlay":
                    string[] song = intent.GetStringArrayListExtra("song").ToArray();
                    Console.WriteLine(song.Length);
                    Console.WriteLine(song[0]);
                    Console.WriteLine(song[1]);
                    Console.WriteLine(song[2]);

                    PlayFromYT(file, song[0], song[1], song[2]);
                    break;
            }

            if (intent.Action != null)
                return StartCommandResult.Sticky;

            if (file != null)
                Play(file);

            return StartCommandResult.Sticky;
        }

        private void InitializeService()
        {
            audioManager = (AudioManager)Application.Context.GetSystemService(AudioService);
            notificationManager = (NotificationManager)Application.Context.GetSystemService(NotificationService);

            player = new MediaPlayer();
            InitializePlayer();
        }

        private void InitializePlayer()
        {
            player.SetAudioStreamType(Stream.Music);
            player.SetWakeMode(Application.Context, WakeLockFlags.Partial);

            player.Prepared += (sender, args) => player.Start();
            player.Completion += (sender, args) => PlayNext();
            player.Error += (sender, args) =>
            {
                Console.WriteLine("Error in playback resetting: " + args.What);
                Stop();
            };
        }

        public async void Play(string filePath)
        {
            isRunning = true;
            if (player == null)
                InitializeService();

            if (mediaSession != null)
            {
                player.Reset();
                InitializePlayer();
                await player.SetDataSourceAsync(Application.Context, Android.Net.Uri.Parse(filePath));
                player.PrepareAsync();
                GetTrackSong(filePath, out Song song);
                CreateNotification(song.GetName(), song.GetArtist(), song.GetAlbumArt());
                queue.Clear();
                AddToQueue(filePath);
                return;
            }
            try
            {
                mediaSession = new MediaSessionCompat(Application.Context, "MusicApp");
                mediaSession.SetFlags(MediaSessionCompat.FlagHandlesMediaButtons | MediaSessionCompat.FlagHandlesTransportControls);
                PlaybackStateCompat.Builder builder = new PlaybackStateCompat.Builder().SetActions(PlaybackStateCompat.ActionPlay | PlaybackStateCompat.ActionPause);
                mediaSession.SetPlaybackState(builder.Build());

                await player.SetDataSourceAsync(Application.Context, Android.Net.Uri.Parse(filePath));
                var audioFocus = audioManager.RequestAudioFocus(this, Stream.Music, AudioFocus.Gain);
                if (audioFocus != AudioFocusRequest.Granted)
                {
                    Console.WriteLine("Can't Get Audio Focus");
                    return;
                }
                player.PrepareAsync();
                GetTrackSong(filePath, out Song song);
                CreateNotification(song.GetName(), song.GetArtist(), song.GetAlbumArt());
                queue.Clear();
                AddToQueue(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
        }

        public async void PlayFromYT(string url, string title, string artist, string imageURL)
        {
            isRunning = true;
            if (player == null)
                InitializeService();

            if (mediaSession != null)
            {
                player.Reset();
                InitializePlayer();
                await player.SetDataSourceAsync(Application.Context, Uri.Parse(url));
                player.PrepareAsync();
                CreateNotification(title, artist, 0, imageURL);

                queue.Clear();
                Song item = new Song(title, artist, imageURL, -1, 1, url);
                queue.Add(item);
                return;
            }
            try
            {
                mediaSession = new MediaSessionCompat(Application.Context, "MusicApp");
                mediaSession.SetFlags(MediaSessionCompat.FlagHandlesMediaButtons | MediaSessionCompat.FlagHandlesTransportControls);
                PlaybackStateCompat.Builder builder = new PlaybackStateCompat.Builder().SetActions(PlaybackStateCompat.ActionPlay | PlaybackStateCompat.ActionPause);
                mediaSession.SetPlaybackState(builder.Build());

                await player.SetDataSourceAsync(Application.Context, Uri.Parse(url));
                var audioFocus = audioManager.RequestAudioFocus(this, Stream.Music, AudioFocus.Gain);
                if (audioFocus != AudioFocusRequest.Granted)
                {
                    Console.WriteLine("Can't Get Audio Focus");
                    return;
                }
                player.PrepareAsync();
                CreateNotification(title, artist, 0, imageURL);

                queue.Clear();
                Song item = new Song(title, artist, imageURL, -1, 1, url);
                queue.Add(item);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
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

        public void AddToQueue(string filePath)
        {
            GetTrackSong(filePath, out Song song);
            Console.WriteLine("AddToQueue path: " + song.GetPath() + " Before get track song: " + filePath);
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

        async void SwitchQueue(string filePath)
        {
            isRunning = true;

            if(player != null)
                player.Reset();
            InitializePlayer();
            await player.SetDataSourceAsync(Application.Context, Uri.Parse(filePath));
            player.PrepareAsync();
            GetTrackSong(filePath, out Song song);
            CreateNotification(song.GetName(), song.GetArtist(), song.GetAlbumArt());

            if (Player.instance != null)
                Player.instance.RefreshPlayer();

            RelativeLayout smallPlayer = MainActivity.instance.FindViewById<RelativeLayout>(Resource.Id.smallPlayer);
            smallPlayer.FindViewById<TextView>(Resource.Id.spTitle).Text = song.GetName();
            smallPlayer.FindViewById<TextView>(Resource.Id.spArtist).Text = song.GetArtist();
            ImageView art = smallPlayer.FindViewById<ImageView>(Resource.Id.spArt);

            var songCover = Uri.Parse("content://media/external/audio/albumart");
            var songAlbumArtUri = ContentUris.WithAppendedId(songCover, song.GetAlbumArt());

            Picasso.With(Application.Context).Load(songAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Into(art);
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
            bar.Max = player.Duration;
            bar.Progress = player.CurrentPosition;
            bar.ProgressChanged += (sender, e) =>
            {
                if (player != null && e.FromUser)
                    player.SeekTo(e.Progress);
            };
        }

        public static int CurrentPosition
        {
            get
            {
                return player.CurrentPosition;
            }
        }

        void GetTrackSong(string filePath, out Song song)
        {
            string Title = "Unknow";
            string Artist = "Unknow";
            string Album = "Unknow";
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
                int albumID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Album);
                int thisID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Id);
                int pathID = musicCursor.GetColumnIndex(MediaStore.Audio.Media.InterfaceConsts.Data);
                do
                {
                    path = musicCursor.GetString(pathID);

                    if (path == filePath)
                    {
                        Artist = musicCursor.GetString(artistID);
                        Title = musicCursor.GetString(titleID);
                        Album = musicCursor.GetString(albumID);
                        AlbumArt = musicCursor.GetLong(musicCursor.GetColumnIndex(MediaStore.Audio.Albums.InterfaceConsts.AlbumId));
                        id = musicCursor.GetLong(thisID);

                        if (Title == null)
                            Title = "Unknown Title";
                        if (Artist == null)
                            Artist = "Unknow Artist";
                        if (Album == null)
                            Album = "Unknow Album";

                        break;
                    }
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }
            song = new Song(Title, Artist, Album, AlbumArt, id, filePath);
        }

        async void CreateNotification(string title, string artist, long albumArt = 0, string imageURI = "")
        {
            MusicPlayer.title = title;
            Bitmap icon = null;

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
            if(player != null && player.IsPlaying)
            {
                Intent tmpPauseIntent = new Intent(Application.Context, typeof(MusicPlayer));
                tmpPauseIntent.SetAction("Pause");
                PendingIntent pauseIntent = PendingIntent.GetService(Application.Context, 0, tmpPauseIntent, PendingIntentFlags.UpdateCurrent);

                notification.Actions[1] = new Notification.Action(Resource.Drawable.ic_play_arrow_black_24dp, "Play", pauseIntent);
                notificationManager.Notify(notificationID, notification);

                player.Pause();
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
            if(player != null && !player.IsPlaying)
            {
                Intent tmpPauseIntent = new Intent(Application.Context, typeof(MusicPlayer));
                tmpPauseIntent.SetAction("Pause");
                PendingIntent pauseIntent = PendingIntent.GetService(Application.Context, 0, tmpPauseIntent, PendingIntentFlags.UpdateCurrent);

                notification.Actions[1] = new Notification.Action(Resource.Drawable.ic_pause_black_24dp, "Pause", pauseIntent);

                player.Start();
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
            if(player != null)
            {
                if (player.IsPlaying)
                    player.Stop();
                player.Reset();
                StopForeground(true);
            }
        }

        public void OnAudioFocusChange(AudioFocus focusChange)
        {
            switch (focusChange)
            {
                case AudioFocus.Gain:
                    if (player == null)
                        InitializePlayer();

                    if (!player.IsPlaying)
                        player.Start();

                    player.SetVolume(1, 1);
                    break;

                case AudioFocus.Loss:
                    Stop();
                    break;

                case AudioFocus.LossTransient:
                    Pause();
                    break;

                case AudioFocus.LossTransientCanDuck:
                    if (player.IsPlaying)
                        player.SetVolume(.2f, .2f);
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
    }
}