using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using System;

namespace MusicApp.Resources.Portable_Class
{
    public class QueueFooter : RecyclerView.ViewHolder
    {
        public Switch SwitchButton;
        public Button MixButton;

        public QueueFooter(View itemView) : base(itemView)
        {
            SwitchButton = itemView.FindViewById<Switch>(Resource.Id.queueSwitch);
            MixButton = itemView.FindViewById<Button>(Resource.Id.createMix);
        }
    }
}