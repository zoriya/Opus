using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using System;

namespace MusicApp.Resources.Portable_Class
{
    public class QueueFooter : RecyclerView.ViewHolder
    {
        public Switch SwitchButton;
        public Button MixButton;
        public LinearLayout Autoplay;
        public TextView NextTitle;
        public ImageView NextAlbum;

        public QueueFooter(View itemView) : base(itemView)
        {
            SwitchButton = itemView.FindViewById<Switch>(Resource.Id.queueSwitch);
            MixButton = itemView.FindViewById<Button>(Resource.Id.createMix);
            Autoplay = itemView.FindViewById<LinearLayout>(Resource.Id.autoplay);
            NextTitle = itemView.FindViewById<TextView>(Resource.Id.apTitle);
            NextAlbum = itemView.FindViewById<ImageView>(Resource.Id.apAlbum);
        }
    }
}