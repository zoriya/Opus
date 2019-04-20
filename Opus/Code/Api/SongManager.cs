using Opus.DataStructure;

namespace Opus.Api
{
    public class SongManager
    {
        /// <summary>
        /// Play a song, can be a local one or a youtube one. The class will handle it automatically.
        /// </summary>
        /// <param name="item"></param>
        public static void Play(Song item)
        {
            if (!item.IsYt)
                LocalManager.Play(item.Path);
            else
                YoutubeManager.Play(item);
        }

        /// <summary>
        /// Add a song to the next slot of the queue. Handle both youtube and local files.
        /// </summary>
        /// <param name="item"></param>
        public static void PlayNext(Song item)
        {
            if (!item.IsYt)
                LocalManager.PlayNext(item.Path);
            else
                YoutubeManager.PlayNext(item);
        }

        /// <summary>
        /// Add a song to the end of the queue. Handle both youtube and local files.
        /// </summary>
        /// <param name="item"></param>
        public static void PlayLast(Song item)
        {
            if (!item.IsYt)
                LocalManager.PlayLast(item.Path);
            else
                YoutubeManager.PlayLast(item);
        }
    }
}