using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using System;

namespace MusicApp.Resources.values
{
    public class ButtonHolder : RecyclerView.ViewHolder
    {
        public Button Button;

        public ButtonHolder(View itemView, Action<int> listener) : base(itemView)
        {
            Button = itemView.FindViewById<Button>(Resource.Id.button);

            if (listener != null)
            {
                itemView.Click += (sender, e) => listener(AdapterPosition);
            }
        }
    }
}