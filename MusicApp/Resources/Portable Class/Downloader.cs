using System;
using System.Linq;
using Android.Widget;
using System.IO;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Models;
using MusicApp.Resources.values;
using Android.App;
using Android.OS;
using Android.Content;
using Android.Support.V4.App;
using Android.Media;
using Java.Nio;
using Java.IO;

using Console = System.Console;
using File = System.IO.File;
using static Android.Media.MediaCodec;
using Android.Provider;

namespace MusicApp.Resources.Portable_Class
{
    [Service]
    public class Downloader : Service
    {
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

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            Console.WriteLine("Downloader service started");
            string videoID = intent.GetStringExtra("videoID");
            string path = intent.GetStringExtra("path");
            string name = intent.GetStringExtra("name");
            DownloadAudio(videoID, path, name);
            return StartCommandResult.Sticky;
        }

        private async void DownloadAudio(string videoID, string path, string name)
        {
            CreateNotification("Downloading: ", name);

            var client = new YoutubeClient();
            var videoInfo = await client.GetVideoInfoAsync(videoID);
            var streamInfo = videoInfo.AudioStreams.OrderBy(s => s.Bitrate).Last();

            string fileExtension = streamInfo.Container.GetFileExtension();
            string fileName = $"{videoInfo.Title}.{fileExtension}";

            string filePath = Path.Combine(path, fileName);
            string finalPath = Path.Combine(path, videoInfo.Title + ".pcm");

            var input = await client.GetMediaStreamAsync(streamInfo);

            var output = File.Create(filePath);
            await input.CopyToAsync(output);
            output.Dispose();

            SetMetaData(filePath, videoInfo.Title, videoInfo.Author.Name, videoInfo.ImageThumbnailUrl);
        }

        private void SetMetaData(string filePath, string title, string artist, string album)
        {
            EditNotification("Retriving meta-data");

            ContentResolver resolver = MainActivity.instance.ContentResolver;
            ContentValues value = new ContentValues();
            value.Put(MediaStore.Audio.Media.InterfaceConsts.Title, title);
            value.Put(MediaStore.Audio.Media.InterfaceConsts.Artist, artist);
            value.Put(MediaStore.Audio.Media.InterfaceConsts.Album, album);
            value.Put(MediaStore.Audio.Media.InterfaceConsts.Data, filePath);
            resolver.Insert(MediaStore.Audio.Media.ExternalContentUri, value);

            StopForeground(true);
        }

        void CreateNotification(string title, string artist)
        {
            notification = new NotificationCompat.Builder(Application.Context)
                .SetVisibility(NotificationCompat.VisibilityPublic)
                .SetSmallIcon(Resource.Drawable.MusicIcon)
                .SetContentTitle(title)
                .SetContentText(artist);
            StartForeground(notificationID, notification.Build());
        }

        void EditNotification(string title)
        {
            notification.SetContentTitle(title);
            StartForeground(notificationID, notification.Build());
        }
    }
}