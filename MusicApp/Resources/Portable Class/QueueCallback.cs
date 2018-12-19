using Android.Gms.Cast.Framework.Media;
using Android.Support.V7.Widget;

namespace MusicApp.Resources.Portable_Class
{
    public class QueueCallback : MediaQueue.Callback
    {
        public RecyclerView.Adapter adapter;

        public QueueCallback(RecyclerView.Adapter adapter)
        {
            this.adapter = adapter;
        }


        public override void ItemsInsertedInRange(int insertIndex, int insertCount)
        {
            base.ItemsInsertedInRange(insertIndex, insertCount);
            adapter.NotifyItemRangeInserted(insertIndex, insertCount);
        }

        public override void ItemsReloaded()
        {
            base.ItemsReloaded();
            adapter.NotifyDataSetChanged();
        }

        public override void ItemsRemovedAtIndexes(int[] indexes)
        {
            base.ItemsRemovedAtIndexes(indexes);
            foreach(int index in indexes)
                adapter.NotifyItemRemoved(index);
        }

        public override void ItemsUpdatedAtIndexes(int[] indexes)
        {
            base.ItemsUpdatedAtIndexes(indexes);
            foreach (int index in indexes)
                adapter.NotifyItemChanged(index);
        }

        public override void MediaQueueChanged()
        {
            base.MediaQueueChanged();
            adapter.NotifyDataSetChanged();
        }

        public override void MediaQueueWillChange()
        {
            base.MediaQueueWillChange();
        }
    }
}