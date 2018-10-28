using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Support.V4.App;
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
        public int maxDownload = 3;
        public static List<DownloadFile> queue = new List<DownloadFile>();

        private int currentStrike = 0;
        private static int downloadCount = 0;
        private static string filePath;
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
            System.Console.WriteLine("&Start Downloading with " + downloadCount + " items already downloading and a queue of " + queue.Count);
            while(downloadCount < maxDownload && queue.Count(x => x.State == DownloadState.None) > 0)
            {
#pragma warning disable CS4014
                Task.Run(() => { DownloadAudio(queue.FindIndex(x => x.State == DownloadState.None), downloadPath); }, cancellation.Token);
                await Task.Delay(10);
            }
        }

        private async void DownloadAudio(int position, string path)
        {
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
                string fileExtension = streamInfo.Container.GetFileExtension();
                string fileName = $"{videoInfo.Title}.{fileExtension}";

                string outpath = path;

                if (queue[position].playlist != null)
                {
                    outpath = Path.Combine(path, queue[position].playlist);
                    Directory.CreateDirectory(outpath);
                }

                filePath = Path.Combine(outpath, fileName);

                if (File.Exists(filePath))
                {
                    System.Console.WriteLine("&File Exist");
                    int i = 1;
                    string defaultPath = filePath;
                    do
                    {
                        filePath = defaultPath + "(" + i + ")";
                        i++;
                        System.Console.WriteLine("&File Still Exist");
                    }
                    while (File.Exists(filePath));
                }

                MediaStream input = await client.GetMediaStreamAsync(streamInfo);

                System.Console.WriteLine("&Creating file at " + filePath);
                FileStream output = File.Create(filePath);
                System.Console.WriteLine("&Copying data");
                await input.CopyToAsync(output, 4096, cancellation.Token);
                output.Dispose();

                System.Console.WriteLine("&Data copyed");
                queue[position].State = DownloadState.MetaData;
                UpdateList(position);
                SetMetaData(position, filePath, videoInfo.Title, videoInfo.Author, videoInfo.Thumbnails.HighResUrl, queue[position].videoID, queue[position].playlist);
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
            string playlist = path.Substring(downloadPath.Length + 1, path.IndexOf("/", downloadPath.Length) - downloadPath.Length);

            if(playlist != "")
            {
                Browse.act = MainActivity.instance;
                long playlistID = Browse.GetPlaylistID(playlist);

                ContentValues value = new ContentValues();
                value.Put(MediaStore.Audio.Playlists.Members.AudioId, id);
                value.Put(MediaStore.Audio.Playlists.Members.PlayOrder, 0);
                ContentResolver.Insert(MediaStore.Audio.Playlists.Members.GetContentUri("external", playlistID), value);
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

        void UpdateList(int position)
        {
            DownloadQueue.instance?.ListView.GetAdapter().NotifyItemChanged(position);
        }

        void CleanDownload()
        {
            if(File.Exists(filePath))
                File.Delete(filePath);

            downloadCount = 0;
            currentStrike = 0;
            StopForeground(true);
            cancellation = new CancellationTokenSource();
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
                notification.SetSubText(currentStrike + "/" + (currentStrike + queue.Count));
                notification.SetProgress(currentStrike + queue.Count, currentStrike, false);
            }
                
            StartForeground(notificationID, notification.Build());
        }

        void SetNotificationCount()
        {
            notification.SetSubText(currentStrike + "/" + (currentStrike + queue.Count));
            notification.SetProgress(currentStrike + queue.Count, currentStrike, false);
            StartForeground(notificationID, notification.Build());
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
        None
    }
}
 