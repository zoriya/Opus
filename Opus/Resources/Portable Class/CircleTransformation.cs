using Android.Graphics;
using Square.Picasso;
using System;

namespace Opus.Resources.Portable_Class
{
    public class CircleTransformation : Java.Lang.Object, ITransformation
    {
        private bool UseBorder = false;
        public string Key => "Circle-" + UseBorder;

        public CircleTransformation() { }

        public CircleTransformation(bool useBorder)
        {
            UseBorder = useBorder;
        }

        public Bitmap Transform(Bitmap source)
        {
            int size = Math.Min(source.Width, source.Height);

            int x = (source.Width - size) / 2;
            int y = (source.Height - size) / 2;

            Bitmap squaredBitmap = Bitmap.CreateBitmap(source, x, y, size , size);
            if (squaredBitmap != source)
            {
                source.Recycle();
            }

            Bitmap bitmap = Bitmap.CreateBitmap(size, size, source.GetConfig());
            Canvas canvas = new Canvas(bitmap);
            float r = size / 2f;

            if (UseBorder)
            {
                Paint BorderPaint = new Paint
                {
                    Color = Color.White,
                    AntiAlias = true
                };

                canvas.DrawCircle(r, r, r, BorderPaint);
            }

            Paint paint = new Paint();
            BitmapShader shader = new BitmapShader(squaredBitmap, Shader.TileMode.Clamp, Shader.TileMode.Clamp);
            paint.SetShader(shader);
            paint.AntiAlias = true;

            canvas.DrawCircle(r, r, r - (UseBorder ? 25 : 0), paint);

            squaredBitmap.Recycle();
            return bitmap;
        }
    }
}