using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using System.Threading.Tasks;

namespace MusicApp.Resources.Portable_Class
{
    [Service]
    public class Sleeper : Service
    {
        public static Sleeper instance;
        public int timer = 0;

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
            int time = intent.GetIntExtra("time", timer);
            if (instance == null && time > 0)
                StartTimer(time);
            else
            {
                NotificationManager notificationManager = (NotificationManager)GetSystemService(NotificationService);
                if (time < 1)
                {
                    notificationManager.Cancel(1001);
                    StopSelf();
                }
                else
                {
                    timer = time;
                    NotificationCompat.Builder notification = new NotificationCompat.Builder(Application.Context, "MusicApp.Channel")
                        .SetVisibility(NotificationCompat.VisibilityPublic)
                        .SetSmallIcon(Resource.Drawable.MusicIcon)
                        .SetContentTitle("Music will stop in:")
                        .SetContentText(timer + " minutes")
                        .SetOngoing(true);

                    notificationManager.Notify(1001, notification.Build());
                }
            }
            return StartCommandResult.Sticky;
        }

        async void StartTimer(int time)
        {
            instance = this;
            timer = time; // In minutes

            Intent mainActivity = new Intent(Application.Context, typeof(MainActivity));
            Intent sleepIntent = new Intent(Application.Context, typeof(Player));
            sleepIntent.SetAction("Sleep");
            PendingIntent defaultIntent = PendingIntent.GetActivities(Application.Context, 0, new Intent[] { mainActivity, sleepIntent }, PendingIntentFlags.UpdateCurrent);


            NotificationCompat.Builder notification = new NotificationCompat.Builder(Application.Context, "MusicApp.Channel")
                .SetVisibility(NotificationCompat.VisibilityPublic)
                .SetSmallIcon(Resource.Drawable.MusicIcon)
                .SetContentTitle("Music will stop in:")
                .SetContentText(timer + " minutes")
                .SetContentIntent(defaultIntent)
                .SetOngoing(true);

            NotificationManager notificationManager = (NotificationManager)GetSystemService(NotificationService);

            while (timer > 0)
            {
                notification.SetContentText(timer + " minutes");
                notificationManager.Notify(1001, notification.Build());

                await Task.Delay(60000); // One minute in ms
                timer -= 1;
            }

            Intent musicIntent = new Intent(Application.Context, typeof(MusicPlayer));
            musicIntent.SetAction("SleepPause");
            Application.Context.StartService(musicIntent);
            notificationManager.Cancel(1001);
            instance = null;
            StopSelf();
        }
    }
}