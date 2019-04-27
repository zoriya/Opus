using Android.App;
using Android.Content;
using Android.Database;
using Android.Gms.Cast;
using Android.Gms.Cast.Framework.Media;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Provider;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V4.Media.Session;
using Android.Support.V7.Preferences;
using Android.Views;
using Android.Widget;
using Com.Google.Android.Exoplayer2;
using Com.Google.Android.Exoplayer2.Extractor;
using Com.Google.Android.Exoplayer2.Source;
using Com.Google.Android.Exoplayer2.Source.Hls;
using Com.Google.Android.Exoplayer2.Trackselection;
using Com.Google.Android.Exoplayer2.Upstream;
using Com.Google.Android.Exoplayer2.Util;
using Newtonsoft.Json;
using Opus.DataStructure;
using Opus.Fragments;
using Opus.Others;
using Org.Json;
using SQLite;
using Square.Picasso;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Models;
using static Android.Support.V4.Media.App.NotificationCompat;
using CursorLoader = Android.Support.V4.Content.CursorLoader;
using MediaInfo = Android.Gms.Cast.MediaInfo;
using MediaMetadata = Android.Gms.Cast.MediaMetadata;
using Uri = Android.Net.Uri;

namespace Opus.Api.Services
{
    [Service]
    public class MusicPlayer : Service, IPlayerEventListener, AudioManager.IOnAudioFocusChangeListener
    {
        public static MusicPlayer instance;
        public static bool UseCastPlayer = false;
        public static SimpleExoPlayer player;
        public static RemoteMediaClient RemotePlayer;
        public static CastCallback CastCallback;
        public static CastQueueManager CastQueueManager;
        public float volume;
        private static AudioStopper noisyReceiver;
        public static List<Song> queue = new List<Song>();
        public static List<int> WaitForIndex = new List<int>();
        public static List<Song> autoPlay = new List<Song>();
        private MediaSessionCompat mediaSession;
        private AudioManager audioManager;
        private AudioFocusRequestClass audioFocusRequest;
        public NotificationManager notificationManager;
        private bool noisyRegistered;
        public static bool isRunning = false;
        private bool generating = false;
        public static int currentID = 0;
        public static bool autoUpdateSeekBar = true;
        public static bool repeat = false;
        public static bool useAutoPlay = true;
        public static bool isLiveStream = false;
        public static bool ShouldResumePlayback;
        public static bool Initialized = false;

        private static long LastTimer = -1;
        private Notification notification;
        private const int notificationID = 1000;
        private bool volumeDuked;

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override void OnCreate()
        {
            base.OnCreate();
            instance = this;
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            if (intent == null)
            {
                RetrieveQueueFromDataBase();
                return StartCommandResult.Sticky;
            }

            string file = intent.GetStringExtra("file");

            switch (intent.Action)
            {
                case "YoutubePlay":
                    string action = intent.GetStringExtra("action");
                    string title = intent.GetStringExtra("title");
                    string artist = intent.GetStringExtra("artist");
                    string thumbnailURL = intent.GetStringExtra("thumbnailURI");
                    bool showPlayer = intent.GetBooleanExtra("showPlayer", true);
                    ParseAndPlay(action, file, title, artist, thumbnailURL, showPlayer);
                    break;

                case "Previus":
                    PlayPrevious();
                    break;

                case "Pause":
                    if(isRunning)
                        Pause();
                    else
                        Resume();
                    break;

                case "ForcePause":
                    if (isRunning)
                        Pause();
                    break;

                case "ForceResume":
                    Resume();
                    Player.errorState = false;
                    break;

                case "Next":
                    PlayNext();
                    break;

                case "RandomPlay":
                    List<string> files = intent.GetStringArrayListExtra("files").ToList();
                    bool clearQueue = intent.GetBooleanExtra("clearQueue", true);
                    RandomPlay(files, clearQueue);
                    break;

                case "RandomizeQueue":
                    RandomizeQueue();
                    break;

                case "PlayNext":
                    AddToQueue(file);
                    break;

                case "PlayLast":
                    PlayLastInQueue(file);
                    break;

                case "Stop":
                    Stop(intent.GetBooleanExtra("saveQueue", true));
                    break;

                case "SleepPause":
                    SleepPause();
                    break;

                case "SwitchQueue":
                    SwitchQueue(intent.GetIntExtra("queueSlot", -1), intent.GetBooleanExtra("showPlayer", true));
                    break;

                case "CastListener":
                    if (CastCallback == null)
                        InitializeService();
                    CastCallback.OnStatusUpdated();
                    return StartCommandResult.Sticky;

                case "StartCasting":
                    StartCasting();
                    break;
            }

            if (intent.Action != null)
                return StartCommandResult.Sticky;

            if (file != null && file != "")
                Play(file);

            return StartCommandResult.Sticky;
        }

        public async static Task<Song> GetItem(int position = -2)
        {
            if (position == -2)
                position = CurrentID();

            if (position >= queue.Count && autoPlay.Count > 0)
                return autoPlay[0];

            if (position < 0 || position >= queue.Count)
                return null;

            if (queue[position] == null && !WaitForIndex.Contains(position))
            {
                RemotePlayer.MediaQueue.GetItemAtIndex(position, true);
                WaitForIndex.Add(position);
            }

            while (queue[position] == null)
                await Task.Delay(100);

            return queue[position];
        }

        private void InitializeService()
        {
            audioManager = (AudioManager)Application.Context.GetSystemService(AudioService);
            notificationManager = (NotificationManager)Application.Context.GetSystemService(NotificationService);
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            AdaptiveTrackSelection.Factory trackSelectionFactory = new AdaptiveTrackSelection.Factory(new DefaultBandwidthMeter());
            TrackSelector trackSelector = new DefaultTrackSelector(trackSelectionFactory);
            player = ExoPlayerFactory.NewSimpleInstance(Application.Context, trackSelector);
            volume = prefManager.GetInt("volumeMultiplier", 100) / 100f;
            player.Volume = volume;
            player.AddListener(this);

            if (noisyReceiver == null)
                noisyReceiver = new AudioStopper();

            RegisterReceiver(noisyReceiver, new IntentFilter(AudioManager.ActionAudioBecomingNoisy));
            noisyRegistered = true;

            RemotePlayer = MainActivity.CastContext.SessionManager.CurrentCastSession?.RemoteMediaClient;
            if(RemotePlayer != null)
            {
                if (CastCallback == null)
                {
                    CastCallback = new CastCallback();
                    RemotePlayer.RegisterCallback(CastCallback);
                }
                if(CastQueueManager == null)
                {
                    CastQueueManager = new CastQueueManager();
                    RemotePlayer.MediaQueue.RegisterCallback(CastQueueManager);
                }
            }
            UseCastPlayer = RemotePlayer != null;
            player.PlayWhenReady = !UseCastPlayer;
        }

        public void ChangeVolume(float volume)
        {
            if(player != null)
                player.Volume = volume * (volumeDuked ? 0.2f : 1);
        }

