using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using System;

namespace Opus.Resources.values
{
    public class HomeChannelHolder : RecyclerView.ViewHolder
    {
        public LinearLayout textLayout;
        public TextView Title;
        public TextView Artist;
        public ImageView AlbumArt;
        public CheckBox CheckBox;

        public HomeChannelHolder(View itemView, Action<int> listener, Action<int> longListener) : base(itemView)
        {
            textLayout = itemView.FindViewById<LinearLayout>(Resource.Id.textLayout);
            Title = itemView.FindViewById<TextView>(Resource.Id.title);
            Artist = itemView.FindViewById<TextView>(Resource.Id.artist);
            AlbumArt = itemView.FindViewById<ImageView>(Resource.Id.albumArt);
            CheckBox = itemView.FindViewById<CheckBox>(Resource.Id.checkBox);

            itemView.Click += (sender, e) => listener(AdapterPosition);
            itemView.LongClick += (sender, e) => longListener(AdapterPosition);
        }
    }
}