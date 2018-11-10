using Android.App;
using Android.Content;
using Android.Database;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Provider;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V4.Media.Session;
using Android.Support.V7.Preferences;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Com.Google.Android.Exoplayer2;
using Com.Google.Android.Exoplayer2.Extractor;
using Com.Google.Android.Exoplayer2.Source;
using Com.Google.Android.Exoplayer2.Source.Hls;
using Com.Google.Android.Exoplayer2.Trackselection;
using Com.Google.Android.Exoplayer2.Upstream;
using MusicApp.Resources.values;
using SQLite;
using Square.Picasso;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Models;
using YoutubeExplode.Models.MediaStreams;
using static Android.Support.V4.Media.App.NotificationCompat;
using Uri = Android.Net.Uri;

namespace MusicApp.Resources.Portable_Class
{
    [Service]
    public class MusicPlayer : Service, IPlayerEventListener, AudioManager.IOnAudioFocusChangeListener
    {
        public static MusicPlayer instance;
        public static SimpleExoPlayer player;
        public float volume;
        private static AudioStopper noisyReceiver;
        public static List<Song> queue = new List<Song>();
        public MediaSessionCompat mediaSession;
        public AudioManager audioManager;
        public NotificationManager notificationManager;
        private bool noisyRegistered;
        public static bool isRunning = false;
        public static string title;
        private static bool parsing = false;
        private bool generating = false;
        public static int currentID = 0;
        public static bool autoUpdateSeekBar = true;
        public static bool repeat = false;
        public static bool useAutoPlay = false;
        public static bool userStopped = false;
        public static bool ShouldResumePlayback;

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
                    bool addToQueue = intent.GetBooleanExtra("addToQueue", true);
                    bool showPlayer = intent.GetBooleanExtra("showPlayer", true);
                    ParseAndPlay(action, file, title, artist, thumbnailURL, addToQueue, showPlayer);
                    break;

                case "Previus":
                    PlayPrevious();
                    break;

                case "Pause":
                    if(isRunning)
                        Pause(true);
                    else
                        Resume();
                    break;

                case "ForcePause":
                    if (isRunning)
                        Pause(true);
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
                    Stop();
                    break;

                case "SleepPause":
                    SleepPause();
                    break;

                case "SwitchQueue":
                    SwitchQueue(queue[intent.GetIntExtra("queueSlot", -1)], intent.GetBooleanExtra("showPlayer", true));
                    break;
            }

            if (intent.Action != null)
                return StartCommandResult.Sticky;

            if (file != null && file != "")
                Play(file);

            return StartCommandResult.Sticky;
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
            player.PlayWhenReady = true;
            player.AddListener(this);

            if (noisyReceiver == null)
                noisyReceiver = new AudioStopper();

