using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Media;
using Android.Net;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.Preferences;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Opus.Adapter;
using Opus.DataStructure;
using Opus.Fragments;
using Opus.Others;
using Square.Picasso;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TagLib;
using YoutubeExplode;
using YoutubeExplode.Models;
using YoutubeExplode.Models.MediaStreams;
using static Android.Provider.MediaStore.Audio;
using Bitmap = Android.Graphics.Bitmap;
using File = System.IO.File;
using Path = System.IO.Path;
using Picture = TagLib.Picture;
using Playlist = Opus.Fragments.Playlist;
using Stream = System.IO.Stream;

namespace Opus.Api.Services
{
    [Service]
    public class Downloader : Service, MediaScannerConnection.IOnScanCompletedListener
    {
        public static Downloader instance;
        public string downloadPath;
        public int maxDownload = 4;
        public static List<DownloadFile> queue = new List<DownloadFile>();

        public int currentStrike = 0;
        private static int downloadCount = 0;
        private static readonly List<string> files = new List<string>();
        private NotificationCompat.Builder notification;
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        private const int notificationID = 1001;
        private const int RequestCode = 5465;


        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override void OnCreate()
        {
            base.OnCreate();
        }

        public override void OnDestroy()
        {
            instance = null;
            queue.Clear();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            if(intent != null && intent.Action == "Cancel")
            {
                Cancel();
                return StartCommandResult.NotSticky;
            }

            instance = this;
            return StartCommandResult.Sticky;
        }

        public async static Task Init()
        {
            ISharedPreferences prefManager = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            string downloadPath = prefManager.GetString("downloadPath", null);
            if (downloadPath == null)
            {
                Snackbar snackBar = Snackbar.Make(MainActivity.instance.FindViewById(Resource.Id.snackBar), Resource.String.download_path_not_set, Snackbar.LengthLong).SetAction(Resource.String.set_path, (v) =>
                {
                    Intent pref = new Intent(Application.Context, typeof(Preferences));
                    MainActivity.instance.StartActivity(pref);
                });
                snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text).SetTextColor(Color.White);
                snackBar.Show();

                ISharedPreferencesEditor editor = prefManager.Edit();
                editor.PutString("downloadPath", Environment.GetExternalStoragePublicDirectory(Environment.DirectoryMusic).ToString());
                editor.Commit();

                downloadPath = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryMusic).ToString();
            }

            if (queue == null)
                queue = new List<DownloadFile>();

            Context context = Application.Context;
            Intent intent = new Intent(context, typeof(Downloader));
            context.StartService(intent);

            while (instance == null)
                await Task.Delay(10);

