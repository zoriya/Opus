using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;

namespace Opus.Resources.Portable_Class
{
    public class BottomSheetAction
    {
        public int Ressource;
        public string name;
        public EventHandler action;

        public BottomSheetAction(int ressource, string name, EventHandler action)
        {
            Ressource = ressource;
            this.name = name;
            this.action = action;
        }
    }

    public class BottomSheetAdapter : ArrayAdapter
    {
        private LayoutInflater inflater;
        private List<BottomSheetAction> items;
        public override int Count => items.Count;

        public BottomSheetAdapter(Context context, int resource, List<BottomSheetAction> items) : base(context, resource, items) { this.items = items; }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if (inflater == null)
                inflater = LayoutInflater.From(parent.Context);

            if (convertView == null)
                convertView = inflater.Inflate(Resource.Layout.BottomSheetText, parent, false);

            ((TextView)convertView).Text = items[position].name;

            Drawable icon = MainActivity.instance.GetDrawable(items[position].Ressource);
            if(MainActivity.Theme != 1)
                icon.SetTintList(ColorStateList.ValueOf(Color.Black));
            else
                icon.SetTintList(ColorStateList.ValueOf(Color.White));

            ((TextView)convertView).SetCompoundDrawablesWithIntrinsicBounds(icon, null, null, null);

            if(!convertView.HasOnClickListeners)
                convertView.Click += items[position].action;

            return convertView;
        }
    }
}