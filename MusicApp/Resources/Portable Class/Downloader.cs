using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Support.V4.App;
using MusicApp.Resources.values;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
            while (isDownloading)
                await Task.Delay(1000);

            isDownloading = true;
            queue.Remove(file);
            currentStrike++;
            CreateNotification(file.name);

            YoutubeClient client = new YoutubeClient();
            Video videoInfo = await client.GetVideoAsync(file.videoID);
            AudioStreamInfo streamInfo = videoInfo.AudioStreamInfos.OrderBy(s => s.Bitrate).Last();

            string fileExtension = streamInfo.Container.GetFileExtension();
            string fileName = $"{videoInfo.Title}.{fileExtension}";

            string filePath = Path.Combine(path, fileName);

            MediaStream input = await client.GetMediaStreamAsync(streamInfo);

            FileStream output = File.Create(filePath);
            await input.CopyToAsync(output);
            output.Dispose();
            SetMetaData(filePath, videoInfo.Title, videoInfo.Author.Name, videoInfo.Thumbnails.HighResUrl);
            isDownloading = false;

            if (queue.Count != 0)
                DownloadAudio(queue[0], path);
        }

        private void SetMetaData(string filePath, string title, string artist, string album)
        {
            ContentResolver resolver = MainActivity.instance.ContentResolver;
            ContentValues value = new ContentValues();
            value.Put(MediaStore.Audio.Media.InterfaceConsts.Title, title);
            value.Put(MediaStore.Audio.Media.InterfaceConsts.Artist, artist);
            value.Put(MediaStore.Audio.Media.InterfaceConsts.Album, album);
            value.Put(MediaStore.Audio.Media.InterfaceConsts.Data, filePath);
            resolver.Insert(MediaStore.Audio.Media.ExternalContentUri, value);

            StopForeground(true);
        }

        void CreateNotification(string title)
        {
            notification = new NotificationCompat.Builder(Application.Context)
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