            instance.downloadPath = downloadPath;
            instance.maxDownload = prefManager.GetInt("maxDownload", 4);
        }

        #region Downloading of the queue
        public async void StartDownload()
        {
            System.Console.WriteLine("&Queue count: " + queue.Count);
            foreach (var item in queue)
            {
                System.Console.WriteLine("&Item: " + item.Name + " State:  " + item.State.ToString());
            }


            while (downloadCount < maxDownload && queue.Count(x => x.State == DownloadState.None) > 0)
            {
                System.Console.WriteLine("&Initiating the download of a song");
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(() => { DownloadAudio(queue.FindIndex(x => x.State == DownloadState.None), downloadPath); }, cancellation.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                await Task.Delay(10);
            }

            System.Console.WriteLine("&Start download finished");
        }

        private async void DownloadAudio(int position, string path)
        {
            if (position < 0 || position > queue.Count || queue[position].State != DownloadState.None)
                return;

            System.Console.WriteLine("&Downloading audio of: " + queue[position].Name);

            queue[position].State = DownloadState.Initialization;
            UpdateList(position);
            const string permission = Manifest.Permission.WriteExternalStorage;
            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(this, permission) != (int)Permission.Granted)
            {
                string[] permissions = new string[] { permission };
                MainActivity.instance.RequestPermissions(permissions, RequestCode);

                await Task.Delay(1000);
                while (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(this, permission) != (int)Permission.Granted)
                    await Task.Delay(500);
            }

            while (downloadCount >= maxDownload)
                await Task.Delay(1000);

            downloadCount++;
            currentStrike++;
            CreateNotification(queue[position].Name);

            try
            {
                YoutubeClient client = new YoutubeClient();
                Video video = await client.GetVideoAsync(queue[position].YoutubeID);
                MediaStreamInfoSet mediaStreamInfo = await client.GetVideoMediaStreamInfosAsync(queue[position].YoutubeID);
                MediaStreamInfo streamInfo;

                if (mediaStreamInfo.Audio.Count > 0)
                    streamInfo = mediaStreamInfo.Audio.Where(x => x.Container == Container.Mp4).OrderBy(s => s.Bitrate).Last();
                else if (mediaStreamInfo.Muxed.Count > 0)
                    streamInfo = mediaStreamInfo.Muxed.Where(x => x.Container == Container.Mp4).OrderBy(x => x.Resolution).Last();
                else
                {
                    queue[position].State = DownloadState.Error;
                    downloadCount--;
                    if (queue.Count != 0)
                        DownloadAudio(queue.FindIndex(x => x.State == DownloadState.None), path);

                    Playlist.instance?.CheckForSync();
                    return;
                }
                    
                queue[position].State = DownloadState.Downloading;
                UpdateList(position);
                string title = video.Title;
                foreach(char c in Path.GetInvalidFileNameChars()) //Make the title a valid filename (remove /, \, : etc).
                {
                    title = title.Replace(c, ' ');
                }

                string fileExtension = "m4a"; //audio only extension containing aac (audio codex of the mp4)

                string outpath = path;

                if (queue[position].PlaylistName != null)
                {
                    outpath = Path.Combine(path, queue[position].PlaylistName);
                    Directory.CreateDirectory(outpath);
                }

                string filePath = Path.Combine(outpath, title + "." + fileExtension);

                if (File.Exists(filePath))
                {
                    int i = 1;
                    string defaultPath = filePath;
                    do
                    {
                        filePath = Path.Combine(outpath, title + " (" + i + ")." + fileExtension);
                        i++;
                    }
                    while (File.Exists(filePath));
                }

                MediaStream input = await client.GetMediaStreamAsync(streamInfo);

                queue[position].Path = filePath;
                files.Add(filePath);
                FileStream output = File.Create(filePath);

                byte[] buffer = new byte[4096];
                int byteReaded = 0;
                while (byteReaded < input.Length)
                {
                    int read = await input.ReadAsync(buffer, 0, 4096);
                    await output.WriteAsync(buffer, 0, read);
                    byteReaded += read;
                    UpdateProgressBar(position, byteReaded / input.Length);
                }
                output.Dispose();

                queue[position].State = DownloadState.MetaData;
                UpdateList(position);
                if (queue.Count == 1)
                    SetNotificationProgress(100, true);

                SetMetaData(position, video.Title, video.Author, video.Thumbnails);
                files.Remove(filePath);
                downloadCount--;

                if (queue.Count != 0)
                    DownloadAudio(queue.FindIndex(x => x.State == DownloadState.None), path);

                Playlist.instance?.CheckForSync();
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
                Cancel();
            }
            catch (DirectoryNotFoundException)
            {
                Handler handler = new Handler(Looper.MainLooper);
                handler.Post(() => { Toast.MakeText(Application.Context, Resource.String.download_path_error, ToastLength.Long).Show(); });
                Cancel();
            }
            catch
            {
                MainActivity.instance.UnknowError(ErrorCode.DL1);
                Cancel();
            }
        }

        private async void SetMetaData(int position, string title, string artist, ThumbnailSet thumbnails)
        {
            string filePath = queue[position].Path;
            
            await Task.Run(async () => 
            {
                Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
                var meta = TagLib.File.Create(new StreamFileAbstraction(filePath, stream, stream));

                meta.Tag.Title = title;
                meta.Tag.Performers = new string[] { artist };
                meta.Tag.Album = title + " - " + artist;
                meta.Tag.Comment = queue[position].YoutubeID;
                IPicture[] pictures = new IPicture[1];
                Bitmap bitmap = Picasso.With(Application.Context).Load(await YoutubeManager.GetBestThumb(thumbnails)).Transform(new RemoveBlackBorder(true)).MemoryPolicy(MemoryPolicy.NoCache).Get();
                byte[] data;
                using (var MemoryStream = new MemoryStream())
                {
                    bitmap.Compress(Bitmap.CompressFormat.Png, 0, MemoryStream);
                    data = MemoryStream.ToArray();
                }
                bitmap.Recycle();
                pictures[0] = new Picture(data);
                meta.Tag.Pictures = pictures;

                meta.Save();
                stream.Dispose();
            });
            
            MediaScannerConnection.ScanFile(this, new string[] { filePath }, null, this);

            if (queue[position].PlaylistName == null)
                queue[position].State = DownloadState.Completed;
            else
                queue[position].State = DownloadState.Playlist;

            if (!queue.Exists(x => x.State == DownloadState.None || x.State == DownloadState.Downloading || x.State == DownloadState.Initialization || x.State == DownloadState.MetaData || x.State == DownloadState.Playlist))
            {
                StopForeground(true);
                DownloadQueue.instance?.Finish();
                queue.Clear();
            }
            else
                UpdateList(position);
        }

        public async void OnScanCompleted(string path, Uri uri)
        {
            System.Console.WriteLine("&Scan Completed with path = " + path + " and uri = " + uri.ToString());

            int position = queue.FindIndex(x => x.Path == path && x.State == DownloadState.Playlist);
            if (position != -1)
            {
                PlaylistManager.InsertToLocalPlaylist(await PlaylistManager.GetOrCreateByName(queue[position].PlaylistName), await LocalManager.GetSong(path), position + 1);
                queue[position].State = DownloadState.Completed;

                if (!queue.Exists(x => x.State == DownloadState.None || x.State == DownloadState.Downloading || x.State == DownloadState.Initialization || x.State == DownloadState.MetaData || x.State == DownloadState.Playlist))
                {
                    StopForeground(true);
                    DownloadQueue.instance?.Finish();
                    queue.Clear();
                }
                else
                    UpdateList(position);
            }
        }
        #endregion

        #region Playlist downloading
        public async void DownloadPlaylist(List<DownloadFile> files, long LocalID, bool keepDeleted)
        {
            if (LocalID != -1)
            {
                List<Song> songs = await PlaylistManager.GetTracksFromLocalPlaylist(LocalID);

                await Task.Run(() =>
                {
                    foreach (Song song in songs)
                        LocalManager.CompleteItem(song);
                });

                for (int i = 0; i < files.Count; i++)
                {
                    Song song = songs.Find(x => x.YoutubeID == files[i].YoutubeID);
                    if (song != null)
                    {
                        //Video is already in the playlist, we want to check if this item has been reordered.
                        if(int.Parse(song.TrackID) != i)
                        {
                            //The plus one is because android playlists have one-based indexes.
                            PlaylistManager.SetQueueSlot(LocalID, song.LocalID, i + 1);
                        }

                        //Video is already downloaded:
                        if (files[i].State == DownloadState.None)
                            files[i].State = DownloadState.UpToDate;

                        currentStrike++;
                        songs.Remove(song);
                    }
                }

                queue.RemoveAll(x => x.State == DownloadState.Completed || x.State == DownloadState.UpToDate || x.State == DownloadState.Canceled);
                if(files.Count(x => x.State == DownloadState.None) > 0)
                {
                    queue.AddRange(files);
                    StartDownload();
                }
                else
                {
                    Toast.MakeText(MainActivity.instance, Resource.String.playlist_uptodate, ToastLength.Long).Show();
                }

                await Task.Run(() =>
                {
                    if (Looper.MyLooper() == null)
                        Looper.Prepare();

                    for (int i = 0; i < songs.Count; i++)
                    {
                        //Video has been removed from the playlist but still exist on local storage
                        ContentResolver resolver = Application.ContentResolver;
                        Uri uri = Playlists.Members.GetContentUri("external", LocalID);
                        resolver.Delete(uri, Playlists.Members.Id + "=?", new string[] { songs[i].LocalID.ToString() });

                        if (!keepDeleted)
                            File.Delete(songs[i].Path);
                    }
                });
            }

            await Task.Delay(1000);
            Playlist.instance?.CheckForSync();
        }
        #endregion

        #region Cancel Handle
        void Cancel()
        {
            cancellation.Cancel(true);
            queue = new List<DownloadFile>();
            SetCancelNotification();
            DownloadQueue.instance?.Finish();
            CleanDownload();
        }

        void CleanDownload()
        {
            Task.Run(() =>
            {
                foreach (string filePath in files)
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
            });

            downloadCount = 0;
            currentStrike = 0;
            StopForeground(true);
            cancellation = new CancellationTokenSource();
            Playlist.instance?.SyncCanceled();
        }
        #endregion

        #region Notification / Queue callbacks
        void UpdateList(int position)
        {
            DownloadQueue.instance?.RunOnUiThread(() => { DownloadQueue.instance?.ListView.GetAdapter().NotifyItemChanged(position); });
        }

        void UpdateProgressBar(int position, long progress)
        {
            queue[position].progress = (int)(progress * 100);
            DownloadQueue.instance?.RunOnUiThread(() =>
            {
                LinearLayoutManager LayoutManager = (LinearLayoutManager)DownloadQueue.instance?.ListView?.GetLayoutManager();
                if (LayoutManager != null && position >= LayoutManager.FindFirstVisibleItemPosition() && position <= LayoutManager.FindLastVisibleItemPosition())
                {
                    View view = DownloadQueue.instance.ListView.GetChildAt(position - LayoutManager.FindFirstVisibleItemPosition());
                    DownloadHolder holder = (DownloadHolder)DownloadQueue.instance.ListView.GetChildViewHolder(view);
                    holder.Progress.Progress = (int)(progress * 100);
                }
            });
            if (queue.Count == 1)
                SetNotificationProgress((int)(progress * 100));
        }

        void CreateNotification(string title)
        {
            Intent intent = new Intent(MainActivity.instance, typeof(DownloadQueue));
            PendingIntent queueIntent = PendingIntent.GetActivity(Application.Context, 471, intent, PendingIntentFlags.UpdateCurrent);

            intent = new Intent(MainActivity.instance, typeof(Downloader));
            intent.SetAction("Cancel");
            PendingIntent cancelIntent = PendingIntent.GetService(Application.Context, 471, intent, PendingIntentFlags.OneShot);

            notification = new NotificationCompat.Builder(Application.Context, "Opus.Channel")
                .SetVisibility(NotificationCompat.VisibilityPublic)
                .SetSmallIcon(Resource.Drawable.NotificationIcon)
                .SetContentTitle(GetString(Resource.String.downloading_notification))
                .SetContentText(downloadCount > 1 ? GetString(Resource.String.tap_details) : title)
                .SetContentIntent(queueIntent)
                .AddAction(Resource.Drawable.Cancel, GetString(Resource.String.cancel), cancelIntent);

            if(queue.Count > 1)
            {
                notification.SetSubText(currentStrike + "/" + queue.Count);
                notification.SetProgress(queue.Count, currentStrike, false);
            }
            else
            {
                notification.SetProgress(100, 0, true);
            }
                
            StartForeground(notificationID, notification.Build());
        }

        void SetNotificationProgress(int progress, bool indeterminate = false)
        {
            notification.SetProgress(100, progress, indeterminate);
        }

        void SetCancelNotification()
        {
            notification.SetContentTitle(GetString(Resource.String.cancelling))
                .SetSubText((string)null);
              
            StartForeground(notificationID, notification.Build());
        }
        #endregion
    }
}
 