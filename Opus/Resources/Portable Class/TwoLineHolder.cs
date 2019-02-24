using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using System;

namespace Opus.Resources.values
{
    public class TwoLineHolder : RecyclerView.ViewHolder
    {
        public TextView Line1;
        public TextView Line2;
        public ImageView sync;
        public ImageView more;

        public TwoLineHolder(View itemView, Action<int> listener, Action<int> longListener) : base(itemView)
        {
            Line1 = itemView.FindViewById<TextView>(Resource.Id.line1);
            Line2 = itemView.FindViewById<TextView>(Resource.Id.line2);
            sync = itemView.FindViewById<ImageView>(Resource.Id.sync);
            more = itemView.FindViewById<ImageView>(Resource.Id.moreButton);

            if(listener != null)
            {
                itemView.Click += (sender, e) => listener(AdapterPosition);
                itemView.LongClick += (sender, e) => longListener(AdapterPosition);
            }
        }
    }
}