using Android.App;
using Android.Content;
using Android.Media;

namespace MusicApp.Resources.Portable_Class
{
    [IntentFilter(new[] { AudioManager.ActionAudioBecomingNoisy })]
    public class AudioStopper : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action != AudioManager.ActionAudioBecomingNoisy)
                return;

            Intent musicIntent = new Intent(Application.Context, typeof(MusicPlayer));
            musicIntent.SetAction("ForcePause");
            Application.Context.StartService(musicIntent);
        }
    }
}