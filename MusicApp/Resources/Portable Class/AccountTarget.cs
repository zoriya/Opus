using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Square.Picasso;

namespace MusicApp.Resources.Portable_Class
{
    public class AccountTarget : Java.Lang.Object, ITarget
    {
        public void OnPrepareLoad(Drawable p0) { }

        public void OnBitmapLoaded(Bitmap p0, Picasso.LoadedFrom p1)
        {
            Drawable drawable = new BitmapDrawable(MainActivity.instance.Resources, p0);
            MainActivity.instance.menu.FindItem(Resource.Id.settings).SetIcon(drawable);
        }

        public void OnBitmapFailed(Drawable p0)
        {
            MainActivity.instance.menu.FindItem(Resource.Id.settings).SetIcon(Resource.Drawable.account);
        }
    }
}