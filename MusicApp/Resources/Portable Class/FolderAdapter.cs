using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Android.Graphics;

namespace MusicApp.Resources.Portable_Class
{
    public class FolderAdapter : ArrayAdapter
    {
        public int selectedPosition;

        private Context context;
        private List<Folder> folders;
        private LayoutInflater inflater;
        private int resource;


        public FolderAdapter(Context context, int resource, List<Folder> folders) : base(context, resource, folders)
        {
            this.context = context;
            this.resource = resource;
            this.folders = folders;
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
            FolderHolder holder = new FolderHolder(convertView)
            {
                Name = { Text = folders[position].name },
            };

            holder.expandChild.Visibility = ViewStates.Visible;

            if (!folders[position].asChild)
                holder.expandChild.Visibility = ViewStates.Invisible;

            if(folders[position].isExtended)
                holder.expandChild.SetImageResource(Resource.Drawable.ic_expand_less_black_24dp);
            else
                holder.expandChild.SetImageResource(Resource.Drawable.ic_expand_more_black_24dp);

            convertView.FindViewById<RelativeLayout>(Resource.Id.folderList).SetPadding(folders[position].Padding, 0, 0, 0);

            holder.used.SetTag(Resource.Id.folderUsed, folders[position].uri);
            holder.used.Click += DownloadFragment.instance.Used_Click;
            holder.used.Checked = position == selectedPosition;
            holder.used.SetTag(Resource.Id.folderName, position);

            if(MainActivity.Theme == 1)
            {
                holder.Name.SetTextColor(Color.White);
                holder.expandChild.SetColorFilter(Color.White);
            }
            else
            {
                convertView.SetBackgroundColor(Color.White);
            }

            return convertView;
        }
    }
}