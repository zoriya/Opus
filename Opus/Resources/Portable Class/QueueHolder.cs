using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using System;

namespace Opus.Resources.Portable_Class
{
    public class QueueHeader : RecyclerView.ViewHolder
    {
        public ImageButton Shuffle;
        public ImageButton Repeat;
        public ImageButton More;

        public QueueHeader(View itemView) : base(itemView)
        {
            Shuffle = itemView.FindViewById<ImageButton>(Resource.Id.shuffle);
            Repeat = itemView.FindViewById<ImageButton>(Resource.Id.repeat);
            More = itemView.FindViewById<ImageButton>(Resource.Id.more);
        }
    }

    public class QueueFooter : RecyclerView.ViewHolder
    {
        public Switch SwitchButton;
        //public Button MixButton;
        public CardView Autoplay;
        public TextView NextTitle;
        public ImageView NextAlbum;
        public ImageView RightIcon;

        public QueueFooter(View itemView) : base(itemView)
        {
            SwitchButton = itemView.FindViewById<Switch>(Resource.Id.queueSwitch);
            //MixButton = itemView.FindViewById<Button>(Resource.Id.createMix);
            Autoplay = itemView.FindViewById<CardView>(Resource.Id.autoplay);
            NextTitle = itemView.FindViewById<TextView>(Resource.Id.apTitle);
            NextAlbum = itemView.FindViewById<ImageView>(Resource.Id.apAlbum);
            RightIcon = itemView.FindViewById<ImageView>(Resource.Id.rightIcon);
        }
    }
}