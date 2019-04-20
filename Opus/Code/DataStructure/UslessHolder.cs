using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using System;

namespace Opus.DataStructure
{
    public class UslessHolder : RecyclerView.ViewHolder
    {
        public UslessHolder(View itemView, Action<int> listener = null) : base(itemView)
        {
            if (listener != null)
            {
                itemView.Click += (sender, e) => listener(AdapterPosition);
            }
        }
    }

    public class EmptyHolder : RecyclerView.ViewHolder
    {
        public TextView text;
        public EmptyHolder(View itemView) : base(itemView)
        {
            text = (TextView)itemView;
        }
    }

    public class HeaderHolder : RecyclerView.ViewHolder
    {
        public TextView headerText;
        public HeaderHolder(View itemView, Action<int> listener, Action<int> longListener) : base(itemView)
        {
            headerText = itemView.FindViewById<TextView>(Android.Resource.Id.Title);
            if (listener != null)
            {
                itemView.Click += (sender, e) => listener(AdapterPosition);
                itemView.LongClick += (sender, e) => longListener(AdapterPosition);
            }
        }
    }
}