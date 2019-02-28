using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using System;

namespace Opus.Resources.Portable_Class
{
    public class RecyclerHolder : RecyclerView.ViewHolder
    {
        public ImageView reorder;
        public TextView Title;
        public TextView Artist;
        public TextView Live;
        public ImageView AlbumArt;
        public ImageView youtubeIcon;
        public ImageView more;
        public TextView status;
        public Button action;
        public View RightButtons;
        public View TextLayout;

        public RecyclerHolder(View itemView, Action<int> listener, Action<int> longListener) : base(itemView)
        {
            reorder = itemView.FindViewById<ImageView>(Resource.Id.reorder);
            Title = itemView.FindViewById<TextView>(Resource.Id.title);
            Artist = itemView.FindViewById<TextView>(Resource.Id.artist);
            Live = itemView.FindViewById<TextView>(Resource.Id.isLive);
            AlbumArt = itemView.FindViewById<ImageView>(Resource.Id.albumArt);
            youtubeIcon = itemView.FindViewById<ImageView>(Resource.Id.youtubeIcon);
            more = itemView.FindViewById<ImageView>(Resource.Id.moreButton);
            RightButtons = itemView.FindViewById(Resource.Id.rightButtons);
            TextLayout = itemView.FindViewById(Resource.Id.textLayout);
            status = itemView.FindViewById<TextView>(Resource.Id.status);
            action = itemView.FindViewById<Button>(Resource.Id.action);

            itemView.Click += (sender, e) => listener(AdapterPosition);
            itemView.LongClick += (sender, e) => longListener(AdapterPosition);
        }
    }
}