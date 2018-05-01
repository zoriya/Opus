using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace MusicApp.Resources.values
{
    public class EmptyCategoryHolder : RecyclerView.ViewHolder
    {
        public TextView text;
        public EmptyCategoryHolder(View itemView) : base(itemView)
        {
            text = (TextView)itemView;
        }
    }
}