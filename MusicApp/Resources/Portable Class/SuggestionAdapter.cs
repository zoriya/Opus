using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using MusicApp.Resources.values;
using System.Collections.Generic;

namespace MusicApp.Resources.Portable_Class
{
    public class SuggestionAdapter : ArrayAdapter
    {
        private List<Suggestion> objects;
        private LayoutInflater inflater;
        private Context context;
        private int resource;

        public override int Count => objects.Count;

        public SuggestionAdapter(Context context, int resource, List<Suggestion> objects) : base(context, resource, objects)
        {
            this.context = context;
            this.resource = resource;
            this.objects = objects;
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

            convertView.FindViewById<ImageView>(Resource.Id.icon1).SetX((int)(18 * SearchableActivity.instance.Resources.DisplayMetrics.Density + 0.5f));
            convertView.FindViewById<TextView>(Resource.Id.text).SetX(SearchableActivity.instance.searchView.GetX());
            convertView.FindViewById<TextView>(Resource.Id.text).SetPadding((int)(16 * SearchableActivity.instance.Resources.DisplayMetrics.Density + 0.5f), 0, 0, 0);
            convertView.FindViewById<ImageView>(Resource.Id.refine).SetPadding(0, 0, (int)(6 * SearchableActivity.instance.Resources.DisplayMetrics.Density + 0.5f), 0);

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