using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;

namespace Opus.Resources.Portable_Class
{
    public class ItemTouchCallback : ItemTouchHelper.Callback
    {
        private IItemTouchAdapter adapter;
        private readonly bool alwaysAllowSwap;

        private int from = -1;
        private int to = -1;

        public override bool IsItemViewSwipeEnabled
        {
            get
            {
                if (alwaysAllowSwap)
                    return true;

                if (PlaylistTracks.instance != null && (!PlaylistTracks.instance.hasWriteAcess || ((PlaylistTrackAdapter)adapter)?.IsEmpty == true))
                    return false;
                return true;
            }
        }
        public override bool IsLongPressDragEnabled
        {
            get
            {
                return false;
            }
        }

        private Drawable drawable;


        public ItemTouchCallback(IItemTouchAdapter adapter, bool alwaysAllowSwap)
        {
            this.adapter = adapter;
            this.alwaysAllowSwap = alwaysAllowSwap;
            drawable = Android.App.Application.Context.GetDrawable(Resource.Drawable.Delete);
        }

        public override int GetMovementFlags(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            int dragFlag = ItemTouchHelper.Up | ItemTouchHelper.Down;
            int swipeFlag = ItemTouchHelper.Left | ItemTouchHelper.Right;

            if (alwaysAllowSwap && (viewHolder.AdapterPosition + 1 == ((QueueAdapter)adapter).ItemCount || viewHolder.AdapterPosition == 0))
                return MakeFlag(0, 0);

            if (alwaysAllowSwap && MusicPlayer.CurrentID() + 1 == viewHolder.AdapterPosition)
                return MakeMovementFlags(dragFlag, 0);

            return MakeMovementFlags(dragFlag, swipeFlag);
        }

        public override bool OnMove(RecyclerView recyclerView, RecyclerView.ViewHolder source, RecyclerView.ViewHolder target)
        {
            adapter.IsSliding = true;

            if (alwaysAllowSwap && (target.AdapterPosition + 1 == ((QueueAdapter)adapter).ItemCount || target.AdapterPosition == 0))
                return false;

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
            adapter.IsSliding = false;

            viewHolder.ItemView.Alpha = 1;

            MainActivity.instance.contentRefresh.Enabled = true;

            if (from != -1 && to != -1 && from != to)
            {
                adapter.ItemMoveEnded(from, to);
                from = -1;
                to = -1;
            }


            if (viewHolder is IItemTouchHolder)
                ((IItemTouchHolder)viewHolder).ItemClear();
        }
    }

    public interface IItemTouchAdapter
    {
        bool IsSliding { get; set; }

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