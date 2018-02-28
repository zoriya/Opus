using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using System.Threading;
using System.Threading.Tasks;

namespace MusicApp.Resources.Portable_Class
{
    [Service]
    public class Sleeper : Service
    {
        public static Sleeper instance;
        public int timer;

        private bool stoped = false;

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
            if (instance == null)
                StartTimer(intent);
            else
            {
                int time = intent.GetIntExtra("time", timer);
                if (time == 0)
                {
                    stoped = true;
                    StopForeground(true);
                }
                else
                    timer = time;
            }
            return StartCommandResult.Sticky;
        }

        async void StartTimer(Intent intent)
        {
            instance = this;
            timer = intent.GetIntExtra("time", 0); // In minutes

            NotificationCompat.Builder notification = new NotificationCompat.Builder(Application.Context, "MusicApp.Channel")
                .SetVisibility(NotificationCompat.VisibilityPublic)
                .SetSmallIcon(Resource.Drawable.MusicIcon)
                .SetContentTitle("Music will stop in:")
                .SetContentText(timer + " minutes")
                .SetOngoing(true);

            NotificationManager notificationManager = (NotificationManager)GetSystemService(NotificationService);

            while (timer > 0)
            {
                notification.SetContentText(timer + " minutes");
                notificationManager.Notify(1001, notification.Build());

                await Task.Delay(60000); // One minute in ms
                timer -= 1;

                if (stoped)
                    return;
            }

            Intent musicIntent = new Intent(Application.Context, typeof(MusicPlayer));
            musicIntent.SetAction("SleepPause");
            MainActivity.instance.StartService(musicIntent);
            notificationManager.Cancel(1001);
        }
    }
}