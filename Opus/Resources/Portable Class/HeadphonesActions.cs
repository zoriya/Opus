using Android.App;
using Android.Content;
using Android.Support.V4.Media.Session;

namespace Opus.Resources.Portable_Class
{
    public class HeadphonesActions : MediaSessionCompat.Callback
    {
        public override void OnPlay()
        {
            //base.OnPlay();
            System.Console.WriteLine("&Play");
            PlayPause();
        }

        public override void OnPause()
        {
            //base.OnPause();
            System.Console.WriteLine("&Pause");
            PlayPause();
        }

        public override void OnSkipToNext()
        {
            //base.OnSkipToNext();
            System.Console.WriteLine("&Next");
            Next();
        }

        public override void OnSkipToPrevious()
        {
            //base.OnSkipToPrevious();
            System.Console.WriteLine("&Previous");
            Previous();
        }

        void PlayPause()
        {
            Intent intent = new Intent(Application.Context, typeof(MusicPlayer));
            intent.SetAction("Pause");
            Application.Context.StartService(intent);
        }

        void Next()
        {
            Intent intent = new Intent(Application.Context, typeof(MusicPlayer));
            intent.SetAction("Next");
            Application.Context.StartService(intent);
        }

        void Previous()
        {
            Intent intent = new Intent(Application.Context, typeof(MusicPlayer));
            intent.SetAction("Previus");
            Application.Context.StartService(intent);
        }
    }
}