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
                if (MusicPlayer.queue.Count > index)
                    MusicPlayer.queue[index] = (Song)MusicPlayer.RemotePlayer.MediaQueue.GetItemAtIndex(index);
                else
                {
                    while (MusicPlayer.queue.Count < index)
                        MusicPlayer.queue.Add(null);

                    MusicPlayer.queue.Add((Song)MusicPlayer.RemotePlayer.MediaQueue.GetItemAtIndex(index));
                }

                MusicPlayer.WaitForIndex.Remove(index);
            }
        }
    }
}