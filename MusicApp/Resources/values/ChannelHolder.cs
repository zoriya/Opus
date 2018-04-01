using Android.Views;
using Android.Widget;

namespace MusicApp.Resources.values
{
    [System.Serializable]
    public class ChannelHolder
    {
        public LinearLayout textLayout;
        public TextView Title;
        public TextView Artist;
        public ImageView AlbumArt;
        public CheckBox CheckBox;

        public ChannelHolder(View v)
        {
            textLayout = v.FindViewById<LinearLayout>(Resource.Id.textLayout);
            Title = v.FindViewById<TextView>(Resource.Id.title);
            Artist = v.FindViewById<TextView>(Resource.Id.artist);
            AlbumArt = v.FindViewById<ImageView>(Resource.Id.albumArt);
            CheckBox = v.FindViewById<CheckBox>(Resource.Id.checkBox);
        }
    }
}