using Android.Views;
using Android.Widget;

namespace MusicApp.Resources.values
{
    [System.Serializable]
    public class TwoLineHolder
    {
        public TextView Line1;
        public TextView Line2;

        public TwoLineHolder(View v)
        {
            Line1 = v.FindViewById<TextView>(Resource.Id.line1);
            Line2 = v.FindViewById<TextView>(Resource.Id.line2);
        }
    }
}