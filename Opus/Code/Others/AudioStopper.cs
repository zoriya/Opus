using Android.App;
using Android.Content;
using Android.Media;
using Opus.Api.Services;

namespace Opus.Others
{
    [IntentFilter(new[] { AudioManager.ActionAudioBecomingNoisy })]
    public class AudioStopper : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action != AudioManager.ActionAudioBecomingNoisy)
                return;

            MusicPlayer.ShouldResumePlayback = false;
            Intent musicIntent = new Intent(Application.Context, typeof(MusicPlayer));
            musicIntent.SetAction("ForcePause");
            Application.Context.StartService(musicIntent);
        }
    }
}