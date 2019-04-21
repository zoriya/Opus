using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Opus.DataStructure;
using Opus.Fragments;
using System.Collections.Generic;

namespace Opus.Adapter
{
    public class SuggestionAdapter : ArrayAdapter
    {
        private List<Suggestion> objects;
        private LayoutInflater inflater;
        private Context context;

        public override int Count => objects.Count;

        public SuggestionAdapter(Context context, int resource, List<Suggestion> objects) : base(context, resource, objects)
        {
            this.context = context;
            this.objects = objects;
        }

        public void Remove(Suggestion item)
        {
            objects.Remove(item);
            NotifyDataSetChanged();
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if (inflater == null)
            {
                inflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
            }
            if (convertView == null)
            {
                convertView = inflater.Inflate(Resource.Layout.SuggestionLayout, parent, false);
            }

            convertView.FindViewById<ImageView>(Resource.Id.icon1).SetImageResource(objects[position].Icon);
            convertView.FindViewById<TextView>(Resource.Id.text).Text = objects[position].Text;
            if (!convertView.FindViewById<ImageView>(Resource.Id.refine).HasOnClickListeners)
                convertView.FindViewById<ImageView>(Resource.Id.refine).Click += (sender, e) => { SearchableActivity.instance.Refine(position); };

            if (MainActivity.Theme == 1)
            {
                convertView.FindViewById<ImageView>(Resource.Id.icon1).SetColorFilter(Color.White);
                convertView.FindViewById<ImageView>(Resource.Id.refine).SetColorFilter(Color.White);
            }

            return convertView;
        }
    }
}