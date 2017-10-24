using Android.App;
using Android.Content;

namespace MusicApp.Resources.Portable_Class
{
    public class SleepManager : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Intent musicIntent = new Intent(Application.Context, typeof(MusicPlayer));
            musicIntent.SetAction("Stop");
            MainActivity.instance.StartService(musicIntent);

            NotificationManager notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService);
            notificationManager.Cancel(Player.notificationID);
        }
    }
}