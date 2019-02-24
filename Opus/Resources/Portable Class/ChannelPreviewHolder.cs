using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Opus.Resources.Portable_Class
{
    public class ChannelPreviewHolder : RecyclerView.ViewHolder
    {
        public RelativeLayout ChannelHolder;
        public TextView Name;
        public ImageView Logo;
        public LinearLayout MixHolder;
        public ImageView ChannelLogo;
        public ImageView MixOne;
        public ImageView MixTwo;

        public ChannelPreviewHolder(View itemView) : base(itemView)
        {
            ChannelHolder = itemView.FindViewById<RelativeLayout>(Resource.Id.channelHolder);
            Name = itemView.FindViewById<TextView>(Resource.Id.name);
            Logo = itemView.FindViewById<ImageView>(Resource.Id.logo);
            MixHolder = itemView.FindViewById<LinearLayout>(Resource.Id.mixHolder);
            ChannelLogo = itemView.FindViewById<ImageView>(Resource.Id.channelLogo);
            MixOne = itemView.FindViewById<ImageView>(Resource.Id.mixOne);
            MixTwo = itemView.FindViewById<ImageView>(Resource.Id.mixTwo);
        }
    }
}