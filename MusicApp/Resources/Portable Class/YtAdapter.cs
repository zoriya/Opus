using System.Collections.Generic;
using Android.Content;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Android.App;
using Square.Picasso;

namespace MusicApp.Resources.Portable_Class
{
    public class YtAdapter : ArrayAdapter
    {
        private Context context;
        private List<YtFile> ytList;
        private LayoutInflater inflater;
        private int resource;

        public YtAdapter(Context context, int resource, List<YtFile> ytList) : base(context, resource, ytList)
        {
            this.context = context;
            this.resource = resource;
            this.ytList = ytList;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if (inflater == null)
            {
                inflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
            }
            if (convertView == null)
            {
                convertView = inflater.Inflate(resource, parent, false);
            }
            Holder holder = new Holder(convertView)
            {
                Title = { Text = ytList[position].Title },
                Artist = { Text = ytList[position].channelTitle },
            };

            Picasso.With(Application.Context).Load(ytList[position].thumbnailUrl).Placeholder(Resource.Drawable.MusicIcon).Into(holder.AlbumArt);
            return convertView;
        }
    }
}