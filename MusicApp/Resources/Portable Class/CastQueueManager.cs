using Android.Gms.Cast.Framework.Media;
using MusicApp.Resources.values;

namespace MusicApp.Resources.Portable_Class
{
    public class CastQueueManager : MediaQueue.Callback
    {
        public override void ItemsUpdatedAtIndexes(int[] indexes)
        {
            base.ItemsUpdatedAtIndexes(indexes);
            foreach (int index in indexes)
            {
                Song song = (Song)MusicPlayer.RemotePlayer.MediaQueue.GetItemAtIndex(index);

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
                    Queue.instance?.adapter.NotifyItemChanged(index, song.Title);
                    Home.instance?.QueueAdapter?.NotifyItemChanged(index, song.Title);
                }

                MusicPlayer.WaitForIndex.Remove(index);
            }
        }
    }
}