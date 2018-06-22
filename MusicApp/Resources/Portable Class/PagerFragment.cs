using Android.OS;
using Android.Views;
using Android.Support.V4.App;
using Android.Support.V4.View;

namespace MusicApp.Resources.Portable_Class
{
    public class Pager : Fragment
    {
        private static Pager instance;
        private int type;
        private int pos;


        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public static Fragment NewInstance(int type, int pos)
        {
            instance = new Pager { Arguments = new Bundle() };
            instance.type = type;
            instance.pos = pos;
            return instance;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.ViewPager, container, false);
            if (type == 0)
                MainActivity.instance.SetBrowseTabs(view.FindViewById<ViewPager>(Resource.Id.pager), pos);
            else if (type == 1 || type == 2)
                MainActivity.instance.SetYtTabs(view.FindViewById<ViewPager>(Resource.Id.pager), YoutubeEngine.searchKeyWorld, pos);
            return view;
        }
    }
}