using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using System;

namespace Opus.Resources.values
{
    [System.Serializable]
    public class ChannelHolder
    {
        public LinearLayout textLayout;
        public TextView Title;
        public TextView Artist;
        public ImageView AlbumArt;
        public CheckBox CheckBox;

        public ChannelHolder(View v)
        {
            textLayout = v.FindViewById<LinearLayout>(Resource.Id.textLayout);
            Title = v.FindViewById<TextView>(Resource.Id.title);
            Artist = v.FindViewById<TextView>(Resource.Id.artist);
            AlbumArt = v.FindViewById<ImageView>(Resource.Id.albumArt);
            CheckBox = v.FindViewById<CheckBox>(Resource.Id.checkBox);
        }
    }

    public class RecyclerChannelHolder : RecyclerView.ViewHolder
    {
        public LinearLayout textLayout;
        public TextView Title;
        public TextView Artist;
        public ImageView AlbumArt;
        public CheckBox CheckBox;
        public Button action;

        public RecyclerChannelHolder(View itemView, Action<int> listener, Action<int> longListener) : base(itemView)
        {
            textLayout = itemView.FindViewById<LinearLayout>(Resource.Id.textLayout);
            Title = itemView.FindViewById<TextView>(Resource.Id.title);
            Artist = itemView.FindViewById<TextView>(Resource.Id.artist);
            AlbumArt = itemView.FindViewById<ImageView>(Resource.Id.albumArt);
            CheckBox = itemView.FindViewById<CheckBox>(Resource.Id.checkBox);
            action = itemView.FindViewById<Button>(Resource.Id.action);

            if (listener != null)
            {
                itemView.Click += (sender, e) => listener(AdapterPosition);
                itemView.LongClick += (sender, e) => longListener(AdapterPosition);
            }
        }
    }
}