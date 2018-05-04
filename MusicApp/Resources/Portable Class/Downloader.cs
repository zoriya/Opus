using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Support.V4.App;
using Java.IO;
using Java.Net;
using MusicApp.Resources.values;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TagLib;
using YoutubeExplode;
using YoutubeExplode.Models;
using YoutubeExplode.Models.MediaStreams;
using File = System.IO.File;

namespace MusicApp.Resources.Portable_Class
{
    [Service]
    public class Downloader : Service
    {
        public static Downloader instance;
        public string downloadPath;

        private static List<DownloadFile> queue = new List<DownloadFile>();
        private int currentStrike = 0;
        private static bool isDownloading = false;
        private NotificationCompat.Builder notification;
        private int notificationID = 1001;
        private int RequestCode = 5465;


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
            instance = this;
            return StartCommandResult.Sticky;
        }

        public void Download(DownloadFile file)
        {
            queue.Add(file);
            if (!isDownloading)
                DownloadAudio(file, downloadPath);
            else
                SetNotificationCount();
        }

        private async void DownloadAudio(DownloadFile file, string path)
        {
            const string permission = Manifest.Permission.WriteExternalStorage;
            if (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(this, permission) != (int)Permission.Granted)
            {
                string[] permissions = new string[] { permission };
                MainActivity.instance.RequestPermissions(permissions, RequestCode);

                await Task.Delay(1000);
                while (Android.Support.V4.Content.ContextCompat.CheckSelfPermission(this, permission) != (int)Permission.Granted)
                    await Task.Delay(500);
            }

            while (isDownloading)
                await Task.Delay(1000);

            isDownloading = true;
            queue.Remove(file);
            currentStrike++;
            CreateNotification(file.name);

            YoutubeClient client = new YoutubeClient();
            Video videoInfo = await client.GetVideoAsync(file.videoID);
            MediaStreamInfoSet mediaStreamInfo = await client.GetVideoMediaStreamInfosAsync(file.videoID);
            AudioStreamInfo streamInfo = mediaStreamInfo.Audio.Where(x => x.Container == Container.M4A).OrderBy(s => s.Bitrate).Last();

            System.Console.WriteLine("&" + streamInfo.Url);
            //With Where container == Container.M4A, output file should be a m4a file, so ffmpeg is usless

            string fileExtension = streamInfo.Container.GetFileExtension();
            string fileName = $"{videoInfo.Title}.{fileExtension}";

            string filePath = Path.Combine(path, fileName);

            System.Console.WriteLine("&Client and path created");

            MediaStream input = await client.GetMediaStreamAsync(streamInfo);

            FileStream output = File.Create(filePath);
            await input.CopyToAsync(output);
            output.Dispose();

            System.Console.WriteLine("&Webm Output created");

            SetMetaData(filePath, videoInfo.Title, videoInfo.Author, videoInfo.Thumbnails.HighResUrl, file.videoID);
            isDownloading = false;

            if (queue.Count != 0)
                DownloadAudio(queue[0], path);
        }

        private void SetMetaData(string filePath, string title, string artist, string album, string youtubeID)
        {
            Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
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
            Android.Media.MediaScannerConnection.ScanFile(this, new string[] { filePath }, null, null);

            StopForeground(true);
        }

        void CreateNotification(string title)
        {
            notification = new NotificationCompat.Builder(Application.Context, "MusicApp.Channel")
                .SetVisibility(NotificationCompat.VisibilityPublic)
                .SetSmallIcon(Resource.Drawable.MusicIcon)
                .SetContentTitle("Downloading: ")
                .SetContentText(title);
            if(queue.Count > 1)
                notification.SetSubText(currentStrike + "/" + queue.Count);

            StartForeground(notificationID, notification.Build());
        }

        void SetNotificationCount()
        {
            notification.SetSubText(currentStrike + "/" + queue.Count);
            StartForeground(notificationID, notification.Build());
        }
    }
}
 