using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Square.Picasso;
using System.Collections.Generic;

namespace MusicApp.Resources.Portable_Class
{
    public class ChannelAdapter : ArrayAdapter
    {
        private Context context;
        private List<Song> channelList;
        private LayoutInflater inflater;
        private int resource;

        public ChannelAdapter(Context context, int resource, List<Song> channelList) : base(context, resource, channelList)
        {
            this.context = context;
            this.resource = resource;
            this.channelList = channelList;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if (position > channelList.Count || position < 0)
                return convertView;

            if (inflater == null)
            {
                inflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
            }
            if (convertView == null)
            {
                convertView = inflater.Inflate(resource, parent, false);
            }
            ChannelHolder holder = new ChannelHolder(convertView)
            {
                Title = { Text = channelList[position].Name },
                Artist = { Text = channelList[position].Artist },
            };

            Picasso.With(Application.Context).Load(channelList[position].Album).Placeholder(Resource.Drawable.MusicIcon).Transform(new CircleTransformation()).Into(holder.AlbumArt);

            if (MainActivity.Theme == 1)
            {
                holder.Title.SetTextColor(Color.White);
                holder.Artist.SetTextColor(Color.White);
                holder.Artist.Alpha = 0.7f;
            }

            if (TopicSelector.instance.selectedTopics.Contains(channelList[position].Name))
                holder.CheckBox.Checked = true;
            else
                holder.CheckBox.Checked = false;

            return convertView;
        }
    }
}