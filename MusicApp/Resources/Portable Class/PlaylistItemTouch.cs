using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Widget;
using Square.Picasso;
using System;
using System.Threading.Tasks;

namespace MusicApp.Resources.Portable_Class
{
    public class PlaylistItemTouch : ItemTouchHelper.Callback
    {
        private IItemTouchAdapter adapter;

        private int from = -1;
        private int to = -1;

        public override bool IsItemViewSwipeEnabled => true;
        public override bool IsLongPressDragEnabled => true;

        private Bitmap drawable;
        private Paint paint;


        public PlaylistItemTouch(IItemTouchAdapter adapter)
        {
            this.adapter = adapter;
            drawable = BitmapFactory.DecodeResource(MainActivity.instance.Resources, Resource.Drawable.MusicIcon);
            Console.WriteLine("&Drawable: " + drawable);
            paint = new Paint();
            paint.SetColorFilter(new PorterDuffColorFilter(Color.Argb(255, 33, 33, 33), PorterDuff.Mode.SrcIn));
        }

        public override int GetMovementFlags(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            int dragFlag = ItemTouchHelper.Up | ItemTouchHelper.Down;
            int swipeFlag = ItemTouchHelper.Left | ItemTouchHelper.Right;
            return MakeMovementFlags(dragFlag, swipeFlag);
        }

        public override bool OnMove(RecyclerView recyclerView, RecyclerView.ViewHolder source, RecyclerView.ViewHolder target)
        {
            if (from == -1)
                from = source.AdapterPosition;

            to = target.AdapterPosition;
            adapter.ItemMoved(source.AdapterPosition, target.AdapterPosition);
            return true;
        }

        public override void OnSwiped(RecyclerView.ViewHolder viewHolder, int direction)
        {
            //adapter.ItemDismissed(viewHolder.AdapterPosition);
        }


        public override void OnChildDraw(Canvas c, RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder, float dX, float dY, int actionState, bool isCurrentlyActive)
        {
            if (actionState == ItemTouchHelper.ActionStateSwipe)
            {
                viewHolder.ItemView.TranslationX = dX;
                ColorDrawable background = new ColorDrawable(Color.Red);
                background.SetBounds(viewHolder.ItemView.Right + (int)dX, viewHolder.ItemView.Top, viewHolder.ItemView.Right, viewHolder.ItemView.Bottom);
                background.Draw(c);
                c.DrawBitmap(drawable, viewHolder.ItemView.Left + MainActivity.instance.DpToPx(16), viewHolder.ItemView.Top + (viewHolder.ItemView.Bottom - viewHolder.ItemView.Top - drawable.Height) / 2, paint);
                c.DrawBitmap(drawable, viewHolder.ItemView.Right + drawable.Width - 20, (viewHolder.ItemView.Top + viewHolder.ItemView.Bottom) / 2, paint);
                MainActivity.instance.contentRefresh.SetEnabled(false);
                //adapter.DisableRefresh(true);
            }
            else
                base.OnChildDraw(c, recyclerView, viewHolder, dX, dY, actionState, isCurrentlyActive);
        }

        //public override void OnSelectedChanged(RecyclerView.ViewHolder viewHolder, int actionState)
        //{
        //    if (actionState != ItemTouchHelper.ActionStateIdle)
        //    {
        //        if (viewHolder is IItemTouchHolder)
        //            ((IItemTouchHolder)viewHolder).ItemSelected();
        //    }

        //    base.OnSelectedChanged(viewHolder, actionState);
        //}

        public override void ClearView(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            DefaultUIUtil.ClearView(viewHolder.ItemView);
        }
    }
}