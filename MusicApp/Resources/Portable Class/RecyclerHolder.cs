using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using System;

namespace MusicApp.Resources.Portable_Class
{
    public class RecyclerHolder : RecyclerView.ViewHolder
    {
        public ImageView reorder;
        public LinearLayout textLayout;
        public TextView Title;
        public TextView Artist;
        public TextView Live;
        public ImageView AlbumArt;
        public ImageView edit;
        public ImageView youtubeIcon;
        public ImageView more;
        public TextView status;
        public CheckBox checkBox;
        public Button action;

        public RecyclerHolder(View itemView, Action<int> listener, Action<int> longListener) : base(itemView)
        {
            reorder = itemView.FindViewById<ImageView>(Resource.Id.reorder);
            textLayout = itemView.FindViewById<LinearLayout>(Resource.Id.textLayout);
            Title = itemView.FindViewById<TextView>(Resource.Id.title);
            Artist = itemView.FindViewById<TextView>(Resource.Id.artist);
            Live = itemView.FindViewById<TextView>(Resource.Id.isLive);
            AlbumArt = itemView.FindViewById<ImageView>(Resource.Id.albumArt);
            edit = itemView.FindViewById<ImageView>(Resource.Id.edit);
            youtubeIcon = itemView.FindViewById<ImageView>(Resource.Id.youtubeIcon);
            more = itemView.FindViewById<ImageView>(Resource.Id.moreButton);
            status = itemView.FindViewById<TextView>(Resource.Id.status);
            checkBox = itemView.FindViewById<CheckBox>(Resource.Id.checkBox);
            action = itemView.FindViewById<Button>(Resource.Id.action);

            itemView.Click += (sender, e) => listener(AdapterPosition);
            itemView.LongClick += (sender, e) => longListener(AdapterPosition);
        }
    }
}