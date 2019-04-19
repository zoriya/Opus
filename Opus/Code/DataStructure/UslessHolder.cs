using Android.Support.V7.Widget;
using Android.Views;
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
}