using Android.OS;
using Android.Views;
using Android.Support.V4.App;

namespace MusicApp.Resources.Fragments
{
    public class DownloadFragment : Fragment
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public static Fragment NewInstance()
        {
            return new DownloadFragment { Arguments = new Bundle() };
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var useless = base.OnCreateView(inflater, container, savedInstanceState);
            return inflater.Inflate(Resource.Layout.DownloadLayout, null);
        }
    }
}