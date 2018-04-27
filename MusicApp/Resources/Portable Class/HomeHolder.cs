using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using System;

namespace MusicApp.Resources.Portable_Class
{
    public class HeaderHolder : RecyclerView.ViewHolder
    {
        public TextView headerText;

        public HeaderHolder(View itemView, Action<int> listener, Action<int> longListener) : base(itemView)
        {
            headerText = itemView.FindViewById<TextView>(Android.Resource.Id.Title);
            if(listener != null)
            {
                itemView.Click += (sender, e) => listener(AdapterPosition);
                itemView.LongClick += (sender, e) => longListener(AdapterPosition);
            }
        }
    }

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