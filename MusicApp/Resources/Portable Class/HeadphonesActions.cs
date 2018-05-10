using Android.Content;
using Android.Support.V4.Media.Session;

namespace MusicApp.Resources.Portable_Class
{
    public class HeadphonesActions : MediaSessionCompat.Callback
    {
        public override void OnPlay()
        {
            base.OnPlay();
            PlayPause();
        }

        public override void OnPause()
        {
            base.OnPause();
            PlayPause();
        }

        void PlayPause()
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(MusicPlayer));
            intent.SetAction("Pause");
            Android.App.Application.Context.StartService(intent);
        }
    }
}