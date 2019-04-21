using Android.Database;
using Android.Support.V7.Widget;
using Android.Views;

namespace Opus.Adapter
{
    public abstract class BaseCursor<T> : RecyclerView.Adapter
    {
        public ICursor cursor;
        /// <summary>
        /// This is the number of items (for example headers) there is before the list represented by the cursor.
        /// </summary>
        public abstract int ItemBefore { get; }
        public int BaseCount { get { return cursor != null ? cursor.Count : 0; } }
        public override int ItemCount => BaseCount + ItemBefore;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (cursor.MoveToPosition(position - ItemBefore))
                OnBindViewHolder(holder, Convert(cursor));
        }
        public abstract void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, T item);

        public void SwapCursor(ICursor newCursor)
        {
            if (newCursor == cursor)
                return;

            if (newCursor != null)
            {
                MainActivity.instance.FindViewById(Resource.Id.loading).Visibility = ViewStates.Gone;
                cursor = newCursor;
                NotifyDataSetChanged();
            }
            else
            {
                NotifyItemRangeRemoved(0, ItemCount);
                cursor = null;
                MainActivity.instance.FindViewById(Resource.Id.loading).Visibility = ViewStates.Visible;
            }
        }

        public T GetItem(int position)
        {
            cursor.MoveToPosition(position - ItemBefore);
            return Convert(cursor);
        }

        public void OnClick(int position)
        {
            if (position >= ItemBefore)
            {
                cursor.MoveToPosition(position - ItemBefore);
                Clicked(Convert(cursor), position - ItemBefore);
            }
            else
                HeaderClicked(position);
        }
        public abstract void Clicked(T item, int position);
        public virtual void HeaderClicked(int position) { }

        public void OnLongClick(int position)
        {
            if (position >= ItemBefore)
            {
                cursor.MoveToPosition(position - ItemBefore);
                LongClicked(Convert(cursor), position - ItemBefore);
            }
            else
                HeaderLongClicked(position);
        }
        public abstract void LongClicked(T item, int positon);
        public virtual void HeaderLongClicked(int position) { }

        public abstract T Convert(ICursor cursor);
    }
}