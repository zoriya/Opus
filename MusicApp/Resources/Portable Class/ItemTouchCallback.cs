using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Widget;
using System;

namespace MusicApp.Resources.Portable_Class
{
    public class ItemTouchCallback : ItemTouchHelper.Callback
    {
        private IItemTouchAdapter adapter;

        private int from = -1;
        private int to = -1;

        public override bool IsItemViewSwipeEnabled => true;
        public override bool IsLongPressDragEnabled => true;


        public ItemTouchCallback(IItemTouchAdapter adapter)
        {
            this.adapter = adapter;
        }

        public override int GetMovementFlags(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            int dragFlag = ItemTouchHelper.Up | ItemTouchHelper.Down;
            int swipeFlag = ItemTouchHelper.Left | ItemTouchHelper.Right;

            if (MusicPlayer.queue[MusicPlayer.CurrentID()].Name == viewHolder.ItemView.FindViewById<TextView>(Resource.Id.title).Text)
                return MakeMovementFlags(dragFlag, 0);

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
            adapter.ItemDismissed(viewHolder.AdapterPosition);
        }


        public override void OnChildDraw(Canvas c, RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder, float dX, float dY, int actionState, bool isCurrentlyActive)
        {
            if (actionState == ItemTouchHelper.ActionStateSwipe)
            {
                viewHolder.ItemView.Alpha = 1 - Math.Abs(dX) / viewHolder.ItemView.Width;
                viewHolder.ItemView.TranslationX = dX;
                MainActivity.instance.contentRefresh.SetEnabled(false);
                adapter.DisableRefresh(true);
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

            MainActivity.instance.contentRefresh.SetEnabled(true);
            adapter.DisableRefresh(false);

            if (from != -1 && to != -1 && from != to)
                adapter.ItemMoveEnded(from, to);


            if (viewHolder is IItemTouchHolder)
                ((IItemTouchHolder)viewHolder).ItemClear();
        }
    }

    public interface IItemTouchAdapter
    {
        bool RefreshDisabled();
        void DisableRefresh(bool disable);

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