using Android.Views;
using Android.Widget;

namespace MusicApp.Resources.values
{
    [System.Serializable]
    public class Holder
    {
        public TextView Title;
        public TextView Artist;
        public ImageView AlbumArt;
        public ImageView youtubeIcon;
        public ImageView more;

        public Holder(View v)
        {
            Title = v.FindViewById<TextView>(Resource.Id.title);
            Artist = v.FindViewById<TextView>(Resource.Id.artist);
            AlbumArt = v.FindViewById<ImageView>(Resource.Id.albumArt);
            youtubeIcon = v.FindViewById<ImageView>(Resource.Id.youtubeIcon);
            more = v.FindViewById<ImageView>(Resource.Id.moreButton);
        }
    }
}