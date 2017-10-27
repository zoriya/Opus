using System.Collections.Generic;

using Android.Content;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using Android.Graphics;
using Android.Util;
using System.IO;
using Android.OS;
using Android.App;
using Square.Picasso;

namespace MusicApp.Resources.Portable_Class
{
    public class TwoLineAdapter : ArrayAdapter
    {
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
            TwoLineHolder holder = new TwoLineHolder(convertView)
            {
                Line1 = { Text = line1[position] },
                Line2 = { Text = line2[position].ToString() + ((line2[position] > 1) ? " elements" : " element") },
            };
            return convertView;
        }
    }
}