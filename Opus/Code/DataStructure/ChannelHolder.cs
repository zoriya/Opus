using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using System;

namespace Opus.DataStructure
{
    [Serializable]
    public class ChannelHolder
    {
        public LinearLayout textLayout;
        public TextView Title;
        public TextView Artist;
        public ImageView AlbumArt;

        public ChannelHolder(View v)
        {
            textLayout = v.FindViewById<LinearLayout>(Resource.Id.textLayout);
            Title = v.FindViewById<TextView>(Resource.Id.title);
            Artist = v.FindViewById<TextView>(Resource.Id.artist);
            AlbumArt = v.FindViewById<ImageView>(Resource.Id.albumArt);
        }
    }

    public class RecyclerChannelHolder : RecyclerView.ViewHolder
    {
        public LinearLayout textLayout;
        public TextView Title;
        public TextView Artist;
        public ImageView AlbumArt;
        public Button action;

        public RecyclerChannelHolder(View itemView, Action<int> listener, Action<int> longListener) : base(itemView)
        {
            textLayout = itemView.FindViewById<LinearLayout>(Resource.Id.textLayout);
            Title = itemView.FindViewById<TextView>(Resource.Id.title);
            Artist = itemView.FindViewById<TextView>(Resource.Id.artist);
            AlbumArt = itemView.FindViewById<ImageView>(Resource.Id.albumArt);
            action = itemView.FindViewById<Button>(Resource.Id.action);

            if (listener != null)
            {
                itemView.Click += (sender, e) => listener(AdapterPosition);
                itemView.LongClick += (sender, e) => longListener(AdapterPosition);
            }
        }
    }

    public class HomeChannelHolder : RecyclerView.ViewHolder
    {
        public LinearLayout textLayout;
        public TextView Title;
        public TextView Artist;
        public ImageView AlbumArt;

        public HomeChannelHolder(View itemView, Action<int> listener, Action<int> longListener) : base(itemView)
        {
            textLayout = itemView.FindViewById<LinearLayout>(Resource.Id.textLayout);
            Title = itemView.FindViewById<TextView>(Resource.Id.title);
            Artist = itemView.FindViewById<TextView>(Resource.Id.artist);
            AlbumArt = itemView.FindViewById<ImageView>(Resource.Id.albumArt);

            itemView.Click += (sender, e) => listener(AdapterPosition);
            itemView.LongClick += (sender, e) => longListener(AdapterPosition);
        }
    }
}