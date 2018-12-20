using Android.Gms.Cast.Framework.Media;
using Android.Widget;
using System;

namespace MusicApp.Resources.Portable_Class
{
    public class CastCallback : RemoteMediaClient.Callback
    {
        public override void OnMetadataUpdated()
        {
            base.OnMetadataUpdated();
            Console.WriteLine("&MetaData Updated");
            if (MusicPlayer.RemotePlayer.CurrentItem != null)
                MusicPlayer.currentID = MusicPlayer.RemotePlayer.MediaQueue.IndexOfItemWithId(MusicPlayer.RemotePlayer.CurrentItem.ItemId);

            Console.WriteLine("&CurrentID: " + MusicPlayer.currentID);

            Player.instance?.RefreshPlayer();
        }

        public override void OnQueueStatusUpdated()
        {
            base.OnQueueStatusUpdated();
            Console.WriteLine("&Queue status updated");
            MusicPlayer.GetQueueFromCast();
        }

        public override void OnSendingRemoteMediaRequest()
        {
            base.OnSendingRemoteMediaRequest();
            Console.WriteLine("&Sending Remote Media Request");
        }

        public override void OnStatusUpdated()
        {
            base.OnStatusUpdated();
            Console.WriteLine("&Stauts Updated");

            if (MusicPlayer.RemotePlayer.IsBuffering)
                Player.instance?.Buffering();
            else
                Player.instance?.Ready();

            if (MusicPlayer.RemotePlayer.IsPaused)
            {
                MusicPlayer.isRunning = false;
                FrameLayout smallPlayer = MainActivity.instance.FindViewById<FrameLayout>(Resource.Id.smallPlayer);
                smallPlayer?.FindViewById<ImageButton>(Resource.Id.spPlay)?.SetImageResource(Resource.Drawable.Play);

                if (Player.instance != null)
                {
                    MainActivity.instance?.FindViewById<ImageButton>(Resource.Id.playButton)?.SetImageResource(Resource.Drawable.Play);
                    Player.instance.handler?.PostDelayed(Player.instance.UpdateSeekBar, 1000);
                }
            }
            else
            {
                MusicPlayer.isRunning = true;
                FrameLayout smallPlayer = MainActivity.instance.FindViewById<FrameLayout>(Resource.Id.smallPlayer);
                smallPlayer?.FindViewById<ImageButton>(Resource.Id.spPlay)?.SetImageResource(Resource.Drawable.Pause);

                if (Player.instance != null)
                {
                    MainActivity.instance?.FindViewById<ImageButton>(Resource.Id.playButton)?.SetImageResource(Resource.Drawable.Pause);
                    Player.instance.handler?.PostDelayed(Player.instance.UpdateSeekBar, 1000);
                }
            }
        }
    }
}