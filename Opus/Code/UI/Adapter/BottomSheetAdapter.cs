using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;
using System;
using System.Collections.Generic;

namespace Opus.Adapter
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

            Color color;
            TypedValue value = new TypedValue();
            if (Context.Theme.ResolveAttribute(Android.Resource.Attribute.ColorForeground, value, true))
                color = Color.ParseColor("#" + Integer.ToHexString(value.Data));
            else
                color = Color.Black;

            Drawable icon = MainActivity.instance.GetDrawable(items[position].Ressource);
            icon.SetTintList(ColorStateList.ValueOf(color));

            ((TextView)convertView).SetCompoundDrawablesWithIntrinsicBounds(icon, null, null, null);

            if(!convertView.HasOnClickListeners)
                convertView.Click += items[position].action;

            return convertView;
        }
    }
}