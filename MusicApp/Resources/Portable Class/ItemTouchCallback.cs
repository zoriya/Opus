using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;

namespace MusicApp.Resources.Portable_Class
{
    public class ItemTouchCallback : ItemTouchHelper.Callback
    {
        private IItemTouchAdapter adapter;

        private int from = -1;
        private int to = -1;

        public override bool IsItemViewSwipeEnabled
        {
            get
            {
                if (PlaylistTracks.instance != null && (!PlaylistTracks.instance.hasWriteAcess || ((PlaylistTrackAdapter)adapter).IsEmpty))
                    return false;
                return true;
            }
        }
        public override bool IsLongPressDragEnabled
        {
            get
            {
                if (Queue.instance != null)
                    return true;
                else
                    return false;
            }
        }

        private Drawable drawable;


        public ItemTouchCallback(IItemTouchAdapter adapter)
        {
            this.adapter = adapter;
            drawable = MainActivity.instance.GetDrawable(Resource.Drawable.Delete);
        }

        public override int GetMovementFlags(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            int dragFlag = ItemTouchHelper.Up | ItemTouchHelper.Down;
            int swipeFlag = ItemTouchHelper.Left | ItemTouchHelper.Right;

            if (Queue.instance != null && viewHolder.AdapterPosition + 1 == Queue.instance.adapter.ItemCount)
                return MakeFlag(0, 0);

            if (Queue.instance != null && MusicPlayer.CurrentID() == viewHolder.AdapterPosition)
                return MakeMovementFlags(dragFlag, 0);

            return MakeMovementFlags(dragFlag, swipeFlag);
        }

        public override bool OnMove(RecyclerView recyclerView, RecyclerView.ViewHolder source, RecyclerView.ViewHolder target)
        {
            if (Queue.instance != null && target.AdapterPosition + 1 == Queue.instance.adapter.ItemCount)
                return false;

            if (from == -1)
                from = source.AdapterPosition;

            to = target.AdapterPosition;
            adapter.ItemMoved(source.AdapterPosition, target.AdapterPosition);
            return true;
        } 

        public override void OnSwiped(RecyclerView.ViewHolder viewHolder, int direction)
        {
            adapter.ItemDismissed(viewHolder.AdapterPosition);
            MainActivity.instance.contentRefresh.Enabled = true;
        }

        public override void OnChildDraw(Canvas c, RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder, float dX, float dY, int actionState, bool isCurrentlyActive)
        {
            if (actionState == ItemTouchHelper.ActionStateSwipe)
            {
                viewHolder.ItemView.TranslationX = dX;

                if(isCurrentlyActive)
                    MainActivity.instance.contentRefresh.Enabled = false;
                else
                    MainActivity.instance.contentRefresh.Enabled = true;

                ColorDrawable background = new ColorDrawable(Color.Red);
                if (dX < 0)
                {
                    background.SetBounds(viewHolder.ItemView.Right + (int)dX, viewHolder.ItemView.Top, viewHolder.ItemView.Right, viewHolder.ItemView.Bottom);
                    drawable.SetBounds(viewHolder.ItemView.Right - MainActivity.instance.DpToPx(52), viewHolder.ItemView.Top + (viewHolder.ItemView.Bottom - viewHolder.ItemView.Top - MainActivity.instance.DpToPx(36)) / 2, viewHolder.ItemView.Right - MainActivity.instance.DpToPx(16), viewHolder.ItemView.Top + (viewHolder.ItemView.Bottom - viewHolder.ItemView.Top + MainActivity.instance.DpToPx(36)) / 2);
                }
                else
                {
                    background.SetBounds(viewHolder.ItemView.Left, viewHolder.ItemView.Top, viewHolder.ItemView.Left + (int)dX, viewHolder.ItemView.Bottom);
                    drawable.SetBounds(viewHolder.ItemView.Left + MainActivity.instance.DpToPx(16), viewHolder.ItemView.Top + (viewHolder.ItemView.Bottom - viewHolder.ItemView.Top - MainActivity.instance.DpToPx(36)) / 2, viewHolder.ItemView.Left + MainActivity.instance.DpToPx(52), viewHolder.ItemView.Top + (viewHolder.ItemView.Bottom - viewHolder.ItemView.Top + MainActivity.instance.DpToPx(36)) / 2);
                }
                background.Draw(c);
                drawable.Draw(c);
            }
            else
                base.OnChildDraw(c, recyclerView, viewHolder, dX, dY, actionState, isCurrentlyActive);
        }

        public override void OnSelectedChanged(RecyclerView.ViewHolder viewHolder, int actionState)
        {
            if (actionState != ItemTouchHelper.ActionStateIdle)
            {
                if (viewHolder is IItemTouchHolder)
                    ((IItemTouchHolder)viewHolder).ItemSelected();
            }

            base.OnSelectedChanged(viewHolder, actionState);
        }

        public override void ClearView(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            base.ClearView(recyclerView, viewHolder);

            viewHolder.ItemView.Alpha = 1;

            MainActivity.instance.contentRefresh.Enabled = true;

            if (from != -1 && to != -1 && from != to)
                adapter.ItemMoveEnded(from, to);


            if (viewHolder is IItemTouchHolder)
                ((IItemTouchHolder)viewHolder).ItemClear();
        }
    }

    public interface IItemTouchAdapter
    {
        void ItemMoved(int fromPosition, int toPosition);
        void ItemMoveEnded(int fromPosition, int toPosition);
        void ItemDismissed(int position);
    }

    public interface IItemTouchHolder
    {
        void ItemSelected();

        void ItemClear();
    }
}