        public async void Play(string filePath, string title = null, string artist = null, string youtubeID = null, string thumbnailURI = null, bool isLive = false, DateTimeOffset? expireDate = null)
        {
            isRunning = true;
            if (player == null)
                InitializeService();

            queue?.Clear();
            currentID = -1;
            Queue.instance?.Refresh();
            Home.instance?.RefreshQueue(false);

            Song song = null;
            if (title == null)
                song = await LocalManager.GetSong(filePath);
            else
            {
                song = new Song(title, artist, thumbnailURI, youtubeID, -1, -1, filePath, true);
            }

            song.IsLiveStream = isLive;
            isLiveStream = isLive;

            if (!UseCastPlayer)
            {
                if (mediaSession == null)
                {
                    mediaSession = new MediaSessionCompat(Application.Context, "Opus");
                    mediaSession.SetFlags(MediaSessionCompat.FlagHandlesMediaButtons | MediaSessionCompat.FlagHandlesTransportControls);
                    PlaybackStateCompat.Builder builder = new PlaybackStateCompat.Builder().SetActions(PlaybackStateCompat.ActionPlay | PlaybackStateCompat.ActionPause | PlaybackStateCompat.ActionSkipToNext | PlaybackStateCompat.ActionSkipToPrevious);
                    mediaSession.SetPlaybackState(builder.Build());
                    mediaSession.SetCallback(new HeadphonesActions());
                }

                DefaultDataSourceFactory dataSourceFactory = new DefaultDataSourceFactory(Application.Context, "Opus");
                IExtractorsFactory extractorFactory = new DefaultExtractorsFactory();
                Handler handler = new Handler();

                IMediaSource mediaSource = null;
                if (isLive)
                    mediaSource = new HlsMediaSource(Uri.Parse(filePath), dataSourceFactory, handler, null);
                else if (title == null)
                    mediaSource = new ExtractorMediaSource(Uri.FromFile(new Java.IO.File(filePath)), dataSourceFactory, extractorFactory, handler, null);
                else
                    mediaSource = new ExtractorMediaSource(Uri.Parse(filePath), dataSourceFactory, extractorFactory, handler, null);

                AudioAttributes attributes = new AudioAttributes.Builder()
                    .SetUsage(AudioUsageKind.Media)
                    .SetContentType(AudioContentType.Music)
                    .Build();

                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                   audioFocusRequest = new AudioFocusRequestClass.Builder(AudioFocus.Gain)
                        .SetAudioAttributes(attributes)
                        .SetAcceptsDelayedFocusGain(true)
                        .SetWillPauseWhenDucked(true)
                        .SetOnAudioFocusChangeListener(this)
                        .Build();
                    AudioFocusRequest audioFocus = audioManager.RequestAudioFocus(audioFocusRequest);

                    if (audioFocus != AudioFocusRequest.Granted)
                    {
                        Console.WriteLine("Can't Get Audio Focus");
                        return;
                    }
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete

                    AudioManager am = (AudioManager)MainActivity.instance.GetSystemService(AudioService);

                    AudioFocusRequest audioFocus = am.RequestAudioFocus(this, Stream.Music, AudioFocus.Gain);

                    if (audioFocus != AudioFocusRequest.Granted)
                    {
                        Console.WriteLine("Can't Get Audio Focus");
                        return;
                    }
#pragma warning restore CS0618
                }

                player.PlayWhenReady = true;
                player.Prepare(mediaSource, true, true);
                CreateNotification(song.Title, song.Artist, song.AlbumArt, song.Album);
                AddToQueue(song);
                currentID = CurrentID() + 1;
            }
            else
            {
                RemotePlayer.Load(GetMediaInfo(song), new MediaLoadOptions.Builder().SetAutoplay(true).Build());
                RemotePlayer.Play();
                queue = new List<Song> { song };
                currentID = 0;
            }

            autoPlay.Clear();

            SaveQueueSlot();
            Player.instance?.RefreshPlayer();
            Home.instance?.AddQueue();
            ParseNextSong();
            if (useAutoPlay)
                GenerateAutoPlay(false);
        }

        public async void Play(Song song, long progress = -1, bool addToQueue = true)
        {
            if (song.IsParsed != true)
            {
                await ParseSong(song, -1, true);
                return;
            }

            if (addToQueue)
            {
                queue?.Clear();
                currentID = -1;
            }
            
            isLiveStream = song.IsLiveStream;

            isRunning = true;
            if (player == null)
                InitializeService();

            if (!UseCastPlayer)
            {
                if (mediaSession == null)
                {
                    mediaSession = new MediaSessionCompat(Application.Context, "Opus");
                    mediaSession.SetFlags(MediaSessionCompat.FlagHandlesMediaButtons | MediaSessionCompat.FlagHandlesTransportControls);
                    PlaybackStateCompat.Builder builder = new PlaybackStateCompat.Builder().SetActions(PlaybackStateCompat.ActionPlay | PlaybackStateCompat.ActionPause | PlaybackStateCompat.ActionSkipToNext | PlaybackStateCompat.ActionSkipToPrevious);
                    mediaSession.SetPlaybackState(builder.Build());
                    mediaSession.SetCallback(new HeadphonesActions());
                }

                DefaultDataSourceFactory dataSourceFactory = new DefaultDataSourceFactory(Application.Context, "Opus");
                IExtractorsFactory extractorFactory = new DefaultExtractorsFactory();
                Handler handler = new Handler();

                IMediaSource mediaSource = null;
                if (song.IsLiveStream)
                    mediaSource = new HlsMediaSource(Uri.Parse(song.Path), dataSourceFactory, handler, null);
                else if (!song.IsYt)
                    mediaSource = new ExtractorMediaSource(Uri.FromFile(new Java.IO.File(song.Path)), dataSourceFactory, extractorFactory, handler, null);
                else
                    mediaSource = new ExtractorMediaSource(Uri.Parse(song.Path), dataSourceFactory, extractorFactory, handler, null);

                AudioAttributes attributes = new AudioAttributes.Builder()
                    .SetUsage(AudioUsageKind.Media)
                    .SetContentType(AudioContentType.Music)
                    .Build();

                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    audioFocusRequest = new AudioFocusRequestClass.Builder(AudioFocus.Gain)
                        .SetAudioAttributes(attributes)
                        .SetAcceptsDelayedFocusGain(true)
                        .SetWillPauseWhenDucked(true)
                        .SetOnAudioFocusChangeListener(this)
                        .Build();
                    AudioFocusRequest audioFocus = audioManager.RequestAudioFocus(audioFocusRequest);

                    if (audioFocus != AudioFocusRequest.Granted)
                    {
                        Console.WriteLine("Can't Get Audio Focus");
                        return;
                    }
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete

                    AudioManager am = (AudioManager)MainActivity.instance.GetSystemService(AudioService);

                    AudioFocusRequest audioFocus = am.RequestAudioFocus(this, Stream.Music, AudioFocus.Gain);

                    if (audioFocus != AudioFocusRequest.Granted)
                    {
                        Console.WriteLine("Can't Get Audio Focus");
                        return;
                    }
#pragma warning restore CS0618
                }

                player.PlayWhenReady = true;
                player.Prepare(mediaSource, true, true);
                CreateNotification(song.Title, song.Artist, song.AlbumArt, song.Album);

                if (progress != -1) //I'm seeking after the prepare because with some format, exoplayer's prepare reset the position
                {
                    player.SeekTo(progress);
                    MainActivity.instance?.FindViewById<ImageButton>(Resource.Id.playButton).SetImageResource(Resource.Drawable.Pause);
                }
            }
            else
            {
                RemotePlayer.Load(GetMediaInfo(song), new MediaLoadOptions.Builder().SetAutoplay(true).Build());
                RemotePlayer.Play();
            }

