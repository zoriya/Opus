using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using System;

namespace MusicApp.Resources.Portable_Class
{
    public class QueueFooter : RecyclerView.ViewHolder
    {
        public Switch switchButton;

        public QueueFooter(View itemView) : base(itemView)
        {
            switchButton = itemView.FindViewById<Switch>(Resource.Id.queueSwitch);
        }
    }
}