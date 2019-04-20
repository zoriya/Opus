using Android.Gms.Cast.Framework.Media;
using Opus.Api.Services;
using Opus.DataStructure;
using Opus.Fragments;

namespace Opus.Others
{
    public class CastQueueManager : MediaQueue.Callback
    {
        public override void ItemsInsertedInRange(int insertIndex, int insertCount)
        {
            base.ItemsInsertedInRange(insertIndex, insertCount);
            if(MusicPlayer.queue.Count == insertCount)
                MainActivity.instance.ShowSmallPlayer();
        }

        public override void ItemsReloaded()
        {
            base.ItemsReloaded();
            Queue.instance?.Refresh();
            Home.instance?.QueueAdapter?.NotifyDataSetChanged();
            Player.instance?.RefreshPlayer();
        }

        public override void ItemsRemovedAtIndexes(int[] indexes)
        {
            base.ItemsRemovedAtIndexes(indexes);
            foreach(int index in indexes)
            {
                Queue.instance?.NotifyItemRemoved(index);
                Home.instance?.QueueAdapter?.NotifyItemRemoved(index);

                if (index == MusicPlayer.CurrentID())
                    Player.instance.RefreshPlayer();
            }
        }

        public override void ItemsUpdatedAtIndexes(int[] indexes)
        {
            base.ItemsUpdatedAtIndexes(indexes);
            System.Console.WriteLine("&Item Updated at Index");
            foreach (int index in indexes)
            {
                Song song = (Song)MusicPlayer.RemotePlayer.MediaQueue.GetItemAtIndex(index);

                if (song == null && (index == MusicPlayer.currentID || index == MusicPlayer.currentID + 1))
                    continue;

                if (MusicPlayer.queue.Count > index)
                    MusicPlayer.queue[index] = song;
                else
                {
                    while (MusicPlayer.queue.Count < index)
                        MusicPlayer.queue.Add(null);

                    MusicPlayer.queue.Add(song);
                }

                if(song != null)
                {
                    Queue.instance?.NotifyItemChanged(index, song.Title);
                    Home.instance?.QueueAdapter?.NotifyItemChanged(index, song.Title);

                    if (index == MusicPlayer.CurrentID())
                        Player.instance.RefreshPlayer();
                }

                MusicPlayer.WaitForIndex.Remove(index);
            }
        }
    }
}