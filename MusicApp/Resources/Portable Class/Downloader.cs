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
            string uri = intent.GetStringExtra("file");
            string path = intent.GetStringExtra("path");
            string name = intent.GetStringExtra("name");
            DownloadAudio(uri, path, name);
            return StartCommandResult.Sticky;
        }

        private async void DownloadAudio(string uri, string path, string name)
        {
            Console.WriteLine("Downloading");
            CreateNotification("Downloading: ", name);
            string videoID = uri.Remove(0, uri.IndexOf("=") + 1);

            var client = new YoutubeClient();
            var videoInfo = await client.GetVideoInfoAsync(videoID);

            Toast.MakeText(Application.Context, "Dowloading " + videoInfo.Title, ToastLength.Short).Show();

            var streamInfo = videoInfo.AudioStreams.OrderBy(s => s.Bitrate).Last();

            string fileExtension = streamInfo.Container.GetFileExtension();
            string fileName = $"{videoInfo.Title}.{fileExtension}";

            string filePath = Path.Combine(path, fileName);
            string finalPath = Path.Combine(path, videoInfo.Title + ".pcm");

            var input = await client.GetMediaStreamAsync(streamInfo);

            var output = File.Create(filePath);
            await input.CopyToAsync(output);
            output.Dispose();

            Toast.MakeText(Application.Context, "Download finish: " + videoInfo.Title + " Now converting", ToastLength.Long).Show();

            /*It's working, actualy download .webm file to the good path but .webm is a video file. Downloading only audio but do not convert to a mp3 file
             *Check for a converter add on or check if this downloader can convert to a mp3 file
             *Add a progress bar
             */

            ConvertToAudio(filePath, finalPath, name);
        }

        private async void ConvertToAudio(string path, string finalPath, string name)
        {
            EditNotification("Converting");
            //Crash here

            //string cmd = $"ffmpeg - i {path} - f mp3 - ab 192000 - vn {finalPath}"; //$"ffmpeg -i {path} -vn -acodec copy {fileName}.mp3";

            //await FFMpeg.Xamarin.FFMpegLibrary.Run(Application.Context, cmd, (s) =>
            //{
            //    /*logger?.Invoke(s);
            //    *int n = Extract(s, "Duration:", ",");
            //    *if (n != -1)
            //    *{
            //    *    total = n;
            //    *}
            //    *n = Extract(s, "time=", " bitrate=");
            //    *if (n != -1)
            //    *{
            //    *    current = n;
            //    *    onProgress?.Invoke(current, total);
            //    *}*/
            //});


            //ContentResolver resolver = Activity.ContentResolver;
            //ContentValues value = new ContentValues();
            //value.Put(MediaStore.Audio.Media.InterfaceConsts.Title, title);
            //value.Put(MediaStore.Audio.Media.InterfaceConsts.Artist, artist);
            //value.Put(MediaStore.Audio.Media.InterfaceConsts.Album, album);
            //value.Put(MediaStore.Audio.Media.InterfaceConsts.Data, path);
            //resolver.Insert(MediaStore.Audio.Media.ExternalContentUri, value);
            MediaExtractor extractor = new MediaExtractor();
            await extractor.SetDataSourceAsync(path);
            MediaFormat format = extractor.GetTrackFormat(0);
            string mine = format.GetString(MediaFormat.KeyMime);
            MediaCodec codec = MediaCodec.CreateDecoderByType(mine);
            codec.Configure(format, null, null, 0);
            codec.Start();

            OutputStream outputStream = new FileOutputStream(new Java.IO.File(finalPath));

            /*Working but the outputStream is empty (0B). The file is created but no data is parse in*/

            while (true)
            {
                int inputIndex = codec.DequeueInputBuffer(0);
                if (inputIndex == -1)
                    continue;

                int size = await extractor.ReadSampleDataAsync(codec.GetInputBuffer(inputIndex), 0);
                if (size == -1)
                    break;

                long time = extractor.SampleTime;
                /*MediaExtractorSampleFlags*/ MediaCodecBufferFlags flags = (MediaCodecBufferFlags) extractor.SampleFlags;
                await extractor.AdvanceAsync();
                codec.QueueInputBuffer(inputIndex, 0, size, time, flags);
                BufferInfo info = new BufferInfo();

                int outputIndex = codec.DequeueOutputBuffer(info, 0);
                if(outputIndex >= 0)
                {
                    byte[] data = new byte[info.Size];
                    codec.GetOutputBuffer(outputIndex).Get(data, 0, data.Length);
                    await outputStream.WriteAsync(data);
                }
            }

            await outputStream.FlushAsync();
            outputStream.Close();
            /*OutputStream os=new FileOutputStream(new File(destFile));
                long count=0;
                while(true){
                        int inputIndex=amc.dequeueInputBuffer(0);
                        if(inputIndex==-1){
                            continue;
                        }
                        int sampleSize=ame.readSampleData(aInputBuffers[inputIndex], 0);
                        if(sampleSize==-1)break;
                        long presentationTime=ame.getSampleTime();
                        int flag=ame.getSampleFlags();
                        ame.advance();
                        amc.queueInputBuffer(inputIndex, 0, sampleSize, presentationTime, flag);
                        BufferInfo info=new BufferInfo();

                        int outputIndex=amc.dequeueOutputBuffer(info, 0);
                        if (outputIndex >= 0) {
                            byte[] data=new byte[info.size];
                            aOutputBuffers[outputIndex].get(data, 0, data.length);
                            aOutputBuffers[outputIndex].clear();
                            os.write(data);
                            count+=data.length;
                            Log.i("write", ""+count);
                            amc.releaseOutputBuffer(outputIndex, false);
                        } else if (outputIndex == MediaCodec.INFO_OUTPUT_BUFFERS_CHANGED) {
                            aOutputBuffers = amc.getOutputBuffers();
                        } else if (outputIndex == MediaCodec.INFO_OUTPUT_FORMAT_CHANGED) {}
                }
                os.flush();
                os.close();*/

            Toast.MakeText(Application.Context, "Coneversion done", ToastLength.Long).Show();
            Console.WriteLine("Download finished");
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