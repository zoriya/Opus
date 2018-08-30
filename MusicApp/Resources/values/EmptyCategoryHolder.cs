using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace MusicApp.Resources.values
{
    public class EmptyHolder : RecyclerView.ViewHolder
    {
        public TextView text;
        public EmptyHolder(View itemView) : base(itemView)
        {
            text = (TextView)itemView;
        }
    }
}