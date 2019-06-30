using Android.Content;
using Android.Views;
using Android.Widget;
using Opus.Fragments;
using System.Collections.Generic;

namespace Opus.Adapter
{
    public class LibrariesAdapter : ArrayAdapter
    {
        public int selectedPosition;

        private readonly List<string> libraries;
        private LayoutInflater inflater;

        public LibrariesAdapter(Context context, int resource, List<string> libraries) : base(context, resource, libraries)
        {
            this.libraries = libraries;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if (inflater == null)
            {
                inflater = Preferences.instance.LayoutInflater;
            }
            if (convertView == null)
            {
                convertView = inflater.Inflate(Android.Resource.Layout.SimpleListItem1, parent, false);
            }

            convertView.FindViewById<TextView>(Android.Resource.Id.Text1).Text = libraries[position];
            return convertView;
        }
    }
}