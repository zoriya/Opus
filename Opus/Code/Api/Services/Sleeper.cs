using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Opus.Api.Services;
using System.Threading.Tasks;

namespace Opus.Api.Services
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
                    NotificationCompat.Builder notification = new NotificationCompat.Builder(Application.Context, "Opus.Channel")
                        .SetVisibility(NotificationCompat.VisibilityPublic)
                        .SetSmallIcon(Resource.Drawable.NotificationIcon)
                        .SetContentTitle(GetString(Resource.String.sleep_timer))
                        .SetContentText(timer + " " + GetString(Resource.String.minutes))
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
            Intent sleepIntent = new Intent(Application.Context, typeof(MainActivity));
            sleepIntent.SetAction("Sleep");
            PendingIntent defaultIntent = PendingIntent.GetActivities(Application.Context, 0, new Intent[] { mainActivity, sleepIntent }, PendingIntentFlags.UpdateCurrent);


            NotificationCompat.Builder notification = new NotificationCompat.Builder(Application.Context, "Opus.Channel")
                .SetVisibility(NotificationCompat.VisibilityPublic)
                .SetSmallIcon(Resource.Drawable.NotificationIcon)
                .SetContentTitle(GetString(Resource.String.sleep_timer))
                .SetContentText(timer + " " + GetString(Resource.String.minutes))
                .SetContentIntent(defaultIntent)
                .SetOngoing(true);

            NotificationManager notificationManager = (NotificationManager)GetSystemService(NotificationService);

            while (timer > 0)
            {
                notification.SetContentText(timer + " " + (timer > 1 ? GetString(Resource.String.minutes) : GetString(Resource.String.minute)));
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