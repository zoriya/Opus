using Android.App;
using Android.Content;
using Android.Database;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Provider;
using Android.Support.Design.Widget;
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
using Com.Google.Android.Exoplayer2.Trackselection;
using Com.Google.Android.Exoplayer2.Upstream;
using Google.Apis.YouTube.v3;
using MusicApp.Resources.values;
using Org.Adw.Library.Widgets.Discreteseekbar;
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
        public static List<Song> queue = new List<Song>();
        public MediaSessionCompat mediaSession;
        public AudioManager audioManager;
        public NotificationManager notificationManager;
        public static bool isRunning = false;
        public static string title;
        private static bool parsing = false;
        private bool generating = false;
        public static int currentID = -1;
        public static bool repeat = false;
        public static bool useAutoPlay = true;

        private Notification notification;
        private const int notificationID = 1000;
        private static long progress;

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
                StopSelf();
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
                    ParseAndPlay(action, file, title, artist, thumbnailURL);
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
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            DefaultBandwidthMeter bandwithMeter = new DefaultBandwidthMeter();
            AdaptiveTrackSelection.Factory trackSelectionFactory = new AdaptiveTrackSelection.Factory(bandwithMeter);
            TrackSelector trackSelector = new DefaultTrackSelector(trackSelectionFactory);
            player = ExoPlayerFactory.NewSimpleInstance(Application.Context, trackSelector);
            player.PlayWhenReady = true;
            player.Volume = prefManager.GetInt("volumeMultiplier", 100) / 100f;
            player.AddListener(this);
        }

        public void ChangeVolume(float volume)
        {
            if(player != null)
                player.Volume = volume;
        }

        public void Play(string filePath, string title = null, string artist = null, string youtubeID = null, string thumbnailURI = null, bool addToQueue = true)
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
            IMediaSource mediaSource = new ExtractorMediaSource(Uri.Parse(filePath), dataSourceFactory, extractorFactory, handler, null);
            AudioAttributes attributes = new AudioAttributes.Builder()
                .SetUsage(AudioUsageKind.Media)
                .SetContentType(AudioContentType.Music)
                .Build();

            if(Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                AudioFocusRequestClass focusRequest = new AudioFocusRequestClass.Builder(AudioFocus.Gain)
                    .SetAudioAttributes(attributes)
                    .SetAcceptsDelayedFocusGain(true)
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
                GetTrackSong(filePath, out song);
            else
            {
                song = new Song(title, artist, thumbnailURI, youtubeID, -1, -1, filePath, true);
            }

            isRunning = true;
            player.PlayWhenReady = true;

            CreateNotification(song.GetName(), song.GetArtist(), song.GetAlbumArt(), song.GetAlbum());

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

            Player.instance?.RefreshPlayer();
            ParseNextSong();
        }

        public void Play(Song song, bool addToQueue = true, long progress = -1)
        {
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
            IMediaSource mediaSource = new ExtractorMediaSource(Uri.Parse(song.GetPath()), dataSourceFactory, extractorFactory, handler, null);
            AudioAttributes attributes = new AudioAttributes.Builder()
                .SetUsage(AudioUsageKind.Media)
                .SetContentType(AudioContentType.Music)
                .Build();

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                AudioFocusRequestClass focusRequest = new AudioFocusRequestClass.Builder(AudioFocus.Gain)
                    .SetAudioAttributes(attributes)
                    .SetAcceptsDelayedFocusGain(true)
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
            CreateNotification(song.GetName(), song.GetArtist(), song.GetAlbumArt(), song.GetAlbum());

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
                Player.instance.FindViewById<ImageButton>(Resource.Id.playButton).SetImageResource(Resource.Drawable.ic_pause_black_24dp);
            }

            Player.instance?.RefreshPlayer();
            ParseNextSong();

            CoordinatorLayout smallPlayer = MainActivity.instance.FindViewById<CoordinatorLayout>(Resource.Id.smallPlayer);
            smallPlayer.FindViewById<TextView>(Resource.Id.spTitle).Text = song.GetName();
            smallPlayer.FindViewById<TextView>(Resource.Id.spArtist).Text = song.GetArtist();
            smallPlayer.FindViewById<ImageView>(Resource.Id.spPlay).SetImageResource(Resource.Drawable.ic_pause_black_24dp);
            ImageView art = smallPlayer.FindViewById<ImageView>(Resource.Id.spArt);

            if (!song.IsYt)
            {
                var songCover = Uri.Parse("content://media/external/audio/albumart");
                var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, song.GetAlbumArt());

                Picasso.With(Application.Context).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(art);
            }
            else
            {
                Picasso.With(Application.Context).Load(song.GetAlbum()).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(art);
            }
        }

        public void UpdateQueueSlots()
        {
            for (int i = 0; i < queue.Count; i++)
            {
                queue[i].queueSlot = i;
                queue[i].SetName(queue[i].GetName() + " " + i);
            }
        }

        private async void ParseAndPlay(string action, string videoID, string title, string artist, string thumbnailURL)
        {
            if (!parsing)
            {
                parsing = true;

                if (MainActivity.instance != null && action == "Play")
                {
                    ProgressBar parseProgress = MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress);
                    parseProgress.Visibility = ViewStates.Visible;
                    parseProgress.ScaleY = 6;
                }

                YoutubeClient client = new YoutubeClient();
                var mediaStreamInfo = await client.GetVideoMediaStreamInfosAsync(videoID);
                AudioStreamInfo streamInfo = mediaStreamInfo.Audio.OrderBy(s => s.Bitrate).Last();

                if(title == null)
                {
                    Video video = await client.GetVideoAsync(videoID);
                    title = video.Title;
                    artist = video.Author;
                    thumbnailURL = video.Thumbnails.HighResUrl;
                }

                switch (action)
                {
                    case "Play":
                        Play(streamInfo.Url, title, artist, videoID, thumbnailURL);
                        break; //Crash chez celia, make a return here and check if the app keep on crashing

                    case "PlayNext":
                        AddToQueue(streamInfo.Url, title, artist, videoID, thumbnailURL);
                        parsing = false;
                        return;

                    case "PlayLast":
                        PlayLastInQueue(streamInfo.Url, title, artist, videoID, thumbnailURL);
                        parsing = false;
                        return;
                }

                Video info = await client.GetVideoAsync(videoID);
                thumbnailURL = info.Thumbnails.HighResUrl;
                if (artist == null || artist == "")
                    artist = info.Author;

                queue[CurrentID()].SetAlbum(thumbnailURL);
                queue[CurrentID()].SetArtist(artist);

                Player.instance?.RefreshPlayer();
                MainActivity.instance?.ShowSmallPlayer();

                if (MainActivity.instance != null)
                {
                    MainActivity.instance.FindViewById<ProgressBar>(Resource.Id.ytProgress).Visibility = ViewStates.Gone;

                    if (!MainActivity.instance.paused)
                    {
                        Intent intent = new Intent(this, typeof(Player));
                        StartActivity(intent);
                    }
                }
                parsing = false;
            }
        }

        async void GenerateNext(int number)
        {
            if (generating == true)
                return;

            generating = true;

            string youtubeID = null;
            if (MainActivity.HasInternet())
            {
                int i = 1;
                while (youtubeID == null)
                {
                    if (queue.Count >= i)
                    {
                        youtubeID = queue[queue.Count - i].youtubeID;
                        i++;
                    }
                    else
                        youtubeID = "local";
                }
            }
            else
                youtubeID = "local";

            if (youtubeID != "local" && MainActivity.HasInternet())
            {
                await MainActivity.instance.WaitForYoutube();

                //if (number < 4)
                //{
                //    SearchResource.ListRequest searchResult = YoutubeEngine.youtubeService.Search.List("snippet");
                //    searchResult.Fields = "items(id/videoId,snippet/title,snippet/thumbnails/high/url,snippet/channelTitle)";
                //    searchResult.Type = "video";
                //    searchResult.MaxResults = number + 2;
                //    searchResult.RelatedToVideoId = youtubeID;


                //    List<Song> songs = new List<Song>();
                //    Google.Apis.YouTube.v3.Data.SearchListResponse response = await searchResult.ExecuteAsync();
                //    foreach (Google.Apis.YouTube.v3.Data.SearchResult video in response.Items)
                //    {
                //        Song song = new Song(video.Snippet.Title, video.Snippet.ChannelTitle, video.Snippet.Thumbnails.High.Url, video.Id.VideoId, -1, -1, null, true, false);
                //        songs.Add(song);
                //    }
                //    songs = songs.Except(songs.Where(x => queue.Exists(y => x.youtubeID == y.youtubeID))).ToList();
                //    if(songs.Count > 0)
                //        PlayLastInQueue(songs[0]);
                //}
                //else
                //{
                    YoutubeClient client = new YoutubeClient();
                    Video video = await client.GetVideoAsync(youtubeID);

                    var ytPlaylistRequest = YoutubeEngine.youtubeService.PlaylistItems.List("snippet, contentDetails");
                    ytPlaylistRequest.PlaylistId = video.GetVideoMixPlaylistId();
                    ytPlaylistRequest.MaxResults = number + 2;

                    var ytPlaylist = await ytPlaylistRequest.ExecuteAsync();

                    foreach (var item in ytPlaylist.Items)
                    {
                        if (item.Snippet.Title != "[Deleted video]" && item.Snippet.Title != "Private video" && item.Snippet.Title != "Deleted video" && item.ContentDetails.VideoId != MusicPlayer.queue[MusicPlayer.CurrentID()].youtubeID)
                        {
                            Song song = new Song(item.Snippet.Title, "", item.Snippet.Thumbnails.Default__.Url, item.ContentDetails.VideoId, -1, -1, item.ContentDetails.VideoId, true, false);
                            if(!queue.Exists(x => x.youtubeID == song.youtubeID))
                            {
                                PlayLastInQueue(song);
                                break;
                            }
                        }
                    }
                //}
                ParseNextSong();
            }
            else
            {
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
            }

            Queue.instance?.Refresh();
            generating = false;
        }

        public async void RandomPlay(List<string> filePath, bool clearQueue)
        {
            currentID = -1;
            if(clearQueue)
                queue.Clear();

            Random r = new Random();
            filePath = filePath.OrderBy(x => r.Next()).ToList();
            if(clearQueue)
                Play(filePath[0]);
            
            while(instance == null)
                await Task.Delay(10);

            for (int i = clearQueue ? 1 : 0; i < filePath.Count; i++)
            {
                GetTrackSong(filePath[i], out Song song);
                song.queueSlot = queue.Count;
                queue.Add(song);
                await Task.Delay(10);
            }
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

            Player.instance?.UpdateNext();
            Queue.instance?.Refresh();
        }

        public void AddToQueue(string filePath, string title = null, string artist = null, string youtubeID = null, string thumbnailURI = null)
        {
            Song song = null;
            if(title == null)
                GetTrackSong(filePath, out song);
            else
                song = new Song(title, artist, thumbnailURI, youtubeID, -1, -1, filePath, true);

            if (song.queueSlot == -1)
                song.queueSlot = CurrentID() + 1;
            queue.Insert(song.queueSlot, song);
            UpdateQueueSlots();
        }

        public void AddToQueue(Song song)
        {
            if (song.queueSlot == -1)
                song.queueSlot = CurrentID() + 1;
            queue.Insert(song.queueSlot, song);
            UpdateQueueSlots();
        }

        public void PlayLastInQueue(string filePath)
        {
            GetTrackSong(filePath, out Song song);
            song.queueSlot = queue.Count;

            queue.Add(song);
        }

        public void PlayLastInQueue(Song song)
        {
            song.queueSlot = queue.Count;
            queue.Add(song);
        }

        public void PlayLastInQueue(string filePath, string title, string artist, string youtubeID, string thumbnailURI)
        {
            Song song = new Song(title, artist, thumbnailURI, youtubeID, -1, -1, filePath, true)
            {
                queueSlot = queue.Count
            };

            queue.Add(song);
        }

        public void PlayPrevious()
        {
            if (CurrentID() - 1 < 0)
                return;

            Song privous = queue[CurrentID() - 1];
            SwitchQueue(privous);
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
                    Pause();
                    return;
                }
            }

            Song next = queue[CurrentID() + 1];
            SwitchQueue(next);

            if (CurrentID() + 3 > queue.Count)
            {
                GenerateNext(1);
            }
        }

        public async void SwitchQueue(Song song)
        {
            if (!song.isParsed)
            {
                YoutubeClient client = new YoutubeClient();
                MediaStreamInfoSet mediaStreamInfo = await client.GetVideoMediaStreamInfosAsync(song.youtubeID);
                AudioStreamInfo streamInfo = mediaStreamInfo.Audio.Where(x => x.Container == Container.M4A).OrderBy(s => s.Bitrate).Last();
                song.SetPath(streamInfo.Url);
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

                Video info = await client.GetVideoAsync(song.youtubeID);
                song.SetAlbum(info.Thumbnails.HighResUrl);
                song.SetArtist(info.Author);
            }

            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            if (YoutubeEngine.FileIsAlreadyDownloaded(song.GetPath()) && !prefManager.GetBoolean("skipExistVerification", false))
            {
                GetTrackSong(YoutubeEngine.GetLocalPathFromYTID(song.GetPath()), out song);
            }

            Play(song, false);

            Player.instance?.RefreshPlayer();
            Queue.instance?.RefreshCurrent();

            CoordinatorLayout smallPlayer = MainActivity.instance.FindViewById<CoordinatorLayout>(Resource.Id.smallPlayer);
            smallPlayer.FindViewById<TextView>(Resource.Id.spTitle).Text = song.GetName();
            smallPlayer.FindViewById<TextView>(Resource.Id.spArtist).Text = song.GetArtist();
            smallPlayer.FindViewById<ImageView>(Resource.Id.spPlay).SetImageResource(Resource.Drawable.ic_pause_black_24dp);
            ImageView art = smallPlayer.FindViewById<ImageView>(Resource.Id.spArt);

            if(!song.IsYt)
            {
                var songCover = Uri.Parse("content://media/external/audio/albumart");
                var nextAlbumArtUri = ContentUris.WithAppendedId(songCover, song.GetAlbumArt());

                Picasso.With(Application.Context).Load(nextAlbumArtUri).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(art);
            }
            else
            {
                Picasso.With(Application.Context).Load(song.GetAlbum()).Placeholder(Resource.Drawable.MusicIcon).Resize(400, 400).CenterCrop().Into(art);
            }
        }

        public static int CurrentID()
        {
            if (queue.Count < currentID)
                currentID = -1;
            return currentID;
        }

        public static void SetSeekBar(DiscreteSeekBar bar)
        {
            bar.Max = (int) player.Duration;
            bar.Progress = (int) player.CurrentPosition;
            bar.ProgressChanged += (sender, e) =>
            {
                bool FromUser = e.P2;
                int Progress = e.P1;

                if (player != null && FromUser)
                    player.SeekTo(Progress);

                if (player != null && player.Duration - Progress <= 1500 && player.Duration - Progress > 0)
                    ParseNextSong();
            };
        }

        public static async void ParseNextSong()
        {
            if (CurrentID() == -1)
                return;

            if (CurrentID() + 1 > queue.Count - 1)
            {
                instance.GenerateNext(1);
                return;
            }

            Song song = queue[CurrentID() + 1];
            if (song.isParsed)
                return;

            if (!song.isParsed && !parsing)
            {
                parsing = true;
                YoutubeClient client = new YoutubeClient();
                MediaStreamInfoSet mediaStreamInfo = await client.GetVideoMediaStreamInfosAsync(song.youtubeID);
                AudioStreamInfo streamInfo = mediaStreamInfo.Audio.Where(x => x.Container == Container.M4A).OrderBy(s => s.Bitrate).Last();
                song.SetPath(streamInfo.Url);
                song.isParsed = true;

                Video info = await client.GetVideoAsync(song.youtubeID);
                song.SetAlbum(info.Thumbnails.HighResUrl);
                song.SetArtist(info.Author);

                parsing = false;
                if (Queue.instance != null)
                {
                    int item = queue.IndexOf(song);
                    int firstItem = ((LinearLayoutManager)Queue.instance.ListView.GetLayoutManager()).FindFirstVisibleItemPosition();
                    int lastItem = ((LinearLayoutManager)Queue.instance.ListView.GetLayoutManager()).FindLastVisibleItemPosition();
                    if(firstItem < item && item < lastItem)
                    {
                        ImageView youtubeIcon = Queue.instance.ListView.GetChildAt(item - firstItem).FindViewById<ImageView>(Resource.Id.youtubeIcon);
                        youtubeIcon.SetImageResource(Resource.Drawable.youtubeIcon);
                        youtubeIcon.ClearColorFilter();
                    }
                }
            }
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
                return player == null ? 0 : (int)player.CurrentPosition;
            }
        }

        void GetTrackSong(string filePath, out Song song)
        {
            string Title = "Unknow";
            string Artist = "Unknow";
            long AlbumArt = 0;
            long id = 0;
            string path;
            Uri musicUri = MediaStore.Audio.Media.ExternalContentUri;

            if (filePath.StartsWith("content://"))
                musicUri = Uri.Parse(filePath);

            Android.Content.CursorLoader cursorLoader = new Android.Content.CursorLoader(Application.Context, musicUri, null, null, null, null);
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

                    if (path == filePath || filePath.StartsWith("content://"))
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
            song = new Song(Title, Artist, null, null, AlbumArt, id, filePath);
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

            Intent tmpPreviusIntent = new Intent(Application.Context, typeof(MusicPlayer));
            tmpPreviusIntent.SetAction("Previus");
            PendingIntent previusIntent = PendingIntent.GetService(Application.Context, 0, tmpPreviusIntent, PendingIntentFlags.UpdateCurrent);

            Intent tmpPauseIntent = new Intent(Application.Context, typeof(MusicPlayer));
            tmpPauseIntent.SetAction("Pause");
            PendingIntent pauseIntent = PendingIntent.GetService(Application.Context, 0, tmpPauseIntent, PendingIntentFlags.UpdateCurrent);

            Intent tmpNextIntent = new Intent(Application.Context, typeof(MusicPlayer));
            tmpNextIntent.SetAction("Next");
            PendingIntent nextIntent = PendingIntent.GetService(Application.Context, 0, tmpNextIntent, PendingIntentFlags.UpdateCurrent);

            Intent mainActivity = new Intent(Application.Context, typeof(MainActivity));
            Intent tmpDefaultIntent = new Intent(Application.Context, typeof(Player));
            PendingIntent defaultIntent = PendingIntent.GetActivities(Application.Context, 0, new Intent[] { mainActivity, tmpDefaultIntent }, PendingIntentFlags.UpdateCurrent);

            Intent tmpDeleteIntent = new Intent(Application.Context, typeof(MusicPlayer));
            tmpDeleteIntent.SetAction("Stop");
            PendingIntent deleteIntent = PendingIntent.GetActivity(Application.Context, 0, tmpDeleteIntent, PendingIntentFlags.UpdateCurrent);

            notification = new NotificationCompat.Builder(Application.Context, "MusicApp.Channel")
                .SetVisibility(NotificationCompat.VisibilityPublic)
                .SetSmallIcon(Resource.Drawable.MusicIcon)

                .AddAction(Resource.Drawable.ic_skip_previous_black_24dp, "Previous", previusIntent)
                .AddAction(Resource.Drawable.ic_pause_black_24dp, "Pause", pauseIntent)
                .AddAction(Resource.Drawable.ic_skip_next_black_24dp, "Next", nextIntent)

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

                CoordinatorLayout smallPlayer = MainActivity.instance.FindViewById<CoordinatorLayout>(Resource.Id.smallPlayer);
                smallPlayer.FindViewById<ImageButton>(Resource.Id.spPlay).SetImageResource(Resource.Drawable.ic_play_arrow_black_24dp);

                Player.instance?.FindViewById<ImageButton>(Resource.Id.playButton).SetImageResource(Resource.Drawable.ic_play_arrow_black_24dp);
                Queue.instance?.RefreshCurrent();
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

                CoordinatorLayout smallPlayer = MainActivity.instance.FindViewById<CoordinatorLayout>(Resource.Id.smallPlayer);
                smallPlayer.FindViewById<ImageButton>(Resource.Id.spPlay).SetImageResource(Resource.Drawable.ic_pause_black_24dp);

                if (Player.instance != null)
                {
                    Player.instance.FindViewById<ImageButton>(Resource.Id.playButton).SetImageResource(Resource.Drawable.ic_pause_black_24dp);
                    Player.instance.handler.PostDelayed(Player.instance.UpdateSeekBar, 1000);
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
                Play(queue[CurrentID()], false, progress);
            }
        }

        public void Stop()
        {
            isRunning = false;
            title = null;
            queue?.Clear();
            parsing = false;
            currentID = -1;
            progress = 0;
            MainActivity.instance.HideSmallPlayer();
            Player.instance?.Stoped();
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
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            switch (focusChange)
            {
                case AudioFocus.Gain:
                    if (player == null)
                        InitializeService();

                    if (!isRunning)
                        player.PlayWhenReady = true;

                    player.Volume = prefManager.GetInt("volumeMultiplier", 100) / 100;
                    break;

                case AudioFocus.Loss:
                    Pause();
                    break;

                case AudioFocus.LossTransient:
                    Pause();
                    break;

                case AudioFocus.LossTransientCanDuck:
                    Pause();
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
            Console.WriteLine("Error in playback resetting: " + args.Cause);
        }

        public void OnPlayerStateChanged(bool playWhenReady, int state)
        {
            if (state == Com.Google.Android.Exoplayer2.Player.StateEnded)
            {
                PlayNext();
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
 