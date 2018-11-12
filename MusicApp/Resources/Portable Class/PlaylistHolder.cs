using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using System;

namespace MusicApp.Resources.Portable_Class
{
    public class PlaylistHolder : RecyclerView.ViewHolder
    {
        public TextView Title;
        public TextView Owner;
        public ImageView AlbumArt;
        public ImageView edit;
        public ImageView sync;
        public ProgressBar SyncLoading;
        public ImageView more;

        public PlaylistHolder(View itemView, Action<int> listener, Action<int> longListener) : base(itemView)
        {
            Title = itemView.FindViewById<TextView>(Resource.Id.title);
            Owner = itemView.FindViewById<TextView>(Resource.Id.artist);
            AlbumArt = itemView.FindViewById<ImageView>(Resource.Id.albumArt);
            edit = itemView.FindViewById<ImageView>(Resource.Id.edit);
            sync = itemView.FindViewById<ImageView>(Resource.Id.sync);
            SyncLoading = itemView.FindViewById<ProgressBar>(Resource.Id.syncLoading);
            more = itemView.FindViewById<ImageView>(Resource.Id.moreButton);

            itemView.Click += (sender, e) => listener(AdapterPosition);
            itemView.LongClick += (sender, e) => longListener(AdapterPosition);
        }
    }
}