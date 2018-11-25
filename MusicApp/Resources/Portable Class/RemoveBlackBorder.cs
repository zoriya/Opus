using Android.Graphics;
using Square.Picasso;

namespace MusicApp.Resources.Portable_Class
{
    public class RemoveBlackBorder : Java.Lang.Object, ITransformation
    {
        private bool ResultIsSquare = false;
        public string Key { get { return "NoBorder"; } }

        public RemoveBlackBorder() { }

        public RemoveBlackBorder(bool ResultIsSquare) { this.ResultIsSquare = ResultIsSquare; }

        public Bitmap Transform(Bitmap source)
        {
            if (ResultIsSquare)
            {
                int size = (int)(source.Width * 0.5625f);
                int x = (int)(source.Width * 0.21875f);  //(source.Width - source.Width * 0.5625f) / 2 = source.Width * (1 - 0.5625) / 2
                int y = (source.Height - size) / 2;
                Bitmap bitmap = Bitmap.CreateBitmap(source, x, y, size, size);
                source.Recycle();
                return bitmap;
            }
            else
            {
                int height = (int)(source.Width * 0.5625f);
                int y = (source.Height - height) / 2;
                Bitmap bitmap = Bitmap.CreateBitmap(source, 0, y, source.Width, height);
                source.Recycle();
                return bitmap;
            }
        }
    }
}