using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using System;

namespace MusicApp.Resources.Portable_Class
{
    public class HomeHolder : RecyclerView.ViewHolder
    {
        public ImageView reorder;
        public LinearLayout textLayout;
        public TextView Title;
        public TextView Artist;
        public ImageView AlbumArt;
        public ImageView youtubeIcon;
        public ImageView more;

        public HomeHolder(View itemView, Action<int> listener, Action<int> longListener) : base(itemView)
        {
            reorder = itemView.FindViewById<ImageView>(Resource.Id.reorder);
            textLayout = itemView.FindViewById<LinearLayout>(Resource.Id.textLayout);
            Title = itemView.FindViewById<TextView>(Resource.Id.title);
            Artist = itemView.FindViewById<TextView>(Resource.Id.artist);
            AlbumArt = itemView.FindViewById<ImageView>(Resource.Id.albumArt);
            youtubeIcon = itemView.FindViewById<ImageView>(Resource.Id.youtubeIcon);
            more = itemView.FindViewById<ImageView>(Resource.Id.moreButton);

            itemView.Click += (sender, e) => listener(AdapterPosition);
            itemView.LongClick += (sender, e) => longListener(AdapterPosition);
        }
    }
}