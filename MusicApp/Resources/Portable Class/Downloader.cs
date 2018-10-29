using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TagLib;
using YoutubeExplode;
using YoutubeExplode.Models;
using YoutubeExplode.Models.MediaStreams;
using File = System.IO.File;

namespace MusicApp.Resources.Portable_Class
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
        private static List<string> files = new List<string>();
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
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            if(intent.Action == "Cancel")
            {
                Cancel();
                return StartCommandResult.NotSticky;
            }

            instance = this;
            return StartCommandResult.Sticky;
        }

        public async void StartDownload()
        {
            while(downloadCount < maxDownload && queue.Count(x => x.State == DownloadState.None) > 0)
            {
#pragma warning disable CS4014
                Task.Run(() => { DownloadAudio(queue.FindIndex(x => x.State == DownloadState.None), downloadPath); }, cancellation.Token);
                await Task.Delay(10);
            }
        }

        private async void DownloadAudio(int position, string path)
        {
            System.Console.WriteLine("&Downloading item " + position + "/" + queue.Count);
            if (position < 0 || position > queue.Count || queue[position].State != DownloadState.None)
                return;

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
            CreateNotification(queue[position].name);

            try
            {
                YoutubeClient client = new YoutubeClient();
                Video videoInfo = await client.GetVideoAsync(queue[position].videoID);
                MediaStreamInfoSet mediaStreamInfo = await client.GetVideoMediaStreamInfosAsync(queue[position].videoID);
                AudioStreamInfo streamInfo = mediaStreamInfo.Audio.Where(x => x.Container == Container.M4A).OrderBy(s => s.Bitrate).Last();

                queue[position].State = DownloadState.Downloading;
                UpdateList(position);
                string title = videoInfo.Title;
                string fileExtension = streamInfo.Container.GetFileExtension();

                string outpath = path;

                if (queue[position].playlist != null)
                {
                    outpath = Path.Combine(path, queue[position].playlist);
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

                queue[position].path = filePath;
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

                SetMetaData(position, filePath, videoInfo.Title, videoInfo.Author, videoInfo.Thumbnails.HighResUrl, queue[position].videoID, queue[position].playlist);
                files.Remove(filePath);
                downloadCount--;

                if (queue.Count != 0)
                    DownloadAudio(queue.FindIndex(x => x.State == DownloadState.None), path);
            }
            catch (YoutubeExplode.Exceptions.ParseException)
            {
                MainActivity.instance.YoutubeEndPointChanged();
                Cancel();
            }
            catch (System.Net.Http.HttpRequestException)
            {
                MainActivity.instance.Timout();
                Cancel();
            }
            catch (DirectoryNotFoundException)
            {
                Handler handler = new Handler(Looper.MainLooper);
                handler.Post(() => { Toast.MakeText(Application.Context, "Download path do not exist anymore, please change it in the settings", ToastLength.Long).Show(); });
                Cancel();
            }
        }

        private void SetMetaData(int position, string filePath, string title, string artist, string album, string youtubeID, string playlist)
        {
            System.IO.Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
            var meta = TagLib.File.Create(new StreamFileAbstraction(filePath, stream, stream));

            meta.Tag.Title = title;
            meta.Tag.Performers = new string[] { artist };
            meta.Tag.Album = album;
            meta.Tag.Comment = youtubeID;

            IPicture[] pictures = new IPicture[1];
            WebClient client = new WebClient();
            byte[] data = client.DownloadData(album);
            pictures[0] = new Picture(data);
            meta.Tag.Pictures = pictures;
            meta.Save();
            stream.Dispose();
            MediaScannerConnection.ScanFile(this, new string[] { filePath }, null, this);

            queue[position].State = DownloadState.Completed;
            UpdateList(position);

            if (!queue.Exists(x => x.State != DownloadState.Completed))
                StopForeground(true);
        }

        public void OnScanCompleted(string path, Uri uri)
        {
            long id = long.Parse(uri.ToString().Substring(uri.ToString().IndexOf("audio/media/") + 12, uri.ToString().Length - uri.ToString().IndexOf("audio/media/") - 12));
            string playlist = path.Substring(downloadPath.Length + 1, path.IndexOf("/", downloadPath.Length + 1) - (downloadPath.Length + 1));

            if (playlist != "")
            {
                Handler handler = new Handler(MainActivity.instance.MainLooper);
                handler.Post(() =>
                {

                    Browse.act = MainActivity.instance;
                    Browse.AddToPlaylist(Browse.GetSong(path), playlist, -1);
                });
            }
        }

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
        }

        void UpdateList(int position)
        {
            DownloadQueue.instance?.RunOnUiThread(() => { DownloadQueue.instance?.ListView.GetAdapter().NotifyItemChanged(position); });
        }

        void UpdateProgressBar(int position, long progress)
        {
            queue[position].progress = (int)(progress * 100);
            DownloadQueue.instance?.RunOnUiThread(() =>
            {
                LinearLayoutManager LayoutManager = (LinearLayoutManager)DownloadQueue.instance.ListView.GetLayoutManager();
                if (position >= LayoutManager.FindFirstVisibleItemPosition() && position <= LayoutManager.FindLastVisibleItemPosition())
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
            PendingIntent queueIntent = PendingIntent.GetActivity(Application.Context, 471, intent, PendingIntentFlags.OneShot);

            intent = new Intent(MainActivity.instance, typeof(Downloader));
            intent.SetAction("Cancel");
            PendingIntent cancelIntent = PendingIntent.GetService(Application.Context, 471, intent, PendingIntentFlags.OneShot);

            notification = new NotificationCompat.Builder(Application.Context, "MusicApp.Channel")
                .SetVisibility(NotificationCompat.VisibilityPublic)
                .SetSmallIcon(Resource.Drawable.MusicIcon)
                .SetContentTitle("Downloading: ")
                .SetContentText(downloadCount > 1 ? "Tap for more details" : title)
                .SetContentIntent(queueIntent)
                .AddAction(Resource.Drawable.Cancel, "Cancel", cancelIntent);

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
            notification.SetContentTitle("Cancelling...")
                .SetSubText((string)null);
              
            StartForeground(notificationID, notification.Build());
        }
    }

    public enum DownloadState
    {
        Initialization,
        Downloading,
        MetaData,
        Completed,
        Canceled,
        None
    }
}
 