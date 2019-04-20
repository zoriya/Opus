using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using System;

namespace Opus.Resources.Portable_Class
{
    public class HomeHolder : RecyclerView.ViewHolder
    {
        public LinearLayout textLayout;
        public TextView Title;
        public TextView Artist;
        public ImageView AlbumArt;
        public ImageView more;

        public HomeHolder(View itemView, Action<int> listener, Action<int> longListener) : base(itemView)
        {
            textLayout = itemView.FindViewById<LinearLayout>(Resource.Id.textLayout);
            Title = itemView.FindViewById<TextView>(Resource.Id.title);
            Artist = itemView.FindViewById<TextView>(Resource.Id.artist);
            AlbumArt = itemView.FindViewById<ImageView>(Resource.Id.albumArt);
            more = itemView.FindViewById<ImageView>(Resource.Id.moreButton);

            itemView.Click += (sender, e) => listener(AdapterPosition);
            itemView.LongClick += (sender, e) => longListener(AdapterPosition);
        }
    }
}