using Android.Graphics;
using Square.Picasso;

namespace MusicApp.Resources.Portable_Class
{
    public class RemoveBlackBorder : Java.Lang.Object, ITransformation
    {
        public string Key { get { return "NoBorder"; } }

        public RemoveBlackBorder() { }

        public Bitmap Transform(Bitmap source)
        {
            int height = (int)(source.Width * 0.5625f);
            int y = (source.Height - height) / 2;
            Bitmap bitmap = Bitmap.CreateBitmap(source, 0, y, source.Width, height);
            source.Recycle();
            return bitmap;
        }
    }
}