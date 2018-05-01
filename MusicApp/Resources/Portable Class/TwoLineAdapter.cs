using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using System.Collections.Generic;

namespace MusicApp.Resources.Portable_Class
{
    public class TwoLineAdapter : ArrayAdapter
    {
        public int listPadding;
        private Context context;
        private List<string> line1;
        private List<int> line2;
        private LayoutInflater inflater;
        private int resource;

        public TwoLineAdapter(Context context, int resource, List<string> line1, List<int> line2) : base(context, resource, line1)
        {
            this.context = context;
            this.resource = resource;
            this.line1 = line1;
            this.line2 = line2;
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
            TwoLineHolder holder = new TwoLineHolder(convertView, null, null)
            {
                Line1 = { Text = line1[position] },
                Line2 = { Text = line2[position].ToString() + ((line2[position] > 1) ? " elements" : " element") },
            };

            if (!holder.more.HasOnClickListeners)
            {
                holder.more.Tag = position;
                holder.more.Click += (sender, e) => 
                {
                    int pos = (int)((ImageView)sender).Tag;

                    if(FolderBrowse.instance != null)
                        FolderBrowse.instance.More(pos);
                };
            }

            if (MainActivity.Theme == 1)
            {
                holder.more.SetColorFilter(Color.White);
                holder.Line1.SetTextColor(Color.White);
                holder.Line2.SetTextColor(Color.White);
                holder.Line2.Alpha = 0.7f;
            }

            float scale = MainActivity.instance.Resources.DisplayMetrics.Density;
            if (position + 1 == line1.Count)
            {
                convertView.SetPadding(0, 0, 0, listPadding);
                RelativeLayout.LayoutParams layoutParams = (RelativeLayout.LayoutParams)holder.more.LayoutParameters;
                layoutParams.SetMargins(0, 0, 0, listPadding);
                holder.more.LayoutParameters = layoutParams;
            }
            else
            {
                convertView.SetPadding(0, 0, 0, 0);
                RelativeLayout.LayoutParams layoutParams = (RelativeLayout.LayoutParams)holder.more.LayoutParameters;
                layoutParams.SetMargins(0, 0, 0, 0);
                holder.more.LayoutParameters = layoutParams;
            }
            return convertView;
        }
    }
}