            isRunning = true;

            if (addToQueue)
            {
                AddToQueue(song);
                SaveQueueSlot();
                autoPlay.Clear();
                currentID = CurrentID() + 1;
                Queue.instance?.Refresh();
            }
            else
                Queue.instance?.RefreshCurrent();

            Player.instance?.RefreshPlayer();
            Home.instance?.AddQueue();
            ParseNextSong();

            if (useAutoPlay)
                GenerateAutoPlay(false);
        }

        private static async Task<Song> ParseSong(Song song, int position = -1, bool startPlaybackWhenPosible = false, bool forceParse = false)
        {
            if ((!forceParse && song.IsParsed == true) || !song.IsYt)
            {
                if (startPlaybackWhenPosible)
                    instance.Play(song, -1, position == -1);

                return song;
            }

            if (song.IsParsed == null)
            {
                while (song.IsParsed == null)
                    await Task.Delay(10);

                if (startPlaybackWhenPosible && (await GetItem()).YoutubeID != song.YoutubeID)
                    instance.Play(song, -1, position == -1);

                return song; //Song is a class, the youtube id will be updated with another method
            }

            try
            {
                YoutubeClient client = new YoutubeClient();
                var mediaStreamInfo = await client.GetVideoMediaStreamInfosAsync(song.YoutubeID);
                if (mediaStreamInfo.HlsLiveStreamUrl != null)
                {
                    song.Path = mediaStreamInfo.HlsLiveStreamUrl;
                    song.IsLiveStream = true;
                }
                else
                {
                    song.IsLiveStream = false;

                    if (mediaStreamInfo.Audio.Count > 0)
                        song.Path = mediaStreamInfo.Audio.OrderBy(s => s.Bitrate).Last().Url;
                    else if (mediaStreamInfo.Muxed.Count > 0)
                        song.Path = mediaStreamInfo.Muxed.OrderBy(x => x.Resolution).Last().Url;
                    else
                    {
                        MainActivity.instance.NotStreamable(song.Title);
                        return null;
                    }
                }
                song.IsParsed = true;

                if (position != -1)
                    Queue.instance?.NotifyItemChanged(position, Resource.Drawable.PublicIcon);

                if (startPlaybackWhenPosible && song.Album != null)
                {
                    instance.Play(song, -1, position == -1);
                    startPlaybackWhenPosible = false;
                }

                Video video = await client.GetVideoAsync(song.YoutubeID);
                song.Title = video.Title;
                song.Artist = video.Author;
                song.Album = await MainActivity.GetBestThumb(new string[] { video.Thumbnails.MaxResUrl, video.Thumbnails.StandardResUrl, video.Thumbnails.HighResUrl });
                Player.instance?.RefreshPlayer();

                if (startPlaybackWhenPosible)
                {
                    instance.Play(song, -1, position == -1);

                    if (position != -1)
                    {
                        Queue.instance?.NotifyItemChanged(position, song.Artist);
                        Home.instance?.NotifyQueueChanged(position, song.Artist);
                    }
                }

                if (!song.IsLiveStream)
                    song.ExpireDate = mediaStreamInfo.ValidUntil;

                if(position != -1)
                    UpdateQueueItemDB(song, position);
            }
            catch (System.Net.Http.HttpRequestException)
            {
                Console.WriteLine("&Parse time out");
                MainActivity.instance.Timout();
                if (MainActivity.instance != null)
                    MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress).Visibility = ViewStates.Gone;
                song.IsParsed = false;

                if (startPlaybackWhenPosible)
                    Player.instance?.Ready();
                return null;
            }
            catch(YoutubeExplode.Exceptions.VideoUnplayableException ex)
            {
                Console.WriteLine("&Parse error: " + ex.Message);
                MainActivity.instance.Unplayable(song.Title, ex.Message);
                if (MainActivity.instance != null)
                    MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress).Visibility = ViewStates.Gone;

                song.IsParsed = false;
                if (position != -1)
                    RemoveFromQueue(position); //Remove the song from the queue since it can't be played.

                if(startPlaybackWhenPosible)
                    Player.instance?.Ready();
                return null;
            }
            catch(YoutubeExplode.Exceptions.VideoUnavailableException)
            {
                MainActivity.instance.NotStreamable(song.Title);
                if (MainActivity.instance != null)
                    MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress).Visibility = ViewStates.Gone;

                song.IsParsed = false;
                if (position != -1)
                    RemoveFromQueue(position); //Remove the song from the queue since it can't be played.

                if (startPlaybackWhenPosible)
                    Player.instance?.Ready();
                return null;
            }
            return song;
        }

        private async void ParseAndPlay(string action, string videoID, string title, string artist, string thumbnailURL, bool showPlayer = true)
        {
            if (MainActivity.instance != null && action == "Play")
            {
                ProgressBar parseProgress = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress);
                parseProgress.Visibility = ViewStates.Visible;
                parseProgress.ScaleY = 6;
                Player.instance.Buffering();
            }

            if (action == "Play")
            {
                if (showPlayer)
                    MainActivity.instance.ShowPlayer();
                else
                    MainActivity.instance.ShowSmallPlayer();

                Song song = new Song(title ?? "", artist ?? "", thumbnailURL, videoID, -1, -1, null, true, false);
                queue.Clear();
                autoPlay.Clear();
                queue.Add(song);
                Queue.instance?.Refresh();
                Player.instance?.RefreshPlayer();
                currentID = 0;
                await ParseSong(song, 0, true);
            }
            else
            {
                Song song = await ParseSong(new Song(title, artist, thumbnailURL, videoID, -1, -1, null, true, false));
                if (song == null) //The song can't be played, do not add it to the queue
                    return;

                switch (action)
                {
                    case "PlayNext":
                        AddToQueue(song);
                        return;

                    case "PlayLast":
                        PlayLastInQueue(song);
                        return;
                }
            }

            if (MainActivity.instance != null)
            {
                MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress).Visibility = ViewStates.Gone;
            }
            UpdateQueueItemDB(await GetItem(), CurrentID());
        }

        public async void GenerateAutoPlay(bool switchToNext)
        {
            if (generating == true)
                return;

            generating = true;

            string youtubeID = null;
            if (MainActivity.HasInternet())
            {
                Song current = await GetItem();
                if (current.IsYt)
                    youtubeID = current.YoutubeID;
                else
                    youtubeID = "local";
            }
            else
                youtubeID = "local";

            if (youtubeID != "local" && await MainActivity.instance.WaitForYoutube())
            {
                await GenerateYtAutoplay(youtubeID);
            }
            else
            {
                GenerateLocalAutoplay();
            }

            Random random = new Random();
            autoPlay = autoPlay.OrderBy(x => random.Next()).ToList().GetRange(0, autoPlay.Count > 20 ? 20 : autoPlay.Count);
            generating = false;
            Queue.instance?.RefreshAP();
            ParseNextSong();

            if (switchToNext)
                PlayNext();
        }

        async Task GenerateYtAutoplay(string youtubeID)
        {
            try
            {
                YoutubeClient client = new YoutubeClient();
                Video video = await client.GetVideoAsync(youtubeID);

                var ytPlaylistRequest = YoutubeManager.YoutubeService.PlaylistItems.List("snippet, contentDetails");
                ytPlaylistRequest.PlaylistId = video.GetVideoMixPlaylistId();
                ytPlaylistRequest.MaxResults = 15;

                List<Song> allSongs = new List<Song>();
                var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                foreach (var item in ytPlaylist.Items)
                {
                    if (item.Snippet.Title != "[Deleted video]" && item.Snippet.Title != "Private video" && item.Snippet.Title != "Deleted video")
                    {
                        Song song = new Song(System.Net.WebUtility.HtmlDecode(item.Snippet.Title), item.Snippet.ChannelTitle, item.Snippet.Thumbnails.High.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true, false);
                        if (!queue.Exists(x => x.YoutubeID == song.YoutubeID) && !autoPlay.Exists(x => x.YoutubeID == song.YoutubeID))
                        {
                            allSongs.Add(song);
                            break;
                        }
                    }
                }

                Random r = new Random();
                allSongs = allSongs.OrderBy(x => r.Next()).ToList();
                autoPlay.AddRange(allSongs.GetRange(0, allSongs.Count > 5 ? 5 : allSongs.Count));
            }
            catch
            {
                GenerateLocalAutoplay();
            }
        }

        void GenerateLocalAutoplay()
        {
            Uri musicUri = MediaStore.Audio.Media.ExternalContentUri;

            List<Song> allSongs = new List<Song>();
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
                    string Artist = musicCursor.GetString(artistID);
                    string Title = musicCursor.GetString(titleID);
                    string Album = musicCursor.GetString(albumID);
                    long AlbumArt = musicCursor.GetLong(musicCursor.GetColumnIndex(MediaStore.Audio.Albums.InterfaceConsts.AlbumId));
                    long id = musicCursor.GetLong(thisID);
                    string path = musicCursor.GetString(pathID);

                    if (Title == null)
                        Title = "Unknown Title";
                    if (Artist == null)
                        Artist = "Unknow Artist";
                    if (Album == null)
                        Album = "Unknow Album";

                    allSongs.Add(new Song(Title, Artist, Album, null, AlbumArt, id, path));
                }
                while (musicCursor.MoveToNext());
                musicCursor.Close();
            }
            Random r = new Random();
            allSongs = allSongs.OrderBy(x => r.Next()).ToList();
            autoPlay.AddRange(allSongs.GetRange(0, allSongs.Count > 5 ? 5 : allSongs.Count));
        }

        public async void RandomPlay(List<string> filePaths, bool clearQueue)
        {
            currentID = 0;
            if (clearQueue)
                queue.Clear();

            Random r = new Random();
            filePaths = filePaths.OrderBy(x => r.Next()).ToList();
            if (clearQueue)
            {
                Play(filePaths[0]);
                filePaths.RemoveAt(0);
            }

            foreach (string filePath in filePaths)
            {
                Song song = await LocalManager.GetSong(filePath);
                queue.Add(song);
            }

            if (UseCastPlayer)
            {
                await RemotePlayer.QueueLoadAsync(queue.ConvertAll(GetQueueItem).ToArray(), 0, 0, null);
            }

            UpdateQueueDataBase();
            Home.instance?.RefreshQueue();
        }

        private void RandomizeQueue()
        {
            Random r = new Random();
            if (UseCastPlayer)
            {
                for (int i = 0; i < RemotePlayer.MediaQueue.ItemCount; i++)
                {
                    int itemID = RemotePlayer.MediaQueue.ItemIdAtIndex(i);
                    if (itemID != MediaQueueItem.InvalidItemId)
                        RemotePlayer.QueueMoveItemToNewIndex(itemID, r.Next(0, RemotePlayer.MediaQueue.ItemCount), null);
                }
            }
            else
            {
                Song current = queue[CurrentID()];
                queue.RemoveAt(CurrentID());
                queue = queue.OrderBy(x => r.Next()).ToList();

                currentID = 0;
                queue.Insert(0, current);

                UpdateQueueDataBase();
                SaveQueueSlot();
                Queue.instance?.Refresh();
                Home.instance?.RefreshQueue();
                Queue.instance?.ListView.ScrollToPosition(0);
            }
        }

        public async void AddToQueue(string filePath, string title = null, string artist = null, string youtubeID = null, string thumbnailURI = null, bool isLive = false)
        {
            Song song = null;
            if(title == null)
                song = await LocalManager.GetSong(filePath);
            else
                song = new Song(title, artist, thumbnailURI, youtubeID, -1, -1, filePath, true);

            song.IsLiveStream = isLive;
            queue.Insert(CurrentID() + 1, song);
            Home.instance?.NotifyQueueInserted(CurrentID() + 1);
            Queue.instance?.NotifyItemInserted(CurrentID() + 1);
            UpdateQueueDataBase();

            if (UseCastPlayer)
            {
                if (RemotePlayer.CurrentItem != null)
                {
                    int currentIndex = (int)RemotePlayer.MediaStatus.GetIndexById(RemotePlayer.CurrentItem.ItemId);
                    if (currentIndex + 1 < RemotePlayer.MediaStatus.QueueItemCount)
                        RemotePlayer.QueueInsertItems(new MediaQueueItem[] { GetQueueItem(song) }, RemotePlayer.MediaQueue.ItemIdAtIndex(currentIndex + 1), null);
                    else
                        RemotePlayer.QueueAppendItem(GetQueueItem(song), null);
                }
                else
                    RemotePlayer.QueueAppendItem(GetQueueItem(song), null);
            }
        }

        public void AddToQueue(Song song)
        {
            queue.Insert(CurrentID() + 1, song);
            Home.instance?.NotifyQueueInserted(CurrentID() + 1);
            Queue.instance?.NotifyItemInserted(CurrentID() + 1);
            UpdateQueueDataBase();

            if(UseCastPlayer)
            {
                if (RemotePlayer.CurrentItem != null)
                {
                    int currentIndex = (int)RemotePlayer.MediaStatus.GetIndexById(RemotePlayer.CurrentItem.ItemId);
                    if (currentIndex + 1 < RemotePlayer.MediaStatus.QueueItemCount)
                        RemotePlayer.QueueInsertItems(new MediaQueueItem[] { GetQueueItem(song) }, RemotePlayer.MediaQueue.ItemIdAtIndex(currentIndex + 1), null);
                    else
                        RemotePlayer.QueueAppendItem(GetQueueItem(song), null);
                }
                else
                    RemotePlayer.QueueAppendItem(GetQueueItem(song), null);
            }
        }

        public async void AddToQueue(IEnumerable<Song> songs)
        {
            queue.AddRange(songs);
            Home.instance?.RefreshQueue();
            Queue.instance?.Refresh();
            UpdateQueueDataBase();

            if (UseCastPlayer)
            {
                foreach (Song song in songs)
                    RemotePlayer.QueueAppendItem(GetQueueItem(await ParseSong(song)), null);
            }
        }

        public static void InsertToQueue(int position, Song song)
        {
            queue.Insert(position, song);
            Home.instance?.NotifyQueueInserted(position);
            Queue.instance?.NotifyItemInserted(position);

            if (UseCastPlayer)
                RemotePlayer.QueueInsertItems(new MediaQueueItem[] { GetQueueItem(song) }, RemotePlayer.MediaQueue.ItemIdAtIndex(position + 1), null);

            UpdateQueueDataBase();
        }

        public void InsertToQueue(int position, Song[] songs)
        {
            queue.InsertRange(position, songs);
            Home.instance?.NotifyQueueRangeInserted(position, songs.Length);
            Queue.instance?.NotifyItemRangeInserted(position, songs.Length);

            if (UseCastPlayer)
                RemotePlayer.QueueInsertItems(GetQueueItems(songs), RemotePlayer.MediaQueue.ItemIdAtIndex(position + 1), null);

            UpdateQueueDataBase();
        }

        public static void RemoveFromQueue(int position)
        {
            if (CurrentID() > position)
                currentID--;
            else if (CurrentID() == position)
            {
                if (position > 0)
                    currentID--;
                else if (queue.Count > position + 1)
                    currentID++;
                else
                    currentID = -1;

                Player.instance?.RefreshPlayer();
                Player.instance?.Ready();
                Queue.instance?.RefreshCurrent();
            }

            SaveQueueSlot();
            queue.RemoveAt(position);

            if (queue.Count == 0)
            {
                MainActivity.instance.HideSmallPlayer();
                if (Home.instance != null && Home.adapterItems?.Count > 0 && Home.adapterItems[0]?.SectionTitle == "Queue")
                {
                    Home.instance?.adapter?.NotifyItemRemoved(0);
                    Home.adapterItems?.RemoveAt(0);
                }
                Queue.instance?.NotifyItemRemoved(position);
            }
            else
            {
                Home.instance?.NotifyQueueRemoved(position);
                Queue.instance?.NotifyItemRemoved(position);
                if (Queue.instance != null && queue.Count - position < 3)
                    Queue.instance?.RefreshAP();
            }

            if (UseCastPlayer)
                RemotePlayer.QueueRemoveItem(RemotePlayer.MediaQueue.ItemIdAtIndex(position), null);

            UpdateQueueDataBase();
        }

        public async void PlayLastInQueue(string filePath)
        {
            Song song = await LocalManager.GetSong(filePath);

            if (UseCastPlayer)
                RemotePlayer.QueueAppendItem(GetQueueItem(song), null);
            else
            {
                queue.Add(song);
                UpdateQueueItemDB(song, queue.Count - 1);
            }
            Home.instance?.NotifyQueueInserted(queue.Count - 1);
            Queue.instance?.NotifyItemInserted(queue.Count - 1);
        }

        public void PlayLastInQueue(Song song)
        {
            if (UseCastPlayer)
                RemotePlayer.QueueAppendItem(GetQueueItem(song), null);
            else
            {
                queue.Add(song);
                UpdateQueueItemDB(song, queue.Count - 1);
            }
            Home.instance?.NotifyQueueInserted(queue.Count - 1);
            Queue.instance?.NotifyItemInserted(queue.Count - 1);
        }

        public void PlayLastInQueue(string filePath, string title, string artist, string youtubeID, string thumbnailURI, bool isLive = false)
        {
            Song song = new Song(title, artist, thumbnailURI, youtubeID, -1, -1, filePath, true)
            {
                IsLiveStream = isLive
            };


            if (UseCastPlayer)
                RemotePlayer.QueueAppendItem(GetQueueItem(song), null);
            else
            {
                queue.Add(song);
                UpdateQueueItemDB(song, queue.Count - 1);
            }
            Home.instance?.NotifyQueueInserted(queue.Count - 1);
            Queue.instance?.NotifyItemInserted(queue.Count - 1);
        }

        public void PlayPrevious()
        {
            Player.instance.playNext = false;
            Player.instance.Buffering();
            if(CurrentPosition > Duration * 0.2f || CurrentID() - 1 < 0)
            {
                if (player != null)
                    player.SeekTo(0);
                else
                    SwitchQueue(CurrentID(), false, false);
                return;
            }

            if (UseCastPlayer)
                RemotePlayer.QueuePrev(null);
            else
                SwitchQueue(CurrentID() - 1);
        }

        public static void Repeat(bool? forceRepeatState = null)
        {
            repeat = forceRepeatState ?? !repeat;

            if (UseCastPlayer)
                RemotePlayer.QueueSetRepeatMode(repeat ? 1 : 0, null);

            if (repeat)
            {
                Queue.instance?.NotifyItemChanged(-1, "Repeat");
                Player.instance?.Repeat(true);
                useAutoPlay = false;
                Queue.instance?.NotifyItemChanged(queue.Count, "UseAutoplay");
            }
            else
            {
                Queue.instance?.NotifyItemChanged(-1, "Repeat");
                Player.instance?.Repeat(false);
                useAutoPlay = true;
                Queue.instance?.NotifyItemChanged(queue.Count, "UseAutoplay");
            }
        }

        public void PlayNext()
        {
            Player.instance.playNext = true;
            if (CurrentID() + 1 >= queue.Count || CurrentID() == -1)
            {
                if (useAutoPlay)
                {
                    Player.instance.Buffering();
                    if (autoPlay.Count > 0)
                    {
                        queue.Add(autoPlay[0]);
                        UpdateQueueItemDB(queue.Last(), queue.Count - 1);
                        autoPlay.RemoveAt(0);

                        if (autoPlay.Count < 1)
                            GenerateAutoPlay(false);
                    }
                    else
                    {
                        GenerateAutoPlay(true);
                        return;
                    }
                }
                else if (repeat)
                {
                    Player.instance.Buffering();
                    SwitchQueue(0);
                    return;
                }
                else
                {
                    Pause();
                    return;
                }
            }

            Player.instance.Buffering();
            if (UseCastPlayer)
                RemotePlayer.QueueNext(null);
            else
                SwitchQueue(CurrentID() + 1);
        }

        public async void SwitchQueue(int position, bool showPlayer = false, bool StartFromOldPosition = false)
        {
            Song song = await GetItem(position);

            if(showPlayer)
                MainActivity.instance.ShowPlayer();

            if (UseCastPlayer)
            {
                currentID = position;
                Console.WriteLine("&Switching to item at " + position + " with itemID: " + RemotePlayer.MediaQueue.ItemIdAtIndex(position));
                RemotePlayer.QueueJumpToItem(RemotePlayer.MediaQueue.ItemIdAtIndex(position), null);
            }
            else if (song.IsParsed != true || song.ExpireDate < DateTime.UtcNow.AddMinutes(-20))
            {
                Player.instance?.Buffering();
                if (MainActivity.instance != null && showPlayer)
                {
                    ProgressBar parseProgress = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress);
                    parseProgress.Visibility = ViewStates.Visible;
                    parseProgress.ScaleY = 6;
                }
                await ParseSong(song, position, !UseCastPlayer, true);

                if (song != null) //Check if the parse has succeed, the song is set to null if there is an error
                {
                    currentID = position;
                    Queue.instance?.RefreshCurrent();
                    Player.instance?.RefreshPlayer();
                }
                else
                    Player.instance?.Ready(); //Remove player's loading bar since we'll not load this song

                if (MainActivity.instance != null && showPlayer)
                {
                    ProgressBar parseProgress = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress);
                    parseProgress.Visibility = ViewStates.Gone;
                }
            }
            else
            {
                currentID = position;
                Play(song, StartFromOldPosition ? LastTimer : -1, false);
            }

            Queue.instance?.RefreshAP();
        }

        public static int CurrentID()
        {
            if (queue.Count <= currentID)
                currentID = -1;
            return currentID;
        }

        public static void SeekTo(long positionMS)
        {
            if (!UseCastPlayer)
                player.SeekTo(positionMS);
            else
                RemotePlayer.Seek(positionMS);
        }

        void AddSongToDataBase(Song item)
        {
            Task.Run(() =>
            {
                SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "Queue.sqlite"));
                db.CreateTable<Song>();

                db.InsertOrReplace(item);
            });
        }

        public static void UpdateQueueDataBase()
        {
            Task.Run(() =>
            {
                SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "Queue.sqlite"));
                db.CreateTable<Song>();

                if (db.Table<Song>().Count() > queue.Count)
                {
                    db.DropTable<Song>();
                    db.CreateTable<Song>();
                }

                for (int i = 0; i < queue.Count; i++)
                {
                    Song item = queue[i];
                    item.Index = i;
                    db.InsertOrReplace(item);
                }
            });
        }

        static void UpdateQueueItemDB(Song item, int position)
        {
            Task.Run(() =>
            {
                SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "Queue.sqlite"));
                db.CreateTable<Song>();

                item.Index = position;
                db.InsertOrReplace(item);
            });
        }

        public static void SaveQueueSlot()
        {
            ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            ISharedPreferencesEditor editor = pref.Edit();
            editor.PutInt("currentID", currentID == -1 ? 0 : currentID);
            editor.Apply();
        }

        public static void RetrieveQueueFromDataBase()
        {
            Task.Run(() =>
            {
                SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "Queue.sqlite"));
                db.CreateTable<Song>();

                queue = db.Table<Song>().ToList().ConvertAll(RemoveParseValues);
                if (queue != null && queue.Count > 0)
                {
                    currentID = RetrieveQueueSlot();
                    LastTimer = RetrieveTimer();

                    MainActivity.instance.RunOnUiThread(() => 
                    {
                        Home.instance?.AddQueue();
                        MainActivity.instance.ShowSmallPlayer();
                    });
                }
                else
                {
                    MainActivity.instance.RunOnUiThread(() => 
                    {
                        MainActivity.instance.HideSmallPlayer();
                        MainActivity.instance.SkipStop = false;
                    });
                }
            });
        }

        static Song RemoveParseValues(Song song)
        {
            if (song.IsYt && song.IsParsed != false)
            {
                if (song.ExpireDate != null && song.ExpireDate.Value.Subtract(DateTimeOffset.UtcNow) > TimeSpan.Zero)
                {
                    return song;
                }
                else
                {
                    song.IsParsed = false;
                    song.Path = song.YoutubeID;
                    song.ExpireDate = null;
                }
            }
            return song;
        }

        public static int RetrieveQueueSlot()
        {
            ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            int queueSlot = pref.GetInt("currentID", 0);
            Console.WriteLine("&Retrieved queue slot: " + queueSlot);
            return queueSlot == -1 ? 0 : queueSlot;
        }

        public static long RetrieveTimer()
        {
            ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            return pref.GetLong("playerProgress", 0);
        }

        public static void SaveTimer(long currentProgress)
        {
            ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            ISharedPreferencesEditor editor = pref.Edit();
            editor.PutLong("playerProgress", currentProgress);
            editor.Apply();
        }

        public static async void ParseNextSong()
        {
            if (CurrentID() == -1 || UseCastPlayer)
                return;

            if (queue.Count == CurrentID() + 1)
            {
                if (useAutoPlay && autoPlay.Count > 0)
                {
                    Song song = autoPlay[0];
                    if (song.IsParsed != false || !song.IsYt)
                        return;
                    await ParseSong(song);
                }
            }
            else
            {
                Song song = queue[CurrentID() + 1];
                if (song.IsParsed != false || !song.IsYt)
                    return;

                await ParseSong(song, currentID + 1);
            }
        }

        public static long Duration
        {
            get
            {
                if(!UseCastPlayer)
                    return player == null ? 1 : player.Duration;
                else
                    return RemotePlayer == null ? 1 : RemotePlayer.StreamDuration;
            }
        }

        public static long CurrentPosition
        {
            get
            {
                if(!UseCastPlayer)
                    return player == null ? 0 : player.CurrentPosition;
                else
                    return RemotePlayer == null ? 1 : RemotePlayer.ApproximateStreamPosition;
            }
        }

        

        async void CreateNotification(string title, string artist, long albumArt = 0, string imageURI = "")
        {
            Bitmap icon = null;
            if(albumArt == -1)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        icon = Picasso.With(Application.Context).Load(imageURI).Error(Resource.Drawable.noAlbum).Transform(new RemoveBlackBorder(true)).Get();
                    }
                    catch (Exception)
                    {
                        icon = Picasso.With(Application.Context).Load(Resource.Drawable.noAlbum).Get();
                    }
                });
            }
            else
            {
                Uri songCover = Uri.Parse("content://media/external/audio/albumart");
                Uri iconURI = ContentUris.WithAppendedId(songCover, albumArt);

                await Task.Run(() =>
                {
                    try
                    {
                        icon = Picasso.With(Application.Context).Load(iconURI).Error(Resource.Drawable.noAlbum).NetworkPolicy(NetworkPolicy.Offline).Resize(400, 400).CenterCrop().Get();
                    }
                    catch (Exception)
                    {
                        icon = Picasso.With(Application.Context).Load(Resource.Drawable.noAlbum).Get();
                    }
                });
            }

            Intent tmpPreviousIntent = new Intent(Application.Context, typeof(MusicPlayer));
            tmpPreviousIntent.SetAction("Previus");
            PendingIntent previousIntent = PendingIntent.GetService(Application.Context, 0, tmpPreviousIntent, PendingIntentFlags.UpdateCurrent);

            Intent tmpPauseIntent = new Intent(Application.Context, typeof(MusicPlayer));
            tmpPauseIntent.SetAction("Pause");
            PendingIntent pauseIntent = PendingIntent.GetService(Application.Context, 0, tmpPauseIntent, PendingIntentFlags.UpdateCurrent);

            Intent tmpNextIntent = new Intent(Application.Context, typeof(MusicPlayer));
            tmpNextIntent.SetAction("Next");
            PendingIntent nextIntent = PendingIntent.GetService(Application.Context, 0, tmpNextIntent, PendingIntentFlags.UpdateCurrent);

            Intent tmpDefaultIntent = new Intent(Application.Context, typeof(MainActivity));
            tmpDefaultIntent.SetAction("Player");
            PendingIntent defaultIntent = PendingIntent.GetActivity(Application.Context, 0, tmpDefaultIntent, PendingIntentFlags.UpdateCurrent);

            Intent tmpDeleteIntent = new Intent(Application.Context, typeof(MusicPlayer));
            tmpDeleteIntent.SetAction("Stop");
            PendingIntent deleteIntent = PendingIntent.GetService(Application.Context, 0, tmpDeleteIntent, PendingIntentFlags.UpdateCurrent);

            notification = new NotificationCompat.Builder(Application.Context, "Opus.Channel")
                .SetVisibility(NotificationCompat.VisibilityPublic)
                .SetSmallIcon(Resource.Drawable.NotificationIcon)

                .AddAction(Resource.Drawable.SkipPrevious, "Previous", previousIntent)
                .AddAction(Resource.Drawable.Pause, "Pause", pauseIntent)
                .AddAction(Resource.Drawable.SkipNext, "Next", nextIntent)

                .SetStyle(new MediaStyle()
                    .SetShowActionsInCompactView(1)
                    .SetShowCancelButton(true)
                    .SetMediaSession(mediaSession.SessionToken))
                .SetDeleteIntent(deleteIntent)
                .SetContentTitle(title)
                .SetContentText(artist)
                .SetLargeIcon(icon)
                .SetContentIntent(defaultIntent)
                .Build();
            ContextCompat.StartForegroundService(Application.Context, new Intent(Application.Context, typeof(MusicPlayer)));
            StartForeground(notificationID, notification);
        }

        public void Pause()
        {
            ShouldResumePlayback = false;

            if (!UseCastPlayer && player != null && isRunning)
            {
                isRunning = false;

                Intent tmpPauseIntent = new Intent(Application.Context, typeof(MusicPlayer));
                tmpPauseIntent.SetAction("Pause");
                PendingIntent pauseIntent = PendingIntent.GetService(Application.Context, 0, tmpPauseIntent, PendingIntentFlags.UpdateCurrent);
                notification.Actions[1] = new Notification.Action(Resource.Drawable.Play, "Play", pauseIntent);
                notificationManager.Notify(notificationID, notification);

                player.PlayWhenReady = false;
                StopForeground(false);

                if (!ShouldResumePlayback && noisyRegistered)
                {
                    UnregisterReceiver(noisyReceiver);
                    noisyRegistered = false;
                }
            }
            else if(UseCastPlayer && RemotePlayer != null && isRunning)
            {
                isRunning = false;
                RemotePlayer.Pause();
            }

            SaveTimer(CurrentPosition);
            FrameLayout smallPlayer = MainActivity.instance.FindViewById<FrameLayout>(Resource.Id.smallPlayer);
            smallPlayer?.FindViewById<ImageButton>(Resource.Id.spPlay)?.SetImageResource(Resource.Drawable.Play);

            MainActivity.instance.FindViewById<ImageButton>(Resource.Id.playButton)?.SetImageResource(Resource.Drawable.Play);
            Queue.instance?.RefreshCurrent();
        }

        public void Resume()
        {
            if(!UseCastPlayer && player != null && notification != null && !isRunning)
            {
                ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
                player.Volume = prefManager.GetInt("volumeMultiplier", 100) / 100f;
                isRunning = true;
                Intent tmpPauseIntent = new Intent(Application.Context, typeof(MusicPlayer));
                tmpPauseIntent.SetAction("Pause");
                PendingIntent pauseIntent = PendingIntent.GetService(Application.Context, 0, tmpPauseIntent, PendingIntentFlags.UpdateCurrent);

                notification.Actions[1] = new Notification.Action(Resource.Drawable.Pause, "Pause", pauseIntent);

                player.PlayWhenReady = true;
                StartForeground(notificationID, notification);

                if (noisyReceiver == null)
                    noisyReceiver = new AudioStopper();

                RegisterReceiver(noisyReceiver, new IntentFilter(AudioManager.ActionAudioBecomingNoisy));
                noisyRegistered = true;

                FrameLayout smallPlayer = MainActivity.instance.FindViewById<FrameLayout>(Resource.Id.smallPlayer);
                smallPlayer?.FindViewById<ImageButton>(Resource.Id.spPlay)?.SetImageResource(Resource.Drawable.Pause);

                if (Player.instance != null)
                {
                    MainActivity.instance?.FindViewById<ImageButton>(Resource.Id.playButton)?.SetImageResource(Resource.Drawable.Pause);
                    Player.instance.handler?.PostDelayed(Player.instance.UpdateSeekBar, 1000);
                }

                Queue.instance?.RefreshCurrent();

                AudioAttributes attributes = new AudioAttributes.Builder()
                    .SetUsage(AudioUsageKind.Media)
                    .SetContentType(AudioContentType.Music)
                    .Build();

                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    AudioFocusRequestClass focusRequest = new AudioFocusRequestClass.Builder(AudioFocus.Gain)
                        .SetAudioAttributes(attributes)
                        .SetAcceptsDelayedFocusGain(true)
                        .SetWillPauseWhenDucked(true)
                        .SetOnAudioFocusChangeListener(this)
                        .Build();
                    AudioFocusRequest audioFocus = audioManager.RequestAudioFocus(focusRequest);

                    if (audioFocus != AudioFocusRequest.Granted)
                    {
                        Console.WriteLine("Can't Get Audio Focus");
                        return;
                    }
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete

                    AudioManager am = (AudioManager)MainActivity.instance.GetSystemService(AudioService);

                    AudioFocusRequest audioFocus = am.RequestAudioFocus(this, Stream.Music, AudioFocus.Gain);

                    if (audioFocus != AudioFocusRequest.Granted)
                    {
                        Console.WriteLine("Can't Get Audio Focus");
                        return;
                    }
#pragma warning restore CS0618
                }
            }
            else if(UseCastPlayer && RemotePlayer != null && !isRunning) //Maybe check that the session is initialised.
            {
                isRunning = true;
                RemotePlayer.Play();

                FrameLayout smallPlayer = MainActivity.instance.FindViewById<FrameLayout>(Resource.Id.smallPlayer);
                smallPlayer?.FindViewById<ImageButton>(Resource.Id.spPlay)?.SetImageResource(Resource.Drawable.Pause);

                if (Player.instance != null)
                {
                    MainActivity.instance?.FindViewById<ImageButton>(Resource.Id.playButton)?.SetImageResource(Resource.Drawable.Pause);
                    Player.instance.handler?.PostDelayed(Player.instance.UpdateSeekBar, 1000);
                }

                Queue.instance?.RefreshCurrent();
            }
            else
            {
                LastTimer = RetrieveTimer();
                SwitchQueue(CurrentID(), false, true);
            }
        }

        private static MediaInfo GetMediaInfo(Song song)
        {
            MediaMetadata metadata = new MediaMetadata(MediaMetadata.MediaTypeMusicTrack);
            metadata.PutString(MediaMetadata.KeyTitle, song.Title);
            metadata.PutString(MediaMetadata.KeyArtist, song.Artist);
            metadata.AddImage(new Android.Gms.Common.Images.WebImage(Uri.Parse(song.Album), 1000, 1000));

            MediaInfo mediaInfo = new MediaInfo.Builder(song.Path)
                .SetStreamType(MediaInfo.StreamTypeBuffered)
                .SetContentType(MimeTypes.AudioMp4)
                .SetMetadata(metadata)
                .SetCustomData(new JSONObject(JsonConvert.SerializeObject(song)))
                .Build();

            return mediaInfo;
        }

        private static MediaQueueItem GetQueueItem(Song song)
        {
            return new MediaQueueItem.Builder(GetMediaInfo(song)).Build();
        }

        private static MediaQueueItem[] GetQueueItems(Song[] songs)
        {
            MediaQueueItem[] items = new MediaQueueItem[songs.Length];
            for (int i = 0; i < songs.Length; i++)
            {
                items[i] = GetQueueItem(songs[i]);
            }
            return items;
        }

        private async void StartCasting()
        {
            if (UseCastPlayer && (RemotePlayer.MediaQueue == null || RemotePlayer.MediaQueue.ItemCount == 0))
            {
                bool showToast = false;
                if (queue.Count(x => x.IsParsed == false) > 1)
                    showToast = true;

                if(showToast)
                    Toast.MakeText(MainActivity.instance, Resource.String.cast_queue_push, ToastLength.Long).Show();

                for (int i = 0; i < queue.Count; i++)
                {
                    if (queue[i].IsYt && queue[i].IsParsed != true)
                        queue[i] = await ParseSong(queue[i], i);
                }

                if(showToast)
                    Toast.MakeText(MainActivity.instance, Resource.String.cast_queue_pushed, ToastLength.Short).Show();
                RemotePlayer.QueueLoad(queue.ConvertAll(GetQueueItem).ToArray(), currentID, 0, CurrentPosition, null);

                if (noisyRegistered)
                    UnregisterReceiver(noisyReceiver);

                if (player != null)
                {
                    player.Stop();
                    player.Release();
                    player = null;
                }

                if (isRunning)
                    RemotePlayer.Play();

                Initialized = true;
                GetQueueFromCast(true);
            }
        }

        public async static void GetQueueFromCast(bool forceShowPlayer = false)
        {
            if (UseCastPlayer && Initialized)
            {
                if(RemotePlayer.CurrentItem != null)
                    currentID = RemotePlayer.MediaQueue.IndexOfItemWithId(RemotePlayer.CurrentItem.ItemId);

                bool showPlayer = queue.Count == 0;
                if (forceShowPlayer)
                    showPlayer = true;

                queue.Clear();
                for (int i = 0; i < RemotePlayer.MediaQueue.ItemCount; i++)
                    queue.Add((Song)RemotePlayer.MediaQueue.GetItemAtIndex(i, true));

                Queue.instance?.Refresh();
                Console.WriteLine("&Waiting for fetch - queue count: " + queue.Count);

                if (queue.Count > 0)
                {
                    if (currentID != -1)
                    {
                        while (currentID >= queue.Count || queue[currentID] == null || Player.instance == null)
                            await Task.Delay(1000);
                    }

                    Console.WriteLine("&Fetched");

                    Intent intent = new Intent(MainActivity.instance, typeof(MusicPlayer));
                    intent.SetAction("CastListener");
                    MainActivity.instance.StartService(intent);

                    Home.instance?.AddQueue();
                    Home.instance?.RefreshQueue(false);
                    Queue.instance?.Refresh();

                    if (showPlayer)
                        MainActivity.instance.ShowSmallPlayer();
                }
                else
                    MainActivity.instance.HideSmallPlayer();
            }
        }

        public void Stop(bool SaveQueue)
        {
            if (noisyRegistered)
                UnregisterReceiver(noisyReceiver);

            if (SaveQueue)
            {
                Console.WriteLine("&Saving the queue");
                ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
                ISharedPreferencesEditor editor = pref.Edit();
                editor.PutInt("currentID", currentID);
                editor.Apply();

                Console.WriteLine("&CurrentID: " + currentID);

                if (player != null && CurrentPosition != 0)
                    SaveTimer(CurrentPosition);
            }
            else
            {
                ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
                ISharedPreferencesEditor editor = pref.Edit();
                editor.PutInt("currentID", 0);
                editor.Apply();

                queue = new List<Song>();
                UpdateQueueDataBase();
                currentID = -1;

                MainActivity.instance?.HideSmallPlayer();
                if (Home.adapterItems?.Count > 0 && Home.adapterItems[0]?.SectionTitle == "Queue")
                {
                    Home.instance?.adapter?.NotifyItemRemoved(0);
                    Home.adapterItems?.RemoveAt(0);
                }
            }

            if(audioFocusRequest != null)
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    audioManager.AbandonAudioFocusRequest(audioFocusRequest);
                else
#pragma warning disable CS0618 // Type or member is obsolete
                    audioManager.AbandonAudioFocus(this);
#pragma warning restore CS0618 // Type or member is obsolete
            }

            MainActivity.instance.SkipStop = false;
            noisyReceiver = null;
            noisyRegistered = false;
            isRunning = false;
            if (player != null)
            {
                player.Release();
                player.Stop();
                player = null;
            }

            if (!UseCastPlayer)
            {
                instance = null;
                StopSelf();
            }
            else if (!SaveQueue)
            {
                RemotePlayer.Stop();
                StopSelf();
            }

            Player.instance?.Ready(); //Refresh play/pause state
        }

        private void SleepPause()
        {
            Stop(true);
        }

        public void OnAudioFocusChange(AudioFocus focusChange)
        {
            Console.WriteLine("&AudioFocus Changed: " + focusChange.ToString());
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            switch (focusChange)
            {
                case AudioFocus.Gain:
                    if (ShouldResumePlayback)
                    {
                        if (player == null)
                            InitializeService();

                        Resume();
                    }

                    if (player != null)
                        player.Volume = prefManager.GetInt("volumeMultiplier", 100) / 100f;
                    volumeDuked = false;
                    break;

                case AudioFocus.Loss:
                    Pause();
                    break;

                case AudioFocus.LossTransient:
                    Pause();
                    ShouldResumePlayback = true;
                    break;

                case AudioFocus.LossTransientCanDuck:
                    volumeDuked = true;
                    player.Volume = prefManager.GetInt("volumeMultiplier", 100) / 500f;
                    ShouldResumePlayback = true;
                    break;

                default:
                    break;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            Stop(true);
            instance = null;
        }

        public void OnLoadingChanged(bool p0) { }

        public void OnPlaybackParametersChanged(PlaybackParameters p0) { }

        public void OnPlayerError(ExoPlaybackException args)
        {
            Console.WriteLine("&Type: " + args.Type + " : " +  args.Cause + " : " + args.Data);
            Player.instance?.Error();


            Intent tmpErrorIntent = new Intent(Application.Context, typeof(MusicPlayer));
            tmpErrorIntent.SetAction("ForceResume");
            PendingIntent errorIntent = PendingIntent.GetService(Application.Context, 0, tmpErrorIntent, PendingIntentFlags.UpdateCurrent);

            notification.Actions[1] = new Notification.Action(Resource.Drawable.Error, "Error", errorIntent);
            notificationManager.Notify(notificationID, notification);
        }

        public void OnPlayerStateChanged(bool playWhenReady, int state)
        {
            if (state == Com.Google.Android.Exoplayer2.Player.StateEnded)
            {
                PlayNext();
            }
            if(state == Com.Google.Android.Exoplayer2.Player.StateBuffering)
            {
                Player.instance?.Buffering();
            }
            if(state == Com.Google.Android.Exoplayer2.Player.StateReady)
            {
                Player.instance?.Ready();
            }
        }


        public void OnPositionDiscontinuity() { }

        public void OnRepeatModeChanged(int p0) { }

        public void OnTracksChanged(TrackGroupArray p0, TrackSelectionArray p1) { }

        public void OnPositionDiscontinuity(int p0) { }

        public void OnSeekProcessed() { }

        public void OnShuffleModeEnabledChanged(bool p0) { }

        public void OnTimelineChanged(Timeline p0, Java.Lang.Object p1, int p2) { }
    }
}
 