            RegisterReceiver(noisyReceiver, new IntentFilter(AudioManager.ActionAudioBecomingNoisy));
            noisyRegistered = true;
        }

        public void ChangeVolume(float volume)
        {
            if(player != null)
                player.Volume = volume * (volumeDuked ? 0.2f : 1);
        }

        public void Play(string filePath, string title = null, string artist = null, string youtubeID = null, string thumbnailURI = null, bool addToQueue = true, bool isLive = false)
        {
            isRunning = true;
            if (player == null)
                InitializeService();

            if(mediaSession == null)
            {
                mediaSession = new MediaSessionCompat(Application.Context, "MusicApp");
                mediaSession.SetFlags(MediaSessionCompat.FlagHandlesMediaButtons | MediaSessionCompat.FlagHandlesTransportControls);
                PlaybackStateCompat.Builder builder = new PlaybackStateCompat.Builder().SetActions(PlaybackStateCompat.ActionPlay | PlaybackStateCompat.ActionPause | PlaybackStateCompat.ActionSkipToNext | PlaybackStateCompat.ActionSkipToPrevious);
                mediaSession.SetPlaybackState(builder.Build());
                mediaSession.SetCallback(new HeadphonesActions());
            }

            DefaultDataSourceFactory dataSourceFactory = new DefaultDataSourceFactory(Application.Context, "MusicApp");
            IExtractorsFactory extractorFactory = new DefaultExtractorsFactory();
            Handler handler = new Handler();

            IMediaSource mediaSource = null;
            if (isLive)
                mediaSource = new HlsMediaSource(Uri.Parse(filePath), dataSourceFactory, handler, null);
            else
                mediaSource = new ExtractorMediaSource(Uri.Parse(filePath), dataSourceFactory, extractorFactory, handler, null);
            
            AudioAttributes attributes = new AudioAttributes.Builder()
                .SetUsage(AudioUsageKind.Media)
                .SetContentType(AudioContentType.Music)
                .Build();

            if(Build.VERSION.SdkInt >= BuildVersionCodes.O)
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

            player.PlayWhenReady = true;
            player.Prepare(mediaSource, true, true);

            Song song = null;
            if(title == null)
                song = Browse.GetSong(filePath);
            else
            {
                song = new Song(title, artist, thumbnailURI, youtubeID, -1, -1, filePath, true);
            }

            isRunning = true;
            player.PlayWhenReady = true;

            CreateNotification(song.Title, song.Artist, song.AlbumArt, song.Album);

            if (addToQueue)
            {
                AddToQueue(song);

                UpdateQueueSlots();
                currentID = CurrentID() + 1;
            }
            else
            {
                currentID = song.queueSlot;
            }

            SaveQueueSlot();
            Player.instance?.RefreshPlayer();
            Home.instance?.AddQueue();
            ParseNextSong();
        }

        public void Play(Song song, bool addToQueue = true, long progress = -1)
        {
            if (!song.isParsed)
            {
                ParseAndPlay("Play", song.youtubeID, song.Title, song.Artist, song.Album, addToQueue);
                return;
            }

            isRunning = true;
            if (player == null)
                InitializeService();

            if (mediaSession == null)
            {
                mediaSession = new MediaSessionCompat(Application.Context, "MusicApp");
                mediaSession.SetFlags(MediaSessionCompat.FlagHandlesMediaButtons | MediaSessionCompat.FlagHandlesTransportControls);
                PlaybackStateCompat.Builder builder = new PlaybackStateCompat.Builder().SetActions(PlaybackStateCompat.ActionPlay | PlaybackStateCompat.ActionPause | PlaybackStateCompat.ActionSkipToNext | PlaybackStateCompat.ActionSkipToPrevious);
                mediaSession.SetPlaybackState(builder.Build());
                mediaSession.SetCallback(new HeadphonesActions());
            }

            DefaultDataSourceFactory dataSourceFactory = new DefaultDataSourceFactory(Application.Context, "MusicApp");
            IExtractorsFactory extractorFactory = new DefaultExtractorsFactory();
            Handler handler = new Handler();
            IMediaSource mediaSource = new ExtractorMediaSource(Uri.Parse(song.Path), dataSourceFactory, extractorFactory, handler, null);
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

            player.PlayWhenReady = true;
            player.Prepare(mediaSource, true, true);
            CreateNotification(song.Title, song.Artist, song.AlbumArt, song.Album);

            isRunning = true;
            player.PlayWhenReady = true;

            if (addToQueue)
            {
                AddToQueue(song);

                UpdateQueueSlots();
                currentID = CurrentID() + 1;
            }
            else
            {
                currentID = song.queueSlot;
            }

            if (progress != -1)
            {
                player.SeekTo(progress);
                MainActivity.instance?.FindViewById<ImageButton>(Resource.Id.playButton).SetImageResource(Resource.Drawable.Pause);
            }

            SaveQueueSlot();
            Player.instance?.RefreshPlayer();
            Home.instance?.AddQueue();
            ParseNextSong();
        }

        public static void UpdateQueueSlots()
        {
            for (int i = 0; i < queue.Count; i++)
            {
                queue[i].queueSlot = i;
            }
            UpdateQueueDataBase();
        }

        private async void ParseAndPlay(string action, string videoID, string title, string artist, string thumbnailURL, bool addToQueue = true, bool showPlayer = true)
        {
            if (!parsing)
            {
                parsing = true;

                if (MainActivity.instance != null && action == "Play")
                {
                    ProgressBar parseProgress = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress);
                    parseProgress.Visibility = ViewStates.Visible;
                    parseProgress.ScaleY = 6;
                    Player.instance.Buffering();
                }

                try
                {
                    YoutubeClient client = new YoutubeClient();
                    var mediaStreamInfo = await client.GetVideoMediaStreamInfosAsync(videoID);
                    AudioStreamInfo streamInfo = mediaStreamInfo.Audio.OrderBy(s => s.Bitrate).Last();
                    bool isLive = false;
                    string streamURL = streamInfo.Url;     
                    if(mediaStreamInfo.HlsLiveStreamUrl != null)
                    {
                        streamURL = mediaStreamInfo.HlsLiveStreamUrl;
                        isLive = true;
                    }


                    if (title == null)
                    {
                        Video video = await client.GetVideoAsync(videoID);
                        title = video.Title;
                        artist = video.Author;
                        thumbnailURL = video.Thumbnails.HighResUrl;
                    }

                    switch (action)
                    {
                        case "Play":
                            Play(streamURL, title, artist, videoID, thumbnailURL, addToQueue, isLive);
                            break;

                        case "PlayNext":
                            AddToQueue(streamURL, title, artist, videoID, thumbnailURL, isLive);
                            parsing = false;
                            return;

                        case "PlayLast":
                            PlayLastInQueue(streamURL, title, artist, videoID, thumbnailURL, isLive);
                            parsing = false;
                            return;
                    }

                    if (!isLive)
                    {
                        DateTimeOffset? expireDate = streamInfo.GetUrlExpiryDate();
                        queue[CurrentID()].expireDate = expireDate;
                    }
                    else
                        queue[CurrentID()].IsLiveStream = true;

                    Video info = await client.GetVideoAsync(videoID);
                    thumbnailURL = info.Thumbnails.HighResUrl;
                    if (artist == null || artist == "")
                        artist = info.Author;

                    queue[CurrentID()].Album = thumbnailURL;
                    queue[CurrentID()].Artist = artist;
                }
                catch (YoutubeExplode.Exceptions.ParseException)
                {
                    MainActivity.instance.YoutubeEndPointChanged();
                    parsing = false;
                    if (MainActivity.instance != null)
                        MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress).Visibility = ViewStates.Gone;
                    return;
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    MainActivity.instance.Timout();
                    parsing = false;
                    if (MainActivity.instance != null)
                        MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress).Visibility = ViewStates.Gone;
                    return;
                }
                catch
                {
                    MainActivity.instance.Unknow();
                    parsing = false;
                    if (MainActivity.instance != null)
                        MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress).Visibility = ViewStates.Gone;
                    return;
                }

                UpdateQueueItemDB(queue[CurrentID()]);
                Player.instance?.RefreshPlayer();

                if (MainActivity.instance != null)
                {
                    MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress).Visibility = ViewStates.Gone;
                    MainActivity.instance.ShowSmallPlayer();
                    MainActivity.instance.ShowPlayer();
                }
                parsing = false;
            }
        }

        /*async*/ void GenerateNext(int number)
        {
            if (generating == true)
                return;

            generating = true;

            //string youtubeID = null;
            //if (MainActivity.HasInternet())
            //{
            //    int i = 1;
            //    while (youtubeID == null)
            //    {
            //        if (queue.Count >= i)
            //        {
            //            youtubeID = queue[queue.Count - i].youtubeID;
            //            i++;
            //        }
            //        else
            //            youtubeID = "local";
            //    }
            //}
            //else
            //    youtubeID = "local";

            //if (youtubeID != "local" && !await MainActivity.instance.WaitForYoutube())
            //{
            //        YoutubeClient client = new YoutubeClient();
            //        Video video = await client.GetVideoAsync(youtubeID);

            //        var ytPlaylistRequest = YoutubeEngine.youtubeService.PlaylistItems.List("snippet, contentDetails");
            //        ytPlaylistRequest.PlaylistId = video.GetVideoMixPlaylistId();
            //        ytPlaylistRequest.MaxResults = number + 2;

            //        var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

            //        foreach (var item in ytPlaylist.Items)
            //        {
            //            if (item.Snippet.Title != "[Deleted video]" && item.Snippet.Title != "Private video" && item.Snippet.Title != "Deleted video" && item.ContentDetails.VideoId != MusicPlayer.queue[MusicPlayer.CurrentID()].youtubeID)
            //            {
            //                Song song = new Song(item.Snippet.Title, "", item.Snippet.Thumbnails.Default__.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true, false);
            //                if(!queue.Exists(x => x.youtubeID == song.youtubeID))
            //                {
            //                    PlayLastInQueue(song);
            //                    break;
            //                }
            //            }
            //        }
            //    ParseNextSong();
            //}
            //else
            //{
                Uri musicUri = MediaStore.Audio.Media.ExternalContentUri;

                List<Song> allSongs = new List<Song>();
                Android.Content.CursorLoader cursorLoader = new Android.Content.CursorLoader(Application.Context, musicUri, null, null, null, null);
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
                List<Song> songList = allSongs.OrderBy(x => r.Next()).ToList();
                for (int i = 0; i < (number > songList.Count ? songList.Count : number); i++)
                    PlayLastInQueue(songList[i]);
            //}

            Queue.instance?.Refresh();
            generating = false;
        }

        public async void RandomPlay(List<string> filePath, bool clearQueue)
        {
            currentID = 0;
            if (clearQueue)
                queue.Clear();

            Random r = new Random();
            filePath = filePath.OrderBy(x => r.Next()).ToList();
            if (clearQueue)
                Play(filePath[0]);

            while (instance == null)
                await Task.Delay(10);

            for (int i = clearQueue ? 1 : 0; i < filePath.Count; i++)
            {
                Song song = Browse.GetSong(filePath[i]);
                song.queueSlot = queue.Count;
                queue.Add(song);
                await Task.Delay(10);
            }

            UpdateQueueDataBase();
            Home.instance?.RefreshQueue();
        }

        private void RandomizeQueue()
        {
            Random r = new Random();
            Song current = queue[CurrentID()];
            queue.RemoveAt(CurrentID());
            queue = queue.OrderBy(x => r.Next()).ToList();

            current.queueSlot = 0;
            currentID = 0;
            queue.Insert(0, current);
            UpdateQueueSlots();

            SaveQueueSlot();
            Player.instance?.UpdateNext();
            Queue.instance?.Refresh();
            Home.instance?.RefreshQueue();
            Queue.instance?.ListView.ScrollToPosition(0);
        }

        public void AddToQueue(string filePath, string title = null, string artist = null, string youtubeID = null, string thumbnailURI = null, bool isLive = false)
        {
            Song song = null;
            if(title == null)
                song = Browse.GetSong(filePath);
            else
                song = new Song(title, artist, thumbnailURI, youtubeID, -1, -1, filePath, true);

            if (song.queueSlot == -1)
                song.queueSlot = CurrentID() + 1;
            song.IsLiveStream = isLive;
            queue.Insert(song.queueSlot, song);
            UpdateQueueSlots();
            Home.instance?.RefreshQueue();
        }

        public void AddToQueue(Song song)
        {
            if (song.queueSlot == -1)
                song.queueSlot = CurrentID() + 1;
            queue.Insert(song.queueSlot, song);
            UpdateQueueSlots();
            Home.instance?.RefreshQueue();
        }

        public void PlayLastInQueue(string filePath)
        {
            Song song = Browse.GetSong(filePath);
            song.queueSlot = queue.Count;

            queue.Add(song);
            UpdateQueueItemDB(song);

            Home.instance?.RefreshQueue();
        }

        public void PlayLastInQueue(Song song)
        {
            song.queueSlot = queue.Count;
            queue.Add(song);
            UpdateQueueItemDB(song);
            Home.instance?.RefreshQueue();
        }

        public void PlayLastInQueue(string filePath, string title, string artist, string youtubeID, string thumbnailURI, bool isLive = false)
        {
            Song song = new Song(title, artist, thumbnailURI, youtubeID, -1, -1, filePath, true)
            {
                queueSlot = queue.Count,
                IsLiveStream = isLive
            };

            queue.Add(song);
            UpdateQueueItemDB(song);
            Home.instance?.RefreshQueue();
        }

        public void PlayPrevious()
        {
            if (CurrentID() - 1 < 0)
            {
                Play(queue[CurrentID()], false, 0);
                return;
            }

            Song previous = queue[CurrentID() - 1];
            SwitchQueue(previous);
        }

        public void PlayNext()
        {
            if (CurrentID() + 1 > queue.Count - 1 || CurrentID() == -1)
            {
                if (repeat)
                {
                    Song first = queue[0];
                    SwitchQueue(first);
                    return;
                }
                else
                {
                    Pause(true);
                    return;
                }
            }

            Song next = queue[CurrentID() + 1];
            SwitchQueue(next);

            if (useAutoPlay && CurrentID() + 3 > queue.Count)
            {
                GenerateNext(1);
            }
        }

        public async void SwitchQueue(Song song, bool showPlayer = false)
        {
            if (!song.isParsed)
            {
                Player.instance?.Buffering();
                if (MainActivity.instance != null && showPlayer)
                {
                    ProgressBar parseProgress = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress);
                    parseProgress.Visibility = ViewStates.Visible;
                    parseProgress.ScaleY = 6;
                }
                try
                {
                    YoutubeClient client = new YoutubeClient();
                    MediaStreamInfoSet mediaStreamInfo = await client.GetVideoMediaStreamInfosAsync(song.youtubeID);
                    AudioStreamInfo streamInfo = mediaStreamInfo.Audio.Where(x => x.Container == Container.M4A).OrderBy(s => s.Bitrate).Last();
                    song.Path = streamInfo.Url;
                    song.isParsed = true;
                    if (Queue.instance != null)
                    {
                        int item = queue.IndexOf(song);
                        int firstItem = ((LinearLayoutManager)Queue.instance.ListView.GetLayoutManager()).FindFirstVisibleItemPosition();
                        int lastItem = ((LinearLayoutManager)Queue.instance.ListView.GetLayoutManager()).FindLastVisibleItemPosition();
                        if (firstItem < item && item < lastItem)
                        {
                            ImageView youtubeIcon = Queue.instance.ListView.GetChildAt(item - firstItem).FindViewById<ImageView>(Resource.Id.youtubeIcon);
                            youtubeIcon.SetImageResource(Resource.Drawable.youtubeIcon);
                            youtubeIcon.ClearColorFilter();
                        }
                    }

                    DateTimeOffset? expireDate = streamInfo.GetUrlExpiryDate();
                    song.expireDate = expireDate;

                    Video info = await client.GetVideoAsync(song.youtubeID);
                    song.Album = info.Thumbnails.HighResUrl;
                    song.Artist = info.Author;
                    UpdateQueueItemDB(song);
                }
                catch (YoutubeExplode.Exceptions.ParseException)
                {
                    MainActivity.instance.YoutubeEndPointChanged();
                    return;
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    MainActivity.instance.Timout();
                    return;
                }
                catch
                {
                    MainActivity.instance.Unknow();
                    return;
                }

                if (MainActivity.instance != null && showPlayer)
                {
                    ProgressBar parseProgress = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress);
                    parseProgress.Visibility = ViewStates.Gone;
                }
            }

            Play(song, false, (currentID == song.queueSlot ? LastTimer : 0));

            MainActivity.instance.ShowSmallPlayer();
            if (showPlayer)
            {
                MainActivity.instance.ShowPlayer();
            }

            Player.instance?.RefreshPlayer();
            Queue.instance?.RefreshCurrent();
        }

        public static int CurrentID()
        {
            if (queue.Count <= currentID)
                currentID = -1;
            return currentID;
        }

        public static void SetSeekBar(SeekBar bar)
        {
            bar.Max = (int) player.Duration;
            bar.Progress = (int) player.CurrentPosition;
            bar.ProgressChanged += (sender, e) =>
            {
                int Progress = e.Progress;

                if (player != null && player.Duration - Progress <= 1500 && player.Duration - Progress > 0)
                    ParseNextSong();
            };
            bar.StartTrackingTouch += (sender, e) =>
            {
                autoUpdateSeekBar = false;
            };
            bar.StopTrackingTouch += (sender, e) =>
            {
                autoUpdateSeekBar = true;
                if(!queue[CurrentID()].IsLiveStream)
                    player.SeekTo(e.SeekBar.Progress);
            };
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

                foreach (Song item in queue)
                {
                    db.InsertOrReplace(item);
                }
            });
        }

        void UpdateQueueItemDB(Song item)
        {
            Task.Run(() =>
            {
                SQLiteConnection db = new SQLiteConnection(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "Queue.sqlite"));
                db.CreateTable<Song>();

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

                    MainActivity.instance?.RunOnUiThread(() => {
                        Home.instance?.AddQueue();
                        MainActivity.instance?.ShowSmallPlayer();
                    });
                }
                else
                {
                    MainActivity.instance?.RunOnUiThread(() => {
                        MainActivity.instance?.HideSmallPlayer();
                    });
                }
            });
        }

        static Song RemoveParseValues(Song song)
        {
            if (song.IsYt && song.isParsed)
            {
                if (song.expireDate != null && song.expireDate.Value.Subtract(DateTimeOffset.UtcNow) > TimeSpan.Zero)
                {
                    return song;
                }
                else
                {
                    song.isParsed = false;
                    song.Path = song.youtubeID;
                    song.expireDate = null;
                }
            }
            return song;
        }

        public static int RetrieveQueueSlot()
        {
            ISharedPreferences pref = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            int queueSlot = pref.GetInt("currentID", 0);
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
            if (CurrentID() == -1)
                return;

            if (CurrentID() + 1 > queue.Count - 1)
            {
                if(useAutoPlay)
                    instance.GenerateNext(1);
                return;
            }

            Song song = queue[CurrentID() + 1];
            if (song.isParsed)
                return;

            if (!song.isParsed && !parsing)
            {
                parsing = true;
                try
                {
                    YoutubeClient client = new YoutubeClient();
                    MediaStreamInfoSet mediaStreamInfo = await client.GetVideoMediaStreamInfosAsync(song.youtubeID);
                    AudioStreamInfo streamInfo = mediaStreamInfo.Audio.Where(x => x.Container == Container.M4A).OrderBy(s => s.Bitrate).Last();
                    song.isParsed = true;
                    bool isLive = false;
                    string streamURL = streamInfo.Url;
                    if (mediaStreamInfo.HlsLiveStreamUrl != null)
                    {
                        streamURL = mediaStreamInfo.HlsLiveStreamUrl;
                        isLive = true;
                    }
                    song.Path = streamURL;

                    Video info = await client.GetVideoAsync(song.youtubeID);
                    song.Album = info.Thumbnails.HighResUrl;
                    song.Artist = info.Author;

                    if(!isLive)
                    {
                        DateTimeOffset? expireDate = streamInfo.GetUrlExpiryDate();
                        song.expireDate = expireDate;
                    }

                    instance.UpdateQueueItemDB(song);
                    parsing = false;
                    if (Queue.instance != null)
                    {
                        int item = queue.IndexOf(song);
                        int firstItem = ((LinearLayoutManager)Queue.instance.ListView.GetLayoutManager()).FindFirstVisibleItemPosition();
                        int lastItem = ((LinearLayoutManager)Queue.instance.ListView.GetLayoutManager()).FindLastVisibleItemPosition();
                        if (firstItem < item && item < lastItem)
                        {
                            ImageView youtubeIcon = Queue.instance.ListView.GetChildAt(item - firstItem).FindViewById<ImageView>(Resource.Id.youtubeIcon);
                            youtubeIcon.SetImageResource(Resource.Drawable.youtubeIcon);
                            youtubeIcon.ClearColorFilter();
                        }
                    }
                }
                catch (YoutubeExplode.Exceptions.ParseException)
                {
                    MainActivity.instance.YoutubeEndPointChanged();
                    parsing = false;
                    return;
                }
                catch (System.Net.Http.HttpRequestException)
                {
                    MainActivity.instance.Timout();
                    parsing = false;
                    return;
                }
                catch
                {
                    MainActivity.instance.Unknow();
                    parsing = false;
                    return;
                }
            }
        }

        public static long Duration
        {
            get
            {
                return player == null ? 1 : player.Duration;
            }
        }

        public static long CurrentPosition
        {
            get
            {
                return player == null ? 0 : player.CurrentPosition;
            }
        }

        async void CreateNotification(string title, string artist, long albumArt = 0, string imageURI = "")
        {
            MusicPlayer.title = title;
            Bitmap icon = null;

            if(albumArt == -1)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        icon = Picasso.With(Application.Context).Load(imageURI).Error(Resource.Drawable.MusicIcon).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Get();
                    }
                    catch (Exception)
                    {
                        icon = Picasso.With(Application.Context).Load(Resource.Drawable.MusicIcon).Get();
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
                        icon = Picasso.With(Application.Context).Load(iconURI).Error(Resource.Drawable.MusicIcon).Placeholder(Resource.Drawable.MusicIcon).NetworkPolicy(NetworkPolicy.Offline).Resize(400, 400).CenterCrop().Get();
                    }
                    catch (Exception)
                    {
                        icon = Picasso.With(Application.Context).Load(Resource.Drawable.MusicIcon).Get();
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

            notification = new NotificationCompat.Builder(Application.Context, "MusicApp.Channel")
                .SetVisibility(NotificationCompat.VisibilityPublic)
                .SetSmallIcon(Resource.Drawable.MusicIcon)

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

        public void Pause(bool userRequested)
        {
            if (userRequested)
                ShouldResumePlayback = false;

            if (player != null && isRunning)
            {
                SaveTimer(CurrentPosition);
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

                FrameLayout smallPlayer = MainActivity.instance.FindViewById<FrameLayout>(Resource.Id.smallPlayer);
                smallPlayer?.FindViewById<ImageButton>(Resource.Id.spPlay)?.SetImageResource(Resource.Drawable.Play);

                MainActivity.instance.FindViewById<ImageButton>(Resource.Id.playButton)?.SetImageResource(Resource.Drawable.Play);
                Queue.instance?.RefreshCurrent();
            }
        }

        public void Resume()
        {
            if(player != null && !isRunning)
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
            else
            {
                Play(queue[CurrentID()], false, RetrieveTimer());
            }
        }

        public void Stop()
        {
            if (noisyRegistered)
                UnregisterReceiver(noisyReceiver);

            if(player != null && CurrentPosition != 0)
                SaveTimer(CurrentPosition);

            noisyRegistered = false;
            isRunning = false;
            title = null;
            parsing = false;
            currentID = -1;
            userStopped = false;
            MainActivity.instance?.HideSmallPlayer();
            if (player != null)
            {
                if (isRunning)
                    player.Stop();
                player.Release();
                player = null;
                StopForeground(true);
            }
            StopSelf();
        }

        private void SleepPause()
        {
            Stop();
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
                    Pause(false);
                    ShouldResumePlayback = false;
                    break;

                case AudioFocus.LossTransient:
                    Pause(false);
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
            Stop();
            instance = null;
        }

        public void OnLoadingChanged(bool p0) { }

        public void OnPlaybackParametersChanged(PlaybackParameters p0) { }

        public void OnPlayerError(ExoPlaybackException args)
        {
            Console.WriteLine("&Type: " + args.Type);
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
 