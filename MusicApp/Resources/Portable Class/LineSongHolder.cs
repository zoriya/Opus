using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using System;

namespace MusicApp.Resources.Portable_Class
{
    public class LineSongHolder : RecyclerView.ViewHolder
    {
        public TextView title;
        public RecyclerView recycler;

        public LineSongHolder(View itemView, Action<int> listener, Action<int> longListener) : base(itemView)
        {
            title = itemView.FindViewById<TextView>(Resource.Id.title);
            recycler = itemView.FindViewById<RecyclerView>(Resource.Id.lineRecycler);

            itemView.Click += (sender, e) => listener(AdapterPosition);
            itemView.LongClick += (sender, e) => longListener(AdapterPosition);
        }
    }
}